using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SoulsOfTerra.Content.Projectiles;
using SoulsOfTerra.Players;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;

namespace SoulsOfTerra.Systems;

public class SoulInterfaceSystem : ModSystem
{
	private const int SoulTargetSize = 32;
	private const float SoulCompositeScale = 2f;
	private static Asset<Texture2D> counterFrame;
	private static RenderTarget2D counterSoulTarget;

	public override void Load()
	{
		if (Main.dedServ)
		{
			return;
		}

		counterFrame = ModContent.Request<Texture2D>("SoulsOfTerra/Content/UI/SoulCounterFrame");
		On_Main.CheckMonoliths += DrawCounterSoulTarget;
		Main.QueueMainThreadAction(CreateCounterSoulTarget);
	}

	public override void Unload()
	{
		On_Main.CheckMonoliths -= DrawCounterSoulTarget;
		counterFrame = null;

		RenderTarget2D targetToDispose = counterSoulTarget;
		counterSoulTarget = null;
		if (targetToDispose is not null)
		{
			// Graphics resources must be disposed on Terraria's main thread.
			Main.QueueMainThreadAction(() => targetToDispose.Dispose());
		}
	}

	private static void CreateCounterSoulTarget()
	{
		counterSoulTarget?.Dispose();
		counterSoulTarget = new RenderTarget2D(Main.instance.GraphicsDevice, SoulTargetSize, SoulTargetSize, false,
			SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
	}

	private static void DrawCounterSoulTarget(On_Main.orig_CheckMonoliths orig)
	{
		orig();
		if (Main.gameMenu || counterSoulTarget is null || counterSoulTarget.IsDisposed
			|| Main.LocalPlayer is null || !Main.LocalPlayer.active)
		{
			return;
		}

		SoulPlayer soulPlayer = Main.LocalPlayer.GetModPlayer<SoulPlayer>();
		float pickupScale = 1f;
		if (soulPlayer.RecentGainTime > 0)
		{
			pickupScale += 0.08f * MathHelper.Clamp(soulPlayer.RecentGainTime / 30f, 0f, 1f);
		}

		bool hasSouls = soulPlayer.SoulBalance > 0;
		float soulOpacity = hasSouls ? 1f : 0.28f;
		float soulScale = (hasSouls ? 0.8f : 0.68f) * pickupScale;
		GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
		RenderTargetBinding[] previousTargets = graphicsDevice.GetRenderTargets();
		graphicsDevice.SetRenderTarget(counterSoulTarget);
		graphicsDevice.Clear(Color.Transparent);

		// Half-resolution rendering gives the UI soul the same two-pixel grid as world souls.
		Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp,
			DepthStencilState.None, Main.Rasterizer, null, Matrix.CreateScale(0.5f));
		SoulOrbProjectile.DrawSoulVisualAt(new Vector2(SoulTargetSize), soulPlayer.SoulBalance, soulOpacity, soulScale);
		Main.spriteBatch.End();
		graphicsDevice.SetRenderTargets(previousTargets);
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

		Vector2 iconCenter = panelPosition + new Vector2(27f, 22f);
		if (counterSoulTarget is not null && !counterSoulTarget.IsDisposed)
		{
			spriteBatch.Draw(counterSoulTarget, iconCenter, null, Color.White, 0f,
				counterSoulTarget.Size() * 0.5f, SoulCompositeScale, SpriteEffects.None, 0f);
		}
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
