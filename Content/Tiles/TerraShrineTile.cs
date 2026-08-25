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

public class TerraShrineTile : ModTile
{
	public const int Width = 4;
	public const int Height = 2;
	private const int StyleFrameWidth = Width * 18;

	// Default tile drawing is suppressed in favor of the standalone 64x32 texture.
	public override string Texture => $"Terraria/Images/Tiles_{TileID.Campfire}";

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLighted[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
		TileObjectData.newTile.Width = Width;
		TileObjectData.newTile.Height = Height;
		TileObjectData.newTile.Origin = new Point16(1, Height - 1);
		TileObjectData.newTile.CoordinateHeights = new[] { 16, 16 };
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, Width, 0);
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.StyleWrapLimit = 2;
		TileObjectData.addTile(Type);
		AddMapEntry(new Color(75, 190, 155), CreateMapEntryName());
		DustType = DustID.Stone;
	}

	public override bool RightClick(int i, int j)
	{
		Tile tile = Framing.GetTileSafely(i, j);
		Point16 topLeft = new(i - tile.TileFrameX / 18 % Width, j - tile.TileFrameY / 18 % Height);
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
		Tile tile = Framing.GetTileSafely(i, j);
		if (tile.TileFrameX / 18 % Width is 1 or 2 && tile.TileFrameY / 18 % Height == 1)
		{
			r = 0.08f;
			g = 0.22f;
			b = 0.18f;
		}
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch) => false;

	public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
	{
		Tile tile = Framing.GetTileSafely(i, j);
		if (tile.TileFrameX % StyleFrameWidth != 0 || tile.TileFrameY != 0)
		{
			return;
		}

		Color lightColor = Lighting.GetColor(i + 1, j + 1);
		Vector2 shrineCenter = new(i * 16f + Width * 8f, j * 16f + Height * 8f);
		// Tile targets include an off-screen margin before the final screen composite.
		Vector2 drawPosition = shrineCenter - Main.screenPosition
			+ (Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange));

		Texture2D anvil = ModContent.Request<Texture2D>("SoulsOfTerra/Content/Tiles/SoulAnvil").Value;
		// A 32x16 source becomes crisp 2x pixel art across the 4x2 footprint.
		Vector2 anvilScale = new(Width * 16f / anvil.Width, Height * 16f / anvil.Height);
		spriteBatch.Draw(anvil, drawPosition, null, lightColor, 0f,
			anvil.Size() * 0.5f, anvilScale, SpriteEffects.None, 0f);

		Point16 topLeft = new(i, j);
		if (SoulMenuSystem.TryGetShrinePreview(topLeft, out int previewItemType))
		{
			Texture2D itemTexture = TextureAssets.Item[previewItemType].Value;
			float itemScale = System.MathF.Min(44f / itemTexture.Width, 44f / itemTexture.Height);
			Vector2 floatPosition = drawPosition + new Vector2(0f,
				-30f + System.MathF.Sin(Main.GlobalTimeWrappedHourly * 3f) * 3f);
			// The preview is client-local until the server accepts and broadcasts the ritual.
			spriteBatch.Draw(itemTexture, floatPosition, null, Color.White, 0f,
				itemTexture.Size() * 0.5f, itemScale, SpriteEffects.None, 0f);
		}
	}

	public override void KillMultiTile(int i, int j, int frameX, int frameY)
	{
		IEntitySource source = new EntitySource_TileBreak(i, j);
		int anvilItemType = frameX / StyleFrameWidth == 1 ? ItemID.LeadAnvil : ItemID.IronAnvil;
		Item.NewItem(source, i * 16, j * 16, Width * 16, Height * 16, ModContent.ItemType<BrokenTerraBladeCore>());
		Item.NewItem(source, i * 16, j * 16, Width * 16, Height * 16, anvilItemType);
	}
}
