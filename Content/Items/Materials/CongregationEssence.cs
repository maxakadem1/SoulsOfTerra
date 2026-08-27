using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Items.Materials;

public class CongregationEssence : BossEssenceItem
{
	public override void SetDefaults()
	{
		Item.width = 16;
		Item.height = 16;
		Item.maxStack = Item.CommonMaxStack;
		Item.rare = ItemRarityID.Orange;
		Item.value = 0;
	}
}
