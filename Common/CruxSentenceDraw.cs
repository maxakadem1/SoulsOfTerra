using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Content.Projectiles;
using SoulsOfTerra.Systems;
using Terraria;
using Terraria.GameContent;

namespace SoulsOfTerra.Common;

public static class CruxSentenceDraw
{
	private const float HalfLength = CruxVolleyProjectile.ArmLength * 0.5f;
	private const float CoreWidth = 14f;
	private const float BloomWidth = 34f;

	public static void Draw(Vector2 worldCenter, float writeProgress, float lingerFade, float time, float seed,
		bool pixelated = false)
	{
		Effect effect = CongregationShaderSystem.GetCruxEffect();
		if (Main.dedServ || lingerFade <= 0.01f || effect is null)
		{
			return;
		}

		CruxVolleyProjectile.GetArms(out Vector2 first, out Vector2 second);
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Texture2D glow = SoulOrbProjectile.GetGlowTexture();
		float shaderTime = time + seed;
		float fade = lingerFade * lingerFade * (3f - 2f * lingerFade);
		Matrix transform = pixelated ? PixelatedRenderSystem.PixelTransform : Main.GameViewMatrix.TransformationMatrix;

		Main.spriteBatch.End();
		Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp,
			DepthStencilState.None, Main.Rasterizer, effect, transform);

		CongregationShaderSystem.ApplyCruxSentence(writeProgress, shaderTime, fade, 2f);
		DrawHalfArm(pixel, worldCenter, first, BloomWidth);
		DrawHalfArm(pixel, worldCenter, -first, BloomWidth);
		DrawHalfArm(pixel, worldCenter, second, BloomWidth);
		DrawHalfArm(pixel, worldCenter, -second, BloomWidth);

		CongregationShaderSystem.ApplyCruxSentence(writeProgress, shaderTime, fade, 0f);
		DrawHalfArm(pixel, worldCenter, first, CoreWidth);
		DrawHalfArm(pixel, worldCenter, -first, CoreWidth);
		DrawHalfArm(pixel, worldCenter, second, CoreWidth);
		DrawHalfArm(pixel, worldCenter, -second, CoreWidth);

		Main.spriteBatch.End();
		Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp,
			DepthStencilState.None, Main.Rasterizer, null, transform);

		DrawScribeHeads(glow, worldCenter, first, second, writeProgress, fade);
		DrawKnot(glow, worldCenter, writeProgress, fade, shaderTime);

		Main.spriteBatch.End();
		if (pixelated)
		{
			// Continue the shared pass without leaking Crux's additive shader state.
			PixelatedRenderSystem.BeginPixelBatch();
		}
		else
		{
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
				DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
		}
	}

	private static void DrawHalfArm(Texture2D pixel, Vector2 worldCenter, Vector2 outward, float width)
	{
		Vector2 tip = worldCenter + outward * HalfLength;
		Vector2 direction = (worldCenter - tip).SafeNormalize(Vector2.UnitX);
		Main.spriteBatch.Draw(pixel, tip - Main.screenPosition, null, Color.White, direction.ToRotation(),
			new Vector2(0f, pixel.Height * 0.5f),
			new Vector2(HalfLength / pixel.Width, width / pixel.Height), SpriteEffects.None, 0f);
	}

	private static void DrawScribeHeads(Texture2D glow, Vector2 worldCenter, Vector2 first, Vector2 second,
		float writeProgress, float fade)
	{
		if (writeProgress >= 0.98f || fade < 0.2f)
		{
			return;
		}

		DrawHead(glow, worldCenter, first, writeProgress, fade);
		DrawHead(glow, worldCenter, -first, writeProgress, fade);
		DrawHead(glow, worldCenter, second, writeProgress, fade);
		DrawHead(glow, worldCenter, -second, writeProgress, fade);
	}

	private static void DrawHead(Texture2D glow, Vector2 worldCenter, Vector2 outward, float writeProgress, float fade)
	{
		Vector2 tip = worldCenter + outward * HalfLength;
		Vector2 along = (worldCenter - tip).SafeNormalize(Vector2.UnitX);
		Vector2 position = tip + along * (HalfLength * writeProgress) - Main.screenPosition;
		Vector2 origin = glow.Size() * 0.5f;
		Color color = new Color(210, 255, 245, 0) * (0.55f * fade);
		Main.spriteBatch.Draw(glow, position, null, color, along.ToRotation(), origin,
			new Vector2(0.22f, 0.1f), SpriteEffects.None, 0f);
	}

	private static void DrawKnot(Texture2D glow, Vector2 worldCenter, float writeProgress, float fade, float time)
	{
		if (writeProgress < 0.86f)
		{
			return;
		}

		float arrive = MathHelper.Clamp((writeProgress - 0.86f) / 0.14f, 0f, 1f);
		float pulse = 0.82f + 0.18f * MathF.Sin(time * 16f);
		float strength = arrive * fade * pulse;
		Vector2 origin = glow.Size() * 0.5f;
		Vector2 screen = worldCenter - Main.screenPosition;
		Main.spriteBatch.Draw(glow, screen, null, new Color(40, 210, 190, 0) * (0.45f * strength), 0f, origin,
			0.95f, SpriteEffects.None, 0f);
		Main.spriteBatch.Draw(glow, screen, null, new Color(120, 245, 225, 0) * (0.55f * strength), 0f, origin,
			0.48f, SpriteEffects.None, 0f);
		Main.spriteBatch.Draw(glow, screen, null, new Color(235, 255, 250, 0) * (0.7f * strength), 0f, origin,
			0.2f, SpriteEffects.None, 0f);
	}
}
