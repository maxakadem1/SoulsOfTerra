using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Common;
using SoulsOfTerra.Common.Rendering;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class CruxVolleyProjectile : ModProjectile, IPixelatedDrawable
{
	public const int VolleyDuration = 22;
	public const int WriteDuration = 12;
	public const float ArmLength = 240f;
	public const float HitWidth = 18f;

	private int Age => VolleyDuration - Projectile.timeLeft;

	public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.PurificationPowder}";

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.DrawScreenCheckFluff[Type] = 320;
	}

	public override void SetDefaults()
	{
		Projectile.width = 2;
		Projectile.height = 2;
		Projectile.friendly = true;
		Projectile.DamageType = DamageClass.Ranged;
		Projectile.penetrate = -1;
		Projectile.timeLeft = VolleyDuration;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.ownerHitCheck = false;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = VolleyDuration;
		Projectile.netImportant = true;
	}

	public override bool ShouldUpdatePosition() => false;

	public override void DrawBehind(int index, System.Collections.Generic.List<int> behindNPCsAndTiles,
		System.Collections.Generic.List<int> behindNPCs, System.Collections.Generic.List<int> behindProjectiles,
		System.Collections.Generic.List<int> overPlayers, System.Collections.Generic.List<int> overWiresUI)
	{
		overPlayers.Add(index);
	}

	public override void AI()
	{
		// Vanilla item-use projectiles can snap back to the player; the lock lives in ai.
		Projectile.Center = new Vector2(Projectile.ai[0], Projectile.ai[1]);
		Projectile.velocity = Vector2.Zero;

		if (Age == 0 && Main.netMode != NetmodeID.Server)
		{
			SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.72f, Pitch = 0.28f }, Projectile.Center);
		}

		if (Age == WriteDuration && Main.netMode != NetmodeID.Server)
		{
			SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.55f, Pitch = 0.35f }, Projectile.Center);
			SpawnKnotDust();
		}

		if (Age < WriteDuration && Age % 2 == 0 && Main.netMode != NetmodeID.Server)
		{
			SpawnWriteDust();
		}

		float glow = LingerFade() * 0.45f;
		Lighting.AddLight(Projectile.Center, 0.12f * glow, 0.62f * glow, 0.54f * glow);
	}

	public override bool? CanDamage() => true;

	public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
	{
		modifiers.HitDirectionOverride = target.Center.X >= Projectile.Center.X ? 1 : -1;
	}

	public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
	{
		GetArms(out Vector2 first, out Vector2 second);
		float collisionPoint = 0f;
		Vector2 center = Projectile.Center;
		float half = ArmLength * 0.5f;
		return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
				center - first * half, center + first * half, HitWidth, ref collisionPoint)
			|| Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
				center - second * half, center + second * half, HitWidth, ref collisionPoint);
	}

	public override bool PreDraw(ref Color lightColor) => false;

	public void DrawPixelated(SpriteBatch spriteBatch)
	{
		// Shader arms, moving heads, and the knot remain one coherent pixelated glyph.
		CruxSentenceDraw.Draw(Projectile.Center, WriteProgress(), LingerFade(), Age / 60f,
			Projectile.identity * 0.173f, pixelated: true);
	}

	private float WriteProgress() => MathHelper.Clamp(Age / (float)WriteDuration, 0f, 1f);

	private float LingerFade()
	{
		if (Age <= WriteDuration)
		{
			return 1f;
		}

		return MathHelper.Clamp(1f - (Age - WriteDuration) / (float)(VolleyDuration - WriteDuration), 0f, 1f);
	}

	internal static void GetArms(out Vector2 first, out Vector2 second)
	{
		first = MathHelper.PiOver4.ToRotationVector2();
		second = (MathHelper.PiOver4 + MathHelper.PiOver2).ToRotationVector2();
	}

	private void SpawnWriteDust()
	{
		GetArms(out Vector2 first, out Vector2 second);
		float write = WriteProgress();
		SpawnHeadDust(first, write);
		SpawnHeadDust(-first, write);
		SpawnHeadDust(second, write);
		SpawnHeadDust(-second, write);
	}

	private void SpawnHeadDust(Vector2 outward, float write)
	{
		Vector2 tip = Projectile.Center + outward * (ArmLength * 0.5f);
		Vector2 along = (Projectile.Center - tip).SafeNormalize(Vector2.UnitX);
		Vector2 position = tip + along * (ArmLength * 0.5f * write);
		Dust dust = Dust.NewDustPerfect(position, DustID.DungeonSpirit,
			along.RotatedByRandom(0.4f) * Main.rand.NextFloat(0.4f, 1.6f), 90,
			new Color(90, 240, 220), Main.rand.NextFloat(0.55f, 0.9f));
		dust.noGravity = true;
	}

	private void SpawnKnotDust()
	{
		for (int index = 0; index < 14; index++)
		{
			Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.DungeonSpirit,
				Main.rand.NextVector2Circular(3.4f, 3.4f), 80, new Color(170, 255, 240),
				Main.rand.NextFloat(0.7f, 1.15f));
			dust.noGravity = true;
		}
	}
}
