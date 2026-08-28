using Microsoft.Xna.Framework;
using SoulsOfTerra.Content.Items.Weapons;
using SoulsOfTerra.Content.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Items.Weapons.Melee;

public class Unison : ImbuementWeaponItem
{
	public override void SetDefaults()
	{
		Item.width = 32;
		Item.height = 32;
		Item.damage = 66;
		Item.DamageType = DamageClass.Melee;
		Item.useTime = UnisonClapProjectile.ClapDuration;
		Item.useAnimation = UnisonClapProjectile.ClapDuration;
		Item.useStyle = ItemUseStyleID.Shoot;
		Item.knockBack = 5.5f;
		Item.UseSound = null;
		Item.autoReuse = true;
		Item.noMelee = true;
		Item.noUseGraphic = true;
		Item.rare = ItemRarityID.Orange;
		Item.value = Item.buyPrice(gold: 2);
		Item.shoot = ModContent.ProjectileType<UnisonClapProjectile>();
		Item.shootSpeed = 1f;
	}

	public override bool CanUseItem(Player player)
	{
		return player.ownedProjectileCounts[ModContent.ProjectileType<UnisonClapProjectile>()] <= 0
			&& player.ownedProjectileCounts[ModContent.ProjectileType<UnisonWaveProjectile>()] <= 0;
	}

	public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		Vector2 aim = (Main.MouseWorld - player.MountedCenter).SafeNormalize(new Vector2(player.direction, 0f));
		Projectile.NewProjectile(source, player.MountedCenter, aim, type, damage, knockback, player.whoAmI);
		return false;
	}
}
