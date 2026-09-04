using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SoulsOfTerra.Common;
using SoulsOfTerra.Players;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

// Grafted Skeletron hands. Velocity-driven like the vanilla boss: they never sit still, they are
// pulled toward a hover point by a damped spring, and attacks are momentum charges that overshoot.
public sealed class MutationSkeletronHandProjectile : ModProjectile
{
	public const int HandCount = 2;
	// The hands drag enemies inward instead of knocking them away, so vanilla knockback is suppressed.
	public const float Knockback = 0f;
	private const float PullStrength = 12f;
	private const float MinPullDistance = 32f;
	// Fraction of the remaining gap closed instantly per slap, and the cap on that haul.
	private const float PullFraction = 0.5f;
	private const float MaxPullStep = 15f * 16f;
	public const int BaseDamage = 25;
	public const float LifeDamageScale = 0.15f;

	// Hover pose: far enough out and low enough that a real length of bone arm is visible. Anything
	// closer and the hand sprite simply covers the whole arm.
	public const float HoverOffsetX = 96f;
	public const float HoverOffsetY = 38f;
	private const float ShoulderOffsetX = 10f;
	private const float ShoulderOffsetY = -6f;
	private const float BobAmplitude = 4f;
	private const float BobSpeed = 0.05f;

	// Spring toward the hover point, tuned just under critical damping: the hand still lags behind the
	// player but settles without oscillating.
	private const float HoverStiffness = 0.025f;
	private const float HoverDamping = 0.75f;

	private const float ChargeAccel = 2.2f;
	private const float ChargeDrag = 0.995f;
	private const float MaxChargeSpeed = 30f;
	// Long enough for a charge to cover the full reach from a standing start.
	private const int MaxChargeTicks = 44;
	private const int ChargeGraceTicks = 6;
	// Per-hand cooldown, plus the nudge given to the other hand after a swipe. Together these land a
	// strike roughly every 1.5 seconds, strictly alternating.
	private const int AttackInterval = 170;
	private const int SiblingDelay = 68;

	private const float Reach = 32f * 16f;
	// Soft leash ramps in past SoftReach; HardReach is the emergency clamp. Both sit beyond Reach so a
	// charge at maximum range is never fought by the leash.
	private const float SoftReach = 37f * 16f;
	private const float HardReach = 42f * 16f;
	private const float LeashAccel = 1.4f;

	private const float DamageSpeed = 6.5f;
	private const float SweepWidth = 32f;
	private const float SpriteScale = 0.65f;
	// Distance in source pixels from the sprite's top edge down to where the bone should meet the wrist.
	private const float WristInset = 6f;
	// Empty space left between consecutive vertebrae, in source pixels.
	private const float BoneGap = 8f;
	private const float RotationLerp = 0.25f;
	private const float OppositeSidePenalty = 1.35f;

	private const float StateHover = 0f;
	private const float StateCharge = 1f;

	private ref float Side => ref Projectile.ai[0];
	private ref float Timer => ref Projectile.ai[1];
	private ref float State => ref Projectile.ai[2];
	private ref float AimX => ref Projectile.localAI[0];
	private ref float AimY => ref Projectile.localAI[1];

	private Vector2 Aim => new(AimX, AimY);
	private bool IsRightHand => Side == 1f;
	private float SideSign => IsRightHand ? 1f : -1f;

	private Vector2 previousCenter;

	public override string Texture => $"Terraria/Images/NPC_{NPCID.SkeletronHand}";

	public override void SetStaticDefaults()
	{
		Main.projFrames[Type] = Math.Max(1, Main.npcFrameCount[NPCID.SkeletronHand]);
	}

	public override void SetDefaults()
	{
		Projectile.width = 36;
		Projectile.height = 36;
		Projectile.friendly = true;
		Projectile.hostile = false;
		Projectile.penetrate = -1;
		Projectile.timeLeft = 2;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.netImportant = true;
		Projectile.DamageType = DamageClass.Generic;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = 20;
	}

	public override bool? CanCutTiles() => false;

	public override bool CanHitPlayer(Player target) => false;

	// The hand is only dangerous while it is actually moving fast, not while it drifts on its leash.
	public override bool? CanDamage() => Projectile.velocity.LengthSquared() >= DamageSpeed * DamageSpeed;

