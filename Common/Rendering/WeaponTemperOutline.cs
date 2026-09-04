using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Systems;
using Terraria;

namespace SoulsOfTerra.Common.Rendering;

/// <summary>
/// Path-colored silhouette rim on tempered weapons, pulsing with a spark that travels the outline.
/// </summary>
internal static class WeaponTemperOutline
{
	private const int RimSamples = 8;

	public static bool TryGetAccent(Item item, out Color accent, out float intensity)
	{
		accent = SoullessUIPalette.Accent;
		intensity = 0f;
		WeaponTemperItem temper = WeaponTemperItem.Get(item);
		if (temper is not { IsTempered: true })
		{
			return false;
		}

		intensity = MathHelper.Lerp(0.75f, 1f,
			(temper.Level - 1) / (float)Math.Max(1, WeaponTemper.MaxLevel - 1));
		if (EssencePathRegistry.TryGet(temper.PathIndex, out EssencePathDefinition path))
		{
			accent = path.GetRimColor(Main.GlobalTimeWrappedHourly);
		}

		return true;
	}

	public static void Draw(SpriteBatch spriteBatch, Item item, Texture2D texture, Vector2 position,
		Rectangle frame, Vector2 origin, float rotation, float scale, Color? multiply = null)
	{
		if (item is null || !TryGetAccent(item, out Color accent, out float intensity))
		{
			return;
		}

		float opacity = (multiply?.A ?? 255) / 255f * intensity;
		float time = Main.GlobalTimeWrappedHourly;
		float pulse = 0.55f + 0.45f * MathF.Sin(time * 3.2f + item.type * 0.13f);
		Color bright = Color.Lerp(accent, Color.White, 0.42f);
		// Offset-only rings keep the glow even around the silhouette; scaling the sprite pools on the hilt.
		DrawRing(spriteBatch, texture, position, frame, origin, rotation, scale,
			2.2f * scale, bright, (0.55f + pulse * 0.1f) * opacity);
		DrawRing(spriteBatch, texture, position, frame, origin, rotation, scale,
			3.6f * scale, bright, (0.38f + pulse * 0.08f) * opacity);

		Vector2 sparkOffset = new Vector2(3.6f * scale, 0f).RotatedBy(time * 2.6f);
		Color spark = new Color(bright.R, bright.G, bright.B, (byte)0) * (0.7f * opacity);
		spriteBatch.Draw(texture, position + sparkOffset, frame, spark, rotation, origin, scale,
			SpriteEffects.None, 0f);
	}

	private static void DrawRing(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, Rectangle frame,
		Vector2 origin, float rotation, float scale, float radius, Color bright, float strength)
	{
		Color rim = new Color(bright.R, bright.G, bright.B, (byte)0) * strength;
		for (int sample = 0; sample < RimSamples; sample++)
		{
			Vector2 offset = new Vector2(radius, 0f).RotatedBy(MathHelper.TwoPi * sample / RimSamples);
			spriteBatch.Draw(texture, position + offset, frame, rim, rotation, origin, scale,
				SpriteEffects.None, 0f);
		}
	}
}
