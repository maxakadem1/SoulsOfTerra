using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SoulsOfTerra.Common;
using SoulsOfTerra.Content.Tiles;
using SoulsOfTerra.Players;
using Terraria;
using Terraria.Audio;
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

	public static void Close()
	{
		MutationUIFrameRenderer.ResetInteraction();
		graftingInterface?.SetState(null);
	}

	public override void UpdateUI(GameTime gameTime)
	{
		if (graftingInterface?.CurrentState is not null)
		{
			graftingInterface.Update(gameTime);
			MutationUIFrameRenderer.Update(gameTime);
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

	private MutationPanelElement panel;
	private UIText title;
	private UIText subtitle;
	private UIText insertInstruction;
	private UIText removeInstruction;
	private MutationCloseElement closeButton;
	private readonly MutationSocketElement[] sockets = new MutationSocketElement[MutationPlayer.SlotCount];
	private readonly MutationId[] previousMutations = new MutationId[MutationPlayer.SlotCount];
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
		SnapshotMutations();
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
		DetectMutationChanges();
		RefreshSelection();
		if (panel.ContainsPoint(Main.MouseScreen))
		{
			player.mouseInterface = true;
		}
	}

	private void CreatePanel()
	{
		panel = new MutationPanelElement();

		title = new UIText(Localize("MutationTitle"), 1.05f);
		title.Left.Set(28f, 0f);
		title.Top.Set(14f, 0f);
		panel.Append(title);

		subtitle = new UIText(Localize("MutationSubtitle"), 0.68f);
		subtitle.Left.Set(23f, 0f);
		subtitle.Top.Set(44f, 0f);
		subtitle.TextColor = SoullessUIPalette.TextSecondary;
		panel.Append(subtitle);

		closeButton = new MutationCloseElement();
		closeButton.Width.Set(34f, 0f);
		closeButton.Height.Set(30f, 0f);
		closeButton.Top.Set(12f, 0f);
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
			sockets[slot].OnMouseOver += (_, _) => HandleSocketHover(capturedSlot, true);
			sockets[slot].OnMouseOut += (_, _) => HandleSocketHover(capturedSlot, false);
			panel.Append(sockets[slot]);
		}
	}

	private void CreateDetails()
	{
		details = new MutationDetailsElement();
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
		float panelLeft = SnapEven(Math.Min(InventoryRight, virtualWidth - panelWidth - 12f));
		float panelTop = SnapEven(Math.Max(38f, (virtualHeight - panelHeight) * 0.5f));
		panel.Left.Set(panelLeft, 0f);
		panel.Top.Set(panelTop, 0f);
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

		Vector2[] centers = new Vector2[sockets.Length];
		for (int slot = 0; slot < sockets.Length; slot++)
		{
			centers[slot] = new Vector2(socketsLeft + slot * (socketWidth + socketGap) + socketWidth * 0.5f,
				128f + 37f);
		}
		MutationUIFrameRenderer.Configure(altarPosition, panelWidth, panelHeight, centers,
			new Vector2(panelWidth - 35f, 27f));
	}

	private void HandleSocketLeftClick(int slot)
	{
		MutationPlayer mutationPlayer = Main.LocalPlayer.GetModPlayer<MutationPlayer>();
		if (!mutationPlayer.IsSlotAvailable(slot))
		{
			SelectSlot(slot);
			RefreshSelection();
			return;
		}

		Item held = Main.mouseItem;
		if (held.IsAir)
		{
			SelectSlot(slot);
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

		SelectSlot(slot);
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
		closeButton.Hovered = hovered;
		MutationUIFrameRenderer.SetCloseHovered(hovered);
		if (hovered)
		{
			PlayTick(0.38f);
		}
	}

	private void HandleSocketHover(int slot, bool hovered)
	{
		MutationUIFrameRenderer.SetHoveredSlot(hovered ? slot : -1);
		if (hovered)
		{
			PlayTick(0.2f);
		}
	}

	private void SelectSlot(int slot)
	{
		if (selectedSlot == slot)
		{
			return;
		}

		selectedSlot = slot;
		MutationUIFrameRenderer.TriggerSelection(slot);
		PlayTick(0.44f);
	}

	private void SnapshotMutations()
	{
		MutationPlayer mutationPlayer = Main.LocalPlayer.GetModPlayer<MutationPlayer>();
		for (int slot = 0; slot < previousMutations.Length; slot++)
		{
			previousMutations[slot] = mutationPlayer.GetMutation(slot);
		}
	}

	private void DetectMutationChanges()
	{
		MutationPlayer mutationPlayer = Main.LocalPlayer.GetModPlayer<MutationPlayer>();
		for (int slot = 0; slot < previousMutations.Length; slot++)
		{
			MutationId current = mutationPlayer.GetMutation(slot);
			MutationId previous = previousMutations[slot];
			if (current == previous)
			{
				continue;
			}

			if (previous == MutationId.None && current != MutationId.None)
			{
				MutationUIFrameRenderer.TriggerInsertion(slot);
				SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.32f, Pitch = 0.22f });
			}
			else if (previous != MutationId.None && current == MutationId.None)
			{
				MutationUIFrameRenderer.TriggerRemoval(slot);
				SoundEngine.PlaySound(SoundID.Shatter with { Volume = 0.26f, Pitch = 0.18f });
			}
			previousMutations[slot] = current;
		}
	}

	private static void PlayTick(float pitch)
	{
		SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.2f, Pitch = pitch });
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

	private static float SnapEven(float value) => MathF.Round(value * 0.5f) * 2f;
}

