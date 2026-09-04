using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Common;
using SoulsOfTerra.Players;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Microsoft.Xna.Framework.Input;

namespace SoulsOfTerra.Systems;

/// <summary>
/// Terraforge Temper tab: raise a specific weapon along one essence path, transfer that investment,
/// or pay to change the path. Inventory slots are linked rather than items being pulled out.
/// </summary>
internal sealed class WeaponTemperPage : UIElement
{
	private const float ListTop = 38f;
	private const float ListHeight = 235f;
	private const float WorkspaceInset = 8f;
	private const float ColumnGap = 10f;
	private const float ButtonHeight = 32f;
	private const float SocketTop = ListTop + ListHeight + 8f;
	private const float StatusTop = SocketTop + ShopFullLayout.BoxHeight + 8f;
	private const float PreviewTop = StatusTop + 16f;
	private const float ButtonTop = PreviewTop + 18f;
	private static readonly float PageWidth = ShopFullLayout.PanelWidth - ShopFullLayout.InteriorLeft * 2f;
	private static readonly float ColumnWidth = ShopFullLayout.SnapEven(
		(PageWidth - WorkspaceInset * 2f - ColumnGap * 2f) / 3f);

	private readonly List<TemperWeaponRow> rows = new();
	private readonly UIList weaponList = new();
	private readonly UIScrollbar scrollBar;
	private readonly UIText hint = new("Click a weapon. Click the essence socket to cycle.", 0.62f);
	private readonly UIText transferHint = new("Shift-click a second weapon to transfer.", 0.62f);
	private readonly UIText status = new(string.Empty, 0.62f);
	private readonly UIText pathPreview = new(string.Empty, 0.58f);
	private readonly ImbuementWeaponSocket weaponSocket = new();
	private readonly ImbuementEssenceSocket essenceSocket = new();
	private readonly ImbuementWeaponSocket destSocket = new();
	private readonly UIPanel raiseButton;
	private readonly UIPanel transferButton;
	private readonly UIPanel reinfuseButton;
	private readonly UIText raiseLabel = new("Raise", 0.58f);
	private readonly UIText transferLabel = new("Transfer", 0.58f);
	private readonly UIText reinfuseLabel = new("Re-infuse", 0.58f);
	private int weaponSlot = -1;
	private int essenceSlot = -1;
	private int destSlot = -1;
	private Point16 terraforgePosition;
	private Action<string, bool> showFeedback;
	private int inventorySignature;

	public WeaponTemperPage(UIScrollbar forgeScrollBar)
	{
		scrollBar = forgeScrollBar;
		Width.Set(PageWidth, 0f);
		Height.Set(ButtonTop + ButtonHeight + 8f, 0f);

		hint.TextColor = SoullessUIPalette.TextSecondary;
		Append(hint);
		transferHint.Top.Set(16f, 0f);
		transferHint.TextColor = SoullessUIPalette.TextSecondary;
		Append(transferHint);

		UIElement listContainer = new();
		listContainer.Width.Set(0f, 1f);
		listContainer.Height.Set(ListHeight, 0f);
		listContainer.Top.Set(ListTop, 0f);
		Append(listContainer);

		weaponList.Left.Set(3f, 0f);
		weaponList.Width.Set(-28f, 1f);
		weaponList.Height.Set(0f, 1f);
		weaponList.ListPadding = 5f;
		listContainer.Append(weaponList);
		weaponList.SetScrollbar(scrollBar);
		listContainer.Append(scrollBar);

		// Sockets and actions share one 3-column grid so Raise/Transfer/Re-infuse sit under the boxes.
		weaponSocket.Left.Set(SocketLeft(0), 0f);
		weaponSocket.Top.Set(SocketTop, 0f);
		Append(weaponSocket);
		essenceSocket.Left.Set(SocketLeft(1), 0f);
		essenceSocket.Top.Set(SocketTop, 0f);
		Append(essenceSocket);
		destSocket.Left.Set(SocketLeft(2), 0f);
		destSocket.Top.Set(SocketTop, 0f);
		Append(destSocket);

		status.Left.Set(WorkspaceInset, 0f);
		status.Top.Set(StatusTop, 0f);
		status.Width.Set(PageWidth - WorkspaceInset * 2f, 0f);
		status.TextColor = SoullessUIPalette.TextSecondary;
		Append(status);

		pathPreview.Left.Set(WorkspaceInset, 0f);
		pathPreview.Top.Set(PreviewTop, 0f);
		pathPreview.Width.Set(PageWidth - WorkspaceInset * 2f, 0f);
		pathPreview.TextColor = SoullessUIPalette.AccentMuted;
		Append(pathPreview);

		raiseButton = CreateActionButton(raiseLabel, 0, (_, _) => TryRaise());
		transferButton = CreateActionButton(transferLabel, 1, (_, _) => TryTransfer());
		reinfuseButton = CreateActionButton(reinfuseLabel, 2, (_, _) => TryReinfuse());
		essenceSocket.OnLeftClick += (_, _) => CycleEssence();
		destSocket.OnLeftClick += (_, _) => destSlot = -1;
	}

