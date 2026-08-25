using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class MoonstoneExplosionProjectile : ModProjectile
{
	private const int ExplosionSize = 320;

	public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.PurificationPowder}";

	public override void SetDefaults()
	{
		Projectile.width = ExplosionSize;
		Projectile.height = ExplosionSize;
		Projectile.friendly = true;
		Projectile.DamageType = DamageClass.Magic;
		Projectile.penetrate = -1;
		Projectile.timeLeft = 3;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = -1;
	}

	public override bool ShouldUpdatePosition() => false;

	public override bool? CanHitNPC(NPC target)
	{
		// The blast is large, but solid terrain still shields enemies from damage.
		return Collision.CanHitLine(Projectile.Center, 1, 1, target.position, target.width, target.height) ? null : false;
	}

	public override void AI()
	{
		Lighting.AddLight(Projectile.Center, 1.2f, 2.2f, 2.7f);
		if (Projectile.localAI[0] != 0f || Main.netMode == NetmodeID.Server)
		{
			return;
		}

		Projectile.localAI[0] = 1f;
		SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.9f, Pitch = 0.3f }, Projectile.Center);
		for (int index = 0; index < 72; index++)
		{
			Vector2 direction = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * index / 72f);
			Dust dust = Dust.NewDustPerfect(Projectile.Center + direction * 20f, DustID.BlueCrystalShard,
				direction * Main.rand.NextFloat(8f, 15f), 35, new Color(220, 250, 255), Main.rand.NextFloat(1.2f, 1.9f));
			dust.noGravity = true;
		}

		for (int index = 0; index < 36; index++)
		{
			Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.TintableDustLighted,
				Main.rand.NextVector2Circular(6f, 6f), 20, new Color(235, 255, 255), Main.rand.NextFloat(1.1f, 1.7f));
			dust.noGravity = true;
		}
	}

	public override bool PreDraw(ref Color lightColor) => false;
}
