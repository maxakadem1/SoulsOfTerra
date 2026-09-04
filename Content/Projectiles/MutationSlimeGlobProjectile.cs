using Microsoft.Xna.Framework;
using SoulsOfTerra.Content.Buffs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public sealed class MutationSlimeGlobProjectile : ModProjectile
{
	public override string Texture => $"Terraria/Images/Item_{ItemID.Gel}";

	public override void SetDefaults()
	{
		Projectile.width = 12;
		Projectile.height = 12;
		Projectile.friendly = true;
		Projectile.penetrate = 1;
		Projectile.timeLeft = 3 * 60;
		Projectile.tileCollide = true;
		Projectile.ignoreWater = false;
		Projectile.DamageType = DamageClass.Generic;
	}

	public override void AI()
	{
		Projectile.velocity.Y = System.Math.Min(10f, Projectile.velocity.Y + 0.24f);
		Projectile.rotation += Projectile.velocity.X * 0.08f;
		Lighting.AddLight(Projectile.Center, 0.06f, 0.2f, 0.11f);
	}

	public override bool OnTileCollide(Vector2 oldVelocity)
	{
		// One rebound gives the radial spray useful terrain coverage without lingering clutter.
		if (Projectile.localAI[0] >= 1f)
		{
			return true;
		}

		Projectile.localAI[0] = 1f;
		if (Projectile.velocity.X != oldVelocity.X)
		{
			Projectile.velocity.X = -oldVelocity.X * 0.55f;
		}
		if (Projectile.velocity.Y != oldVelocity.Y)
		{
			Projectile.velocity.Y = -oldVelocity.Y * 0.55f;
		}
		return false;
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		target.AddBuff(ModContent.BuffType<MutationSlimeCoatingBuff>(), 5 * 60);
	}

}
