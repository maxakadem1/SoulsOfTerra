using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SoulsOfTerra.Common;
using SoulsOfTerra.Content.Items;
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

internal sealed class SoulMenuState : UIState
{
	private enum MenuKind
	{
		Soulless,
		Shrine
	}

	private const int FeedbackDuration = 180;
	private UIPanel panel;
	private UIText title;
	private UIText subtitle;
	private UIText balance;
	private SoulActionRow primaryRow;
	private SoulActionRow secondaryRow;
	private UIElement essenceGrid;
	private SoulEssenceCard[] essenceCards;
	private UIPanel essenceDetails;
	private SoulItemIcon detailIcon;
	private UIText detailName;
	private UIText detailDescription;
	private UIText detailCost;
	private UITextPanel<string> condenseButton;
	private UIText feedback;
	private UITextPanel<string> closeButton;
	private MenuKind kind;
	private int npcIndex;
	private Point16 shrinePosition;
	private int feedbackTime;
	private int selectedEssenceIndex;

	public override void OnInitialize()
	{
		panel = new UIPanel();
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
		CreateEssenceCatalogue();

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
		BuildSoullessLayout();
		ClearFeedback();
		RefreshContent();
	}

	public void ConfigureShrine(Point16 requestedShrinePosition)
	{
		kind = MenuKind.Shrine;
		shrinePosition = requestedShrinePosition;
		selectedEssenceIndex = 0;
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
		panel.Height.Set(340f, 0f);
		panel.Append(title);
		panel.Append(subtitle);
		panel.Append(balance);
		panel.Append(primaryRow);
		panel.Append(secondaryRow);
		feedback.Top.Set(266f, 0f);
		panel.Append(feedback);
		closeButton.Top.Set(294f, 0f);
		panel.Append(closeButton);
	}

	private void BuildShrineLayout()
	{
		panel.RemoveAllChildren();
		panel.Height.Set(410f, 0f);
		panel.Append(title);
		panel.Append(subtitle);
		panel.Append(balance);
		panel.Append(essenceGrid);
		panel.Append(essenceDetails);
		panel.Append(condenseButton);
		feedback.Top.Set(340f, 0f);
		panel.Append(feedback);
		closeButton.Top.Set(368f, 0f);
		panel.Append(closeButton);
	}

