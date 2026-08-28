using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Content.Projectiles;
using SoulsOfTerra.Systems;
using Terraria;
using Terraria.GameContent;

namespace SoulsOfTerra.Common;

public static class SoulBloodstainDraw
{
	private static readonly Color DeepViolet = new(72, 18, 112);
	private static readonly Color PaleViolet = new(205, 118, 245);
	private static readonly Color CyanEdge = new(72, 218, 230);

	public static int GetVisualTier(long souls)
	{
		float valueLog = (float)Math.Log10(Math.Max(1, souls));
		return Math.Clamp((int)Math.Floor((valueLog - 1f) / 1.3f), 0, 3);
	}

	public static void DrawMarker(Projectile projectile, long souls, bool reactive)
	{
		int tier = GetVisualTier(souls);
		Vector2 groundCenter = projectile.Center + Vector2.UnitY * 11f;
		float time = Main.GlobalTimeWrappedHourly;
		float seed = projectile.whoAmI * 0.731f;
		float pulse = 0.94f + MathF.Sin(time * 2.7f + seed) * 0.06f;
		float intensity = (1f + tier * 0.1f + (reactive ? 0.2f : 0f)) * pulse;
		Vector2 poolSize = new(64f + tier * 4f, 23f + tier * 1.5f);

		DrawPool(groundCenter, poolSize, time, intensity, seed, reactive ? 1f : 0f);
		DrawWisps(projectile, groundCenter, tier, time, seed, reactive);
	}

	public static void DrawRecovery(Projectile projectile, Player target, int tier, float progress)
	{
		Texture2D glow = SoulOrbProjectile.GetGlowTexture();
		Vector2 origin = glow.Size() * 0.5f;
		Vector2 start = projectile.Center + Vector2.UnitY * 11f;
		float eased = 1f - MathF.Pow(1f - progress, 3f);
		float fade = 1f - MathHelper.Clamp((progress - 0.76f) / 0.24f, 0f, 1f);
		int wispCount = 3 + tier;

		for (int index = 0; index < wispCount; index++)
		{
			float phase = index / (float)wispCount * MathHelper.TwoPi;
			Vector2 curl = new(MathF.Cos(phase + progress * 8f), MathF.Sin(phase + progress * 8f) * 0.55f);
			curl *= (1f - eased) * (10f + tier * 2f);
			Vector2 destination = target.Center + new Vector2(target.direction * -4f, -5f);
			Vector2 position = Vector2.Lerp(start, destination, eased) + curl - Main.screenPosition;
			Color color = Color.Lerp(PaleViolet, CyanEdge, index / (float)Math.Max(1, wispCount - 1));
			float scale = MathHelper.Lerp(0.23f, 0.08f, eased) * fade;
			Main.EntitySpriteDraw(glow, position, null, WithAlpha(color, 220) * fade, 0f, origin,
				new Vector2(scale * 0.72f, scale), SpriteEffects.None);
		}
	}

	public static void SpawnRecoveryBurst(Vector2 position, int tier)
	{
		if (Main.dedServ)
		{
			return;
		}

		int count = 10 + tier * 3;
		for (int index = 0; index < count; index++)
		{
			float angle = MathHelper.TwoPi * index / count + Main.rand.NextFloat(-0.16f, 0.16f);
			Vector2 velocity = new Vector2(MathF.Cos(angle) * 1.45f, MathF.Sin(angle) * 0.48f - 0.35f);
			Color color = Color.Lerp(DeepViolet, CyanEdge, Main.rand.NextFloat(0.25f, 0.75f));
			Dust dust = Dust.NewDustPerfect(position + Vector2.UnitY * 11f, Terraria.ID.DustID.DungeonSpirit,
				velocity, 110, color, 0.72f + tier * 0.05f);
			dust.noGravity = true;
		}
	}

