using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace SoulsOfTerra.Systems;

/// <summary>Renders the Mutation UI's animated frame on the shared two-pixel visual grid.</summary>
[Autoload(Side = ModSide.Client)]
internal sealed class MutationUIFrameRenderer : ModSystem
{
	private const float TargetScale = 0.5f;
	private const float CompositeScale = 2f;
	private const float FramePadding = 12f;
	private const float HoverRampDuration = 0.12f;
	private const float SelectionDuration = 0.45f;
	private const float InsertionDuration = 0.6f;
	private const float RemovalDuration = 0.5f;
	private const int AmbientParticleCount = 26;

	private static readonly List<FrameSegment> CornerSegments = new();
	private static readonly AmbientParticle[] AmbientParticles = new AmbientParticle[AmbientParticleCount];
	private static RenderTarget2D frameTarget;
	private static bool targetCreationQueued;
	private static bool acceptingTargets = true;
	private static int panelWidth;
	private static int panelHeight;
	private static int configuredSeed;
	private static bool hasContent;
	private static float animationTime;
	private static float hoverStrength;
	private static int hoveredSlot = -1;
	private static bool closeHovered;
	private static Vector2[] socketCenters = Array.Empty<Vector2>();
	private static Vector2 closeCenter;
	private static FrameAnimation activeAnimation;
	private static int activeSlot = -1;
	private static float animationRemaining;

	private enum FrameAnimation : byte
	{
		None,
		Selection,
		Insertion,
		Removal
	}

	private readonly record struct FrameSegment(Vector2 Start, Vector2 End, float Phase, float Strength);
	private readonly record struct AmbientParticle(float PerimeterOffset, float Speed, float Phase, int Size);

	public override void Load()
	{
		if (!Main.dedServ)
		{
			acceptingTargets = true;
			On_Main.CheckMonoliths += DrawIntoTarget;
		}
	}

	public override void Unload()
	{
		On_Main.CheckMonoliths -= DrawIntoTarget;
		acceptingTargets = false;
		targetCreationQueued = false;
		CornerSegments.Clear();
		socketCenters = Array.Empty<Vector2>();
		hasContent = false;
		RenderTarget2D targetToDispose = frameTarget;
		frameTarget = null;
		if (targetToDispose is not null)
		{
			// Graphics resources must be disposed on Terraria's main thread during reloads.
			Main.QueueMainThreadAction(() =>
			{
				if (!targetToDispose.IsDisposed)
				{
					targetToDispose.Dispose();
				}
			});
		}
	}

	internal static void Configure(Point16 altarPosition, float requestedWidth, float requestedHeight,
		Vector2[] requestedSocketCenters, Vector2 requestedCloseCenter)
	{
		int width = Math.Max(1, (int)MathF.Round(requestedWidth));
		int height = Math.Max(1, (int)MathF.Round(requestedHeight));
		int seed = unchecked(altarPosition.X * 73856093 ^ altarPosition.Y * 19349663);
		bool dimensionsChanged = width != panelWidth || height != panelHeight;
		bool geometryChanged = dimensionsChanged || seed != configuredSeed;
		panelWidth = width;
		panelHeight = height;
		configuredSeed = seed;
		socketCenters = requestedSocketCenters;
		closeCenter = requestedCloseCenter;

		if (dimensionsChanged || frameTarget is null || frameTarget.IsDisposed)
		{
			QueueTargetCreation();
		}
		if (geometryChanged)
		{
			GenerateGeometry(seed);
		}
	}

	internal static void Update(GameTime gameTime)
	{
		if (!GraftingAltarSystem.IsOpen || !Main.hasFocus)
		{
			return;
		}

		float delta = Math.Min(0.05f, (float)gameTime.ElapsedGameTime.TotalSeconds);
		animationTime += delta;
		float hoverTarget = hoveredSlot >= 0 || closeHovered ? 1f : 0f;
		hoverStrength = MoveTowards(hoverStrength, hoverTarget, delta / HoverRampDuration);
		if (animationRemaining > 0f)
		{
			animationRemaining = Math.Max(0f, animationRemaining - delta);
			if (animationRemaining <= 0f)
			{
				activeAnimation = FrameAnimation.None;
				activeSlot = -1;
			}
		}
	}

	internal static void SetHoveredSlot(int slot)
	{
		hoveredSlot = slot;
		if (slot >= 0)
		{
			closeHovered = false;
		}
	}

