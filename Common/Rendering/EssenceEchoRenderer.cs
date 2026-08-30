using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SoulsOfTerra.Common;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Common.Rendering;

internal static class EssenceEchoRenderer
{
	private static readonly Dictionary<int, Color> AccentColors = new();
	private static readonly Dictionary<EssenceVisualSource, Rectangle> SourceFrames = new();
	private static readonly Color SoulTint = new(144, 255, 229);

	public static bool TryDraw(SpriteBatch spriteBatch, int itemType, Vector2 center, float maximumSize,
		Color drawColor, float rotation = 0f)
	{
		if (!TryGetDefinition(itemType, out EssenceVisualDefinition definition))
		{
			return false;
		}

		float opacity = drawColor.A / 255f;
		Color accent = GetAccentColor(itemType, definition);
		float time = Main.GlobalTimeWrappedHourly;
		float pulse = 0.94f + MathF.Sin(time * 2.4f + definition.Seed * 0.01f) * 0.04f;
		DrawBackGlow(spriteBatch, center, maximumSize, accent, opacity, definition.Seed, time);

		if (definition.Composition == EssenceComposition.Split
			&& definition.SecondarySources is { Count: > 0 })
		{
			float separation = maximumSize * 0.15f;
			DrawSource(spriteBatch, definition.PrimarySource, center - Vector2.UnitX * separation,
				maximumSize * 0.68f * pulse, drawColor, accent, opacity);
			DrawSource(spriteBatch, definition.SecondarySources[0], center + Vector2.UnitX * separation,
				maximumSize * 0.68f * pulse, drawColor, accent, opacity);
		}
		else
		{
			DrawSource(spriteBatch, definition.PrimarySource, center, maximumSize * 0.88f * pulse,
				drawColor, accent, opacity);
		}

		return true;
	}

	public static void Unload()
	{
		AccentColors.Clear();
		SourceFrames.Clear();
	}

	private static bool TryGetDefinition(int itemType, out EssenceVisualDefinition definition)
	{
		ModItem modItem = ModContent.GetModItem(itemType);
		if (modItem is not null)
		{
			foreach (EssenceVisualDefinition candidate in EssenceVisualRegistry.Definitions)
			{
				if (candidate.OutputName == modItem.Name)
				{
					definition = candidate;
					return true;
				}
			}
		}

		definition = null;
		return false;
	}

	private static void DrawBackGlow(SpriteBatch spriteBatch, Vector2 center, float size, Color accent,
		float opacity, int seed, float time)
	{
		Texture2D glow = TextureAssets.Extra[ExtrasID.SharpTears].Value;
		float pulse = 0.96f + MathF.Sin(time * 2f + seed * 0.013f) * 0.08f;
		float scale = size * 1.12f / Math.Max(glow.Width, glow.Height) * pulse;
		Color outer = new Color(accent.R, accent.G, accent.B, 0) * (opacity * 0.34f);
		Color inner = new Color(SoulTint.R, SoulTint.G, SoulTint.B, 0) * (opacity * 0.12f);
		spriteBatch.Draw(glow, center, null, outer, 0f, glow.Size() * 0.5f,
			scale, SpriteEffects.None, 0f);
		spriteBatch.Draw(glow, center, null, inner, 0f, glow.Size() * 0.5f,
			scale * 0.62f, SpriteEffects.None, 0f);
	}

	private static void DrawSource(SpriteBatch spriteBatch, EssenceVisualSource source, Vector2 center,
		float maximumSize, Color drawColor, Color accent, float opacity)
	{
		GetSourceTexture(source, out Texture2D texture, out Rectangle frame);
		float scale = Math.Min(maximumSize / frame.Width, maximumSize / frame.Height);
		Vector2 origin = frame.Size() * 0.5f;
		float time = Main.GlobalTimeWrappedHourly;
		float phase = accent.R * 0.013f + accent.G * 0.007f + accent.B * 0.003f;
		float breath = 0.5f + 0.5f * MathF.Sin(time * 2.15f + phase);
		float auraScale = scale * MathHelper.Lerp(1.07f, 1.15f, breath);
		Color auraColor = Color.Lerp(accent, SoulTint, 0.2f)
			* (opacity * MathHelper.Lerp(0.3f, 0.52f, breath));

		// One enlarged silhouette stays continuous on compact boss heads instead of splitting into dots.
		spriteBatch.Draw(texture, center, frame, auraColor, 0f, origin,
			auraScale, SpriteEffects.None, 0f);

		// Most authored color survives; only a restrained spectral cast unifies the Essences.
		Color sourceColor = Color.Lerp(drawColor, SoulTint * (drawColor.A / 255f), 0.2f);
		sourceColor.A = drawColor.A;
		spriteBatch.Draw(texture, center, frame, sourceColor, 0f, origin, scale,
			SpriteEffects.None, 0f);
	}

