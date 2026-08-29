using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Content.Items.Access;
using SoulsOfTerra.Content.Projectiles;
using SoulsOfTerra.Systems;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace SoulsOfTerra.Content.Tiles;

public class TerraforgeTile : ModTile
{
	public const int Width = 4;
	public const int Height = 3;
	private const int StyleFrameWidth = Width * 18;

	// Custom rendering keeps the authored 2x pixel clusters crisp.
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
		TileObjectData.newTile.CoordinateHeights = new[] { 16, 16, 16 };
		TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, Width, 0);
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.StyleWrapLimit = 2;
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(75, 190, 155), CreateMapEntryName());
		DustType = DustID.Stone;
		MinPick = 0;
		MineResist = 1f;
	}

	public override bool RightClick(int i, int j)
	{
		Point16 topLeft = GetTopLeft(i, j);
		if (TerraforgeFormationProjectile.IsFormingAt(topLeft))
		{
			return true;
		}

		SoulMenuSystem.OpenTerraforge(topLeft);
		return true;
	}

	public override void MouseOver(int i, int j)
	{
		Player player = Main.LocalPlayer;
		player.noThrow = 2;
		player.cursorItemIconEnabled = true;
		player.cursorItemIconID = ModContent.ItemType<TerraBladeFragment>();
	}

	public override bool CanExplode(int i, int j) => false;

	public override bool CanKillTile(int i, int j, ref bool blockDamaged)
	{
		if (!TerraforgeFormationProjectile.IsFormingAt(GetTopLeft(i, j)))
		{
			return true;
		}

		blockDamaged = false;
		return false;
	}

	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
	{
		Tile tile = Framing.GetTileSafely(i, j);
		if (tile.TileFrameX / 18 % Width is 1 or 2 && tile.TileFrameY / 18 % Height == 1)
		{
			float strength = 0.18f + GetVisualStage() * 0.055f;
			r = strength * 0.7f;
			g = strength * 1.25f;
			b = strength * 0.75f;
		}
	}

	public override void NearbyEffects(int i, int j, bool closer)
	{
		int stage = GetVisualStage();
		int dustInterval = 52 - stage * 10;
		if (!closer || Main.dedServ || GetTopLeft(i, j) != new Point16(i, j) || !Main.rand.NextBool(dustInterval))
		{
			return;
		}

		Vector2 center = new(i * 16f + Width * 8f, j * 16f + 23f);
		int dustType = stage >= 2 && Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.GreenTorch;
		Color color = dustType == DustID.GoldFlame ? new Color(245, 205, 85) : new Color(80, 225, 180);
		Dust dust = Dust.NewDustPerfect(center + Main.rand.NextVector2Circular(10f + stage * 2f, 5f), dustType,
			new Vector2(Main.rand.NextFloat(-0.15f, 0.15f), Main.rand.NextFloat(-0.45f, -0.2f)), 140,
			color, 0.55f + stage * 0.08f);
		dust.noGravity = true;
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch) => false;

	public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
	{
		Tile tile = Framing.GetTileSafely(i, j);
		if (tile.TileFrameX % StyleFrameWidth != 0 || tile.TileFrameY != 0)
		{
			return;
		}

		Vector2 forgeCenter = new(i * 16f + Width * 8f, j * 16f + Height * 8f);
		Vector2 drawPosition = forgeCenter - Main.screenPosition
			+ (Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange));
		Point16 topLeft = new(i, j);
		if (TerraforgeFormationProjectile.TryGetProgress(topLeft, out float formationProgress)
			&& formationProgress < 0.54f)
		{
			int anvilType = tile.TileFrameX / StyleFrameWidth == 1 ? ItemID.LeadAnvil : ItemID.IronAnvil;
			DrawSourceAnvil(spriteBatch, drawPosition + new Vector2(0f, 13f), anvilType);
			return;
		}

		Texture2D texture = ModContent.Request<Texture2D>("SoulsOfTerra/Content/Tiles/TerraForge").Value;
		// The supplied 62x32 sprite sits in the lower two rows of the 4x3 footprint.
		Vector2 spritePosition = drawPosition + new Vector2(0f, 8f);
		Color lightColor = Lighting.GetColor(i + 1, j + 2);
		if (formationProgress < 1f)
		{
			lightColor *= Utils.GetLerpValue(0.54f, 0.76f, formationProgress, true);
		}
		spriteBatch.Draw(texture, spritePosition, null, lightColor, 0f, texture.Size() * 0.5f, 1f, SpriteEffects.None, 0f);

		DrawEnergyGlow(spriteBatch, drawPosition, topLeft);
		if (SoulMenuSystem.TryGetTerraforgePreview(topLeft, out int previewItemType))
		{
			DrawItemPreview(spriteBatch, drawPosition, previewItemType);
		}
	}

	public override void KillMultiTile(int i, int j, int frameX, int frameY)
	{
		Point16 topLeft = new(i, j);
		SoulWorldSystem.ClearActiveTerraforge(topLeft);
		IEntitySource source = new EntitySource_TileBreak(i, j);
		int anvilItemType = frameX / StyleFrameWidth == 1 ? ItemID.LeadAnvil : ItemID.IronAnvil;
		Item.NewItem(source, i * 16, j * 16, Width * 16, Height * 16, ModContent.ItemType<TerraBladeFragment>());
		Item.NewItem(source, i * 16, j * 16, Width * 16, Height * 16, anvilItemType);
	}

	public static Point16 GetTopLeft(int i, int j)
	{
		Tile tile = Framing.GetTileSafely(i, j);
		return new Point16(i - tile.TileFrameX / 18 % Width, j - tile.TileFrameY / 18 % Height);
	}

	private static int GetVisualStage()
	{
		return SoulWorldSystem.TerraforgeTemper switch
		{
			0 => 0,
			<= 3 => 1,
			<= 8 => 2,
			_ => 3
		};
	}

	private static void DrawEnergyGlow(SpriteBatch spriteBatch, Vector2 drawPosition, Point16 topLeft)
	{
		Texture2D glow = TextureAssets.Extra[ExtrasID.SharpTears].Value;
		float pulse = 0.9f + System.MathF.Sin(Main.GlobalTimeWrappedHourly * 2.4f) * 0.08f;
		float strength = 0.12f + GetVisualStage() * 0.045f;
		if (SoulMenuSystem.IsTerraforgeOpen(topLeft))
		{
			strength *= 1.45f;
		}
		Vector2 position = drawPosition + new Vector2(0f, -8f);
		spriteBatch.Draw(glow, position, null, new Color(95, 255, 150, 0) * strength, 0f,
			glow.Size() * 0.5f, 0.32f * pulse, SpriteEffects.None, 0f);
	}

	private static void DrawItemPreview(SpriteBatch spriteBatch, Vector2 drawPosition, int itemType)
	{
		Texture2D itemTexture = TextureAssets.Item[itemType].Value;
		float itemScale = System.MathF.Min(44f / itemTexture.Width, 44f / itemTexture.Height);
		Vector2 floatPosition = drawPosition + new Vector2(0f,
			-40f + System.MathF.Sin(Main.GlobalTimeWrappedHourly * 3f) * 3f);
		spriteBatch.Draw(itemTexture, floatPosition, null, Color.White, 0f,
			itemTexture.Size() * 0.5f, itemScale, SpriteEffects.None, 0f);
	}

	private static void DrawSourceAnvil(SpriteBatch spriteBatch, Vector2 drawPosition, int itemType)
	{
		Texture2D texture = TextureAssets.Item[itemType].Value;
		float scale = System.MathF.Min(34f / texture.Width, 26f / texture.Height);
		spriteBatch.Draw(texture, drawPosition, null, Color.White, 0f,
			texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
	}
}
