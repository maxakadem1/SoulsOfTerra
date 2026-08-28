using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Content.Projectiles;
using Terraria;
using Terraria.GameContent;

namespace SoulsOfTerra.Common;

public static class CongregationHymnWave
{
	public const float DefaultHalfThickness = 18f;
	private const int WaveSegments = 112;

	public static bool HitsAnnulus(Vector2 center, float radius, Rectangle targetHitbox,
		float halfThickness = DefaultHalfThickness)
	{
		Vector2 closest = new(
			MathHelper.Clamp(center.X, targetHitbox.Left, targetHitbox.Right),
			MathHelper.Clamp(center.Y, targetHitbox.Top, targetHitbox.Bottom));
		float minimumDistance = Vector2.Distance(center, closest);
		float maximumDistance = 0f;
		Vector2[] corners =
		[
			targetHitbox.TopLeft(),
			targetHitbox.TopRight(),
			targetHitbox.BottomLeft(),
			targetHitbox.BottomRight()
		];
		foreach (Vector2 corner in corners)
		{
			maximumDistance = Math.Max(maximumDistance, Vector2.Distance(center, corner));
		}

		return radius + halfThickness >= minimumDistance && radius - halfThickness <= maximumDistance;
	}

	public static bool AngleFallsInGap(float angle, float gapOrigin, float gapHalfAngle, float padding = 0f)
	{
		if (gapHalfAngle <= 0f)
		{
			return false;
		}

		float relative = MathHelper.WrapAngle(angle - gapOrigin);
		float nearestGap = MathF.Round(relative / MathHelper.PiOver2) * MathHelper.PiOver2;
		return MathF.Abs(MathHelper.WrapAngle(relative - nearestGap)) <= gapHalfAngle + padding;
	}

	public static float EasedRadius(float progress, float startRadius, float maximumRadius)
	{
		float eased = 1f - MathF.Pow(1f - MathHelper.Clamp(progress, 0f, 1f), 3f);
		return MathHelper.Lerp(startRadius, maximumRadius, eased);
	}

	public static void DrawExpandingWave(Vector2 worldCenter, float radius, float progress, float gapOrigin,
		float gapHalfAngle, float darkBodyStrength = 1f)
	{
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Texture2D glow = SoulOrbProjectile.GetGlowTexture();
		Vector2 glowOrigin = glow.Size() * 0.5f;
		float opacity = MathF.Sin(MathHelper.Clamp(progress, 0f, 1f) * MathHelper.Pi);
		float phase = Main.GlobalTimeWrappedHourly * 5.2f;

		DrawWaveRing(pixel, worldCenter, radius - 48f, 8f, new Color(16, 105, 101, 0) * (opacity * 0.22f), 8f,
			phase + 1.6f, gapOrigin, gapHalfAngle, 0.035f);
		DrawWaveRing(pixel, worldCenter, radius - 24f, 12f, new Color(28, 176, 163, 0) * (opacity * 0.3f), 5f,
			phase + 0.8f, gapOrigin, gapHalfAngle, 0.035f);
		DrawWaveRing(pixel, worldCenter, radius + 14f, 2f, new Color(132, 255, 234, 0) * (opacity * 0.4f), 4f,
			phase, gapOrigin, gapHalfAngle, 0.035f);

		for (int segment = 0; segment < WaveSegments; segment++)
		{
			float startAngle = MathHelper.TwoPi * segment / WaveSegments;
			float endAngle = MathHelper.TwoPi * (segment + 1) / WaveSegments;
			float middleAngle = (startAngle + endAngle) * 0.5f;
			if (AngleFallsInGap(middleAngle, gapOrigin, gapHalfAngle))
			{
				continue;
			}

			Vector2 start = worldCenter + startAngle.ToRotationVector2() * radius - Main.screenPosition;
			Vector2 end = worldCenter + endAngle.ToRotationVector2() * radius - Main.screenPosition;
			if (darkBodyStrength > 0f)
			{
				DrawLine(pixel, start, end, new Color(2, 8, 11, 220) * (opacity * darkBodyStrength), 24f);
			}

			DrawLine(pixel, start, end, new Color(35, 213, 193, 0) * (opacity * 0.52f), 15f);
			DrawLine(pixel, start, end, new Color(222, 255, 248, 0) * (opacity * 0.98f), 4.5f);

			if (segment % 7 == 0)
			{
				float flicker = 0.75f + 0.25f * MathF.Sin(segment * 2.7f + phase);
				Main.EntitySpriteDraw(glow, (start + end) * 0.5f, null,
					new Color(79, 239, 215, 0) * (opacity * 0.65f * flicker), middleAngle + MathHelper.PiOver2, glowOrigin,
					new Vector2(0.2f, 0.055f), SpriteEffects.None);
			}
		}

		DrawWaveFragments(glow, glowOrigin, worldCenter, radius, opacity, phase, gapOrigin, gapHalfAngle);
	}

