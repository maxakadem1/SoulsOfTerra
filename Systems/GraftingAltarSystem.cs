using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SoulsOfTerra.Common;
using SoulsOfTerra.Content.Tiles;
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

public sealed class GraftingAltarSystem : ModSystem
{
	private static UserInterface graftingInterface;
	private static GraftingAltarState graftingState;

	public static bool IsOpen => graftingInterface?.CurrentState == graftingState && graftingState is not null;

	public override void Load()
	{
		if (Main.dedServ)
		{
			return;
		}

		graftingInterface = new UserInterface();
		graftingState = new GraftingAltarState();
		graftingState.Activate();
	}

	public override void Unload()
	{
		graftingInterface = null;
		graftingState = null;
	}

	public static void Open(Point16 topLeft)
	{
		if (Main.dedServ || graftingState is null)
		{
			return;
		}

		SoulMenuSystem.Close();
		SoulApparatusSystem.Close();
		SoulSpellBookSystem.Close();
		Main.playerInventory = true;
		graftingState.Open(topLeft);
		graftingInterface.SetState(graftingState);
	}

	public static void Close() => graftingInterface?.SetState(null);

	public override void UpdateUI(GameTime gameTime)
	{
		if (graftingInterface?.CurrentState is not null)
		{
			graftingInterface.Update(gameTime);
		}
	}

	public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
	{
		int mouseTextIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
		if (mouseTextIndex < 0)
		{
			return;
		}

		layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer("SoulsOfTerra: Grafting Altar",
			() =>
			{
				graftingInterface?.Draw(Main.spriteBatch, new GameTime());
				return true;
			}, InterfaceScaleType.UI));
	}
}

internal sealed class GraftingAltarState : UIState
{
	private const float InteractionRangeSquared = 12f * 16f * 12f * 16f;
	private const float InventoryRight = 660f;
	private const float PreferredPanelWidth = 456f;
	private const float MinimumPanelWidth = 340f;
	private const float PreferredPanelHeight = 548f;
	private const float MinimumPanelHeight = 480f;
	private const int CursorInventorySlot = 58;

	private SoulMenuFramePanel panel;
	private UIText title;
	private UIText subtitle;
	private UIText insertInstruction;
	private UIText removeInstruction;
	private UITextPanel<string> closeButton;
	private readonly MutationSocketElement[] sockets = new MutationSocketElement[MutationPlayer.SlotCount];
	private MutationDetailsElement details;
	private Point16 altarPosition;
	private int selectedSlot = -1;
	private float currentPanelWidth;
	private float currentPanelHeight;

	public override void OnInitialize()
	{
		CreatePanel();
		CreateSockets();
		CreateDetails();
		Append(panel);
		ApplyLayout(force: true);
	}

	public void Open(Point16 topLeft)
	{
		altarPosition = topLeft;
		RefreshLocalizedText();
		SelectFirstOccupied();
		ApplyLayout(force: true);
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		Player player = Main.LocalPlayer;
		Tile tile = Framing.GetTileSafely(altarPosition.X, altarPosition.Y);
		if (!player.active || player.dead || !Main.playerInventory || Main.keyState.IsKeyDown(Keys.Escape)
			|| !tile.HasTile || tile.TileType != ModContent.TileType<GraftingAltarTile>()
			|| Vector2.DistanceSquared(player.Center, altarPosition.ToWorldCoordinates(24f, 24f)) > InteractionRangeSquared)
		{
			GraftingAltarSystem.Close();
			return;
		}

		ApplyLayout();
		RefreshSelection();
		if (panel.ContainsPoint(Main.MouseScreen))
		{
			player.mouseInterface = true;
		}
	}

