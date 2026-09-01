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
		Main.playerInventory = false;
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
				apparatusInterface?.Draw(Main.spriteBatch, new GameTime());
				return true;
			}, InterfaceScaleType.UI));
	}
}

internal sealed class SoulApparatusState : UIState
{
	private const float InteractionRangeSquared = 12f * 16f * 12f * 16f;
	private SoulMenuFramePanel panel;
	private UIText title;
	private UIText subtitle;
	private UIText progress;
	private UITextPanel<string> dissolveTab;
	private UIElement recipeContent;
	private UIElement recipeListContainer;
	private UIList recipeList;
	private UIScrollbar recipeScrollBar;
	private UIText recipeHint;
	private UIText feedback;
	private UITextPanel<string> closeButton;
	private readonly List<ImbuementRecipeRow> rows = new();

	private ImbuementRitualCanvas ritualContent;
	private ImbuementWeaponSocket potionSocket;
	private ImbuementEssenceSocket essenceSocket;
	private UIText potionName;
	private UIText essenceName;
	private UIText ritualDrain;
	private UITextPanel<string> backButton;
	private UITextPanel<string> dissolveButton;
	private float ritualReveal;

	private Point16 apparatusPosition;
	private bool showingRecipes = true;
	private int selectedRecipe = -1;
	private int potionSlot = -1;
	private int essenceSlot = -1;
	internal bool IsRitualResonating => !showingRecipes && CanDissolveSelected();

	public override void OnInitialize()
	{
		CreateCatalogueFrame();
		CreateRecipePage();
		CreateRitualPage();
	}

	public void Open(Point16 topLeft)
	{
		apparatusPosition = topLeft;
		ShowRecipes();
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		Player player = Main.LocalPlayer;
		Tile tile = Framing.GetTileSafely(apparatusPosition.X, apparatusPosition.Y);
		if (!player.active || player.dead || Main.keyState.IsKeyDown(Keys.Escape) || !tile.HasTile
			|| tile.TileType != ModContent.TileType<SoulApparatusTile>()
			|| Vector2.DistanceSquared(player.Center, apparatusPosition.ToWorldCoordinates(24f, 24f)) > InteractionRangeSquared)
		{
			SoulApparatusSystem.Close();
			return;
		}

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
			ritualContent.Top.Set((1f - easedReveal) * 18f, 0f);
			Recalculate();
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
		panel = new SoulMenuFramePanel();
		panel.Width.Set(540f, 0f);
		panel.Height.Set(548f, 0f);
		panel.Left.Set(36f, 0f);
		panel.VAlign = 0.5f;
		panel.BackgroundColor = new Color(17, 22, 28, 245);
		panel.BorderColor = new Color(76, 111, 103, 255);

		title = new UIText("Soul Apparatus", 1.05f);
		title.Left.Set(20f, 0f);
		title.Top.Set(14f, 0f);

		subtitle = new UIText("Permanent potion soulspells", 0.72f);
		subtitle.Left.Set(21f, 0f);
		subtitle.Top.Set(43f, 0f);
		subtitle.TextColor = new Color(154, 177, 169);

		progress = new UIText(string.Empty, 0.82f);
		progress.HAlign = 1f;
		progress.Left.Set(-20f, 0f);
		progress.Top.Set(22f, 0f);
		progress.TextColor = new Color(180, 238, 210);

		dissolveTab = new UITextPanel<string>("Dissolve", 0.68f, false);
		dissolveTab.Width.Set(130f, 0f);
		dissolveTab.Height.Set(30f, 0f);
		dissolveTab.Left.Set(16f, 0f);
		dissolveTab.Top.Set(68f, 0f);
		dissolveTab.BackgroundColor = new Color(42, 72, 65);
		dissolveTab.BorderColor = new Color(117, 182, 151);
		dissolveTab.TextColor = new Color(220, 244, 231);

		feedback = new UIText(string.Empty, 0.72f);
		feedback.HAlign = 0.5f;
		feedback.Top.Set(486f, 0f);
		feedback.TextColor = new Color(147, 225, 183);

		closeButton = new UITextPanel<string>("Close", 0.72f, false);
		closeButton.Width.Set(92f, 0f);
		closeButton.Height.Set(32f, 0f);
		closeButton.HAlign = 0.5f;
		closeButton.Top.Set(506f, 0f);
		closeButton.BackgroundColor = new Color(48, 58, 66);
		closeButton.BorderColor = new Color(83, 103, 99);
		closeButton.OnMouseOver += (_, _) => closeButton.BackgroundColor = new Color(65, 82, 79);
		closeButton.OnMouseOut += (_, _) => closeButton.BackgroundColor = new Color(48, 58, 66);
		closeButton.OnLeftClick += (_, _) => SoulApparatusSystem.Close();
	}

