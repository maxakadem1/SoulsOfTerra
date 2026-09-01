using Microsoft.Xna.Framework;
using SoulsOfTerra.Content.Items.Access;
using SoulsOfTerra.Content.Items.Consumables.SoulCrystals;
using SoulsOfTerra.Content.Items.Materials;
using SoulsOfTerra.Content.Projectiles;
using SoulsOfTerra.Content.Tiles;
using SoulsOfTerra.NPCs;
using SoulsOfTerra.Players;
using SoulsOfTerra.Systems;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Common;

public enum TerraforgePlacementFailure
{
	None,
	AnotherTerraforgeActive,
	InvalidAnvil,
	Obstructed,
	Unsupported
}

public static class SoulTransactions
{
	public const long FragmentCost = 100;
	public const long WardensFragmentCost = 10_000;
	public const long SoulApparatusCost = 1_000;
	public const long GraftingAltarCost = 1_000;
	private static readonly long[] SoulCrystalCosts = { 1_250, 6_250, 31_250 };
	private static readonly long[] SoulCrystalValues = { 1_000, 5_000, 25_000 };
	private const float InteractionRange = 12f * 16f;

	public static bool TryPurchaseTerraBladeFragment(Player player, int npcIndex)
	{
		if (!IsValidSoullessInteraction(player, npcIndex)
			|| !SoulWorldSystem.TryPurchaseTerraBladeFragment(player, FragmentCost))
		{
			return false;
		}

		player.QuickSpawnItem(new EntitySource_Misc("SoulsOfTerra:FragmentPurchase"), ModContent.ItemType<TerraBladeFragment>());
		return true;
	}

	public static bool TryRecallTerraBladeFragment(Player player, int npcIndex)
	{
		if (!IsValidSoullessInteraction(player, npcIndex) || !SoulWorldSystem.TerraBladeFragmentPurchased
			|| SoulWorldSystem.HasActiveTerraforge || player.HasItem(ModContent.ItemType<TerraBladeFragment>()))
		{
			return false;
		}

		player.QuickSpawnItem(new EntitySource_Misc("SoulsOfTerra:FragmentRecall"), ModContent.ItemType<TerraBladeFragment>());
		return true;
	}

	public static bool TryTemperTerraforge(Player player, int npcIndex)
	{
		return IsValidSoullessInteraction(player, npcIndex) && SoulWorldSystem.TryTemperTerraforge(player);
	}

	public static bool TryPurchaseWardensFragment(Player player, int npcIndex)
	{
		if (!NPC.downedBoss3 || !IsValidSoullessInteraction(player, npcIndex)
			|| !player.GetModPlayer<SoulPlayer>().TrySpendSouls(WardensFragmentCost))
		{
			return false;
		}

		// The fragment is a permanent reusable key, but spare copies remain purchasable.
		player.QuickSpawnItem(new EntitySource_Misc("SoulsOfTerra:WardensFragmentPurchase"), ModContent.ItemType<WardensFragment>());
		return true;
	}

	public static bool TryPurchaseSoulApparatus(Player player, int npcIndex)
	{
		if (!NPC.downedBoss1 || !IsValidSoullessInteraction(player, npcIndex)
			|| !player.GetModPlayer<SoulPlayer>().TrySpendSouls(SoulApparatusCost))
		{
			return false;
		}

		// Unlimited copies keep the station replaceable and useful for multiple bases.
		player.QuickSpawnItem(new EntitySource_Misc("SoulsOfTerra:SoulApparatusPurchase"),
			ModContent.ItemType<SoulApparatusItem>());
		return true;
	}

	public static bool TryPurchaseGraftingAltar(Player player, int npcIndex)
	{
		if (!(NPC.downedSlimeKing || NPC.downedBoss1) || !IsValidSoullessInteraction(player, npcIndex)
			|| !player.GetModPlayer<SoulPlayer>().TrySpendSouls(GraftingAltarCost))
		{
			return false;
		}

		player.QuickSpawnItem(new EntitySource_Misc("SoulsOfTerra:GraftingAltarPurchase"),
			ModContent.ItemType<GraftingAltarItem>());
		return true;
	}

