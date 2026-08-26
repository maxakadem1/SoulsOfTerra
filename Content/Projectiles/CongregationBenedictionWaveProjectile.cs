using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Content.Bosses.SealedCongregation;
using SoulsOfTerra.Systems;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
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
		const float halfThickness = 18f;
		Vector2 closest = new(
			MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right),
			MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom));
		float minimumDistance = Vector2.Distance(Projectile.Center, closest);
		float maximumDistance = 0f;
		Vector2[] corners =
		[
			targetHitbox.TopLeft(),
			targetHitbox.TopRight(),
			targetHitbox.BottomLeft(),
			targetHitbox.BottomRight()
		];
		foreach (Vector2 corner in corners)
		{
			maximumDistance = Math.Max(maximumDistance, Vector2.Distance(Projectile.Center, corner));
		}

		if (radius + halfThickness < minimumDistance || radius - halfThickness > maximumDistance)
		{
			return false;
		}

		// The same angular gaps omitted by the renderer are always safe in collision.
		float targetAngle = (targetHitbox.Center.ToVector2() - Projectile.Center).ToRotation();
		return !AngleFallsInGap(targetAngle);
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
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Texture2D glow = SoulOrbProjectile.GetGlowTexture();
		Vector2 glowOrigin = glow.Size() * 0.5f;
		float progress = WaveProgress();
		float radius = CurrentWaveRadius();
		float opacity = MathF.Sin(progress * MathHelper.Pi);
		float phase = Main.GlobalTimeWrappedHourly * 5.2f;

		// Soft echoes make the ring read as a moving wall of energy instead of one flat line.
		DrawWaveRing(pixel, radius - 48f, 8f, new Color(16, 105, 101, 0) * (opacity * 0.22f), 8f, phase + 1.6f);
		DrawWaveRing(pixel, radius - 24f, 12f, new Color(28, 176, 163, 0) * (opacity * 0.3f), 5f, phase + 0.8f);
		DrawWaveRing(pixel, radius + 14f, 2f, new Color(132, 255, 234, 0) * (opacity * 0.4f), 4f, phase);

		const int segments = 112;

		for (int segment = 0; segment < segments; segment++)
		{
			float startAngle = MathHelper.TwoPi * segment / segments;
			float endAngle = MathHelper.TwoPi * (segment + 1) / segments;
			float middleAngle = (startAngle + endAngle) * 0.5f;
			if (AngleFallsInGap(middleAngle))
			{
				continue;
			}

			Vector2 start = Projectile.Center + startAngle.ToRotationVector2() * radius - Main.screenPosition;
			Vector2 end = Projectile.Center + endAngle.ToRotationVector2() * radius - Main.screenPosition;
			DrawLine(pixel, start, end, new Color(2, 8, 11, 220) * opacity, 24f);
			DrawLine(pixel, start, end, new Color(35, 213, 193, 0) * (opacity * 0.52f), 15f);
			DrawLine(pixel, start, end, new Color(222, 255, 248, 0) * (opacity * 0.98f), 4.5f);

			if (segment % 7 == 0)
			{
				float flicker = 0.75f + 0.25f * MathF.Sin(segment * 2.7f + phase);
				Main.EntitySpriteDraw(glow, (start + end) * 0.5f, null,
					new Color(79, 239, 215, 0) * (opacity * 0.65f * flicker), middleAngle + MathHelper.PiOver2, glowOrigin,
					new Vector2(0.2f, 0.055f), SpriteEffects.None);
			}
		}

		DrawWaveFragments(glow, glowOrigin, radius, opacity, phase);
	}

	private void DrawWaveRing(Texture2D pixel, float radius, float width, Color color, float wobble, float phase)
	{
		if (radius <= wobble + 2f)
		{
			return;
		}

		const int segments = 112;
		for (int segment = 0; segment < segments; segment++)
		{
			float startAngle = MathHelper.TwoPi * segment / segments;
			float endAngle = MathHelper.TwoPi * (segment + 1) / segments;
			if (AngleFallsInGap((startAngle + endAngle) * 0.5f, 0.035f))
			{
				continue;
			}

			float startRadius = radius + MathF.Sin(startAngle * 9f + phase) * wobble;
			float endRadius = radius + MathF.Sin(endAngle * 9f + phase) * wobble;
			Vector2 start = Projectile.Center + startAngle.ToRotationVector2() * startRadius - Main.screenPosition;
			Vector2 end = Projectile.Center + endAngle.ToRotationVector2() * endRadius - Main.screenPosition;
			DrawLine(pixel, start, end, color, width);
		}
	}

	private void DrawWaveFragments(Texture2D glow, Vector2 origin, float radius, float opacity, float phase)
	{
		for (int fragment = 0; fragment < 28; fragment++)
		{
			float angle = MathHelper.TwoPi * fragment / 28f + 0.035f * MathF.Sin(fragment * 4.1f + phase);
			if (AngleFallsInGap(angle, 0.045f))
			{
				continue;
			}

			float offset = MathF.Sin(fragment * 2.3f + phase * 1.4f) * 20f;
			Vector2 position = Projectile.Center + angle.ToRotationVector2() * (radius + offset) - Main.screenPosition;
			float scale = 0.045f + 0.025f * (0.5f + 0.5f * MathF.Sin(fragment * 1.7f + phase));
			Main.EntitySpriteDraw(glow, position, null, new Color(157, 255, 237, 0) * (opacity * 0.7f),
				angle + MathHelper.PiOver2, origin, new Vector2(scale * 3.8f, scale), SpriteEffects.None);
		}
	}

	private void DrawReleaseFlash()
	{
		float age = Projectile.localAI[0] - WaveStart;
		if (age is < 0f or > ReleaseFlashDuration)
		{
			return;
		}

		Texture2D glow = SoulOrbProjectile.GetGlowTexture();
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Vector2 center = Projectile.Center - Main.screenPosition;
		Vector2 origin = glow.Size() * 0.5f;
		float progress = age / ReleaseFlashDuration;
		float opacity = MathF.Pow(1f - progress, 1.35f);
		float bloomScale = MathHelper.Lerp(0.75f, 5.8f, MathF.Sqrt(progress));
		Main.EntitySpriteDraw(glow, center, null, new Color(18, 211, 190, 0) * (opacity * 0.58f),
			0f, origin, bloomScale, SpriteEffects.None);
		Main.EntitySpriteDraw(glow, center, null, new Color(235, 255, 250, 0) * opacity,
			0f, origin, MathHelper.Lerp(0.85f, 2f, progress), SpriteEffects.None);

		for (int ring = 0; ring < 3; ring++)
		{
			float ringProgress = MathHelper.Clamp(progress * 1.45f - ring * 0.14f, 0f, 1f);
			if (ringProgress <= 0f)
			{
				continue;
			}

			float ringOpacity = MathF.Sin(ringProgress * MathHelper.Pi) * (1f - ring * 0.18f);
			float ringRadius = MathHelper.Lerp(18f, 290f, 1f - MathF.Pow(1f - ringProgress, 2f));
			DrawReleaseRing(pixel, ringRadius, MathHelper.Lerp(8f, 1.5f, ringProgress),
				new Color(177, 255, 239, 0) * ringOpacity);
		}

		// Irregular shards sell the detonation without recreating the four charge spokes.
		for (int shard = 0; shard < 24; shard++)
		{
			float variance = MathF.Sin(shard * 12.9898f) * 0.08f;
			float angle = MathHelper.TwoPi * shard / 24f + variance;
			Vector2 direction = angle.ToRotationVector2();
			float startDistance = MathHelper.Lerp(18f, 105f, progress);
			float length = MathHelper.Lerp(80f + shard % 5 * 8f, 20f, progress);
			DrawLine(pixel, center + direction * startDistance, center + direction * (startDistance + length),
				new Color(204, 255, 244, 0) * (opacity * 0.72f), MathHelper.Lerp(4.5f, 0.8f, progress));
		}
	}

	private void DrawReleaseRing(Texture2D pixel, float radius, float width, Color color)
	{
		const int segments = 72;
		Vector2 center = Projectile.Center - Main.screenPosition;
		for (int segment = 0; segment < segments; segment++)
		{
			float startAngle = MathHelper.TwoPi * segment / segments;
			float endAngle = MathHelper.TwoPi * (segment + 1) / segments;
			Vector2 start = center + startAngle.ToRotationVector2() * radius;
			Vector2 end = center + endAngle.ToRotationVector2() * radius;
			DrawLine(pixel, start, end, color, width);
		}
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
		float progress = WaveProgress();
		float eased = 1f - MathF.Pow(1f - progress, 3f);
		return MathHelper.Lerp(18f, MaximumRadius, eased);
	}

	private float WaveProgress()
	{
		return MathHelper.Clamp((Projectile.localAI[0] - WaveStart) / (WaveEnd - WaveStart), 0f, 1f);
	}

	private bool AngleFallsInGap(float angle, float padding = 0f)
	{
		float relative = MathHelper.WrapAngle(angle - Projectile.ai[1]);
		float nearestGap = MathF.Round(relative / MathHelper.PiOver2) * MathHelper.PiOver2;
		return MathF.Abs(MathHelper.WrapAngle(relative - nearestGap)) <= GapHalfAngle + padding;
	}

	private static float SmoothStep(float from, float to, float value)
	{
		float amount = MathHelper.Clamp((value - from) / (to - from), 0f, 1f);
		return amount * amount * (3f - 2f * amount);
	}

	private static void DrawLine(Texture2D texture, Vector2 start, Vector2 end, Color color, float width)
	{
		Vector2 difference = end - start;
		Vector2 origin = new(0f, texture.Height * 0.5f);
		Main.EntitySpriteDraw(texture, start, null, color, difference.ToRotation(), origin,
			new Vector2(difference.Length() / texture.Width, width / texture.Height), SpriteEffects.None);
	}
}
