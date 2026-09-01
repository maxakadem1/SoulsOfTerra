using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Content.Projectiles;
using Terraria;
using Terraria.ModLoader;

namespace SoulsOfTerra.Systems;

/// <summary>Pixelates the imbuement-orbit souls the same way as the HUD counter.</summary>
[Autoload(Side = ModSide.Client)]
public class ImbuementOrbitSoulRenderer : ModSystem
{
	internal const int SoulCount = 8;
	private const int CellSize = 32;
	private const float CompositeScale = 2f;
	private const float OrbitSpeed = 0.78f;
	private const float MinScale = 0.45f;
	private const float MaxScale = 0.62f;

	private static RenderTarget2D atlas;
	private static bool hasContent;

	public override void Load()
	{
		if (Main.dedServ)
		{
			return;
		}

		On_Main.CheckMonoliths += DrawAtlas;
		Main.QueueMainThreadAction(CreateAtlas);
	}

	public override void Unload()
	{
		On_Main.CheckMonoliths -= DrawAtlas;
		hasContent = false;
		RenderTarget2D targetToDispose = atlas;
		atlas = null;
		if (targetToDispose is not null)
		{
			Main.QueueMainThreadAction(() =>
			{
				if (!targetToDispose.IsDisposed)
				{
					targetToDispose.Dispose();
				}
			});
		}
	}

	internal static void Draw(SpriteBatch spriteBatch, Vector2 center, bool drawFront)
	{
		if (!hasContent || atlas is null || atlas.IsDisposed)
		{
			return;
		}

		spriteBatch.End();
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
			DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);

		Vector2 origin = new(CellSize * 0.5f);
		for (int index = 0; index < SoulCount; index++)
		{
			GetSoulState(index, out _, out _, out _, out float depth, out float phase);
			if ((depth >= 0f) != drawFront)
			{
				continue;
			}

			Vector2 position = SnapToPixelGrid(center + GetOrbitOffset(index, phase, depth));
			Rectangle source = new(index * CellSize, 0, CellSize, CellSize);
			spriteBatch.Draw(atlas, position, source, Color.White, 0f, origin, CompositeScale, SpriteEffects.None, 0f);
		}

		spriteBatch.End();
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
			DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);
	}

	private static void CreateAtlas()
	{
		atlas?.Dispose();
		hasContent = false;
		atlas = new RenderTarget2D(Main.instance.GraphicsDevice, CellSize * SoulCount, CellSize, false,
			SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
	}

	private static void DrawAtlas(On_Main.orig_CheckMonoliths orig)
	{
		orig();
		hasContent = false;
		// Both stations use the same resonant soul animation and render target.
		bool isResonating = SoulMenuSystem.IsImbuementResonating() || SoulApparatusSystem.IsDissolutionResonating();
		if (Main.gameMenu || atlas is null || atlas.IsDisposed || !isResonating)
		{
			return;
		}

		GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
		RenderTargetBinding[] previousTargets = graphicsDevice.GetRenderTargets();
		graphicsDevice.SetRenderTarget(atlas);
		graphicsDevice.Clear(Color.Transparent);

		// Half-resolution cells, then 2x point-sampled blit — same two-pixel grid as the HUD soul.
		Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp,
			DepthStencilState.None, Main.Rasterizer, null, Matrix.CreateScale(0.5f));
		for (int index = 0; index < SoulCount; index++)
		{
			GetSoulState(index, out long visualSouls, out float opacity, out float scale, out _, out _);
			Vector2 cellCenter = new(index * CellSize * 2f + CellSize, CellSize);
			SoulOrbProjectile.DrawSoulVisualAt(cellCenter, visualSouls, opacity, scale, index * 2.1f);
		}

		Main.spriteBatch.End();
		graphicsDevice.SetRenderTargets(previousTargets);
		hasContent = true;
	}

	private static void GetSoulState(int index, out long visualSouls, out float opacity, out float scale,
		out float depth, out float phase)
	{
		// Log-spaced values walk the pale → green → blue → purple pickup gradient.
		float valueLog = 1f + index / (float)(SoulCount - 1) * 3f;
		visualSouls = Math.Max(1L, (long)MathF.Round(MathF.Pow(10f, valueLog)));
		phase = Main.GlobalTimeWrappedHourly * OrbitSpeed + MathHelper.TwoPi * index / SoulCount;
		depth = MathF.Sin(phase);
		float depthProgress = (depth + 1f) * 0.5f;
		opacity = MathHelper.Lerp(0.42f, 0.88f, depthProgress);
		scale = MathHelper.Lerp(MinScale, MaxScale, depthProgress);
	}

	private static Vector2 GetOrbitOffset(int index, float phase, float depth)
	{
		float wobble = MathF.Sin(Main.GlobalTimeWrappedHourly * 1.35f + index * 1.7f);
		return new Vector2(MathF.Cos(phase) * (70f + wobble * 4f), depth * (33f + wobble * 2f));
	}

	private static Vector2 SnapToPixelGrid(Vector2 position) => new(
		MathF.Round(position.X * 0.5f) * 2f,
		MathF.Round(position.Y * 0.5f) * 2f);
}
