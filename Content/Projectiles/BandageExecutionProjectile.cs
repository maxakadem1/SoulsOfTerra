using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class BandageExecutionProjectile : ModProjectile
{
	public const int ExecutionDuration = 36;
	private const int PullStart = 8;
	private const int SwingStart = 16;
	private const int SwingEnd = 29;
	private const float SwingReach = 126f;

	private int Age => ExecutionDuration - Projectile.timeLeft;
	private float AimAngle => Projectile.velocity.ToRotation();

	public override string Texture => $"Terraria/Images/Item_{ItemID.BreakerBlade}";

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.TrailCacheLength[Type] = 10;
		ProjectileID.Sets.TrailingMode[Type] = 2;
	}

	public override void SetDefaults()
	{
		Projectile.width = 220;
		Projectile.height = 220;
		Projectile.friendly = true;
		Projectile.DamageType = DamageClass.Melee;
		Projectile.penetrate = -1;
		Projectile.timeLeft = ExecutionDuration;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.ownerHitCheck = true;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = ExecutionDuration;
	}

	public override bool ShouldUpdatePosition() => false;

	public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
		List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
	{
		// The execution blade must remain readable when its arc crosses a large enemy sprite.
		overPlayers.Add(index);
	}

	public override void AI()
	{
		Player player = Main.player[Projectile.owner];
		if (!player.active || player.dead)
		{
			Projectile.Kill();
			return;
		}

		int direction = Projectile.velocity.X >= 0f ? 1 : -1;
		player.ChangeDir(direction);
		float swordAngle = GetSwordAngle(Age, direction);
		Projectile.Center = GetHandPosition(player, swordAngle);
		player.heldProj = Projectile.whoAmI;
		player.itemTime = 2;
		player.itemAnimation = 2;
		Projectile.rotation = swordAngle;
		player.itemRotation = MathHelper.WrapAngle(swordAngle - (direction < 0 ? MathHelper.Pi : 0f));
		player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, swordAngle - MathHelper.PiOver2);

		if (Age < SwingStart)
		{
			player.velocity.X *= 0.82f;
		}

		if (Age == 0 && Main.netMode != NetmodeID.Server)
		{
			SoundEngine.PlaySound(SoundID.Item37 with { Volume = 0.55f, Pitch = -0.45f }, player.Center);
		}
		if (Age == PullStart)
		{
			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				SoulBandageTetherProjectile.ActivateNetwork(Projectile);
			}
			if (Main.netMode != NetmodeID.Server)
			{
				SoundEngine.PlaySound(SoundID.Item16 with { Volume = 0.75f, Pitch = -0.35f }, player.Center);
			}
		}
		if (Age == SwingStart && Main.netMode != NetmodeID.Server)
		{
			SoundEngine.PlaySound(SoundID.Item1 with { Volume = 1f, Pitch = -0.38f }, player.Center);
			SoundEngine.PlaySound(SoundID.Item7 with { Volume = 0.42f, Pitch = 0.15f }, player.Center);
			CreateSwingBurst(player, swordAngle);
		}
	}

	public override bool? CanDamage() => Age >= SwingStart && Age <= SwingEnd ? null : false;

	public override bool? Colliding(Rectangle projectileHitbox, Rectangle targetHitbox)
	{
		Player player = Main.player[Projectile.owner];
		float angle = GetSwordAngle(Age, player.direction);
		Vector2 start = GetHandPosition(player, angle);
		Vector2 end = start + angle.ToRotationVector2() * SwingReach;
		float collisionPoint = 0f;
		return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 48f, ref collisionPoint);
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Player player = Main.player[Projectile.owner];
		Texture2D blade = TextureAssets.Item[ItemID.BreakerBlade].Value;
		int direction = Projectile.velocity.X >= 0f ? 1 : -1;
		float currentAngle = GetSwordAngle(Age, direction);
		Vector2 handPosition = GetHandPosition(player, currentAngle);

		// The cached rotations are real prior blade poses, keeping the trail attached to the animation.
		if (Age >= SwingStart)
		{
			for (int ghost = Projectile.oldRot.Length - 1; ghost >= 1; ghost--)
			{
				if (ghost > Age - SwingStart + 1)
				{
					continue;
				}

				float ghostAngle = Projectile.oldRot[ghost];
				float strength = 1f - ghost / (float)Projectile.oldRot.Length;
				DrawBlade(Main.spriteBatch, blade, handPosition, ghostAngle, direction,
					new Color(168, 235, 220) * (0.12f + strength * 0.42f));
			}
		}

		DrawBlade(Main.spriteBatch, blade, handPosition, currentAngle, direction, Color.White);
		return false;
	}

	private float GetSwordAngle(int age, int direction)
	{
		float startAngle = AimAngle - direction * 2.15f;
		float endAngle = AimAngle + direction * 1.05f;
		if (age < PullStart)
		{
			return MathHelper.Lerp(AimAngle - direction * 0.65f, startAngle, EaseInOut(age / (float)PullStart));
		}
		if (age < SwingStart)
		{
			return startAngle + MathF.Sin((age - PullStart) / (float)(SwingStart - PullStart) * MathHelper.Pi) * direction * 0.08f;
		}
		if (age <= SwingEnd)
		{
			float progress = (age - SwingStart) / (float)(SwingEnd - SwingStart);
			return MathHelper.Lerp(startAngle, endAngle, EaseOutCubic(progress));
		}

		return endAngle;
	}

	private static void DrawBlade(SpriteBatch spriteBatch, Texture2D texture, Vector2 handPosition,
		float bladeAngle, int direction, Color color)
	{
		// Breaker Blade points northeast in its source sprite, with its grip near the lower-left corner.
		Vector2 gripOrigin = direction > 0
			? new Vector2(texture.Width * 0.1f, texture.Height * 0.9f)
			: new Vector2(texture.Width * 0.1f, texture.Height * 0.1f);
		SpriteEffects effects = direction > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically;
		float textureRotation = bladeAngle + direction * MathHelper.PiOver4;
		spriteBatch.Draw(texture, handPosition - Main.screenPosition, null, color, textureRotation,
			gripOrigin, 1.2f, effects, 0f);
	}

	private static Vector2 GetHandPosition(Player player, float swordAngle)
	{
		float armRotation = swordAngle - MathHelper.PiOver2;
		Vector2 handPosition = player.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, armRotation);
		handPosition.Y += player.gfxOffY;
		return handPosition;
	}

	private static void CreateSwingBurst(Player player, float angle)
	{
		Vector2 direction = angle.ToRotationVector2();
		for (int index = 0; index < 18; index++)
		{
			Vector2 position = player.MountedCenter + direction * Main.rand.NextFloat(28f, SwingReach);
			Dust dust = Dust.NewDustPerfect(position, DustID.Web,
				direction.RotatedByRandom(0.45f) * Main.rand.NextFloat(1.5f, 5f), 90,
				new Color(205, 190, 164), Main.rand.NextFloat(0.65f, 1.05f));
			dust.noGravity = true;
		}
	}

	private static float EaseInOut(float progress) => progress * progress * (3f - 2f * progress);
	private static float EaseOutCubic(float progress) => 1f - MathF.Pow(1f - progress, 3f);
}
