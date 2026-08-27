using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

/// <summary>
/// Reusable custom melee swing base. Sword snaps toward aim while slash hitbox
/// travels forward through enemies, not rotating around player like vanilla chop.
/// </summary>
public abstract class BaseCustomSwingProjectile : ModProjectile
{
	protected int Age => SwingDuration - Projectile.timeLeft;
	protected float AimAngle => Projectile.velocity.ToRotation();
	protected bool HasHitTarget => Projectile.ai[1] > 0f;
	protected int HitstopTimer => (int)Projectile.ai[1];

	// Swing timing - override in derived class
	protected abstract int SwingDuration { get; }
	protected abstract int WindupEnd { get; }
	protected abstract int SnapStart { get; }
	protected abstract int SnapEnd { get; }
	protected abstract float SlashReach { get; }        // How far the slash travels forward
	protected abstract float SlashWidth { get; }        // Width of the slash hitbox
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

	// Sword rotation - small snap around aim, NOT a wide arc
	protected virtual float GetWindupRotation() => -0.25f;   // Small pullback
	protected virtual float GetSnapRotation() => 0.15f;      // Small snap through

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
		float swordAngle = GetSwordAngle(Age);
		Projectile.Center = GetHandPosition(player, swordAngle);
		player.heldProj = Projectile.whoAmI;
		player.itemTime = 2;
		player.itemAnimation = 2;
		Projectile.rotation = swordAngle;
		player.itemRotation = MathHelper.WrapAngle(swordAngle - (direction < 0 ? MathHelper.Pi : 0f));
		player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, swordAngle - MathHelper.PiOver2);

		OnSwingTick(player, Age, direction, swordAngle);
	}

	public override bool? CanDamage() => Age >= SnapStart && Age <= SnapEnd && HitstopTimer == 0 ? null : false;

	public override bool? Colliding(Rectangle projectileHitbox, Rectangle targetHitbox)
	{
		// Slash travels forward along aim direction, not rotating around player
		if (Age < SnapStart || Age > SnapEnd) return false;

		Player player = Main.player[Projectile.owner];
		float slashProgress = (Age - SnapStart) / (float)(SnapEnd - SnapStart);
		
		// Slash starts near player and extends forward along aim
		Vector2 slashStart = player.MountedCenter + AimAngle.ToRotationVector2() * 20f;
		Vector2 slashEnd = slashStart + AimAngle.ToRotationVector2() * (SlashReach * slashProgress);
		
		float collisionPoint = 0f;
		return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), 
			slashStart, slashEnd, SlashWidth, ref collisionPoint);
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
		float currentAngle = GetSwordAngle(Age);
		Vector2 handPosition = GetHandPosition(player, currentAngle);

		// Draw slash trail during snap phase
		if (Age >= SnapStart && Age <= SnapEnd)
		{
			DrawSlashTrail(player, Main.spriteBatch);
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
	protected abstract void DrawSlashTrail(Player player, SpriteBatch spriteBatch);

	// Sword snaps toward aim with small rotation, NOT wide arc
	private float GetSwordAngle(int age)
	{
		// Windup - small pullback
		if (age < WindupEnd)
		{
			return AimAngle + GetWindupRotation();
		}

		// Snap - small rotation through aim
		if (age < SnapEnd)
		{
			float snapProgress = (age - WindupEnd) / (float)(SnapEnd - WindupEnd);
			return MathHelper.Lerp(AimAngle + GetWindupRotation(), AimAngle + GetSnapRotation(), EaseOutCubic(snapProgress));
		}

		// Recovery hold
		return AimAngle + GetSnapRotation();
	}

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

	protected static float EaseOutCubic(float progress) => 1f - MathF.Pow(1f - progress, 3f);
}
