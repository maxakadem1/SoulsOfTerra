using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SoulsOfTerra.Common;
using SoulsOfTerra.Common.Rendering;
using SoulsOfTerra.Content.Items.Access;
using SoulsOfTerra.Content.Items.Consumables.SoulCrystals;
using SoulsOfTerra.Content.Items.Materials;
using SoulsOfTerra.Content.Tiles;
using SoulsOfTerra.NPCs;
using SoulsOfTerra.Players;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace SoulsOfTerra.Systems;

public class SoulMenuSystem : ModSystem
{
	private static UserInterface soulInterface;
	private static SoulMenuState menuState;

	internal static UserInterface Interface => soulInterface;
	internal static bool IsOpen => soulInterface?.CurrentState == menuState && menuState is not null;

	public override void Load()
	{
		if (Main.dedServ)
		{
			return;
		}

		soulInterface = new UserInterface();
		menuState = new SoulMenuState();
		menuState.Activate();
	}

	public override void Unload()
	{
		soulInterface = null;
		menuState = null;
	}

	public static void OpenSoulless(int npcIndex)
	{
		if (Main.dedServ || menuState is null)
		{
			return;
		}

		menuState.ConfigureSoulless(npcIndex);
		soulInterface.SetState(menuState);
		SoulSpellBookSystem.Close();
		SoulApparatusSystem.Close();
		GraftingAltarSystem.Close();
	}

	public static void OpenTerraforge(Point16 terraforgePosition)
	{
		if (Main.dedServ || menuState is null)
		{
			return;
		}

		Main.playerInventory = true;
		menuState.ConfigureTerraforge(terraforgePosition);
		soulInterface.SetState(menuState);
		SoulSpellBookSystem.Close();
		SoulApparatusSystem.Close();
		GraftingAltarSystem.Close();
	}

	public static void Close()
	{
		soulInterface?.SetState(null);
	}

	public static bool TryGetTerraforgePreview(Point16 terraforgeTopLeft, out int itemType)
	{
		itemType = ItemID.None;
		return soulInterface?.CurrentState == menuState && menuState is not null
			&& menuState.TryGetTerraforgePreview(terraforgeTopLeft, out itemType);
	}

	public static bool IsTerraforgeOpen(Point16 terraforgeTopLeft)
	{
		return soulInterface?.CurrentState == menuState && menuState is not null
			&& menuState.IsTerraforgeOpen(terraforgeTopLeft);
	}

	internal static bool IsImbuementResonating()
	{
		return soulInterface?.CurrentState == menuState && menuState is not null
			&& menuState.IsImbuementResonating();
	}

	public override void UpdateUI(GameTime gameTime)
	{
		if (soulInterface?.CurrentState is not null)
		{
			soulInterface.Update(gameTime);
		}
	}

	public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
	{
		int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
		if (mouseTextIndex < 0)
		{
			return;
		}

		layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
			"SoulsOfTerra: Soul Menus",
			() =>
			{
				menuState?.DrawInterface();
				return true;
			},
			InterfaceScaleType.UI));
	}
}

internal sealed class SoulMenuFramePanel : UIPanel
{
	private const int TileSize = 32;
	private const int FrameInset = TileSize / 2;
	private const int HeaderWidth = 180;
	private const int HeaderTopOffset = -36;
	internal int SoulEffectSeed { get; set; }

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		CalculatedStyle dimensions = GetDimensions();
		Rectangle frame = new((int)dimensions.X, (int)dimensions.Y, (int)dimensions.Width, (int)dimensions.Height);
		DrawFrame(spriteBatch, frame, BackgroundColor);
		UICornerSoulRenderer.Draw(spriteBatch, frame, SoulEffectSeed);
	}

	internal static void DrawFrame(SpriteBatch spriteBatch, Rectangle frame, Color backgroundColor)
	{
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Texture2D corner = ModContent.Request<Texture2D>("SoulsOfTerra/Content/UI/ShopUI_corner").Value;
		Texture2D horizontalEdge = ModContent.Request<Texture2D>("SoulsOfTerra/Content/UI/ShopUI_top_bottom").Value;
		Texture2D verticalEdge = ModContent.Request<Texture2D>("SoulsOfTerra/Content/UI/ShopUI_left_right").Value;
		Texture2D header = ModContent.Request<Texture2D>("SoulsOfTerra/Content/UI/Shop_UI_header").Value;

		// The inset fill leaves the transparent outer corner shapes intact.
		Rectangle interior = new(frame.X + FrameInset, frame.Y + FrameInset, frame.Width - FrameInset * 2, frame.Height - FrameInset * 2);
		spriteBatch.Draw(pixel, interior, backgroundColor);

		DrawHorizontalEdge(spriteBatch, horizontalEdge, frame.X + TileSize, frame.Right - TileSize, frame.Y, SpriteEffects.None);
		DrawHorizontalEdge(spriteBatch, horizontalEdge, frame.X + TileSize, frame.Right - TileSize, frame.Bottom - TileSize, SpriteEffects.FlipVertically);
		DrawVerticalEdge(spriteBatch, verticalEdge, frame.Y + TileSize, frame.Bottom - TileSize, frame.X, SpriteEffects.None);
		DrawVerticalEdge(spriteBatch, verticalEdge, frame.Y + TileSize, frame.Bottom - TileSize, frame.Right - TileSize, SpriteEffects.FlipHorizontally);

		// One authored corner is mirrored to keep all four corners pixel-identical.
		spriteBatch.Draw(corner, new Vector2(frame.X, frame.Y), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
		spriteBatch.Draw(corner, new Vector2(frame.Right - TileSize, frame.Y), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.FlipHorizontally, 0f);
		spriteBatch.Draw(corner, new Vector2(frame.X, frame.Bottom - TileSize), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.FlipVertically, 0f);
		spriteBatch.Draw(corner, new Vector2(frame.Right - TileSize, frame.Bottom - TileSize), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically, 0f);

		Vector2 headerPosition = new(frame.Center.X - HeaderWidth / 2f, frame.Y + HeaderTopOffset);
		spriteBatch.Draw(header, headerPosition, Color.White);
	}

	private static void DrawHorizontalEdge(SpriteBatch spriteBatch, Texture2D texture, int startX, int endX, int y, SpriteEffects effects)
	{
		for (int x = startX; x < endX; x += TileSize)
		{
			int width = Math.Min(TileSize, endX - x);
			Rectangle source = effects.HasFlag(SpriteEffects.FlipHorizontally)
				? new Rectangle(TileSize - width, 0, width, TileSize)
				: new Rectangle(0, 0, width, TileSize);
			spriteBatch.Draw(texture, new Rectangle(x, y, width, TileSize), source, Color.White, 0f, Vector2.Zero, effects, 0f);
		}
	}

	private static void DrawVerticalEdge(SpriteBatch spriteBatch, Texture2D texture, int startY, int endY, int x, SpriteEffects effects)
	{
		for (int y = startY; y < endY; y += TileSize)
		{
			int height = Math.Min(TileSize, endY - y);
			Rectangle source = effects.HasFlag(SpriteEffects.FlipVertically)
				? new Rectangle(0, TileSize - height, TileSize, height)
				: new Rectangle(0, 0, TileSize, height);
			spriteBatch.Draw(texture, new Rectangle(x, y, TileSize, height), source, Color.White, 0f, Vector2.Zero, effects, 0f);
		}
	}
}

internal sealed class SoulMenuState : UIState
{
	private enum MenuKind
	{
		Soulless,
		Terraforge
	}

	private enum SoullessTab
	{
		Services,
		Crystals
	}

	private enum TerraforgeTab
	{
		Condense,
		Imbue
	}

	private const int FeedbackDuration = 180;
	private const float ImbuementRecipeListTop = 25f;
	private const float ImbuementRecipeListHeight = ShopFullLayout.PanelHeight - ShopFullLayout.BodyTop
		- ShopFullLayout.InteriorBottomInset - ImbuementRecipeListTop;
	private SoulMenuFramePanel tiledPanel;
	private ShopFullPanel fullPanel;
	private UIElement panel;
	private ShopFullCloseElement fullClose;
	private UIText title;
	private UIText subtitle;
	private UIText balance;
	private SoulActionRow primaryRow;
	private SoulActionRow secondaryRow;
	private SoulActionRow tertiaryRow;
	private SoulActionRow quaternaryRow;
	private SoulActionRow graftingRow;
	private UITextPanel<string> servicesTabButton;
	private UITextPanel<string> crystalsTabButton;
	private UITextPanel<string> condensationTabButton;
	private UITextPanel<string> imbuementTabButton;
	private UIElement crystalGrid;
	private SoulEssenceCard[] crystalCards;
	private UIElement essenceGrid;
	private UIList essenceList;
	private UIScrollbar essenceScrollBar;
	private SoulEssenceCatalogueCard[] essenceCards;
	private SoulEssenceDefinition[] essenceDefinitions;
	private UIPanel essenceDetails;
	private SoulItemIcon detailIcon;
	private UIText detailName;
	private UIText detailDescription;
	private UIText detailCost;
	private UITextPanel<string> condenseButton;
	private ImbuementRitualCanvas imbuementContent;
	private ImbuementWeaponSocket imbuementWeaponSocket;
	private ImbuementEssenceSocket imbuementEssenceSocket;
	private UIText imbuementWeaponName;
	private UIText imbuementEssenceName;
	private UIText ritualBalance;
	private UITextPanel<string> bindEssenceButton;
	private UITextPanel<string> imbuementRecipesButton;
	private UIElement imbuementRecipeContent;
	private UIElement imbuementRecipeListContainer;
	private UIList imbuementRecipeList;
	private UIScrollbar imbuementRecipeScrollBar;
	private UIText imbuementRecipeHint;
	private readonly List<ImbuementRecipeRow> imbuementRecipeRows = new();
	private readonly List<int> visibleImbuementRecipeIndices = new();
	private UIText feedback;
	private UITextPanel<string> closeButton;
	private MenuKind kind;
	private SoullessTab soullessTab;
	private TerraforgeTab terraforgeTab;
	private int npcIndex;
	private Point16 terraforgePosition;
	private int feedbackTime;
	private int selectedEssenceIndex;
	private int selectedCrystalIndex;
	private int selectedImbuementIndex;
	private int linkedWeaponSlot;
	private int linkedEssenceSlot;
	private bool showingImbuementRecipes;
	private float ritualReveal;
	private float currentPanelLeft;
	private float currentPanelTop;

	public override void OnActivate()
	{
		ApplyTerraforgePlacement(force: true);
	}

	internal void DrawInterface()
	{
		if (SoulMenuSystem.Interface?.CurrentState != this)
		{
			return;
		}

		if (kind == MenuKind.Terraforge)
		{
			ShopFullLayout.Draw(SoulMenuSystem.Interface, this, fullPanel, ref currentPanelLeft,
				ref currentPanelTop);
			return;
		}

		SoulMenuSystem.Interface.Draw(Main.spriteBatch, new GameTime());
	}