	public void Open(Point16 position, Action<string, bool> feedback)
	{
		terraforgePosition = position;
		showFeedback = feedback;
		weaponSlot = -1;
		essenceSlot = -1;
		destSlot = -1;
		scrollBar.ViewPosition = 0f;
		inventorySignature = 0;
		RebuildRows(force: true);
	}

	public void Refresh()
	{
		if (!InventorySlotAlive(weaponSlot) || !WeaponTemper.CanTemper(Main.LocalPlayer.inventory[weaponSlot]))
		{
			weaponSlot = -1;
		}
		if (!InventorySlotAlive(essenceSlot))
		{
			essenceSlot = -1;
		}
		if (!InventorySlotAlive(destSlot) || destSlot == weaponSlot)
		{
			destSlot = -1;
		}

		RebuildRows(force: false);
		if (essenceSlot < 0)
		{
			AutoLinkEssence();
		}
		RefreshSockets();
		RefreshStatus();
		RefreshButtons();
	}

	private void RebuildRows(bool force)
	{
		Player player = Main.LocalPlayer;
		int signature = 17;
		List<int> slots = new();
		for (int slot = 0; slot < 50; slot++)
		{
			Item item = player.inventory[slot];
			if (!WeaponTemper.CanTemper(item))
			{
				continue;
			}

			slots.Add(slot);
			WeaponTemperItem temper = WeaponTemperItem.Get(item);
			signature = unchecked(signature * 31 + item.type + item.stack + (temper?.Level ?? 0) * 13
				+ (temper?.PathIndex ?? -1));
		}

		if (!force && signature == inventorySignature && rows.Count == slots.Count)
		{
			for (int index = 0; index < rows.Count; index++)
			{
				int slot = slots[index];
				rows[index].SetContent(player.inventory[slot], slot == weaponSlot, slot == destSlot);
			}
			return;
		}

		inventorySignature = signature;
		weaponList.Clear();
		rows.Clear();
		for (int index = 0; index < slots.Count; index++)
		{
			int captured = slots[index];
			TemperWeaponRow row = new();
			row.SetAction(() => SelectWeapon(captured));
			row.SetContent(player.inventory[captured], captured == weaponSlot, captured == destSlot);
			weaponList.Add(row);
			rows.Add(row);
		}
	}

	private void SelectWeapon(int slot)
	{
		if (!InventorySlotAlive(slot) || !WeaponTemper.CanTemper(Main.LocalPlayer.inventory[slot]))
		{
			return;
		}

		if ((Main.keyState.IsKeyDown(Keys.LeftShift) || Main.keyState.IsKeyDown(Keys.RightShift))
			&& weaponSlot >= 0 && slot != weaponSlot)
		{
			destSlot = slot;
			return;
		}

		weaponSlot = slot;
		if (destSlot == weaponSlot)
		{
			destSlot = -1;
		}
		essenceSlot = -1;
		AutoLinkEssence();
	}

