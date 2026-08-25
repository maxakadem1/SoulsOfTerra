using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class MoonstoneCollapseProjectile : ModProjectile
{
	private const int CollapseSize = 200;
	private const int VisualLifetime = 18;

	public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.PurificationPowder}";

	public override void SetDefaults()
	{
		Projectile.width = CollapseSize;
		Projectile.height = CollapseSize;
		Projectile.friendly = true;
		Projectile.DamageType = DamageClass.Magic;
		Projectile.penetrate = -1;
		Projectile.timeLeft = VisualLifetime;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = -1;
	}

	public override bool ShouldUpdatePosition() => false;

	public override bool? CanHitNPC(NPC target)
	{
		return Collision.CanHitLine(Projectile.Center, 1, 1, target.position, target.width, target.height) ? null : false;
	}

	public override void AI()
	{
		Projectile.ai[0]++;
		float progress = MathHelper.Clamp(Projectile.ai[0] / VisualLifetime, 0f, 1f);
		Projectile.friendly = Projectile.ai[0] <= 3f;
		float flash = 1f - progress;
		Lighting.AddLight(Projectile.Center, 1.4f * flash, 2.5f * flash, 3f * flash);
		if (Main.netMode == NetmodeID.Server)
		{
			return;
		}

		if (Projectile.localAI[0] == 0f)
		{
			Projectile.localAI[0] = 1f;
			SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.95f, Pitch = -0.25f }, Projectile.Center);
			for (int index = 0; index < 36; index++)
			{
				Vector2 direction = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * index / 36f);
				Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.TintableDustLighted,
					direction * Main.rand.NextFloat(3f, 7f), 15, new Color(245, 255, 255), Main.rand.NextFloat(1.35f, 2f));
				dust.noGravity = true;
			}
		}

		float waveRadius = MathHelper.SmoothStep(6f, 165f, progress);
		int waveParticles = 12;
		for (int index = 0; index < waveParticles; index++)
		{
			float angle = MathHelper.TwoPi * index / waveParticles + Projectile.ai[0] * 0.025f;
			Vector2 direction = angle.ToRotationVector2();
			Dust dust = Dust.NewDustPerfect(Projectile.Center + direction * waveRadius, DustID.BlueCrystalShard,
				direction * MathHelper.Lerp(7f, 1.5f, progress), 20, new Color(220, 250, 255),
				MathHelper.Lerp(1.55f, 0.7f, progress));
			dust.noGravity = true;
		}
	}

	public override bool PreDraw(ref Color lightColor) => false;
}