	private void CreateEssenceCatalogue()
	{
		essenceGrid = new UIElement();
		essenceGrid.Width.Set(-32f, 1f);
		essenceGrid.Height.Set(124f, 0f);
		essenceGrid.Left.Set(16f, 0f);
		essenceGrid.Top.Set(70f, 0f);

		essenceCards = new SoulEssenceCard[8];
		for (int index = 0; index < essenceCards.Length; index++)
		{
			int selectedIndex = index;
			SoulEssenceCard card = new();
			card.Width.Set(116f, 0f);
			card.Height.Set(58f, 0f);
			card.Left.Set(4f + index % 4 * 124f, 0f);
			card.Top.Set(index / 4 * 66f, 0f);
			card.OnLeftClick += (_, _) => SelectEssence(selectedIndex);
			essenceCards[index] = card;
			essenceGrid.Append(card);
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
		condenseButton.Width.Set(170f, 0f);
		condenseButton.Height.Set(38f, 0f);
		condenseButton.HAlign = 0.5f;
		condenseButton.Top.Set(294f, 0f);
		condenseButton.OnLeftClick += (_, _) => UsePrimaryAction();
		condenseButton.OnMouseOver += (_, _) =>
		{
			if (selectedEssenceIndex == 0 && NPC.downedSlimeKing)
			{
				condenseButton.BackgroundColor = HasEnoughForSelectedEssence() ? new Color(55, 112, 91) : new Color(104, 65, 59);
			}
		};
		condenseButton.OnMouseOut += (_, _) => ApplyCondenseButtonStyle();
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
			RefreshSoullessContent();
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

	private void RefreshShrineContent()
	{
		title.SetText("Terra Shrine");
		subtitle.SetText($"World-wide strength: tier {SoulWorldSystem.TerraShrineTier}");
		bool slimeUnlocked = NPC.downedSlimeKing;
		essenceCards[0].SetContent(ModContent.ItemType<SlimeEssence>(), slimeUnlocked ? "Slime Essence" : "Unknown", slimeUnlocked, selectedEssenceIndex == 0);
		for (int index = 1; index < essenceCards.Length; index++)
		{
			essenceCards[index].SetContent(ItemID.FallenStar, "Unknown", false, selectedEssenceIndex == index);
		}

		if (selectedEssenceIndex == 0)
		{
			detailIcon.ItemType = slimeUnlocked ? ModContent.ItemType<SlimeEssence>() : ItemID.FallenStar;
			detailIcon.Opacity = slimeUnlocked ? 1f : 0.25f;
			detailName.SetText(slimeUnlocked ? "Slime Essence" : "Unknown Essence");
			detailDescription.SetText(slimeUnlocked ? "A royal, viscous echo condensed into matter." : "Defeat its source to reveal this echo.");
			detailCost.SetText(slimeUnlocked ? $"Cost: {SoulTransactions.SlimeEssenceCost:N0} souls" : "Requires King Slime");
			detailCost.TextColor = slimeUnlocked && !HasEnoughForSelectedEssence() ? new Color(238, 154, 137) : new Color(180, 238, 210);
		}
		else
		{
			detailIcon.ItemType = ItemID.FallenStar;
			detailIcon.Opacity = 0.25f;
			detailName.SetText("Unknown Essence");
			detailDescription.SetText("A silent space where another echo may awaken.");
			detailCost.SetText("Source undiscovered");
			detailCost.TextColor = new Color(130, 139, 140);
		}

		ApplyCondenseButtonStyle();
	}

	private void UsePrimaryAction()
	{
		if (kind == MenuKind.Soulless)
		{
			if (!HasSouls(SoulTransactions.CoreCost))
			{
				return;
			}

			bool completed = SendNpcTransaction(SoulMessageType.RequestCorePurchase, () => SoulTransactions.TryPurchaseCore(Main.LocalPlayer, npcIndex));
			ShowFeedback(completed ? "Broken Terra Blade acquired." : "Purchase request sent.", true);
		}
		else
		{
			if (selectedEssenceIndex != 0)
			{
				ShowFeedback("This echo has not awakened.", false);
				return;
			}

			if (!NPC.downedSlimeKing)
			{
				ShowFeedback("King Slime's echo has not awakened.", false);
				return;
			}

			if (!HasSouls(SoulTransactions.SlimeEssenceCost))
			{
				return;
			}

			bool completed = SendShrineTransaction();
			ShowFeedback(completed ? "Slime Essence condensed." : "Condensation request sent.", true);
		}
	}

	private bool HasEnoughForSelectedEssence()
	{
		return Main.LocalPlayer.GetModPlayer<SoulPlayer>().SoulBalance >= SoulTransactions.SlimeEssenceCost;
	}

	private void ApplyCondenseButtonStyle()
	{
		bool available = selectedEssenceIndex == 0 && NPC.downedSlimeKing;
		bool affordable = available && HasEnoughForSelectedEssence();
		condenseButton.SetText(available ? "Condense" : "Locked");
		condenseButton.BackgroundColor = !available ? new Color(43, 47, 51) : affordable ? new Color(43, 83, 70) : new Color(73, 52, 50);
		condenseButton.BorderColor = !available ? new Color(68, 72, 76) : affordable ? new Color(90, 143, 121) : new Color(123, 78, 71);
		condenseButton.TextColor = !available ? new Color(125, 130, 132) : affordable ? new Color(215, 244, 229) : new Color(236, 183, 171);
	}

	private void UseSecondaryAction()
	{
		if (kind != MenuKind.Soulless || SoulWorldSystem.GetNextUpgradeCost() <= 0)
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

	private bool SendShrineTransaction()
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			ModPacket packet = ModContent.GetInstance<SoulsOfTerra>().GetPacket();
			packet.Write((byte)SoulMessageType.RequestSlimeCondensation);
			packet.Write(shrinePosition.X);
			packet.Write(shrinePosition.Y);
			packet.Send();
			return false;
		}

		return SoulTransactions.TryCondenseSlimeEssence(Main.LocalPlayer, shrinePosition);
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