	private void CycleEssence()
	{
		List<int> owned = new();
		for (int slot = 0; slot < Main.LocalPlayer.inventory.Length; slot++)
		{
			Item item = Main.LocalPlayer.inventory[slot];
			int path = EssencePathRegistry.IndexOfEssence(item.type);
			if (path >= 0 && item.stack > 0 && SoulEssenceRegistry.TryGet(path, out SoulEssenceDefinition definition)
				&& definition.IsUnlocked() && !owned.Exists(candidate => Main.LocalPlayer.inventory[candidate].type == item.type))
			{
				owned.Add(slot);
			}
		}

		if (owned.Count == 0)
		{
			essenceSlot = -1;
			return;
		}

		int current = owned.FindIndex(slot => slot == essenceSlot);
		essenceSlot = owned[(current + 1) % owned.Count];
	}

	private void AutoLinkEssence()
	{
		if (weaponSlot < 0)
		{
			return;
		}

		Item weapon = Main.LocalPlayer.inventory[weaponSlot];
		WeaponTemperItem temper = WeaponTemperItem.Get(weapon);
		int requiredType = temper is { IsTempered: true }
			? EssencePathRegistry.EssenceItemType(temper.PathIndex)
			: ItemID.None;
		if (requiredType > ItemID.None)
		{
			essenceSlot = FindItemSlot(requiredType);
			return;
		}

		if (essenceSlot >= 0)
		{
			return;
		}

		// First temper: use the first unlocked essence the player is carrying.
		for (int slot = 0; slot < Main.LocalPlayer.inventory.Length; slot++)
		{
			Item item = Main.LocalPlayer.inventory[slot];
			int path = EssencePathRegistry.IndexOfEssence(item.type);
			if (path >= 0 && item.stack > 0 && SoulEssenceRegistry.TryGet(path, out SoulEssenceDefinition definition)
				&& definition.IsUnlocked())
			{
				essenceSlot = slot;
				return;
			}
		}
	}

	private void RefreshSockets()
	{
		Item weapon = InventorySlotAlive(weaponSlot) ? Main.LocalPlayer.inventory[weaponSlot] : null;
		Item essence = InventorySlotAlive(essenceSlot) ? Main.LocalPlayer.inventory[essenceSlot] : null;
		Item dest = InventorySlotAlive(destSlot) ? Main.LocalPlayer.inventory[destSlot] : null;
		weaponSocket.SetItem(weapon?.type ?? ItemID.None, weapon is not null, weapon);
		essenceSocket.SetItem(essence?.type ?? ItemID.None);
		destSocket.SetItem(dest?.type ?? ItemID.None, dest is not null, dest);
	}

	private void RefreshStatus()
	{
		if (weaponSlot < 0)
		{
			status.SetText("Click a weapon in the list. Shift-click another to mark a transfer.");
			pathPreview.SetText(string.Empty);
			return;
		}

		Item weapon = Main.LocalPlayer.inventory[weaponSlot];
		WeaponTemperItem temper = WeaponTemperItem.Get(weapon);
		int level = temper?.Level ?? 0;
		int ceiling = WeaponTemper.LevelCeiling();
		int currentPath = temper?.PathIndex ?? -1;
		EssencePathRegistry.TryGet(currentPath, out EssencePathDefinition current);
		int currentDamage = weapon.damage;
		bool raising = CanRaise(out _, out _);
		int nextDamage = WeaponTemper.GetTemperedDamage(weapon, Math.Min(level + 1, WeaponTemper.MaxLevel));
		string pathLine = current is null ? "No path yet" : $"{current.EffectName} +{level}";
		string damageLine = raising ? $"{currentDamage} → {nextDamage} dmg" : $"{currentDamage} dmg";
		status.SetText($"{pathLine}  •  Ceiling +{ceiling}  •  {damageLine}");
		pathPreview.SetText(BuildPathPreview(current, level, currentPath));
	}

