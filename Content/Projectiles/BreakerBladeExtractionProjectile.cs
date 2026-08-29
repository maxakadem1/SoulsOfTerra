using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class BreakerBladeExtractionProjectile : ModProjectile
{
	private int TargetIndex => (int)Projectile.ai[0];

	public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.PurificationPowder}";

	public override void SetDefaults()
	{
		Projectile.width = 4;
		Projectile.height = 4;
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
	public override bool PreDraw(ref Color lightColor) => false;

	public override bool? CanHitNPC(NPC target) => target.whoAmI == TargetIndex ? null : false;

	public override void AI()
	{
		if (TargetIndex < 0 || TargetIndex >= Main.maxNPCs || !Main.npc[TargetIndex].active)
		{
			Projectile.Kill();
			return;
		}

		Projectile.Center = Main.npc[TargetIndex].Center;
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (Main.netMode == NetmodeID.Server)
		{
			return;
		}

		SoundEngine.PlaySound(SoundID.Item16 with { Volume = 0.62f, Pitch = -0.15f }, target.Center);
		for (int index = 0; index < 10; index++)
		{
			Dust dust = Dust.NewDustPerfect(target.Center, DustID.Web, Main.rand.NextVector2Circular(3.5f, 3.5f),
				85, new Color(214, 201, 178), 0.85f);
			dust.noGravity = true;
		}
	}
}
