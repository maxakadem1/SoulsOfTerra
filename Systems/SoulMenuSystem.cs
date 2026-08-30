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
using SoulsOfTerra.Content.Projectiles;
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
using Terraria.UI;

namespace SoulsOfTerra.Systems;

public class SoulMenuSystem : ModSystem
{
	private static UserInterface soulInterface;
	private static SoulMenuState menuState;

	public override void Load()
	{
		if (Main.dedServ)
		{
			return;
		}

		menuState = new SoulMenuState();
		menuState.Activate();
		soulInterface = new UserInterface();
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
	}

	public static void OpenTerraforge(Point16 terraforgePosition)
	{
		if (Main.dedServ || menuState is null)
		{
			return;
		}

		menuState.ConfigureTerraforge(terraforgePosition);
		soulInterface.SetState(menuState);
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
				soulInterface?.Draw(Main.spriteBatch, new GameTime());
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

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		CalculatedStyle dimensions = GetDimensions();
		Rectangle frame = new((int)dimensions.X, (int)dimensions.Y, (int)dimensions.Width, (int)dimensions.Height);
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Texture2D corner = ModContent.Request<Texture2D>("SoulsOfTerra/Content/UI/ShopUI_corner").Value;
		Texture2D horizontalEdge = ModContent.Request<Texture2D>("SoulsOfTerra/Content/UI/ShopUI_top_bottom").Value;
		Texture2D verticalEdge = ModContent.Request<Texture2D>("SoulsOfTerra/Content/UI/ShopUI_left_right").Value;
		Texture2D header = ModContent.Request<Texture2D>("SoulsOfTerra/Content/UI/Shop_UI_header").Value;

		// The inset fill leaves the transparent outer corner shapes intact.
		Rectangle interior = new(frame.X + FrameInset, frame.Y + FrameInset, frame.Width - FrameInset * 2, frame.Height - FrameInset * 2);
		spriteBatch.Draw(pixel, interior, BackgroundColor);

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
	private SoulMenuFramePanel panel;
	private UIText title;
	private UIText subtitle;
	private UIText balance;
	private SoulActionRow primaryRow;
	private SoulActionRow secondaryRow;
	private SoulActionRow tertiaryRow;
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
	private UIElement imbuementContent;
	private ImbuementWeaponSocket imbuementWeaponSocket;
	private ImbuementEssenceSocket imbuementEssenceSocket;
	private UIText imbuementWeaponName;
	private UIText imbuementEssenceName;
	private UITextPanel<string> bindEssenceButton;
	private UITextPanel<string> imbuementRecipesButton;
	private UIElement imbuementRecipeContent;
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

	public override void OnInitialize()
	{
		panel = new SoulMenuFramePanel();
		panel.Width.Set(540f, 0f);
		panel.Height.Set(340f, 0f);
		// Shop-style placement keeps the world and player visible during interaction.
		panel.Left.Set(36f, 0f);
		panel.VAlign = 0.5f;
		panel.BackgroundColor = new Color(17, 22, 28, 245);
		panel.BorderColor = new Color(76, 111, 103, 255);
		Append(panel);

		title = new UIText(string.Empty, 1.05f);
		title.Left.Set(20f, 0f);
		title.Top.Set(14f, 0f);
		panel.Append(title);

		subtitle = new UIText(string.Empty, 0.72f);
		subtitle.Left.Set(21f, 0f);
		subtitle.Top.Set(43f, 0f);
		subtitle.TextColor = new Color(154, 177, 169);
		panel.Append(subtitle);

		balance = new UIText(string.Empty, 0.82f);
		balance.HAlign = 1f;
		balance.Left.Set(-20f, 0f);
		balance.Top.Set(22f, 0f);
		balance.TextColor = new Color(180, 238, 210);
		panel.Append(balance);

		primaryRow = CreateRow(78f);
		primaryRow.SetAction(UsePrimaryAction);
		secondaryRow = CreateRow(168f);
		secondaryRow.SetAction(UseSecondaryAction);
		tertiaryRow = CreateRow(258f);
		tertiaryRow.SetAction(UseTertiaryAction);
		CreateSoullessTabs();
		CreateTerraforgeTabs();
		CreateEssenceCatalogue();
		CreateImbuementPage();
		CreateImbuementRecipePage();
		CreateCrystalCatalogue();

		feedback = new UIText(string.Empty, 0.72f);
		feedback.HAlign = 0.5f;
		feedback.Top.Set(266f, 0f);
		feedback.TextColor = new Color(205, 220, 212);
		panel.Append(feedback);

		closeButton = new UITextPanel<string>("Close", 0.72f, false);
		closeButton.Width.Set(92f, 0f);
		closeButton.Height.Set(32f, 0f);
		closeButton.HAlign = 0.5f;
		closeButton.Top.Set(294f, 0f);
		closeButton.BackgroundColor = new Color(48, 58, 66);
		closeButton.BorderColor = new Color(83, 103, 99);
		closeButton.OnMouseOver += (_, _) => closeButton.BackgroundColor = new Color(65, 82, 79);
		closeButton.OnMouseOut += (_, _) => closeButton.BackgroundColor = new Color(48, 58, 66);
		closeButton.OnLeftClick += (_, _) => SoulMenuSystem.Close();
		panel.Append(closeButton);
	}

	public void ConfigureSoulless(int requestedNpcIndex)
	{
		kind = MenuKind.Soulless;
		npcIndex = requestedNpcIndex;
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

		if (panel.ContainsPoint(Main.MouseScreen))
		{
			player.mouseInterface = true;
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
		panel.RemoveAllChildren();
		panel.Height.Set(soullessTab == SoullessTab.Services ? 490f : 400f, 0f);
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
			panel.Append(primaryRow);
			panel.Append(secondaryRow);
			panel.Append(tertiaryRow);
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

		feedback.Top.Set(soullessTab == SoullessTab.Services ? 416f : 326f, 0f);
		panel.Append(feedback);
		closeButton.Top.Set(soullessTab == SoullessTab.Services ? 448f : 358f, 0f);
		panel.Append(closeButton);
		ApplySoullessTabStyles();
	}

	private void BuildTerraforgeLayout()
	{
		panel.RemoveAllChildren();
		panel.Height.Set(410f, 0f);
		panel.Append(title);
		panel.Append(subtitle);
		panel.Append(balance);
		panel.Append(condensationTabButton);
		panel.Append(imbuementTabButton);
		if (terraforgeTab == TerraforgeTab.Condense)
		{
			panel.Append(essenceGrid);
			condenseButton.Top.Set(306f, 0f);
			panel.Append(condenseButton);
		}
		else
		{
			if (showingImbuementRecipes)
			{
				panel.Append(imbuementRecipeContent);
			}
			else
			{
				panel.Append(imbuementContent);
				panel.Append(imbuementRecipesButton);
				bindEssenceButton.Top.Set(306f, 0f);
				panel.Append(bindEssenceButton);
			}
		}
		feedback.Top.Set(348f, 0f);
		panel.Append(feedback);
		closeButton.Top.Set(368f, 0f);
		panel.Append(closeButton);
		ApplyTerraforgeTabStyles();
	}

	private void CreateEssenceCatalogue()
	{
		// The shared registry keeps UI, server validation, and multiplayer IDs in one stable order.
		essenceDefinitions = SoulEssenceRegistry.Definitions;

		essenceGrid = new UIElement();
		essenceGrid.Width.Set(-32f, 1f);
		essenceGrid.Height.Set(190f, 0f);
		essenceGrid.Left.Set(16f, 0f);
		essenceGrid.Top.Set(106f, 0f);

		essenceList = new UIList();
		essenceList.Width.Set(0f, 1f);
		essenceList.Height.Set(0f, 1f);
		essenceList.ListPadding = 6f;
		essenceGrid.Append(essenceList);

		essenceScrollBar = new UIScrollbar();
		essenceScrollBar.Width.Set(14f, 0f);
		essenceScrollBar.Height.Set(0f, 1f);
		essenceScrollBar.HAlign = 1f;
		essenceList.SetScrollbar(essenceScrollBar);

		int rowCount = (essenceDefinitions.Length + 4) / 5;
		if (rowCount > 3)
		{
			essenceList.Width.Set(-20f, 1f);
			essenceGrid.Append(essenceScrollBar);
		}

		essenceCards = new SoulEssenceCatalogueCard[essenceDefinitions.Length];
		for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
		{
			UIElement row = new();
			row.Width.Set(0f, 1f);
			row.Height.Set(82f, 0f);
			essenceList.Add(row);

			for (int columnIndex = 0; columnIndex < 5; columnIndex++)
			{
				int index = rowIndex * 5 + columnIndex;
				if (index >= essenceDefinitions.Length)
				{
					break;
				}

				int selectedIndex = index;
				SoulEssenceCatalogueCard card = new();
				card.Width.Set(86f, 0f);
				card.Height.Set(82f, 0f);
				card.Left.Set(columnIndex * 96f, 0f);
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
		essenceDetails.BackgroundColor = new Color(25, 32, 39, 245);
		essenceDetails.BorderColor = new Color(64, 86, 82);

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
		detailDescription.TextColor = new Color(153, 172, 166);
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
				condenseButton.BackgroundColor = HasEnoughForCurrentSelection() ? new Color(55, 112, 91) : new Color(104, 65, 59);
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
		balance.SetText($"{Main.LocalPlayer.GetModPlayer<SoulPlayer>().SoulBalance:N0} souls");
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

		bool keyUnlocked = NPC.downedBoss3;
		bool canAffordKey = Main.LocalPlayer.GetModPlayer<SoulPlayer>().SoulBalance >= SoulTransactions.WardensFragmentCost;
		tertiaryRow.SetContent(
			ModContent.ItemType<WardensFragment>(),
			"Warden's Fragment",
			keyUnlocked ? $"Reusable Buried Court key  •  {SoulTransactions.WardensFragmentCost:N0} souls" : "Requires Skeletron",
			keyUnlocked ? "Purchase" : "Locked",
			keyUnlocked,
			canAffordKey);
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
		detailCost.TextColor = selectedUnlocked && !HasEnoughForSelectedCrystal() ? new Color(238, 154, 137) : new Color(180, 238, 210);
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
			string tooltip = unlocked ? definition.Description : definition.GetRequirement();
			essenceCards[index].SetContent(
				definition.ItemType,
				definition.Name,
				definition.Cost,
				unlocked,
				balanceValue >= definition.Cost,
				selectedEssenceIndex == index,
				tooltip);
		}

		ApplyCurrentActionButtonStyle();
	}

	private void RefreshImbuementContent()
	{
		title.SetText("Terraforge");
		if (showingImbuementRecipes)
		{
			subtitle.SetText("Known bindings revealed by defeated foes.");
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
		bindEssenceButton.BackgroundColor = enabled ? new Color(54, 66, 76) : new Color(43, 47, 51);
		bindEssenceButton.BorderColor = enabled ? new Color(111, 142, 137) : new Color(68, 72, 76);
		bindEssenceButton.TextColor = enabled ? new Color(190, 214, 207) : new Color(125, 130, 132);
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
		imbuementContent = new UIElement();
		imbuementContent.Width.Set(-32f, 1f);
		imbuementContent.Height.Set(190f, 0f);
		imbuementContent.Left.Set(16f, 0f);
		imbuementContent.Top.Set(108f, 0f);

		// The authored frame is the ritual focus; only a valid pair awakens its glow.
		imbuementWeaponSocket = new ImbuementWeaponSocket();
		imbuementWeaponSocket.HAlign = 0.5f;
		imbuementContent.Append(imbuementWeaponSocket);

		imbuementWeaponName = new UIText("Select Weapon", 0.57f);
		imbuementWeaponName.Width.Set(180f, 0f);
		imbuementWeaponName.Top.Set(80f, 0f);
		imbuementWeaponName.HAlign = 0.5f;
		imbuementWeaponName.TextColor = new Color(205, 220, 212);
		imbuementContent.Append(imbuementWeaponName);

		imbuementEssenceSocket = new ImbuementEssenceSocket();
		imbuementEssenceSocket.HAlign = 0.5f;
		imbuementEssenceSocket.Top.Set(108f, 0f);
		imbuementContent.Append(imbuementEssenceSocket);

		imbuementEssenceName = new UIText("Select Essence", 0.54f);
		imbuementEssenceName.Width.Set(180f, 0f);
		imbuementEssenceName.Top.Set(162f, 0f);
		imbuementEssenceName.HAlign = 0.5f;
		imbuementEssenceName.TextColor = new Color(166, 190, 181);
		imbuementContent.Append(imbuementEssenceName);

		bindEssenceButton = new UITextPanel<string>("Bind Essence", 0.76f, false);
		bindEssenceButton.Width.Set(220f, 0f);
		bindEssenceButton.Height.Set(38f, 0f);
		bindEssenceButton.HAlign = 0.5f;
		bindEssenceButton.OnLeftClick += (_, _) => UseEssenceImbuement();
		bindEssenceButton.OnMouseOut += (_, _) => ApplyBindButtonStyle();
	}

	private void CreateImbuementRecipePage()
	{
		imbuementRecipesButton = new UITextPanel<string>("Back to Recipes", 0.64f, false);
		imbuementRecipesButton.Width.Set(140f, 0f);
		imbuementRecipesButton.Height.Set(30f, 0f);
		imbuementRecipesButton.Left.Set(292f, 0f);
		imbuementRecipesButton.Top.Set(68f, 0f);
		imbuementRecipesButton.BackgroundColor = new Color(40, 54, 58);
		imbuementRecipesButton.BorderColor = new Color(78, 105, 100);
		imbuementRecipesButton.OnMouseOver += (_, _) => imbuementRecipesButton.BackgroundColor = new Color(54, 75, 74);
		imbuementRecipesButton.OnMouseOut += (_, _) => imbuementRecipesButton.BackgroundColor = new Color(40, 54, 58);
		imbuementRecipesButton.OnLeftClick += (_, _) => OpenImbuementRecipes();

		imbuementRecipeContent = new UIElement();
		imbuementRecipeContent.Width.Set(-32f, 1f);
		imbuementRecipeContent.Height.Set(190f, 0f);
		imbuementRecipeContent.Left.Set(16f, 0f);
		imbuementRecipeContent.Top.Set(108f, 0f);

		imbuementRecipeHint = new UIText("Defeat bosses to reveal their bindings.", 0.62f);
		imbuementRecipeHint.TextColor = new Color(154, 177, 169);
		imbuementRecipeContent.Append(imbuementRecipeHint);

		UIElement listContainer = new();
		listContainer.Width.Set(0f, 1f);
		listContainer.Height.Set(158f, 0f);
		listContainer.Top.Set(25f, 0f);
		imbuementRecipeContent.Append(listContainer);

		imbuementRecipeList = new UIList();
		imbuementRecipeList.Width.Set(0f, 1f);
		imbuementRecipeList.Height.Set(0f, 1f);
		imbuementRecipeList.ListPadding = 5f;
		listContainer.Append(imbuementRecipeList);

		imbuementRecipeScrollBar = new UIScrollbar();
		imbuementRecipeScrollBar.Width.Set(14f, 0f);
		imbuementRecipeScrollBar.Height.Set(0f, 1f);
		imbuementRecipeScrollBar.HAlign = 1f;
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

		for (int index = 0; index < EssenceImbuementRegistry.Definitions.Length; index++)
		{
			EssenceImbuementDefinition definition = EssenceImbuementRegistry.Definitions[index];
			if (!SoulEssenceRegistry.TryFindByItemType(definition.EssenceItemType, out SoulEssenceDefinition essence)
				|| !essence.IsDiscovered())
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

		bool hasRecipes = imbuementRecipeRows.Count > 0;
		imbuementRecipeHint.SetText(hasRecipes
			? "Select a complete recipe to begin its binding ritual."
			: "No imbuements have been revealed yet.");

		// Two complete rows fit without scrolling in the compact forge frame.
		bool needsScrollBar = imbuementRecipeRows.Count > 2;
		imbuementRecipeList.Width.Set(needsScrollBar ? -18f : 0f, 1f);
		if (needsScrollBar && imbuementRecipeScrollBar.Parent is null)
		{
			imbuementRecipeContent.Append(imbuementRecipeScrollBar);
			imbuementRecipeScrollBar.Top.Set(25f, 0f);
			imbuementRecipeScrollBar.Height.Set(158f, 0f);
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
		button.BackgroundColor = selected ? new Color(42, 72, 65) : new Color(31, 39, 45);
		button.BorderColor = selected ? new Color(117, 182, 151) : new Color(63, 76, 78);
		button.TextColor = selected ? new Color(220, 244, 231) : new Color(154, 169, 165);
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
		condenseButton.BackgroundColor = !available ? new Color(43, 47, 51) : affordable ? new Color(43, 83, 70) : new Color(73, 52, 50);
		condenseButton.BorderColor = !available ? new Color(68, 72, 76) : affordable ? new Color(90, 143, 121) : new Color(123, 78, 71);
		condenseButton.TextColor = !available ? new Color(125, 130, 132) : affordable ? new Color(215, 244, 229) : new Color(236, 183, 171);
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
		feedback.TextColor = success ? new Color(147, 225, 183) : new Color(238, 154, 137);
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
		background.BackgroundColor = new Color(27, 34, 41, 245);
		background.BorderColor = new Color(59, 76, 75, 255);
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
		detail.TextColor = new Color(153, 172, 166);
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
				actionButton.BackgroundColor = affordable ? new Color(55, 112, 91) : new Color(104, 65, 59);
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
		name.TextColor = enabled ? Color.White : new Color(155, 161, 163);
		icon.Opacity = enabled ? 1f : 0.42f;
		ApplyButtonStyle();
	}

	private void ApplyButtonStyle()
	{
		actionButton.BackgroundColor = !enabled ? new Color(43, 47, 51) : affordable ? new Color(43, 83, 70) : new Color(73, 52, 50);
		actionButton.BorderColor = !enabled ? new Color(68, 72, 76) : affordable ? new Color(90, 143, 121) : new Color(123, 78, 71);
		actionButton.TextColor = !enabled ? new Color(125, 130, 132) : affordable ? new Color(215, 244, 229) : new Color(236, 183, 171);
	}
}

internal sealed class SoulEssenceCatalogueCard : UIElement
{
	private readonly UIPanel background;
	private readonly SoulItemIcon icon;
	private readonly UIText name;
	private readonly UIText cost;
	private string tooltipText = string.Empty;
	private bool unlocked;
	private bool selected;

	public SoulEssenceCatalogueCard()
	{
		background = new UIPanel();
		background.Width.Set(0f, 1f);
		background.Height.Set(0f, 1f);
		background.PaddingTop = 0f;
		background.PaddingBottom = 0f;
		background.PaddingLeft = 2f;
		background.PaddingRight = 2f;
		Append(background);

		icon = new SoulItemIcon();
		icon.Width.Set(48f, 0f);
		icon.Height.Set(48f, 0f);
		icon.HAlign = 0.5f;
		icon.Top.Set(1f, 0f);
		background.Append(icon);

		name = new UIText(string.Empty, 0.52f);
		name.HAlign = 0.5f;
		name.Top.Set(48f, 0f);
		background.Append(name);

		cost = new UIText(string.Empty, 0.48f);
		cost.HAlign = 0.5f;
		cost.Top.Set(65f, 0f);
		background.Append(cost);

		OnMouseOver += (_, _) => ApplyStyle(true);
		OnMouseOut += (_, _) => ApplyStyle(false);
	}

	public void SetContent(int itemType, string requestedName, long soulCost, bool isUnlocked, bool canAfford, bool isSelected, string tooltip)
	{
		icon.ItemType = itemType;
		name.SetText(requestedName);
		cost.SetText(isUnlocked ? $"{soulCost:N0} souls" : "Locked");
		tooltipText = $"{requestedName}\n{tooltip}";
		unlocked = isUnlocked;
		selected = isSelected;
		icon.Opacity = unlocked ? 1f : 0.28f;
		name.TextColor = unlocked ? new Color(218, 235, 226) : new Color(142, 151, 150);
		cost.TextColor = !unlocked ? new Color(112, 121, 121) : canAfford ? new Color(180, 238, 210) : new Color(238, 154, 137);
		ApplyStyle(false);
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		base.DrawSelf(spriteBatch);
		if (IsMouseHovering && !string.IsNullOrEmpty(tooltipText))
		{
			Main.instance.MouseText(tooltipText);
		}
	}

	private void ApplyStyle(bool hovered)
	{
		background.BackgroundColor = selected
			? new Color(42, 72, 65)
			: hovered ? new Color(44, 57, 61) : new Color(26, 33, 39);
		background.BorderColor = selected
			? new Color(117, 182, 151)
			: unlocked ? new Color(65, 82, 79) : new Color(57, 65, 67);
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
				background.BackgroundColor = new Color(44, 57, 61);
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
		name.TextColor = unlocked ? new Color(218, 235, 226) : new Color(112, 121, 121);
		ApplyStyle();
	}

	private void ApplyStyle()
	{
		background.BackgroundColor = selected ? new Color(42, 72, 65) : new Color(26, 33, 39);
		background.BorderColor = selected ? new Color(117, 182, 151) : new Color(57, 71, 71);
	}
}

internal sealed class ImbuementRecipeRow : UIElement
{
	private readonly UIPanel background;
	private readonly ImbuementRecipeItemSlot weaponSlot;
	private readonly ImbuementRecipeItemSlot essenceSlot;
	private readonly ImbuementRecipeItemSlot outputSlot;
	private readonly UIPanel detailsPanel;
	private readonly UIText resultName;
	private readonly UIText ingredients;
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
		Append(background);

		weaponSlot = CreateSlot(10f);
		CreateOperator("+", 53f, 0.58f);
		essenceSlot = CreateSlot(69f);
		CreateOperator("→", 112f, 0.58f);
		outputSlot = CreateSlot(139f);

		// The inset separates readable recipe details from the compact visual equation.
		detailsPanel = new UIPanel();
		detailsPanel.Left.Set(190f, 0f);
		detailsPanel.Top.Set(5f, 0f);
		detailsPanel.Width.Set(-196f, 1f);
		detailsPanel.Height.Set(62f, 0f);
		detailsPanel.PaddingTop = 0f;
		detailsPanel.PaddingBottom = 0f;
		detailsPanel.PaddingLeft = 9f;
		detailsPanel.PaddingRight = 7f;
		background.Append(detailsPanel);

		resultName = new UIText(string.Empty, 0.54f);
		resultName.Top.Set(5f, 0f);
		detailsPanel.Append(resultName);

		ingredients = new UIText(string.Empty, 0.46f);
		ingredients.Top.Set(24f, 0f);
		detailsPanel.Append(ingredients);

		status = new UIText(string.Empty, 0.48f);
		status.Top.Set(43f, 0f);
		detailsPanel.Append(status);

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

	public void SetContent(EssenceImbuementDefinition definition, bool canSelect, string statusText)
	{
		string essenceName = Lang.GetItemNameValue(definition.EssenceItemType);
		weaponSlot.SetItem(definition.PreviewInputItemType, definition.InputDisplayName);
		essenceSlot.SetItem(definition.EssenceItemType, essenceName);
		outputSlot.SetItem(definition.OutputItemType, definition.OutputName);
		resultName.SetText(definition.OutputName);
		ingredients.SetText($"{definition.InputDisplayName} + {essenceName}");
		status.SetText(statusText);
		ready = canSelect;
		float opacity = ready ? 1f : 0.55f;
		weaponSlot.Opacity = opacity;
		essenceSlot.Opacity = opacity;
		outputSlot.Opacity = opacity;
		resultName.TextColor = ready ? new Color(137, 235, 205) : new Color(154, 168, 163);
		ingredients.TextColor = ready ? new Color(210, 229, 220) : new Color(142, 153, 151);
		status.TextColor = ready ? new Color(144, 226, 190) : new Color(207, 164, 135);
		ApplyStyle(false);
	}

	private ImbuementRecipeItemSlot CreateSlot(float left)
	{
		ImbuementRecipeItemSlot slot = new();
		slot.Left.Set(left, 0f);
		slot.VAlign = 0.5f;
		background.Append(slot);
		return slot;
	}

	private void CreateOperator(string text, float left, float scale)
	{
		UIText operation = new(text, scale)
		{
			TextColor = new Color(165, 203, 190),
			VAlign = 0.5f
		};
		operation.Left.Set(left, 0f);
		background.Append(operation);
	}

	private void ApplyStyle(bool hovered)
	{
		background.BackgroundColor = hovered
			? ready ? new Color(43, 73, 65) : new Color(42, 48, 51)
			: new Color(26, 33, 39);
		background.BorderColor = ready ? new Color(103, 171, 143) : new Color(60, 70, 71);
		detailsPanel.BackgroundColor = hovered ? new Color(30, 43, 46) : new Color(22, 29, 35);
		detailsPanel.BorderColor = ready ? new Color(75, 120, 105) : new Color(53, 64, 65);
	}
}

internal sealed class ImbuementRecipeItemSlot : UIElement
{
	private int itemType;
	private string tooltip = string.Empty;

	public float Opacity { get; set; } = 1f;

	public ImbuementRecipeItemSlot()
	{
		Width.Set(40f, 0f);
		Height.Set(40f, 0f);
	}

	public void SetItem(int requestedItemType, string requestedTooltip)
	{
		itemType = requestedItemType;
		tooltip = requestedTooltip;
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		Rectangle area = GetDimensions().ToRectangle();
		ImbuementSlotDrawing.DrawSlot(spriteBatch, area, IsMouseHovering);
		ImbuementSlotDrawing.DrawItem(spriteBatch, itemType, area.Center.ToVector2(), 30f, Color.White * Opacity);
		if (IsMouseHovering && !string.IsNullOrWhiteSpace(tooltip))
		{
			Main.instance.MouseText(tooltip);
		}
	}
}

internal sealed class ImbuementWeaponSocket : UIElement
{
	private int itemType;
	private bool resonating;

	public ImbuementWeaponSocket()
	{
		Width.Set(80f, 0f);
		Height.Set(80f, 0f);
	}

	public void SetItem(int requestedItemType, bool validCombination)
	{
		itemType = requestedItemType;
		resonating = validCombination;
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		CalculatedStyle dimensions = GetDimensions();
		Vector2 position = new(dimensions.X, dimensions.Y);
		Vector2 center = dimensions.Center();
		Texture2D frame = ModContent.Request<Texture2D>("SoulsOfTerra/Content/UI/ShopUI_weapon_frame").Value;
		float impactPulse = 0f;

		if (resonating)
		{
			impactPulse = DrawSoulTransfer(spriteBatch, center);
			// A slow, broad breath makes resonance feel deliberate instead of reactive UI feedback.
			float pulse = 0.5f + 0.2f * MathF.Sin(Main.GlobalTimeWrappedHourly * 2.15f) + impactPulse * 0.32f;
			Color glow = new Color(48, 232, 205) * pulse;
			// Offset silhouettes make the authored frame glow without changing SpriteBatch state.
			for (int direction = 0; direction < 8; direction++)
			{
				float angle = MathHelper.TwoPi * direction / 8f;
				Vector2 offset = angle.ToRotationVector2() * (3.75f + impactPulse * 2.25f);
				spriteBatch.Draw(frame, position + offset, glow);
			}
		}

		spriteBatch.Draw(frame, position, Color.White);
		ImbuementSlotDrawing.DrawItem(spriteBatch, itemType, center, 44f);
		if (resonating)
		{
			// A faint displaced copy gives the bound weapon a spectral shimmer.
			Vector2 shimmer = new(MathF.Sin(Main.GlobalTimeWrappedHourly * 6f), MathF.Cos(Main.GlobalTimeWrappedHourly * 4f) * 0.5f);
			ImbuementSlotDrawing.DrawItem(spriteBatch, itemType, center + shimmer, 44f, new Color(115, 255, 225) * (0.2f + impactPulse * 0.2f));
			DrawOrbitingSouls(spriteBatch, center);
			spriteBatch.Draw(frame, position, new Color(75, 245, 215) * (0.18f + impactPulse * 0.32f));
		}
		if (IsMouseHovering)
		{
			Main.instance.MouseText(itemType > ItemID.None ? Lang.GetItemNameValue(itemType) : "Select Weapon");
		}
	}

	private static float DrawSoulTransfer(SpriteBatch spriteBatch, Vector2 weaponCenter)
	{
		Texture2D glow = SoulOrbProjectile.GetGlowTexture();
		Texture2D ring = SoulOrbProjectile.GetRingTexture();
		Vector2 origin = glow.Size() * 0.5f;
		Vector2 essenceCenter = weaponCenter + new Vector2(0f, 94f);
		float strongestImpact = 0f;
		for (int index = 0; index < 3; index++)
		{
			float cycle = (Main.GlobalTimeWrappedHourly * 0.58f + index / 3f) % 1f;
			float side = index == 1 ? 1f : -1f;
			Vector2 control = Vector2.Lerp(essenceCenter, weaponCenter, 0.52f) + new Vector2(side * (18f + index * 4f), 0f);
			for (int trailIndex = 5; trailIndex >= 0; trailIndex--)
			{
				float trailProgress = MathHelper.Clamp(cycle - trailIndex * 0.027f, 0f, 1f);
				Vector2 trailPosition = QuadraticBezier(essenceCenter, control, weaponCenter, trailProgress);
				float strength = 1f - trailIndex / 6f;
				spriteBatch.Draw(glow, trailPosition, null, new Color(70, 235, 207) * (strength * 0.42f), 0f, origin,
					0.075f + strength * 0.045f, SpriteEffects.None, 0f);
			}

			Vector2 wispPosition = QuadraticBezier(essenceCenter, control, weaponCenter, cycle);
			float soulPulse = 0.92f + MathF.Sin((Main.GlobalTimeWrappedHourly + index) * 8f) * 0.08f;
			spriteBatch.Draw(glow, wispPosition, null, new Color(92, 255, 220) * 0.72f, 0f, origin, 0.19f * soulPulse, SpriteEffects.None, 0f);
			spriteBatch.Draw(ring, wispPosition, null, new Color(205, 255, 241) * 0.92f, 0f, origin, 0.13f * soulPulse, SpriteEffects.None, 0f);

			float arrival = MathHelper.Clamp((cycle - 0.84f) / 0.16f, 0f, 1f);
			strongestImpact = Math.Max(strongestImpact, MathF.Sin(arrival * MathHelper.Pi));
		}

		return strongestImpact;
	}

	private static void DrawOrbitingSouls(SpriteBatch spriteBatch, Vector2 center)
	{
		Texture2D glow = SoulOrbProjectile.GetGlowTexture();
		Texture2D ring = SoulOrbProjectile.GetRingTexture();
		Vector2 origin = glow.Size() * 0.5f;
		for (int index = 0; index < 2; index++)
		{
			float direction = index == 0 ? 1f : -1f;
			float phase = Main.GlobalTimeWrappedHourly * (2.2f + index * 0.35f) * direction + index * MathHelper.Pi;
			Vector2 orbit = new(MathF.Cos(phase) * 28f, MathF.Sin(phase) * 15f);
			float depth = MathHelper.Lerp(0.65f, 1f, (MathF.Sin(phase) + 1f) * 0.5f);
			Vector2 soulPosition = center + orbit;
			spriteBatch.Draw(glow, soulPosition, null, new Color(74, 242, 213) * (0.65f * depth), 0f, origin, 0.17f * depth, SpriteEffects.None, 0f);
			spriteBatch.Draw(ring, soulPosition, null, new Color(220, 255, 244) * depth, 0f, origin, 0.105f * depth, SpriteEffects.None, 0f);
		}
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
		Width.Set(52f, 0f);
		Height.Set(52f, 0f);
	}

	public void SetItem(int requestedItemType)
	{
		itemType = requestedItemType;
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		CalculatedStyle dimensions = GetDimensions();
		Rectangle area = dimensions.ToRectangle();
		ImbuementSlotDrawing.DrawSlot(spriteBatch, area, false);
		ImbuementSlotDrawing.DrawItem(spriteBatch, itemType, area.Center.ToVector2(), 34f);
		if (IsMouseHovering)
		{
			Main.instance.MouseText(itemType > ItemID.None ? Lang.GetItemNameValue(itemType) : "Select Essence");
		}
	}
}

internal static class ImbuementSlotDrawing
{
	public static void DrawSlot(SpriteBatch spriteBatch, Rectangle area, bool highlighted)
	{
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Color border = highlighted ? new Color(102, 201, 177) : new Color(66, 79, 81);
		spriteBatch.Draw(pixel, area, border);
		spriteBatch.Draw(pixel, new Rectangle(area.X + 2, area.Y + 2, area.Width - 4, area.Height - 4), new Color(28, 36, 43, 245));
	}

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