	public override void OnInitialize()
	{
		tiledPanel = new SoulMenuFramePanel();
		tiledPanel.Width.Set(540f, 0f);
		tiledPanel.Height.Set(340f, 0f);
		tiledPanel.HAlign = 0.5f;
		tiledPanel.VAlign = 0.5f;
		tiledPanel.BackgroundColor = SoullessUIPalette.Panel;
		tiledPanel.BorderColor = SoullessUIPalette.PanelBorder;
		panel = tiledPanel;
		Append(panel);

		fullPanel = new ShopFullPanel();
		fullClose = new ShopFullCloseElement();
		ShopFullLayout.PlaceClose(fullClose);
		fullClose.OnMouseOver += (_, _) => fullClose.Hovered = true;
		fullClose.OnMouseOut += (_, _) => fullClose.Hovered = false;
		fullClose.OnLeftClick += (_, _) => SoulMenuSystem.Close();

		title = new UIText(string.Empty, 1.05f);
		title.Left.Set(20f, 0f);
		title.Top.Set(14f, 0f);
		panel.Append(title);

		subtitle = new UIText(string.Empty, 0.72f);
		subtitle.Left.Set(21f, 0f);
		subtitle.Top.Set(43f, 0f);
		subtitle.TextColor = SoullessUIPalette.TextSecondary;
		panel.Append(subtitle);

		balance = new UIText(string.Empty, 0.82f);
		balance.HAlign = 1f;
		balance.Left.Set(-20f, 0f);
		balance.Top.Set(22f, 0f);
		balance.TextColor = SoullessUIPalette.AccentText;
		panel.Append(balance);

		primaryRow = CreateRow(78f);
		primaryRow.SetAction(UsePrimaryAction);
		secondaryRow = CreateRow(168f);
		secondaryRow.SetAction(UseSecondaryAction);
		tertiaryRow = CreateRow(258f);
		tertiaryRow.SetAction(UseQuaternaryAction);
		quaternaryRow = CreateRow(348f);
		quaternaryRow.SetAction(UseTertiaryAction);
		graftingRow = CreateRow(438f);
		graftingRow.SetAction(UseGraftingAltarAction);
		CreateSoullessTabs();
		CreateTerraforgeTabs();
		CreateEssenceCatalogue();
		CreateImbuementPage();
		CreateImbuementRecipePage();
		CreateCrystalCatalogue();

		feedback = new UIText(string.Empty, 0.72f);
		feedback.HAlign = 0.5f;
		feedback.Top.Set(266f, 0f);
		feedback.TextColor = SoullessUIPalette.TextPrimary;
		panel.Append(feedback);

		closeButton = new UITextPanel<string>("Close", 0.72f, false);
		closeButton.Width.Set(92f, 0f);
		closeButton.Height.Set(32f, 0f);
		closeButton.HAlign = 0.5f;
		closeButton.Top.Set(294f, 0f);
		closeButton.BackgroundColor = SoullessUIPalette.SurfaceRaised;
		closeButton.BorderColor = SoullessUIPalette.Steel;
		closeButton.OnMouseOver += (_, _) =>
		{
			closeButton.BackgroundColor = SoullessUIPalette.SurfaceHover;
			closeButton.BorderColor = SoullessUIPalette.AccentHoverBorder;
		};
		closeButton.OnMouseOut += (_, _) =>
		{
			closeButton.BackgroundColor = SoullessUIPalette.SurfaceRaised;
			closeButton.BorderColor = SoullessUIPalette.Steel;
		};
		closeButton.OnLeftClick += (_, _) => SoulMenuSystem.Close();
		panel.Append(closeButton);
	}

	public void ConfigureSoulless(int requestedNpcIndex)
	{
		kind = MenuKind.Soulless;
		npcIndex = requestedNpcIndex;
		tiledPanel.SoulEffectSeed = unchecked(requestedNpcIndex * 486187739 + 17);
		panel = tiledPanel;
		ApplySoullessHeaderPositions();
		soullessTab = SoullessTab.Services;
		selectedCrystalIndex = 0;
		BuildSoullessLayout();
		ClearFeedback();
		RefreshContent();
	}

	public void ConfigureTerraforge(Point16 requestedTerraforgePosition)
	{
		kind = MenuKind.Terraforge;
		terraforgePosition = requestedTerraforgePosition;
		fullPanel.SoulEffectSeed = unchecked(requestedTerraforgePosition.X * 73856093
			^ requestedTerraforgePosition.Y * 19349663);
		panel = fullPanel;
		ApplyTerraforgeHeaderPositions();
		terraforgeTab = TerraforgeTab.Condense;
		linkedWeaponSlot = -1;
		linkedEssenceSlot = -1;
		showingImbuementRecipes = false;
		selectedImbuementIndex = -1;
		selectedEssenceIndex = 0;
		essenceScrollBar.ViewPosition = 0f;
		BuildTerraforgeLayout();
		ClearFeedback();
		RefreshContent();
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		Player player = Main.LocalPlayer;
		if (!player.active || player.dead || !InteractionStillValid(player) || Main.keyState.IsKeyDown(Keys.Escape))
		{
			SoulMenuSystem.Close();
			return;
		}

		ApplyTerraforgePlacement();
		if ((panel.Parent is not null && panel.ContainsPoint(Main.MouseScreen))
			|| (imbuementContent.Parent is not null && imbuementContent.ContainsPoint(Main.MouseScreen)))
		{
			player.mouseInterface = true;
		}

		if (imbuementContent.Parent is not null && ritualReveal < 1f)
		{
			ritualReveal = Math.Min(1f, ritualReveal + 0.075f);
			float easedReveal = 1f - MathF.Pow(1f - ritualReveal, 3f);
			imbuementContent.Reveal = easedReveal;
			imbuementContent.Top.Set(ShopFullLayout.BodyTop + (1f - easedReveal) * 18f, 0f);
			ShopFullLayout.Recalculate(this, SoulMenuSystem.Interface);
		}

		if (feedbackTime > 0 && --feedbackTime == 0)
		{
			feedback.SetText(string.Empty);
		}

		RefreshContent();
	}

	private SoulActionRow CreateRow(float top)
	{
		SoulActionRow row = new();
		row.Width.Set(-32f, 1f);
		row.Height.Set(80f, 0f);
		row.Left.Set(16f, 0f);
		row.Top.Set(top, 0f);
		return row;
	}

	private void BuildSoullessLayout()
	{
		panel = tiledPanel;
		ShowFramedPanel();
		panel.RemoveAllChildren();
		ApplySoullessHeaderPositions();
		tiledPanel.Height.Set(soullessTab == SoullessTab.Services ? 670f : 400f, 0f);
		panel.Append(title);
		panel.Append(subtitle);
		panel.Append(balance);
		panel.Append(servicesTabButton);
		panel.Append(crystalsTabButton);
		if (soullessTab == SoullessTab.Services)
		{
			primaryRow.Top.Set(108f, 0f);
			secondaryRow.Top.Set(198f, 0f);
			tertiaryRow.Top.Set(288f, 0f);
			graftingRow.Top.Set(378f, 0f);
			quaternaryRow.Top.Set(468f, 0f);
			panel.Append(primaryRow);
			panel.Append(secondaryRow);
			panel.Append(tertiaryRow);
			panel.Append(quaternaryRow);
			panel.Append(graftingRow);
		}
		else
		{
			crystalGrid.Top.Set(112f, 0f);
			panel.Append(crystalGrid);
			essenceDetails.Top.Set(184f, 0f);
			panel.Append(essenceDetails);
			condenseButton.Top.Set(276f, 0f);
			panel.Append(condenseButton);
		}

		feedback.Top.Set(soullessTab == SoullessTab.Services ? 596f : 326f, 0f);
		panel.Append(feedback);
		closeButton.Top.Set(soullessTab == SoullessTab.Services ? 628f : 358f, 0f);
		panel.Append(closeButton);
		ApplySoullessTabStyles();
	}

	private void BuildTerraforgeLayout()
	{
		if (terraforgeTab == TerraforgeTab.Imbue && !showingImbuementRecipes)
		{
			BuildImbuementRitualLayout();
			return;
		}

		panel = fullPanel;
		ShowFramedPanel();
		panel.RemoveAllChildren();
		AppendTerraforgeHeader();
		panel.Append(condensationTabButton);
		panel.Append(imbuementTabButton);
		if (terraforgeTab == TerraforgeTab.Condense)
		{
			panel.Append(essenceGrid);
			condenseButton.Top.Set(540f, 0f);
			panel.Append(condenseButton);
		}
		else
		{
			panel.Append(imbuementRecipeContent);
		}
		feedback.Top.Set(590f, 0f);
		panel.Append(feedback);
		ApplyTerraforgeTabStyles();
		ApplyTerraforgePlacement(force: true);
	}

	private void ShowFramedPanel()
	{
		feedback.Remove();
		RemoveAllChildren();
		Append(panel);
	}

	private void ApplyTerraforgePlacement(bool force = false)
	{
		if (kind != MenuKind.Terraforge)
		{
			return;
		}

		if (!ShopFullLayout.TryPlaceBesideInventory(fullPanel, ref currentPanelLeft, ref currentPanelTop, force))
		{
			return;
		}

		ShopFullLayout.Recalculate(this, SoulMenuSystem.Interface);
	}

	private void AppendTerraforgeHeader()
	{
		ApplyTerraforgeHeaderPositions();
		panel.Append(title);
		panel.Append(subtitle);
		panel.Append(balance);
		panel.Append(fullClose);
	}

	private void ApplySoullessHeaderPositions()
	{
		title.HAlign = 0f;
		title.Left.Set(20f, 0f);
		title.Top.Set(14f, 0f);
		subtitle.HAlign = 0f;
		subtitle.Left.Set(21f, 0f);
		subtitle.Top.Set(43f, 0f);
		balance.HAlign = 1f;
		balance.Left.Set(-20f, 0f);
		balance.Top.Set(22f, 0f);
	}

	private void ApplyTerraforgeHeaderPositions()
	{
		ShopFullLayout.PlaceTitle(title);
		ShopFullLayout.PlaceSubtitle(subtitle);
		balance.HAlign = 1f;
		balance.Left.Set(-68f, 0f);
		balance.Top.Set(ShopFullLayout.BodyHeaderTop, 0f);
		condensationTabButton.Left.Set(ShopFullLayout.InteriorLeft, 0f);
		condensationTabButton.Top.Set(ShopFullLayout.TabsTop, 0f);
		imbuementTabButton.Left.Set(ShopFullLayout.InteriorLeft + 138f, 0f);
		imbuementTabButton.Top.Set(ShopFullLayout.TabsTop, 0f);
	}

	private void BuildImbuementRitualLayout()
	{
		panel = fullPanel;
		ShowFramedPanel();
		panel.RemoveAllChildren();
		AppendTerraforgeHeader();
		panel.Append(condensationTabButton);
		panel.Append(imbuementTabButton);
		ApplyTerraforgeTabStyles();
		imbuementContent.RemoveAllChildren();
		imbuementContent.DrawHeader = false;
		imbuementContent.Width.Set(ShopFullLayout.PanelWidth - ShopFullLayout.InteriorLeft * 2f, 0f);
		imbuementContent.Height.Set(430f, 0f);
		imbuementContent.Left.Set(ShopFullLayout.InteriorLeft, 0f);
		imbuementContent.Top.Set(ShopFullLayout.BodyTop, 0f);
		imbuementContent.HAlign = 0f;
		imbuementContent.VAlign = 0f;
		panel.Append(imbuementContent);
		imbuementContent.Append(imbuementWeaponSocket);
		imbuementContent.Append(imbuementWeaponName.Parent);
		imbuementContent.Append(imbuementEssenceSocket);
		imbuementContent.Append(imbuementEssenceName.Parent);
		imbuementContent.Append(ritualBalance);

		imbuementRecipesButton.Left.Set(20f, 0f);
		imbuementRecipesButton.Top.Set(340f, 0f);
		imbuementContent.Append(imbuementRecipesButton);
		bindEssenceButton.Left.Set(176f, 0f);
		bindEssenceButton.Top.Set(340f, 0f);
		imbuementContent.Append(bindEssenceButton);

		feedback.Top.Set(390f, 0f);
		imbuementContent.Append(feedback);
		ritualReveal = 0f;
		imbuementContent.Reveal = 0f;
		imbuementContent.Top.Set(ShopFullLayout.BodyTop + 18f, 0f);
		ApplyTerraforgePlacement(force: true);
	}

