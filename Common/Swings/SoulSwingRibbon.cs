using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Systems;
using Terraria;
using Terraria.GameContent;

namespace SoulsOfTerra.Common.Swings;

public static class SoulSwingRibbon
{
	private const int MaximumPoints = 48;
	private static readonly VertexPositionColorTexture[] Vertices = new VertexPositionColorTexture[MaximumPoints * 2];

	public static void Draw(IReadOnlyList<Vector2> worldPoints, Color color, float fade,
		float maxWidth, float time, int seed)
	{
		if (fade <= 0.01f || worldPoints is null || worldPoints.Count < 2 || Main.dedServ)
		{
			return;
		}

		int pointCount = Math.Min(worldPoints.Count, MaximumPoints);
		int startIndex = worldPoints.Count - pointCount;
		int vertexCount = pointCount * 2;
		for (int index = 0; index < pointCount; index++)
		{
			float progress = index / (float)Math.Max(1, pointCount - 1);
			Vector2 tangent = GetTangent(worldPoints, startIndex, index, pointCount);
			Vector2 perpendicular = new(-tangent.Y, tangent.X);
			float width = MathF.Sin(progress * MathHelper.Pi) * maxWidth * fade;
			if (width < 1.2f)
			{
				width = 1.2f * fade;
			}

			Vector2 screen = worldPoints[startIndex + index] - Main.screenPosition;
			Color vertexColor = color * fade;
			Vertices[index * 2] = new VertexPositionColorTexture(new Vector3(screen + perpendicular * width, 0f),
				vertexColor, new Vector2(progress, 0f));
			Vertices[index * 2 + 1] = new VertexPositionColorTexture(new Vector3(screen - perpendicular * width, 0f),
				vertexColor, new Vector2(progress, 1f));
		}

		Main.spriteBatch.End();
		GraphicsDevice device = Main.instance.GraphicsDevice;
		BlendState previousBlend = device.BlendState;
		RasterizerState previousRasterizer = device.RasterizerState;
		device.BlendState = BlendState.Additive;
		device.RasterizerState = RasterizerState.CullNone;
		device.SamplerStates[0] = SamplerState.LinearClamp;
		device.Textures[0] = TextureAssets.MagicPixel.Value;

		if (SoulSwingShaderSystem.ApplyRibbon(color, time + seed * 0.037f, fade))
		{
			device.DrawUserPrimitives(PrimitiveType.TriangleStrip, Vertices, 0, vertexCount - 2);
		}

		device.BlendState = previousBlend;
		device.RasterizerState = previousRasterizer;
		Main.pixelShader.CurrentTechnique.Passes[0].Apply();
		Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
			DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
	}

	private static Vector2 GetTangent(IReadOnlyList<Vector2> points, int startIndex, int index, int count)
	{
		int worldIndex = startIndex + index;
		Vector2 tangent;
		if (index == 0)
		{
			tangent = points[worldIndex + 1] - points[worldIndex];
		}
		else if (index >= count - 1)
		{
			tangent = points[worldIndex] - points[worldIndex - 1];
		}
		else
		{
			tangent = points[worldIndex + 1] - points[worldIndex - 1];
		}

		return tangent.LengthSquared() < 0.001f ? Vector2.UnitX : Vector2.Normalize(tangent);
	}
}
