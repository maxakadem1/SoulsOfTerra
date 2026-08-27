using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

/// <summary>
/// Reusable custom melee swing base with rising diagonal arc.
/// Swords derive from this to avoid vanilla overhead chop feel.
/// </summary>
public abstract class BaseCustomSwingProjectile : ModProjectile
{
	protected int Age => SwingDuration - Projectile.timeLeft;
	protected bool HasHitTarget => Projectile.ai[1] > 0f;
	protected int HitstopTimer => (int)Projectile.ai[1];

	// Swing timing - override in derived class
	protected abstract int SwingDuration { get; }
	protected abstract int WindupEnd { get; }
	protected abstract int SnapStart { get; }
	protected abstract int SnapEnd { get; }
	protected abstract float SwingReach { get; }
	protected abstract float CollisionWidth { get; }
	protected virtual int HitstopFrames => 3;
	protected virtual int TrailLength => 12;

	// Visual customization
	protected abstract float SwordScale { get; }
	protected abstract int SwordItemType { get; }
	protected virtual Vector2 GetGripOrigin(Texture2D texture, int direction)
	{
		return direction > 0
			? new Vector2(texture.Width * 0.15f, texture.Height * 0.85f)
			: new Vector2(texture.Width * 0.15f, texture.Height * 0.15f);
	}

	// Arc definition - horizontal sword swing centered on aim direction
	// Tight fan (~80-90°) that stays in front of player at chest height
	// Offsets are relative to cursor/aim direction
	protected virtual float GetWindupOffset() => -0.65f;     // Coil slightly above aim
	protected virtual float GetSnapOffset() => 0.75f;        // Follow through slightly below aim
	protected virtual float GetWindupSlowdown() => 0.75f;

	public override void SetStaticDefaults()
	{
		Terraria.ID.ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
		Terraria.ID.ProjectileID.Sets.TrailingMode[Type] = 2;
	}

	public override void SetDefaults()
	{
		Projectile.width = 160;
		Projectile.height = 160;
		Projectile.friendly = true;
		Projectile.DamageType = DamageClass.Melee;
		Projectile.penetrate = -1;
		Projectile.timeLeft = SwingDuration;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.ownerHitCheck = true;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = SwingDuration;
	}

	public override bool ShouldUpdatePosition() => false;

	public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
		List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
	{
		overPlayers.Add(index);
	}

	public sealed override void AI()
	{
		Player player = Main.player[Projectile.owner];
		if (!player.active || player.dead)
		{
			Projectile.Kill();
			return;
		}

		// Hitstop freeze
		if (HitstopTimer > 0)
		{
			Projectile.ai[1]--;
			Projectile.timeLeft++;
			OnHitstopTick(player);
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

		// Windup slows player
		if (Age < WindupEnd)
		{
			player.velocity.X *= GetWindupSlowdown();
		}

		OnSwingTick(player, Age, direction, swordAngle);
	}

	public override bool? CanDamage() => Age >= SnapStart && Age <= SnapEnd && HitstopTimer == 0 ? null : false;

	public override bool? Colliding(Rectangle projectileHitbox, Rectangle targetHitbox)
	{
		Player player = Main.player[Projectile.owner];
		float angle = GetSwordAngle(Age, player.direction);
		Vector2 start = GetHandPosition(player, angle);
		Vector2 end = start + angle.ToRotationVector2() * SwingReach;
		float collisionPoint = 0f;
		return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, CollisionWidth, ref collisionPoint);
	}

	public sealed override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
	{
		if (!HasHitTarget)
		{
			Projectile.ai[1] = HitstopFrames;
			OnFirstHit(target);
		}
		OnModifyHit(target, ref modifiers);
	}

	public sealed override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		OnImpact(target, hit, damageDone, HasHitTarget);
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Player player = Main.player[Projectile.owner];
		Texture2D blade = Terraria.GameContent.TextureAssets.Item[SwordItemType].Value;
		int direction = Projectile.velocity.X >= 0f ? 1 : -1;
		float currentAngle = GetSwordAngle(Age, direction);
		Vector2 handPosition = GetHandPosition(player, currentAngle);

		// Trail during snap phase
		if (Age >= SnapStart)
		{
			for (int ghost = Projectile.oldRot.Length - 1; ghost >= 1; ghost--)
			{
				if (ghost > Age - SnapStart + 1)
				{
					continue;
				}

				float ghostAngle = Projectile.oldRot[ghost];
				float strength = 1f - ghost / (float)Projectile.oldRot.Length;
				Color trailColor = GetTrailColor(strength);
				if (trailColor.A > 0)
				{
					DrawBlade(Main.spriteBatch, blade, handPosition, ghostAngle, direction, trailColor);
				}
			}
		}

		DrawBlade(Main.spriteBatch, blade, handPosition, currentAngle, direction, lightColor);
		return false;
	}

	// Customization hooks for derived projectiles
	protected virtual void OnSwingTick(Player player, int age, int direction, float swordAngle) { }
	protected virtual void OnHitstopTick(Player player) { }
	protected virtual void OnFirstHit(NPC target) { }
	protected virtual void OnModifyHit(NPC target, ref NPC.HitModifiers modifiers) { }
	protected abstract void OnImpact(NPC target, NPC.HitInfo hit, int damageDone, bool alreadyHit);
	protected abstract Color GetTrailColor(float strength);

	// Horizontal sword swing centered on cursor/aim direction
	// Tight fan that sweeps through the target, never overhead or underfoot
	private float GetSwordAngle(int age, int direction)
	{
		// Windup coil
		if (age < WindupEnd)
		{
			float windupProgress = age / (float)WindupEnd;
			float wobble = GetWindupWobble(windupProgress, direction);
			return AimAngle + GetWindupOffset() + wobble;
		}

		// Fast snap through target
		if (age < SnapEnd)
		{
			float snapProgress = (age - WindupEnd) / (float)(SnapEnd - WindupEnd);
			return MathHelper.Lerp(AimAngle + GetWindupOffset(), AimAngle + GetSnapOffset(), EaseOutCubic(snapProgress));
		}

		// Recovery hold
		return AimAngle + GetSnapOffset();
	}

	protected virtual float GetWindupWobble(float windupProgress, int direction) => 0f;

	private void DrawBlade(SpriteBatch spriteBatch, Texture2D texture, Vector2 handPosition,
		float bladeAngle, int direction, Color color)
	{
		Vector2 gripOrigin = GetGripOrigin(texture, direction);
		SpriteEffects effects = direction > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically;
		float textureRotation = bladeAngle + direction * MathHelper.PiOver4;
		spriteBatch.Draw(texture, handPosition - Main.screenPosition, null, color, textureRotation,
			gripOrigin, SwordScale, effects, 0f);
	}

	private Vector2 GetHandPosition(Player player, float swordAngle)
	{
		float armRotation = swordAngle - MathHelper.PiOver2;
		Vector2 handPosition = player.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, armRotation);
		handPosition.Y += player.gfxOffY;
		return handPosition;
	}

	protected static float EaseInOut(float progress) => progress * progress * (3f - 2f * progress);
	protected static float EaseOutCubic(float progress) => 1f - MathF.Pow(1f - progress, 3f);
}
