using SoulsOfTerra.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Items.Accessories;

public class BorrowedSentence : ModItem
{
	public override void SetDefaults()
	{
		Item.width = 32;
		Item.height = 36;
		Item.accessory = true;
		Item.rare = ItemRarityID.Expert;
		Item.expert = true;
		Item.value = Item.sellPrice(gold: 2);
	}

	public override void UpdateAccessory(Player player, bool hideVisual)
	{
		// The ModPlayer owns the trial so every player-attributed damage source can repay it.
		player.GetModPlayer<BorrowedSentencePlayer>().Equipped = true;
	}
}
