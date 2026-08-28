using Microsoft.Xna.Framework;
using SoulsOfTerra.Common;
using SoulsOfTerra.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class UnisonWaveProjectile : ModProjectile
{
	public const int WaveDuration = 50;
	public const float MaximumRadius = 360f;
	private const float ReleaseFlashDuration = 22f;

	private float Age => Projectile.localAI[0];

	public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.PurificationPowder}";

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.DrawScreenCheckFluff[Type] = 420;
	}

	public override void SetDefaults()
	{
		Projectile.width = 2;
		Projectile.height = 2;
		Projectile.friendly = true;
		Projectile.DamageType = DamageClass.Melee;
		Projectile.penetrate = -1;
		Projectile.timeLeft = WaveDuration;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = WaveDuration;
		Projectile.netImportant = true;
	}

	public override bool ShouldUpdatePosition() => false;
	public override bool? CanDamage() => true;

	public override void AI()
	{
		if (Projectile.localAI[0] == 0f)
		{
			SpawnReleaseDust();
		}

		Projectile.localAI[0]++;
		float progress = WaveProgress();
		CongregationShaderSystem.UpdateShockwave(Projectile.Center, progress);
		float radius = CurrentRadius();
		Vector3 glow = new(0.12f, 0.62f, 0.54f);
		Lighting.AddLight(Projectile.Center + new Vector2(radius, 0f), glow * 0.35f);
		Lighting.AddLight(Projectile.Center, glow * MathHelper.Clamp(1f - progress, 0.15f, 0.7f));
	}

	public override void OnKill(int timeLeft) => CongregationShaderSystem.StopShockwave();

	public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
	{
		modifiers.HitDirectionOverride = target.Center.X >= Projectile.Center.X ? 1 : -1;
	}

	public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
	{
		return CongregationHymnWave.HitsAnnulus(Projectile.Center, CurrentRadius(), targetHitbox);
	}

	public override bool PreDraw(ref Color lightColor)
	{
		float progress = WaveProgress();
		// Daylight claps should read as a hymn of light, not a thick black hoop.
		CongregationHymnWave.DrawExpandingWave(Projectile.Center, CurrentRadius(), progress, 0f, 0f,
			darkBodyStrength: 0f);
		CongregationHymnWave.DrawReleaseFlash(Projectile.Center, Age, ReleaseFlashDuration, MaximumRadius / 820f);
		return false;
	}

	private float CurrentRadius() => CongregationHymnWave.EasedRadius(WaveProgress(), 12f, MaximumRadius);

	private float WaveProgress() => MathHelper.Clamp(Age / WaveDuration, 0f, 1f);

	private void SpawnReleaseDust()
	{
		if (Main.dedServ)
		{
			return;
		}

		for (int index = 0; index < 36; index++)
		{
			float angle = MathHelper.TwoPi * index / 36f + Main.rand.NextFloat(-0.04f, 0.04f);
			Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.DungeonSpirit,
				angle.ToRotationVector2() * Main.rand.NextFloat(2.4f, 7.5f), 80, new Color(85, 235, 215),
				Main.rand.NextFloat(0.85f, 1.35f));
			dust.noGravity = true;
		}
	}
}
