using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class SoulBandageImpactProjectile : ModProjectile
{
	public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.PurificationPowder}";

	public override void SetDefaults()
	{
		Projectile.width = 36;
		Projectile.height = 36;
		Projectile.friendly = true;
		Projectile.DamageType = DamageClass.Melee;
		Projectile.penetrate = 1;
		Projectile.timeLeft = 3;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = -1;
	}

	public override bool ShouldUpdatePosition() => false;

	public override void AI()
	{
		if (Projectile.localAI[0] != 0f || Main.netMode == NetmodeID.Server)
		{
			return;
		}

		Projectile.localAI[0] = 1f;
		Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = 0.5f, Pitch = -0.25f }, Projectile.Center);
		for (int index = 0; index < 10; index++)
		{
			Microsoft.Xna.Framework.Vector2 velocity = Main.rand.NextVector2Circular(4f, 4f);
			Dust dust = Dust.NewDustPerfect(Projectile.Center, index % 2 == 0 ? DustID.Web : DustID.DungeonSpirit,
				velocity, 90, index % 2 == 0 ? new Microsoft.Xna.Framework.Color(205, 190, 164) : new Microsoft.Xna.Framework.Color(82, 220, 198),
				Main.rand.NextFloat(0.7f, 1.05f));
			dust.noGravity = true;
		}
	}

	public override bool? CanHitNPC(NPC target)
	{
		return target.whoAmI == (int)Projectile.ai[0] ? null : false;
	}

	public override bool PreDraw(ref Microsoft.Xna.Framework.Color lightColor) => false;
}
