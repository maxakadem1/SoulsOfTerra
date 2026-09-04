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

	internal static UserInterface Interface => graftingInterface;

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
				graftingState?.DrawShopFull();
				return true;
			}, InterfaceScaleType.UI));
	}
}

internal sealed class GraftingAltarState : UIState
{
	private const float InteractionRangeSquared = 12f * 16f * 12f * 16f;
	private const int CursorInventorySlot = 58;

	private const int PanelWidth = ShopFullLayout.PanelWidth;
	private const int PanelHeight = ShopFullLayout.PanelHeight;
	private const int InteriorLeft = ShopFullLayout.InteriorLeft;
	private const int InteriorBottomInset = ShopFullLayout.InteriorBottomInset;
	private const int BoxWidth = ShopFullLayout.BoxWidth;
	private const int BoxHeight = ShopFullLayout.BoxHeight;
	private const int BoxGap = 20;
	private const int SocketLabelHeight = 26;
	private const int SocketsTop = 158;
	private const int DetailsTop = 262;

	private MutationPanelElement panel;
	private UIText title;
	private UIText subtitle;
	private UIText insertInstruction;
	private UIText removeInstruction;
	private ShopFullCloseElement closeButton;
	private readonly MutationSocketElement[] sockets = new MutationSocketElement[MutationPlayer.SlotCount];
	private readonly MutationId[] previousMutations = new MutationId[MutationPlayer.SlotCount];
	private MutationDetailsElement details;
	private Point16 altarPosition;
	private int selectedSlot = -1;
	private float currentPanelLeft;
	private float currentPanelTop;

	public override void OnInitialize()
	{
		CreatePanel();
		CreateSockets();
		CreateDetails();
		Append(panel);
	}

	public override void OnActivate()
	{
		ApplyLayout(force: true);
	}

	public void Open(Point16 topLeft)
	{
		altarPosition = topLeft;
		RefreshLocalizedText();
		SelectFirstOccupied();
		SnapshotMutations();
		ApplyLayout(force: true);
		RefreshSelection();
	}

	internal void DrawShopFull()
	{
		if (!GraftingAltarSystem.IsOpen)
		{
			return;
		}

		ShopFullLayout.Draw(GraftingAltarSystem.Interface, this, panel, ref currentPanelLeft, ref currentPanelTop);
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
		ShopFullLayout.PlaceTitle(title);
		panel.Append(title);

		subtitle = new UIText(Localize("MutationSubtitle"), 0.68f);
		ShopFullLayout.PlaceSubtitle(subtitle);
		subtitle.TextColor = SoullessUIPalette.TextSecondary;
		panel.Append(subtitle);

		closeButton = new ShopFullCloseElement();
		ShopFullLayout.PlaceClose(closeButton);
		closeButton.OnMouseOver += (_, _) => SetCloseButtonHover(true);
		closeButton.OnMouseOut += (_, _) => SetCloseButtonHover(false);
		closeButton.OnLeftClick += (_, _) => GraftingAltarSystem.Close();
		panel.Append(closeButton);

		insertInstruction = new UIText(Localize("MutationInsertInstruction"), 0.59f);
		insertInstruction.Left.Set(InteriorLeft, 0f);
		insertInstruction.Top.Set(108f, 0f);
		insertInstruction.TextColor = SoullessUIPalette.TextSecondary;
		panel.Append(insertInstruction);

		removeInstruction = new UIText(Localize("MutationRemoveInstruction"), 0.59f);
		removeInstruction.Left.Set(InteriorLeft, 0f);
		removeInstruction.Top.Set(128f, 0f);
		removeInstruction.TextColor = SoullessUIPalette.WarningText;
		panel.Append(removeInstruction);
	}

