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

public sealed class SoulApparatusTile : ModTile
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

		AddMapEntry(new Color(56, 210, 211), CreateMapEntryName());
		DustType = DustID.Glass;
		MineResist = 1f;
	}

	public override bool RightClick(int i, int j)
	{
		SoulApparatusSystem.Open(GetTopLeft(i, j));
		return true;
	}

	public override void MouseOver(int i, int j)
	{
		Player player = Main.LocalPlayer;
		player.noThrow = 2;
		player.cursorItemIconEnabled = true;
		player.cursorItemIconID = ModContent.ItemType<SoulApparatusItem>();
	}

	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
	{
		Tile tile = Framing.GetTileSafely(i, j);
		if (tile.TileFrameX / 18 % Width == 1 && tile.TileFrameY / 18 % Height == 1)
		{
			r = 0.08f;
			g = 0.34f;
			b = 0.36f;
		}
	}

	public override void NearbyEffects(int i, int j, bool closer)
	{
		if (!closer || Main.dedServ || GetTopLeft(i, j) != new Point16(i, j) || !Main.rand.NextBool(45))
		{
			return;
		}

		Vector2 center = new(i * 16f + 24f, j * 16f + 18f);
		Dust dust = Dust.NewDustPerfect(center + Main.rand.NextVector2Circular(12f, 7f), DustID.BlueTorch,
			new Vector2(Main.rand.NextFloat(-0.12f, 0.12f), Main.rand.NextFloat(-0.35f, -0.12f)), 130,
			new Color(80, 245, 235), 0.55f);
		dust.noGravity = true;
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch) => false;

	public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
	{
		Tile tile = Framing.GetTileSafely(i, j);
		if (tile.TileFrameX % (Width * 18) != 0 || tile.TileFrameY != 0)
		{
			return;
		}

		// Custom drawing preserves the authored 48-by-48 sprite without tile padding.
		Texture2D texture = ModContent.Request<Texture2D>("SoulsOfTerra/Content/Tiles/SoulApparatus").Value;
		Vector2 center = new Vector2(i * 16f + 24f, j * 16f + 24f) - Main.screenPosition
			+ (Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange));
		Color lightColor = Lighting.GetColor(i + 1, j + 1);
		float pulse = 0.16f + 0.06f * System.MathF.Sin(Main.GlobalTimeWrappedHourly * 2.8f);
		for (int direction = 0; direction < 4; direction++)
		{
			Vector2 offset = (MathHelper.PiOver2 * direction).ToRotationVector2() * 2f;
			spriteBatch.Draw(texture, center + offset, null, new Color(50, 235, 225, 0) * pulse, 0f,
				texture.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
		}
		spriteBatch.Draw(texture, center, null, lightColor, 0f, texture.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
	}

	public override void KillMultiTile(int i, int j, int frameX, int frameY)
	{
		Item.NewItem(new EntitySource_TileBreak(i, j), i * 16, j * 16, Width * 16, Height * 16,
			ModContent.ItemType<SoulApparatusItem>());
	}

	public static Point16 GetTopLeft(int i, int j)
	{
		Tile tile = Framing.GetTileSafely(i, j);
		return new Point16(i - tile.TileFrameX / 18 % Width, j - tile.TileFrameY / 18 % Height);
	}
}
