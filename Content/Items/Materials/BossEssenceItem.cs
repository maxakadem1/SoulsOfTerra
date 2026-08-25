using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Items.Materials;

public abstract class BossEssenceItem : ModItem
{
	private static readonly Dictionary<int, Color> CachedGlowColors = new();
	protected virtual Color? InventoryGlowColor => null;

	public override void Unload()
	{
		CachedGlowColors.Clear();
	}

	public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor,
		Color itemColor, Vector2 origin, float scale)
	{
		Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
		float time = Main.GlobalTimeWrappedHourly;
		float breath = 0.5f + 0.5f * MathF.Sin(time * 2.15f + Type * 0.37f);
		float radius = MathHelper.Lerp(2.25f, 4f, breath);
		Color glowColor = InventoryGlowColor ?? GetSpriteGlowColor(texture, frame);
		Color aura = glowColor * MathHelper.Lerp(0.2f, 0.38f, breath);

		// Offset silhouettes preserve the crisp sprite while creating the shrine's broad teal pulse.
		for (int direction = 0; direction < 8; direction++)
		{
			float angle = MathHelper.TwoPi * direction / 8f;
			Vector2 offset = angle.ToRotationVector2() * radius;
			spriteBatch.Draw(texture, position + offset, frame, aura, 0f, origin, scale * 1.04f, SpriteEffects.None, 0f);
		}

		return true;
	}

	private Color GetSpriteGlowColor(Texture2D texture, Rectangle frame)
	{
		if (CachedGlowColors.TryGetValue(Type, out Color cachedColor))
		{
			return cachedColor;
		}

		Color[] pixels = new Color[frame.Width * frame.Height];
		texture.GetData(0, frame, pixels, 0, pixels.Length);
		double red = 0d;
		double green = 0d;
		double blue = 0d;
		double totalWeight = 0d;
		foreach (Color pixel in pixels)
		{
			float alpha = pixel.A / 255f;
			float maximum = Math.Max(pixel.R, Math.Max(pixel.G, pixel.B)) / 255f;
			float minimum = Math.Min(pixel.R, Math.Min(pixel.G, pixel.B)) / 255f;
			if (alpha < 0.2f || maximum < 0.12f)
			{
				continue;
			}

			float saturation = maximum <= 0f ? 0f : (maximum - minimum) / maximum;
			// Saturated, visible pixels outweigh pale highlights and dark outlines.
			double weight = alpha * (0.25f + saturation * 1.75f) * (0.35f + maximum * 0.65f);
			red += pixel.R * weight;
			green += pixel.G * weight;
			blue += pixel.B * weight;
			totalWeight += weight;
		}

		Color sampled = totalWeight > 0d
			? BrightenAndSaturate((float)(red / totalWeight), (float)(green / totalWeight), (float)(blue / totalWeight))
			: new Color(48, 232, 205);
		CachedGlowColors[Type] = sampled;
		return sampled;
	}

	private static Color BrightenAndSaturate(float red, float green, float blue)
	{
		float maximum = Math.Max(red, Math.Max(green, blue));
		float minimum = Math.Min(red, Math.Min(green, blue));
		float midpoint = (maximum + minimum) * 0.5f;
		red = midpoint + (red - midpoint) * 1.3f;
		green = midpoint + (green - midpoint) * 1.3f;
		blue = midpoint + (blue - midpoint) * 1.3f;
		maximum = Math.Max(red, Math.Max(green, blue));
		float brightnessScale = maximum > 0f && maximum < 225f ? 225f / maximum : 1f;
		return new Color(
			(int)MathHelper.Clamp(red * brightnessScale, 0f, 255f),
			(int)MathHelper.Clamp(green * brightnessScale, 0f, 255f),
			(int)MathHelper.Clamp(blue * brightnessScale, 0f, 255f));
	}
}