	private static Color GetAccentColor(int itemType, EssenceVisualDefinition definition)
	{
		if (definition.AccentOverride is Color explicitAccent)
		{
			return explicitAccent;
		}
		if (AccentColors.TryGetValue(itemType, out Color cached))
		{
			return cached;
		}

		GetSourceTexture(definition.PrimarySource, out Texture2D texture, out Rectangle frame);
		Color[] pixels = new Color[frame.Width * frame.Height];
		texture.GetData(0, frame, pixels, 0, pixels.Length);
		Vector3 total = Vector3.Zero;
		float weightTotal = 0f;
		foreach (Color pixel in pixels)
		{
			float alpha = pixel.A / 255f;
			float saturation = GetSaturation(pixel);
			float brightness = Math.Max(pixel.R, Math.Max(pixel.G, pixel.B)) / 255f;
			if (alpha < 0.2f || brightness < 0.12f)
			{
				continue;
			}
			float weight = alpha * (0.3f + saturation * 1.7f) * (0.35f + brightness * 0.65f);
			total += pixel.ToVector3() * weight;
			weightTotal += weight;
		}

		Color sampled = weightTotal > 0f ? new Color(total / weightTotal) : SoulTint;
		Vector3 value = sampled.ToVector3();
		float maximum = Math.Max(value.X, Math.Max(value.Y, value.Z));
		if (maximum < 0.78f)
		{
			value *= 0.78f / Math.Max(0.01f, maximum);
		}
		sampled = new Color(Vector3.Clamp(value, Vector3.Zero, Vector3.One));
		AccentColors[itemType] = sampled;
		return sampled;
	}

	private static void GetSourceTexture(EssenceVisualSource source, out Texture2D texture,
		out Rectangle frame)
	{
		if (!string.IsNullOrWhiteSpace(source.TexturePath))
		{
			texture = ModContent.Request<Texture2D>(source.TexturePath, AssetRequestMode.ImmediateLoad).Value;
			frame = texture.Bounds;
		}
		else
		{
			int headIndex = source.NpcType >= 0 && source.NpcType < NPCID.Sets.BossHeadTextures.Length
				? NPCID.Sets.BossHeadTextures[source.NpcType]
				: -1;
			if (headIndex >= 0 && headIndex < TextureAssets.NpcHeadBoss.Length)
			{
				texture = TextureAssets.NpcHeadBoss[headIndex].Value;
				frame = texture.Bounds;
			}
			else
			{
				Main.instance.LoadNPC(source.NpcType);
				texture = TextureAssets.Npc[source.NpcType].Value;
				frame = new Rectangle(0, 0, texture.Width,
					texture.Height / Math.Max(1, Main.npcFrameCount[source.NpcType]));
			}
		}

		frame = ApplyCrop(frame, source.NormalizedCrop);
		if (SourceFrames.TryGetValue(source, out Rectangle cachedFrame))
		{
			frame = cachedFrame;
			return;
		}

		// Transparent map-icon padding should not determine the visible inventory scale.
		frame = FindOpaqueBounds(texture, frame);
		SourceFrames[source] = frame;
	}

	private static Rectangle FindOpaqueBounds(Texture2D texture, Rectangle searchFrame)
	{
		Color[] pixels = new Color[searchFrame.Width * searchFrame.Height];
		texture.GetData(0, searchFrame, pixels, 0, pixels.Length);
		int minX = searchFrame.Width;
		int minY = searchFrame.Height;
		int maxX = -1;
		int maxY = -1;
		for (int y = 0; y < searchFrame.Height; y++)
		{
			for (int x = 0; x < searchFrame.Width; x++)
			{
				if (pixels[y * searchFrame.Width + x].A < 24)
				{
					continue;
				}
				minX = Math.Min(minX, x);
				minY = Math.Min(minY, y);
				maxX = Math.Max(maxX, x);
				maxY = Math.Max(maxY, y);
			}
		}

		return maxX >= minX && maxY >= minY
			? new Rectangle(searchFrame.X + minX, searchFrame.Y + minY,
				maxX - minX + 1, maxY - minY + 1)
			: searchFrame;
	}

	private static Rectangle ApplyCrop(Rectangle frame, Vector4? crop)
	{
		if (crop is not Vector4 normalized)
		{
			return frame;
		}
		float left = MathHelper.Clamp(normalized.X, 0f, 1f);
		float top = MathHelper.Clamp(normalized.Y, 0f, 1f);
		float right = MathHelper.Clamp(normalized.Z, left, 1f);
		float bottom = MathHelper.Clamp(normalized.W, top, 1f);
		return new Rectangle(frame.X + (int)(frame.Width * left), frame.Y + (int)(frame.Height * top),
			Math.Max(1, (int)(frame.Width * (right - left))),
			Math.Max(1, (int)(frame.Height * (bottom - top))));
	}

	private static float GetSaturation(Color color)
	{
		float maximum = Math.Max(color.R, Math.Max(color.G, color.B)) / 255f;
		float minimum = Math.Min(color.R, Math.Min(color.G, color.B)) / 255f;
		return maximum <= 0f ? 0f : (maximum - minimum) / maximum;
	}

}
