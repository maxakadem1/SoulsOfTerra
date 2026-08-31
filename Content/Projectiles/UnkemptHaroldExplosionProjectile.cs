using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Common.Rendering;
using SoulsOfTerra.Systems;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class UnkemptHaroldExplosionProjectile : ModProjectile, IPixelatedDrawable
{
	public const int BlastSize = 64;
	private const int VisualLifetime = 34;
	private const int FlashLifetime = 14;
	private const int ShockwaveLifetime = 12;
	private const int DamageDuration = 3;
	private const int RingSegments = 12;
	private const int SmokePuffCount = 6;
	private readonly SmokePuff[] smokePuffs = new SmokePuff[SmokePuffCount];

	public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.PurificationPowder}";

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.DrawScreenCheckFluff[Type] = 160;
	}

	public override void SetDefaults()
	{
		Projectile.width = BlastSize;
		Projectile.height = BlastSize;
		Projectile.friendly = true;
		Projectile.DamageType = DamageClass.Ranged;
		Projectile.penetrate = -1;
		Projectile.timeLeft = VisualLifetime;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = -1;
	}

	public override bool ShouldUpdatePosition() => false;

	public override bool CanHitPlayer(Player target) => false;

	public override bool? CanHitNPC(NPC target)
	{
		if (Projectile.ai[0] > DamageDuration)
		{
			return false;
		}

		return Collision.CanHitLine(Projectile.Center, 1, 1, target.position, target.width, target.height)
			? null
			: false;
	}

	public override void AI()
	{
		Projectile.ai[0]++;
		Projectile.friendly = Projectile.ai[0] <= DamageDuration;
		float fade = 1f - Projectile.ai[0] / FlashLifetime;
		fade = MathHelper.Clamp(fade, 0f, 1f);
		Lighting.AddLight(Projectile.Center, 1.05f * fade, 0.78f * fade, 0.28f * fade);

		if (Main.netMode == NetmodeID.Server || Projectile.localAI[0] != 0f)
		{
			return;
		}

		Projectile.localAI[0] = 1f;
		InitializeSmoke();
		// Sparks sell the initial pressure without lingering over the procedural smoke.
		for (int index = 0; index < 10; index++)
		{
			Vector2 direction = Main.rand.NextVector2Unit();
			Dust spark = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame,
				direction * Main.rand.NextFloat(2.8f, 7.5f), 40, new Color(255, 230, 140),
				Main.rand.NextFloat(0.85f, 1.35f));
			spark.noGravity = true;
		}
	}

	public override bool PreDraw(ref Color lightColor) => false;

	public void DrawPixelated(SpriteBatch spriteBatch)
	{
		spriteBatch.End();
		PixelatedRenderSystem.BeginPixelBatch();

		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Vector2 center = Snap(Projectile.Center) - Main.screenPosition + PixelatedRenderSystem.CameraRemainder;
		DrawSmoke(spriteBatch, pixel, center);
		DrawFireball(spriteBatch, pixel, center);
		DrawShockwave(spriteBatch, pixel, center);
	}

	private void InitializeSmoke()
	{
		for (int index = 0; index < smokePuffs.Length; index++)
		{
			Vector2 direction = Main.rand.NextVector2Unit();
			smokePuffs[index] = new SmokePuff
			{
				Offset = direction * Main.rand.NextFloat(2f, 8f),
				Velocity = direction * Main.rand.NextFloat(0.45f, 1.15f) + new Vector2(0f, -0.2f),
				Size = Main.rand.NextFloat(9f, 15f),
				Delay = Main.rand.NextFloat(2f, 7f),
				Lifetime = Main.rand.NextFloat(22f, 29f),
				Rotation = Main.rand.NextFloat(MathHelper.TwoPi)
			};
		}
	}

	private void DrawSmoke(SpriteBatch spriteBatch, Texture2D pixel, Vector2 center)
	{
		for (int index = 0; index < smokePuffs.Length; index++)
		{
			SmokePuff puff = smokePuffs[index];
			float age = Projectile.ai[0] - puff.Delay;
			if (age <= 0f || age >= puff.Lifetime)
			{
				continue;
			}

			float progress = age / puff.Lifetime;
			float appear = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(age / 3f, 0f, 1f));
			float fade = 1f - MathHelper.SmoothStep(0f, 1f,
				MathHelper.Clamp((progress - 0.55f) / 0.45f, 0f, 1f));
			float opacity = appear * fade;
			Vector2 position = center + puff.Offset + puff.Velocity * age + new Vector2(0f, -0.025f * age * age);
			float size = puff.Size * MathHelper.Lerp(0.55f, 1.25f, progress);
			Color color = SmokeColor(progress, opacity);
			Vector2 axis = puff.Rotation.ToRotationVector2();

			// Three offset blocks form an irregular puff without requiring a texture asset.
			DrawSquare(spriteBatch, pixel, position, size, color, puff.Rotation * 0.12f);
			DrawSquare(spriteBatch, pixel, position + axis * size * 0.38f, size * 0.62f, color, 0f);
			DrawSquare(spriteBatch, pixel, position - axis.RotatedBy(MathHelper.PiOver2) * size * 0.3f,
				size * 0.48f, color, 0f);
		}
	}

	private void DrawFireball(SpriteBatch spriteBatch, Texture2D pixel, Vector2 center)
	{
		float progress = Projectile.ai[0] / FlashLifetime;
		if (progress >= 1f)
		{
			return;
		}

		float expand = 1f - MathF.Pow(1f - progress, 3f);
		float fade = 1f - progress * progress;
		float radius = MathHelper.Lerp(4f, 23f, expand);
		Color orange = Tint(255, 112, 24, fade);
		Color gold = Tint(255, 205, 72, fade);

		DrawSquare(spriteBatch, pixel, center, MathHelper.Lerp(26f, 8f, progress), orange, progress * 0.35f);
		for (int lobe = 0; lobe < 8; lobe++)
		{
			float angle = MathHelper.TwoPi * lobe / 8f + Projectile.identity * 0.37f;
			Vector2 position = center + angle.ToRotationVector2() * radius;
			float size = MathHelper.Lerp(17f, 5f, progress) * (lobe % 2 == 0 ? 1f : 0.78f);
			DrawSquare(spriteBatch, pixel, position, size, lobe % 2 == 0 ? orange : gold, angle * 0.2f);
		}

		float coreFade = 1f - MathHelper.Clamp(progress / 0.65f, 0f, 1f);
		DrawSquare(spriteBatch, pixel, center, MathHelper.Lerp(15f, 4f, progress),
			Tint(255, 250, 215, coreFade), MathHelper.PiOver4);
	}

	private void DrawShockwave(SpriteBatch spriteBatch, Texture2D pixel, Vector2 center)
	{
		float progress = Projectile.ai[0] / ShockwaveLifetime;
		if (progress >= 1f)
		{
			return;
		}

		float expand = 1f - MathF.Pow(1f - progress, 2f);
		float fade = 1f - progress;
		float radius = MathHelper.Lerp(9f, 38f, expand);
		DrawRing(spriteBatch, pixel, center, radius, MathHelper.Lerp(6f, 1.5f, progress),
			Tint(255, 174, 48, fade));
		DrawRing(spriteBatch, pixel, center, radius + 3f, MathHelper.Lerp(2.5f, 1f, progress),
			Tint(255, 246, 195, fade * 0.8f));
	}

	private static Vector2 Snap(Vector2 world) =>
		new(MathF.Round(world.X * 0.5f) * 2f, MathF.Round(world.Y * 0.5f) * 2f);

	private static Color Tint(int r, int g, int b, float fade) =>
		Color.FromNonPremultiplied(r, g, b, (int)(230 * fade));

	private static Color SmokeColor(float progress, float opacity)
	{
		Color color = Color.Lerp(new Color(205, 92, 28), new Color(54, 45, 40),
			MathHelper.Clamp(progress * 1.35f, 0f, 1f));
		return Color.FromNonPremultiplied(color.R, color.G, color.B, (int)(190f * opacity));
	}

	private static void DrawRing(SpriteBatch spriteBatch, Texture2D pixel, Vector2 center, float radius,
		float width, Color color)
	{
		for (int segment = 0; segment < RingSegments; segment++)
		{
			Vector2 start = center + (MathHelper.TwoPi * segment / RingSegments).ToRotationVector2() * radius;
			Vector2 end = center + (MathHelper.TwoPi * (segment + 1) / RingSegments).ToRotationVector2() * radius;
			DrawLine(spriteBatch, pixel, start, end, color, width);
		}
	}

	private static void DrawSquare(SpriteBatch spriteBatch, Texture2D pixel, Vector2 center, float size, Color color,
		float rotation = 0f)
	{
		spriteBatch.Draw(pixel, center, null, color, rotation,
			new Vector2(pixel.Width * 0.5f, pixel.Height * 0.5f),
			new Vector2(size / pixel.Width, size / pixel.Height), SpriteEffects.None, 0f);
	}

	private static void DrawLine(SpriteBatch spriteBatch, Texture2D pixel, Vector2 start, Vector2 end, Color color,
		float width)
	{
		Vector2 delta = end - start;
		float length = delta.Length();
		if (length < 0.5f)
		{
			return;
		}

		spriteBatch.Draw(pixel, start, null, color, delta.ToRotation(), new Vector2(0f, pixel.Height * 0.5f),
			new Vector2(length / pixel.Width, width / pixel.Height), SpriteEffects.None, 0f);
	}

	private struct SmokePuff
	{
		public Vector2 Offset;
		public Vector2 Velocity;
		public float Size;
		public float Delay;
		public float Lifetime;
		public float Rotation;
	}
}
