using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Systems;
using Terraria;
using Terraria.GameContent;

namespace SoulsOfTerra.Common.Rendering;

internal static class SoulDashTrailDraw
{
	private const int StripPoints = 6;
	private const float BloomWidth = 52f;
	private const float FireWidth = 44f;
	private const float CoreWidth = 6f;
	private static readonly Vector2[] StripPositions = new Vector2[StripPoints];
	private static readonly float[] Cumulative = new float[32];

	public static void Draw(Vector2[] path, int pointCount, float retract, float snapFlash, float intensity,
		float time, float seed, float echoProgress, float echoOpacity, int direction)
	{
		if (pointCount < 1 || intensity <= 0.02f)
		{
			return;
		}

		Vector2 origin = path[0];
		Vector2 landing = path[pointCount - 1];
		DrawConvergingFragments(origin, landing, echoProgress, echoOpacity, direction, seed);
		DrawSnapRing(landing, snapFlash);
		if (pointCount < 2 || !BuildStrip(path, pointCount, retract))
		{
			return;
		}

		Effect effect = SoulShaderSystem.GetDashWakeEffect();
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		if (effect is null)
		{
			DrawFallback(pixel, FireWidth, intensity);
			return;
		}

		Main.spriteBatch.End();
		Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointClamp,
			DepthStencilState.None, Main.Rasterizer, effect, PixelatedRenderSystem.PixelTransform);

		DrawLayer(pixel, BloomWidth, intensity * 0.32f, snapFlash, time, seed, 1f, 3f, 0.2f);
		DrawLayer(pixel, FireWidth, intensity, snapFlash, time, seed, 0f, 4f, 0.7f);
		DrawLayer(pixel, CoreWidth, intensity, snapFlash, time, seed, 2f, 0.5f, 1.3f);
		// Two escaping wisps break the full-height flame away from its central seam.
		DrawLayer(pixel, 3f, intensity * 0.72f, snapFlash, time, seed + 3f, 2f, 15f, 0.1f);
		DrawLayer(pixel, 2f, intensity * 0.55f, snapFlash, time, seed + 7f, 2f, -17f, 2.2f);

