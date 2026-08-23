using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SoulsOfTerra.Players;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;

namespace SoulsOfTerra.Systems;

public class SoulInterfaceSystem : ModSystem
{
	private static Asset<Texture2D> counterFrame;
	private static Asset<Texture2D> counterIcon;

	public override void Load()
	{
		if (Main.dedServ)
		{
			return;
		}

		counterFrame = ModContent.Request<Texture2D>("SoulsOfTerra/Content/UI/SoulCounterFrame");
		counterIcon = ModContent.Request<Texture2D>("SoulsOfTerra/Content/UI/SoulCounterIcon");
	}

	public override void Unload()
	{
		counterFrame = null;
		counterIcon = null;
	}

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

		// The interface layer applies UI scaling; screen coordinates keep this corner anchored.
		SoulPlayer soulPlayer = Main.LocalPlayer.GetModPlayer<SoulPlayer>();
		string balanceText = soulPlayer.SoulBalance.ToString("N0");
		Vector2 textSize = FontAssets.MouseText.Value.MeasureString(balanceText);
		float panelWidth = System.Math.Max(160f, textSize.X + 62f);
		Vector2 panelPosition = new(Main.screenWidth - panelWidth - 22f, Main.screenHeight - 66f);

		SpriteBatch spriteBatch = Main.spriteBatch;
		DrawNineSlice(spriteBatch, counterFrame.Value, new Rectangle((int)panelPosition.X, (int)panelPosition.Y, (int)panelWidth, 44));

		Texture2D icon = counterIcon.Value;
		float iconPulse = 1f + 0.035f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 3.5f);
		if (soulPlayer.RecentGainTime > 0)
		{
			iconPulse += 0.08f * MathHelper.Clamp(soulPlayer.RecentGainTime / 30f, 0f, 1f);
		}

		float iconScale = 27f / System.Math.Max(icon.Width, icon.Height) * iconPulse;
		Vector2 iconCenter = panelPosition + new Vector2(27f, 22f);
		spriteBatch.Draw(icon, iconCenter, null, Color.White, 0f, icon.Size() * 0.5f, iconScale, SpriteEffects.None, 0f);
		Utils.DrawBorderString(spriteBatch, balanceText, panelPosition + new Vector2(panelWidth - 14f, 11f), Color.White, 0.9f, 1f);

		if (soulPlayer.RecentGainTime > 0 && soulPlayer.RecentGain > 0)
		{
			float opacity = MathHelper.Clamp(soulPlayer.RecentGainTime / 30f, 0f, 1f);
			string gainText = $"+{soulPlayer.RecentGain:N0}";
			Utils.DrawBorderString(spriteBatch, gainText, panelPosition + new Vector2(panelWidth - 10f, -18f), new Color(125, 235, 255) * opacity, 0.75f, 1f);
		}

		return true;
	}

	private static void DrawNineSlice(SpriteBatch spriteBatch, Texture2D texture, Rectangle destination)
	{
		const int cornerWidth = 20;
		const int cornerHeight = 12;
		int sourceCenterWidth = texture.Width - cornerWidth * 2;
		int sourceCenterHeight = texture.Height - cornerHeight * 2;
		int destinationCenterWidth = destination.Width - cornerWidth * 2;
		int destinationCenterHeight = destination.Height - cornerHeight * 2;

		// Fixed corners and stretchable edges preserve the pixel-art frame at any balance width.
		DrawSlice(spriteBatch, texture, new Rectangle(0, 0, cornerWidth, cornerHeight), new Rectangle(destination.X, destination.Y, cornerWidth, cornerHeight));
		DrawSlice(spriteBatch, texture, new Rectangle(cornerWidth, 0, sourceCenterWidth, cornerHeight), new Rectangle(destination.X + cornerWidth, destination.Y, destinationCenterWidth, cornerHeight));
		DrawSlice(spriteBatch, texture, new Rectangle(texture.Width - cornerWidth, 0, cornerWidth, cornerHeight), new Rectangle(destination.Right - cornerWidth, destination.Y, cornerWidth, cornerHeight));
		DrawSlice(spriteBatch, texture, new Rectangle(0, cornerHeight, cornerWidth, sourceCenterHeight), new Rectangle(destination.X, destination.Y + cornerHeight, cornerWidth, destinationCenterHeight));
		DrawSlice(spriteBatch, texture, new Rectangle(cornerWidth, cornerHeight, sourceCenterWidth, sourceCenterHeight), new Rectangle(destination.X + cornerWidth, destination.Y + cornerHeight, destinationCenterWidth, destinationCenterHeight));
		DrawSlice(spriteBatch, texture, new Rectangle(texture.Width - cornerWidth, cornerHeight, cornerWidth, sourceCenterHeight), new Rectangle(destination.Right - cornerWidth, destination.Y + cornerHeight, cornerWidth, destinationCenterHeight));
		DrawSlice(spriteBatch, texture, new Rectangle(0, texture.Height - cornerHeight, cornerWidth, cornerHeight), new Rectangle(destination.X, destination.Bottom - cornerHeight, cornerWidth, cornerHeight));
		DrawSlice(spriteBatch, texture, new Rectangle(cornerWidth, texture.Height - cornerHeight, sourceCenterWidth, cornerHeight), new Rectangle(destination.X + cornerWidth, destination.Bottom - cornerHeight, destinationCenterWidth, cornerHeight));
		DrawSlice(spriteBatch, texture, new Rectangle(texture.Width - cornerWidth, texture.Height - cornerHeight, cornerWidth, cornerHeight), new Rectangle(destination.Right - cornerWidth, destination.Bottom - cornerHeight, cornerWidth, cornerHeight));
	}

	private static void DrawSlice(SpriteBatch spriteBatch, Texture2D texture, Rectangle source, Rectangle destination)
	{
		spriteBatch.Draw(texture, destination, source, Color.White);
	}
}