	public static bool TryConvertSoulCrystal(Player player, int npcIndex, int crystalIndex)
	{
		if (!IsValidSoullessInteraction(player, npcIndex) || !IsSoulCrystalUnlocked(crystalIndex))
		{
			return false;
		}

		long cost = GetSoulCrystalCost(crystalIndex);
		if (!player.GetModPlayer<SoulPlayer>().TrySpendSouls(cost))
		{
			return false;
		}

		int itemType = crystalIndex switch
		{
			0 => ModContent.ItemType<FaintSoulCrystal>(),
			1 => ModContent.ItemType<VividSoulCrystal>(),
			2 => ModContent.ItemType<ProfoundSoulCrystal>(),
			_ => ItemID.None
		};
		player.QuickSpawnItem(new EntitySource_Misc("SoulsOfTerra:SoulCrystalConversion"), itemType);
		return true;
	}

	public static long GetSoulCrystalCost(int crystalIndex)
	{
		return crystalIndex >= 0 && crystalIndex < SoulCrystalCosts.Length ? SoulCrystalCosts[crystalIndex] : 0;
	}

	public static long GetSoulCrystalValue(int crystalIndex)
	{
		return crystalIndex >= 0 && crystalIndex < SoulCrystalValues.Length ? SoulCrystalValues[crystalIndex] : 0;
	}

	public static bool IsSoulCrystalUnlocked(int crystalIndex)
	{
		return crystalIndex switch
		{
			0 => true,
			1 => SoulWorldSystem.TerraforgeTemper >= 1,
			2 => SoulWorldSystem.TerraforgeTemper >= 4,
			_ => false
		};
	}

	public static bool TryCondenseEssence(Player player, Point16 terraforgePosition, int essenceIndex)
	{
		if (!SoulEssenceRegistry.TryGet(essenceIndex, out SoulEssenceDefinition essence)
			|| !essence.IsUnlocked() || !IsValidTerraforgeInteraction(player, terraforgePosition)
			|| !player.GetModPlayer<SoulPlayer>().TrySpendSouls(essence.Cost))
		{
			return false;
		}

		// The registry ID keeps source diagnostics useful without one method per boss.
		player.QuickSpawnItem(new EntitySource_Misc($"SoulsOfTerra:{essence.Id}Condensation"), essence.ItemType);
		CondensationSoulWispProjectile.Spawn(player, terraforgePosition);
		ForgeOutputManifestationProjectile.Spawn(player, terraforgePosition, essence.ItemType, 48);
		return true;
	}

