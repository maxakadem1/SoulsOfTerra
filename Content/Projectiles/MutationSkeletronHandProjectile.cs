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

// Grafted Skeletron hands rest on the player's shoulders, then lunge and snap back.
public sealed class MutationSkeletronHandProjectile : ModProjectile
{
	public const int HandCount = 2;
	public const float Knockback = 2f;

	private const int OutboundDuration = 12;
	private const int ReturnDuration = 18;
	private const int CycleDuration = 72;
	private const int StaggerTicks = 36;
	private const int IdleWaitAfterAttack = CycleDuration - OutboundDuration - ReturnDuration;
	private const float RestDistance = 3f * 16f;
	private const float LungeDistance = 5f * 16f;
	private const float Reach = 8f * 16f;
	private const float OppositeSidePenalty = 1.35f;
	private const float SpriteScale = 0.5f;
	private const float IdleLerp = 0.18f;
	private const float StateIdle = 0f;
	private const float StateOutbound = 1f;
	private const float StateReturn = 2f;

	private ref float Side => ref Projectile.ai[0];
	private ref float Timer => ref Projectile.ai[1];
	private ref float State => ref Projectile.ai[2];

	private bool IsRightHand => Side == 1f;
	private float SideSign => IsRightHand ? 1f : -1f;
	private static Asset<Texture2D> boneArmTexture;

	public override string Texture => $"Terraria/Images/NPC_{NPCID.SkeletronHand}";

	public override void SetStaticDefaults()
	{
		Main.projFrames[Type] = Math.Max(1, Main.npcFrameCount[NPCID.SkeletronHand]);
		boneArmTexture = Main.Assets.Request<Texture2D>("Images/Arm_Bone");
	}

	public override void Unload()
	{
		boneArmTexture = null;
	}