	private string BuildPathPreview(EssencePathDefinition current, int level, int currentPath)
	{
		if (!TryGetSelectedEssence(out _, out int incomingPath, out _)
			|| !EssencePathRegistry.TryGet(incomingPath, out EssencePathDefinition next))
		{
			pathPreview.TextColor = SoullessUIPalette.TextMuted;
			return current is null
				? "Click the essence socket to cycle."
				: current.DescribeCompact(level);
		}

		if (current is null)
		{
			pathPreview.TextColor = SoullessUIPalette.AccentText;
			return $"Will bind {next.DescribeCompact(1)}";
		}

		if (incomingPath != currentPath)
		{
			pathPreview.TextColor = SoullessUIPalette.AccentText;
			return $"{current.DescribeCompact(level)}  →  {next.DescribeCompact(level)}";
		}

		if (level < WeaponTemper.LevelCeiling() && level < WeaponTemper.MaxLevel)
		{
			pathPreview.TextColor = SoullessUIPalette.AccentMuted;
			return $"{current.DescribeEffect(level, verbose: false)}  →  {current.DescribeEffect(level + 1, verbose: false)}";
		}

		pathPreview.TextColor = SoullessUIPalette.TextSecondary;
		return current.DescribeCompact(level);
	}

	private void RefreshButtons()
	{
		bool canRaise = CanRaise(out _, out long raiseCost);
		bool canTransfer = CanTransfer(out _, out long transferCost);
		bool canReinfuse = CanReinfuse(out _, out long reinfuseCost);
		StyleButton(raiseButton, raiseLabel, canRaise, canRaise ? $"Raise ({raiseCost:N0})" : "Raise");
		StyleButton(transferButton, transferLabel, canTransfer, canTransfer ? $"Transfer ({transferCost:N0})" : "Transfer");
		StyleButton(reinfuseButton, reinfuseLabel, canReinfuse, canReinfuse ? $"Re-infuse ({reinfuseCost:N0})" : "Re-infuse");
	}

	private void TryRaise()
	{
		if (!CanRaise(out string failure, out long cost))
		{
			showFeedback?.Invoke(failure, false);
			return;
		}

		bool completed = Send(SoulMessageType.RequestWeaponTemper, weaponSlot, essenceSlot,
			() => SoulTransactions.TryTemperWeapon(Main.LocalPlayer, terraforgePosition, weaponSlot, essenceSlot));
		showFeedback?.Invoke(completed ? $"Raised to +{WeaponTemperItem.LevelOf(Main.LocalPlayer.inventory[weaponSlot])}."
			: "Temper request sent.", true);
		_ = cost;
	}

	private void TryTransfer()
	{
		if (!CanTransfer(out string failure, out _))
		{
			showFeedback?.Invoke(failure, false);
			return;
		}

		bool completed = Send(SoulMessageType.RequestWeaponTemperTransfer, weaponSlot, destSlot,
			() => SoulTransactions.TryTransferWeaponTemper(Main.LocalPlayer, terraforgePosition, weaponSlot, destSlot));
		if (completed)
		{
			weaponSlot = destSlot;
			destSlot = -1;
		}
		showFeedback?.Invoke(completed ? "Temper moved." : "Transfer request sent.", true);
	}

	private void TryReinfuse()
	{
		if (!CanReinfuse(out string failure, out _))
		{
			showFeedback?.Invoke(failure, false);
			return;
		}

		bool completed = Send(SoulMessageType.RequestWeaponTemperReinfuse, weaponSlot, essenceSlot,
			() => SoulTransactions.TryReinfuseWeapon(Main.LocalPlayer, terraforgePosition, weaponSlot, essenceSlot));
		showFeedback?.Invoke(completed ? "The weapon drinks a new essence." : "Re-infuse request sent.", true);
	}

	private bool CanRaise(out string failure, out long cost)
	{
		failure = string.Empty;
		cost = 0;
		if (WeaponTemper.LevelCeiling() <= 0)
		{
			failure = "Temper the fragment before a weapon can be raised.";
			return false;
		}
		if (!TryGetSelectedWeapon(out Item weapon, out WeaponTemperItem temper))
		{
			failure = "Select a weapon.";
			return false;
		}
		if (temper.Level >= WeaponTemper.LevelCeiling())
		{
			failure = "The fragment cannot pull this weapon any further yet.";
			return false;
		}
		if (!TryGetSelectedEssence(out Item essence, out int pathIndex, out SoulEssenceDefinition definition))
		{
			failure = "Carry the essence this path requires.";
			return false;
		}
		if (temper.Level > 0 && temper.PathIndex != pathIndex)
		{
			failure = "This weapon already follows another path. Re-infuse to change it.";
			return false;
		}
		if (!definition.IsUnlocked())
		{
			failure = "That essence is still locked.";
			return false;
		}
		cost = WeaponTemper.GetLevelCost(temper.Level + 1);
		if (Main.LocalPlayer.GetModPlayer<SoulPlayer>().SoulBalance < cost)
		{
			failure = $"You need {cost:N0} souls.";
			return false;
		}
		_ = weapon;
		_ = essence;
		return true;
	}

