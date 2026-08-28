using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Common;
using SoulsOfTerra.Content.Bosses.SealedCongregation;
using SoulsOfTerra.Systems;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class CongregationBenedictionWaveProjectile : ModProjectile
{
	private const int ChargeEnd = 120;
	private const int WaveStart = 142;
	private const int WaveEnd = 226;
	private const float MaximumRadius = 820f;
	private const float GapHalfAngle = 0.2f;
	private const float ReleaseFlashDuration = 30f;
	private NPC Parent => Projectile.ai[0] >= 0f && Projectile.ai[0] < Main.maxNPCs ? Main.npc[(int)Projectile.ai[0]] : null;
	public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.PurificationPowder}";

	public override void SetDefaults()
	{
		Projectile.width = 2;
		Projectile.height = 2;
		Projectile.hostile = true;
		Projectile.penetrate = -1;
		Projectile.timeLeft = WaveEnd + 2;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.netImportant = true;
	}

	public override bool ShouldUpdatePosition() => false;
	public override bool? CanDamage() => Projectile.localAI[0] is >= WaveStart and <= WaveEnd;

	public override void AI()
	{
		NPC parent = Parent;
		if (parent is null || !parent.active || parent.ModNPC is not SealedCongregationBoss)
		{
			Projectile.Kill();
			return;
		}

		Projectile.Center = parent.Center;
		Projectile.localAI[0]++;
		if (Projectile.localAI[0] == 1f)
		{
			SoundEngine.PlaySound(SoundID.Item117 with { Pitch = -0.45f, Volume = 0.7f }, Projectile.Center);
		}
		else if (TryGetSealBeat(out int beat))
		{
			SoundEngine.PlaySound(SoundID.Item29 with { Pitch = -0.35f + beat * 0.12f, Volume = 0.55f }, Projectile.Center);
			SpawnSealBeatDust(beat);
		}
		else if (Projectile.localAI[0] == WaveStart)
		{
			SoundEngine.PlaySound(SoundID.Item74 with { Pitch = -0.25f, Volume = 1.05f }, Projectile.Center);
			SoundEngine.PlaySound(SoundID.Item14 with { Pitch = -0.65f, Volume = 0.8f }, Projectile.Center);
			SpawnReleaseDust();
		}

		if (Projectile.localAI[0] is >= WaveStart and <= WaveEnd)
		{
			// The screen distortion follows the damaging ring and fades before reaching the arena edge.
			CongregationShaderSystem.UpdateShockwave(Projectile.Center, WaveProgress());
		}
		else if (Projectile.localAI[0] > WaveEnd)
		{
			CongregationShaderSystem.StopShockwave();
		}

		float releaseLight = MathHelper.Clamp(1f - (Projectile.localAI[0] - WaveStart) / ReleaseFlashDuration, 0f, 1f);
		Lighting.AddLight(Projectile.Center,
			new Vector3(0.05f, 0.25f, 0.23f) * ChargeStrength() + new Vector3(0.22f, 0.85f, 0.72f) * releaseLight);
	}

	public override void OnKill(int timeLeft)
	{
		CongregationShaderSystem.StopShockwave();
	}

	public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
	{
		float radius = CurrentWaveRadius();
		if (!CongregationHymnWave.HitsAnnulus(Projectile.Center, radius, targetHitbox))
		{
			return false;
		}

		// The same angular gaps omitted by the renderer are always safe in collision.
		float targetAngle = (targetHitbox.Center.ToVector2() - Projectile.Center).ToRotation();
		return !CongregationHymnWave.AngleFallsInGap(targetAngle, Projectile.ai[1], GapHalfAngle);
	}

	public override bool PreDraw(ref Color lightColor)
	{
		float time = Projectile.localAI[0];
		if (time < WaveStart)
		{
			DrawGatheringStreams();
			DrawChargingSphere();
		}
		else
		{
			DrawExpandingWave();
			DrawReleaseFlash();
		}

		return false;
	}

	private void DrawChargingSphere()
	{
		Texture2D glow = SoulOrbProjectile.GetGlowTexture();
		Texture2D ring = SoulOrbProjectile.GetRingTexture();
		Vector2 center = Projectile.Center - Main.screenPosition;
		Vector2 glowOrigin = glow.Size() * 0.5f;
		Vector2 ringOrigin = ring.Size() * 0.5f;
		float strength = ChargeStrength();
		float radius = CurrentSphereRadius();
		float pulse = 1f + MathF.Sin(Main.GlobalTimeWrappedHourly * 6f) * 0.04f;
		float glowScale = radius * 2f / glow.Width;
		float ringScale = radius * 2f / ring.Width;

		Main.EntitySpriteDraw(glow, center, null, new Color(24, 188, 171, 0) * (0.3f * strength), 0f,
			glowOrigin, glowScale * 1.35f * pulse, SpriteEffects.None);
		Main.EntitySpriteDraw(glow, center, null, new Color(3, 7, 11, 235) * strength, 0f,
			glowOrigin, glowScale, SpriteEffects.None);
		Main.EntitySpriteDraw(ring, center, null, new Color(96, 235, 213, 0) * (0.65f * strength),
			Main.GlobalTimeWrappedHourly * 0.35f, ringOrigin, ringScale * pulse, SpriteEffects.None);
		DrawCollapsingRings(ring, ringOrigin, center, radius, strength);
		DrawOrbitingMotes(glow, glowOrigin, center, radius, strength);

		for (int gap = 0; gap < 4; gap++)
		{
			float angle = Projectile.ai[1] + MathHelper.PiOver2 * gap;
			Vector2 direction = angle.ToRotationVector2();
			Vector2 marker = center + direction * (radius + 28f);
			Main.EntitySpriteDraw(ring, marker, null, new Color(185, 255, 239, 0) * (0.45f + strength * 0.45f),
				-angle, ringOrigin, new Vector2(0.14f, 0.09f), SpriteEffects.None);
		}
	}

	private void DrawCollapsingRings(Texture2D ring, Vector2 origin, Vector2 center, float sphereRadius, float strength)
	{
		for (int index = 0; index < 3; index++)
		{
			float cycle = (strength * 1.8f + index / 3f) % 1f;
			float radius = sphereRadius + MathHelper.Lerp(95f, 8f, cycle);
			float scale = radius * 2f / ring.Width;
			float opacity = MathF.Sin(cycle * MathHelper.Pi) * strength;
			Main.EntitySpriteDraw(ring, center, null, new Color(55, 202, 186, 0) * (opacity * 0.28f),
				Main.GlobalTimeWrappedHourly * (index % 2 == 0 ? 0.4f : -0.4f), origin, scale, SpriteEffects.None);
		}
	}

	private void DrawOrbitingMotes(Texture2D glow, Vector2 origin, Vector2 center, float radius, float strength)
	{
		for (int index = 0; index < 12; index++)
		{
			float angle = MathHelper.TwoPi * index / 12f + Main.GlobalTimeWrappedHourly * 0.85f;
			float orbitRadius = radius + 18f + 8f * MathF.Sin(Main.GlobalTimeWrappedHourly * 2f + index);
			Vector2 position = center + angle.ToRotationVector2() * orbitRadius;
			float scale = index % 3 == 0 ? 0.1f : 0.055f;
			Main.EntitySpriteDraw(glow, position, null, new Color(94, 233, 211, 0) * (strength * 0.55f),
				angle, origin, new Vector2(scale * 1.7f, scale), SpriteEffects.None);
		}
	}

	private void DrawGatheringStreams()
	{
		Texture2D glow = SoulOrbProjectile.GetGlowTexture();
		Vector2 origin = glow.Size() * 0.5f;
		int streamIndex = 0;
		foreach (NPC seal in Main.ActiveNPCs)
		{
			if (seal.type != ModContent.NPCType<SealedCongregationSeal>() || (int)seal.ai[0] != (int)Projectile.ai[0])
			{
				continue;
			}

			int slot = (int)seal.ai[1];
			float activation = MathHelper.Clamp((Projectile.localAI[0] - SealBeatTime(slot)) / 14f, 0f, 1f);
			if (activation <= 0f)
			{
				continue;
			}

			Vector2 path = Projectile.Center - seal.Center;
			Vector2 normal = path.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2);
			DrawAwakenedSealHalo(seal, glow, origin, activation);
			for (int wisp = 0; wisp < 5; wisp++)
			{
				float progress = (Main.GlobalTimeWrappedHourly * 0.72f + wisp / 5f + streamIndex * 0.17f) % 1f;
				Vector2 worldPosition = Vector2.Lerp(seal.Center, Projectile.Center, progress);
				worldPosition += normal * MathF.Sin(progress * MathHelper.TwoPi * 1.5f + streamIndex) * (13f * (1f - progress));
				float scale = MathHelper.Lerp(0.11f, 0.04f, progress);
				Main.EntitySpriteDraw(glow, worldPosition - Main.screenPosition, null,
					new Color(94, 235, 214, 0) * ((0.35f + progress * 0.45f) * activation),
					path.ToRotation(), origin, new Vector2(scale * 1.55f, scale), SpriteEffects.None);
			}
			streamIndex++;
		}
	}

	private static void DrawAwakenedSealHalo(NPC seal, Texture2D glow, Vector2 origin, float activation)
	{
		float pulse = 0.31f + 0.035f * MathF.Sin(Main.GlobalTimeWrappedHourly * 5f + seal.ai[1]);
		Main.EntitySpriteDraw(glow, seal.Center - Main.screenPosition, null,
			new Color(57, 221, 201, 0) * (activation * 0.42f), 0f, origin, pulse, SpriteEffects.None);
	}

	private void DrawExpandingWave()
	{
		CongregationHymnWave.DrawExpandingWave(Projectile.Center, CurrentWaveRadius(), WaveProgress(),
			Projectile.ai[1], GapHalfAngle);
	}

	private void DrawReleaseFlash()
	{
		CongregationHymnWave.DrawReleaseFlash(Projectile.Center, Projectile.localAI[0] - WaveStart,
			ReleaseFlashDuration);
	}

	private void SpawnReleaseDust()
	{
		if (Main.dedServ)
		{
			return;
		}

		for (int index = 0; index < 80; index++)
		{
			float angle = MathHelper.TwoPi * index / 80f + Main.rand.NextFloat(-0.045f, 0.045f);
			float speed = Main.rand.NextFloat(3.5f, 10.5f);
			Vector2 velocity = angle.ToRotationVector2() * speed;
			Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.DungeonSpirit, velocity, 80,
				new Color(85, 235, 215), Main.rand.NextFloat(1f, 1.65f));
			dust.noGravity = true;
		}
	}

	private void SpawnSealBeatDust(int beat)
	{
		if (Main.dedServ)
		{
			return;
		}

		foreach (NPC seal in Main.ActiveNPCs)
		{
			if (seal.type != ModContent.NPCType<SealedCongregationSeal>() || (int)seal.ai[0] != (int)Projectile.ai[0] || (int)seal.ai[1] != beat)
			{
				continue;
			}

			for (int index = 0; index < 12; index++)
			{
				Vector2 velocity = (MathHelper.TwoPi * index / 12f).ToRotationVector2() * 2.5f;
				Dust dust = Dust.NewDustPerfect(seal.Center, DustID.DungeonSpirit, velocity, 90,
					new Color(90, 232, 212), 0.9f);
				dust.noGravity = true;
			}
			break;
		}
	}

	private bool TryGetSealBeat(out int beat)
	{
		for (beat = 0; beat < 4; beat++)
		{
			if (Projectile.localAI[0] == SealBeatTime(beat))
			{
				return true;
			}
		}

		beat = -1;
		return false;
	}

	private static int SealBeatTime(int slot) => 12 + slot * 24;

	private float ChargeStrength()
	{
		if (Projectile.localAI[0] < ChargeEnd)
		{
			return SmoothStep(0f, 1f, Projectile.localAI[0] / ChargeEnd);
		}

		return 1f - SmoothStep(0f, 1f, (Projectile.localAI[0] - ChargeEnd) / (WaveStart - ChargeEnd));
	}

	private float CurrentSphereRadius()
	{
		if (Projectile.localAI[0] < ChargeEnd)
		{
			return MathHelper.Lerp(18f, 108f, ChargeStrength());
		}

		float implosion = MathHelper.Clamp((Projectile.localAI[0] - ChargeEnd) / (WaveStart - ChargeEnd), 0f, 1f);
		return MathHelper.Lerp(108f, 12f, implosion * implosion);
	}

	private float CurrentWaveRadius()
	{
		return CongregationHymnWave.EasedRadius(WaveProgress(), 18f, MaximumRadius);
	}

	private float WaveProgress()
	{
		return MathHelper.Clamp((Projectile.localAI[0] - WaveStart) / (WaveEnd - WaveStart), 0f, 1f);
	}

	private static float SmoothStep(float from, float to, float value)
	{
		float amount = MathHelper.Clamp((value - from) / (to - from), 0f, 1f);
		return amount * amount * (3f - 2f * amount);
	}
}
