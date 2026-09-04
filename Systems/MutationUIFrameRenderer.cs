using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Common;
using SoulsOfTerra.Players;
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
	private const float FramePadding = 48f;
	private const float HoverRampDuration = 0.12f;
	private const float SelectionDuration = 0.45f;
	private const float InsertionDuration = 0.6f;
	private const float RemovalDuration = 0.5f;
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
	private static int selectedSlot = -1;
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
		panelWidth = width;
		panelHeight = height;
		configuredSeed = seed;
		socketCenters = requestedSocketCenters;
		closeCenter = requestedCloseCenter;

		if (dimensionsChanged || frameTarget is null || frameTarget.IsDisposed)
		{
			QueueTargetCreation();
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

	internal static void SetSelectedSlot(int slot)
	{
		selectedSlot = slot;
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
		selectedSlot = -1;
		closeHovered = false;
		hoverStrength = 0f;
		activeAnimation = FrameAnimation.None;
		activeSlot = -1;
		animationRemaining = 0f;
	}

	internal static void DrawInteraction(SpriteBatch spriteBatch, Vector2 panelTopLeft)
	{
		if (!hasContent || frameTarget is null || frameTarget.IsDisposed)
		{
			return;
		}

		// Only the interaction target uses point sampling; ordinary UI text resumes with linear sampling.
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

		DrawInteraction(Main.spriteBatch);
		Main.spriteBatch.End();
		graphicsDevice.SetRenderTargets(previousTargets);
		hasContent = true;
	}


	private static void DrawInteraction(SpriteBatch spriteBatch)
	{
		if (Main.LocalPlayer.active)
		{
			MutationPlayer mutationPlayer = Main.LocalPlayer.GetModPlayer<MutationPlayer>();
			for (int slot = 0; slot < socketCenters.Length; slot++)
			{
				if (mutationPlayer.GetMutation(slot) != MutationId.None)
				{
					DrawOccupiedDrift(spriteBatch, slot);
				}
			}
		}

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

		if (selectedSlot >= 0 && selectedSlot < socketCenters.Length && selectedSlot != hoveredSlot
			&& !closeHovered)
		{
			DrawHoverCurrent(spriteBatch, selectedSlot, Math.Max(0.42f, hoverStrength * 0.5f));
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
		float head = animationTime * 1.8f;
		for (int index = 0; index < 7; index++)
		{
			float angle = head - index * 0.16f;
			Vector2 position = target + angle.ToRotationVector2() * 11f;
			DrawPixel(spriteBatch, pixel, position, index < 2 ? 2 : 1,
				SoullessUIPalette.AccentText * (strength * (1f - index / 8f) * 0.58f));
		}
	}

	private static void DrawOccupiedDrift(SpriteBatch spriteBatch, int slot)
	{
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Vector2 target = ToTarget(socketCenters[slot]);
		float head = animationTime * 0.95f + slot * 1.7f;
		for (int index = 0; index < 6; index++)
		{
			float angle = head - index * 0.22f;
			Vector2 position = target + angle.ToRotationVector2() * 10f;
			DrawPixel(spriteBatch, pixel, position, index < 2 ? 2 : 1,
				SoullessUIPalette.Accent * ((1f - index / 7f) * 0.42f));
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
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Vector2 target = ToTarget(socketCenters[slot]);
		float head = progress * MathHelper.TwoPi;
		for (int index = 0; index < 12; index++)
		{
			float angle = head - index * 0.13f;
			float strength = 1f - index / 12f;
			DrawPixel(spriteBatch, pixel, target + angle.ToRotationVector2() * 11f, index < 3 ? 2 : 1,
				SoullessUIPalette.AccentBright * (strength * 0.86f));
		}
	}

	private static void DrawInsertion(SpriteBatch spriteBatch, int slot, float progress)
	{
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Vector2 target = ToTarget(socketCenters[slot]);
		float convergence = SmoothStep(MathHelper.Clamp(progress / 0.72f, 0f, 1f));
		for (int ray = 0; ray < 8; ray++)
		{
			Vector2 direction = (MathHelper.TwoPi * ray / 8f + 0.2f).ToRotationVector2();
			for (int index = 0; index < 5; index++)
			{
				float trail = MathHelper.Clamp(convergence - index * 0.07f, 0f, 1f);
				Vector2 position = target + direction * MathHelper.Lerp(28f, 0f, trail);
				DrawPixel(spriteBatch, pixel, position, index < 2 ? 2 : 1,
					SoullessUIPalette.AccentText * ((1f - index / 6f) * 0.88f));
			}
		}

		if (progress > 0.68f)
		{
			float pulse = MathF.Sin((progress - 0.68f) / 0.32f * MathHelper.Pi);
			for (int index = 0; index < 16; index++)
			{
				float angle = MathHelper.TwoPi * index / 16f;
				Vector2 position = target + angle.ToRotationVector2() * MathHelper.Lerp(7f, 18f, pulse);
				DrawPixel(spriteBatch, pixel, position, index % 4 == 0 ? 2 : 1,
					SoullessUIPalette.AccentBright * (pulse * 0.72f));
			}
		}
	}

	private static void DrawRemoval(SpriteBatch spriteBatch, int slot, float progress)
	{
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Vector2 source = ToTarget(socketCenters[slot]);
		float expansion = SmoothStep(progress);
		for (int fragment = 0; fragment < 20; fragment++)
		{
			float angle = MathHelper.TwoPi * fragment / 20f + MathF.Sin(fragment * 2.17f) * 0.18f;
			float distance = expansion * (15f + fragment % 5 * 3f);
			Vector2 position = source + angle.ToRotationVector2() * distance;
			DrawPixel(spriteBatch, pixel, position, fragment % 5 == 0 ? 2 : 1,
				SoullessUIPalette.Warning * ((1f - progress * 0.68f) * (0.55f + fragment % 3 * 0.12f)));
		}
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
