using Microsoft.Xna.Framework;
using SoulsOfTerra.Content.Items.Materials;
using SoulsOfTerra.Content.Projectiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Items.Weapons.Magic;

public class MoonstoneStaff : ModItem
{
	public override void SetStaticDefaults()
	{
		// Staff handling aligns the diagonal 80-pixel artwork with the casting pose.
		Item.staff[Type] = true;
	}

	public override void SetDefaults()
	{
		Item.width = 80;
		Item.height = 80;
		Item.damage = 450;
		Item.DamageType = DamageClass.Magic;
		Item.mana = 50;
		Item.useTime = MoonstoneChargeProjectile.ChargeDuration;
		Item.useAnimation = MoonstoneChargeProjectile.ChargeDuration;
		Item.useStyle = ItemUseStyleID.Shoot;
		Item.noMelee = true;
		Item.knockBack = 8f;
		Item.autoReuse = false;
		Item.rare = ItemRarityID.Red;
		Item.value = Item.buyPrice(gold: 10);
		Item.shoot = ModContent.ProjectileType<MoonstoneChargeProjectile>();
		Item.shootSpeed = 1f;
	}

	public override bool CanUseItem(Player player)
	{
		return player.ownedProjectileCounts[ModContent.ProjectileType<MoonstoneChargeProjectile>()] == 0;
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity,
		int type, int damage, float knockback)
	{
		Vector2 aim = velocity.SafeNormalize(new Vector2(player.direction, 0f));
		Projectile.NewProjectile(source, player.MountedCenter, aim, type, damage, knockback, player.whoAmI);
		return false;
	}

	public override void AddRecipes()
	{
		// Ordinary crafting preserves compatibility with Magic Storage.
		CreateRecipe()
			.AddIngredient<MoonLordEssence>()
			.AddIngredient(ItemID.LunarBar, 18)
			.AddTile(TileID.LunarCraftingStation)
			.Register();
	}
}
