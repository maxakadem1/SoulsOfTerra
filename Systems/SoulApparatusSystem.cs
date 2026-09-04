using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SoulsOfTerra.Common;
using SoulsOfTerra.Content.Tiles;
using SoulsOfTerra.Players;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace SoulsOfTerra.Systems;

public sealed class SoulApparatusSystem : ModSystem
{
	private static UserInterface apparatusInterface;
	private static SoulApparatusState apparatusState;

	internal static UserInterface Interface => apparatusInterface;
	public static bool IsOpen => apparatusInterface?.CurrentState == apparatusState && apparatusState is not null;
	internal static bool IsDissolutionResonating() => IsOpen && apparatusState.IsRitualResonating;

	public override void Load()
	{
		if (Main.dedServ)
		{
			return;
		}

		apparatusInterface = new UserInterface();
		apparatusState = new SoulApparatusState();
		apparatusState.Activate();
	}

	public override void Unload()
	{
		apparatusInterface = null;
		apparatusState = null;
	}

	public static void Open(Point16 topLeft)
	{
		if (Main.dedServ || apparatusState is null)
		{
			return;
		}

		SoulMenuSystem.Close();
		SoulSpellBookSystem.Close();
		GraftingAltarSystem.Close();
		Main.playerInventory = true;
		apparatusState.Open(topLeft);
		apparatusInterface.SetState(apparatusState);
	}

	public static void Close() => apparatusInterface?.SetState(null);

	public override void UpdateUI(GameTime gameTime) => apparatusInterface?.Update(gameTime);

	public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
	{
		int mouseTextIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
		if (mouseTextIndex < 0)
		{
			return;
		}

		layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer("SoulsOfTerra: Soul Apparatus",
			() =>
			{
				apparatusState?.DrawShopFull();
				return true;
			}, InterfaceScaleType.UI));
	}
}

internal sealed class SoulApparatusState : UIState
{
	private const float InteractionRangeSquared = 12f * 16f * 12f * 16f;
	private const string DefaultRecipeHint = "Select a complete recipe to begin its dissolution ritual.";
	private const float RecipeListTop = 25f;
	private const float RecipeListHeight = ShopFullLayout.PanelHeight - ShopFullLayout.BodyTop
		- ShopFullLayout.InteriorBottomInset - RecipeListTop;
	private ShopFullPanel panel;
	private ShopFullCloseElement closeButton;
	private UIText title;
	private UIText progress;
	private UITextPanel<string> dissolveTab;
	private UIElement recipeContent;
	private UIElement recipeListContainer;
	private UIList recipeList;
	private UIScrollbar recipeScrollBar;
	private UIText recipeHint;
	private UIText feedback;
	private readonly List<ImbuementRecipeRow> rows = new();

	private ImbuementRitualCanvas ritualContent;
	private ImbuementWeaponSocket potionSocket;
	private ImbuementEssenceSocket essenceSocket;
	private UIText potionName;
	private UIText soulCostName;
	private UIText ritualDrain;
	private UITextPanel<string> backButton;
	private UITextPanel<string> dissolveButton;
	private float ritualReveal;

	private Point16 apparatusPosition;
	private bool showingRecipes = true;
	private int selectedRecipe = -1;
	private int potionSlot = -1;
	private float currentPanelLeft;
	private float currentPanelTop;
	internal bool IsRitualResonating => !showingRecipes && CanDissolveSelected();

	public override void OnInitialize()
	{
		CreateCatalogueFrame();
		CreateRecipePage();
		CreateRitualPage();
	}

	public override void OnActivate()
	{
		ApplyShopPlacement(force: true);
	}

	public void Open(Point16 topLeft)
	{
		apparatusPosition = topLeft;
		panel.SoulEffectSeed = unchecked(topLeft.X * 73856093 ^ topLeft.Y * 19349663);
		ShowRecipes();
	}

