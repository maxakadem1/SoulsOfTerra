using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;

namespace SoulsOfTerra.Systems;

/// <summary>Shared 2× ShopUI_full chrome used by grafting, Terraforge, and Soul Apparatus.</summary>
[Autoload(Side = ModSide.Client)]
internal sealed class ShopFullArt : ModSystem
{
	internal static Asset<Texture2D> PanelTexture { get; private set; }
	internal static Asset<Texture2D> BoxTexture { get; private set; }

	// Point-sampled draws End/Begin the batch; keep UIList scissor so scrolled rows cannot paint over tabs.
	private static readonly RasterizerState ScissorRasterizer = new()
	{
		CullMode = CullMode.None,
		ScissorTestEnable = true
	};

	public override void Load()
	{
		if (Main.dedServ)
		{
			return;
		}

		PanelTexture = ModContent.Request<Texture2D>("SoulsOfTerra/Content/UI/ShopUI_full",
			AssetRequestMode.ImmediateLoad);
		BoxTexture = ModContent.Request<Texture2D>("SoulsOfTerra/Content/UI/ShopUI_box",
			AssetRequestMode.ImmediateLoad);
	}

	public override void Unload()
	{
		PanelTexture = null;
		BoxTexture = null;
	}

	internal static void DrawPixelArt(SpriteBatch spriteBatch, Texture2D texture, Rectangle destination,
		Color color)
	{
		GraphicsDevice device = spriteBatch.GraphicsDevice;
		Rectangle previousScissor = device.ScissorRectangle;
		RasterizerState previousRasterizer = device.RasterizerState ?? Main.Rasterizer;
		bool clip = previousRasterizer.ScissorTestEnable;

		spriteBatch.End();
		BeginPixelArt(spriteBatch, device, SamplerState.PointClamp,
			clip ? ScissorRasterizer : Main.Rasterizer, clip, previousScissor);
		spriteBatch.Draw(texture, destination, color);
		spriteBatch.End();
		BeginPixelArt(spriteBatch, device, SamplerState.LinearClamp, previousRasterizer, clip,
			previousScissor);
	}

	private static void BeginPixelArt(SpriteBatch spriteBatch, GraphicsDevice device, SamplerState sampler,
		RasterizerState rasterizer, bool clip, Rectangle scissor)
	{
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, sampler,
			DepthStencilState.None, rasterizer, null, Main.UIScaleMatrix);
		if (clip)
		{
			device.ScissorRectangle = scissor;
		}
	}

	internal static void DrawBox(SpriteBatch spriteBatch, Rectangle destination, Color color)
	{
		Asset<Texture2D> texture = BoxTexture;
		if (texture is null || !texture.IsLoaded)
		{
			return;
		}

		DrawPixelArt(spriteBatch, texture.Value, destination, color);
	}
}

internal static class ShopFullLayout
{
	internal const int TextureWidth = 216;
	internal const int TextureHeight = 329;
	internal const int PanelScale = 2;
	internal const int PanelWidth = TextureWidth * PanelScale;
	internal const int PanelHeight = TextureHeight * PanelScale;
	internal const int ClipHeight = 22 * PanelScale;
	internal const int InteriorLeft = 12 * PanelScale;
	internal const int InteriorBottomInset = 14 * PanelScale;
	internal const int TitleLeft = 28;
	// Centered in the octagonal slot on the main panel's top bar (source y 25, 2×).
	internal const int TitleTop = 60;
	internal const int SubtitleTop = 82;
	internal const int BodyHeaderTop = 52;
	internal const int CloseWidth = 34;
	internal const int CloseHeight = 30;
	internal const int CloseLeft = PanelWidth - 64;
	internal const int CloseTop = BodyHeaderTop;
	internal const int TabsTop = 108;
	internal const int BodyTop = 144;
	internal const int BoxScale = 1;
	internal const int BoxWidth = 64 * BoxScale;
	internal const int BoxHeight = 61 * BoxScale;
	// Vanilla inventory occupies the left; keep ShopUI_full just to the right of it.
	internal const float InventoryRight = 660f;

	internal static void ApplyFixedSize(UIElement element)
	{
		element.Width.Set(PanelWidth, 0f);
		element.Height.Set(PanelHeight, 0f);
		element.MaxWidth.Set(PanelWidth, 0f);
		element.MaxHeight.Set(PanelHeight, 0f);
	}

