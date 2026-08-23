using Microsoft.Xna.Framework;
using SoulsOfTerra.Content.Items.Materials;
using SoulsOfTerra.Content.Projectiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Items.Weapons.Magic;

public class ServantsGaze : ModItem
{
	private const int ServantsPerCast = 3;

	public override void SetStaticDefaults()
	{
		// Staff handling keeps the diagonal sprite aligned with the casting pose.
		Item.staff[Type] = true;
	}

	public override void SetDefaults()
	{
		Item.width = 40;
		Item.height = 40;
		Item.damage = 15;
		Item.DamageType = DamageClass.Magic;
		Item.mana = 10;
		Item.useTime = 36;
		Item.useAnimation = 36;
		Item.useStyle = ItemUseStyleID.Shoot;
		Item.noMelee = true;
		Item.knockBack = 2.5f;
		Item.UseSound = SoundID.Item8;
		Item.autoReuse = true;
		Item.rare = ItemRarityID.Blue;
		Item.value = Item.buyPrice(silver: 60);
		Item.shoot = ModContent.ProjectileType<ServantEyeProjectile>();
		Item.shootSpeed = 2.75f;
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		for (int index = 0; index < ServantsPerCast; index++)
		{
			int spreadIndex = index - 1;
			Vector2 servantVelocity = velocity.RotatedBy(spreadIndex * 0.18f);
			Projectile.NewProjectile(source, position, servantVelocity, type, damage, knockback, player.whoAmI, 0f, spreadIndex);
		}

		return false;
	}

	public override void AddRecipes()
	{
		// Separate evil-bar recipes remain discoverable by ordinary crafting integrations.
		RegisterRecipe(ItemID.DemoniteBar);
		RegisterRecipe(ItemID.CrimtaneBar);
	}

	private void RegisterRecipe(int evilBarType)
	{
		CreateRecipe()
			.AddIngredient<EyeEssence>()
			.AddIngredient(evilBarType, 8)
			.AddIngredient(ItemID.Lens, 3)
			.AddTile(TileID.Anvils)
			.Register();
	}
}