	private void CreatePanel()
	{
		panel = new SoulMenuFramePanel
		{
			BackgroundColor = SoullessUIPalette.Panel,
			BorderColor = SoullessUIPalette.PanelBorder
		};

		title = new UIText(Localize("MutationTitle"), 1.05f);
		title.Left.Set(22f, 0f);
		title.Top.Set(14f, 0f);
		panel.Append(title);

		subtitle = new UIText(Localize("MutationSubtitle"), 0.68f);
		subtitle.Left.Set(23f, 0f);
		subtitle.Top.Set(44f, 0f);
		subtitle.TextColor = SoullessUIPalette.TextSecondary;
		panel.Append(subtitle);

		closeButton = new UITextPanel<string>("×", 0.82f, false);
		closeButton.Width.Set(34f, 0f);
		closeButton.Height.Set(30f, 0f);
		closeButton.Top.Set(12f, 0f);
		closeButton.BackgroundColor = SoullessUIPalette.SurfaceRaised;
		closeButton.BorderColor = SoullessUIPalette.Steel;
		closeButton.OnMouseOver += (_, _) => SetCloseButtonHover(true);
		closeButton.OnMouseOut += (_, _) => SetCloseButtonHover(false);
		closeButton.OnLeftClick += (_, _) => GraftingAltarSystem.Close();
		panel.Append(closeButton);

		insertInstruction = new UIText(Localize("MutationInsertInstruction"), 0.59f);
		insertInstruction.Left.Set(23f, 0f);
		insertInstruction.Top.Set(77f, 0f);
		insertInstruction.TextColor = SoullessUIPalette.TextSecondary;
		panel.Append(insertInstruction);

		removeInstruction = new UIText(Localize("MutationRemoveInstruction"), 0.59f);
		removeInstruction.Left.Set(23f, 0f);
		removeInstruction.Top.Set(99f, 0f);
		removeInstruction.TextColor = SoullessUIPalette.WarningText;
		panel.Append(removeInstruction);
	}

	private void CreateSockets()
	{
		for (int slot = 0; slot < sockets.Length; slot++)
		{
			int capturedSlot = slot;
			sockets[slot] = new MutationSocketElement(slot);
			sockets[slot].OnLeftClick += (_, _) => HandleSocketLeftClick(capturedSlot);
			sockets[slot].OnRightClick += (_, _) => HandleSocketRightClick(capturedSlot);
			panel.Append(sockets[slot]);
		}
	}

	private void CreateDetails()
	{
		details = new MutationDetailsElement
		{
			BackgroundColor = SoullessUIPalette.SurfaceInset,
			BorderColor = SoullessUIPalette.SteelMuted
		};
		panel.Append(details);
	}

	private void ApplyLayout(bool force = false)
	{
		float virtualWidth = Main.screenWidth / Main.UIScale;
		float virtualHeight = Main.screenHeight / Main.UIScale;
		float availableWidth = virtualWidth - InventoryRight - 18f;
		float panelWidth = MathHelper.Clamp(availableWidth, MinimumPanelWidth, PreferredPanelWidth);
		float panelHeight = MathHelper.Clamp(virtualHeight - 54f, MinimumPanelHeight, PreferredPanelHeight);
		if (!force && Math.Abs(panelWidth - currentPanelWidth) < 0.5f
			&& Math.Abs(panelHeight - currentPanelHeight) < 0.5f)
		{
			return;
		}

		currentPanelWidth = panelWidth;
		currentPanelHeight = panelHeight;
		// Compact widths preserve the three-column composition instead of covering the inventory.
		panel.Left.Set(Math.Min(InventoryRight, virtualWidth - panelWidth - 12f), 0f);
		panel.Top.Set(Math.Max(38f, (virtualHeight - panelHeight) * 0.5f), 0f);
		panel.Width.Set(panelWidth, 0f);
		panel.Height.Set(panelHeight, 0f);
		closeButton.Left.Set(panelWidth - 52f, 0f);

		float socketGap = 8f;
		float socketsLeft = 20f;
		float socketWidth = (panelWidth - socketsLeft * 2f - socketGap * 2f) / sockets.Length;
		for (int slot = 0; slot < sockets.Length; slot++)
		{
			sockets[slot].Left.Set(socketsLeft + slot * (socketWidth + socketGap), 0f);
			sockets[slot].Top.Set(128f, 0f);
			sockets[slot].Width.Set(socketWidth, 0f);
			sockets[slot].Height.Set(112f, 0f);
		}

		details.Left.Set(20f, 0f);
		details.Top.Set(258f, 0f);
		details.Width.Set(panelWidth - 40f, 0f);
		details.Height.Set(panelHeight - 278f, 0f);
		Recalculate();
	}

