using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class ServantGoreExplosionProjectile : ModProjectile
{
	private const int ExplosionSize = 48;

	public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.PurificationPowder}";

	public override void SetDefaults()
	{
		Projectile.width = ExplosionSize;
		Projectile.height = ExplosionSize;
		Projectile.friendly = true;
		Projectile.DamageType = DamageClass.Magic;
		Projectile.penetrate = -1;
		Projectile.timeLeft = 2;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = -1;
	}

	public override bool ShouldUpdatePosition() => false;

	public override void AI()
	{
		if (Projectile.localAI[0] != 0f)
		{
			return;
		}

		Projectile.localAI[0] = 1f;
		CreateGoreBurst();
	}

	public override bool PreDraw(ref Color lightColor) => false;

	private void CreateGoreBurst()
	{
		if (Main.netMode == NetmodeID.Server)
		{
			return;
		}

		SoundEngine.PlaySound(SoundID.NPCDeath1 with { Volume = 0.7f, Pitch = 0.2f }, Projectile.Center);
		for (int index = 0; index < 14; index++)
		{
			Vector2 velocity = Main.rand.NextVector2Circular(3.8f, 3.8f);
			Dust.NewDustPerfect(Projectile.Center, DustID.Blood, velocity, 40, default, Main.rand.NextFloat(0.8f, 1.25f));
		}

		// Small vanilla flesh chunks make the servant visibly rupture without excessive clutter.
		int[] goreTypes = { GoreID.BloodZombieChunk, GoreID.BloodZombieChunk2 };
		for (int index = 0; index < goreTypes.Length; index++)
		{
			Vector2 velocity = Main.rand.NextVector2Circular(2.8f, 2.8f) + new Vector2(0f, -1f);
			Gore.NewGore(Projectile.GetSource_Death(), Projectile.Center, velocity, goreTypes[index], 0.45f);
		}
	}
}