	internal void DrawShopFull()
	{
		if (!SoulApparatusSystem.IsOpen)
		{
			return;
		}

		ShopFullLayout.Draw(SoulApparatusSystem.Interface, this, panel, ref currentPanelLeft, ref currentPanelTop);
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		Player player = Main.LocalPlayer;
		Tile tile = Framing.GetTileSafely(apparatusPosition.X, apparatusPosition.Y);
		if (!player.active || player.dead || !Main.playerInventory || Main.keyState.IsKeyDown(Keys.Escape)
			|| !tile.HasTile
			|| tile.TileType != ModContent.TileType<SoulApparatusTile>()
			|| Vector2.DistanceSquared(player.Center, apparatusPosition.ToWorldCoordinates(24f, 24f)) > InteractionRangeSquared)
		{
			SoulApparatusSystem.Close();
			return;
		}

		ApplyShopPlacement();
		if (panel.Parent is not null && panel.ContainsPoint(Main.MouseScreen)
			|| ritualContent.Parent is not null && ritualContent.ContainsPoint(Main.MouseScreen))
		{
			player.mouseInterface = true;
		}

		if (ritualContent.Parent is not null && ritualReveal < 1f)
		{
			ritualReveal = Math.Min(1f, ritualReveal + 0.075f);
			float easedReveal = 1f - MathF.Pow(1f - ritualReveal, 3f);
			ritualContent.Reveal = easedReveal;
			ritualContent.Top.Set(ShopFullLayout.BodyTop + (1f - easedReveal) * 18f, 0f);
			ShopFullLayout.Recalculate(this, SoulApparatusSystem.Interface);
		}

		if (showingRecipes)
		{
			RefreshRows();
			RefreshProgress();
		}
		else
		{
			RefreshRitual();
		}
	}

	private void CreateCatalogueFrame()
	{
		panel = new ShopFullPanel();

		title = new UIText("Soul Apparatus", 1.05f);
		ShopFullLayout.PlaceTitle(title);

		progress = new UIText(string.Empty, 0.82f);
		progress.HAlign = 1f;
		// Keep progress below the title and clear of the close control.
		progress.Left.Set(-ShopFullLayout.TitleLeft, 0f);
		progress.Top.Set(ShopFullLayout.SubtitleTop, 0f);
		progress.TextColor = SoullessUIPalette.AccentText;

		dissolveTab = new UITextPanel<string>("Dissolve", 0.68f, false);
		dissolveTab.Width.Set(130f, 0f);
		dissolveTab.Height.Set(30f, 0f);
		dissolveTab.Left.Set(ShopFullLayout.InteriorLeft, 0f);
		dissolveTab.Top.Set(ShopFullLayout.TabsTop - 4f, 0f);
		dissolveTab.BackgroundColor = SoullessUIPalette.AccentSurface;
		dissolveTab.BorderColor = SoullessUIPalette.Accent;
		dissolveTab.TextColor = SoullessUIPalette.AccentText;

		feedback = new UIText(string.Empty, 0.72f);
		feedback.HAlign = 0.5f;
		feedback.TextColor = SoullessUIPalette.AccentMuted;

		closeButton = new ShopFullCloseElement();
		ShopFullLayout.PlaceClose(closeButton);
		closeButton.OnMouseOver += (_, _) => closeButton.Hovered = true;
		closeButton.OnMouseOut += (_, _) => closeButton.Hovered = false;
		closeButton.OnLeftClick += (_, _) => SoulApparatusSystem.Close();
	}

	private void CreateRecipePage()
	{
		recipeContent = new UIElement();
		recipeContent.Width.Set(-(ShopFullLayout.InteriorLeft * 2f), 1f);
		recipeContent.Height.Set(RecipeListTop + RecipeListHeight, 0f);
		recipeContent.Left.Set(ShopFullLayout.InteriorLeft, 0f);
		recipeContent.Top.Set(ShopFullLayout.BodyTop, 0f);

		recipeHint = new UIText(DefaultRecipeHint, 0.62f);
		recipeHint.TextColor = SoullessUIPalette.TextSecondary;
		recipeContent.Append(recipeHint);

		recipeListContainer = new UIElement();
		recipeListContainer.Width.Set(0f, 1f);
		recipeListContainer.Height.Set(RecipeListHeight, 0f);
		recipeListContainer.Top.Set(RecipeListTop, 0f);
		recipeContent.Append(recipeListContainer);

		recipeList = new UIList();
		// Inset animated row borders so the list scissor does not clip their left edge.
		recipeList.Left.Set(3f, 0f);
		recipeList.Width.Set(-29f, 1f);
		recipeList.Height.Set(0f, 1f);
		// Six rows use the full interior height while staying clear of the lower frame.
		recipeList.ListPadding = 4.5f;
		recipeListContainer.Append(recipeList);

		recipeScrollBar = new FixedUIScrollbar(SoulApparatusSystem.Interface);
		recipeScrollBar.Height.Set(0f, 1f);
		recipeScrollBar.HAlign = 1f;
		recipeList.SetScrollbar(recipeScrollBar);
		recipeListContainer.Append(recipeScrollBar);
	}