	private void HandleSocketLeftClick(int slot)
	{
		MutationPlayer mutationPlayer = Main.LocalPlayer.GetModPlayer<MutationPlayer>();
		if (!mutationPlayer.IsSlotAvailable(slot))
		{
			selectedSlot = slot;
			RefreshSelection();
			return;
		}

		Item held = Main.mouseItem;
		if (held.IsAir)
		{
			selectedSlot = slot;
			RefreshSelection();
			return;
		}
		if (mutationPlayer.GetMutation(slot) != MutationId.None)
		{
			ShowMessage("MutationOccupiedFeedback");
			return;
		}
		if (!MutationRegistry.TryFindByItemType(held.type, out MutationDefinition definition))
		{
			ShowMessage("MutationEssenceOnlyFeedback");
			return;
		}
		if (!definition.Implemented)
		{
			ShowMessage("MutationUndiscoveredFeedback");
			return;
		}
		if (mutationPlayer.Contains(definition.Id))
		{
			ShowMessage("MutationDuplicateFeedback");
			return;
		}

		selectedSlot = slot;
		RequestGraft(slot);
		RefreshSelection();
	}

	private void HandleSocketRightClick(int slot)
	{
		MutationPlayer mutationPlayer = Main.LocalPlayer.GetModPlayer<MutationPlayer>();
		if (!mutationPlayer.IsSlotAvailable(slot) || mutationPlayer.GetMutation(slot) == MutationId.None)
		{
			return;
		}

		// Removal is deliberate and immediate; the permanent behavior is stated beside the sockets.
		RequestPurge(slot);
		if (selectedSlot == slot)
		{
			SelectFirstOccupied(slot);
		}
		RefreshSelection();
	}

	private void RequestGraft(int slot)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			ModPacket packet = ModContent.GetInstance<SoulsOfTerra>().GetPacket();
			packet.Write((byte)SoulMessageType.RequestGraftMutation);
			packet.Write((byte)slot);
			// Multiplayer mirrors Main.mouseItem into inventory slot 58 for server validation.
			packet.Write((byte)CursorInventorySlot);
			packet.Write(Main.mouseItem.type);
			packet.Write(altarPosition.X);
			packet.Write(altarPosition.Y);
			packet.Send();
			return;
		}

		SoulTransactions.TryGraftMutationFromCursor(Main.LocalPlayer, altarPosition, slot, Main.mouseItem);
	}

	private void RequestPurge(int slot)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			ModPacket packet = ModContent.GetInstance<SoulsOfTerra>().GetPacket();
			packet.Write((byte)SoulMessageType.RequestPurgeMutation);
			packet.Write((byte)slot);
			packet.Write(altarPosition.X);
			packet.Write(altarPosition.Y);
			packet.Send();
			return;
		}

		SoulTransactions.TryPurgeMutation(Main.LocalPlayer, altarPosition, slot);
	}

	private void SelectFirstOccupied(int exceptSlot = -1)
	{
		MutationPlayer mutationPlayer = Main.LocalPlayer.GetModPlayer<MutationPlayer>();
		selectedSlot = -1;
		for (int slot = 0; slot < sockets.Length; slot++)
		{
			if (slot != exceptSlot && mutationPlayer.IsSlotAvailable(slot)
				&& mutationPlayer.GetMutation(slot) != MutationId.None)
			{
				selectedSlot = slot;
				break;
			}
		}
	}

	private void RefreshSelection()
	{
		for (int slot = 0; slot < sockets.Length; slot++)
		{
			sockets[slot].Selected = selectedSlot == slot;
		}
		details.SelectedSlot = selectedSlot;
	}

	private void SetCloseButtonHover(bool hovered)
	{
		closeButton.BackgroundColor = hovered ? SoullessUIPalette.SurfaceHover : SoullessUIPalette.SurfaceRaised;
		closeButton.BorderColor = hovered ? SoullessUIPalette.AccentHoverBorder : SoullessUIPalette.Steel;
	}

	private void RefreshLocalizedText()
	{
		// UIState initializes during loading, so resolve localized values again when the panel opens.
		title.SetText(Localize("MutationTitle"));
		subtitle.SetText(Localize("MutationSubtitle"));
		insertInstruction.SetText(Localize("MutationInsertInstruction"));
		removeInstruction.SetText(Localize("MutationRemoveInstruction"));
	}

	private static void ShowMessage(string key)
	{
		Main.NewText(Localize(key), SoullessUIPalette.Warning);
	}

	private static string Localize(string key) => Language.GetTextValue($"Mods.SoulsOfTerra.UI.{key}");
}