	private void CreateRecipePage()
	{
		recipeContent = new UIElement();
		recipeContent.Width.Set(-32f, 1f);
		recipeContent.Height.Set(328f, 0f);
		recipeContent.Left.Set(16f, 0f);
		recipeContent.Top.Set(108f, 0f);

		recipeHint = new UIText("Select a complete recipe to begin its dissolution ritual.", 0.62f);
		recipeHint.TextColor = new Color(154, 177, 169);
		recipeContent.Append(recipeHint);

		recipeListContainer = new UIElement();
		recipeListContainer.Width.Set(0f, 1f);
		recipeListContainer.Height.Set(303f, 0f);
		recipeListContainer.Top.Set(25f, 0f);
		recipeContent.Append(recipeListContainer);

		recipeList = new UIList();
		recipeList.Width.Set(-26f, 1f);
		recipeList.Height.Set(0f, 1f);
		recipeList.ListPadding = 5f;
		recipeListContainer.Append(recipeList);

		// Use the same corrected scrollbar geometry as the Terraforge catalogue.
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
		ritualContent.Width.Set(480f, 0f);
		ritualContent.Height.Set(410f, 0f);
		ritualContent.HAlign = 0.5f;
		ritualContent.VAlign = 0.5f;

		potionSocket = new ImbuementWeaponSocket();
		potionSocket.HAlign = 0.5f;
		potionSocket.Top.Set(88f, 0f);

		potionName = new UIText("Select Potion", 0.57f);
		potionName.HAlign = 0.5f;
		potionName.VAlign = 0.5f;
		potionName.TextColor = new Color(205, 220, 212);
		CreateRitualLabelPlate(potionName, 181f, 250f);

		essenceSocket = new ImbuementEssenceSocket();
		essenceSocket.HAlign = 0.5f;
		essenceSocket.Top.Set(225f, 0f);

		essenceName = new UIText("Select Essence", 0.54f);
		essenceName.HAlign = 0.5f;
		essenceName.VAlign = 0.5f;
		essenceName.TextColor = new Color(166, 190, 181);
		CreateRitualLabelPlate(essenceName, 282f, 220f);

		ritualDrain = new UIText(string.Empty, 0.64f);
		ritualDrain.HAlign = 1f;
		ritualDrain.Left.Set(-16f, 0f);
		ritualDrain.Top.Set(18f, 0f);
		ritualDrain.TextColor = new Color(167, 218, 198);

		backButton = CreateRitualButton("Back to Recipes", 140f);
		backButton.OnLeftClick += (_, _) => ShowRecipes();
		backButton.BackgroundColor = new Color(40, 54, 58);
		backButton.BorderColor = new Color(78, 105, 100);
		backButton.OnMouseOver += (_, _) => backButton.BackgroundColor = new Color(54, 75, 74);
		backButton.OnMouseOut += (_, _) => backButton.BackgroundColor = new Color(40, 54, 58);

		dissolveButton = CreateRitualButton("Dissolve", 178f);
		dissolveButton.OnLeftClick += (_, _) => DissolveSelected();
		dissolveButton.OnMouseOver += (_, _) =>
		{
			if (CanDissolveSelected())
			{
				dissolveButton.BackgroundColor = new Color(39, 91, 81, 248);
			}
		};
		dissolveButton.OnMouseOut += (_, _) => ApplyDissolveButtonStyle();
	}

	private void ShowRecipes(string message = null)
	{
		showingRecipes = true;
		selectedRecipe = -1;
		potionSlot = -1;
		essenceSlot = -1;
		feedback.Remove();
		RemoveAllChildren();
		panel.RemoveAllChildren();
		Append(panel);
		panel.Append(title);
		panel.Append(subtitle);
		panel.Append(progress);
		panel.Append(dissolveTab);
		panel.Append(recipeContent);
		feedback.SetText(message ?? string.Empty);
		feedback.Top.Set(486f, 0f);
		panel.Append(feedback);
		panel.Append(closeButton);
		RebuildRows();
		RefreshProgress();
		Recalculate();
	}

