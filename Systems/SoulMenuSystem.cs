using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SoulsOfTerra.Common;
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

	public static void OpenShrine(Point16 shrinePosition)
	{
		if (Main.dedServ || menuState is null)
		{
			return;
		}

		menuState.ConfigureShrine(shrinePosition);
		soulInterface.SetState(menuState);
	}

	public static void Close()
	{
		soulInterface?.SetState(null);
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
		Shrine
	}

	private enum SoullessTab
	{
		Services,
		Crystals
	}

	private const int FeedbackDuration = 180;
	private SoulMenuFramePanel panel;
	private UIText title;
	private UIText subtitle;
	private UIText balance;
	private SoulActionRow primaryRow;
	private SoulActionRow secondaryRow;
	private UITextPanel<string> servicesTabButton;
	private UITextPanel<string> crystalsTabButton;
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
	private UIText feedback;
	private UITextPanel<string> closeButton;
	private MenuKind kind;
	private SoullessTab soullessTab;
	private int npcIndex;
	private Point16 shrinePosition;
	private int feedbackTime;
	private int selectedEssenceIndex;
	private int selectedCrystalIndex;

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
		CreateSoullessTabs();
		CreateEssenceCatalogue();
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

	public void ConfigureShrine(Point16 requestedShrinePosition)
	{
		kind = MenuKind.Shrine;
		shrinePosition = requestedShrinePosition;
		selectedEssenceIndex = 0;
		essenceScrollBar.ViewPosition = 0f;
		BuildShrineLayout();
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
		panel.Height.Set(400f, 0f);
		panel.Append(title);
		panel.Append(subtitle);
		panel.Append(balance);
		panel.Append(servicesTabButton);
		panel.Append(crystalsTabButton);
		if (soullessTab == SoullessTab.Services)
		{
			primaryRow.Top.Set(108f, 0f);
			secondaryRow.Top.Set(198f, 0f);
			panel.Append(primaryRow);
			panel.Append(secondaryRow);
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

		feedback.Top.Set(326f, 0f);
		panel.Append(feedback);
		closeButton.Top.Set(358f, 0f);
		panel.Append(closeButton);
		ApplySoullessTabStyles();
	}

	private void BuildShrineLayout()
	{
		panel.RemoveAllChildren();
		panel.Height.Set(410f, 0f);
		panel.Append(title);
		panel.Append(subtitle);
		panel.Append(balance);
		panel.Append(essenceGrid);
		condenseButton.Top.Set(306f, 0f);
		panel.Append(condenseButton);
		feedback.Top.Set(348f, 0f);
		panel.Append(feedback);
		closeButton.Top.Set(368f, 0f);
		panel.Append(closeButton);
	}

	private void CreateEssenceCatalogue()
	{
		essenceDefinitions = new SoulEssenceDefinition[]
		{
			new(
				ModContent.ItemType<SlimeEssence>(),
				"Slime Essence",
				SoulTransactions.SlimeEssenceCost,
				"A royal, viscous echo condensed into matter.",
				() => NPC.downedSlimeKing,
				() => "Requires King Slime"),
			new(
				ModContent.ItemType<EyeEssence>(),
				"Eye Essence",
				SoulTransactions.EyeEssenceCost,
				"A watchful crimson echo bound into matter.",
				() => NPC.downedBoss1 && SoulWorldSystem.TerraShrineTier >= 1,
				GetEyeEssenceRequirement),
			new(
				ModContent.ItemType<MoonLordEssence>(),
				"Moon Lord Essence",
				SoulTransactions.MoonLordEssenceCost,
				"A celestial sovereign's echo condensed into matter.",
				() => NPC.downedMoonlord && SoulWorldSystem.TerraShrineTier >= 9,
				GetMoonLordEssenceRequirement)
		};

		essenceGrid = new UIElement();
		essenceGrid.Width.Set(-32f, 1f);
		essenceGrid.Height.Set(228f, 0f);
		essenceGrid.Left.Set(16f, 0f);
		essenceGrid.Top.Set(70f, 0f);

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
			row.Height.Set(72f, 0f);
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
				card.Height.Set(72f, 0f);
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
		RefreshShrineContent();
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
			RefreshShrineContent();
		}
	}

	private void RefreshSoullessContent()
	{
		title.SetText("Soulless");
		subtitle.SetText("Trade in that which clings to you.");
		bool canBuyCore = Main.LocalPlayer.GetModPlayer<SoulPlayer>().SoulBalance >= SoulTransactions.CoreCost;
		primaryRow.SetContent(
			ItemID.BrokenHeroSword,
			"Broken Terra Blade",
			$"Forms a Terra Shrine  •  {SoulTransactions.CoreCost:N0} souls",
			"Purchase",
			true,
			canBuyCore);

		long upgradeCost = SoulWorldSystem.GetNextUpgradeCost();
		if (upgradeCost <= 0)
		{
			secondaryRow.SetContent(ItemID.IronAnvil, "Terra Shrine", "All known strength has awakened", "Complete", false);
			return;
		}

		bool milestoneUnlocked = SoulWorldSystem.IsNextUpgradeUnlocked();
		bool canAfford = Main.LocalPlayer.GetModPlayer<SoulPlayer>().SoulBalance >= upgradeCost;
		string detail = milestoneUnlocked
			? $"World-wide tier {SoulWorldSystem.TerraShrineTier + 1}  •  {upgradeCost:N0} souls"
			: $"Requires {GetNextMilestoneName()}";
		secondaryRow.SetContent(ItemID.IronAnvil, "Strengthen Terra Shrine", detail, milestoneUnlocked ? "Strengthen" : "Locked", milestoneUnlocked, canAfford);
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

	private void RefreshShrineContent()
	{
		title.SetText("Terra Shrine");
		subtitle.SetText($"World-wide strength: tier {SoulWorldSystem.TerraShrineTier}");
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

	private void UsePrimaryAction()
	{
		if (kind == MenuKind.Soulless)
		{
			if (soullessTab == SoullessTab.Crystals)
			{
				UseCrystalConversion();
				return;
			}

			if (!HasSouls(SoulTransactions.CoreCost))
			{
				return;
			}

			bool completed = SendNpcTransaction(SoulMessageType.RequestCorePurchase, () => SoulTransactions.TryPurchaseCore(Main.LocalPlayer, npcIndex));
			ShowFeedback(completed ? "Broken Terra Blade acquired." : "Purchase request sent.", true);
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

			bool completed = SendShrineTransaction();
			string essenceName = essenceDefinitions[selectedEssenceIndex].Name;
			ShowFeedback(completed ? $"{essenceName} condensed." : "Condensation request sent.", true);
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
		if (kind != MenuKind.Soulless || soullessTab != SoullessTab.Services || SoulWorldSystem.GetNextUpgradeCost() <= 0)
		{
			return;
		}

		if (!SoulWorldSystem.IsNextUpgradeUnlocked())
		{
			ShowFeedback($"Defeat {GetNextMilestoneName()} first.", false);
			return;
		}

		long cost = SoulWorldSystem.GetNextUpgradeCost();
		if (!HasSouls(cost))
		{
			return;
		}

		bool completed = SendNpcTransaction(SoulMessageType.RequestShrineUpgrade, () => SoulTransactions.TryUpgradeShrine(Main.LocalPlayer, npcIndex));
		ShowFeedback(completed ? "Every Terra Shrine grows stronger." : "Strengthening request sent.", true);
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

	private bool SendShrineTransaction()
	{
		SoulMessageType messageType = selectedEssenceIndex switch
		{
			0 => SoulMessageType.RequestSlimeCondensation,
			1 => SoulMessageType.RequestEyeCondensation,
			2 => SoulMessageType.RequestMoonLordCondensation,
			_ => SoulMessageType.RequestSlimeCondensation
		};

		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			ModPacket packet = ModContent.GetInstance<SoulsOfTerra>().GetPacket();
			packet.Write((byte)messageType);
			packet.Write(shrinePosition.X);
			packet.Write(shrinePosition.Y);
			packet.Send();
			return false;
		}

		return selectedEssenceIndex switch
		{
			0 => SoulTransactions.TryCondenseSlimeEssence(Main.LocalPlayer, shrinePosition),
			1 => SoulTransactions.TryCondenseEyeEssence(Main.LocalPlayer, shrinePosition),
			2 => SoulTransactions.TryCondenseMoonLordEssence(Main.LocalPlayer, shrinePosition),
			_ => false
		};
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

	private static string GetEyeEssenceRequirement()
	{
		if (!NPC.downedBoss1)
		{
			return "Requires Eye of Cthulhu";
		}

		return SoulWorldSystem.TerraShrineTier < 1 ? "Requires Terra Shrine tier 1" : string.Empty;
	}

	private static string GetMoonLordEssenceRequirement()
	{
		if (!NPC.downedMoonlord)
		{
			return "Requires Moon Lord";
		}

		return SoulWorldSystem.TerraShrineTier < 9 ? "Requires Terra Shrine tier 9" : string.Empty;
	}

	private string GetSoulCrystalRequirement()
	{
		return selectedCrystalIndex switch
		{
			1 => "Requires Terra Shrine tier 1",
			2 => "Requires Terra Shrine tier 4",
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
		return SoulWorldSystem.TerraShrineTier switch
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

	private bool InteractionStillValid(Player player)
	{
		const float rangeSquared = 12f * 16f * 12f * 16f;
		if (kind == MenuKind.Soulless)
		{
			return npcIndex >= 0 && npcIndex < Main.maxNPCs && Main.npc[npcIndex].active
				&& Main.npc[npcIndex].type == ModContent.NPCType<SoullessNPC>()
				&& Vector2.DistanceSquared(player.Center, Main.npc[npcIndex].Center) <= rangeSquared;
		}

		Tile tile = Framing.GetTileSafely(shrinePosition.X, shrinePosition.Y);
		return tile.HasTile && tile.TileType == ModContent.TileType<TerraShrineTile>()
			&& Vector2.DistanceSquared(player.Center, shrinePosition.ToWorldCoordinates(24f, 16f)) <= rangeSquared;
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

internal sealed class SoulEssenceDefinition
{
	public int ItemType { get; }
	public string Name { get; }
	public long Cost { get; }
	public string Description { get; }
	public Func<bool> IsUnlocked { get; }
	public Func<string> GetRequirement { get; }

	public SoulEssenceDefinition(int itemType, string name, long cost, string description, Func<bool> isUnlocked, Func<string> getRequirement)
	{
		ItemType = itemType;
		Name = name;
		Cost = cost;
		Description = description;
		IsUnlocked = isUnlocked;
		GetRequirement = getRequirement;
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
		icon.Width.Set(38f, 0f);
		icon.Height.Set(38f, 0f);
		icon.HAlign = 0.5f;
		icon.Top.Set(1f, 0f);
		background.Append(icon);

		name = new UIText(string.Empty, 0.52f);
		name.HAlign = 0.5f;
		name.Top.Set(39f, 0f);
		background.Append(name);

		cost = new UIText(string.Empty, 0.48f);
		cost.HAlign = 0.5f;
		cost.Top.Set(55f, 0f);
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

		Texture2D texture = TextureAssets.Item[ItemType].Value;
		CalculatedStyle dimensions = GetDimensions();
		float scale = Math.Min(1f, 40f / Math.Max(texture.Width, texture.Height));
		Vector2 center = new(dimensions.X + dimensions.Width * 0.5f, dimensions.Y + dimensions.Height * 0.5f);
		spriteBatch.Draw(texture, center, null, Color.White * Opacity, 0f, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
	}
}