internal sealed class MutationPanelElement : UIElement
{
	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		CalculatedStyle dimensions = GetDimensions();
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Rectangle panelArea = dimensions.ToRectangle();
		// A quiet drop shadow separates the borderless panel from busy world backgrounds.
		spriteBatch.Draw(pixel, new Rectangle(panelArea.X + 5, panelArea.Y + 7, panelArea.Width, panelArea.Height),
			Color.Black * 0.32f);
		spriteBatch.Draw(pixel, panelArea, SoullessUIPalette.Panel);
		MutationUIFrameRenderer.Draw(spriteBatch, new Vector2(dimensions.X, dimensions.Y));
	}
}

internal sealed class MutationCloseElement : UIElement
{
	public bool Hovered { get; set; }

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		CalculatedStyle dimensions = GetDimensions();
		Color color = Hovered ? SoullessUIPalette.AccentText : SoullessUIPalette.TextSecondary;
		Utils.DrawBorderString(spriteBatch, "×", dimensions.Center(), color, 0.82f, 0.5f, 0.5f);
	}
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
		DrawSocketGround(spriteBatch, socket, available);

		string label;
		if (!available)
		{
			DrawLockedIcon(spriteBatch, socket.Center.ToVector2());
			label = Localize("MutationLockedName");
		}
		else if (occupied)
		{
			if (Selected || IsMouseHovering)
			{
				DrawItemGlow(spriteBatch, definition.EssenceItemType, socket.Center.ToVector2());
			}
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

	private void DrawSocketGround(SpriteBatch spriteBatch, Rectangle area, bool available)
	{
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Vector2 center = area.Center.ToVector2();
		// Layered inset shadows suggest a drop area without enclosing it in a box.
		spriteBatch.Draw(pixel, new Rectangle((int)center.X - 29, area.Y + 10, 58, 48),
			Color.Black * (available ? 0.16f : 0.24f));
		spriteBatch.Draw(pixel, new Rectangle((int)center.X - 25, area.Y + 14, 50, 40),
			SoullessUIPalette.SurfaceInset * (available ? 0.38f : 0.24f));

		int lineY = area.Bottom - 4;
		Color lineColor = available ? SoullessUIPalette.SteelMuted : SoullessUIPalette.SteelLow;
		spriteBatch.Draw(pixel, new Rectangle((int)center.X - 25, lineY, 50, 2), lineColor * 0.72f);
		if (Selected)
		{
			spriteBatch.Draw(pixel, new Rectangle((int)center.X - 32, lineY - 2, 64, 6),
				SoullessUIPalette.Accent * 0.14f);
			spriteBatch.Draw(pixel, new Rectangle((int)center.X - 26, lineY, 52, 2),
				SoullessUIPalette.Accent);
		}
		else if (IsMouseHovering && available)
		{
			spriteBatch.Draw(pixel, new Rectangle((int)center.X - 25, lineY, 50, 2),
				SoullessUIPalette.AccentMuted);
		}
	}

	private static void DrawItemGlow(SpriteBatch spriteBatch, int itemType, Vector2 center)
	{
		for (int direction = 0; direction < 4; direction++)
		{
			Vector2 offset = (MathHelper.PiOver2 * direction).ToRotationVector2() * 2f;
			ImbuementSlotDrawing.DrawItem(spriteBatch, itemType, center + offset, 50f,
				SoullessUIPalette.Accent * 0.16f);
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

internal sealed class MutationDetailsElement : UIElement
{
	public int SelectedSlot { get; set; } = -1;

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		CalculatedStyle dimensions = GetDimensions();
		DrawEffectWash(spriteBatch, dimensions);
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
			92, 2), SoullessUIPalette.AccentBorder * 0.7f);
	}

	private static void DrawEffectWash(SpriteBatch spriteBatch, CalculatedStyle dimensions)
	{
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		// Stepped transparency becomes a soft pixel gradient without enclosing the content.
		for (int band = 0; band < 7; band++)
		{
			int inset = band * 6;
			int width = Math.Max(0, (int)dimensions.Width - inset * 2);
			if (width <= 0)
			{
				break;
			}
			spriteBatch.Draw(pixel, new Rectangle((int)dimensions.X + inset,
				(int)dimensions.Y + 43 + band * 5, width, 9),
				SoullessUIPalette.AccentSurface * (0.055f - band * 0.006f));
		}
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
