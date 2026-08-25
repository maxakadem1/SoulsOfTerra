using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Items.Materials;

public class MoonLordEssence : ModItem
{
	// A vanilla lunar fragment stands in until the authored essence sprite is ready.
	public override string Texture => $"Terraria/Images/Item_{ItemID.FragmentNebula}";

	public override void SetDefaults()
	{
		Item.width = 16;
		Item.height = 16;
		Item.maxStack = Item.CommonMaxStack;
		Item.rare = ItemRarityID.Red;
		Item.value = 0;
	}
}
