using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Items;

public class SlimeboundBlade : ModItem
{
	public override string Texture => $"Terraria/Images/Item_{ItemID.BluePhaseblade}";

	public override void SetDefaults()
	{
		Item.width = 40;
		Item.height = 40;
		Item.damage = 18;
		Item.DamageType = DamageClass.Melee;
		Item.useTime = 22;
		Item.useAnimation = 22;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.knockBack = 5f;
		Item.UseSound = SoundID.Item1;
		Item.autoReuse = true;
		Item.rare = ItemRarityID.Blue;
		Item.value = Item.buyPrice(silver: 50);
	}

	public override void AddRecipes()
	{
		// A standard recipe keeps the weapon compatible with Magic Storage.
		CreateRecipe()
			.AddIngredient<SlimeEssence>()
			.AddIngredient(ItemID.Gel, 30)
			.AddRecipeGroup(RecipeGroupID.IronBar, 8)
			.AddTile(TileID.Anvils)
			.Register();
	}
}
