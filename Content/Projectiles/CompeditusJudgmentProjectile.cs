using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class CompeditusJudgmentProjectile : ModProjectile
{
	private const int ImplosionDuration = 18;
	private const int TotalDuration = 30;
	private int age;

	public override string Texture => "Terraria/Images/MagicPixel";

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.MinionShot[Type] = true;
	}

	public override void SetDefaults()
	{
		Projectile.width = 112;
		Projectile.height = 112;
		Projectile.friendly = true;
		Projectile.DamageType = DamageClass.Summon;
		Projectile.penetrate = -1;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.timeLeft = TotalDuration;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = -1;
	}

	public override bool? CanDamage() => age >= ImplosionDuration && age <= ImplosionDuration + 2;

	public override void AI()
	{
		int targetIndex = (int)Projectile.ai[0] - 1;
		if (age < ImplosionDuration && targetIndex >= 0 && targetIndex < Main.maxNPCs)
		{
			NPC target = Main.npc[targetIndex];
			if (target.active)
			{
				Projectile.Center = target.Center;
			}
		}

		if (age == 0)
		{
			SoundEngine.PlaySound(SoundID.Item15 with { Volume = 0.5f, Pitch = -0.25f }, Projectile.Center);
		}
		else if (age == ImplosionDuration)
		{
			SoundEngine.PlaySound(SoundID.Item94 with { Volume = 0.72f, Pitch = 0.32f }, Projectile.Center);
			CreateReleaseDust();
		}

		float intensity = age < ImplosionDuration
			? age / (float)ImplosionDuration
			: 1f - (age - ImplosionDuration) / (float)(TotalDuration - ImplosionDuration);
		Lighting.AddLight(Projectile.Center, new Vector3(0.08f, 0.46f, 0.42f) * MathHelper.Clamp(intensity, 0f, 1f));
		age++;
	}

	private void CreateReleaseDust()
	{
		if (Main.dedServ)
		{
			return;
		}

		int sealCount = Math.Clamp((int)Projectile.ai[1], 1, 4);
		for (int index = 0; index < 14 + sealCount * 3; index++)
		{
			Vector2 velocity = Main.rand.NextVector2CircularEdge(3.2f, 3.2f) * Main.rand.NextFloat(0.55f, 1f);
			Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.DungeonSpirit, velocity,
				90, new Color(83, 241, 220), Main.rand.NextFloat(0.65f, 1f));
			dust.noGravity = true;
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D glow = SoulOrbProjectile.GetGlowTexture();
		Texture2D ring = SoulOrbProjectile.GetRingTexture();
		Vector2 center = Projectile.Center - Main.screenPosition;
		Vector2 origin = glow.Size() * 0.5f;
		if (age < ImplosionDuration)
		{
			float progress = age / (float)ImplosionDuration;
			float eased = progress * progress;
			float ringScale = MathHelper.Lerp(1.05f, 0.12f, eased);
			Main.EntitySpriteDraw(ring, center, null, new Color(76, 239, 219, 0) * (0.35f + progress * 0.55f),
				0f, origin, ringScale, SpriteEffects.None);
			Main.EntitySpriteDraw(glow, center, null, new Color(197, 255, 247, 0) * (progress * 0.65f),
				0f, origin, MathHelper.Lerp(0.38f, 0.16f, eased), SpriteEffects.None);
			return false;
		}

		float release = (age - ImplosionDuration) / (float)(TotalDuration - ImplosionDuration);
		float opacity = 1f - release;
		float releaseScale = MathHelper.Lerp(0.18f, 1.25f, MathF.Sqrt(MathHelper.Clamp(release, 0f, 1f)));
		Main.EntitySpriteDraw(glow, center, null, new Color(215, 255, 250, 0) * opacity,
			0f, origin, releaseScale, SpriteEffects.None);
		Main.EntitySpriteDraw(ring, center, null, new Color(59, 234, 215, 0) * (opacity * 0.85f),
			0f, origin, releaseScale * 0.9f, SpriteEffects.None);
		Main.EntitySpriteDraw(glow, center, null, Color.White * (opacity * 0.85f),
			0f, origin, 0.16f * opacity, SpriteEffects.None);
		return false;
	}
}