	public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
	{
		if (projHitbox.Intersects(targetHitbox))
		{
			return true;
		}

		// Sweep the hand along its own motion so a fast charge cannot tunnel past an enemy.
		float collisionPoint = 0f;
		return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
			previousCenter, Projectile.Center, SweepWidth, ref collisionPoint);
	}

	public override bool? CanHitNPC(NPC target)
	{
		if (target.friendly || target.townNPC)
		{
			return false;
		}

		return null;
	}

	public override void AI()
	{
		Player owner = Main.player[Projectile.owner];
		if (!owner.active || owner.dead || !owner.GetModPlayer<MutationPlayer>().HasActive(MutationId.Skeletron))
		{
			Projectile.Kill();
			return;
		}

		previousCenter = Projectile.Center;
		Projectile.timeLeft = 2;
		RefreshDamage(owner);

		if (State == StateCharge)
		{
			UpdateCharge();
		}
		else
		{
			UpdateHover(owner);
		}

		ApplyLeash(owner);
		UpdateRotation(owner);
		Projectile.frame = State == StateCharge ? AttackFrame() : 0;
		if (Projectile.velocity.LengthSquared() > DamageSpeed * DamageSpeed)
		{
			SpawnBoneDust();
		}
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		SoundEngine.PlaySound(SoundID.NPCHit2, Projectile.Center);
		PullTowardOwner(target);
	}

	// Yank the victim back toward the player. Weight is respected through knockBackResist, so bosses and
	// other knockback-immune enemies are unaffected.
	private void PullTowardOwner(NPC target)
	{
		Player owner = Main.player[Projectile.owner];
		if (!owner.active || target.boss || target.knockBackResist <= 0f)
		{
			return;
		}

		Vector2 toOwner = owner.MountedCenter - target.Center;
		float distance = toOwner.Length();
		if (distance < MinPullDistance)
		{
			return;
		}

		Vector2 direction = toOwner / distance;
		target.velocity += direction * PullStrength * target.knockBackResist;

		// Most enemy AI rewrites its own velocity every tick, so an impulse alone barely moves grounded
		// foes. Close part of the gap directly, but never drag anything through terrain.
		float haul = Math.Min(distance - MinPullDistance, MaxPullStep) * PullFraction * target.knockBackResist;
		Vector2 hauled = target.position + direction * haul;
		if (Collision.CanHitLine(target.position, target.width, target.height, hauled, target.width, target.height))
		{
			target.position = hauled;
		}

		target.netUpdate = true;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Player owner = Main.player[Projectile.owner];
		if (!owner.active)
		{
			return false;
		}

		Main.instance.LoadNPC(NPCID.SkeletronHand);
		Texture2D texture = TextureAssets.Npc[NPCID.SkeletronHand].Value;
		int frames = Math.Max(1, Main.projFrames[Type]);
		Rectangle frame = texture.Frame(1, frames, 0, Math.Clamp(Projectile.frame, 0, frames - 1));
		SpriteEffects effects = IsRightHand ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

		// Anchor the chain to the wrist of the sprite as actually drawn, so the bones always meet the hand.
		Vector2 fingerDirection = (Projectile.rotation + MathHelper.PiOver2).ToRotationVector2();
		Vector2 wrist = Projectile.Center - fingerDirection * (frame.Height * 0.5f - WristInset) * SpriteScale;
		DrawBoneArm(GetShoulder(owner), wrist);

		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, lightColor,
			Projectile.rotation, frame.Size() * 0.5f, SpriteScale, effects);
		return false;
	}

	private void UpdateHover(Player owner)
	{
		Vector2 toHover = GetHoverPosition(owner) - Projectile.Center;
		Projectile.velocity += toHover * HoverStiffness;
		Projectile.velocity *= HoverDamping;

		if (Timer > 0f)
		{
			Timer--;
			return;
		}

		TryStartCharge(owner);
	}

	private void TryStartCharge(Player owner)
	{
		// Only one hand may be committed at a time.
		if (TryGetSibling(out Projectile sibling) && sibling.ai[2] == StateCharge)
		{
			Timer = SiblingDelay;
			return;
		}

		NPC target = FindTarget(owner);
		if (target is null)
		{
			return;
		}

		AimX = target.Center.X;
		AimY = target.Center.Y;
		State = StateCharge;
		Timer = 0f;
		Projectile.ResetLocalNPCHitImmunity();
		Projectile.netUpdate = true;
		SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.45f, PitchVariance = 0.2f }, Projectile.Center);
	}

	private void UpdateCharge()
	{
		Timer++;
		Vector2 toAim = Aim - Projectile.Center;
		Projectile.velocity += toAim.SafeNormalize(Vector2.UnitX * SideSign) * ChargeAccel;
		Projectile.velocity *= ChargeDrag;
		if (Projectile.velocity.LengthSquared() > MaxChargeSpeed * MaxChargeSpeed)
		{
			Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.Zero) * MaxChargeSpeed;
		}

		// End on timeout or the moment the hand blows past its aim point; momentum carries the overshoot
		// and the hover spring reels it back in.
		// The grace period stops a hand that launched while drifting backwards from cancelling instantly.
		bool passed = Timer > ChargeGraceTicks && Vector2.Dot(toAim, Projectile.velocity) <= 0f;
		if (Timer < MaxChargeTicks && !passed)
		{
			return;
		}

		State = StateHover;
		Timer = AttackInterval;
		if (TryGetSibling(out Projectile sibling))
		{
			sibling.ai[1] = Math.Max(sibling.ai[1], SiblingDelay);
		}

		Projectile.netUpdate = true;
	}

	private void ApplyLeash(Player owner)
	{
		Vector2 shoulder = GetShoulder(owner);
		Vector2 fromShoulder = Projectile.Center - shoulder;
		float distance = fromShoulder.Length();
		if (distance <= SoftReach || distance < 1f)
		{
			return;
		}

		Vector2 outward = fromShoulder / distance;
		float strain = MathHelper.Clamp((distance - SoftReach) / (HardReach - SoftReach), 0f, 1f);
		Projectile.velocity -= outward * LeashAccel * strain;

		if (distance <= HardReach)
		{
			return;
		}

		Projectile.Center = shoulder + outward * HardReach;
		float outwardSpeed = Vector2.Dot(Projectile.velocity, outward);
		if (outwardSpeed > 0f)
		{
			Projectile.velocity -= outward * outwardSpeed;
		}
	}

	private void UpdateRotation(Player owner)
	{
		float target = GetFingerRotation(Projectile.Center - GetShoulder(owner));
		Projectile.rotation += MathHelper.WrapAngle(target - Projectile.rotation) * RotationLerp;
	}

	private NPC FindTarget(Player owner)
	{
		NPC chosen = null;
		float bestScore = float.MaxValue;

		foreach (NPC npc in Main.ActiveNPCs)
		{
			if (!npc.CanBeChasedBy(this)
				|| Vector2.DistanceSquared(npc.Center, owner.MountedCenter) > Reach * Reach)
			{
				continue;
			}

			float score = Vector2.Distance(npc.Center, owner.MountedCenter);
			// Each hand favours its own world side but will still reach across the body.
			if (Math.Sign(npc.Center.X - owner.MountedCenter.X) == -Math.Sign(SideSign))
			{
				score *= OppositeSidePenalty;
			}

			if (score < bestScore)
			{
				bestScore = score;
				chosen = npc;
			}
		}

		return chosen;
	}

	private bool TryGetSibling(out Projectile sibling)
	{
		sibling = null;
		foreach (Projectile projectile in Main.ActiveProjectiles)
		{
			if (projectile.whoAmI != Projectile.whoAmI && projectile.type == Type
				&& projectile.owner == Projectile.owner)
			{
				sibling = projectile;
				return true;
			}
		}

		return false;
	}

	private Vector2 GetHoverPosition(Player owner)
	{
		float bob = MathF.Sin(Main.GameUpdateCount * BobSpeed + Side * 2.2f) * BobAmplitude;
		return GetShoulder(owner) + new Vector2(SideSign * HoverOffsetX, HoverOffsetY + bob);
	}

	// World-space sides: the hands never swap when the player turns around.
	public static Vector2 GetShoulder(Player owner, float sideSign) =>
		owner.MountedCenter + new Vector2(sideSign * ShoulderOffsetX, ShoulderOffsetY);

	private Vector2 GetShoulder(Player owner) => GetShoulder(owner, SideSign);

	// NPC_36 has the wrist at the top and fingers at the bottom; this aims the fingers along `fingerDirection`.
	private static float GetFingerRotation(Vector2 fingerDirection) =>
		fingerDirection.ToRotation() - MathHelper.PiOver2;

	private void RefreshDamage(Player owner)
	{
		int baseDamage = BaseDamage + (int)(owner.statLifeMax2 * LifeDamageScale);
		Projectile.damage = Math.Max(1, (int)owner.GetTotalDamage(DamageClass.Generic).ApplyTo(baseDamage));
		Projectile.knockBack = Knockback;
	}

	private int AttackFrame()
	{
		int frames = Math.Max(1, Main.projFrames[Type]);
		return frames > 1 ? 1 : 0;
	}

	private void SpawnBoneDust()
	{
		if (!Main.rand.NextBool(3))
		{
			return;
		}

		Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Bone,
			Projectile.velocity.X * 0.05f, Projectile.velocity.Y * 0.05f, 120, default, 0.7f);
		dust.noGravity = true;
	}

	private static void DrawBoneArm(Vector2 shoulder, Vector2 wrist)
	{
		Texture2D bone = GetBoneTexture();
		if (bone is null || bone.Height < 4)
		{
			return;
		}

		Vector2 toShoulder = shoulder - wrist;
		float length = toShoulder.Length();
		if (length < 4f)
		{
			return;
		}

		Vector2 direction = toShoulder / length;
		float rotation = direction.ToRotation() - MathHelper.PiOver2;
		Vector2 origin = bone.Size() * 0.5f;
		// Vanilla spaces the vertebrae apart rather than butting them together.
		float step = (bone.Height + BoneGap) * SpriteScale;
		// Walk outward from the wrist so the chain always starts flush with the hand; any leftover
		// slack overhangs the shoulder, where the player sprite hides it.
		for (float travelled = bone.Height * SpriteScale * 0.5f; travelled < length; travelled += step)
		{
			Vector2 position = wrist + direction * travelled;
			Color color = Lighting.GetColor(position.ToTileCoordinates());
			Main.EntitySpriteDraw(bone, position - Main.screenPosition, null, color, rotation, origin,
				SpriteScale, SpriteEffects.None);
		}
	}

	private static Texture2D GetBoneTexture()
	{
		Asset<Texture2D> asset = TextureAssets.BoneArm;
		if (asset is null || !asset.IsLoaded)
		{
			asset = Main.Assets.Request<Texture2D>("Images/Arm_Bone", AssetRequestMode.ImmediateLoad);
		}

		return asset?.Value;
	}
}
