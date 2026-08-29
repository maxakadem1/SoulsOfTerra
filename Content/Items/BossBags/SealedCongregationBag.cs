using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using SoulsOfTerra.Content.Items.Accessories;

namespace SoulsOfTerra.Content.Items.BossBags;

public class SealedCongregationBag : ModItem
{
	public override void SetStaticDefaults()
	{
		ItemID.Sets.BossBag[Type] = true;
		ItemID.Sets.PreHardmodeLikeBossBag[Type] = true;
		Item.ResearchUnlockCount = 3;
	}

	public override void SetDefaults()
	{
		Item.width = 36;
		Item.height = 36;
		Item.maxStack = Item.CommonMaxStack;
		Item.consumable = true;
		Item.rare = ItemRarityID.Expert;
		Item.expert = true;
	}

	public override bool CanRightClick() => true;

	public override void ModifyItemLoot(ItemLoot itemLoot)
	{
		// The signature accessory is guaranteed in every Expert or Master bag.
		itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<BorrowedSentence>()));
		itemLoot.Add(ItemDropRule.Common(ItemID.HealingPotion, 1, 5, 8));
	}
}