	private static void DrawPool(Vector2 center, Vector2 size, float time, float intensity, float seed, float reactive)
	{
		Effect effect = SoulShaderSystem.GetBloodstainEffect();
		if (effect is null)
		{
			DrawFallbackPool(center, size, intensity, reactive);
			return;
		}

		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Main.spriteBatch.End();
		Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp,
			DepthStencilState.None, Main.Rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);
		SoulShaderSystem.ApplyBloodstain(time, intensity, seed, reactive);
		Main.spriteBatch.Draw(pixel, center - Main.screenPosition, null, Color.White, 0f, pixel.Size() * 0.5f,
			size / pixel.Size(), SpriteEffects.None, 0f);
		Main.spriteBatch.End();
		Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
			DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
	}

	private static void DrawFallbackPool(Vector2 center, Vector2 size, float intensity, float reactive)
	{
		Texture2D glow = SoulOrbProjectile.GetGlowTexture();
		Texture2D ring = SoulOrbProjectile.GetRingTexture();
		Vector2 origin = glow.Size() * 0.5f;
		Vector2 screen = center - Main.screenPosition;
		Vector2 scale = size / glow.Size();

		Main.EntitySpriteDraw(glow, screen, null, WithAlpha(DeepViolet, 145) * intensity, 0f, origin, scale,
			SpriteEffects.None);
		Main.EntitySpriteDraw(ring, screen, null, WithAlpha(CyanEdge, (byte)(70 + reactive * 65f)) * intensity,
			0f, origin, scale * 0.92f, SpriteEffects.None);
	}

	private static void DrawWisps(Projectile projectile, Vector2 center, int tier, float time, float seed, bool reactive)
	{
		Texture2D glow = SoulOrbProjectile.GetGlowTexture();
		Vector2 origin = glow.Size() * 0.5f;
		int wispCount = 3 + tier;
		Vector2 attraction = Vector2.Zero;
		if (reactive && Main.LocalPlayer.active && !Main.LocalPlayer.dead)
		{
			attraction = (Main.LocalPlayer.Center - center).SafeNormalize(Vector2.Zero) * 7f;
		}

		float fanCenter = (wispCount - 1) * 0.5f;
		for (int index = 0; index < wispCount; index++)
		{
			float cycle = (time * 0.16f + index * 0.09f + seed) % 1f;
			float lift = MathF.Sin(cycle * MathHelper.Pi);
			float fanOffset = (index - fanCenter) * (tier > 1 ? 7f : 11f);
			float curl = MathF.Sin(cycle * MathHelper.TwoPi + index * 0.82f) * 3.5f;
			float outwardArc = fanCenter > 0f ? (index - fanCenter) / fanCenter * lift * 3f : 0f;
			float horizontal = fanOffset + curl + outwardArc;
			float rise = lift * (27f + tier * 5f + index % 2 * 4f);
			Vector2 wispPosition = center + new Vector2(horizontal, -5f - rise) + attraction * lift;
			float opacity = (0.34f + MathF.Pow(lift, 0.62f) * 0.66f) * (reactive ? 1f : 0.92f);
			Color color = Color.Lerp(PaleViolet, CyanEdge, 0.18f + index / (float)Math.Max(1, wispCount - 1) * 0.38f);
			float scale = (0.12f + tier * 0.012f) * (0.78f + lift * 0.38f);

			// Three fading beads form a curved trail without merging into one vertical pillar.
			for (int segment = 2; segment >= 0; segment--)
			{
				float segmentFade = 1f - segment * 0.3f;
				Vector2 trailOffset = new(-curl * segment * 0.16f, segment * (2.8f + lift));
				Vector2 position = wispPosition + trailOffset - Main.screenPosition;
				float segmentScale = scale * (1f - segment * 0.16f);
				Main.EntitySpriteDraw(glow, position, null, WithAlpha(color, 245) * (opacity * segmentFade),
					0f, origin, new Vector2(segmentScale * 0.72f, segmentScale * 1.18f), SpriteEffects.None);
			}
		}
	}

	private static Color WithAlpha(Color color, byte alpha)
	{
		return Color.FromNonPremultiplied(color.R, color.G, color.B, alpha);
	}
}