	private void ShowRitual(int recipeIndex, int foundPotionSlot, int foundEssenceSlot)
	{
		showingRecipes = false;
		selectedRecipe = recipeIndex;
		potionSlot = foundPotionSlot;
		essenceSlot = foundEssenceSlot;
		feedback.Remove();
		RemoveAllChildren();
		ritualContent.RemoveAllChildren();
		Append(ritualContent);
		ritualContent.Append(potionSocket);
		ritualContent.Append(potionName.Parent);
		ritualContent.Append(essenceSocket);
		ritualContent.Append(essenceName.Parent);
		ritualContent.Append(ritualDrain);

		backButton.Left.Set(72f, 0f);
		backButton.Top.Set(326f, 0f);
		ritualContent.Append(backButton);
		dissolveButton.Left.Set(230f, 0f);
		dissolveButton.Top.Set(326f, 0f);
		ritualContent.Append(dissolveButton);

		feedback.SetText("The ingredients resonate.");
		feedback.Top.Set(374f, 0f);
		ritualContent.Append(feedback);
		ritualReveal = 0f;
		ritualContent.Reveal = 0f;
		ritualContent.Top.Set(18f, 0f);
		RefreshRitual();
		Recalculate();
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
			int foundEssence = FindInventorySlot(spell.EssenceItemType);
			bool learned = player.HasLearned(spell.Id);
			SoulEssenceRegistry.TryFindByItemType(spell.EssenceItemType, out SoulEssenceDefinition essence);
			bool unlocked = essence is not null && essence.IsUnlocked();
			bool ready = !learned && unlocked && foundPotion >= 0 && foundEssence >= 0;
			rows[index].SetContent(spell, ready, GetStatus(spell, essence, learned, foundPotion, foundEssence));
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
		SoulEssenceRegistry.TryFindByItemType(spell.EssenceItemType, out SoulEssenceDefinition essence);
		int foundPotion = FindInventorySlot(spell.PotionItemType);
		int foundEssence = FindInventorySlot(spell.EssenceItemType);
		if (Main.LocalPlayer.GetModPlayer<SoulSpellPlayer>().HasLearned(spell.Id)
			|| essence is null || !essence.IsUnlocked() || foundPotion < 0 || foundEssence < 0)
		{
			return;
		}

		ShowRitual(recipeIndex, foundPotion, foundEssence);
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
		essenceSocket.SetItem(spell.EssenceItemType);
		potionName.SetText(Lang.GetItemNameValue(spell.PotionItemType));
		essenceName.SetText(Lang.GetItemNameValue(spell.EssenceItemType));
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
			&& SlotMatches(potionSlot, spell.PotionItemType) && SlotMatches(essenceSlot, spell.EssenceItemType);
	}

	private void ApplyDissolveButtonStyle()
	{
		bool ready = CanDissolveSelected();
		dissolveButton.SetText(ready ? "Dissolve" : "No Resonance");
		dissolveButton.BackgroundColor = ready ? new Color(29, 66, 61, 242) : new Color(31, 35, 39, 232);
		dissolveButton.BorderColor = ready ? new Color(87, 181, 153) : new Color(62, 68, 71);
		dissolveButton.TextColor = ready ? new Color(214, 249, 232) : new Color(125, 130, 132);
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
			packet.Write((byte)essenceSlot);
			packet.Write(apparatusPosition.X);
			packet.Write(apparatusPosition.Y);
			packet.Send();
		}
		else
		{
			SoulTransactions.TryDissolveSoulspell(Main.LocalPlayer, apparatusPosition, selectedRecipe, potionSlot, essenceSlot);
		}

		ShowRecipes("Soulspell learned.");
	}

	private static string GetStatus(SoulSpellDefinition spell, SoulEssenceDefinition essence, bool learned,
		int potionSlot, int essenceSlot)
	{
		if (learned)
		{
			return "Learned";
		}
		if (essence is not null && !essence.IsUnlocked())
		{
			return essence.GetRequirement();
		}
		if (potionSlot < 0 && essenceSlot < 0)
		{
			return $"Missing {Lang.GetItemNameValue(spell.PotionItemType)} and {Lang.GetItemNameValue(spell.EssenceItemType)}";
		}
		if (potionSlot < 0)
		{
			return $"Missing {Lang.GetItemNameValue(spell.PotionItemType)}";
		}
		if (essenceSlot < 0)
		{
			return $"Missing {Lang.GetItemNameValue(spell.EssenceItemType)}";
		}
		return "Ready — select recipe";
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
