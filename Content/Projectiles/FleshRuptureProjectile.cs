using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class FleshRuptureProjectile : ModProjectile
{
	private const int RuptureSize = 112;

	public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.PurificationPowder}";

	public override void SetDefaults()
	{
		Projectile.width = RuptureSize;
		Projectile.height = RuptureSize;
		Projectile.friendly = true;
		Projectile.DamageType = DamageClass.Melee;
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
		return Collision.CanHitLine(Projectile.Center, 1, 1, target.position, target.width, target.height) ? null : false;
	}

	public override void AI()
	{
		if (Projectile.localAI[0] != 0f || Main.netMode == NetmodeID.Server)
		{
			return;
		}

		Projectile.localAI[0] = 1f;
		SoundEngine.PlaySound(SoundID.NPCDeath13 with { Volume = 0.65f, Pitch = -0.2f }, Projectile.Center);
		for (int index = 0; index < 28; index++)
		{
			Vector2 direction = Main.rand.NextVector2Unit();
			Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Blood,
				direction * Main.rand.NextFloat(2.5f, 7f), 70, new Color(150, 25, 40), Main.rand.NextFloat(0.9f, 1.35f));
			dust.noGravity = true;
		}
	}

	public override bool PreDraw(ref Color lightColor) => false;
}