	private void CreateEssenceCatalogue()
	{
		// The shared registry keeps UI, server validation, and multiplayer IDs in one stable order.
		essenceDefinitions = SoulEssenceRegistry.Definitions;

		essenceGrid = new UIElement();
		essenceGrid.Width.Set(ShopFullLayout.PanelWidth - ShopFullLayout.InteriorLeft * 2f, 0f);
		essenceGrid.Height.Set(380f, 0f);
		essenceGrid.Left.Set(ShopFullLayout.InteriorLeft, 0f);
		essenceGrid.Top.Set(ShopFullLayout.BodyTop, 0f);

		essenceList = new UIList();
		essenceList.Width.Set(0f, 1f);
		essenceList.Height.Set(0f, 1f);
		essenceList.ListPadding = 2f;
		essenceGrid.Append(essenceList);

		essenceScrollBar = CreateForgeScrollBar();
		essenceList.SetScrollbar(essenceScrollBar);

		int rowCount = (essenceDefinitions.Length + 4) / 5;
		if (rowCount > 4)
		{
			essenceList.Width.Set(-26f, 1f);
			essenceGrid.Append(essenceScrollBar);
		}

		const int columns = 5;
		const int boxGap = 16;
		float rowHeight = ShopFullLayout.BoxHeight + 32f;
		essenceCards = new SoulEssenceCatalogueCard[essenceDefinitions.Length];
		for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
		{
			UIElement row = new();
			row.Width.Set(0f, 1f);
			row.Height.Set(rowHeight, 0f);
			essenceList.Add(row);

			for (int columnIndex = 0; columnIndex < columns; columnIndex++)
			{
				int index = rowIndex * columns + columnIndex;
				if (index >= essenceDefinitions.Length)
				{
					break;
				}

				int selectedIndex = index;
				SoulEssenceCatalogueCard card = new();
				card.Width.Set(ShopFullLayout.BoxWidth, 0f);
				card.Height.Set(rowHeight, 0f);
				card.Left.Set(columnIndex * (ShopFullLayout.BoxWidth + boxGap), 0f);
				card.OnLeftClick += (_, _) => SelectEssence(selectedIndex);
				essenceCards[index] = card;
				row.Append(card);
			}
		}

		essenceDetails = new UIPanel();
		essenceDetails.Width.Set(-32f, 1f);
		essenceDetails.Height.Set(82f, 0f);
		essenceDetails.Left.Set(16f, 0f);
		essenceDetails.Top.Set(204f, 0f);
		essenceDetails.BackgroundColor = SoullessUIPalette.Surface;
		essenceDetails.BorderColor = SoullessUIPalette.SteelMuted;

		detailIcon = new SoulItemIcon();
		detailIcon.Width.Set(52f, 0f);
		detailIcon.Height.Set(52f, 0f);
		detailIcon.Left.Set(8f, 0f);
		detailIcon.VAlign = 0.5f;
		essenceDetails.Append(detailIcon);

		detailName = new UIText(string.Empty, 0.82f);
		detailName.Left.Set(68f, 0f);
		detailName.Top.Set(9f, 0f);
		essenceDetails.Append(detailName);

		detailDescription = new UIText(string.Empty, 0.64f);
		detailDescription.Left.Set(68f, 0f);
		detailDescription.Top.Set(34f, 0f);
		detailDescription.TextColor = SoullessUIPalette.TextSecondary;
		essenceDetails.Append(detailDescription);

		detailCost = new UIText(string.Empty, 0.68f);
		detailCost.Left.Set(68f, 0f);
		detailCost.Top.Set(57f, 0f);
		essenceDetails.Append(detailCost);

		condenseButton = new UITextPanel<string>("Condense", 0.76f, false);
		condenseButton.Width.Set(220f, 0f);
		condenseButton.Height.Set(38f, 0f);
		condenseButton.HAlign = 0.5f;
		condenseButton.Top.Set(294f, 0f);
		condenseButton.OnLeftClick += (_, _) => UsePrimaryAction();
		condenseButton.OnMouseOver += (_, _) =>
		{
			if (IsCurrentSelectionAvailable())
			{
				condenseButton.BackgroundColor = HasEnoughForCurrentSelection()
					? SoullessUIPalette.AccentSurfaceHover : SoullessUIPalette.WarningSurfaceHover;
			}
		};
		condenseButton.OnMouseOut += (_, _) => ApplyCurrentActionButtonStyle();
	}

	private void CreateCrystalCatalogue()
	{
		crystalGrid = new UIElement();
		crystalGrid.Width.Set(-32f, 1f);
		crystalGrid.Height.Set(62f, 0f);
		crystalGrid.Left.Set(16f, 0f);

		crystalCards = new SoulEssenceCard[3];
		for (int index = 0; index < crystalCards.Length; index++)
		{
			int selectedIndex = index;
			SoulEssenceCard card = new();
			card.Width.Set(160f, 0f);
			card.Height.Set(58f, 0f);
			card.Left.Set(4f + index * 166f, 0f);
			card.OnLeftClick += (_, _) => SelectCrystal(selectedIndex);
			crystalCards[index] = card;
			crystalGrid.Append(card);
		}
	}

	private void SelectEssence(int index)
	{
		selectedEssenceIndex = index;
		ClearFeedback();
		RefreshTerraforgeContent();
	}

	private void RefreshContent()
	{
		string balanceText = $"{Main.LocalPlayer.GetModPlayer<SoulPlayer>().SoulBalance:N0} souls";
		balance.SetText(balanceText);
		ritualBalance.SetText(balanceText);
		if (kind == MenuKind.Soulless)
		{
			if (soullessTab == SoullessTab.Services)
			{
				RefreshSoullessContent();
			}
			else
			{
				RefreshCrystalContent();
			}
		}
		else
		{
			RefreshTerraforgeContent();
		}
	}

	private void RefreshSoullessContent()
	{
		title.SetText("Soulless");
		subtitle.SetText("Trade in that which clings to you.");
		int fragmentType = ModContent.ItemType<TerraBladeFragment>();
		if (!SoulWorldSystem.TerraBladeFragmentPurchased)
		{
			bool canAfford = Main.LocalPlayer.GetModPlayer<SoulPlayer>().SoulBalance >= SoulTransactions.FragmentCost;
			primaryRow.SetContent(fragmentType, "Terra Blade Fragment",
				$"Forms the Terraforge  •  {SoulTransactions.FragmentCost:N0} souls", "Acquire", true, canAfford);
		}
		else if (SoulWorldSystem.HasActiveTerraforge)
		{
			primaryRow.SetContent(fragmentType, "Terraforge",
				$"Temper {SoulWorldSystem.TerraforgeTemper}  •  Active", "Active", false);
		}
		else if (Main.LocalPlayer.HasItem(fragmentType))
		{
			primaryRow.SetContent(fragmentType, "Terra Blade Fragment",
				"The fragment is in your possession", "Carried", false);
		}
		else
		{
			primaryRow.SetContent(fragmentType, "Recall the Fragment",
				"Draw the ancient fragment back  •  No cost", "Recall", true);
		}

		long temperCost = SoulWorldSystem.GetNextTemperCost();
		if (!SoulWorldSystem.TerraBladeFragmentPurchased)
		{
			secondaryRow.SetContent(fragmentType, "Temper the Fragment", "Acquire the fragment first", "Locked", false);
		}
		else if (temperCost <= 0)
		{
			secondaryRow.SetContent(fragmentType, "Terraforge", "The fragment is fully tempered", "Complete", false);
		}
		else
		{
			bool milestoneUnlocked = SoulWorldSystem.IsNextTemperUnlocked();
			bool canAfford = Main.LocalPlayer.GetModPlayer<SoulPlayer>().SoulBalance >= temperCost;
			string detail = milestoneUnlocked
				? $"Temper {SoulWorldSystem.TerraforgeTemper + 1}  •  {temperCost:N0} souls"
				: $"Requires {GetNextMilestoneName()}";
			secondaryRow.SetContent(fragmentType, "Temper the Fragment", detail,
				milestoneUnlocked ? "Temper" : "Locked", milestoneUnlocked, canAfford);
		}

		bool apparatusUnlocked = NPC.downedBoss1;
		bool canAffordApparatus = Main.LocalPlayer.GetModPlayer<SoulPlayer>().SoulBalance >= SoulTransactions.SoulApparatusCost;
		tertiaryRow.SetContent(
			ModContent.ItemType<SoulApparatusItem>(),
			"Soul Apparatus",
			apparatusUnlocked ? $"Dissolves potions into soulspells  •  {SoulTransactions.SoulApparatusCost:N0} souls" : "Requires Eye of Cthulhu",
			apparatusUnlocked ? "Purchase" : "Locked",
			apparatusUnlocked,
			canAffordApparatus);

		bool keyUnlocked = NPC.downedBoss3;
		bool canAffordKey = Main.LocalPlayer.GetModPlayer<SoulPlayer>().SoulBalance >= SoulTransactions.WardensFragmentCost;
		quaternaryRow.SetContent(
			ModContent.ItemType<WardensFragment>(),
			"Warden's Fragment",
			keyUnlocked ? $"Reusable Buried Court key  •  {SoulTransactions.WardensFragmentCost:N0} souls" : "Requires Skeletron",
			keyUnlocked ? "Purchase" : "Locked",
			keyUnlocked,
			canAffordKey);

		bool graftingUnlocked = NPC.downedSlimeKing || NPC.downedBoss1;
		bool canAffordGraftingAltar = Main.LocalPlayer.GetModPlayer<SoulPlayer>().SoulBalance >= SoulTransactions.GraftingAltarCost;
		graftingRow.SetContent(
			ModContent.ItemType<GraftingAltarItem>(),
			"Grafting Altar",
			graftingUnlocked ? $"Embeds Essences into the body  •  {SoulTransactions.GraftingAltarCost:N0} souls"
				: "Requires King Slime or Eye of Cthulhu",
			graftingUnlocked ? "Purchase" : "Locked",
			graftingUnlocked,
			canAffordGraftingAltar);
	}

	private void RefreshCrystalContent()
	{
		title.SetText("Soulless");
		subtitle.SetText("I can bind what death would otherwise reclaim.");
		int[] itemTypes =
		{
			ModContent.ItemType<FaintSoulCrystal>(),
			ModContent.ItemType<VividSoulCrystal>(),
			ModContent.ItemType<ProfoundSoulCrystal>()
		};
		string[] names = { "Faint", "Vivid", "Profound" };

		for (int index = 0; index < crystalCards.Length; index++)
		{
			bool unlocked = SoulTransactions.IsSoulCrystalUnlocked(index);
			crystalCards[index].SetContent(itemTypes[index], unlocked ? names[index] : "Unknown", unlocked, selectedCrystalIndex == index);
		}

		bool selectedUnlocked = SoulTransactions.IsSoulCrystalUnlocked(selectedCrystalIndex);
		long crystalValue = SoulTransactions.GetSoulCrystalValue(selectedCrystalIndex);
		long conversionCost = SoulTransactions.GetSoulCrystalCost(selectedCrystalIndex);
		detailIcon.ItemType = selectedUnlocked ? itemTypes[selectedCrystalIndex] : ItemID.FallenStar;
		detailIcon.Opacity = selectedUnlocked ? 1f : 0.25f;
		detailName.SetText(selectedUnlocked ? $"{names[selectedCrystalIndex]} Soul Crystal" : "Unknown Crystal");
		detailDescription.SetText(selectedUnlocked ? $"A tradable vessel containing {crystalValue:N0} souls." : "Soulless withholds this deeper art.");
		detailCost.SetText(selectedUnlocked ? $"Cost: {conversionCost:N0} souls  •  Contains: {crystalValue:N0}" : GetSoulCrystalRequirement());
		detailCost.TextColor = selectedUnlocked && !HasEnoughForSelectedCrystal()
			? SoullessUIPalette.Warning : SoullessUIPalette.AccentText;
		ApplyCurrentActionButtonStyle();
	}