	private void CreateRitualPage()
	{
		ritualContent = new ImbuementRitualCanvas("DISSOLUTION RITUAL",
			"Dissolve a draught into a permanent rite.");
		ritualContent.DrawHeader = false;
		ritualContent.Width.Set(-(ShopFullLayout.InteriorLeft * 2f), 1f);
		ritualContent.Height.Set(430f, 0f);
		ritualContent.Left.Set(ShopFullLayout.InteriorLeft, 0f);
		ritualContent.Top.Set(ShopFullLayout.BodyTop, 0f);

		potionSocket = new ImbuementWeaponSocket();
		potionSocket.HAlign = 0.5f;
		potionSocket.Top.Set(28f, 0f);

		potionName = new UIText("Select Potion", 0.57f);
		potionName.HAlign = 0.5f;
		potionName.VAlign = 0.5f;
		potionName.TextColor = SoullessUIPalette.TextPrimary;
		CreateRitualLabelPlate(potionName, 108f, 250f);

		essenceSocket = new ImbuementEssenceSocket();
		essenceSocket.HAlign = 0.5f;
		essenceSocket.Top.Set(155f, 0f);

		soulCostName = new UIText($"{SoulTransactions.SoulspellLearnCost:N0} souls", 0.54f);
		soulCostName.HAlign = 0.5f;
		soulCostName.VAlign = 0.5f;
		soulCostName.TextColor = SoullessUIPalette.TextSecondary;
		CreateRitualLabelPlate(soulCostName, 230f, 220f);

		ritualDrain = new UIText(string.Empty, 0.64f);
		ritualDrain.HAlign = 1f;
		ritualDrain.Left.Set(-8f, 0f);
		ritualDrain.Top.Set(4f, 0f);
		ritualDrain.TextColor = SoullessUIPalette.AccentMuted;

		backButton = CreateRitualButton("Back to Recipes", 140f);
		backButton.OnLeftClick += (_, _) => ShowRecipes();
		backButton.BackgroundColor = SoullessUIPalette.SurfaceRaised;
		backButton.BorderColor = SoullessUIPalette.Steel;
		backButton.OnMouseOver += (_, _) =>
		{
			backButton.BackgroundColor = SoullessUIPalette.SurfaceHover;
			backButton.BorderColor = SoullessUIPalette.AccentHoverBorder;
		};
		backButton.OnMouseOut += (_, _) =>
		{
			backButton.BackgroundColor = SoullessUIPalette.SurfaceRaised;
			backButton.BorderColor = SoullessUIPalette.Steel;
		};

		dissolveButton = CreateRitualButton("Dissolve", 178f);
		dissolveButton.OnLeftClick += (_, _) => DissolveSelected();
		dissolveButton.OnMouseOver += (_, _) =>
		{
			if (CanDissolveSelected())
			{
				dissolveButton.BackgroundColor = SoullessUIPalette.AccentSurfaceHover;
			}
		};
		dissolveButton.OnMouseOut += (_, _) => ApplyDissolveButtonStyle();
	}

	private void ApplyShopPlacement(bool force = false)
	{
		if (!ShopFullLayout.TryPlaceBesideInventory(panel, ref currentPanelLeft, ref currentPanelTop, force))
		{
			return;
		}

		ShopFullLayout.Recalculate(this, SoulApparatusSystem.Interface);
	}