internal sealed class MutationSocketElement : UIElement
{
	private const int SocketSize = 74;
	private readonly int slot;

	public bool Selected { get; set; }

	public MutationSocketElement(int slotIndex)
	{
		slot = slotIndex;
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		CalculatedStyle dimensions = GetDimensions();
		MutationPlayer mutationPlayer = Main.LocalPlayer.GetModPlayer<MutationPlayer>();
		bool available = mutationPlayer.IsSlotAvailable(slot);
		MutationId id = mutationPlayer.GetMutation(slot);
		bool occupied = MutationRegistry.TryGet(id, out MutationDefinition definition);
		Rectangle socket = new((int)(dimensions.Center().X - SocketSize * 0.5f), (int)dimensions.Y,
			SocketSize, SocketSize);
		DrawSocket(spriteBatch, socket, available);

		string label;
		if (!available)
		{
			DrawLockedIcon(spriteBatch, socket.Center.ToVector2());
			label = Localize("MutationLockedName");
		}
		else if (occupied)
		{
			ImbuementSlotDrawing.DrawItem(spriteBatch, definition.EssenceItemType,
				socket.Center.ToVector2(), 50f);
			label = definition.DisplayName;
		}
		else
		{
			DrawEmptyMark(spriteBatch, socket.Center.ToVector2());
			label = Localize("MutationEmptyName");
		}

		Color labelColor = Selected ? SoullessUIPalette.AccentText
			: available ? SoullessUIPalette.TextSecondary : SoullessUIPalette.TextMuted;
		Utils.DrawBorderString(spriteBatch, label,
			new Vector2(dimensions.Center().X, dimensions.Y + 82f), labelColor, 0.58f, 0.5f);

		if (IsMouseHovering)
		{
			Main.instance.MouseText(GetTooltip(available, occupied, definition));
		}
	}

	private void DrawSocket(SpriteBatch spriteBatch, Rectangle area, bool available)
	{
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Color border = Selected ? SoullessUIPalette.Accent
			: IsMouseHovering ? SoullessUIPalette.AccentHoverBorder
			: available ? SoullessUIPalette.Steel : SoullessUIPalette.SteelMuted;
		Color fill = available ? SoullessUIPalette.Surface : SoullessUIPalette.SurfaceDisabled;
		spriteBatch.Draw(pixel, area, border);
		spriteBatch.Draw(pixel, new Rectangle(area.X + 3, area.Y + 3, area.Width - 6, area.Height - 6), fill);
		if (Selected)
		{
			// The inner hairline keeps selection legible behind bright Essence sprites.
			spriteBatch.Draw(pixel, new Rectangle(area.X + 5, area.Y + 5, area.Width - 10, 1),
				SoullessUIPalette.AccentMuted);
		}
	}

	private static void DrawEmptyMark(SpriteBatch spriteBatch, Vector2 center)
	{
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Color color = SoullessUIPalette.SteelMuted * 0.72f;
		spriteBatch.Draw(pixel, new Rectangle((int)center.X - 10, (int)center.Y - 1, 20, 2), color);
		spriteBatch.Draw(pixel, new Rectangle((int)center.X - 1, (int)center.Y - 10, 2, 20), color);
	}

	private static void DrawLockedIcon(SpriteBatch spriteBatch, Vector2 center)
	{
		Main.instance.LoadItem(ItemID.DemonHeart);
		ImbuementSlotDrawing.DrawItem(spriteBatch, ItemID.DemonHeart, center, 34f,
			SoullessUIPalette.TextMuted * 0.82f);
	}

	private static string GetTooltip(bool available, bool occupied, MutationDefinition definition)
	{
		if (!available)
		{
			return Localize("MutationLockedTooltip");
		}
		if (occupied)
		{
			return $"{definition.DisplayName}\n{Localize("MutationOccupiedTooltip")}";
		}
		return Localize("MutationEmptyTooltip");
	}

	private static string Localize(string key) => Language.GetTextValue($"Mods.SoulsOfTerra.UI.{key}");
}

internal sealed class MutationDetailsElement : UIPanel
{
	public int SelectedSlot { get; set; } = -1;

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		base.DrawSelf(spriteBatch);
		CalculatedStyle dimensions = GetDimensions();
		DrawHeader(spriteBatch, dimensions);

