using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Common.Rendering;
using SoulsOfTerra.Players;
using Terraria;
using Terraria.ModLoader;

namespace SoulsOfTerra.Systems;

[Autoload(Side = ModSide.Client)]
public class PixelatedRenderSystem : ModSystem
{
	private const float TargetScale = 0.5f;
	private const float CompositeScale = 2f;
	private static readonly List<IPixelatedDrawable> DrawQueue = new();
	private static RenderTarget2D pixelTarget;
	private static bool hasContent;

	/// <summary>Keeps half-resolution primitives aligned to Terraria's world-pixel grid.</summary>
	public static Vector2 CameraRemainder => new(Main.screenPosition.X % CompositeScale,
		Main.screenPosition.Y % CompositeScale);

	/// <summary>Maps ordinary screen coordinates into the world-anchored pixel target.</summary>
	public static Matrix PixelTransform
	{
		get
		{
			Matrix transform = Matrix.CreateScale(TargetScale);
			Vector2 cameraRemainder = CameraRemainder * TargetScale;
			transform.Translation = new Vector3(cameraRemainder, 0f);
			return transform;
		}
	}

	public override void Load()
	{
		On_Main.CheckMonoliths += DrawIntoTarget;
		On_Main.DrawItems += CompositeTarget;
		Main.OnResolutionChanged += ResizeTarget;
		Main.QueueMainThreadAction(CreateTarget);
	}

	public override void Unload()
	{
		On_Main.CheckMonoliths -= DrawIntoTarget;
		On_Main.DrawItems -= CompositeTarget;
		Main.OnResolutionChanged -= ResizeTarget;
		DrawQueue.Clear();
		hasContent = false;

		RenderTarget2D targetToDispose = pixelTarget;
		pixelTarget = null;
		if (targetToDispose is not null)
		{
			// Graphics resources must be released on Terraria's main thread during reloads.
			Main.QueueMainThreadAction(() =>
			{
				if (!targetToDispose.IsDisposed)
				{
					targetToDispose.Dispose();
				}
			});
		}
	}

	private static void ResizeTarget(Vector2 size)
	{
		// Resolution changes can arrive outside the graphics thread.
		Main.QueueMainThreadAction(CreateTarget);
	}

	private static void CreateTarget()
	{
		if (Main.dedServ)
		{
			return;
		}

		GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
		pixelTarget?.Dispose();
		hasContent = false;
		pixelTarget = new RenderTarget2D(graphicsDevice,
			Math.Max(1, (int)(Main.screenWidth * TargetScale)),
			Math.Max(1, (int)(Main.screenHeight * TargetScale)), false,
			SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
	}

	private static void DrawIntoTarget(On_Main.orig_CheckMonoliths orig)
	{
		orig();
		hasContent = false;
		if (Main.gameMenu || pixelTarget is null || pixelTarget.IsDisposed)
		{
			return;
		}

		CollectDrawables();
		if (DrawQueue.Count == 0)
		{
			return;
		}

		GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
		RenderTargetBinding[] previousTargets = graphicsDevice.GetRenderTargets();
		graphicsDevice.SetRenderTarget(pixelTarget);
		graphicsDevice.Clear(Color.Transparent);

		// Screen-space sprites are scaled into the same target used by primitive trails.
		BeginPixelBatch();
		foreach (IPixelatedDrawable drawable in DrawQueue)
		{
			drawable.DrawPixelated(Main.spriteBatch);
		}

		Main.spriteBatch.End();
		graphicsDevice.SetRenderTargets(previousTargets);
		hasContent = true;
	}

	/// <summary>Restarts the standard batch used while the pixel target is bound.</summary>
	public static void BeginPixelBatch()
	{
		GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
		Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp,
			DepthStencilState.None, Main.Rasterizer, null, PixelTransform);
		graphicsDevice.BlendState = BlendState.AlphaBlend;
		graphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
	}

	private static void CollectDrawables()
	{
		DrawQueue.Clear();
		foreach (Projectile projectile in Main.ActiveProjectiles)
		{
			if (projectile.ModProjectile is IPixelatedDrawable drawable)
			{
				DrawQueue.Add(drawable);
			}
		}

		foreach (Player player in Main.ActivePlayers)
		{
			SoulSpellPlayer spellPlayer = player.GetModPlayer<SoulSpellPlayer>();
			if (spellPlayer.HasDashVisual)
			{
				DrawQueue.Add(spellPlayer);
			}
		}
	}

	private static void CompositeTarget(On_Main.orig_DrawItems orig, Main self)
	{
		if (hasContent && pixelTarget is not null && !pixelTarget.IsDisposed)
		{
			// The camera remainder prevents the enlarged texels from swimming during movement.
			Main.spriteBatch.Draw(pixelTarget, -CameraRemainder, null, Color.White, 0f, Vector2.Zero,
				CompositeScale, SpriteEffects.None, 0f);
		}

		orig(self);
	}
}