	private void ShowRecipes(string message = null)
	{
		showingRecipes = true;
		selectedRecipe = -1;
		potionSlot = -1;
		feedback.Remove();
		RemoveAllChildren();
		panel.RemoveAllChildren();
		Append(panel);
		panel.Append(title);
		panel.Append(progress);
		panel.Append(dissolveTab);
		panel.Append(recipeContent);
		// Catalogue feedback temporarily replaces the instructional hint.
		recipeHint.SetText(message ?? DefaultRecipeHint);
		recipeHint.TextColor = message is null ? SoullessUIPalette.TextSecondary : SoullessUIPalette.AccentMuted;
		panel.Append(closeButton);
		RebuildRows();
		RefreshProgress();
		ApplyShopPlacement(force: true);
	}

	private void ShowRitual(int recipeIndex, int foundPotionSlot)
	{
		showingRecipes = false;
		selectedRecipe = recipeIndex;
		potionSlot = foundPotionSlot;
		feedback.Remove();
		RemoveAllChildren();
		panel.RemoveAllChildren();
		Append(panel);
		panel.Append(title);
		panel.Append(progress);
		panel.Append(closeButton);
		panel.Append(dissolveTab);
		// Keep the shared ShopUI_full chrome; only the interior switches to the ritual.
		ritualContent.RemoveAllChildren();
		ritualContent.Width.Set(-(ShopFullLayout.InteriorLeft * 2f), 1f);
		ritualContent.Height.Set(430f, 0f);
		ritualContent.Left.Set(ShopFullLayout.InteriorLeft, 0f);
		ritualContent.Top.Set(ShopFullLayout.BodyTop, 0f);
		ritualContent.HAlign = 0f;
		ritualContent.VAlign = 0f;
		panel.Append(ritualContent);
		ritualContent.Append(potionSocket);
		ritualContent.Append(potionName.Parent);
		ritualContent.Append(essenceSocket);
		ritualContent.Append(soulCostName.Parent);
		ritualContent.Append(ritualDrain);

		backButton.Left.Set(20f, 0f);
		backButton.Top.Set(340f, 0f);
		ritualContent.Append(backButton);
		dissolveButton.Left.Set(176f, 0f);
		dissolveButton.Top.Set(340f, 0f);
		ritualContent.Append(dissolveButton);

		feedback.SetText("The draught and souls resonate.");
		feedback.Top.Set(390f, 0f);
		ritualContent.Append(feedback);
		ritualReveal = 0f;
		ritualContent.Reveal = 0f;
		ritualContent.Top.Set(ShopFullLayout.BodyTop + 18f, 0f);
		RefreshRitual();
		ApplyShopPlacement(force: true);
	}

	private void RebuildRows()
	{
		recipeList.Clear();
		rows.Clear();
		for (int index = 0; index < SoulSpellRegistry.PotionSpells.Length; index++)
		{
			int recipeIndex = index;
			ImbuementRecipeRow row = new();
			row.Width.Set(0f, 1f);
			row.Height.Set(72f, 0f);
			row.SetAction(() => SelectRecipe(recipeIndex));
			recipeList.Add(row);
			rows.Add(row);
		}

		RefreshRows();
	}

	private void RefreshRows()
	{
		SoulSpellPlayer player = Main.LocalPlayer.GetModPlayer<SoulSpellPlayer>();
		for (int index = 0; index < rows.Count; index++)
		{
			SoulSpellDefinition spell = SoulSpellRegistry.PotionSpells[index];
			int foundPotion = FindInventorySlot(spell.PotionItemType);
			bool learned = player.HasLearned(spell.Id);
			bool canAfford = CanAffordLearn();
			bool ready = !learned && foundPotion >= 0 && canAfford;
			rows[index].SetContent(spell, ready, GetStatus(spell, learned, foundPotion, canAfford));
		}
	}

	private void RefreshProgress()
	{
		SoulSpellPlayer player = Main.LocalPlayer.GetModPlayer<SoulSpellPlayer>();
		int learned = 0;
		foreach (SoulSpellDefinition spell in SoulSpellRegistry.PotionSpells)
		{
			learned += player.HasLearned(spell.Id) ? 1 : 0;
		}
		progress.SetText($"Learned: {learned}/{SoulSpellRegistry.PotionSpells.Length}");
	}

	private void SelectRecipe(int recipeIndex)
	{
		SoulSpellDefinition spell = SoulSpellRegistry.PotionSpells[recipeIndex];
		int foundPotion = FindInventorySlot(spell.PotionItemType);
		if (Main.LocalPlayer.GetModPlayer<SoulSpellPlayer>().HasLearned(spell.Id)
			|| foundPotion < 0 || !CanAffordLearn())
		{
			return;
		}

		ShowRitual(recipeIndex, foundPotion);
	}