	private void RefreshTerraforgeContent()
	{
		if (terraforgeTab == TerraforgeTab.Imbue)
		{
			RefreshImbuementContent();
			return;
		}

		title.SetText("Terraforge");
		subtitle.SetText($"Temper: {SoulWorldSystem.TerraforgeTemper}");
		long balanceValue = Main.LocalPlayer.GetModPlayer<SoulPlayer>().SoulBalance;
		for (int index = 0; index < essenceCards.Length; index++)
		{
			SoulEssenceDefinition definition = essenceDefinitions[index];
			bool unlocked = definition.IsUnlocked();
			essenceCards[index].SetContent(
				definition.ItemType,
				definition.Name,
				definition.Cost,
				unlocked,
				balanceValue >= definition.Cost,
				selectedEssenceIndex == index);
		}

		ApplyCurrentActionButtonStyle();
	}

	private void RefreshImbuementContent()
	{
		title.SetText("Terraforge");
		if (showingImbuementRecipes)
		{
			subtitle.SetText("Every binding is listed. Locked recipes stay visible.");
			RefreshImbuementRecipeRows();
			return;
		}

		subtitle.SetText("Bind a defeated foe's echo into its weapon.");
		if (!InventorySlotAvailable(linkedWeaponSlot))
		{
			linkedWeaponSlot = -1;
		}
		if (!InventorySlotAvailable(linkedEssenceSlot))
		{
			linkedEssenceSlot = -1;
		}

		Item weapon = linkedWeaponSlot >= 0 ? Main.LocalPlayer.inventory[linkedWeaponSlot] : null;
		Item essence = linkedEssenceSlot >= 0 ? Main.LocalPlayer.inventory[linkedEssenceSlot] : null;
		bool validCombination = weapon is not null && essence is not null
			&& EssenceImbuementRegistry.TryFind(weapon.type, essence.type, out selectedImbuementIndex, out _);
		if (!validCombination)
		{
			selectedImbuementIndex = -1;
		}

		imbuementWeaponSocket.SetItem(weapon?.type ?? ItemID.None, validCombination);
		imbuementEssenceSocket.SetItem(essence?.type ?? ItemID.None);
		imbuementWeaponName.SetText(weapon?.Name ?? "Select Weapon");
		imbuementEssenceName.SetText(essence?.Name ?? "Select Essence");
		ApplyBindButtonStyle();
	}

	private bool InventorySlotAvailable(int slot)
	{
		return slot >= 0 && slot < Main.LocalPlayer.inventory.Length
			&& Main.LocalPlayer.inventory[slot].stack > 0
			&& !Main.LocalPlayer.inventory[slot].IsAir;
	}

	private bool InventorySlotMatches(int slot, int requiredType)
	{
		return slot >= 0 && slot < Main.LocalPlayer.inventory.Length
			&& Main.LocalPlayer.inventory[slot].type == requiredType
			&& Main.LocalPlayer.inventory[slot].stack > 0;
	}

	private bool CanBindSelectedImbuement()
	{
		return EssenceImbuementRegistry.TryGet(selectedImbuementIndex, out EssenceImbuementDefinition imbuement)
			&& SoulEssenceRegistry.TryFindByItemType(imbuement.EssenceItemType, out SoulEssenceDefinition essence)
			&& essence.IsUnlocked()
			&& linkedWeaponSlot != linkedEssenceSlot
			&& InventorySlotMatchesImbuement(linkedWeaponSlot, imbuement)
			&& InventorySlotMatches(linkedEssenceSlot, imbuement.EssenceItemType);
	}

	private static bool InventorySlotMatchesImbuement(int slot, EssenceImbuementDefinition imbuement)
	{
		return slot >= 0 && slot < Main.LocalPlayer.inventory.Length
			&& Main.LocalPlayer.inventory[slot].stack > 0
			&& imbuement.AcceptsInput(Main.LocalPlayer.inventory[slot].type);
	}

	private void ApplyBindButtonStyle()
	{
		bool enabled = CanBindSelectedImbuement();
		bindEssenceButton.SetText(enabled ? "Bind Essence" : "No Resonance");
		bindEssenceButton.BackgroundColor = enabled ? SoullessUIPalette.AccentSurface : SoullessUIPalette.SurfaceDisabled;
		bindEssenceButton.BorderColor = enabled ? SoullessUIPalette.Accent : SoullessUIPalette.SteelMuted;
		bindEssenceButton.TextColor = enabled ? SoullessUIPalette.AccentText : SoullessUIPalette.TextMuted;
	}

	private void UsePrimaryAction()
	{
		if (kind == MenuKind.Soulless)
		{
			if (soullessTab == SoullessTab.Crystals)
			{
				UseCrystalConversion();
				return;
			}

			if (!SoulWorldSystem.TerraBladeFragmentPurchased)
			{
				if (!HasSouls(SoulTransactions.FragmentCost))
				{
					return;
				}

				bool purchased = SendNpcTransaction(SoulMessageType.RequestFragmentPurchase,
					() => SoulTransactions.TryPurchaseTerraBladeFragment(Main.LocalPlayer, npcIndex));
				Main.npcChatText = Language.GetTextValue("Mods.SoulsOfTerra.Dialogue.Soulless.FragmentSale");
				ShowFeedback(purchased ? "Terra Blade Fragment acquired." : "Purchase request sent.", true);
				return;
			}

			if (SoulWorldSystem.HasActiveTerraforge || Main.LocalPlayer.HasItem(ModContent.ItemType<TerraBladeFragment>()))
			{
				return;
			}

			bool recalled = SendNpcTransaction(SoulMessageType.RequestFragmentRecall,
				() => SoulTransactions.TryRecallTerraBladeFragment(Main.LocalPlayer, npcIndex));
			ShowFeedback(recalled ? "The fragment returns." : "Recall request sent.", true);
		}
		else
		{
			if (selectedEssenceIndex < 0 || selectedEssenceIndex >= essenceDefinitions.Length)
			{
				ShowFeedback("This echo has not awakened.", false);
				return;
			}

			if (!IsSelectedEssenceUnlocked())
			{
				ShowFeedback(essenceDefinitions[selectedEssenceIndex].GetRequirement(), false);
				return;
			}

			long essenceCost = GetSelectedEssenceCost();
			if (!HasSouls(essenceCost))
			{
				return;
			}

			bool completed = SendTerraforgeTransaction();
			string essenceName = essenceDefinitions[selectedEssenceIndex].Name;
			ShowFeedback(completed ? $"{essenceName} condensed." : "Condense request sent.", true);
		}
	}

	private void UseCrystalConversion()
	{
		if (!SoulTransactions.IsSoulCrystalUnlocked(selectedCrystalIndex))
		{
			ShowFeedback(GetSoulCrystalRequirement(), false);
			return;
		}

		long cost = SoulTransactions.GetSoulCrystalCost(selectedCrystalIndex);
		if (!HasSouls(cost))
		{
			return;
		}

		bool completed = SendCrystalTransaction();
		ShowFeedback(completed ? "Soul Crystal bound." : "Conversion request sent.", true);
	}

	private void UseEssenceImbuement()
	{
		if (!CanBindSelectedImbuement())
		{
			ShowFeedback("Select both required items from your inventory.", false);
			return;
		}

		bool completed = SendImbuementTransaction();
		linkedWeaponSlot = -1;
		linkedEssenceSlot = -1;
		selectedImbuementIndex = -1;
		showingImbuementRecipes = true;
		RebuildImbuementRecipes();
		BuildTerraforgeLayout();
		ShowFeedback(completed ? "The binding ritual has begun." : "Binding request sent.", true);
		RefreshContent();
	}

	private bool HasEnoughForSelectedEssence()
	{
		return Main.LocalPlayer.GetModPlayer<SoulPlayer>().SoulBalance >= GetSelectedEssenceCost();
	}

	private bool HasEnoughForSelectedCrystal()
	{
		return Main.LocalPlayer.GetModPlayer<SoulPlayer>().SoulBalance >= SoulTransactions.GetSoulCrystalCost(selectedCrystalIndex);
	}

	private void SelectCrystal(int index)
	{
		selectedCrystalIndex = index;
		ClearFeedback();
		RefreshCrystalContent();
	}

	private void CreateSoullessTabs()
	{
		servicesTabButton = CreateTabButton("Services", 16f, SoullessTab.Services);
		crystalsTabButton = CreateTabButton("Soul Crystals", 144f, SoullessTab.Crystals);
	}

	private void CreateTerraforgeTabs()
	{
		condensationTabButton = CreateTerraforgeTabButton("Condense", 16f, TerraforgeTab.Condense);
		imbuementTabButton = CreateTerraforgeTabButton("Imbue", 154f, TerraforgeTab.Imbue);
	}

	private static UIScrollbar CreateForgeScrollBar()
	{
		// Vanilla scrollbar textures are 20px; a narrower width stretches the track and leaves the thumb offset.
		UIScrollbar scrollbar = new FixedUIScrollbar(SoulMenuSystem.Interface);
		scrollbar.Height.Set(0f, 1f);
		scrollbar.HAlign = 1f;
		return scrollbar;
	}

	private UITextPanel<string> CreateTerraforgeTabButton(string text, float left, TerraforgeTab tab)
	{
		UITextPanel<string> button = new(text, 0.68f, false);
		button.Width.Set(130f, 0f);
		button.Height.Set(30f, 0f);
		button.Left.Set(left, 0f);
		button.Top.Set(68f, 0f);
		button.OnLeftClick += (_, _) => SetTerraforgeTab(tab);
		button.OnMouseOut += (_, _) => ApplyTerraforgeTabStyles();
		return button;
	}

	private void CreateImbuementPage()
	{
		imbuementContent = new ImbuementRitualCanvas();
		imbuementContent.DrawHeader = false;
		imbuementContent.Width.Set(ShopFullLayout.PanelWidth - ShopFullLayout.InteriorLeft * 2f, 0f);
		imbuementContent.Height.Set(430f, 0f);

		imbuementWeaponSocket = new ImbuementWeaponSocket();
		imbuementWeaponSocket.HAlign = 0.5f;
		imbuementWeaponSocket.Top.Set(28f, 0f);

		imbuementWeaponName = new UIText("Select Weapon", 0.57f);
		imbuementWeaponName.HAlign = 0.5f;
		imbuementWeaponName.VAlign = 0.5f;
		imbuementWeaponName.TextColor = SoullessUIPalette.TextPrimary;
		CreateRitualLabelPlate(imbuementWeaponName, 108f, 250f);

		imbuementEssenceSocket = new ImbuementEssenceSocket();
		imbuementEssenceSocket.HAlign = 0.5f;
		imbuementEssenceSocket.Top.Set(155f, 0f);

		imbuementEssenceName = new UIText("Select Essence", 0.54f);
		imbuementEssenceName.HAlign = 0.5f;
		imbuementEssenceName.VAlign = 0.5f;
		imbuementEssenceName.TextColor = SoullessUIPalette.TextSecondary;
		CreateRitualLabelPlate(imbuementEssenceName, 230f, 220f);

		ritualBalance = new UIText(string.Empty, 0.64f);
		ritualBalance.HAlign = 1f;
		ritualBalance.Left.Set(-8f, 0f);
		ritualBalance.Top.Set(4f, 0f);
		ritualBalance.TextColor = SoullessUIPalette.AccentMuted;

		bindEssenceButton = new UITextPanel<string>("Bind Essence", 0.76f, false);
		bindEssenceButton.Width.Set(178f, 0f);
		bindEssenceButton.Height.Set(36f, 0f);
		bindEssenceButton.OnLeftClick += (_, _) => UseEssenceImbuement();
		bindEssenceButton.OnMouseOver += (_, _) =>
		{
			if (CanBindSelectedImbuement())
			{
				bindEssenceButton.BackgroundColor = SoullessUIPalette.AccentSurfaceHover;
			}
		};
		bindEssenceButton.OnMouseOut += (_, _) => ApplyBindButtonStyle();
	}

	private static void CreateRitualLabelPlate(UIText label, float top, float width)
	{
		RitualLabelPlate plate = new();
		plate.Width.Set(width, 0f);
		plate.Height.Set(30f, 0f);
		plate.HAlign = 0.5f;
		plate.Top.Set(top, 0f);
		plate.Append(label);
	}