	public static bool TryBeginEssenceImbuement(Player player, Point16 terraforgePosition, int imbuementIndex,
		int weaponSlot, int essenceSlot)
	{
		if (!EssenceImbuementRegistry.TryGet(imbuementIndex, out EssenceImbuementDefinition imbuement)
			|| !SoulEssenceRegistry.TryFindByItemType(imbuement.EssenceItemType, out SoulEssenceDefinition essenceDefinition)
			|| !essenceDefinition.IsUnlocked()
			|| !IsValidTerraforgeInteraction(player, terraforgePosition) || weaponSlot == essenceSlot
			|| weaponSlot < 0 || weaponSlot >= player.inventory.Length
			|| essenceSlot < 0 || essenceSlot >= player.inventory.Length)
		{
			return false;
		}

		Item weapon = player.inventory[weaponSlot];
		Item essence = player.inventory[essenceSlot];
		if (!imbuement.AcceptsInput(weapon.type) || weapon.stack <= 0
			|| essence.type != imbuement.EssenceItemType || essence.stack <= 0)
		{
			return false;
		}

		int preservedPrefix = weapon.prefix;
		int consumedWeaponType = weapon.type;
		ConsumeOne(weapon);
		ConsumeOne(essence);
		if (Main.netMode == NetmodeID.Server)
		{
			NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, player.whoAmI, weaponSlot);
			NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, player.whoAmI, essenceSlot);
		}

		Item output = new(imbuement.OutputItemType);
		if (preservedPrefix > 0)
		{
			output.Prefix(preservedPrefix);
		}
		player.QuickSpawnItem(new EntitySource_Misc("SoulsOfTerra:EssenceBindingComplete"), output, 1);

		Vector2 ritualCenter = terraforgePosition.ToWorldCoordinates(TerraforgeTile.Width * 8f, -12f);
		Projectile.NewProjectile(new EntitySource_Misc("SoulsOfTerra:EssenceImbuement"), ritualCenter, Vector2.Zero,
			ModContent.ProjectileType<EssenceBindingRitualProjectile>(), 0, 0f, player.whoAmI,
			imbuementIndex, preservedPrefix, consumedWeaponType);
		CondensationSoulWispProjectile.Spawn(player, terraforgePosition);
		return true;
	}

	public static bool TryDissolveSoulspell(Player player, Point16 apparatusPosition, int recipeIndex,
		int potionSlot, int essenceSlot)
	{
		if (recipeIndex < 0 || recipeIndex >= SoulSpellRegistry.PotionSpells.Length
			|| potionSlot == essenceSlot || potionSlot < 0 || potionSlot >= player.inventory.Length
			|| essenceSlot < 0 || essenceSlot >= player.inventory.Length
			|| !IsValidSoulApparatusInteraction(player, apparatusPosition))
		{
			return false;
		}

		SoulSpellDefinition spell = SoulSpellRegistry.PotionSpells[recipeIndex];
		SoulSpellPlayer spellPlayer = player.GetModPlayer<SoulSpellPlayer>();
		if (spellPlayer.HasLearned(spell.Id)
			|| !SoulEssenceRegistry.TryFindByItemType(spell.EssenceItemType, out SoulEssenceDefinition essenceDefinition)
			|| !essenceDefinition.IsUnlocked())
		{
			return false;
		}

		Item potion = player.inventory[potionSlot];
		Item essence = player.inventory[essenceSlot];
		if (potion.type != spell.PotionItemType || potion.stack <= 0
			|| essence.type != spell.EssenceItemType || essence.stack <= 0)
		{
			return false;
		}

		if (!spellPlayer.TryLearn(spell.Id))
		{
			return false;
		}

		ConsumeOne(potion);
		ConsumeOne(essence);
		if (Main.netMode == NetmodeID.Server)
		{
			NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, player.whoAmI, potionSlot);
			NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, player.whoAmI, essenceSlot);
		}

		Vector2 center = apparatusPosition.ToWorldCoordinates(SoulApparatusTile.Width * 8f, 8f);
		Projectile.NewProjectile(new EntitySource_Misc("SoulsOfTerra:SoulspellDissolution"), center, Vector2.Zero,
			ModContent.ProjectileType<SoulspellDissolutionRitualProjectile>(), 0, 0f, player.whoAmI,
			recipeIndex, spell.PotionItemType, spell.EssenceItemType);
		return true;
	}

	public static bool TryGraftMutation(Player player, Point16 altarPosition, int mutationSlot, int sourceSlot,
		int expectedItemType = ItemID.None)
	{
		if (!IsValidGraftingAltarInteraction(player, altarPosition))
		{
			return false;
		}

		sourceSlot = ResolveEssenceSlot(player, sourceSlot, expectedItemType);
		if (sourceSlot < 0)
		{
			return false;
		}

		return TryApplyMutationGraft(player, mutationSlot, player.inventory[sourceSlot], sourceSlot);
	}

	public static bool TryGraftMutationFromCursor(Player player, Point16 altarPosition, int mutationSlot,
		Item cursorEssence)
	{
		if (!IsValidGraftingAltarInteraction(player, altarPosition))
		{
			return false;
		}

		// Main.mouseItem is distinct from its inventory[58] synchronization clone.
		return TryApplyMutationGraft(player, mutationSlot, cursorEssence, -1);
	}

	private static bool TryApplyMutationGraft(Player player, int mutationSlot, Item essence, int sourceSlot)
	{
		if (essence.stack <= 0 || !MutationRegistry.TryFindByItemType(essence.type, out MutationDefinition definition)
			|| !definition.Implemented)
		{
			return false;
		}

		MutationPlayer mutationPlayer = player.GetModPlayer<MutationPlayer>();
		if (!mutationPlayer.TrySetMutation(mutationSlot, definition.Id))
		{
			return false;
		}

		ConsumeOne(essence);
		if (Main.netMode == NetmodeID.Server && sourceSlot >= 0)
		{
			NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, player.whoAmI, sourceSlot);
		}
		mutationPlayer.SendState();
		return true;
	}

	private static int ResolveEssenceSlot(Player player, int preferredSlot, int expectedItemType)
	{
		// Cursor-slot synchronization can arrive one packet later, so fall back to the matching stack.
		if (preferredSlot >= 0 && preferredSlot < player.inventory.Length
			&& player.inventory[preferredSlot].stack > 0
			&& (expectedItemType <= ItemID.None || player.inventory[preferredSlot].type == expectedItemType))
		{
			return preferredSlot;
		}

		for (int slot = 0; slot < player.inventory.Length; slot++)
		{
			Item item = player.inventory[slot];
			if (item.stack > 0 && item.type == expectedItemType)
			{
				return slot;
			}
		}
		return -1;
	}

	public static bool TryPurgeMutation(Player player, Point16 altarPosition, int mutationSlot)
	{
		if (!IsValidGraftingAltarInteraction(player, altarPosition)
			|| !player.GetModPlayer<MutationPlayer>().TryPurge(mutationSlot))
		{
			return false;
		}

		player.GetModPlayer<MutationPlayer>().SendState();
		return true;
	}

	private static void ConsumeOne(Item item)
	{
		if (--item.stack <= 0)
		{
			item.TurnToAir();
		}
	}

	public static bool TryFormTerraforge(Player player, Point16 anvilTopLeft)
	{
		if (!player.active || player.dead || Vector2.DistanceSquared(player.Center, anvilTopLeft.ToWorldCoordinates(16f, 8f)) > InteractionRange * InteractionRange)
		{
			return false;
		}

		Item heldItem = player.inventory[player.selectedItem];
		if (heldItem.type != ModContent.ItemType<TerraBladeFragment>() || heldItem.stack <= 0
			|| !TryGetTerraforgePlacement(anvilTopLeft, out Point16 forgeTopLeft, out int anvilItemType, out _)
			|| !SoulWorldSystem.TryActivateTerraforge(forgeTopLeft))
		{
			return false;
		}

		int forgeStyle = anvilItemType == ItemID.LeadAnvil ? 1 : 0;
		ushort forgeType = (ushort)ModContent.TileType<TerraforgeTile>();
		for (int x = 0; x < TerraforgeTile.Width; x++)
		{
			for (int y = 0; y < TerraforgeTile.Height; y++)
			{
				Tile tile = Framing.GetTileSafely(forgeTopLeft.X + x, forgeTopLeft.Y + y);
				tile.HasTile = true;
				tile.TileType = forgeType;
				// The horizontal style remembers whether the ritual consumed iron or lead.
				tile.TileFrameX = (short)((forgeStyle * TerraforgeTile.Width + x) * 18);
				tile.TileFrameY = (short)(y * 18);
				tile.Slope = SlopeType.Solid;
				tile.IsHalfBlock = false;
			}
		}

		heldItem.stack--;
		if (heldItem.stack <= 0)
		{
			heldItem.TurnToAir();
		}

		if (Main.netMode == NetmodeID.Server)
		{
			NetMessage.SendTileSquare(-1, forgeTopLeft.X + 2, forgeTopLeft.Y + 1, TerraforgeTile.Width, TerraforgeTile.Height);
			NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, player.whoAmI, player.selectedItem);
		}

		TerraforgeFormationProjectile.Spawn(player, forgeTopLeft);

		return true;
	}

	public static Point16 GetAnvilTopLeft(int i, int j)
	{
		Tile tile = Framing.GetTileSafely(i, j);
		return new Point16(i - tile.TileFrameX / 18 % 2, j);
	}

	public static bool TryGetTerraforgePlacement(Point16 anvilTopLeft, out Point16 forgeTopLeft,
		out int anvilItemType, out TerraforgePlacementFailure failure)
	{
		forgeTopLeft = new Point16(anvilTopLeft.X - 1, anvilTopLeft.Y - (TerraforgeTile.Height - 1));
		anvilItemType = ItemID.None;
		failure = TerraforgePlacementFailure.None;
		if (SoulWorldSystem.HasActiveTerraforge)
		{
			failure = TerraforgePlacementFailure.AnotherTerraforgeActive;
			return false;
		}

		if (!WorldGen.InWorld(forgeTopLeft.X, forgeTopLeft.Y, 2)
			|| !WorldGen.InWorld(forgeTopLeft.X + TerraforgeTile.Width - 1, forgeTopLeft.Y + TerraforgeTile.Height, 2))
		{
			failure = TerraforgePlacementFailure.Obstructed;
			return false;
		}

		Tile leftAnvil = Framing.GetTileSafely(anvilTopLeft.X, anvilTopLeft.Y);
		int anvilStyle = leftAnvil.TileFrameX / 36;
		if (anvilStyle is < 0 or > 1)
		{
			failure = TerraforgePlacementFailure.InvalidAnvil;
			return false;
		}

		for (int x = 0; x < 2; x++)
		{
			Tile tile = Framing.GetTileSafely(anvilTopLeft.X + x, anvilTopLeft.Y);
			if (!tile.HasTile || tile.TileType != TileID.Anvils || GetAnvilTopLeft(anvilTopLeft.X + x, anvilTopLeft.Y) != anvilTopLeft
				|| tile.TileFrameX / 36 != anvilStyle)
			{
				failure = TerraforgePlacementFailure.InvalidAnvil;
				return false;
			}
		}

		for (int x = 0; x < TerraforgeTile.Width; x++)
		{
			for (int y = 0; y < TerraforgeTile.Height; y++)
			{
				bool isSourceAnvil = y == TerraforgeTile.Height - 1 && x is 1 or 2;
				if (!isSourceAnvil && Framing.GetTileSafely(forgeTopLeft.X + x, forgeTopLeft.Y + y).HasTile)
				{
					failure = TerraforgePlacementFailure.Obstructed;
					return false;
				}
			}

			if (!WorldGen.SolidTile(forgeTopLeft.X + x, forgeTopLeft.Y + TerraforgeTile.Height))
			{
				failure = TerraforgePlacementFailure.Unsupported;
				return false;
			}
		}

		anvilItemType = anvilStyle == 1 ? ItemID.LeadAnvil : ItemID.IronAnvil;
		return true;
	}

	public static string GetPlacementFailureMessage(TerraforgePlacementFailure failure)
	{
		return failure switch
		{
			TerraforgePlacementFailure.AnotherTerraforgeActive => "Only one Terraforge can be active in this world.",
			TerraforgePlacementFailure.InvalidAnvil => "The fragment requires a complete Iron or Lead Anvil.",
			TerraforgePlacementFailure.Unsupported => "The Terraforge requires solid ground beneath its entire base.",
			_ => "Clear a 4 by 3 space around and above the anvil."
		};
	}

	private static bool IsValidSoullessInteraction(Player player, int npcIndex)
	{
		if (!player.active || player.dead || npcIndex < 0 || npcIndex >= Main.maxNPCs)
		{
			return false;
		}

		NPC npc = Main.npc[npcIndex];
		return npc.active && npc.type == ModContent.NPCType<SoullessNPC>() && Vector2.DistanceSquared(player.Center, npc.Center) <= InteractionRange * InteractionRange;
	}

	private static bool IsValidTerraforgeInteraction(Player player, Point16 terraforgePosition)
	{
		Tile tile = Framing.GetTileSafely(terraforgePosition.X, terraforgePosition.Y);
		return player.active && !player.dead && tile.HasTile && tile.TileType == ModContent.TileType<TerraforgeTile>()
			&& Vector2.DistanceSquared(player.Center, terraforgePosition.ToWorldCoordinates(32f, 24f)) <= InteractionRange * InteractionRange;
	}

	private static bool IsValidSoulApparatusInteraction(Player player, Point16 apparatusPosition)
	{
		Tile tile = Framing.GetTileSafely(apparatusPosition.X, apparatusPosition.Y);
		return player.active && !player.dead && tile.HasTile && tile.TileType == ModContent.TileType<SoulApparatusTile>()
			&& Vector2.DistanceSquared(player.Center, apparatusPosition.ToWorldCoordinates(24f, 24f)) <= InteractionRange * InteractionRange;
	}

	private static bool IsValidGraftingAltarInteraction(Player player, Point16 altarPosition)
	{
		Tile tile = Framing.GetTileSafely(altarPosition.X, altarPosition.Y);
		return player.active && !player.dead && tile.HasTile && tile.TileType == ModContent.TileType<GraftingAltarTile>()
			&& Vector2.DistanceSquared(player.Center, altarPosition.ToWorldCoordinates(24f, 24f)) <= InteractionRange * InteractionRange;
	}
}
