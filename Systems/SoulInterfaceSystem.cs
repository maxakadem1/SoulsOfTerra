using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Players;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace SoulsOfTerra.Systems;

public class SoulInterfaceSystem : ModSystem
{
	public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
	{
		int mouseTextIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
		if (mouseTextIndex < 0)
		{
			return;
		}

		layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
			"SoulsOfTerra: Soul Counter",
			DrawSoulCounter,
			InterfaceScaleType.UI));
	}

	private static bool DrawSoulCounter()
	{
		if (Main.gameMenu || Main.LocalPlayer is null || !Main.LocalPlayer.active)
		{
			return true;
		}

		SoulPlayer soulPlayer = Main.LocalPlayer.GetModPlayer<SoulPlayer>();
		string balanceText = soulPlayer.SoulBalance.ToString("N0");
		Vector2 textSize = FontAssets.MouseText.Value.MeasureString(balanceText);
		float panelWidth = System.Math.Max(142f, textSize.X + 54f);
		Vector2 panelPosition = new(Main.screenWidth / Main.UIScale - panelWidth - 22f, Main.screenHeight / Main.UIScale - 62f);

		SpriteBatch spriteBatch = Main.spriteBatch;
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		spriteBatch.Draw(pixel, new Rectangle((int)panelPosition.X, (int)panelPosition.Y, (int)panelWidth, 38), new Color(12, 18, 24, 205));
		spriteBatch.Draw(pixel, new Rectangle((int)panelPosition.X, (int)panelPosition.Y, (int)panelWidth, 2), new Color(80, 185, 205, 190));

		Texture2D icon = TextureAssets.Item[ItemID.SoulofLight].Value;
		Vector2 iconScale = new(24f / icon.Width, 24f / icon.Height);
		spriteBatch.Draw(icon, panelPosition + new Vector2(9f, 7f), null, Color.White, 0f, Vector2.Zero, iconScale, SpriteEffects.None, 0f);
		Utils.DrawBorderString(spriteBatch, balanceText, panelPosition + new Vector2(panelWidth - 10f, 8f), Color.White, 0.9f, 1f);

		if (soulPlayer.RecentGainTime > 0 && soulPlayer.RecentGain > 0)
		{
			float opacity = MathHelper.Clamp(soulPlayer.RecentGainTime / 30f, 0f, 1f);
			string gainText = $"+{soulPlayer.RecentGain:N0}";
			Utils.DrawBorderString(spriteBatch, gainText, panelPosition + new Vector2(panelWidth - 10f, -18f), new Color(125, 235, 255) * opacity, 0.75f, 1f);
		}

		return true;
	}
}
