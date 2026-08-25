using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class ServantEyeProjectile : ModProjectile
{
	private static readonly VertexStrip TrailStrip = new();
	private const int AwakeningDelay = 60;
	private const float TargetRange = 700f;
	private const float HomingSpeed = 16f;
	private const float HomingInertia = 6f;
	private const float ArcBias = 0.85f;
	private const float ArcFadeDistance = 320f;

	public override string Texture => "SoulsOfTerra/Content/Projectiles/ServantsGaze_proj";

	public override void SetStaticDefaults()
	{
		Main.projFrames[Type] = 1;
		ProjectileID.Sets.CultistIsResistantTo[Type] = true;
		ProjectileID.Sets.TrailCacheLength[Type] = 6;
		ProjectileID.Sets.TrailingMode[Type] = 2;
	}

	public override void SetDefaults()
	{
		// The custom servant uses a compact native-size sprite and matching collision box.
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
		Projectile.spriteDirection = 1;
		// The side-profile artwork faces right at zero rotation.
		Projectile.rotation = Projectile.velocity.ToRotation();

		if (Projectile.ai[0] < AwakeningDelay)
		{
			float pulse = 0.75f + System.MathF.Sin(Projectile.ai[0] * 0.16f) * 0.25f;
			Lighting.AddLight(Projectile.Center, 0.2f * pulse, 0.018f, 0.028f);
			CreateDormantMote();
			// A slight outward curl separates the volley before target acquisition.
			Projectile.velocity = Projectile.velocity.RotatedBy(Projectile.ai[1] * 0.0018f);
			return;
		}

		Lighting.AddLight(Projectile.Center, 0.28f, 0.055f, 0.07f);
		CreateHomingGlint();

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

	public override bool PreDraw(ref Color lightColor)
	{
		if (Projectile.ai[0] < AwakeningDelay)
		{
			return true;
		}

		// Use the same primitive-strip shader pipeline as Terraria's Rainbow Rod.
		GameShaders.Misc["RainbowRod"]
			.UseSaturation(-2.8f)
			.UseOpacity(4f)
			.Apply();
		TrailStrip.PrepareStripWithProceduralPadding(Projectile.oldPos, Projectile.oldRot, TrailColor, TrailWidth,
			-Main.screenPosition + Projectile.Size * 0.5f, includeBacksides: false, tryStoppingOddBug: true);
		TrailStrip.DrawTrail();
		Main.pixelShader.CurrentTechnique.Passes[0].Apply();

		return true;
	}

	private static Color TrailColor(float progress)
	{
		float colorProgress = Utils.GetLerpValue(-0.2f, 0.5f, progress, clamped: true);
		Color color = Color.Lerp(new Color(255, 230, 200), new Color(175, 8, 24), colorProgress);
		color *= 1f - Utils.GetLerpValue(0f, 0.98f, progress, clamped: false);
		color.A = 0;
		return color;
	}

	private static float TrailWidth(float progress)
	{
		float opening = Utils.GetLerpValue(0f, 0.2f, progress, clamped: true);
		float curvedOpening = 1f - (1f - opening) * (1f - opening);
		return MathHelper.Lerp(0f, 2.2f, curvedOpening);
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
		// The blood ring ruptures outward while a warm iris flash wakes the eye.
		for (int index = 0; index < 10; index++)
		{
			Vector2 direction = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * index / 10f);
			Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Blood, direction * 2.4f, 70, default, 0.9f);
			dust.noGravity = true;
		}

		for (int index = 0; index < 6; index++)
		{
			Vector2 direction = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * index / 6f);
			Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.TintableDustLighted, direction * 1.4f,
				80, new Color(255, 220, 184), 0.75f);
			dust.noGravity = true;
		}
	}

	private void CreateDormantMote()
	{
		if (!Main.rand.NextBool(7))
		{
			return;
		}

		Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(7f, 7f), DustID.Blood,
			-Projectile.velocity * 0.08f, 110, default, 0.65f);
		dust.noGravity = true;
	}

	private void CreateHomingGlint()
	{
		if (!Main.rand.NextBool(5))
		{
			return;
		}

		Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
		Dust dust = Dust.NewDustPerfect(Projectile.Center + forward * 8f, DustID.TintableDustLighted,
			-Projectile.velocity * 0.04f, 100, new Color(255, 220, 184), 0.65f);
		dust.noGravity = true;
	}
}