	internal static void PlaceTitle(UIText title)
	{
		title.HAlign = 0.5f;
		title.Left.Set(0f, 0f);
		title.Top.Set(TitleTop, 0f);
	}

	internal static void PlaceSubtitle(UIText subtitle)
	{
		subtitle.HAlign = 0f;
		subtitle.Left.Set(TitleLeft, 0f);
		subtitle.Top.Set(SubtitleTop, 0f);
	}

	internal static void PlaceClose(UIElement close)
	{
		close.Width.Set(CloseWidth, 0f);
		close.Height.Set(CloseHeight, 0f);
		close.Left.Set(CloseLeft, 0f);
		close.Top.Set(CloseTop, 0f);
	}

	internal static bool TryPlaceBesideInventory(UIElement panel, ref float currentLeft, ref float currentTop,
		bool force = false)
	{
		float virtualWidth = Main.screenWidth / Main.UIScale;
		float panelLeft = SnapEven(Math.Min(InventoryRight, virtualWidth - PanelWidth - 12f));
		if (!force && Math.Abs(panelLeft - currentLeft) < 0.5f)
		{
			return false;
		}

		currentLeft = panelLeft;
		currentTop = 0f;
		// VAlign is resolved at Recalculate time so Y matches the UI zoom used to draw.
		panel.HAlign = 0f;
		panel.VAlign = 0.5f;
		panel.Left.Set(panelLeft, 0f);
		panel.Top.Set(0f, 0f);
		return true;
	}

	internal static void Recalculate(UIState state, UserInterface ui)
	{
		UserInterface previous = UserInterface.ActiveInstance;
		try
		{
			if (ui is not null)
			{
				UserInterface.ActiveInstance = ui;
			}
			state.Recalculate();
		}
		finally
		{
			UserInterface.ActiveInstance = previous;
		}
	}

	// Interface layers run after SetZoom_UI. Recalculate here so the first draw uses
	// UI-space metrics instead of the world-zoom Recalculate from SetState/Update.
	internal static void Draw(UserInterface ui, UIState state, UIElement panel, ref float currentLeft,
		ref float currentTop)
	{
		if (ui?.CurrentState is null)
		{
			return;
		}

		TryPlaceBesideInventory(panel, ref currentLeft, ref currentTop, force: true);
		UserInterface previous = UserInterface.ActiveInstance;
		try
		{
			UserInterface.ActiveInstance = ui;
			state.Recalculate();
			ui.Draw(Main.spriteBatch, new GameTime());
		}
		finally
		{
			UserInterface.ActiveInstance = previous;
		}
	}

	internal static float SnapEven(float value) => MathF.Round(value * 0.5f) * 2f;
}

internal class ShopFullPanel : UIElement
{
	internal int SoulEffectSeed { get; set; }

	public ShopFullPanel()
	{
		HAlign = 0f;
		VAlign = 0.5f;
		ShopFullLayout.ApplyFixedSize(this);
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		CalculatedStyle dimensions = GetDimensions();
		Asset<Texture2D> texture = ShopFullArt.PanelTexture;
		if (texture is not null && texture.IsLoaded)
		{
			ShopFullArt.DrawPixelArt(spriteBatch, texture.Value, new Rectangle(
				(int)MathF.Round(dimensions.X),
				(int)MathF.Round(dimensions.Y),
				ShopFullLayout.PanelWidth,
				ShopFullLayout.PanelHeight), Color.White);
		}

		Rectangle soulPanel = new((int)MathF.Round(dimensions.X), (int)MathF.Round(dimensions.Y),
			ShopFullLayout.PanelWidth, ShopFullLayout.PanelHeight);
		soulPanel.Y += ShopFullLayout.ClipHeight;
		soulPanel.Height -= ShopFullLayout.ClipHeight;
		UICornerSoulRenderer.Draw(spriteBatch, soulPanel, SoulEffectSeed);
	}
}

internal sealed class ShopFullCloseElement : UIElement
{
	public bool Hovered { get; set; }

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		CalculatedStyle dimensions = GetDimensions();
		Color color = Hovered ? SoullessUIPalette.AccentText : SoullessUIPalette.TextSecondary;
		Utils.DrawBorderString(spriteBatch, "×", dimensions.Center(), color, 0.82f, 0.5f, 0.5f);
	}
}
