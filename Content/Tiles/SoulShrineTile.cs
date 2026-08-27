using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Content.Projectiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.ObjectInteractions;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace SoulsOfTerra.Content.Tiles;

public class SoulShrineTile : ModTile
{
	private const int ArtOffsetX = -21;
	private const int ArtOffsetY = -52;
	private static readonly Vector2 SocketOffset = new(46f, 36f);
	public static readonly Vector2 SocketWorldOffset = new(ArtOffsetX + SocketOffset.X, ArtOffsetY + SocketOffset.Y);
	public override string Texture => "SoulsOfTerra/Content/Tiles/SoulShrine";

	public override void SetStaticDefaults()
	{
		Main.tileFrameImportant[Type] = true;
		Main.tileNoAttach[Type] = true;
		Main.tileLavaDeath[Type] = false;
		TileID.Sets.DisableSmartCursor[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
		TileObjectData.newTile.Origin = new Point16(1, 1);
		TileObjectData.newTile.LavaDeath = false;
		TileObjectData.addTile(Type);

		DustType = DustID.Stone;
		HitSound = SoundID.Tink;
		AddMapEntry(new Color(45, 63, 63), CreateMapEntryName());
	}

	public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;

	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
	{
		// Keep the dormant shrine subtle so its summoning flare can provide the contrast.
		r = 0.015f;
		g = 0.11f;
		b = 0.1f;
	}

	public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
	{
		Tile tile = Main.tile[i, j];
		if (tile.TileFrameX != 0 || tile.TileFrameY != 0)
		{
			return false;
		}

		Texture2D texture = TextureAssets.Tile[Type].Value;
		Vector2 screenOffset = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
		Vector2 drawPosition = new Vector2(i * 16 + ArtOffsetX, j * 16 + ArtOffsetY) - Main.screenPosition + screenOffset;
		Vector2 socketPosition = drawPosition + SocketOffset;
		Texture2D glow = SoulOrbProjectile.GetGlowTexture();
		Texture2D ring = SoulOrbProjectile.GetRingTexture();
		Vector2 glowOrigin = glow.Size() * 0.5f;
		float time = Main.GlobalTimeWrappedHourly;
		float pulse = 1f + MathF.Sin(time * 2.25f) * 0.075f;

		// A broad haze and brighter inner bloom make the socket readable against dark court walls.
		spriteBatch.Draw(glow, socketPosition, null, new Color(28, 185, 174, 0) * 0.19f,
			0f, glowOrigin, 1.3f * pulse, SpriteEffects.None, 0f);
		spriteBatch.Draw(glow, socketPosition, null, new Color(62, 238, 211, 0) * 0.4f,
			0f, glowOrigin, 0.88f * pulse, SpriteEffects.None, 0f);
		spriteBatch.Draw(ring, socketPosition, null, new Color(125, 255, 232, 92),
			time * 0.18f, glowOrigin, 0.5f * pulse, SpriteEffects.None, 0f);

		Color lightColor = Lighting.GetColor(i + 1, j + 1);
		spriteBatch.Draw(texture, drawPosition, null, lightColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

		for (int mote = 0; mote < 3; mote++)
		{
			float angle = time * (0.42f + mote * 0.07f) + mote * MathHelper.TwoPi / 3f;
			Vector2 offset = new(MathF.Cos(angle) * 17f, MathF.Sin(angle) * 7f);
			float moteScale = 0.065f + 0.018f * MathF.Sin(angle * 1.8f);
			spriteBatch.Draw(glow, socketPosition + offset, null, new Color(180, 255, 239, 0) * 0.52f,
				0f, glowOrigin, moteScale, SpriteEffects.None, 0f);
		}

		return false;
	}
}
