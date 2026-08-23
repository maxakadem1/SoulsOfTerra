using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class ServantEyeProjectile : ModProjectile
{
	private const int AwakeningDelay = 60;
	private const float TargetRange = 700f;
	private const float HomingSpeed = 16f;
	private const float HomingInertia = 6f;
	private const float ArcBias = 0.85f;
	private const float ArcFadeDistance = 320f;

	public override string Texture => $"Terraria/Images/NPC_{NPCID.ServantofCthulhu}";

	public override void SetStaticDefaults()
	{
		Main.projFrames[Type] = Main.npcFrameCount[NPCID.ServantofCthulhu];
		ProjectileID.Sets.CultistIsResistantTo[Type] = true;
	}

	public override void SetDefaults()
	{
		Projectile.width = 22;
		Projectile.height = 22;
		Projectile.friendly = false;
		Projectile.DamageType = DamageClass.Magic;
		Projectile.penetrate = -1;
		Projectile.timeLeft = 360;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
	}

	public override void AI()
	{
		Projectile.ai[0]++;
		Animate();
		Projectile.spriteDirection = 1;
		// The vanilla servant artwork faces downward at zero rotation.
		Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
		Lighting.AddLight(Projectile.Center, 0.22f, 0.025f, 0.035f);

		if (Projectile.ai[0] < AwakeningDelay)
		{
			// A slight outward curl separates the volley before target acquisition.
			Projectile.velocity = Projectile.velocity.RotatedBy(Projectile.ai[1] * 0.0018f);
			return;
		}

		if (Projectile.ai[0] == AwakeningDelay)
		{
			CreateAwakeningPulse();
			Projectile.netUpdate = true;
		}

		NPC target = FindNearestTarget();
		if (target is not null)
		{
			Vector2 toTarget = target.Center - Projectile.Center;
			float distance = toTarget.Length();
			Vector2 targetDirection = toTarget.SafeNormalize(Vector2.UnitX);
			Vector2 perpendicular = new(-targetDirection.Y, targetDirection.X);
			float arcDirection = Projectile.ai[1] != 0f ? System.Math.Sign(Projectile.ai[1]) : (Projectile.identity % 2 == 0 ? 1f : -1f);
			// A persistent tangent bias creates one broad arc instead of a repeated wobble.
			float arcFade = MathHelper.Clamp((distance - 28f) / ArcFadeDistance, 0f, 1f);
			Vector2 arcingDirection = (targetDirection + perpendicular * arcDirection * ArcBias * arcFade).SafeNormalize(targetDirection);
			Vector2 desiredVelocity = arcingDirection * HomingSpeed;
			Projectile.velocity = (Projectile.velocity * (HomingInertia - 1f) + desiredVelocity) / HomingInertia;

			if (Projectile.owner == Main.myPlayer && Projectile.Hitbox.Intersects(target.Hitbox))
			{
				Explode();
			}
		}
	}

	private void Animate()
	{
		if (++Projectile.frameCounter < 6)
		{
			return;
		}

		Projectile.frameCounter = 0;
		Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
	}

	private NPC FindNearestTarget()
	{
		NPC nearest = null;
		float nearestDistanceSquared = TargetRange * TargetRange;
		for (int index = 0; index < Main.maxNPCs; index++)
		{
			NPC npc = Main.npc[index];
			if (!npc.CanBeChasedBy(Projectile))
			{
				continue;
			}

			float distanceSquared = Vector2.DistanceSquared(Projectile.Center, npc.Center);
			if (distanceSquared < nearestDistanceSquared)
			{
				nearest = npc;
				nearestDistanceSquared = distanceSquared;
			}
		}

		return nearest;
	}

	private void Explode()
	{
		Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
			ModContent.ProjectileType<ServantGoreExplosionProjectile>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
		Projectile.Kill();
	}

	private void CreateAwakeningPulse()
	{
		for (int index = 0; index < 5; index++)
		{
			Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Blood, Main.rand.NextVector2Circular(1.8f, 1.8f), 80, default, 0.8f);
			dust.noGravity = true;
		}
	}
}
