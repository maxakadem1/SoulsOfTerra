using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Content.Buffs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class CompeditusCoreProjectile : ModProjectile
{
	internal const int VerseDuration = 60;
	internal const int CycleDuration = 96;
	private const int JudgmentSpawnTick = 61;
	private const float TargetRange = 900f;

	public override string Texture => "Terraria/Images/MagicPixel";
	internal int CycleTimer => (int)Projectile.ai[0];
	internal int TargetIndex => (int)Projectile.ai[1] - 1;
	internal bool IsJudging => CycleTimer >= VerseDuration;

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.MinionSacrificable[Type] = false;
		ProjectileID.Sets.CultistIsResistantTo[Type] = true;
	}

	public override void SetDefaults()
	{
		Projectile.width = 20;
		Projectile.height = 20;
		Projectile.friendly = false;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.netImportant = true;
		Projectile.penetrate = -1;
		Projectile.timeLeft = 2;
	}

	public override bool? CanDamage() => false;

	public override void AI()
	{
		Player owner = Main.player[Projectile.owner];
		if (!owner.active || owner.dead)
		{
			owner.ClearBuff(ModContent.BuffType<CompeditusBuff>());
			Projectile.Kill();
			return;
		}

		List<Projectile> seals = GetOwnedSeals(Projectile.owner);
		if (!owner.HasBuff(ModContent.BuffType<CompeditusBuff>()) || seals.Count == 0)
		{
			Projectile.Kill();
			return;
		}

		Projectile.timeLeft = 2;
		NPC target = GetCurrentTarget();
		if (Projectile.owner == Main.myPlayer)
		{
			NPC selectedTarget = FindTarget(owner);
			int selectedIndex = selectedTarget?.whoAmI ?? -1;
			if (selectedIndex != TargetIndex)
			{
				Projectile.ai[1] = selectedIndex + 1;
				Projectile.ai[0] = 0f;
				Projectile.netUpdate = true;
			}
			target = selectedTarget;
		}

		MoveFormation(owner, target);
		Lighting.AddLight(Projectile.Center, new Vector3(0.04f, 0.34f, 0.31f));
		if (target is null)
		{
			Projectile.ai[0] = 0f;
			return;
		}

		int timer = CycleTimer;
		if (Projectile.owner == Main.myPlayer)
		{
			FireScheduledLance(seals, target, timer);
			if (timer == JudgmentSpawnTick)
			{
				SpawnJudgment(target, seals.Count);
			}
		}

		Projectile.ai[0] = (timer + 1) % CycleDuration;
		if (Projectile.ai[0] == 0f)
		{
			Projectile.netUpdate = true;
		}
	}

	private void MoveFormation(Player owner, NPC target)
	{
		Vector2 destination;
		if (target is null)
		{
			destination = owner.Center + new Vector2(-owner.direction * 54f, -68f);
		}
		else
		{
			Vector2 playerSide = (owner.Center - target.Center).SafeNormalize(-Vector2.UnitY);
			destination = target.Center + playerSide * 105f - Vector2.UnitY * 38f;
		}

		Vector2 offset = destination - Projectile.Center;
		float speed = target is null ? 12f : 16f;
		Vector2 desiredVelocity = offset.LengthSquared() > speed * speed
			? offset.SafeNormalize(Vector2.Zero) * speed
			: offset;
		Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, target is null ? 0.11f : 0.16f);
		if (Vector2.DistanceSquared(Projectile.Center, owner.Center) > 1_500f * 1_500f)
		{
			Projectile.Center = owner.Center;
			Projectile.velocity = Vector2.Zero;
			Projectile.netUpdate = true;
		}
	}

	private void FireScheduledLance(List<Projectile> seals, NPC target, int timer)
	{
		if (timer >= VerseDuration || seals.Count == 0)
		{
			return;
		}

		for (int index = 0; index < seals.Count; index++)
		{
			int fireTick = 8 + index * 44 / seals.Count;
			if (timer != fireTick)
			{
				continue;
			}

			Projectile seal = seals[index];
			if (!Collision.CanHitLine(seal.Center, 2, 2, target.Center, 2, 2))
			{
				return;
			}

			Vector2 velocity = (target.Center - seal.Center).SafeNormalize(Vector2.UnitX) * 12.5f;
			int lance = Projectile.NewProjectile(Projectile.GetSource_FromThis(), seal.Center, velocity,
				ModContent.ProjectileType<CompeditusLanceProjectile>(), Projectile.damage,
				Projectile.knockBack, Projectile.owner, target.whoAmI + 1);
			if (lance >= 0 && lance < Main.maxProjectiles)
			{
				Main.projectile[lance].originalDamage = Projectile.originalDamage;
			}
			return;
		}
	}

	private void SpawnJudgment(NPC target, int sealCount)
	{
		if (!Collision.CanHitLine(Projectile.Center, 2, 2, target.Center, 2, 2))
		{
			return;
		}

		float multiplier = 0.7f + sealCount * 0.15f;
		int damage = Math.Max(1, (int)MathF.Round(Projectile.damage * multiplier));
		Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero,
			ModContent.ProjectileType<CompeditusJudgmentProjectile>(), damage, Projectile.knockBack,
			Projectile.owner, target.whoAmI + 1, sealCount);
	}

	private NPC GetCurrentTarget()
	{
		int index = TargetIndex;
		return index >= 0 && index < Main.maxNPCs && IsValidTarget(Main.npc[index]) ? Main.npc[index] : null;
	}

	private static NPC FindTarget(Player owner)
	{
		if (owner.HasMinionAttackTargetNPC)
		{
			NPC designated = Main.npc[owner.MinionAttackTargetNPC];
			if (IsValidTarget(designated) && Vector2.DistanceSquared(owner.Center, designated.Center) <= 1_200f * 1_200f)
			{
				return designated;
			}
		}

		NPC nearest = null;
		float nearestDistanceSquared = TargetRange * TargetRange;
		foreach (NPC npc in Main.ActiveNPCs)
		{
			float distanceSquared = Vector2.DistanceSquared(owner.Center, npc.Center);
			if (!IsValidTarget(npc) || distanceSquared >= nearestDistanceSquared
				|| !Collision.CanHitLine(owner.Center, 2, 2, npc.Center, 2, 2))
			{
				continue;
			}

			nearest = npc;
			nearestDistanceSquared = distanceSquared;
		}
		return nearest;
	}

	private static bool IsValidTarget(NPC npc) => npc is not null && npc.active && npc.CanBeChasedBy();

	internal Vector2 GetSealDestination(int sealIndex, int sealCount)
	{
		float baseAngle = Main.GlobalTimeWrappedHourly * 0.9f;
		float angle = baseAngle + MathHelper.TwoPi * sealIndex / Math.Max(1, sealCount);
		float radius = GetFormationRadius();
		return Projectile.Center + angle.ToRotationVector2() * radius;
	}

	internal float GetFormationRadius()
	{
		if (!IsJudging)
		{
			return 50f;
		}

		float progress = MathHelper.Clamp((CycleTimer - VerseDuration) / (float)(CycleDuration - VerseDuration), 0f, 1f);
		if (progress < 0.2f)
		{
			return MathHelper.Lerp(50f, 80f, SmoothStep(progress / 0.2f));
		}
		if (progress < 0.28f)
		{
			return 80f;
		}
		if (progress < 0.53f)
		{
			return MathHelper.Lerp(80f, 16f, SmoothStep((progress - 0.28f) / 0.25f));
		}
		return MathHelper.Lerp(16f, 50f, SmoothStep((progress - 0.53f) / 0.47f));
	}

	private static float SmoothStep(float value) => value * value * (3f - 2f * value);

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D glow = SoulOrbProjectile.GetGlowTexture();
		Texture2D ring = SoulOrbProjectile.GetRingTexture();
		Vector2 position = Projectile.Center - Main.screenPosition;
		Vector2 origin = glow.Size() * 0.5f;
		float pulse = 1f + 0.08f * MathF.Sin(Main.GlobalTimeWrappedHourly * 5f);
		float judgmentStrength = IsJudging
			? MathF.Sin(MathHelper.Clamp((CycleTimer - VerseDuration) / 36f, 0f, 1f) * MathHelper.Pi)
			: 0f;

		Main.EntitySpriteDraw(glow, position, null, new Color(42, 238, 218, 0) * (0.42f + judgmentStrength * 0.35f),
			0f, origin, (0.34f + judgmentStrength * 0.16f) * pulse, SpriteEffects.None);
		Main.EntitySpriteDraw(ring, position, null, new Color(175, 255, 244, 0) * (0.8f + judgmentStrength * 0.2f),
			0f, origin, (0.2f + judgmentStrength * 0.18f) * pulse, SpriteEffects.None);
		return false;
	}

	internal static int FindOwnedCore(int owner)
	{
		int coreType = ModContent.ProjectileType<CompeditusCoreProjectile>();
		foreach (Projectile projectile in Main.ActiveProjectiles)
		{
			if (projectile.owner == owner && projectile.type == coreType)
			{
				return projectile.whoAmI;
			}
		}
		return -1;
	}

	internal static int CountOwnedSeals(int owner) => GetOwnedSeals(owner).Count;

	internal static List<Projectile> GetOwnedSeals(int owner)
	{
		int sealType = ModContent.ProjectileType<CompeditusSealMinionProjectile>();
		List<Projectile> seals = new(4);
		foreach (Projectile projectile in Main.ActiveProjectiles)
		{
			if (projectile.owner == owner && projectile.type == sealType)
			{
				seals.Add(projectile);
			}
		}
		seals.Sort((left, right) => left.identity.CompareTo(right.identity));
		return seals;
	}
}
