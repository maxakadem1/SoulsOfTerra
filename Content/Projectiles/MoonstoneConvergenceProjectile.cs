using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class MoonstoneConvergenceProjectile : ModProjectile
{
	private const int Lifetime = 90;
	private const float PullRadius = 320f;
	private const float MaximumPullSpeed = 9f;
	private const float MoonRadius = 92f;

	public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.MagnetSphereBall}";

	public override void SetDefaults()
	{
		Projectile.width = 36;
		Projectile.height = 36;
		Projectile.friendly = false;
		Projectile.DamageType = DamageClass.Magic;
		Projectile.penetrate = -1;
		Projectile.timeLeft = Lifetime;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.netImportant = true;
	}

	public override bool ShouldUpdatePosition() => false;

	public override void AI()
	{
		Projectile.ai[0]++;
		float progress = MathHelper.Clamp(Projectile.ai[0] / Lifetime, 0f, 1f);
		float intensity = progress * progress;
		Lighting.AddLight(Projectile.Center, 0.35f + intensity * 0.65f, 0.8f + intensity, 1.05f + intensity * 1.2f);

		PullEnemies(intensity);
		CreateParticleMoon(progress);

		if (Projectile.ai[0] == 1f && Main.netMode != NetmodeID.Server)
		{
			SoundEngine.PlaySound(SoundID.Item15 with { Volume = 0.65f, Pitch = -0.35f }, Projectile.Center);
		}
		else if (Projectile.ai[0] == 68f && Main.netMode != NetmodeID.Server)
		{
			SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.65f, Pitch = -0.1f }, Projectile.Center);
		}

		if (Projectile.timeLeft > 1)
		{
			return;
		}

		if (Projectile.owner == Main.myPlayer)
		{
			Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
				ModContent.ProjectileType<MoonstoneCollapseProjectile>(), Projectile.damage, Projectile.knockBack,
				Projectile.owner);
		}
	}

	public override bool PreDraw(ref Color lightColor) => false;

	private void PullEnemies(float intensity)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			return;
		}

		foreach (NPC npc in Main.ActiveNPCs)
		{
			if (!CanPull(npc))
			{
				continue;
			}

			Vector2 offset = Projectile.Center - npc.Center;
			float distance = offset.Length();
			if (distance <= 18f || distance > PullRadius
				|| !Collision.CanHitLine(Projectile.Center, 1, 1, npc.position, npc.width, npc.height))
			{
				continue;
			}

			float distanceStrength = 1f - distance / PullRadius;
			Vector2 acceleration = offset.SafeNormalize(Vector2.Zero) * (0.12f + intensity * 0.72f) * distanceStrength;
			npc.velocity = Vector2.Clamp(npc.velocity + acceleration,
				new Vector2(-MaximumPullSpeed), new Vector2(MaximumPullSpeed));
			if ((int)Projectile.ai[0] % 10 == 0)
			{
				npc.netUpdate = true;
			}
		}
	}

	private static bool CanPull(NPC npc)
	{
		return npc.CanBeChasedBy() && !npc.boss && npc.realLife < 0 && npc.aiStyle != NPCAIStyleID.Worm;
	}

	private void CreateParticleMoon(float progress)
	{
		if (Main.netMode == NetmodeID.Server)
		{
			return;
		}

		float formation = MathHelper.SmoothStep(0f, 1f, Utils.GetLerpValue(0f, 0.2f, progress, clamped: true));
		float implosion = MathHelper.SmoothStep(0f, 1f, Utils.GetLerpValue(0.75f, 1f, progress, clamped: true));
		float pulse = 1f + System.MathF.Sin(Projectile.ai[0] * 0.18f) * 0.035f * (1f - implosion);
		float radius = MoonRadius * formation * (1f - implosion) * pulse;
		int particleCount = 7 + (int)(progress * 3f);
		for (int index = 0; index < particleCount; index++)
		{
			float angle = Projectile.ai[0] * 0.045f + MathHelper.TwoPi * index / particleCount;
			Vector2 radial = angle.ToRotationVector2();
			Vector2 tangent = new(-radial.Y, radial.X);
			Vector2 velocity = tangent * MathHelper.Lerp(0.35f, 1.2f, progress) - radial * implosion * 5.5f;
			Dust dust = Dust.NewDustPerfect(Projectile.Center + radial * radius, DustID.TintableDustLighted,
				velocity, 25, new Color(215, 250, 255), MathHelper.Lerp(0.9f, 1.4f, progress));
			dust.noGravity = true;
		}

		// Inward streaks make the final contraction visibly accelerate into the center.
		if (implosion > 0f)
		{
			for (int index = 0; index < 3; index++)
			{
				Vector2 offset = Main.rand.NextVector2Unit() * Main.rand.NextFloat(100f, 150f);
				Dust dust = Dust.NewDustPerfect(Projectile.Center + offset, DustID.BlueCrystalShard,
					-offset.SafeNormalize(Vector2.Zero) * MathHelper.Lerp(4f, 10f, implosion), 25,
					new Color(235, 255, 255), Main.rand.NextFloat(0.9f, 1.35f));
				dust.noGravity = true;
			}
		}
	}
}