	internal static void SetCloseHovered(bool hovered)
	{
		closeHovered = hovered;
		if (hovered)
		{
			hoveredSlot = -1;
		}
	}

	internal static void TriggerSelection(int slot) => StartAnimation(FrameAnimation.Selection, slot,
		SelectionDuration);

	internal static void TriggerInsertion(int slot) => StartAnimation(FrameAnimation.Insertion, slot,
		InsertionDuration);

	internal static void TriggerRemoval(int slot) => StartAnimation(FrameAnimation.Removal, slot,
		RemovalDuration);

	internal static void ResetInteraction()
	{
		hoveredSlot = -1;
		closeHovered = false;
		hoverStrength = 0f;
		activeAnimation = FrameAnimation.None;
		activeSlot = -1;
		animationRemaining = 0f;
	}

	internal static void Draw(SpriteBatch spriteBatch, Vector2 panelTopLeft)
	{
		if (!hasContent || frameTarget is null || frameTarget.IsDisposed)
		{
			return;
		}

		// Only the effect target uses point sampling; ordinary UI text resumes with linear sampling.
		spriteBatch.End();
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
			DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);
		Vector2 position = SnapEven(panelTopLeft - new Vector2(FramePadding));
		spriteBatch.Draw(frameTarget, position, null, Color.White, 0f, Vector2.Zero,
			CompositeScale, SpriteEffects.None, 0f);
		spriteBatch.End();
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
			DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);
	}

	private static void StartAnimation(FrameAnimation animation, int slot, float duration)
	{
		activeAnimation = animation;
		activeSlot = slot;
		animationRemaining = duration;
	}

	private static void QueueTargetCreation()
	{
		if (targetCreationQueued || !acceptingTargets)
		{
			return;
		}

		targetCreationQueued = true;
		Main.QueueMainThreadAction(() =>
		{
			targetCreationQueued = false;
			if (acceptingTargets)
			{
				CreateTarget();
			}
		});
	}

	private static void CreateTarget()
	{
		if (Main.dedServ || panelWidth <= 0 || panelHeight <= 0)
		{
			return;
		}

		frameTarget?.Dispose();
		int width = Math.Max(1, (int)MathF.Ceiling((panelWidth + FramePadding * 2f) * TargetScale));
		int height = Math.Max(1, (int)MathF.Ceiling((panelHeight + FramePadding * 2f) * TargetScale));
		frameTarget = new RenderTarget2D(Main.instance.GraphicsDevice, width, height, false,
			SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
		hasContent = false;
	}

	private static void GenerateGeometry(int seed)
	{
		CornerSegments.Clear();
		Random random = new(seed);
		GetFrameBounds(out float left, out float top, out float right, out float bottom);
		Vector2[] corners =
		{
			new(left, top), new(right, top), new(right, bottom), new(left, bottom)
		};

		for (int cornerIndex = 0; cornerIndex < corners.Length; cornerIndex++)
		{
			Vector2 corner = corners[cornerIndex];
			float horizontalDirection = cornerIndex is 0 or 3 ? 1f : -1f;
			float verticalDirection = cornerIndex < 2 ? 1f : -1f;
			for (int branch = 0; branch < 6; branch++)
			{
				float horizontalLength = random.Next(5, 17);
				float verticalLength = random.Next(5, 15);
				float offset = random.Next(0, 7);
				Vector2 horizontalStart = corner + new Vector2(horizontalDirection * offset, verticalDirection * branch);
				Vector2 verticalStart = corner + new Vector2(horizontalDirection * branch, verticalDirection * offset);
				float phase = random.NextSingle() * MathHelper.TwoPi;
				float strength = 0.55f + random.NextSingle() * 0.45f;
				CornerSegments.Add(new FrameSegment(horizontalStart,
					horizontalStart + new Vector2(horizontalDirection * horizontalLength, 0f), phase, strength));
				CornerSegments.Add(new FrameSegment(verticalStart,
					verticalStart + new Vector2(0f, verticalDirection * verticalLength), phase + 1.7f, strength));
			}
		}

		for (int index = 0; index < AmbientParticles.Length; index++)
		{
			AmbientParticles[index] = new AmbientParticle(random.NextSingle(),
				0.008f + random.NextSingle() * 0.018f, random.NextSingle() * MathHelper.TwoPi,
				index % 7 == 0 ? 2 : 1);
		}
	}

	private static void DrawIntoTarget(On_Main.orig_CheckMonoliths orig)
	{
		orig();
		hasContent = false;
		if (!GraftingAltarSystem.IsOpen || frameTarget is null || frameTarget.IsDisposed)
		{
			return;
		}

		GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
		RenderTargetBinding[] previousTargets = graphicsDevice.GetRenderTargets();
		graphicsDevice.SetRenderTarget(frameTarget);
		graphicsDevice.Clear(Color.Transparent);
		Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp,
			DepthStencilState.None, Main.Rasterizer);

		DrawAmbientFrame(Main.spriteBatch);
		DrawInteraction(Main.spriteBatch);
		Main.spriteBatch.End();
		graphicsDevice.SetRenderTargets(previousTargets);
		hasContent = true;
	}

	private static void DrawAmbientFrame(SpriteBatch spriteBatch)
	{
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		foreach (FrameSegment segment in CornerSegments)
		{
			float flicker = 0.5f + MathF.Sin(animationTime * 1.15f + segment.Phase) * 0.18f;
			Color color = SoullessUIPalette.Accent * (flicker * segment.Strength * 0.48f);
			DrawLine(spriteBatch, pixel, segment.Start, segment.End, 1f, color);
		}

		// Two dim packets keep the perimeter alive while leaving most of each edge dark.
		DrawPerimeterPacket(spriteBatch, animationTime * 0.027f, SoullessUIPalette.Accent, 0.34f, 7);
		DrawPerimeterPacket(spriteBatch, animationTime * 0.019f + 0.47f,
			SoullessUIPalette.AccentMuted, 0.2f, 5);
		DrawAmbientParticles(spriteBatch, pixel);
		DrawTitleWisp(spriteBatch, pixel);
	}

	private static void DrawAmbientParticles(SpriteBatch spriteBatch, Texture2D pixel)
	{
		float perimeter = GetPerimeterLength();
		foreach (AmbientParticle particle in AmbientParticles)
		{
			float travel = (particle.PerimeterOffset + animationTime * particle.Speed) % 1f;
			Vector2 position = PointOnPerimeter(travel * perimeter);
			float flicker = 0.24f + MathF.Sin(animationTime * 1.8f + particle.Phase) * 0.1f;
			DrawPixel(spriteBatch, pixel, position, particle.Size,
				SoullessUIPalette.AccentText * flicker);
		}
	}

	private static void DrawTitleWisp(SpriteBatch spriteBatch, Texture2D pixel)
	{
		Vector2 center = ToTarget(new Vector2(13f, 27f));
		for (int index = 0; index < 6; index++)
		{
			float phase = animationTime * (0.45f + index * 0.035f) + index * 1.31f;
			Vector2 offset = new(MathF.Cos(phase) * (3f + index % 2), MathF.Sin(phase * 1.3f) * 4f);
			DrawPixel(spriteBatch, pixel, center + offset, index == 0 ? 2 : 1,
				SoullessUIPalette.AccentText * (0.34f + index * 0.045f));
		}
	}

	private static void DrawInteraction(SpriteBatch spriteBatch)
	{
		if (hoverStrength > 0f)
		{
			if (closeHovered)
			{
				DrawCloseCurl(spriteBatch, hoverStrength);
			}
			else if (hoveredSlot >= 0 && hoveredSlot < socketCenters.Length)
			{
				DrawHoverCurrent(spriteBatch, hoveredSlot, hoverStrength);
			}
		}

		if (activeAnimation == FrameAnimation.None || activeSlot < 0 || activeSlot >= socketCenters.Length)
		{
			return;
		}

		float duration = activeAnimation switch
		{
			FrameAnimation.Selection => SelectionDuration,
			FrameAnimation.Insertion => InsertionDuration,
			FrameAnimation.Removal => RemovalDuration,
			_ => 1f
		};
		float progress = 1f - animationRemaining / duration;
		switch (activeAnimation)
		{
			case FrameAnimation.Selection:
				DrawSelectionSweep(spriteBatch, activeSlot, progress);
				break;
			case FrameAnimation.Insertion:
				DrawInsertion(spriteBatch, activeSlot, progress);
				break;
			case FrameAnimation.Removal:
				DrawRemoval(spriteBatch, activeSlot, progress);
				break;
		}
	}

	private static void DrawHoverCurrent(SpriteBatch spriteBatch, int slot, float strength)
	{
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Vector2 target = ToTarget(socketCenters[slot]);
		Vector2 corner = GetNearestCorner(target);
		float packet = (animationTime * 0.75f) % 1f;
		for (int index = 0; index < 6; index++)
		{
			float progress = MathHelper.Clamp(packet - index * 0.055f, 0f, 1f);
			Vector2 position = Vector2.Lerp(corner, target, SmoothStep(progress));
			DrawPixel(spriteBatch, pixel, position, index < 2 ? 2 : 1,
				SoullessUIPalette.AccentText * (strength * (1f - index / 7f) * 0.72f));
		}
	}

	private static void DrawCloseCurl(SpriteBatch spriteBatch, float strength)
	{
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Vector2 center = ToTarget(closeCenter);
		for (int index = 0; index < 9; index++)
		{
			float angle = animationTime * 2.1f + MathHelper.TwoPi * index / 9f;
			float radius = 5f + index * 0.22f;
			Vector2 position = center + angle.ToRotationVector2() * radius;
			DrawPixel(spriteBatch, pixel, position, index < 2 ? 2 : 1,
				SoullessUIPalette.AccentText * (strength * (0.32f + index * 0.035f)));
		}
	}

	private static void DrawSelectionSweep(SpriteBatch spriteBatch, int slot, float progress)
	{
		Vector2 target = ToTarget(socketCenters[slot]);
		float startDistance = GetNearestPerimeterDistance(target);
		float perimeter = GetPerimeterLength();
		DrawPerimeterPacket(spriteBatch, (startDistance + progress * perimeter) / perimeter,
			SoullessUIPalette.AccentBright, 0.92f, 12);
	}

	private static void DrawInsertion(SpriteBatch spriteBatch, int slot, float progress)
	{
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Vector2 target = ToTarget(socketCenters[slot]);
		GetFrameCorners(out Vector2[] corners);
		float convergence = SmoothStep(MathHelper.Clamp(progress / 0.72f, 0f, 1f));
		foreach (Vector2 corner in corners)
		{
			for (int index = 0; index < 5; index++)
			{
				float trail = MathHelper.Clamp(convergence - index * 0.07f, 0f, 1f);
				Vector2 position = Vector2.Lerp(corner, target, trail);
				DrawPixel(spriteBatch, pixel, position, index < 2 ? 2 : 1,
					SoullessUIPalette.AccentText * ((1f - index / 6f) * 0.88f));
			}
		}

		if (progress > 0.68f)
		{
			float pulse = MathF.Sin((progress - 0.68f) / 0.32f * MathHelper.Pi);
			foreach (FrameSegment segment in CornerSegments)
			{
				DrawLine(spriteBatch, pixel, segment.Start, segment.End, 1f,
					SoullessUIPalette.AccentBright * (pulse * 0.78f));
			}
		}
	}

	private static void DrawRemoval(SpriteBatch spriteBatch, int slot, float progress)
	{
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Vector2 source = ToTarget(socketCenters[slot]);
		GetFrameCorners(out Vector2[] corners);
		float expansion = SmoothStep(progress);
		for (int cornerIndex = 0; cornerIndex < corners.Length; cornerIndex++)
		{
			Vector2 direction = corners[cornerIndex] - source;
			for (int fragment = 0; fragment < 7; fragment++)
			{
				float offset = fragment * 0.055f;
				float travel = MathHelper.Clamp(expansion - offset, 0f, 1f);
				Vector2 perpendicular = direction.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
				float scatter = MathF.Sin(fragment * 2.17f + cornerIndex) * progress * 4f;
				Vector2 position = Vector2.Lerp(source, corners[cornerIndex], travel) + perpendicular * scatter;
				DrawPixel(spriteBatch, pixel, position, fragment < 2 ? 2 : 1,
					SoullessUIPalette.Warning * ((1f - progress * 0.58f) * (1f - fragment / 8f)));
			}
		}
	}

	private static void DrawPerimeterPacket(SpriteBatch spriteBatch, float normalizedPosition, Color color,
		float opacity, int trailLength)
	{
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		float perimeter = GetPerimeterLength();
		float head = ((normalizedPosition % 1f) + 1f) % 1f * perimeter;
		for (int index = 0; index < trailLength; index++)
		{
			Vector2 position = PointOnPerimeter(head - index * 2f);
			float strength = 1f - index / (float)trailLength;
			DrawPixel(spriteBatch, pixel, position, index < 2 ? 2 : 1,
				color * (opacity * strength));
		}
	}

	private static Vector2 PointOnPerimeter(float distance)
	{
		GetFrameBounds(out float left, out float top, out float right, out float bottom);
		float width = right - left;
		float height = bottom - top;
		float perimeter = (width + height) * 2f;
		distance = (distance % perimeter + perimeter) % perimeter;
		if (distance <= width)
		{
			return new Vector2(left + distance, top);
		}
		distance -= width;
		if (distance <= height)
		{
			return new Vector2(right, top + distance);
		}
		distance -= height;
		if (distance <= width)
		{
			return new Vector2(right - distance, bottom);
		}
		return new Vector2(left, bottom - (distance - width));
	}

	private static float GetNearestPerimeterDistance(Vector2 point)
	{
		GetFrameBounds(out float left, out float top, out float right, out float bottom);
		float width = right - left;
		float height = bottom - top;
		float[] distances =
		{
			Math.Abs(point.Y - top), Math.Abs(point.X - right),
			Math.Abs(point.Y - bottom), Math.Abs(point.X - left)
		};
		int edge = 0;
		for (int index = 1; index < distances.Length; index++)
		{
			if (distances[index] < distances[edge])
			{
				edge = index;
			}
		}

		return edge switch
		{
			0 => MathHelper.Clamp(point.X - left, 0f, width),
			1 => width + MathHelper.Clamp(point.Y - top, 0f, height),
			2 => width + height + MathHelper.Clamp(right - point.X, 0f, width),
			_ => width * 2f + height + MathHelper.Clamp(bottom - point.Y, 0f, height)
		};
	}

	private static Vector2 GetNearestCorner(Vector2 point)
	{
		GetFrameCorners(out Vector2[] corners);
		Vector2 nearest = corners[0];
		float nearestDistance = Vector2.DistanceSquared(point, nearest);
		for (int index = 1; index < corners.Length; index++)
		{
			float distance = Vector2.DistanceSquared(point, corners[index]);
			if (distance < nearestDistance)
			{
				nearest = corners[index];
				nearestDistance = distance;
			}
		}
		return nearest;
	}

	private static void GetFrameCorners(out Vector2[] corners)
	{
		GetFrameBounds(out float left, out float top, out float right, out float bottom);
		corners = new[]
		{
			new Vector2(left, top), new Vector2(right, top),
			new Vector2(right, bottom), new Vector2(left, bottom)
		};
	}

	private static void GetFrameBounds(out float left, out float top, out float right, out float bottom)
	{
		left = FramePadding * TargetScale;
		top = FramePadding * TargetScale;
		right = left + panelWidth * TargetScale - 1f;
		bottom = top + panelHeight * TargetScale - 1f;
	}

	private static float GetPerimeterLength()
	{
		GetFrameBounds(out float left, out float top, out float right, out float bottom);
		return ((right - left) + (bottom - top)) * 2f;
	}

	private static Vector2 ToTarget(Vector2 panelPosition) =>
		(panelPosition + new Vector2(FramePadding)) * TargetScale;

	private static void DrawPixel(SpriteBatch spriteBatch, Texture2D pixel, Vector2 center, int size, Color color)
	{
		int halfSize = size / 2;
		Rectangle destination = new((int)MathF.Round(center.X) - halfSize,
			(int)MathF.Round(center.Y) - halfSize, size, size);
		spriteBatch.Draw(pixel, destination, color);
	}

	private static void DrawLine(SpriteBatch spriteBatch, Texture2D pixel, Vector2 start, Vector2 end,
		float width, Color color)
	{
		Vector2 delta = end - start;
		if (delta.LengthSquared() <= 0.001f)
		{
			return;
		}
		spriteBatch.Draw(pixel, start, null, color, delta.ToRotation(),
			new Vector2(0f, pixel.Height * 0.5f),
			new Vector2(delta.Length() / pixel.Width, width / pixel.Height), SpriteEffects.None, 0f);
	}

	private static float SmoothStep(float value) => value * value * (3f - 2f * value);

	private static float MoveTowards(float current, float target, float maximumDelta)
	{
		if (Math.Abs(target - current) <= maximumDelta)
		{
			return target;
		}
		return current + Math.Sign(target - current) * maximumDelta;
	}

	private static Vector2 SnapEven(Vector2 position) => new(
		MathF.Round(position.X * 0.5f) * 2f,
		MathF.Round(position.Y * 0.5f) * 2f);
}