	private void CreateImbuementRecipePage()
	{
		imbuementRecipesButton = new UITextPanel<string>("Back to Recipes", 0.64f, false);
		imbuementRecipesButton.Width.Set(140f, 0f);
		imbuementRecipesButton.Height.Set(36f, 0f);
		imbuementRecipesButton.Left.Set(292f, 0f);
		imbuementRecipesButton.Top.Set(68f, 0f);
		imbuementRecipesButton.BackgroundColor = SoullessUIPalette.SurfaceRaised;
		imbuementRecipesButton.BorderColor = SoullessUIPalette.Steel;
		imbuementRecipesButton.OnMouseOver += (_, _) =>
		{
			imbuementRecipesButton.BackgroundColor = SoullessUIPalette.SurfaceHover;
			imbuementRecipesButton.BorderColor = SoullessUIPalette.AccentHoverBorder;
		};
		imbuementRecipesButton.OnMouseOut += (_, _) =>
		{
			imbuementRecipesButton.BackgroundColor = SoullessUIPalette.SurfaceRaised;
			imbuementRecipesButton.BorderColor = SoullessUIPalette.Steel;
		};
		imbuementRecipesButton.OnLeftClick += (_, _) => OpenImbuementRecipes();

		imbuementRecipeContent = new UIElement();
		imbuementRecipeContent.Width.Set(ShopFullLayout.PanelWidth - ShopFullLayout.InteriorLeft * 2f, 0f);
		imbuementRecipeContent.Height.Set(ImbuementRecipeListTop + ImbuementRecipeListHeight, 0f);
		imbuementRecipeContent.Left.Set(ShopFullLayout.InteriorLeft, 0f);
		imbuementRecipeContent.Top.Set(ShopFullLayout.BodyTop, 0f);

		imbuementRecipeHint = new UIText("Select a complete recipe to begin its binding ritual.", 0.62f);
		imbuementRecipeHint.TextColor = SoullessUIPalette.TextSecondary;
		imbuementRecipeContent.Append(imbuementRecipeHint);

		imbuementRecipeListContainer = new UIElement();
		imbuementRecipeListContainer.Width.Set(0f, 1f);
		imbuementRecipeListContainer.Height.Set(ImbuementRecipeListHeight, 0f);
		imbuementRecipeListContainer.Top.Set(ImbuementRecipeListTop, 0f);
		imbuementRecipeContent.Append(imbuementRecipeListContainer);

		imbuementRecipeList = new UIList();
		// Match the apparatus inset so animated borders remain inside the scissor area.
		imbuementRecipeList.Left.Set(3f, 0f);
		imbuementRecipeList.Width.Set(-6f, 1f);
		imbuementRecipeList.Height.Set(0f, 1f);
		imbuementRecipeList.ListPadding = 5f;
		imbuementRecipeListContainer.Append(imbuementRecipeList);

		imbuementRecipeScrollBar = CreateForgeScrollBar();
		imbuementRecipeList.SetScrollbar(imbuementRecipeScrollBar);

	}

	private UITextPanel<string> CreateTabButton(string text, float left, SoullessTab tab)
	{
		UITextPanel<string> button = new(text, 0.68f, false);
		button.Width.Set(120f, 0f);
		button.Height.Set(30f, 0f);
		button.Left.Set(left, 0f);
		button.Top.Set(68f, 0f);
		button.OnLeftClick += (_, _) => SetSoullessTab(tab);
		button.OnMouseOut += (_, _) => ApplySoullessTabStyles();
		return button;
	}

	private void SetSoullessTab(SoullessTab tab)
	{
		if (kind != MenuKind.Soulless || soullessTab == tab)
		{
			return;
		}

		soullessTab = tab;
		ClearFeedback();
		BuildSoullessLayout();
		RefreshContent();
	}

	private void SetTerraforgeTab(TerraforgeTab tab)
	{
		if (kind != MenuKind.Terraforge || terraforgeTab == tab)
		{
			return;
		}

		terraforgeTab = tab;
		showingImbuementRecipes = tab == TerraforgeTab.Imbue;
		if (showingImbuementRecipes)
		{
			RebuildImbuementRecipes();
		}
		ClearFeedback();
		BuildTerraforgeLayout();
		RefreshContent();
	}

	private void ApplyTerraforgeTabStyles()
	{
		ApplyTabStyle(condensationTabButton, terraforgeTab == TerraforgeTab.Condense);
		ApplyTabStyle(imbuementTabButton, terraforgeTab == TerraforgeTab.Imbue);
	}

	private void OpenImbuementRecipes()
	{
		showingImbuementRecipes = true;
		linkedWeaponSlot = -1;
		linkedEssenceSlot = -1;
		selectedImbuementIndex = -1;
		RebuildImbuementRecipes();
		ClearFeedback();
		BuildTerraforgeLayout();
		RefreshContent();
	}

	private void RebuildImbuementRecipes()
	{
		imbuementRecipeList.Clear();
		imbuementRecipeRows.Clear();
		visibleImbuementRecipeIndices.Clear();

		// List every registered binding from the first visit so players can preview later rewards.
		for (int index = 0; index < EssenceImbuementRegistry.Definitions.Length; index++)
		{
			EssenceImbuementDefinition definition = EssenceImbuementRegistry.Definitions[index];
			if (!SoulEssenceRegistry.TryFindByItemType(definition.EssenceItemType, out _))
			{
				continue;
			}

			int recipeIndex = index;
			ImbuementRecipeRow row = new();
			row.Width.Set(0f, 1f);
			row.Height.Set(72f, 0f);
			row.SetAction(() => SelectImbuementRecipe(recipeIndex));
			imbuementRecipeList.Add(row);
			imbuementRecipeRows.Add(row);
			visibleImbuementRecipeIndices.Add(index);
		}

		imbuementRecipeHint.SetText("Select a complete recipe to begin its binding ritual.");

		// The expanded catalogue fits six complete rows before scrolling.
		bool needsScrollBar = imbuementRecipeRows.Count > 6;
		imbuementRecipeList.Width.Set(needsScrollBar ? -29f : -6f, 1f);
		if (needsScrollBar && imbuementRecipeScrollBar.Parent is null)
		{
			imbuementRecipeListContainer.Append(imbuementRecipeScrollBar);
		}
		else if (!needsScrollBar)
		{
			imbuementRecipeScrollBar.Remove();
		}
	}

	private void RefreshImbuementRecipeRows()
	{
		for (int rowIndex = 0; rowIndex < imbuementRecipeRows.Count; rowIndex++)
		{
			EssenceImbuementDefinition definition = EssenceImbuementRegistry.Definitions[visibleImbuementRecipeIndices[rowIndex]];
			SoulEssenceRegistry.TryFindByItemType(definition.EssenceItemType, out SoulEssenceDefinition essence);
			int weaponSlot = FindInventorySlot(definition);
			int essenceSlot = FindInventorySlot(definition.EssenceItemType);
			bool essenceUnlocked = essence is not null && essence.IsUnlocked();
			bool ready = essenceUnlocked && weaponSlot >= 0 && essenceSlot >= 0;
			string status = GetImbuementRecipeStatus(definition, essence, weaponSlot, essenceSlot, ready);
			imbuementRecipeRows[rowIndex].SetContent(definition, ready, status);
		}
	}

	private void SelectImbuementRecipe(int recipeIndex)
	{
		if (!EssenceImbuementRegistry.TryGet(recipeIndex, out EssenceImbuementDefinition definition)
			|| !SoulEssenceRegistry.TryFindByItemType(definition.EssenceItemType, out SoulEssenceDefinition essence)
			|| !essence.IsUnlocked())
		{
			return;
		}

		int weaponSlot = FindInventorySlot(definition);
		int essenceSlot = FindInventorySlot(definition.EssenceItemType);
		if (weaponSlot < 0 || essenceSlot < 0)
		{
			return;
		}

		linkedWeaponSlot = weaponSlot;
		linkedEssenceSlot = essenceSlot;
		selectedImbuementIndex = recipeIndex;
		showingImbuementRecipes = false;
		BuildTerraforgeLayout();
		ShowFeedback("The ingredients resonate.", true);
		RefreshContent();
	}

	private static int FindInventorySlot(int itemType)
	{
		for (int slot = 0; slot < Main.LocalPlayer.inventory.Length; slot++)
		{
			Item item = Main.LocalPlayer.inventory[slot];
			if (!item.IsAir && item.stack > 0 && item.type == itemType)
			{
				return slot;
			}
		}

		return -1;
	}

	private static int FindInventorySlot(EssenceImbuementDefinition imbuement)
	{
		for (int slot = 0; slot < Main.LocalPlayer.inventory.Length; slot++)
		{
			Item item = Main.LocalPlayer.inventory[slot];
			if (!item.IsAir && item.stack > 0 && imbuement.AcceptsInput(item.type))
			{
				return slot;
			}
		}

		return -1;
	}

	private static string GetImbuementRecipeStatus(EssenceImbuementDefinition definition,
		SoulEssenceDefinition essence, int weaponSlot, int essenceSlot, bool ready)
	{
		if (ready)
		{
			return "Ready — select recipe";
		}

		if (essence is not null && !essence.IsUnlocked())
		{
			return essence.GetRequirement();
		}

		if (weaponSlot < 0 && essenceSlot < 0)
		{
			return $"Missing {definition.InputDisplayName} and {Lang.GetItemNameValue(definition.EssenceItemType)}";
		}

		return weaponSlot < 0
			? $"Missing {definition.InputDisplayName}"
			: $"Missing {Lang.GetItemNameValue(definition.EssenceItemType)}";
	}

	private void ApplySoullessTabStyles()
	{
		ApplyTabStyle(servicesTabButton, soullessTab == SoullessTab.Services);
		ApplyTabStyle(crystalsTabButton, soullessTab == SoullessTab.Crystals);
	}

	private static void ApplyTabStyle(UITextPanel<string> button, bool selected)
	{
		button.BackgroundColor = selected ? SoullessUIPalette.AccentSurface : SoullessUIPalette.SurfaceRaised;
		button.BorderColor = selected ? SoullessUIPalette.Accent : SoullessUIPalette.SteelMuted;
		button.TextColor = selected ? SoullessUIPalette.AccentText : SoullessUIPalette.TextSecondary;
	}

	private void ApplyCurrentActionButtonStyle()
	{
		bool available = IsCurrentSelectionAvailable();
		bool affordable = available && HasEnoughForCurrentSelection();
		string actionText;
		if (kind == MenuKind.Soulless)
		{
			actionText = available ? "Convert" : "Locked";
		}
		else if (selectedEssenceIndex < 0 || selectedEssenceIndex >= essenceDefinitions.Length)
		{
			actionText = "Select an Essence";
		}
		else if (!available)
		{
			actionText = "Locked";
		}
		else if (!affordable)
		{
			long missingSouls = GetSelectedEssenceCost() - Main.LocalPlayer.GetModPlayer<SoulPlayer>().SoulBalance;
			actionText = $"Need {missingSouls:N0} More Souls";
		}
		else
		{
			actionText = $"Condense {essenceDefinitions[selectedEssenceIndex].Name}";
		}

		condenseButton.SetText(actionText);
		condenseButton.BackgroundColor = !available ? SoullessUIPalette.SurfaceDisabled
			: affordable ? SoullessUIPalette.AccentSurface : SoullessUIPalette.WarningSurface;
		condenseButton.BorderColor = !available ? SoullessUIPalette.SteelMuted
			: affordable ? SoullessUIPalette.Accent : SoullessUIPalette.WarningBorder;
		condenseButton.TextColor = !available ? SoullessUIPalette.TextMuted
			: affordable ? SoullessUIPalette.AccentText : SoullessUIPalette.WarningText;
	}

