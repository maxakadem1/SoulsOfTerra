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

public class CongregationJudgmentBeamProjectile : ModProjectile
{
	private const int WarningDuration = 45;
	private const int BeamDuration = 150;
	private const float BeamLength = 2_600f;
	private const float SweepAngle = MathHelper.Pi / 7.5f;
	private const float CollisionWidth = 42f;
	private readonly bool[] hitPlayers = new bool[Main.maxPlayers];

	private int Age => WarningDuration + BeamDuration - Projectile.timeLeft;
	private bool IsFiring => Age >= WarningDuration;
	private int ParentIndex => (int)Projectile.ai[0];
	private int SweepDirection => Projectile.ai[1] >= 0f ? 1 : -1;
	private bool IsDamageWindow => Age >= WarningDuration + 4 && Age < WarningDuration + BeamDuration - 8;

	public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.LostSoulHostile}";

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.DrawScreenCheckFluff[Type] = (int)BeamLength + 200;
	}

	public override void SetDefaults()
	{
		Projectile.width = 2;
		Projectile.height = 2;
		Projectile.hostile = true;
		Projectile.penetrate = -1;
		Projectile.timeLeft = WarningDuration + BeamDuration;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.netImportant = true;
	}

	public override bool ShouldUpdatePosition() => false;
	public override bool? CanDamage() => IsDamageWindow ? null : false;
	public override bool CanHitPlayer(Player target) => !hitPlayers[target.whoAmI];

	public override void AI()
	{
		NPC parent = ParentIndex >= 0 && ParentIndex < Main.maxNPCs ? Main.npc[ParentIndex] : null;
		if (parent is null || !parent.active || parent.ModNPC is not SealedCongregationBoss)
		{
			Projectile.Kill();
			return;
		}

		Projectile.Center = parent.Center;
		Projectile.rotation = CurrentDirection().ToRotation();
		Lighting.AddLight(Projectile.Center, new Vector3(0.1f, 0.55f, 0.48f) * (IsFiring ? 1f : 0.45f));

		if (Age == 0 && Main.netMode != NetmodeID.Server)
		{
			SoundEngine.PlaySound(SoundID.Item29 with { Pitch = 0.22f, Volume = 0.72f }, Projectile.Center);
		}
		if (Age == WarningDuration && Main.netMode != NetmodeID.Server)
		{
			SoundEngine.PlaySound(SoundID.Item74 with { Pitch = -0.52f, Volume = 1f }, Projectile.Center);
			SoundEngine.PlaySound(SoundID.NPCDeath6 with { Pitch = 0.18f, Volume = 0.72f }, Projectile.Center);
			CongregationCameraSystem.AddShake(Projectile.Center, 8f);
			SpawnIgnitionBurst();
		}
		else if (IsFiring && Age % 38 == 0 && Main.netMode != NetmodeID.Server)
		{
			SoundEngine.PlaySound(SoundID.Item15 with { Pitch = -0.72f, Volume = 0.34f }, Projectile.Center);
		}

		if (IsFiring && Main.netMode != NetmodeID.Server)
		{
			float fade = 1f - BeamProgress();
			CongregationCameraSystem.AddShake(Projectile.Center, 0.55f + fade * 0.7f);
			if (Age % 2 == 0)
			{
				SpawnBeamDust();
			}
		}
	}

	public override bool? Colliding(Rectangle projectileHitbox, Rectangle targetHitbox)
	{
		Vector2 direction = CurrentDirection();
		Vector2 start = Projectile.Center + direction * 46f;
		Vector2 end = start + direction * BeamLength;
		float collisionPoint = 0f;
		return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end,
			CollisionWidth, ref collisionPoint);
	}

	public override void OnHitPlayer(Player target, Player.HurtInfo info)
	{
		// Each cast may punish a missed dodge only once per player.
		hitPlayers[target.whoAmI] = true;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Vector2 direction = CurrentDirection();
		Vector2 start = Projectile.Center + direction * 42f;
		Vector2 end = start + direction * BeamLength;
		if (!IsFiring)
		{
			DrawWarning(start, end, direction);
			DrawChargeRings();
			return false;
		}

		DrawBeam(start, direction);
		return false;
	}

	private Vector2 CurrentDirection()
	{
		float rotation = Projectile.velocity.ToRotation();
		if (IsFiring)
		{
			rotation += SweepAngle * SweepDirection * EaseInOut(BeamProgress());
		}

		return rotation.ToRotationVector2();
	}

	private float BeamProgress()
	{
		return MathHelper.Clamp((Age - WarningDuration) / (float)BeamDuration, 0f, 1f);
	}

	private void DrawWarning(Vector2 start, Vector2 end, Vector2 direction)
	{
		float progress = MathHelper.Clamp(Age / (float)WarningDuration, 0f, 1f);
		float pulse = 0.65f + 0.35f * MathF.Sin(Age * 0.34f);
		DrawShaderBeam(start, direction, 10f + pulse * 3f, 0.62f + progress * 0.38f, 2f);

		Texture2D glow = SoulOrbProjectile.GetGlowTexture();
		Vector2 origin = glow.Size() * 0.5f;
		for (int mote = 0; mote < 9; mote++)
		{
			float along = (Age * 0.018f + mote / 9f) % 1f;
			Vector2 position = Vector2.Lerp(start, end, along) - Main.screenPosition;
			float scale = 0.08f + 0.08f * (1f - along);
			Main.EntitySpriteDraw(glow, position, null, new Color(154, 255, 233, 0) * (0.35f + pulse * 0.25f),
				direction.ToRotation(), origin, new Vector2(scale * 1.8f, scale), SpriteEffects.None);
		}
	}

	private void DrawChargeRings()
	{
		Texture2D ring = SoulOrbProjectile.GetRingTexture();
		Vector2 origin = ring.Size() * 0.5f;
		Vector2 center = Projectile.Center - Main.screenPosition;
		float progress = MathHelper.Clamp(Age / (float)WarningDuration, 0f, 1f);
		for (int index = 0; index < 3; index++)
		{
			float cycle = (progress + index / 3f) % 1f;
			float scale = MathHelper.Lerp(2.4f, 0.58f, cycle);
			Main.EntitySpriteDraw(ring, center, null, new Color(89, 245, 219, 0) * (cycle * 0.56f),
				0f, origin, scale, SpriteEffects.None);
		}
	}

	private void DrawBeam(Vector2 start, Vector2 direction)
	{
		float progress = BeamProgress();
		float ignition = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(progress / 0.07f, 0f, 1f));
		float fade = MathHelper.SmoothStep(1f, 0f, MathHelper.Clamp((progress - 0.92f) / 0.08f, 0f, 1f));
		float strength = ignition * fade;
		float pulse = 1f + 0.055f * MathF.Sin(Age * 0.31f);
		float shaderTime = Age / 60f;
		float shaderSeed = Projectile.identity * 0.173f;

		DrawShaderBeamLayers(start, direction, shaderTime, shaderSeed, strength, pulse);

		Vector2 perpendicular = direction.RotatedBy(MathHelper.PiOver2);
		DrawBeamWisps(start, direction, perpendicular, strength);
		DrawSourceFlare(start, direction, strength, pulse);
	}

	private void DrawShaderBeamLayers(Vector2 start, Vector2 direction, float shaderTime, float shaderSeed,
		float strength, float pulse)
	{
		Effect effect = CongregationShaderSystem.GetBeamEffect();
		if (effect is null)
		{
			return;
		}

		BeginBeamBatch(effect);
		DrawShaderBeamPrimitive(start, direction, 156f * pulse, shaderTime, strength * 0.72f, shaderSeed, 1f);
		DrawShaderBeamPrimitive(start, direction, 94f * pulse, shaderTime, strength, shaderSeed, 0f);
		EndBeamBatch();
	}

	private void DrawShaderBeam(Vector2 start, Vector2 direction, float width, float intensity, float mode)
	{
		Effect effect = CongregationShaderSystem.GetBeamEffect();
		if (effect is null)
		{
			return;
		}

		BeginBeamBatch(effect);
		DrawShaderBeamPrimitive(start, direction, width, Age / 60f, intensity,
			Projectile.identity * 0.173f, mode);
		EndBeamBatch();
	}

	private static void DrawShaderBeamPrimitive(Vector2 start, Vector2 direction, float width, float time,
		float intensity, float seed, float mode)
	{
		if (!CongregationShaderSystem.ApplyBeam(time, intensity, seed, mode))
		{
			return;
		}

		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Main.spriteBatch.Draw(pixel, start - Main.screenPosition, null, Color.White, direction.ToRotation(),
			new Vector2(0f, pixel.Height * 0.5f), new Vector2(BeamLength / pixel.Width, width / pixel.Height),
			SpriteEffects.None, 0f);
	}

	private static void BeginBeamBatch(Effect effect)
	{
		Main.spriteBatch.End();
		Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap,
			DepthStencilState.None, Main.Rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);
	}

	private static void EndBeamBatch()
	{
		Main.spriteBatch.End();
		Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
			DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
	}

	private void DrawBeamWisps(Vector2 start, Vector2 direction, Vector2 perpendicular, float strength)
	{
		Texture2D glow = SoulOrbProjectile.GetGlowTexture();
		Vector2 origin = glow.Size() * 0.5f;
		for (int wisp = 0; wisp < 11; wisp++)
		{
			float along = (Age * 0.009f + wisp / 11f) % 1f;
			float side = MathF.Sin(Age * 0.13f + wisp * 2.1f) * 27f;
			Vector2 position = start + direction * (along * BeamLength) + perpendicular * side - Main.screenPosition;
			float scale = 0.14f + 0.055f * MathF.Sin(wisp + Age * 0.08f);
			Main.EntitySpriteDraw(glow, position, null, new Color(190, 255, 235, 0) * (strength * 0.34f),
				direction.ToRotation(), origin, new Vector2(scale * 2.4f, scale), SpriteEffects.None);
		}
	}

	private static void DrawSourceFlare(Vector2 start, Vector2 direction, float strength, float pulse)
	{
		Texture2D glow = SoulOrbProjectile.GetGlowTexture();
		Texture2D ring = SoulOrbProjectile.GetRingTexture();
		Vector2 glowOrigin = glow.Size() * 0.5f;
		Vector2 ringOrigin = ring.Size() * 0.5f;
		Vector2 position = start - Main.screenPosition;
		Main.EntitySpriteDraw(glow, position, null, new Color(76, 238, 213, 0) * (strength * 0.58f),
			direction.ToRotation(), glowOrigin, new Vector2(1.7f, 0.8f) * pulse, SpriteEffects.None);
		Main.EntitySpriteDraw(glow, position, null, new Color(226, 255, 247, 0) * (strength * 0.76f),
			0f, glowOrigin, 0.72f * pulse, SpriteEffects.None);
		Main.EntitySpriteDraw(ring, position, null, new Color(132, 255, 229, 0) * (strength * 0.68f),
			AgeRotation(), ringOrigin, 0.94f * pulse, SpriteEffects.None);
	}

	private static float AgeRotation()
	{
		return Main.GlobalTimeWrappedHourly * 1.8f;
	}

	private void SpawnIgnitionBurst()
	{
		for (int index = 0; index < 38; index++)
		{
			Vector2 velocity = Main.rand.NextVector2Circular(8f, 8f);
			Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.DungeonSpirit, velocity, 80,
				new Color(155, 255, 232), Main.rand.NextFloat(1f, 1.65f));
			dust.noGravity = true;
		}
	}

	private void SpawnBeamDust()
	{
		Vector2 direction = CurrentDirection();
		Vector2 perpendicular = direction.RotatedBy(MathHelper.PiOver2);
		float distance = Main.rand.NextFloat(80f, Math.Min(BeamLength, Main.screenWidth * 1.35f));
		Vector2 position = Projectile.Center + direction * distance + perpendicular * Main.rand.NextFloat(-34f, 34f);
		Dust dust = Dust.NewDustPerfect(position, DustID.DungeonSpirit,
			perpendicular * Main.rand.NextFloat(-1.8f, 1.8f), 100, new Color(91, 236, 215), Main.rand.NextFloat(0.65f, 1.05f));
		dust.noGravity = true;
	}

	private static float EaseInOut(float progress)
	{
		return progress * progress * (3f - 2f * progress);
	}
}
