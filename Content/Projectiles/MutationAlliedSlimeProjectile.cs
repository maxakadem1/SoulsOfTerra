using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Content.Buffs;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public sealed class MutationAlliedSlimeProjectile : ModProjectile
{
	private const float DetectionRange = 600f;

	public override string Texture => $"Terraria/Images/NPC_{NPCID.BlueSlime}";

	public override void SetStaticDefaults()
	{
		Main.projFrames[Type] = Main.npcFrameCount[NPCID.BlueSlime];
		ProjectileID.Sets.MinionTargettingFeature[Type] = true;
	}

	public override void SetDefaults()
	{
		Projectile.width = 28;
		Projectile.height = 20;
		Projectile.friendly = true;
		Projectile.penetrate = -1;
		Projectile.timeLeft = 15 * 60;
		Projectile.tileCollide = true;
		Projectile.ignoreWater = false;
		Projectile.DamageType = DamageClass.Generic;
		Projectile.minion = true;
		Projectile.minionSlots = 1f;
		Projectile.netImportant = true;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = 30;
	}

	public override void AI()
	{
		Player owner = Main.player[Projectile.owner];
		if (!owner.active || owner.dead)
		{
			Projectile.Kill();
			return;
		}

		NPC target = FindTarget(owner);
		RefreshDamage(owner);
		// Ground acceleration and timed hops imitate ordinary slime pursuit.
		float targetX = target?.Center.X ?? owner.Center.X + owner.direction * 28f;
		float direction = System.Math.Sign(targetX - Projectile.Center.X);
		Projectile.velocity.X = MathHelper.Clamp(Projectile.velocity.X + direction * 0.09f, -2.4f, 2.4f);
		Projectile.velocity.Y = System.Math.Min(10f, Projectile.velocity.Y + 0.35f);

		bool grounded = Collision.SolidCollision(Projectile.BottomLeft + new Vector2(2f, 0f),
			Projectile.width - 4, 4);
		if (grounded && Projectile.velocity.Y >= 0f)
		{
			float distance = System.Math.Abs(targetX - Projectile.Center.X);
			Projectile.velocity.Y = distance > 180f ? -6.2f : -4.8f;
		}

		if (target is null && Vector2.DistanceSquared(Projectile.Center, owner.Center) > 900f * 900f)
		{
			Projectile.Center = owner.Center;
			Projectile.velocity = Vector2.Zero;
			Projectile.netUpdate = true;
		}

		Projectile.spriteDirection = Projectile.velocity.X < 0f ? -1 : 1;
		UpdateFrames(grounded);
	}

	public override bool OnTileCollide(Vector2 oldVelocity)
	{
		if (Projectile.velocity.X != oldVelocity.X)
		{
			Projectile.velocity.X = 0f;
		}
		if (Projectile.velocity.Y != oldVelocity.Y && oldVelocity.Y > 0f)
		{
			Projectile.velocity.Y = 0f;
		}
		return false;
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		target.AddBuff(ModContent.BuffType<MutationSlimeCoatingBuff>(), 5 * 60);
		Projectile.velocity.X *= -0.65f;
		Projectile.velocity.Y = -3.5f;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D texture = TextureAssets.Npc[NPCID.BlueSlime].Value;
		Rectangle frame = texture.Frame(1, Main.npcFrameCount[NPCID.BlueSlime], 0, Projectile.frame);
		SpriteEffects effects = Projectile.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, lightColor,
			Projectile.rotation, frame.Size() * 0.5f, 0.8f, effects);
		return false;
	}

	private NPC FindTarget(Player owner)
	{
		if (owner.HasMinionAttackTargetNPC)
		{
			NPC designated = Main.npc[owner.MinionAttackTargetNPC];
			if (designated.CanBeChasedBy(this) && Vector2.DistanceSquared(Projectile.Center, designated.Center)
				<= DetectionRange * DetectionRange)
			{
				return designated;
			}
		}

		NPC nearest = null;
		float nearestDistance = DetectionRange * DetectionRange;
		foreach (NPC npc in Main.ActiveNPCs)
		{
			float distance = Vector2.DistanceSquared(Projectile.Center, npc.Center);
			if (distance < nearestDistance && npc.CanBeChasedBy(this))
			{
				nearest = npc;
				nearestDistance = distance;
			}
		}
		return nearest;
	}

	private void RefreshDamage(Player owner)
	{
		int baseDamage = 6 + (int)(owner.statLifeMax2 * 0.04f);
		Projectile.damage = System.Math.Max(1, (int)owner.GetTotalDamage(DamageClass.Generic).ApplyTo(baseDamage));
	}

	private void UpdateFrames(bool grounded)
	{
		if (++Projectile.frameCounter < 8)
		{
			return;
		}

		Projectile.frameCounter = 0;
		Projectile.frame = grounded ? (Projectile.frame + 1) % Main.projFrames[Type] : 1 % Main.projFrames[Type];
	}
}