	private bool CanTransfer(out string failure, out long cost)
	{
		failure = string.Empty;
		cost = 0;
		if (!TryGetSelectedWeapon(out _, out WeaponTemperItem source) || !source.IsTempered)
		{
			failure = "Select a tempered weapon to transfer from.";
			return false;
		}
		int transferred = WeaponTemper.GetTransferredLevel(source.Level);
		if (transferred <= 0)
		{
			failure = $"Transfer loses {WeaponTemper.TransferLevelLoss} levels; raise the source further first.";
			return false;
		}
		if (!InventorySlotAlive(destSlot) || destSlot == weaponSlot
			|| !WeaponTemper.CanTemper(Main.LocalPlayer.inventory[destSlot]))
		{
			failure = "Click a second weapon to mark the transfer target.";
			return false;
		}
		if (WeaponTemperItem.Get(Main.LocalPlayer.inventory[destSlot]) is { IsTempered: true })
		{
			failure = "The destination already carries a temper.";
			return false;
		}
		cost = WeaponTemper.GetTransferCost(source.Level);
		if (Main.LocalPlayer.GetModPlayer<SoulPlayer>().SoulBalance < cost)
		{
			failure = $"You need {cost:N0} souls.";
			return false;
		}
		return true;
	}

	private bool CanReinfuse(out string failure, out long cost)
	{
		failure = string.Empty;
		cost = 0;
		if (!TryGetSelectedWeapon(out _, out WeaponTemperItem temper) || !temper.IsTempered)
		{
			failure = "Select a tempered weapon to re-infuse.";
			return false;
		}
		if (!TryGetSelectedEssence(out _, out int pathIndex, out SoulEssenceDefinition definition))
		{
			failure = "Carry a different unlocked essence.";
			return false;
		}
		if (pathIndex == temper.PathIndex)
		{
			failure = "Pick an essence other than the current path.";
			return false;
		}
		if (!definition.IsUnlocked())
		{
			failure = "That essence is still locked.";
			return false;
		}
		cost = WeaponTemper.GetReinfuseCost(temper.Level);
		if (Main.LocalPlayer.GetModPlayer<SoulPlayer>().SoulBalance < cost)
		{
			failure = $"You need {cost:N0} souls.";
			return false;
		}
		return true;
	}

	private bool TryGetSelectedWeapon(out Item weapon, out WeaponTemperItem temper)
	{
		weapon = InventorySlotAlive(weaponSlot) ? Main.LocalPlayer.inventory[weaponSlot] : null;
		temper = WeaponTemperItem.Get(weapon);
		return temper is not null;
	}

	private bool TryGetSelectedEssence(out Item essence, out int pathIndex, out SoulEssenceDefinition definition)
	{
		essence = InventorySlotAlive(essenceSlot) ? Main.LocalPlayer.inventory[essenceSlot] : null;
		pathIndex = essence is not null ? EssencePathRegistry.IndexOfEssence(essence.type) : -1;
		definition = null;
		return essence is { stack: > 0 } && SoulEssenceRegistry.TryGet(pathIndex, out definition);
	}

