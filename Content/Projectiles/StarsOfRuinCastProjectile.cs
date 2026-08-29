using System;
using Microsoft.Xna.Framework;
using SoulsOfTerra.Content.Items.Weapons.Magic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class StarsOfRuinCastProjectile : ModProjectile
{
	public const int StarCount = 12;
	public const int ConjureDuration = 10;
	public const int SpawnInterval = 2;
	public const int RecoveryDuration = 24;
	public const int VerseDuration = ConjureDuration + SpawnInterval * (StarCount - 1) + RecoveryDuration;
	public const float StaffTipDistance = 34f;
	private const float CursorTargetRadius = 220f;
	private const float MaximumTargetRange = 800f;
	private const float WaveAmplitude = 0.55f;
	private static readonly int[] LaunchRanks = { 6, 7, 2, 3, 0, 1, 10, 11, 4, 5, 8, 9 };

	public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.PurificationPowder}";

	public override void SetDefaults()
	{
		Projectile.width = 2;
		Projectile.height = 2;
		Projectile.friendly = false;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.timeLeft = VerseDuration + 8;
		Projectile.netImportant = true;
	}

	public override bool ShouldUpdatePosition() => false;

	public override void AI()
	{
		Player player = Main.player[Projectile.owner];
		if (!player.active || player.dead || player.HeldItem.type != ModContent.ItemType<StarsOfRuin>())
		{
			Projectile.Kill();
			return;
		}

		Projectile.Center = player.MountedCenter;
		player.ChangeDir(Projectile.velocity.X >= 0f ? 1 : -1);
		player.heldProj = Projectile.whoAmI;
		player.itemTime = 2;
		player.itemAnimation = 2;
		float wave = MathF.Sin(Projectile.localAI[0] * 0.42f) * WaveAmplitude;
		player.itemRotation = (Projectile.velocity * player.direction).ToRotation() + wave;

		Projectile.localAI[0]++;
		int age = (int)Projectile.localAI[0];
		Lighting.AddLight(GetStaffTip(player), 0.18f, 0.5f, 0.62f);
		if (Main.netMode != NetmodeID.Server && age % 2 == 0)
		{
			Vector2 tip = GetStaffTip(player);
			Dust dust = Dust.NewDustPerfect(tip + Main.rand.NextVector2Circular(5f, 5f), DustID.BlueCrystalShard,
				Main.rand.NextVector2Circular(0.5f, 0.5f), 70, new Color(160, 230, 255), Main.rand.NextFloat(0.4f, 0.75f));
			dust.noGravity = true;
		}

		if (age == 1)
		{
			if (Main.netMode != NetmodeID.Server)
			{
				SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.62f, Pitch = 0.2f }, player.MountedCenter);
			}

			if (Projectile.owner == Main.myPlayer)
			{
				for (int index = 0; index < StarCount; index++)
				{
					SpawnStar(player, index);
				}
			}
		}

		if (age >= VerseDuration)
		{
			Projectile.Kill();
		}
	}

	public override bool PreDraw(ref Color lightColor) => false;

	internal static Vector2 GetStaffTip(Player player, Vector2 aim)
	{
		return player.MountedCenter + aim.SafeNormalize(new Vector2(player.direction, 0f)) * StaffTipDistance;
	}

	internal Vector2 GetStaffTip(Player player)
	{
		return GetStaffTip(player, Projectile.velocity);
	}

	internal static int GetLaunchRank(int index) => LaunchRanks[index];

	internal static int FindTarget(Vector2 cursor, Vector2 playerCenter)
	{
		// A cursor-radius lock substitutes for Elden Ring's explicit target lock.
		NPC bestTarget = null;
		float bestCursorDistanceSquared = CursorTargetRadius * CursorTargetRadius;
		foreach (NPC npc in Main.ActiveNPCs)
		{
			if (!npc.CanBeChasedBy() || Vector2.DistanceSquared(playerCenter, npc.Center) > MaximumTargetRange * MaximumTargetRange ||
				!Collision.CanHitLine(playerCenter, 1, 1, npc.Center, 1, 1))
			{
				continue;
			}

			float cursorDistanceSquared = Vector2.DistanceSquared(cursor, npc.Center);
			if (cursorDistanceSquared < bestCursorDistanceSquared)
			{
				bestTarget = npc;
				bestCursorDistanceSquared = cursorDistanceSquared;
			}
		}

		return bestTarget?.whoAmI ?? -1;
	}

	private void SpawnStar(Player player, int index)
	{
		Vector2 aim = Projectile.velocity.SafeNormalize(new Vector2(player.direction, 0f));
		Vector2 spawn = GetStaffTip(player, aim);
		int star = Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawn, aim,
			ModContent.ProjectileType<StarsOfRuinStarProjectile>(), Projectile.damage, Projectile.knockBack,
			Projectile.owner, Projectile.ai[0], index, aim.ToRotation());
		if (star >= 0 && star < Main.maxProjectiles)
		{
			Main.projectile[star].Center = spawn;
		}
	}
}
