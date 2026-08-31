using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SoulsOfTerra.Content.Items.Weapons;
using SoulsOfTerra.Content.Projectiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Items.Weapons.Ranged;

public class UnkemptHarold : ImbuementWeaponItem
{
	public override void SetDefaults()
	{
		Item.width = 49;
		Item.height = 37;
		Item.damage = 85;
		Item.DamageType = DamageClass.Ranged;
		Item.useTime = 22;
		Item.useAnimation = 22;
		Item.useStyle = ItemUseStyleID.Shoot;
		Item.knockBack = 6f;
		Item.UseSound = SoundID.Item41 with { Pitch = -0.2f, Volume = 0.7f };
		Item.autoReuse = true;
		Item.noMelee = true;
		Item.rare = ItemRarityID.Red;
		Item.value = Item.buyPrice(gold: 10);
		Item.useAmmo = AmmoID.Bullet;
		Item.shoot = ModContent.ProjectileType<UnkemptHaroldGyrojetProjectile>();
		Item.shootSpeed = UnkemptHaroldGyrojetProjectile.ForwardSpeed;
	}

	public override Vector2? HoldoutOffset() => new Vector2(-10f, 2f);

	public override bool CanUseItem(Player player) => CountBulletAmmo(player) >= 3;

	public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type,
		ref int damage, ref float knockback)
	{
		type = ModContent.ProjectileType<UnkemptHaroldGyrojetProjectile>();
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
		Vector2 velocity, int type, int damage, float knockback)
	{
		ConsumeExtraBullets(player, 2);
		Vector2 aim = velocity.SafeNormalize(new Vector2(player.direction, 0f));
		Vector2 perp = aim.RotatedBy(MathHelper.PiOver2);
		for (int lane = -1; lane <= 1; lane++)
		{
			Vector2 spawn = position + perp * UnkemptHaroldGyrojetProjectile.OffsetAt(lane, 0f);
			float nextSplit = lane == 0 ? 0f : 1f;
			Projectile.NewProjectile(source, spawn, aim * UnkemptHaroldGyrojetProjectile.ForwardSpeed, type, damage,
				knockback, player.whoAmI, lane, nextSplit, 0f);
		}

		return false;
	}

	public override void ModifyTooltips(List<TooltipLine> tooltips)
	{
		TooltipLine flavor = new(Mod, "HaroldRedText",
			"Did I fire six shots, or only five? Three? Seven. Whatever.")
		{
			OverrideColor = new Color(255, 24, 24)
		};
		int tooltipIndex = tooltips.FindLastIndex(line => line.Name.StartsWith("Tooltip"));
		if (tooltipIndex >= 0)
		{
			tooltips.Insert(tooltipIndex + 1, flavor);
			return;
		}

		tooltips.Add(flavor);
	}

	private static int CountBulletAmmo(Player player)
	{
		int count = 0;
		for (int slot = 0; slot < player.inventory.Length; slot++)
		{
			Item item = player.inventory[slot];
			if (item.IsAir || item.ammo != AmmoID.Bullet)
			{
				continue;
			}

			if (!item.consumable)
			{
				return 3;
			}

			count += item.stack;
			if (count >= 3)
			{
				return count;
			}
		}

		return count;
	}

	private void ConsumeExtraBullets(Player player, int extra)
	{
		for (int index = 0; index < extra; index++)
		{
			player.PickAmmo(Item, out _, out _, out _, out _, out _);
		}
	}
}