	private bool Send(SoulMessageType messageType, int firstSlot, int secondSlot, Func<bool> singlePlayer)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			ModPacket packet = ModContent.GetInstance<SoulsOfTerra>().GetPacket();
			packet.Write((byte)messageType);
			packet.Write((byte)firstSlot);
			packet.Write((byte)secondSlot);
			packet.Write(terraforgePosition.X);
			packet.Write(terraforgePosition.Y);
			packet.Send();
			return false;
		}

		return singlePlayer();
	}

	private static float ColumnLeft(int column) =>
		WorkspaceInset + column * (ColumnWidth + ColumnGap);

	private static float SocketLeft(int column) =>
		ColumnLeft(column) + ShopFullLayout.SnapEven((ColumnWidth - ShopFullLayout.BoxWidth) * 0.5f);

	private UIPanel CreateActionButton(UIText label, int column, UIElement.MouseEvent click)
	{
		UIPanel button = new();
		button.Width.Set(ColumnWidth, 0f);
		button.Height.Set(ButtonHeight, 0f);
		button.MaxWidth.Set(ColumnWidth, 0f);
		button.MaxHeight.Set(ButtonHeight, 0f);
		button.Left.Set(ColumnLeft(column), 0f);
		button.Top.Set(ButtonTop, 0f);
		button.SetPadding(0f);
		button.OverflowHidden = true;
		label.HAlign = 0.5f;
		label.VAlign = 0.5f;
		button.Append(label);
		button.OnLeftClick += click;
		Append(button);
		return button;
	}

	private static void StyleButton(UIPanel button, UIText label, bool enabled, string text)
	{
		label.SetText(text);
		label.TextColor = enabled ? SoullessUIPalette.AccentText : SoullessUIPalette.TextMuted;
		button.BackgroundColor = enabled ? SoullessUIPalette.AccentSurface : SoullessUIPalette.SurfaceDisabled;
		button.BorderColor = enabled ? SoullessUIPalette.Accent : SoullessUIPalette.SteelMuted;
	}

	private static bool InventorySlotAlive(int slot) =>
		slot >= 0 && slot < Main.LocalPlayer.inventory.Length && Main.LocalPlayer.inventory[slot].stack > 0;

	private static int FindItemSlot(int itemType)
	{
		for (int slot = 0; slot < Main.LocalPlayer.inventory.Length; slot++)
		{
			Item item = Main.LocalPlayer.inventory[slot];
			if (item.type == itemType && item.stack > 0)
			{
				return slot;
			}
		}
		return -1;
	}
}

internal sealed class TemperWeaponRow : UIElement
{
	private readonly ImbuementRecipeItemSlot slot = new();
	private readonly UIText name = new(string.Empty, 0.62f);
	private readonly UIText detail = new(string.Empty, 0.52f);
	private Action action;
	private bool selected;
	private bool destination;

	public TemperWeaponRow()
	{
		Width.Set(0f, 1f);
		Height.Set(ImbuementRecipeItemSlot.SlotHeight, 0f);

		slot.Left.Set(0f, 0f);
		slot.VAlign = 0.5f;
		Append(slot);

		float textLeft = ImbuementRecipeItemSlot.SlotWidth + 10f;
		name.Left.Set(textLeft, 0f);
		name.Top.Set(10f, 0f);
		Append(name);
		detail.Left.Set(textLeft, 0f);
		detail.Top.Set(30f, 0f);
		detail.TextColor = SoullessUIPalette.TextSecondary;
		Append(detail);
		OnLeftClick += (_, _) => action?.Invoke();
	}

	public void SetAction(Action next) => action = next;

	public void SetContent(Item item, bool isSelected, bool isDestination)
	{
		selected = isSelected;
		destination = isDestination;
		slot.SetItem(item.type, item.Name, item);
		slot.ReadyAnimation = isSelected;
		WeaponTemperItem temper = WeaponTemperItem.Get(item);
		string pathName = EssencePathRegistry.PathName(temper?.PathIndex ?? -1);
		name.SetText(item.Name);
		name.TextColor = isSelected ? SoullessUIPalette.AccentText : SoullessUIPalette.TextPrimary;
		if (temper is { IsTempered: true })
		{
			detail.SetText(string.IsNullOrEmpty(pathName) ? $"+{temper.Level}" : $"{pathName} +{temper.Level}");
			detail.TextColor = SoullessUIPalette.AccentMuted;
		}
		else
		{
			detail.SetText(isDestination ? "Transfer target" : "Untempered");
			detail.TextColor = isDestination ? SoullessUIPalette.AccentText : SoullessUIPalette.TextSecondary;
		}
	}
}
