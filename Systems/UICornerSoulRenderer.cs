using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Content.Projectiles;
using Terraria;
using Terraria.ModLoader;

namespace SoulsOfTerra.Systems;

/// <summary>Draws reusable pickup-soul clusters around UI panel corners on the two-pixel grid.</summary>
[Autoload(Side = ModSide.Client)]
internal sealed class UICornerSoulRenderer : ModSystem
{
	private const int SoulCount = 12;
	private const int CellSize = 64;
	private const float CompositeScale = 2f;
	private const float SoulRenderScale = 0.82f;
	private static readonly long[] SoulValues = { 10L, 100L, 1000L, 10000L };

	private static RenderTarget2D atlas;
	private static bool hasContent;
	private static float animationTime;

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
		RenderTarget2D atlasToDispose = atlas;
		atlas = null;
		if (atlasToDispose is not null)
		{
			// FNA graphics resources must be released on Terraria's main thread.
			Main.QueueMainThreadAction(() =>
			{
				if (!atlasToDispose.IsDisposed)
				{
					atlasToDispose.Dispose();
				}
			});
		}
	}

	public override void PostUpdateEverything()
	{
		if (Main.hasFocus && IsAnySupportedUIOpen())
		{
			animationTime += 1f / 60f;
		}
	}

	internal static void Draw(SpriteBatch spriteBatch, Rectangle panel, int seed)
	{
		if (!hasContent || atlas is null || atlas.IsDisposed)
		{
			return;
		}

		spriteBatch.End();
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
			DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);

		Vector2[] corners =
		{
			new(panel.Left, panel.Top), new(panel.Right, panel.Top),
			new(panel.Right, panel.Bottom), new(panel.Left, panel.Bottom)
		};
		Vector2 sourceOrigin = new(CellSize * 0.5f);
		for (int index = 0; index < SoulCount; index++)
		{
			int corner = index / 3;
			uint hash = Hash(seed, index);
			float phase = Unit(hash) * MathHelper.TwoPi;
			float direction = (hash & 1u) == 0u ? -1f : 1f;
			float speed = direction * MathHelper.Lerp(0.56f, 1.18f, Unit(Hash(hash, 1)));
			float angle = phase + animationTime * speed;
			float wobblePhase = Unit(Hash(hash, 2)) * MathHelper.TwoPi;
			float irregularity = MathF.Sin(angle * 2f + wobblePhase);
			float radiusX = MathHelper.Lerp(14f, 26f, Unit(Hash(hash, 3))) + irregularity * 2.5f;
			float radiusY = MathHelper.Lerp(12f, 22f, Unit(Hash(hash, 4))) + irregularity * 1.6f;
			Vector2 orbit = new(MathF.Cos(angle) * radiusX, MathF.Sin(angle) * radiusY);
			Vector2 position = SnapEven(corners[corner] + orbit);
			float scale = MathHelper.Lerp(0.8f, 1.18f, Unit(Hash(hash, 5)));
			float opacity = MathHelper.Lerp(0.58f, 0.88f, Unit(Hash(hash, 6)));
			Rectangle source = new(index * CellSize, 0, CellSize, CellSize);
			spriteBatch.Draw(atlas, position, source, Color.White * opacity, 0f, sourceOrigin,
				CompositeScale * scale, SpriteEffects.None, 0f);
		}

		spriteBatch.End();
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
			DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);
	}

	private static void CreateAtlas()
	{
		atlas?.Dispose();
		atlas = new RenderTarget2D(Main.instance.GraphicsDevice, CellSize * SoulCount, CellSize, false,
			SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
		hasContent = false;
	}

	private static void DrawAtlas(On_Main.orig_CheckMonoliths orig)
	{
		orig();
		hasContent = false;
		if (atlas is null || atlas.IsDisposed || !IsAnySupportedUIOpen())
		{
			return;
		}

		GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
		RenderTargetBinding[] previousTargets = graphicsDevice.GetRenderTargets();
		graphicsDevice.SetRenderTarget(atlas);
		graphicsDevice.Clear(Color.Transparent);
		Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp,
			DepthStencilState.None, Main.Rasterizer, null, Matrix.CreateScale(0.5f));
		for (int index = 0; index < SoulCount; index++)
		{
			Vector2 center = new(index * CellSize * 2f + CellSize, CellSize);
			// Render large before pixelation so enlarged souls retain the stable two-pixel grid.
			SoulOrbProjectile.DrawSoulVisualAt(center, SoulValues[index % SoulValues.Length], 1f, SoulRenderScale,
				index * 1.73f);
		}
		Main.spriteBatch.End();
		graphicsDevice.SetRenderTargets(previousTargets);
		hasContent = true;
	}

	private static bool IsAnySupportedUIOpen() => GraftingAltarSystem.IsOpen
		|| SoulMenuSystem.IsOpen || SoulApparatusSystem.IsOpen;

	private static uint Hash(int seed, int value) => Hash(unchecked((uint)seed), value);

	private static uint Hash(uint seed, int value)
	{
		uint hash = seed ^ unchecked((uint)value * 0x9E3779B9u);
		hash ^= hash >> 16;
		hash *= 0x7FEB352Du;
		hash ^= hash >> 15;
		hash *= 0x846CA68Bu;
		return hash ^ hash >> 16;
	}

	private static float Unit(uint value) => (value & 0x00FFFFFFu) / 16777215f;

	private static Vector2 SnapEven(Vector2 position) => new(
		MathF.Round(position.X * 0.5f) * 2f,
		MathF.Round(position.Y * 0.5f) * 2f);
}
