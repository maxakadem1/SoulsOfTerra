using SoulsOfTerra.Content.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Items.Access;

public sealed class SoulApparatusItem : ModItem
{
	// The supplied station art doubles as its inventory icon.
	public override string Texture => "SoulsOfTerra/Content/Tiles/SoulApparatus";

	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(ModContent.TileType<SoulApparatusTile>());
		Item.width = 32;
		Item.height = 32;
		Item.maxStack = 99;
		Item.rare = ItemRarityID.Blue;
		Item.value = 0;
	}
}