	private void CreateSockets()
	{
		float groupWidth = sockets.Length * BoxWidth + (sockets.Length - 1) * BoxGap;
		float socketsLeft = ShopFullLayout.SnapEven((PanelWidth - groupWidth) * 0.5f);
		for (int slot = 0; slot < sockets.Length; slot++)
		{
			int capturedSlot = slot;
			sockets[slot] = new MutationSocketElement(slot);
			sockets[slot].Left.Set(socketsLeft + slot * (BoxWidth + BoxGap), 0f);
			sockets[slot].Top.Set(SocketsTop, 0f);
			sockets[slot].Width.Set(BoxWidth, 0f);
			sockets[slot].Height.Set(BoxHeight + SocketLabelHeight, 0f);
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
		details.Left.Set(InteriorLeft, 0f);
		details.Top.Set(DetailsTop, 0f);
		details.Width.Set(PanelWidth - InteriorLeft * 2f, 0f);
		details.Height.Set(PanelHeight - DetailsTop - InteriorBottomInset, 0f);
		panel.Append(details);
	}

	private void ApplyLayout(bool force = false)
	{
		if (!ShopFullLayout.TryPlaceBesideInventory(panel, ref currentPanelLeft, ref currentPanelTop, force))
		{
			return;
		}

		panel.SoulEffectSeed = unchecked(altarPosition.X * 73856093 ^ altarPosition.Y * 19349663);
		ShopFullLayout.Recalculate(this, GraftingAltarSystem.Interface);

		float groupWidth = sockets.Length * BoxWidth + (sockets.Length - 1) * BoxGap;
		float socketsLeft = ShopFullLayout.SnapEven((PanelWidth - groupWidth) * 0.5f);
		Vector2[] centers = new Vector2[sockets.Length];
		for (int slot = 0; slot < sockets.Length; slot++)
		{
			centers[slot] = new Vector2(socketsLeft + slot * (BoxWidth + BoxGap) + BoxWidth * 0.5f,
				SocketsTop + BoxHeight * 0.5f);
		}
		MutationUIFrameRenderer.Configure(altarPosition, PanelWidth, PanelHeight, centers,
			new Vector2(PanelWidth - 47f, 67f));
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
		MutationUIFrameRenderer.SetSelectedSlot(selectedSlot);
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
}

internal sealed class MutationPanelElement : ShopFullPanel
{
	public override void Draw(SpriteBatch spriteBatch)
	{
		base.Draw(spriteBatch);
		CalculatedStyle dimensions = GetDimensions();
		// Boxes are child elements, so socket orbits must be composited after them.
		MutationUIFrameRenderer.DrawInteraction(spriteBatch, new Vector2(dimensions.X, dimensions.Y));
	}
}

internal sealed class MutationSocketElement : UIElement
{
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
		Rectangle box = new((int)MathF.Round(dimensions.X), (int)MathF.Round(dimensions.Y),
			ShopFullLayout.BoxWidth, ShopFullLayout.BoxHeight);
		DrawBox(spriteBatch, box, available);
		Vector2 itemCenter = new(box.X + box.Width * 0.5f, box.Y + box.Height * 0.5f);

		string label;
		if (!available)
		{
			DrawLockedIcon(spriteBatch, itemCenter);
			label = Localize("MutationLockedName");
		}
		else if (occupied)
		{
			if (Selected || IsMouseHovering)
			{
				DrawItemGlow(spriteBatch, definition.EssenceItemType, itemCenter);
			}
			ImbuementSlotDrawing.DrawItem(spriteBatch, definition.EssenceItemType, itemCenter, 40f);
			label = definition.DisplayName;
		}
		else
		{
			DrawEmptyMark(spriteBatch, itemCenter);
			label = Localize("MutationEmptyName");
		}

		Color labelColor = Selected ? SoullessUIPalette.AccentText
			: available ? SoullessUIPalette.TextSecondary : SoullessUIPalette.TextMuted;
		Utils.DrawBorderString(spriteBatch, label,
			new Vector2(dimensions.Center().X, box.Bottom + 6f), labelColor, 0.58f, 0.5f);

		if (IsMouseHovering)
		{
			Main.instance.MouseText(GetTooltip(available, occupied, definition));
		}
	}

	private void DrawBox(SpriteBatch spriteBatch, Rectangle destination, bool available)
	{
		Color color = !available ? Color.White * 0.55f
			: Selected ? Color.Lerp(Color.White, SoullessUIPalette.Accent, 0.28f)
			: IsMouseHovering ? Color.Lerp(Color.White, SoullessUIPalette.Accent, 0.14f)
			: Color.White;
		ShopFullArt.DrawBox(spriteBatch, destination, color);
	}

	private static void DrawItemGlow(SpriteBatch spriteBatch, int itemType, Vector2 center)
	{
		for (int direction = 0; direction < 4; direction++)
		{
			Vector2 offset = (MathHelper.PiOver2 * direction).ToRotationVector2() * 2f;
			ImbuementSlotDrawing.DrawItem(spriteBatch, itemType, center + offset, 40f,
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
		ImbuementSlotDrawing.DrawItem(spriteBatch, ItemID.DemonHeart, center, 28f,
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
