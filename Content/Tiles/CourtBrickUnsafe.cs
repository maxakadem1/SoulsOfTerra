using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Tiles;

public sealed class CourtBrickUnsafe : ModTile
{
	// Vanilla dungeon brick is temporary graybox art for the final cold-stone sheet.
	public override string Texture => $"Terraria/Images/Tiles_{TileID.BlueDungeonBrick}";

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileBlockLight[Type] = true;
		Main.tileBrick[Type] = true;
		HitSound = SoundID.Tink;
		DustType = DustID.Stone;
		AddMapEntry(new Color(46, 57, 73), CreateMapEntryName());
	}

	public override bool CanKillTile(int i, int j, ref bool blockDamaged) => false;
	public override bool CanExplode(int i, int j) => false;
	public override bool CanReplace(int i, int j, int tileTypeBeingPlaced) => false;
	public override bool Slope(int i, int j) => false;
}
