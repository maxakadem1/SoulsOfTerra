using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Common;
using SoulsOfTerra.Content.Items.Weapons.Melee;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class UnisonClapProjectile : ModProjectile
{
	public const int ClapDuration = 32;
	public const int SmashTime = 22;

	private int Age => ClapDuration - Projectile.timeLeft;
	private bool smashed;

	public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.PurificationPowder}";

	public override void SetDefaults()
	{
		Projectile.width = 64;
		Projectile.height = 64;
		Projectile.friendly = false;
		Projectile.DamageType = DamageClass.Melee;
		Projectile.penetrate = -1;
		Projectile.timeLeft = ClapDuration;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.netImportant = true;
	}

	public override bool ShouldUpdatePosition() => false;
	public override bool? CanDamage() => false;

	public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
		List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
	{
		overPlayers.Add(index);
	}

	public override void OnSpawn(IEntitySource source)
	{
		smashed = false;
	}

	public override void AI()
	{
		Player player = Main.player[Projectile.owner];
		if (!player.active || player.dead || player.HeldItem.type != ModContent.ItemType<Unison>())
		{
			Projectile.Kill();
			return;
		}

		int direction = Projectile.velocity.X >= 0f ? 1 : -1;
		player.ChangeDir(direction);
		GetFistPositions(player, direction, out Vector2 leftFist, out Vector2 rightFist);
		Projectile.Center = (leftFist + rightFist) * 0.5f;
		player.heldProj = Projectile.whoAmI;
		player.itemTime = 2;
		player.itemAnimation = 2;
		player.itemRotation = 0f;
		PoseArms(player, leftFist, rightFist);

		if (Age == 0 && Main.netMode != NetmodeID.Server)
		{
			SoundEngine.PlaySound(SoundID.Item117 with { Pitch = -0.2f, Volume = 0.42f }, player.MountedCenter);
		}

		if (Age >= SmashTime && !smashed)
		{
			smashed = true;
			if (Projectile.owner == Main.myPlayer)
			{
				Projectile.NewProjectile(player.GetSource_ItemUse(player.HeldItem), player.MountedCenter, Vector2.Zero,
					ModContent.ProjectileType<UnisonWaveProjectile>(), Projectile.damage, Projectile.knockBack,
					player.whoAmI);
			}

			if (Main.netMode != NetmodeID.Server)
			{
				SoundEngine.PlaySound(SoundID.Item74 with { Pitch = -0.25f, Volume = 0.92f }, player.MountedCenter);
				SoundEngine.PlaySound(SoundID.Item14 with { Pitch = -0.65f, Volume = 0.7f }, player.MountedCenter);
			}
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Player player = Main.player[Projectile.owner];
		int direction = Projectile.velocity.X >= 0f ? 1 : -1;
		GetFistPositions(player, direction, out Vector2 leftFist, out Vector2 rightFist);
		DrawFist(leftFist, -1);
		DrawFist(rightFist, 1);
		return false;
	}

	private void GetFistPositions(Player player, int direction, out Vector2 leftFist, out Vector2 rightFist)
	{
		float separation = GetSeparation();
		float spread = MathHelper.Lerp(4f, 30f, separation);
		Vector2 origin = player.MountedCenter + new Vector2(direction * MathHelper.Lerp(14f, 20f, 1f - separation),
			player.gfxOffY - 4f);
		leftFist = origin + new Vector2(-spread, 0f);
		rightFist = origin + new Vector2(spread, 0f);
	}

	private float GetSeparation()
	{
		if (Age >= SmashTime)
		{
			float recover = (Age - SmashTime) / (float)Math.Max(1, ClapDuration - SmashTime);
			return MathHelper.Lerp(0.04f, 0.12f, recover * recover);
		}

		const float split = 0.62f;
		float progress = Age / (float)SmashTime;
		if (progress < split)
		{
			return MathHelper.Lerp(0.22f, 1f, EaseOutCubic(progress / split));
		}

		return MathHelper.Lerp(1f, 0.04f, EaseInCubic((progress - split) / (1f - split)));
	}

	private static void PoseArms(Player player, Vector2 leftFist, Vector2 rightFist)
	{
		float leftAngle = (leftFist - player.MountedCenter).ToRotation();
		float rightAngle = (rightFist - player.MountedCenter).ToRotation();
		float frontAngle = player.direction > 0 ? rightAngle : leftAngle;
		float backAngle = player.direction > 0 ? leftAngle : rightAngle;
		player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, frontAngle - MathHelper.PiOver2);
		player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, backAngle - MathHelper.PiOver2);
	}

	private static void DrawFist(Vector2 worldPosition, int side)
	{
		Texture2D glow = SoulOrbProjectile.GetGlowTexture();
		Texture2D ring = SoulOrbProjectile.GetRingTexture();
		Vector2 glowOrigin = glow.Size() * 0.5f;
		Vector2 ringOrigin = ring.Size() * 0.5f;
		Vector2 position = worldPosition - Main.screenPosition;
		float tilt = side * 0.35f;
		Main.EntitySpriteDraw(glow, position, null, new Color(24, 188, 171, 0) * 0.55f, tilt, glowOrigin,
			new Vector2(0.42f, 0.28f), SpriteEffects.None);
		Main.EntitySpriteDraw(glow, position, null, new Color(3, 7, 11, 220), tilt, glowOrigin,
			new Vector2(0.28f, 0.2f), SpriteEffects.None);
		Main.EntitySpriteDraw(ring, position, null, new Color(96, 235, 213, 0) * 0.85f, tilt + Main.GlobalTimeWrappedHourly * side * 0.4f,
			ringOrigin, new Vector2(0.22f, 0.16f), SpriteEffects.None);
		Main.EntitySpriteDraw(glow, position + new Vector2(0f, -4f), null, new Color(210, 255, 246, 0) * 0.45f, tilt,
			glowOrigin, 0.08f, SpriteEffects.None);
	}

	private static float EaseOutCubic(float progress) => 1f - MathF.Pow(1f - progress, 3f);
	private static float EaseInCubic(float progress) => progress * progress * progress;
}
