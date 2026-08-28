using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Common.Swings;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class SoulSwingProjectile : ModProjectile
{
	private const int AfterimageSlots = 6;

	private readonly List<Vector2> ribbonPoints = new(32);
	private readonly float[] afterimageAngles = new float[AfterimageSlots];
	private SoulSwingStyle style;
	private bool styleApplied;
	private bool cutStarted;
	private bool impactFired;
	private int afterimageFilled;
	private int ticksSinceCutEnd = -1;

	public override string Texture => $"Terraria/Images/Item_{ItemID.WoodenSword}";

	private int ItemType => (int)Projectile.ai[0];
	private int Age => Math.Max(0, (style?.Duration ?? Projectile.timeLeft) - Projectile.timeLeft);
	private float AimAngle => Projectile.velocity.ToRotation();

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.DrawScreenCheckFluff[Type] = 280;
	}

	public override void SetDefaults()
	{
		Projectile.width = 160;
		Projectile.height = 160;
		Projectile.friendly = true;
		Projectile.DamageType = DamageClass.Melee;
		Projectile.penetrate = -1;
		Projectile.timeLeft = 45;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.ownerHitCheck = true;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = 45;
		Projectile.netImportant = true;
	}

	public override bool ShouldUpdatePosition() => false;

	public override void OnSpawn(IEntitySource source)
	{
		ribbonPoints.Clear();
		afterimageFilled = 0;
		cutStarted = false;
		impactFired = false;
		ticksSinceCutEnd = -1;
		styleApplied = false;
		ApplyStyle();
	}

	public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
		List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
	{
		overPlayers.Add(index);
	}

	public override void AI()
	{
		Player player = Main.player[Projectile.owner];
		if (!player.active || player.dead || player.HeldItem.type != ItemType)
		{
			Projectile.Kill();
			return;
		}

		ApplyStyle();
		if (style is null)
		{
			Projectile.Kill();
			return;
		}

		int direction = Facing();
		player.ChangeDir(direction);
		SoulSwingPose pose = style.Evaluate(Age, AimAngle, ResolveSign(player, direction));
		Vector2 hand = GetHandPosition(player, pose.Angle);
		Vector2 tip = GetTip(hand, pose);
		Projectile.Center = player.MountedCenter;
		player.heldProj = Projectile.whoAmI;
		// Hold the use open for the authored duration so autoReuse cannot overlap swings.
		player.itemTime = 2;
		player.itemAnimation = 2;
		Projectile.rotation = pose.Angle;
		player.itemRotation = MathHelper.WrapAngle(pose.Angle - (direction < 0 ? MathHelper.Pi : 0f));
		player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, pose.Angle - MathHelper.PiOver2);

		UpdateRibbon(pose, tip);
		RecordAfterimage(pose);

		if (pose.InCut && !cutStarted)
		{
			cutStarted = true;
			if (Main.netMode != NetmodeID.Server)
			{
				SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.9f, Pitch = -0.12f }, hand);
			}
		}

		if (pose.InCut && pose.CutProgress >= 0.5f && !impactFired)
		{
			impactFired = true;
			if (Projectile.owner == Main.myPlayer && ResolveSwingItem(player) is ISoulSwingItem swingItem)
			{
				swingItem.OnSwingCut(player, Projectile, tip, AimAngle.ToRotationVector2());
			}
		}

		if (pose.InCut)
		{
			Color glow = style.RibbonColor;
			Lighting.AddLight(tip, glow.R / 255f * 0.28f, glow.G / 255f * 0.28f, glow.B / 255f * 0.28f);
		}
	}

	public override bool? CanDamage()
	{
		if (style is null)
		{
			return false;
		}

		return style.Evaluate(Age, AimAngle, ResolveSign(Main.player[Projectile.owner], Facing())).CanDamage ? null : false;
	}

	public override bool? Colliding(Rectangle projectileHitbox, Rectangle targetHitbox)
	{
		if (style is null)
		{
			return false;
		}

		Player player = Main.player[Projectile.owner];
		SoulSwingPose pose = style.Evaluate(Age, AimAngle, ResolveSign(player, Facing()));
		Vector2 start = GetHandPosition(player, pose.Angle);
		Vector2 end = GetTip(start, pose);
		float collisionPoint = 0f;
		return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end,
			style.HitWidth, ref collisionPoint);
	}

	public override bool PreDraw(ref Color lightColor)
	{
		if (style is null)
		{
			return false;
		}

		Player player = Main.player[Projectile.owner];
		int direction = Facing();
		SoulSwingPose pose = style.Evaluate(Age, AimAngle, ResolveSign(player, direction));
		Vector2 hand = GetHandPosition(player, pose.Angle);
		Main.instance.LoadItem(ItemType);
		Texture2D blade = TextureAssets.Item[ItemType].Value;
		float scale = player.HeldItem.type == ItemType ? player.GetAdjustedItemScale(player.HeldItem) : style.Scale;

		SoulSwingRibbon.Draw(ribbonPoints, style.RibbonColor, RibbonFade(), style.RibbonWidth,
			Age / 60f, Projectile.identity);

		if (pose.InCut || ticksSinceCutEnd >= 0)
		{
			for (int ghost = afterimageFilled - 1; ghost >= 1; ghost--)
			{
				float strength = 1f - ghost / (float)Math.Max(1, afterimageFilled);
				Color ghostColor = Color.Lerp(style.RibbonColor, Color.White, 0.35f) * (0.1f + strength * 0.28f);
				float ghostAngle = afterimageAngles[ghost];
				DrawBlade(blade, GetHandPosition(player, ghostAngle), ghostAngle, direction, scale, ghostColor);
			}
		}

		DrawBlade(blade, hand, pose.Angle, direction, scale, Color.White);
		return false;
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		Player player = Main.player[Projectile.owner];
		player.HeldItem.ModItem?.OnHitNPC(player, target, hit, damageDone);
	}

	private void ApplyStyle()
	{
		if (styleApplied)
		{
			return;
		}

		Player player = Main.player[Projectile.owner];
		if (ResolveSwingItem(player) is not ISoulSwingItem swingItem)
		{
			return;
		}

		style = swingItem.GetSwingStyle(player);
		Projectile.timeLeft = style.Duration;
		Projectile.localNPCHitCooldown = style.Duration;
		int box = (int)MathF.Ceiling(Math.Max(96f, style.Reach * 2f + style.HitWidth + 32f));
		Projectile.width = box;
		Projectile.height = box;
		Projectile.Center = player.MountedCenter;
		styleApplied = true;
	}

	private ISoulSwingItem ResolveSwingItem(Player player)
	{
		if (player.HeldItem.type == ItemType && player.HeldItem.ModItem is ISoulSwingItem held)
		{
			return held;
		}

		return ContentSamples.ItemsByType.TryGetValue(ItemType, out Item sample)
			? sample.ModItem as ISoulSwingItem
			: ItemLoader.GetItem(ItemType) as ISoulSwingItem;
	}

	private float ResolveSign(Player player, int direction)
	{
		if (style.Path == SoulSwingPath.AlternatingLateral)
		{
			return Projectile.ai[1] >= 0f ? 1f : -1f;
		}

		return direction != 0 ? direction : player.direction;
	}

	private int Facing() => Projectile.velocity.X >= 0f ? 1 : -1;

	private void UpdateRibbon(SoulSwingPose pose, Vector2 tip)
	{
		if (pose.InCut)
		{
			if (ribbonPoints.Count == 0 || Vector2.DistanceSquared(ribbonPoints[^1], tip) > 2.25f)
			{
				if (ribbonPoints.Count >= 48)
				{
					ribbonPoints.RemoveAt(0);
				}

				ribbonPoints.Add(tip);
			}
			else
			{
				ribbonPoints[^1] = tip;
			}

			ticksSinceCutEnd = -1;
			return;
		}

		if (!cutStarted || ribbonPoints.Count == 0)
		{
			return;
		}

		if (ticksSinceCutEnd < 0)
		{
			ticksSinceCutEnd = 0;
		}
		else
		{
			ticksSinceCutEnd++;
		}
	}

	private float RibbonFade()
	{
		if (ticksSinceCutEnd < 0)
		{
			return cutStarted ? 1f : 0f;
		}

		return MathHelper.Clamp(1f - ticksSinceCutEnd / (float)Math.Max(1, style.RibbonLifetime), 0f, 1f);
	}

	private void RecordAfterimage(SoulSwingPose pose)
	{
		if (!pose.InCut && ticksSinceCutEnd < 0)
		{
			return;
		}

		int slots = Math.Min(style.AfterimageCount, AfterimageSlots);
		for (int index = slots - 1; index > 0; index--)
		{
			afterimageAngles[index] = afterimageAngles[index - 1];
		}

		afterimageAngles[0] = pose.Angle;
		if (afterimageFilled < slots)
		{
			afterimageFilled++;
		}
	}

	private Vector2 GetTip(Vector2 hand, SoulSwingPose pose)
	{
		return hand + pose.Angle.ToRotationVector2() * (style.Reach * pose.ReachMultiplier);
	}

	private void DrawBlade(Texture2D texture, Vector2 handPosition, float bladeAngle, int direction, float scale,
		Color color)
	{
		Vector2 grip = style.GripOrigin ?? new Vector2(texture.Width * 0.1f, texture.Height * 0.9f);
		if (direction < 0)
		{
			grip = new Vector2(grip.X, texture.Height - grip.Y);
		}

		SpriteEffects effects = direction > 0 ? SpriteEffects.None : SpriteEffects.FlipVertically;
		float textureRotation = bladeAngle + direction * MathHelper.PiOver4;
		Main.EntitySpriteDraw(texture, handPosition - Main.screenPosition, null, color, textureRotation, grip, scale,
			effects);
	}

	private static Vector2 GetHandPosition(Player player, float swordAngle)
	{
		float armRotation = swordAngle - MathHelper.PiOver2;
		Vector2 handPosition = player.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, armRotation);
		handPosition.Y += player.gfxOffY;
		return handPosition;
	}
}