		MutationPlayer mutationPlayer = Main.LocalPlayer.GetModPlayer<MutationPlayer>();
		if (SelectedSlot < 0)
		{
			DrawEmptyOverview(spriteBatch, dimensions);
			return;
		}
		if (!mutationPlayer.IsSlotAvailable(SelectedSlot))
		{
			DrawLockedDetails(spriteBatch, dimensions);
			return;
		}

		MutationId id = mutationPlayer.GetMutation(SelectedSlot);
		if (!MutationRegistry.TryGet(id, out MutationDefinition definition))
		{
			DrawEmptySocketDetails(spriteBatch, dimensions);
			return;
		}
		DrawMutationDetails(spriteBatch, dimensions, definition);
	}

	private static void DrawHeader(SpriteBatch spriteBatch, CalculatedStyle dimensions)
	{
		Utils.DrawBorderString(spriteBatch, Localize("MutationDetailsHeader"),
			new Vector2(dimensions.X + 14f, dimensions.Y + 11f), SoullessUIPalette.AccentMuted, 0.62f);
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		spriteBatch.Draw(pixel, new Rectangle((int)dimensions.X + 14, (int)dimensions.Y + 38,
			(int)dimensions.Width - 28, 1), SoullessUIPalette.SteelMuted);
	}

	private static void DrawMutationDetails(SpriteBatch spriteBatch, CalculatedStyle dimensions,
		MutationDefinition definition)
	{
		Vector2 iconCenter = new(dimensions.X + 48f, dimensions.Y + 72f);
		ImbuementSlotDrawing.DrawItem(spriteBatch, definition.EssenceItemType, iconCenter, 48f);
		Utils.DrawBorderString(spriteBatch, definition.DisplayName,
			new Vector2(dimensions.X + 82f, dimensions.Y + 55f), SoullessUIPalette.TextPrimary, 0.78f);

		string[] effects = definition.DetailedDescription.Split('\n', StringSplitOptions.RemoveEmptyEntries);
		float y = dimensions.Y + 108f;
		foreach (string effect in effects)
		{
			Utils.DrawBorderString(spriteBatch, $"• {effect.Trim()}", new Vector2(dimensions.X + 18f, y),
				SoullessUIPalette.AccentText, 0.56f);
			y += 27f;
		}
	}

	private static void DrawEmptyOverview(SpriteBatch spriteBatch, CalculatedStyle dimensions)
	{
		DrawStateText(spriteBatch, dimensions, Localize("MutationNoActiveTitle"),
			Localize("MutationNoActiveBody"), SoullessUIPalette.TextSecondary);
	}

	private static void DrawEmptySocketDetails(SpriteBatch spriteBatch, CalculatedStyle dimensions)
	{
		DrawStateText(spriteBatch, dimensions, Localize("MutationEmptyTitle"),
			Localize("MutationEmptyBody"), SoullessUIPalette.TextSecondary);
	}

	private static void DrawLockedDetails(SpriteBatch spriteBatch, CalculatedStyle dimensions)
	{
		Vector2 iconCenter = new(dimensions.X + 48f, dimensions.Y + 76f);
		ImbuementSlotDrawing.DrawItem(spriteBatch, ItemID.DemonHeart, iconCenter, 42f,
			SoullessUIPalette.TextMuted);
		Utils.DrawBorderString(spriteBatch, Localize("MutationLockedTitle"),
			new Vector2(dimensions.X + 82f, dimensions.Y + 56f), SoullessUIPalette.TextPrimary, 0.76f);
		Utils.DrawBorderString(spriteBatch, Localize("MutationLockedBody"),
			new Vector2(dimensions.X + 82f, dimensions.Y + 86f), SoullessUIPalette.Requirement, 0.56f);
	}

	private static void DrawStateText(SpriteBatch spriteBatch, CalculatedStyle dimensions, string heading,
		string body, Color bodyColor)
	{
		Utils.DrawBorderString(spriteBatch, heading,
			new Vector2(dimensions.Center().X, dimensions.Y + 68f), SoullessUIPalette.TextPrimary, 0.78f, 0.5f);
		Utils.DrawBorderString(spriteBatch, body,
			new Vector2(dimensions.Center().X, dimensions.Y + 102f), bodyColor, 0.57f, 0.5f);
	}

	private static string Localize(string key) => Language.GetTextValue($"Mods.SoulsOfTerra.UI.{key}");
}