	private bool IsCurrentSelectionAvailable()
	{
		return kind == MenuKind.Soulless
			? soullessTab == SoullessTab.Crystals && SoulTransactions.IsSoulCrystalUnlocked(selectedCrystalIndex)
			: IsSelectedEssenceUnlocked();
	}

	private bool HasEnoughForCurrentSelection()
	{
		return kind == MenuKind.Soulless ? HasEnoughForSelectedCrystal() : HasEnoughForSelectedEssence();
	}

	private void UseSecondaryAction()
	{
		if (kind != MenuKind.Soulless || soullessTab != SoullessTab.Services
			|| !SoulWorldSystem.TerraBladeFragmentPurchased || SoulWorldSystem.GetNextTemperCost() <= 0)
		{
			return;
		}

		if (!SoulWorldSystem.IsNextTemperUnlocked())
		{
			ShowFeedback($"Defeat {GetNextMilestoneName()} first.", false);
			return;
		}

		long cost = SoulWorldSystem.GetNextTemperCost();
		if (!HasSouls(cost))
		{
			return;
		}

		bool completed = SendNpcTransaction(SoulMessageType.RequestTerraforgeTemper,
			() => SoulTransactions.TryTemperTerraforge(Main.LocalPlayer, npcIndex));
		ShowFeedback(completed ? "The fragment accepts a deeper temper." : "Tempering request sent.", true);
	}

	private bool HasSouls(long required)
	{
		if (Main.LocalPlayer.GetModPlayer<SoulPlayer>().SoulBalance >= required)
		{
			return true;
		}

		ShowFeedback($"You need {required:N0} souls.", false);
		return false;
	}

	private bool SendNpcTransaction(SoulMessageType messageType, Func<bool> singlePlayerAction)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			ModPacket packet = ModContent.GetInstance<SoulsOfTerra>().GetPacket();
			packet.Write((byte)messageType);
			packet.Write((short)npcIndex);
			packet.Send();
			return false;
		}

		return singlePlayerAction();
	}

	private void UseTertiaryAction()
	{
		if (kind != MenuKind.Soulless || soullessTab != SoullessTab.Services)
		{
			return;
		}

		if (!NPC.downedBoss3)
		{
			ShowFeedback("Defeat Skeletron first.", false);
			return;
		}

		if (!HasSouls(SoulTransactions.WardensFragmentCost))
		{
			return;
		}

		bool completed = SendNpcTransaction(SoulMessageType.RequestWardenFragmentPurchase,
			() => SoulTransactions.TryPurchaseWardensFragment(Main.LocalPlayer, npcIndex));
		ShowFeedback(completed ? "Warden's Fragment acquired." : "Purchase request sent.", true);
	}

	private void UseQuaternaryAction()
	{
		if (kind != MenuKind.Soulless || soullessTab != SoullessTab.Services)
		{
			return;
		}

		if (!NPC.downedBoss1)
		{
			ShowFeedback("Defeat the Eye of Cthulhu first.", false);
			return;
		}

		if (!HasSouls(SoulTransactions.SoulApparatusCost))
		{
			return;
		}

		bool completed = SendNpcTransaction(SoulMessageType.RequestSoulApparatusPurchase,
			() => SoulTransactions.TryPurchaseSoulApparatus(Main.LocalPlayer, npcIndex));
		ShowFeedback(completed ? "Soul Apparatus acquired." : "Purchase request sent.", true);
	}

	private void UseGraftingAltarAction()
	{
		if (kind != MenuKind.Soulless || soullessTab != SoullessTab.Services)
		{
			return;
		}

		if (!(NPC.downedSlimeKing || NPC.downedBoss1))
		{
			ShowFeedback("Defeat King Slime or the Eye of Cthulhu first.", false);
			return;
		}

		if (!HasSouls(SoulTransactions.GraftingAltarCost))
		{
			return;
		}

		bool completed = SendNpcTransaction(SoulMessageType.RequestGraftingAltarPurchase,
			() => SoulTransactions.TryPurchaseGraftingAltar(Main.LocalPlayer, npcIndex));
		ShowFeedback(completed ? "Grafting Altar acquired." : "Purchase request sent.", true);
	}

	private bool SendCrystalTransaction()
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			ModPacket packet = ModContent.GetInstance<SoulsOfTerra>().GetPacket();
			packet.Write((byte)SoulMessageType.RequestSoulCrystalConversion);
			packet.Write((short)npcIndex);
			packet.Write((byte)selectedCrystalIndex);
			packet.Send();
			return false;
		}

		return SoulTransactions.TryConvertSoulCrystal(Main.LocalPlayer, npcIndex, selectedCrystalIndex);
	}

	private bool SendTerraforgeTransaction()
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			ModPacket packet = ModContent.GetInstance<SoulsOfTerra>().GetPacket();
			packet.Write((byte)SoulMessageType.RequestEssenceCondensation);
			packet.Write((byte)selectedEssenceIndex);
			packet.Write(terraforgePosition.X);
			packet.Write(terraforgePosition.Y);
			packet.Send();
			return false;
		}

		return SoulTransactions.TryCondenseEssence(Main.LocalPlayer, terraforgePosition, selectedEssenceIndex);
	}

	private bool SendImbuementTransaction()
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			ModPacket packet = ModContent.GetInstance<SoulsOfTerra>().GetPacket();
			packet.Write((byte)SoulMessageType.RequestEssenceImbuement);
			packet.Write((byte)selectedImbuementIndex);
			packet.Write((byte)linkedWeaponSlot);
			packet.Write((byte)linkedEssenceSlot);
			packet.Write(terraforgePosition.X);
			packet.Write(terraforgePosition.Y);
			packet.Send();
			return false;
		}

		return SoulTransactions.TryBeginEssenceImbuement(Main.LocalPlayer, terraforgePosition,
			selectedImbuementIndex, linkedWeaponSlot, linkedEssenceSlot);
	}

	private bool IsSelectedEssenceUnlocked()
	{
		return selectedEssenceIndex >= 0
			&& selectedEssenceIndex < essenceDefinitions.Length
			&& essenceDefinitions[selectedEssenceIndex].IsUnlocked();
	}

	private long GetSelectedEssenceCost()
	{
		return selectedEssenceIndex >= 0 && selectedEssenceIndex < essenceDefinitions.Length
			? essenceDefinitions[selectedEssenceIndex].Cost
			: 0;
	}

	private string GetSoulCrystalRequirement()
	{
		return selectedCrystalIndex switch
		{
			1 => "Requires Terraforge Temper 1",
			2 => "Requires Terraforge Temper 4",
			_ => string.Empty
		};
	}

	private void ShowFeedback(string message, bool success)
	{
		feedback.SetText(message);
		feedback.TextColor = success ? SoullessUIPalette.AccentMuted : SoullessUIPalette.Warning;
		feedbackTime = FeedbackDuration;
	}

	private void ClearFeedback()
	{
		feedbackTime = 0;
		feedback?.SetText(string.Empty);
	}

	private string GetNextMilestoneName()
	{
		return SoulWorldSystem.TerraforgeTemper switch
		{
			0 => "the Eye of Cthulhu",
			1 => "the world's evil boss",
			2 => "Skeletron",
			3 => "the Wall of Flesh",
			4 => "all three mechanical bosses",
			5 => "Plantera",
			6 => "Golem",
			7 => "the Lunatic Cultist",
			8 => "the Moon Lord",
			_ => "the next great foe"
		};
	}

	public bool TryGetTerraforgePreview(Point16 requestedTerraforge, out int itemType)
	{
		itemType = ItemID.None;
		if (kind != MenuKind.Terraforge || terraforgeTab != TerraforgeTab.Imbue || requestedTerraforge != terraforgePosition
			|| !InventorySlotAvailable(linkedWeaponSlot))
		{
			return false;
		}

		itemType = Main.LocalPlayer.inventory[linkedWeaponSlot].type;
		return true;
	}

	public bool IsTerraforgeOpen(Point16 requestedTerraforge)
	{
		return kind == MenuKind.Terraforge && requestedTerraforge == terraforgePosition;
	}

	internal bool IsImbuementResonating()
	{
		return kind == MenuKind.Terraforge && terraforgeTab == TerraforgeTab.Imbue
			&& !showingImbuementRecipes && selectedImbuementIndex >= 0;
	}

	private bool InteractionStillValid(Player player)
	{
		const float rangeSquared = 12f * 16f * 12f * 16f;
		if (kind == MenuKind.Soulless)
		{
			return npcIndex >= 0 && npcIndex < Main.maxNPCs && Main.npc[npcIndex].active
				&& Main.npc[npcIndex].type == ModContent.NPCType<SoullessNPC>()
				&& Vector2.DistanceSquared(player.Center, Main.npc[npcIndex].Center) <= rangeSquared;
		}

		Tile tile = Framing.GetTileSafely(terraforgePosition.X, terraforgePosition.Y);
		return tile.HasTile && tile.TileType == ModContent.TileType<TerraforgeTile>()
			&& Main.playerInventory
			&& Vector2.DistanceSquared(player.Center, terraforgePosition.ToWorldCoordinates(32f, 24f)) <= rangeSquared;
	}
}

internal sealed class SoulActionRow : UIElement
{
	private readonly UIPanel background;
	private readonly SoulItemIcon icon;
	private readonly UIText name;
	private readonly UIText detail;
	private readonly UITextPanel<string> actionButton;
	private Action action;
	private bool enabled;
	private bool affordable;

	public SoulActionRow()
	{
		background = new UIPanel();
		background.Width.Set(0f, 1f);
		background.Height.Set(0f, 1f);
		background.BackgroundColor = SoullessUIPalette.Surface;
		background.BorderColor = SoullessUIPalette.SteelMuted;
		Append(background);

		icon = new SoulItemIcon();
		icon.Width.Set(50f, 0f);
		icon.Height.Set(50f, 0f);
		icon.Left.Set(10f, 0f);
		icon.VAlign = 0.5f;
		background.Append(icon);

		name = new UIText(string.Empty, 0.82f);
		name.Left.Set(70f, 0f);
		name.Top.Set(13f, 0f);
		background.Append(name);

		detail = new UIText(string.Empty, 0.65f);
		detail.Left.Set(70f, 0f);
		detail.Top.Set(42f, 0f);
		detail.TextColor = SoullessUIPalette.TextSecondary;
		background.Append(detail);

		actionButton = new UITextPanel<string>(string.Empty, 0.68f, false);
		actionButton.Width.Set(116f, 0f);
		actionButton.Height.Set(34f, 0f);
		actionButton.HAlign = 1f;
		actionButton.Left.Set(-10f, 0f);
		actionButton.VAlign = 0.5f;
		actionButton.OnLeftClick += (_, _) =>
		{
			if (enabled)
			{
				action?.Invoke();
			}
		};
		actionButton.OnMouseOver += (_, _) =>
		{
			if (enabled)
			{
				actionButton.BackgroundColor = affordable
					? SoullessUIPalette.AccentSurfaceHover : SoullessUIPalette.WarningSurfaceHover;
				actionButton.BorderColor = affordable
					? SoullessUIPalette.AccentBright : SoullessUIPalette.WarningBorder;
			}
		};
		actionButton.OnMouseOut += (_, _) => ApplyButtonStyle();
		background.Append(actionButton);
	}

	public void SetAction(Action requestedAction)
	{
		action = requestedAction;
	}

	public void SetContent(int itemType, string requestedName, string requestedDetail, string buttonText, bool isEnabled, bool canAfford = true)
	{
		icon.ItemType = itemType;
		name.SetText(requestedName);
		detail.SetText(requestedDetail);
		actionButton.SetText(buttonText);
		enabled = isEnabled;
		affordable = canAfford;
		name.TextColor = enabled ? SoullessUIPalette.TextPrimary : SoullessUIPalette.TextMuted;
		icon.Opacity = enabled ? 1f : 0.42f;
		ApplyButtonStyle();
	}

