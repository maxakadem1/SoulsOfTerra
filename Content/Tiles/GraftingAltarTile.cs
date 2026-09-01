using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Content.Items.Access;
using SoulsOfTerra.Systems;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace SoulsOfTerra.Content.Tiles;

public sealed class GraftingAltarTile : ModTile
{
	public const int Width = 3;
	public const int Height = 3;

	public override string Texture => $"Terraria/Images/Tiles_{TileID.Campfire}";

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLighted[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
		TileObjectData.newTile.Width = Width;
		TileObjectData.newTile.Height = Height;
		TileObjectData.newTile.Origin = new Point16(1, 2);
		TileObjectData.newTile.CoordinateHeights = new[] { 16, 16, 16 };
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, Width, 0);
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(174, 66, 92), CreateMapEntryName());
		DustType = DustID.Blood;
		MineResist = 1f;
	}

	public override bool RightClick(int i, int j)
	{
		GraftingAltarSystem.Open(GetTopLeft(i, j));
		return true;
	}

	public override void MouseOver(int i, int j)
	{
		Player player = Main.LocalPlayer;
		player.noThrow = 2;
		player.cursorItemIconEnabled = true;
		player.cursorItemIconID = ModContent.ItemType<GraftingAltarItem>();
	}

	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
	{
		Tile tile = Framing.GetTileSafely(i, j);
		if (tile.TileFrameX / 18 % Width == 1 && tile.TileFrameY / 18 % Height == 1)
		{
			r = 0.22f;
			g = 0.08f;
			b = 0.12f;
		}
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch) => false;

	public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
	{
		Tile tile = Framing.GetTileSafely(i, j);
		if (tile.TileFrameX % (Width * 18) != 0 || tile.TileFrameY != 0)
		{
			return;
		}

		Texture2D texture = ModContent.Request<Texture2D>("SoulsOfTerra/Content/Tiles/SoulApparatus").Value;
		Vector2 center = new Vector2(i * 16f + 24f, j * 16f + 24f) - Main.screenPosition
			+ (Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange));
		spriteBatch.Draw(texture, center, null, Lighting.GetColor(i + 1, j + 1), 0f,
			texture.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
	}

	public override void KillMultiTile(int i, int j, int frameX, int frameY)
	{
		Item.NewItem(new EntitySource_TileBreak(i, j), i * 16, j * 16, Width * 16, Height * 16,
			ModContent.ItemType<GraftingAltarItem>());
	}

	public static Point16 GetTopLeft(int i, int j)
	{
		Tile tile = Framing.GetTileSafely(i, j);
		return new Point16(i - tile.TileFrameX / 18 % Width, j - tile.TileFrameY / 18 % Height);
	}
}