		Main.spriteBatch.End();
		PixelatedRenderSystem.BeginPixelBatch();
	}

	private static void DrawLayer(Texture2D pixel, float width, float intensity, float snapFlash, float time,
		float seed, float mode, float turbulence, float phase)
	{
		for (int index = 0; index < StripPoints - 1; index++)
		{
			Vector2 from = GetTurbulentPoint(index, turbulence, phase, seed);
			Vector2 to = GetTurbulentPoint(index + 1, turbulence, phase, seed);
			Vector2 delta = to - from;
			float length = delta.Length();
			if (length < 0.5f)
			{
				continue;
			}

			float alongStart = index / (float)(StripPoints - 1);
			float alongEnd = (index + 1) / (float)(StripPoints - 1);
			SoulShaderSystem.ApplyDashWake(intensity, snapFlash, alongStart, alongEnd, time, seed, mode);
			Main.spriteBatch.Draw(pixel, from - Main.screenPosition, null, Color.White, delta.ToRotation(),
				new Vector2(0f, pixel.Height * 0.5f),
				new Vector2(length / pixel.Width, width / pixel.Height), SpriteEffects.None, 0f);
		}
	}

	private static Vector2 GetTurbulentPoint(int index, float amplitude, float phase, float seed)
	{
		int previous = System.Math.Max(0, index - 1);
		int next = System.Math.Min(StripPoints - 1, index + 1);
		Vector2 tangent = StripPositions[next] - StripPositions[previous];
		if (tangent.LengthSquared() < 0.01f)
		{
			return StripPositions[index];
		}

		tangent.Normalize();
		Vector2 normal = new(-tangent.Y, tangent.X);
		float along = index / (float)(StripPoints - 1);
		float envelope = (float)System.Math.Sin(along * MathHelper.Pi);
		float wave = (float)System.Math.Sin(along * 15.5f + phase + seed * 0.73f);
		return StripPositions[index] + normal * (wave * amplitude * envelope);
	}

	private static void DrawFallback(Texture2D pixel, float width, float intensity)
	{
		Color color = new Color(70, 230, 210) * intensity;
		for (int index = 0; index < StripPoints - 1; index++)
		{
			DrawLine(pixel, StripPositions[index], StripPositions[index + 1], color, width);
		}
	}

	private static bool BuildStrip(Vector2[] path, int pointCount, float retract)
	{
		Cumulative[0] = 0f;
		for (int index = 1; index < pointCount; index++)
		{
			Cumulative[index] = Cumulative[index - 1] + Vector2.Distance(path[index - 1], path[index]);
		}

		float total = Cumulative[pointCount - 1];
		if (total < 2f)
		{
			return false;
		}

		float trim = total * MathHelper.Clamp(retract, 0f, 0.995f);
		if (total - trim < 2f)
		{
			return false;
		}

		for (int index = 0; index < StripPoints; index++)
		{
			float progress = index / (float)(StripPoints - 1);
			// Trim from the cast origin while retaining forward shader coordinates.
			StripPositions[index] = SamplePath(path, pointCount, MathHelper.Lerp(trim, total, progress));
		}

		return true;
	}

	private static Vector2 SamplePath(Vector2[] path, int pointCount, float distance)
	{
		if (distance <= 0f)
		{
			return path[0];
		}

		for (int index = 1; index < pointCount; index++)
		{
			if (Cumulative[index] < distance)
			{
				continue;
			}

			float span = Cumulative[index] - Cumulative[index - 1];
			float lerp = span <= 0.001f ? 1f : (distance - Cumulative[index - 1]) / span;
			return Vector2.Lerp(path[index - 1], path[index], lerp);
		}

		return path[pointCount - 1];
	}

	private static void DrawConvergingFragments(Vector2 origin, Vector2 landing, float progress, float opacity,
		int direction, float seed)
	{
		if (opacity <= 0.02f)
		{
			return;
		}

		Texture2D pixel = TextureAssets.MagicPixel.Value;
		for (int index = 0; index < 7; index++)
		{
			float hash = (float)System.Math.Sin(index * 19.17f + seed * 2.31f);
			float vertical = hash * 25f + (index % 3 - 1) * 7f;
			Vector2 start = origin + new Vector2(-direction * (4f + index * 2.5f), vertical);
			float chase = MathHelper.Clamp(progress * 1.3f - index * 0.025f, 0f, 1f);
			chase *= chase;
			Vector2 target = landing + new Vector2(-direction * (index % 4) * 2f, vertical * (1f - chase) * 0.12f);
			Vector2 position = Vector2.Lerp(start, target, chase);
			Vector2 pull = target - position;
			float length = MathHelper.Lerp(8f, 2f, chase);
			float thickness = index % 3 == 0 ? 3f : 2f;
			Color color = new Color(90, 255, 220) * (opacity * (0.55f + index % 2 * 0.35f));
			Main.spriteBatch.Draw(pixel, position - Main.screenPosition, null, color, pull.ToRotation(),
				pixel.Size() * 0.5f, new Vector2(length / pixel.Width, thickness / pixel.Height),
				SpriteEffects.None, 0f);
		}
	}

	private static void DrawSnapRing(Vector2 center, float flash)
	{
		if (flash <= 0.02f)
		{
			return;
		}

		Texture2D pixel = TextureAssets.MagicPixel.Value;
		float expansion = 1f - flash;
		DrawJaggedRing(pixel, center, MathHelper.Lerp(8f, 32f, expansion), flash, 0f);
		DrawJaggedRing(pixel, center, MathHelper.Lerp(5f, 22f, expansion), flash * 0.65f, 0.37f);
		for (int index = 0; index < 8; index++)
		{
			float angle = MathHelper.TwoPi * index / 8f + 0.2f;
			Vector2 radial = angle.ToRotationVector2();
			DrawLine(pixel, center + radial * 7f, center + radial * MathHelper.Lerp(24f, 42f, expansion),
				new Color(215, 255, 248) * (flash * 0.8f), 2f);
		}
	}

	private static void DrawJaggedRing(Texture2D pixel, Vector2 center, float radius, float opacity, float phase)
	{
		const int segments = 16;
		Color color = new Color(170, 255, 235) * opacity;
		for (int index = 0; index < segments; index++)
		{
			float angle = MathHelper.TwoPi * index / segments;
			float nextAngle = MathHelper.TwoPi * (index + 1) / segments;
			float fromRadius = radius + (float)System.Math.Sin(index * 4.7f + phase) * 3f;
			float toRadius = radius + (float)System.Math.Sin((index + 1) * 4.7f + phase) * 3f;
			DrawLine(pixel, center + angle.ToRotationVector2() * fromRadius,
				center + nextAngle.ToRotationVector2() * toRadius, color, 2f);
		}
	}

	private static void DrawLine(Texture2D pixel, Vector2 from, Vector2 to, Color color, float width)
	{
		Vector2 delta = to - from;
		Main.spriteBatch.Draw(pixel, from - Main.screenPosition, null, color, delta.ToRotation(),
			new Vector2(0f, pixel.Height * 0.5f),
			new Vector2(delta.Length() / pixel.Width, width / pixel.Height), SpriteEffects.None, 0f);
	}
}
