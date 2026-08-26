using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Items.Access;

public class WardensFragment : ModItem
{
	// Moon Lord Essence is a temporary inventory placeholder until dedicated key art is ready.
	public override string Texture => "SoulsOfTerra/Content/Items/Materials/MoonLordEssence";

	public override void SetDefaults()
	{
		Item.width = 20;
		Item.height = 20;
		Item.maxStack = 1;
		Item.rare = ItemRarityID.Orange;
		Item.value = 0;
	}
}
