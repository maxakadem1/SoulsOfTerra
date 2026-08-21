using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Content.Items;
using SoulsOfTerra.Systems;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace SoulsOfTerra.Content.Tiles;

public class TerraShrineTile : ModTile
{
	public override string Texture => $"Terraria/Images/Tiles_{TileID.Campfire}";

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLighted[Type] = true;
		TileID.Sets.Campfire[Type] = true;
		AdjTiles = new[] { (int)TileID.Campfire };

		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
		TileObjectData.addTile(Type);
		AddMapEntry(new Color(75, 190, 155), CreateMapEntryName());
		DustType = DustID.Stone;
	}

	public override bool RightClick(int i, int j)
	{
		Tile tile = Framing.GetTileSafely(i, j);
		Point16 topLeft = new(i - tile.TileFrameX / 18 % 3, j - tile.TileFrameY / 18 % 2);
		SoulMenuSystem.OpenShrine(topLeft);
		return true;
	}

	public override void MouseOver(int i, int j)
	{
		Player player = Main.LocalPlayer;
		player.noThrow = 2;
		player.cursorItemIconEnabled = true;
		player.cursorItemIconID = ModContent.ItemType<BrokenTerraBladeCore>();
	}

	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
	{
		r = 0.35f;
		g = 0.55f;
		b = 0.42f;
	}

	public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
	{
		Tile tile = Framing.GetTileSafely(i, j);
		if (tile.TileFrameX != 0 || tile.TileFrameY != 0)
		{
			return;
		}

		// Placeholder blade overlay extends beyond the unchanged campfire footprint.
		Texture2D blade = TextureAssets.Item[ItemID.BrokenHeroSword].Value;
		Vector2 position = new Vector2(i * 16f + 24f, j * 16f + 8f) - Main.screenPosition;
		spriteBatch.Draw(blade, position, null, Lighting.GetColor(i, j), -0.55f, blade.Size() * 0.5f, 0.72f, SpriteEffects.None, 0f);
	}

	public override void KillMultiTile(int i, int j, int frameX, int frameY)
	{
		IEntitySource source = new EntitySource_TileBreak(i, j);
		Item.NewItem(source, i * 16, j * 16, 48, 32, ModContent.ItemType<BrokenTerraBladeCore>());
		Item.NewItem(source, i * 16, j * 16, 48, 32, ItemID.Campfire);
	}
}
