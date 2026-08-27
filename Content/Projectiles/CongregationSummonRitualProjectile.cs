using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Systems;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class CongregationSummonRitualProjectile : ModProjectile
{
	public const int RitualDuration = 156;
	private const int SealRevealTime = 46;
	private const int ImplosionTime = 108;
	private const int ReleaseTime = 132;
	private ref float Timer => ref Projectile.ai[0];
	private int PreferredPlayer => (int)Projectile.ai[1];
	public override string Texture => "Terraria/Images/MagicPixel";

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1_800;
	}

	public override void SetDefaults()
	{
		Projectile.width = 2;
		Projectile.height = 2;
		Projectile.timeLeft = RitualDuration + 2;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.netImportant = true;
	}

	public override void AI()
	{
		Vector2 shrineCenter = BuriedCourtSystem.GetDaisEffectPosition();
		Vector2 manifestationCenter = BuriedCourtSystem.GetBossSpawnPosition();
		Projectile.Center = shrineCenter;
		Timer++;

		float charge = SmoothStep(0f, 1f, Timer / ImplosionTime);
		Lighting.AddLight(shrineCenter, new Vector3(0.05f, 0.55f, 0.48f) * (0.35f + charge));
		Lighting.AddLight(manifestationCenter, new Vector3(0.08f, 0.7f, 0.62f) * charge);

		if (Timer == 1f && Main.netMode != NetmodeID.Server)
		{
			SoundEngine.PlaySound(SoundID.DD2_EtherianPortalOpen, shrineCenter);
		}

		if (Main.netMode != NetmodeID.Server)
		{
			SpawnRitualDust(shrineCenter, manifestationCenter, charge);
		}

		if (Timer >= ImplosionTime)
		{
			float shake = MathHelper.Lerp(0.35f, 3.5f, SmoothStep(0f, 1f, (Timer - ImplosionTime) / (ReleaseTime - ImplosionTime)));
			CongregationCameraSystem.AddShake(manifestationCenter, shake);
		}

		if (Timer == ReleaseTime)
		{
			ReleaseCongregation(manifestationCenter);
		}

		if (Timer >= ReleaseTime)
		{
			float waveProgress = MathHelper.Clamp((Timer - ReleaseTime) / (RitualDuration - ReleaseTime), 0f, 1f);
			CongregationShaderSystem.UpdateShockwave(manifestationCenter, waveProgress);
		}

		if (Timer >= RitualDuration)
		{
			Projectile.Kill();
		}
	}

	private void ReleaseCongregation(Vector2 manifestationCenter)
	{
		CongregationCameraSystem.AddShake(manifestationCenter, 13f);
		if (Main.netMode != NetmodeID.Server)
		{
			SoundEngine.PlaySound(SoundID.Roar, manifestationCenter);
			for (int index = 0; index < 48; index++)
			{
				Vector2 velocity = Main.rand.NextVector2CircularEdge(7.5f, 7.5f) * Main.rand.NextFloat(0.45f, 1f);
				Dust dust = Dust.NewDustPerfect(manifestationCenter, DustID.DungeonSpirit, velocity,
					80, new Color(116, 255, 229), Main.rand.NextFloat(0.85f, 1.35f));
				dust.noGravity = true;
			}
		}

		if (Main.netMode != NetmodeID.MultiplayerClient)
		{
			BuriedCourtSystem.SpawnBossFromRitual(PreferredPlayer);
		}
	}

	private void SpawnRitualDust(Vector2 shrineCenter, Vector2 manifestationCenter, float charge)
	{
		if (!Main.rand.NextBool(Timer < ImplosionTime ? 2 : 1))
		{
			return;
		}

		float pathProgress = Main.rand.NextFloat();
		Vector2 path = Vector2.Lerp(shrineCenter, manifestationCenter, pathProgress);
		float spiralRadius = MathHelper.Lerp(30f, 8f, pathProgress) * (1f - charge * 0.35f);
		float angle = Timer * 0.12f + pathProgress * MathHelper.TwoPi * 2f;
		Vector2 position = path + new Vector2(MathF.Cos(angle) * spiralRadius, MathF.Sin(angle) * spiralRadius * 0.42f);
		Vector2 velocity = (manifestationCenter - position).SafeNormalize(-Vector2.UnitY) * Main.rand.NextFloat(0.4f, 1.4f);
		Dust dust = Dust.NewDustPerfect(position, DustID.DungeonSpirit, velocity, 100,
			new Color(94, 246, 220), Main.rand.NextFloat(0.55f, 0.95f));
		dust.noGravity = true;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D glow = SoulOrbProjectile.GetGlowTexture();
		Texture2D ring = SoulOrbProjectile.GetRingTexture();
		Texture2D seal = ModContent.Request<Texture2D>(
			"SoulsOfTerra/Content/Bosses/SealedCongregation/SealedCongregation_seal").Value;
		Vector2 glowOrigin = glow.Size() * 0.5f;
		Vector2 sealOrigin = seal.Size() * 0.5f;
		Vector2 shrineCenter = BuriedCourtSystem.GetDaisEffectPosition() - Main.screenPosition;
		Vector2 manifestationCenter = BuriedCourtSystem.GetBossSpawnPosition() - Main.screenPosition;
		float charge = SmoothStep(0f, 1f, Timer / ImplosionTime);
		float time = Main.GlobalTimeWrappedHourly;

		DrawAscendingSouls(glow, glowOrigin, shrineCenter, manifestationCenter, charge, time);

		float shrinePulse = 1f + MathF.Sin(time * 5f) * 0.08f;
		Main.EntitySpriteDraw(glow, shrineCenter, null, new Color(61, 238, 214, 0) * (0.38f + charge * 0.38f),
			0f, glowOrigin, 1.25f * shrinePulse, SpriteEffects.None);
		Main.EntitySpriteDraw(ring, shrineCenter, null, new Color(176, 255, 239, 200),
			time * -0.42f, glowOrigin, (0.58f + charge * 0.24f) * shrinePulse, SpriteEffects.None);

		DrawSpectralSeals(seal, sealOrigin, glow, glowOrigin, manifestationCenter, charge, time);
		DrawReleaseBloom(glow, glowOrigin, ring, manifestationCenter);
		return false;
	}

	private void DrawAscendingSouls(Texture2D glow, Vector2 origin, Vector2 start, Vector2 end, float charge, float time)
	{
		for (int index = 0; index < 14; index++)
		{
			float pathProgress = (time * (0.17f + charge * 0.2f) + index / 14f) % 1f;
			Vector2 position = Vector2.Lerp(start, end, pathProgress);
			float spiralRadius = MathHelper.Lerp(34f, 10f, pathProgress) * (1f - charge * 0.42f);
			float angle = time * 2.2f + pathProgress * MathHelper.TwoPi * 2.5f + index;
			position += new Vector2(MathF.Cos(angle) * spiralRadius, MathF.Sin(angle) * spiralRadius * 0.38f);
			float scale = MathHelper.Lerp(0.13f, 0.055f, pathProgress) * (0.7f + charge * 0.5f);
			Main.EntitySpriteDraw(glow, position, null, new Color(158, 255, 237, 0) * (0.35f + charge * 0.5f),
				0f, origin, scale, SpriteEffects.None);
		}
	}

	private void DrawSpectralSeals(Texture2D seal, Vector2 sealOrigin, Texture2D glow, Vector2 glowOrigin,
		Vector2 center, float charge, float time)
	{
		float reveal = SmoothStep(0f, 1f, (Timer - SealRevealTime) / 34f);
		if (reveal <= 0f)
		{
			return;
		}

		float implosion = SmoothStep(0f, 1f, (Timer - ImplosionTime) / (ReleaseTime - ImplosionTime));
		float radius = MathHelper.Lerp(150f, 105f, reveal);
		radius = MathHelper.Lerp(radius, 34f, implosion);
		float alpha = reveal * (1f - SmoothStep(0f, 1f, (Timer - ReleaseTime) / 8f));
		for (int slot = 0; slot < 4; slot++)
		{
			float angle = -MathHelper.PiOver2 + slot * MathHelper.PiOver2 + time * MathHelper.Lerp(0.24f, 1.15f, implosion);
			Vector2 position = center + angle.ToRotationVector2() * radius;
			float scale = MathHelper.Lerp(0.62f, 0.3f, implosion);
			Main.EntitySpriteDraw(glow, position, null, new Color(74, 236, 215, 0) * (0.48f * alpha),
				0f, glowOrigin, 0.72f * scale, SpriteEffects.None);
			Main.EntitySpriteDraw(seal, position, null, new Color(155, 255, 236, 0) * (0.72f * alpha),
				angle + MathHelper.PiOver2, sealOrigin, scale, SpriteEffects.None);
		}
	}

	private void DrawReleaseBloom(Texture2D glow, Vector2 glowOrigin, Texture2D ring, Vector2 center)
	{
		float implosion = SmoothStep(0f, 1f, (Timer - ImplosionTime) / (ReleaseTime - ImplosionTime));
		if (implosion > 0f && Timer < ReleaseTime)
		{
			float pulse = 0.9f + implosion * 2.2f;
			Main.EntitySpriteDraw(glow, center, null, new Color(195, 255, 242, 0) * (0.32f + implosion * 0.5f),
				0f, glowOrigin, pulse, SpriteEffects.None);
			Main.EntitySpriteDraw(ring, center, null, new Color(221, 255, 248, 220),
				0f, glowOrigin, MathHelper.Lerp(1.25f, 0.22f, implosion), SpriteEffects.None);
		}

		if (Timer < ReleaseTime)
		{
			return;
		}

		float release = MathHelper.Clamp((Timer - ReleaseTime) / (RitualDuration - ReleaseTime), 0f, 1f);
		float fade = 1f - release;
		Main.EntitySpriteDraw(glow, center, null, new Color(228, 255, 249, 0) * fade,
			0f, glowOrigin, MathHelper.Lerp(5.5f, 2f, release), SpriteEffects.None);
		Main.EntitySpriteDraw(ring, center, null, new Color(139, 255, 232, 230) * fade,
			0f, glowOrigin, MathHelper.Lerp(0.25f, 3.8f, release), SpriteEffects.None);
	}

	public override void OnKill(int timeLeft) => CongregationShaderSystem.StopShockwave();

	public static bool IsRitualActive()
	{
		int ritualType = ModContent.ProjectileType<CongregationSummonRitualProjectile>();
		foreach (Projectile projectile in Main.ActiveProjectiles)
		{
			if (projectile.type == ritualType)
			{
				return true;
			}
		}

		return false;
	}

	private static float SmoothStep(float from, float to, float value)
	{
		float progress = MathHelper.Clamp((value - from) / (to - from), 0f, 1f);
		return progress * progress * (3f - 2f * progress);
	}
}