	private void ApplyButtonStyle()
	{
		actionButton.BackgroundColor = !enabled ? SoullessUIPalette.SurfaceDisabled
			: affordable ? SoullessUIPalette.AccentSurface : SoullessUIPalette.WarningSurface;
		actionButton.BorderColor = !enabled ? SoullessUIPalette.SteelMuted
			: affordable ? SoullessUIPalette.Accent : SoullessUIPalette.WarningBorder;
		actionButton.TextColor = !enabled ? SoullessUIPalette.TextMuted
			: affordable ? SoullessUIPalette.AccentText : SoullessUIPalette.WarningText;
	}
}

internal sealed class SoulEssenceCatalogueCard : UIElement
{
	private int itemType;
	private string label = string.Empty;
	private string costLabel = string.Empty;
	private bool unlocked;
	private bool selected;
	private bool canAfford;

	public void SetContent(int requestedItemType, string requestedName, long soulCost, bool isUnlocked,
		bool isAffordable, bool isSelected)
	{
		itemType = requestedItemType;
		label = isUnlocked ? ShortName(requestedName) : "Locked";
		costLabel = isUnlocked ? $"{soulCost:N0} souls" : string.Empty;
		unlocked = isUnlocked;
		canAfford = isAffordable;
		selected = isSelected;
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		CalculatedStyle dimensions = GetDimensions();
		Rectangle box = new((int)MathF.Round(dimensions.X), (int)MathF.Round(dimensions.Y),
			ShopFullLayout.BoxWidth, ShopFullLayout.BoxHeight);
		Vector2 itemCenter = new(box.X + box.Width * 0.5f, box.Y + box.Height * 0.5f);
		Color boxColor = !unlocked ? Color.White * 0.55f
			: selected ? Color.Lerp(Color.White, SoullessUIPalette.Accent, 0.28f)
			: IsMouseHovering ? Color.Lerp(Color.White, SoullessUIPalette.Accent, 0.14f)
			: Color.White;
		ShopFullArt.DrawBox(spriteBatch, box, boxColor);

		if (unlocked && (selected || IsMouseHovering))
		{
			DrawItemGlow(spriteBatch, itemCenter);
		}

		ImbuementSlotDrawing.DrawItem(spriteBatch, itemType, itemCenter, 40f,
			unlocked ? Color.White : Color.White * 0.28f);

		Color labelColor = selected ? SoullessUIPalette.AccentText
			: !unlocked ? SoullessUIPalette.TextMuted : SoullessUIPalette.TextSecondary;
		Utils.DrawBorderString(spriteBatch, label, new Vector2(dimensions.Center().X, box.Bottom + 4f),
			labelColor, 0.44f, 0.5f);
		if (!string.IsNullOrEmpty(costLabel))
		{
			Color costColor = selected ? SoullessUIPalette.AccentText
				: canAfford ? SoullessUIPalette.AccentMuted : SoullessUIPalette.Warning;
			Utils.DrawBorderString(spriteBatch, costLabel, new Vector2(dimensions.Center().X, box.Bottom + 18f),
				costColor, 0.42f, 0.5f);
		}
	}

	private void DrawItemGlow(SpriteBatch spriteBatch, Vector2 center)
	{
		for (int direction = 0; direction < 4; direction++)
		{
			Vector2 offset = (MathHelper.PiOver2 * direction).ToRotationVector2() * 2f;
			ImbuementSlotDrawing.DrawItem(spriteBatch, itemType, center + offset, 40f,
				SoullessUIPalette.Accent * 0.16f);
		}
	}

	private static string ShortName(string name)
	{
		const string suffix = " Essence";
		return name.EndsWith(suffix, StringComparison.Ordinal) ? name[..^suffix.Length] : name;
	}
}

internal sealed class SoulEssenceCard : UIElement
{
	private readonly UIPanel background;
	private readonly SoulItemIcon icon;
	private readonly UIText name;
	private bool unlocked;
	private bool selected;

	public SoulEssenceCard()
	{
		background = new UIPanel();
		background.Width.Set(0f, 1f);
		background.Height.Set(0f, 1f);
		background.PaddingLeft = 4f;
		background.PaddingRight = 4f;
		Append(background);

		icon = new SoulItemIcon();
		icon.Width.Set(34f, 0f);
		icon.Height.Set(34f, 0f);
		icon.Left.Set(2f, 0f);
		icon.VAlign = 0.5f;
		background.Append(icon);

		name = new UIText(string.Empty, 0.56f);
		name.Left.Set(40f, 0f);
		name.VAlign = 0.5f;
		background.Append(name);

		OnMouseOver += (_, _) =>
		{
			if (!selected)
			{
				background.BackgroundColor = SoullessUIPalette.SurfaceHover;
				background.BorderColor = SoullessUIPalette.AccentHoverBorder;
			}
		};
		OnMouseOut += (_, _) => ApplyStyle();
	}

	public void SetContent(int itemType, string requestedName, bool isUnlocked, bool isSelected)
	{
		icon.ItemType = itemType;
		name.SetText(requestedName);
		unlocked = isUnlocked;
		selected = isSelected;
		icon.Opacity = unlocked ? 1f : 0.18f;
		name.TextColor = unlocked ? SoullessUIPalette.TextPrimary : SoullessUIPalette.TextDisabled;
		ApplyStyle();
	}

	private void ApplyStyle()
	{
		background.BackgroundColor = selected ? SoullessUIPalette.AccentSurface : SoullessUIPalette.Surface;
		background.BorderColor = selected ? SoullessUIPalette.Accent : SoullessUIPalette.SteelMuted;
	}
}

internal sealed class ImbuementRecipeRow : UIElement
{
	private const float BoxGap = 12f;
	private const float EquationWidth = ImbuementRecipeItemSlot.SlotWidth * 3f + BoxGap * 2f;
	private const float TextLeft = EquationWidth + 12f;

	private readonly UIPanel background;
	private readonly ImbuementRecipeItemSlot weaponSlot;
	private readonly ImbuementRecipeItemSlot essenceSlot;
	private readonly ImbuementRecipeItemSlot outputSlot;
	private readonly UIText resultName;
	private readonly UIText status;
	private Action action;
	private bool ready;

	public ImbuementRecipeRow()
	{
		background = new UIPanel();
		background.Width.Set(0f, 1f);
		background.Height.Set(0f, 1f);
		background.PaddingTop = 0f;
		background.PaddingBottom = 0f;
		background.PaddingLeft = 0f;
		background.PaddingRight = 0f;
		Append(background);

		weaponSlot = CreateSlot(0f, 0f);
		CreateOperator("+", ImbuementRecipeItemSlot.SlotWidth + BoxGap * 0.5f, 0.62f);
		essenceSlot = CreateSlot(ImbuementRecipeItemSlot.SlotWidth + BoxGap, 1.7f);
		CreateOperator("→", ImbuementRecipeItemSlot.SlotWidth * 2f + BoxGap * 1.5f, 0.62f);
		outputSlot = CreateSlot(ImbuementRecipeItemSlot.SlotWidth * 2f + BoxGap * 2f, 3.4f);

		resultName = new UIText(string.Empty, 0.54f);
		resultName.Left.Set(TextLeft, 0f);
		resultName.Top.Set(14f, 0f);
		background.Append(resultName);

		status = new UIText(string.Empty, 0.48f);
		status.Left.Set(TextLeft, 0f);
		status.Top.Set(38f, 0f);
		background.Append(status);

		OnLeftClick += (_, _) =>
		{
			if (ready)
			{
				action?.Invoke();
			}
		};
		OnMouseOver += (_, _) => ApplyStyle(true);
		OnMouseOut += (_, _) => ApplyStyle(false);
	}

	public void SetAction(Action requestedAction) => action = requestedAction;

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		// Ready rows breathe continuously instead of relying on easy-to-miss status text.
		ApplyStyle(IsMouseHovering);
	}

	public void SetContent(EssenceImbuementDefinition definition, bool canSelect, string statusText)
	{
		string essenceName = Lang.GetItemNameValue(definition.EssenceItemType);
		weaponSlot.SetItem(definition.PreviewInputItemType, definition.InputDisplayName);
		essenceSlot.SetItem(definition.EssenceItemType, essenceName);
		outputSlot.SetItem(definition.OutputItemType, definition.OutputName);
		resultName.SetText(definition.OutputName);
		status.SetText(statusText);
		ready = canSelect;
		float opacity = ready ? 1f : 0.55f;
		weaponSlot.Opacity = opacity;
		essenceSlot.Opacity = opacity;
		outputSlot.Opacity = opacity;
		SetReadyAnimation(ready);
		resultName.TextColor = ready ? SoullessUIPalette.AccentText : SoullessUIPalette.TextSecondary;
		status.TextColor = ready ? SoullessUIPalette.AccentMuted : SoullessUIPalette.Requirement;
		ApplyStyle(false);
	}

	public void SetContent(SoulSpellDefinition spell, bool canSelect, string statusText)
	{
		string potionName = Lang.GetItemNameValue(spell.PotionItemType);
		string essenceName = Lang.GetItemNameValue(spell.EssenceItemType);
		weaponSlot.SetItem(spell.PotionItemType, potionName);
		essenceSlot.SetItem(spell.EssenceItemType, essenceName);
		outputSlot.SetBuff(spell.BuffType, spell.Name);
		resultName.SetText(spell.Name);
		status.SetText(statusText);
		ready = canSelect;
		float opacity = ready ? 1f : 0.55f;
		weaponSlot.Opacity = opacity;
		essenceSlot.Opacity = opacity;
		outputSlot.Opacity = opacity;
		SetReadyAnimation(ready);
		resultName.TextColor = ready ? SoullessUIPalette.AccentText : SoullessUIPalette.TextSecondary;
		status.TextColor = statusText == "Learned" ? SoullessUIPalette.AccentMuted
			: ready ? SoullessUIPalette.AccentText : SoullessUIPalette.Requirement;
		ApplyStyle(false);
	}

	private ImbuementRecipeItemSlot CreateSlot(float left, float pulsePhase)
	{
		ImbuementRecipeItemSlot slot = new();
		slot.Left.Set(left, 0f);
		slot.VAlign = 0.5f;
		slot.PulsePhase = pulsePhase;
		background.Append(slot);
		return slot;
	}

	private void SetReadyAnimation(bool enabled)
	{
		weaponSlot.ReadyAnimation = enabled;
		essenceSlot.ReadyAnimation = enabled;
		outputSlot.ReadyAnimation = enabled;
	}

	private void CreateOperator(string text, float left, float scale)
	{
		UIText operation = new(text, scale)
		{
			TextColor = SoullessUIPalette.TextSecondary,
			VAlign = 0.5f
		};
		operation.Left.Set(left - 6f, 0f);
		background.Append(operation);
	}

	private void ApplyStyle(bool hovered)
	{
		if (ready)
		{
			float pulse = 0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 3.4f);
			background.BackgroundColor = SoullessUIPalette.AccentSurface * (hovered ? 0.48f : 0.24f + pulse * 0.1f);
			background.BorderColor = SoullessUIPalette.Accent * (hovered ? 0.9f : 0.38f + pulse * 0.34f);
			return;
		}

		// Incomplete rows use neutral hover feedback so cyan always means ready.
		background.BackgroundColor = hovered ? SoullessUIPalette.SurfaceHover * 0.35f : Color.Transparent;
		background.BorderColor = hovered ? SoullessUIPalette.Steel * 0.4f : Color.Transparent;
	}
}

internal sealed class ImbuementRecipeItemSlot : UIElement
{
	// Recipe slots are intentionally smaller than the station's primary sockets.
	internal const int SlotWidth = 56;
	internal const int SlotHeight = 54;
	private const float IconSize = 36f;

	private int itemType;
	private int buffType;
	private string tooltip = string.Empty;

	public float Opacity { get; set; } = 1f;
	public bool ReadyAnimation { get; set; }
	public float PulsePhase { get; set; }

	public ImbuementRecipeItemSlot()
	{
		Width.Set(SlotWidth, 0f);
		Height.Set(SlotHeight, 0f);
	}