	public override void SetDefaults()
	{
		Projectile.width = 40;
		Projectile.height = 40;
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

	public override bool ShouldUpdatePosition() => false;

	public override bool? CanCutTiles() => false;

	public override bool CanHitPlayer(Player target) => false;

	public override bool? CanDamage() => State == StateOutbound;

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

		Projectile.timeLeft = 2;
		RefreshDamage(owner);

		if (State == StateOutbound)
		{
			UpdateOutbound(owner);
			return;
		}

		if (State == StateReturn)
		{
			UpdateReturn(owner);
			return;
		}

		FollowRest(owner, bob: true);
		UpdateIdleFrame();
		if (Timer > 0f)
		{
			Timer--;
			return;
		}

		TryStartLunge(owner);
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		SoundEngine.PlaySound(SoundID.NPCHit2, Projectile.Center);
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
		Vector2 shoulder = GetShoulder(owner);
		Vector2 toShoulder = (shoulder - Projectile.Center).SafeNormalize(Vector2.Zero);
		// Wrist sits on the player-facing end so BoneArm meets bone, not palm.
		Vector2 wrist = Projectile.Center + toShoulder * (frame.Height * SpriteScale * 0.22f);
		DrawBoneArm(wrist, shoulder);

		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, lightColor,
			Projectile.rotation, frame.Size() * 0.5f, SpriteScale, SpriteEffects.None);
		return false;
	}

	private void TryStartLunge(Player owner)
	{
		NPC target = FindTarget(owner);
		if (target is null)
		{
			return;
		}

		if (TryGetSibling(out Projectile sibling))
		{
			int siblingAge = GetAttackAge(sibling);
			if (siblingAge >= 0 && siblingAge < StaggerTicks)
			{
				Timer = StaggerTicks - siblingAge;
				return;
			}

			// When both hands are ready, the left hand leads so they do not slap together.
			if (siblingAge < 0 && sibling.ai[1] <= 0f && IsRightHand)
			{
				Timer = StaggerTicks;
				return;
			}
		}

		Vector2 rest = GetRestPosition(owner, bob: false);
		Vector2 toTarget = target.Center - rest;
		float distance = Math.Clamp(toTarget.Length(), 16f, LungeDistance);
		Projectile.velocity = toTarget.SafeNormalize(Vector2.UnitX * owner.direction) * distance;
		State = StateOutbound;
		Timer = 0f;
		Projectile.netUpdate = true;
		SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.45f, PitchVariance = 0.2f }, Projectile.Center);
		UpdateOutbound(owner);
	}

	private void UpdateOutbound(Player owner)
	{
		Timer++;
		float progress = MathHelper.Clamp(Timer / OutboundDuration, 0f, 1f);
		SetLungePose(owner, SmoothStep(progress), returning: false);
		Projectile.frame = AttackFrame();
		SpawnBoneDust();

		if (Timer >= OutboundDuration)
		{
			State = StateReturn;
			Timer = 0f;
			Projectile.netUpdate = true;
		}
	}

	private void UpdateReturn(Player owner)
	{
		Timer++;
		float progress = MathHelper.Clamp(Timer / ReturnDuration, 0f, 1f);
		SetLungePose(owner, SmoothStep(progress), returning: true);
		Projectile.frame = AttackFrame();

		if (Timer >= ReturnDuration)
		{
			State = StateIdle;
			Timer = IdleWaitAfterAttack;
			Projectile.velocity = Vector2.Zero;
			Projectile.netUpdate = true;
		}
	}

	private void SetLungePose(Player owner, float progress, bool returning)
	{
		Vector2 rest = GetRestPosition(owner, bob: false);
		Vector2 apex = rest + Projectile.velocity;
		Projectile.Center = Vector2.Lerp(returning ? apex : rest, returning ? rest : apex, progress);

		float restRotation = GetFingerRotation(Projectile.Center - GetShoulder(owner));
		float slapRotation = GetFingerRotation(Projectile.velocity);
		float from = returning ? slapRotation : restRotation;
		float to = returning ? restRotation : slapRotation;
		Projectile.rotation = from + MathHelper.WrapAngle(to - from) * progress;
	}

	private void FollowRest(Player owner, bool bob)
	{
		Vector2 rest = GetRestPosition(owner, bob);
		if (Vector2.DistanceSquared(Projectile.Center, rest) > 400f * 400f)
		{
			Projectile.Center = rest;
		}
		else
		{
			Projectile.Center = Vector2.Lerp(Projectile.Center, rest, IdleLerp);
		}

		Projectile.rotation = GetFingerRotation(Projectile.Center - GetShoulder(owner));
	}

	private NPC FindTarget(Player owner)
	{
		Vector2 rest = GetRestPosition(owner, bob: false);
		float lockRange = LungeDistance + 24f;
		int preferredSide = Math.Sign(owner.direction * SideSign);
		NPC chosen = null;
		float bestScore = float.MaxValue;

		foreach (NPC npc in Main.ActiveNPCs)
		{
			if (!npc.CanBeChasedBy(this)
				|| Vector2.DistanceSquared(npc.Center, owner.MountedCenter) > Reach * Reach
				|| Vector2.DistanceSquared(npc.Center, rest) > lockRange * lockRange)
			{
				continue;
			}

			float score = Vector2.Distance(npc.Center, owner.MountedCenter);
			int enemySide = Math.Sign(npc.Center.X - owner.MountedCenter.X);
			if (preferredSide != 0 && enemySide != 0 && enemySide != preferredSide)
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

	private Vector2 GetRestPosition(Player owner, bool bob)
	{
		float bobY = bob ? MathF.Sin(Main.GameUpdateCount * 0.09f + Side * 2.2f) * 5f : 0f;
		return owner.MountedCenter + new Vector2(owner.direction * SideSign * RestDistance, -8f + bobY);
	}

	private Vector2 GetShoulder(Player owner) =>
		owner.MountedCenter + new Vector2(owner.direction * SideSign * 8f, -10f);

	// NPC_36 has the wrist at the top and fingers at the bottom; this aims the fingers along `fingerDirection`.
	private static float GetFingerRotation(Vector2 fingerDirection) =>
		fingerDirection.ToRotation() - MathHelper.PiOver2;

	private void RefreshDamage(Player owner)
	{
		int baseDamage = 10 + (int)(owner.statLifeMax2 * 0.05f);
		Projectile.damage = Math.Max(1, (int)owner.GetTotalDamage(DamageClass.Generic).ApplyTo(baseDamage));
		Projectile.knockBack = Knockback;
	}

	private void UpdateIdleFrame() => Projectile.frame = 0;

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

	private static int GetAttackAge(Projectile projectile)
	{
		if (projectile.ai[2] == StateOutbound)
		{
			return (int)projectile.ai[1];
		}

		if (projectile.ai[2] == StateReturn)
		{
			return OutboundDuration + (int)projectile.ai[1];
		}

		return -1;
	}

	private static float SmoothStep(float amount)
	{
		amount = MathHelper.Clamp(amount, 0f, 1f);
		return amount * amount * (3f - 2f * amount);
	}

	private static void DrawBoneArm(Vector2 from, Vector2 to)
	{
		Asset<Texture2D> boneAsset = boneArmTexture is { IsLoaded: true } ? boneArmTexture : TextureAssets.BoneArm;
		if (boneAsset is null || !boneAsset.IsLoaded)
		{
			return;
		}

		Texture2D bone = boneAsset.Value;
		float step = bone.Height * SpriteScale;
		if (step < 4f)
		{
			return;
		}

		Vector2 delta = to - from;
		float length = delta.Length();
		if (length < step)
		{
			return;
		}

		Vector2 direction = delta / length;
		float rotation = direction.ToRotation() - MathHelper.PiOver2;
		Vector2 origin = bone.Size() * 0.5f;
		// Walk toward the shoulder one bone at a time; never step past the remaining length.
		int maxSegments = 8;
		for (int i = 0; i < maxSegments && length >= step; i++)
		{
			from += direction * step;
			length -= step;
			Color color = Lighting.GetColor(from.ToTileCoordinates());
			Main.EntitySpriteDraw(bone, from - Main.screenPosition, null, color, rotation, origin, SpriteScale,
				SpriteEffects.None);
		}
	}
}