	private void RefreshRitual()
	{
		if (selectedRecipe < 0 || selectedRecipe >= SoulSpellRegistry.PotionSpells.Length)
		{
			ShowRecipes();
			return;
		}

		SoulSpellDefinition spell = SoulSpellRegistry.PotionSpells[selectedRecipe];
		bool ready = CanDissolveSelected();
		potionSocket.SetItem(spell.PotionItemType, ready);
		essenceSocket.SetSouls(SoulTransactions.SoulspellLearnCost);
		potionName.SetText(Lang.GetItemNameValue(spell.PotionItemType));
		soulCostName.SetText($"{SoulTransactions.SoulspellLearnCost:N0} souls");
		ritualDrain.SetText($"{spell.Name}  •  {spell.CostText}");
		ApplyDissolveButtonStyle();
	}

	private bool CanDissolveSelected()
	{
		if (selectedRecipe < 0 || selectedRecipe >= SoulSpellRegistry.PotionSpells.Length)
		{
			return false;
		}

		SoulSpellDefinition spell = SoulSpellRegistry.PotionSpells[selectedRecipe];
		return !Main.LocalPlayer.GetModPlayer<SoulSpellPlayer>().HasLearned(spell.Id)
			&& SlotMatches(potionSlot, spell.PotionItemType) && CanAffordLearn();
	}

	private void ApplyDissolveButtonStyle()
	{
		bool ready = CanDissolveSelected();
		dissolveButton.SetText(ready ? "Dissolve" : "No Resonance");
		dissolveButton.BackgroundColor = ready ? SoullessUIPalette.AccentSurface : SoullessUIPalette.SurfaceDisabled;
		dissolveButton.BorderColor = ready ? SoullessUIPalette.Accent : SoullessUIPalette.SteelMuted;
		dissolveButton.TextColor = ready ? SoullessUIPalette.AccentText : SoullessUIPalette.TextMuted;
	}

	private void DissolveSelected()
	{
		if (!CanDissolveSelected())
		{
			return;
		}

		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			ModPacket packet = ModContent.GetInstance<SoulsOfTerra>().GetPacket();
			packet.Write((byte)SoulMessageType.RequestSoulspellDissolution);
			packet.Write((byte)selectedRecipe);
			packet.Write((byte)potionSlot);
			packet.Write(apparatusPosition.X);
			packet.Write(apparatusPosition.Y);
			packet.Send();
		}
		else
		{
			SoulTransactions.TryDissolveSoulspell(Main.LocalPlayer, apparatusPosition, selectedRecipe, potionSlot);
		}

		ShowRecipes("Soulspell learned.");
	}

	private static string GetStatus(SoulSpellDefinition spell, bool learned, int potionSlot, bool canAfford)
	{
		if (learned)
		{
			return "Learned";
		}
		if (potionSlot < 0 && !canAfford)
		{
			return $"Missing {Lang.GetItemNameValue(spell.PotionItemType)} and {SoulTransactions.SoulspellLearnCost:N0} souls";
		}
		if (potionSlot < 0)
		{
			return $"Missing {Lang.GetItemNameValue(spell.PotionItemType)}";
		}
		if (!canAfford)
		{
			return $"Need {SoulTransactions.SoulspellLearnCost:N0} souls";
		}
		return "Ready — select recipe";
	}

	private static bool CanAffordLearn() =>
		Main.LocalPlayer.GetModPlayer<SoulPlayer>().SoulBalance >= SoulTransactions.SoulspellLearnCost;

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

	private static bool SlotMatches(int slot, int itemType)
	{
		return slot >= 0 && slot < Main.LocalPlayer.inventory.Length
			&& Main.LocalPlayer.inventory[slot].type == itemType && Main.LocalPlayer.inventory[slot].stack > 0;
	}

	private static UITextPanel<string> CreateRitualButton(string text, float width)
	{
		UITextPanel<string> button = new(text, 0.64f, false);
		button.Width.Set(width, 0f);
		button.Height.Set(36f, 0f);
		return button;
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
}