	public void SetItem(int requestedItemType, string requestedTooltip)
	{
		itemType = requestedItemType;
		buffType = 0;
		tooltip = requestedTooltip;
	}

	public void SetBuff(int requestedBuffType, string requestedTooltip)
	{
		itemType = ItemID.None;
		buffType = requestedBuffType;
		tooltip = requestedTooltip;
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		CalculatedStyle dimensions = GetDimensions();
		Rectangle box = new((int)MathF.Round(dimensions.X), (int)MathF.Round(dimensions.Y),
			SlotWidth, SlotHeight);
		Color boxColor = IsMouseHovering
			? Color.Lerp(Color.White, SoullessUIPalette.Accent, 0.14f) * Opacity
			: Color.White * Opacity;
		if (ReadyAnimation)
		{
			// Staggered highlights visually trace the completed recipe equation.
			float pulse = 0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 4.2f - PulsePhase);
			boxColor = Color.Lerp(boxColor, SoullessUIPalette.AccentBright * Opacity, 0.08f + pulse * 0.24f);
		}
		ShopFullArt.DrawBox(spriteBatch, box, boxColor);
		Vector2 center = new(box.X + box.Width * 0.5f, box.Y + box.Height * 0.5f);
		if (buffType > 0)
		{
			Texture2D texture = TextureAssets.Buff[buffType].Value;
			float scale = Math.Min(1f, IconSize / Math.Max(texture.Width, texture.Height));
			spriteBatch.Draw(texture, center, null, Color.White * Opacity, 0f,
				texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
		}
		else
		{
			ImbuementSlotDrawing.DrawItem(spriteBatch, itemType, center, IconSize, Color.White * Opacity);
		}
		if (IsMouseHovering && !string.IsNullOrWhiteSpace(tooltip))
		{
			Main.instance.MouseText(tooltip);
		}
	}
}

internal sealed class ImbuementRitualCanvas : UIElement
{
	private readonly string ritualTitle;
	private readonly string ritualDescription;

	public float Reveal { get; set; }
	public bool DrawHeader { get; set; } = true;

	public ImbuementRitualCanvas(string ritualTitle = "IMBUEMENT RITUAL",
		string ritualDescription = "Bind a defeated echo into its weapon.")
	{
		this.ritualTitle = ritualTitle;
		this.ritualDescription = ritualDescription;
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		CalculatedStyle dimensions = GetDimensions();
		Vector2 center = new(dimensions.Center().X, dimensions.Y + 128f);
		float opacity = MathHelper.Clamp(Reveal, 0f, 1f);
		if (DrawHeader)
		{
			Utils.DrawBorderString(spriteBatch, ritualTitle, new Vector2(center.X, dimensions.Y + 20f),
				SoullessUIPalette.AccentText * opacity, 0.82f, 0.5f);
			Utils.DrawBorderString(spriteBatch, ritualDescription,
				new Vector2(center.X, dimensions.Y + 49f), SoullessUIPalette.TextSecondary * opacity, 0.58f, 0.5f);
		}
	}

}

internal sealed class RitualLabelPlate : UIElement
{
	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		Rectangle area = GetDimensions().ToRectangle();
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		spriteBatch.Draw(pixel, area, SoullessUIPalette.Panel * 0.86f);
		spriteBatch.Draw(pixel, new Rectangle(area.X, area.Y, area.Width, 2), SoullessUIPalette.AccentBorder * 0.82f);
		spriteBatch.Draw(pixel, new Rectangle(area.X, area.Bottom - 2, area.Width, 2), SoullessUIPalette.SteelMuted * 0.82f);
		spriteBatch.Draw(pixel, new Rectangle(area.X, area.Y, 2, area.Height), SoullessUIPalette.Steel * 0.82f);
		spriteBatch.Draw(pixel, new Rectangle(area.Right - 2, area.Y, 2, area.Height), SoullessUIPalette.Steel * 0.82f);
	}
}

internal sealed class ImbuementWeaponSocket : UIElement
{
	private int itemType;
	private bool resonating;

	public ImbuementWeaponSocket()
	{
		Width.Set(ShopFullLayout.BoxWidth, 0f);
		Height.Set(ShopFullLayout.BoxHeight, 0f);
	}

	public void SetItem(int requestedItemType, bool validCombination)
	{
		itemType = requestedItemType;
		resonating = validCombination;
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		CalculatedStyle dimensions = GetDimensions();
		Rectangle box = new((int)MathF.Round(dimensions.X), (int)MathF.Round(dimensions.Y),
			ShopFullLayout.BoxWidth, ShopFullLayout.BoxHeight);
		Vector2 center = new(box.X + box.Width * 0.5f, box.Y + box.Height * 0.5f);
		float impactPulse = 0f;
		Color boxColor = Color.White;

		if (resonating)
		{
			impactPulse = DrawSoulTransfer(spriteBatch, center);
			ImbuementOrbitSoulRenderer.Draw(spriteBatch, center, drawFront: false);
			boxColor = Color.Lerp(Color.White, SoullessUIPalette.Accent, 0.22f + impactPulse * 0.2f);
		}

		ShopFullArt.DrawBox(spriteBatch, box, boxColor);
		ImbuementSlotDrawing.DrawItem(spriteBatch, itemType, center, 40f);
		if (resonating)
		{
			Vector2 shimmer = new(MathF.Sin(Main.GlobalTimeWrappedHourly * 6f), MathF.Cos(Main.GlobalTimeWrappedHourly * 4f) * 0.5f);
			ImbuementSlotDrawing.DrawItem(spriteBatch, itemType, center + shimmer, 40f,
				SoullessUIPalette.AccentBright * (0.2f + impactPulse * 0.2f));
			ImbuementOrbitSoulRenderer.Draw(spriteBatch, center, drawFront: true);
		}
		if (IsMouseHovering)
		{
			Main.instance.MouseText(itemType > ItemID.None ? Lang.GetItemNameValue(itemType) : "Select Weapon");
		}
	}

	private static float DrawSoulTransfer(SpriteBatch spriteBatch, Vector2 weaponCenter)
	{
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Vector2 essenceCenter = weaponCenter + new Vector2(0f, 127f);
		float strongestImpact = 0f;
		for (int index = 0; index < 3; index++)
		{
			float cycle = (Main.GlobalTimeWrappedHourly * 0.58f + index / 3f) % 1f;
			float side = index == 1 ? 1f : -1f;
			Vector2 control = Vector2.Lerp(essenceCenter, weaponCenter, 0.52f)
				+ new Vector2(side * (22f + index * 5f), 0f);
			for (int trailIndex = 7; trailIndex >= 0; trailIndex--)
			{
				float trailProgress = MathHelper.Clamp(cycle - trailIndex * 0.022f, 0f, 1f);
				Vector2 trailPosition = SnapToPixelGrid(QuadraticBezier(essenceCenter, control, weaponCenter, trailProgress));
				float strength = 1f - trailIndex / 8f;
				int size = trailIndex < 2 ? 6 : trailIndex < 5 ? 4 : 2;
				DrawPixel(spriteBatch, pixel, trailPosition, size,
					SoullessUIPalette.AccentAdditive * (strength * 0.62f));
			}

			Vector2 wispPosition = SnapToPixelGrid(QuadraticBezier(essenceCenter, control, weaponCenter, cycle));
			float soulPulse = 0.92f + MathF.Sin((Main.GlobalTimeWrappedHourly + index) * 8f) * 0.08f;
			DrawPixel(spriteBatch, pixel, wispPosition, soulPulse > 0.95f ? 8 : 6, SoullessUIPalette.AccentAdditive * 0.82f);
			DrawPixel(spriteBatch, pixel, wispPosition, 4, SoullessUIPalette.AccentTextAdditive * 0.94f);

			float arrival = MathHelper.Clamp((cycle - 0.84f) / 0.16f, 0f, 1f);
			strongestImpact = Math.Max(strongestImpact, MathF.Sin(arrival * MathHelper.Pi));
		}

		return strongestImpact;
	}

	private static Vector2 SnapToPixelGrid(Vector2 position) => new(
		MathF.Round(position.X * 0.5f) * 2f,
		MathF.Round(position.Y * 0.5f) * 2f);

	private static void DrawPixel(SpriteBatch spriteBatch, Texture2D pixel, Vector2 center, int size, Color color)
	{
		int halfSize = size / 2;
		spriteBatch.Draw(pixel, new Rectangle((int)center.X - halfSize, (int)center.Y - halfSize, size, size), color);
	}

	private static Vector2 QuadraticBezier(Vector2 start, Vector2 control, Vector2 end, float progress)
	{
		float inverse = 1f - progress;
		return inverse * inverse * start + 2f * inverse * progress * control + progress * progress * end;
	}
}

internal sealed class ImbuementEssenceSocket : UIElement
{
	private int itemType;

	public ImbuementEssenceSocket()
	{
		Width.Set(ShopFullLayout.BoxWidth, 0f);
		Height.Set(ShopFullLayout.BoxHeight, 0f);
	}

	public void SetItem(int requestedItemType)
	{
		itemType = requestedItemType;
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		CalculatedStyle dimensions = GetDimensions();
		Rectangle box = new((int)MathF.Round(dimensions.X), (int)MathF.Round(dimensions.Y),
			ShopFullLayout.BoxWidth, ShopFullLayout.BoxHeight);
		ShopFullArt.DrawBox(spriteBatch, box, Color.White);
		ImbuementSlotDrawing.DrawItem(spriteBatch, itemType,
			new Vector2(box.X + box.Width * 0.5f, box.Y + box.Height * 0.5f), 40f);
		if (IsMouseHovering)
		{
			Main.instance.MouseText(itemType > ItemID.None ? Lang.GetItemNameValue(itemType) : "Select Essence");
		}
	}
}

internal static class ImbuementSlotDrawing
{
	public static void DrawItem(SpriteBatch spriteBatch, int itemType, Vector2 center, float maximumSize, Color? drawColor = null)
	{
		if (itemType <= ItemID.None)
		{
			return;
		}
		if (EssenceEchoRenderer.TryDraw(spriteBatch, itemType, center, maximumSize,
			drawColor ?? Color.White))
		{
			return;
		}

		// Vanilla item textures are loaded lazily and may not have appeared elsewhere yet.
		Main.instance.LoadItem(itemType);
		Texture2D texture = TextureAssets.Item[itemType].Value;
		Rectangle frame = ItemAnimationDrawing.GetFrame(itemType, texture);
		float scale = Math.Min(1f, maximumSize / Math.Max(frame.Width, frame.Height));
		spriteBatch.Draw(texture, center, frame, drawColor ?? Color.White, 0f,
			frame.Size() * 0.5f, scale, SpriteEffects.None, 0f);
	}

}

internal sealed class SoulItemIcon : UIElement
{
	public int ItemType { get; set; }
	public float Opacity { get; set; } = 1f;

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		base.DrawSelf(spriteBatch);
		if (ItemType <= ItemID.None)
		{
			return;
		}

		CalculatedStyle dimensions = GetDimensions();
		Vector2 center = new(dimensions.X + dimensions.Width * 0.5f, dimensions.Y + dimensions.Height * 0.5f);
		float maximumSize = Math.Min(52f, Math.Min(dimensions.Width, dimensions.Height));
		if (EssenceEchoRenderer.TryDraw(spriteBatch, ItemType, center, maximumSize, Color.White * Opacity))
		{
			return;
		}

		Main.instance.LoadItem(ItemType);
		Texture2D texture = TextureAssets.Item[ItemType].Value;
		Rectangle frame = ItemAnimationDrawing.GetFrame(ItemType, texture);
		float scale = Math.Min(1f, maximumSize / Math.Max(frame.Width, frame.Height));
		spriteBatch.Draw(texture, center, frame, Color.White * Opacity, 0f,
			frame.Size() * 0.5f, scale, SpriteEffects.None, 0f);
	}
}