	public static void DrawReleaseFlash(Vector2 worldCenter, float age, float duration, float sizeScale = 1f)
	{
		if (age is < 0f || age > duration)
		{
			return;
		}

		Texture2D glow = SoulOrbProjectile.GetGlowTexture();
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Vector2 center = worldCenter - Main.screenPosition;
		Vector2 origin = glow.Size() * 0.5f;
		float progress = age / duration;
		float opacity = MathF.Pow(1f - progress, 1.35f);
		float bloomScale = MathHelper.Lerp(0.75f, 5.8f, MathF.Sqrt(progress)) * sizeScale;
		Main.EntitySpriteDraw(glow, center, null, new Color(18, 211, 190, 0) * (opacity * 0.58f),
			0f, origin, bloomScale, SpriteEffects.None);
		Main.EntitySpriteDraw(glow, center, null, new Color(235, 255, 250, 0) * opacity,
			0f, origin, MathHelper.Lerp(0.85f, 2f, progress) * sizeScale, SpriteEffects.None);

		for (int ring = 0; ring < 3; ring++)
		{
			float ringProgress = MathHelper.Clamp(progress * 1.45f - ring * 0.14f, 0f, 1f);
			if (ringProgress <= 0f)
			{
				continue;
			}

			float ringOpacity = MathF.Sin(ringProgress * MathHelper.Pi) * (1f - ring * 0.18f);
			float ringRadius = MathHelper.Lerp(18f, 290f, 1f - MathF.Pow(1f - ringProgress, 2f)) * sizeScale;
			DrawClosedRing(pixel, center, ringRadius, MathHelper.Lerp(8f, 1.5f, ringProgress),
				new Color(177, 255, 239, 0) * ringOpacity);
		}

		for (int shard = 0; shard < 24; shard++)
		{
			float variance = MathF.Sin(shard * 12.9898f) * 0.08f;
			float angle = MathHelper.TwoPi * shard / 24f + variance;
			Vector2 direction = angle.ToRotationVector2();
			float startDistance = MathHelper.Lerp(18f, 105f, progress) * sizeScale;
			float length = MathHelper.Lerp(80f + shard % 5 * 8f, 20f, progress) * sizeScale;
			DrawLine(pixel, center + direction * startDistance, center + direction * (startDistance + length),
				new Color(204, 255, 244, 0) * (opacity * 0.72f), MathHelper.Lerp(4.5f, 0.8f, progress));
		}
	}

	public static void DrawLine(Texture2D texture, Vector2 start, Vector2 end, Color color, float width)
	{
		Vector2 difference = end - start;
		Vector2 origin = new(0f, texture.Height * 0.5f);
		Main.EntitySpriteDraw(texture, start, null, color, difference.ToRotation(), origin,
			new Vector2(difference.Length() / texture.Width, width / texture.Height), SpriteEffects.None);
	}

	private static void DrawWaveRing(Texture2D pixel, Vector2 worldCenter, float radius, float width, Color color,
		float wobble, float phase, float gapOrigin, float gapHalfAngle, float gapPadding)
	{
		if (radius <= wobble + 2f)
		{
			return;
		}

		for (int segment = 0; segment < WaveSegments; segment++)
		{
			float startAngle = MathHelper.TwoPi * segment / WaveSegments;
			float endAngle = MathHelper.TwoPi * (segment + 1) / WaveSegments;
			if (AngleFallsInGap((startAngle + endAngle) * 0.5f, gapOrigin, gapHalfAngle, gapPadding))
			{
				continue;
			}

			float startRadius = radius + MathF.Sin(startAngle * 9f + phase) * wobble;
			float endRadius = radius + MathF.Sin(endAngle * 9f + phase) * wobble;
			Vector2 start = worldCenter + startAngle.ToRotationVector2() * startRadius - Main.screenPosition;
			Vector2 end = worldCenter + endAngle.ToRotationVector2() * endRadius - Main.screenPosition;
			DrawLine(pixel, start, end, color, width);
		}
	}

	private static void DrawWaveFragments(Texture2D glow, Vector2 origin, Vector2 worldCenter, float radius,
		float opacity, float phase, float gapOrigin, float gapHalfAngle)
	{
		for (int fragment = 0; fragment < 28; fragment++)
		{
			float angle = MathHelper.TwoPi * fragment / 28f + 0.035f * MathF.Sin(fragment * 4.1f + phase);
			if (AngleFallsInGap(angle, gapOrigin, gapHalfAngle, 0.045f))
			{
				continue;
			}

			float offset = MathF.Sin(fragment * 2.3f + phase * 1.4f) * 20f;
			Vector2 position = worldCenter + angle.ToRotationVector2() * (radius + offset) - Main.screenPosition;
			float scale = 0.045f + 0.025f * (0.5f + 0.5f * MathF.Sin(fragment * 1.7f + phase));
			Main.EntitySpriteDraw(glow, position, null, new Color(157, 255, 237, 0) * (opacity * 0.7f),
				angle + MathHelper.PiOver2, origin, new Vector2(scale * 3.8f, scale), SpriteEffects.None);
		}
	}

	private static void DrawClosedRing(Texture2D pixel, Vector2 screenCenter, float radius, float width, Color color)
	{
		const int segments = 72;
		for (int segment = 0; segment < segments; segment++)
		{
			float startAngle = MathHelper.TwoPi * segment / segments;
			float endAngle = MathHelper.TwoPi * (segment + 1) / segments;
			Vector2 start = screenCenter + startAngle.ToRotationVector2() * radius;
			Vector2 end = screenCenter + endAngle.ToRotationVector2() * radius;
			DrawLine(pixel, start, end, color, width);
		}
	}
}
