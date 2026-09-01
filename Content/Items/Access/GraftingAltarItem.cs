using SoulsOfTerra.Content.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Items.Access;

public sealed class GraftingAltarItem : ModItem
{
	// The Soul Apparatus art is a temporary placeholder for the new station.
	public override string Texture => "SoulsOfTerra/Content/Tiles/SoulApparatus";

	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(ModContent.TileType<GraftingAltarTile>());
		Item.width = 32;
		Item.height = 32;
		Item.maxStack = Item.CommonMaxStack;
		Item.rare = ItemRarityID.Blue;
		Item.value = 0;
	}
}
