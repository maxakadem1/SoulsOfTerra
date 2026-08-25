using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class MoonstoneChildExplosionProjectile : ModProjectile
{
	private const int VisualLifetime = 8;

	public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.PurificationPowder}";

	public override void SetDefaults()
	{
		Projectile.width = 60;
		Projectile.height = 60;
		Projectile.friendly = true;
		Projectile.DamageType = DamageClass.Magic;
		Projectile.penetrate = -1;
		Projectile.timeLeft = VisualLifetime;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = -1;
	}

	public override void OnSpawn(IEntitySource source)
	{
		// ai[0] lets both rocket tiers share the same explosion implementation.
		int size = System.Math.Max(24, (int)Projectile.ai[0]);
		Projectile.Resize(size, size);
	}

	public override bool ShouldUpdatePosition() => false;

	public override bool? CanHitNPC(NPC target)
	{
		return Collision.CanHitLine(Projectile.Center, 1, 1, target.position, target.width, target.height) ? null : false;
	}

	public override void AI()
	{
		Projectile.ai[1]++;
		Projectile.friendly = Projectile.ai[1] <= 3f;
		float scale = Projectile.width / 104f;
		Lighting.AddLight(Projectile.Center, 0.45f * scale, 0.95f * scale, 1.3f * scale);
		if (Main.netMode == NetmodeID.Server || Projectile.localAI[0] != 0f)
		{
			return;
		}

		Projectile.localAI[0] = 1f;
		SoundEngine.PlaySound(SoundID.Item14 with
		{
			Volume = Projectile.width >= 100 ? 0.45f : 0.25f,
			Pitch = Projectile.width >= 100 ? 0.45f : 0.7f
		}, Projectile.Center);

		int particleCount = Projectile.width >= 100 ? 22 : 12;
		for (int index = 0; index < particleCount; index++)
		{
			Vector2 direction = Main.rand.NextVector2Unit();
			Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.BlueCrystalShard,
				direction * Main.rand.NextFloat(2.5f, 7f) * scale, 40,
				new Color(210, 245, 255), Main.rand.NextFloat(0.65f, 1.1f) * scale);
			dust.noGravity = true;
		}
	}

	public override bool PreDraw(ref Color lightColor) => false;
}
