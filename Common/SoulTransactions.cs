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

public static class SoulTransactions
{
	public const long CoreCost = 100;
	public const long WardensFragmentCost = 10_000;
	private static readonly long[] SoulCrystalCosts = { 1_250, 6_250, 31_250 };
	private static readonly long[] SoulCrystalValues = { 1_000, 5_000, 25_000 };
	private const float InteractionRange = 12f * 16f;

	public static bool TryPurchaseCore(Player player, int npcIndex)
	{
		if (!IsValidSoullessInteraction(player, npcIndex) || !player.GetModPlayer<SoulPlayer>().TrySpendSouls(CoreCost))
		{
			return false;
		}

		player.QuickSpawnItem(new EntitySource_Misc("SoulsOfTerra:CorePurchase"), ModContent.ItemType<BrokenTerraBladeCore>());
		return true;
	}

	public static bool TryUpgradeShrine(Player player, int npcIndex)
	{
		return IsValidSoullessInteraction(player, npcIndex) && SoulWorldSystem.TryUpgradeShrine(player);
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
			1 => SoulWorldSystem.TerraShrineTier >= 1,
			2 => SoulWorldSystem.TerraShrineTier >= 4,
			_ => false
		};
	}

	public static bool TryCondenseEssence(Player player, Point16 shrinePosition, int essenceIndex)
	{
		if (!SoulEssenceRegistry.TryGet(essenceIndex, out SoulEssenceDefinition essence)
			|| !essence.IsUnlocked() || !IsValidShrineInteraction(player, shrinePosition)
			|| !player.GetModPlayer<SoulPlayer>().TrySpendSouls(essence.Cost))
		{
			return false;
		}

		// The registry ID keeps source diagnostics useful without one method per boss.
		player.QuickSpawnItem(new EntitySource_Misc($"SoulsOfTerra:{essence.Id}Condensation"), essence.ItemType);
		CondensationSoulWispProjectile.Spawn(player, shrinePosition);
		return true;
	}

	public static bool TryBeginEssenceImbuement(Player player, Point16 shrinePosition, int imbuementIndex,
		int weaponSlot, int essenceSlot)
	{
		if (!EssenceImbuementRegistry.TryGet(imbuementIndex, out EssenceImbuementDefinition imbuement)
			|| !IsValidShrineInteraction(player, shrinePosition) || weaponSlot == essenceSlot
			|| weaponSlot < 0 || weaponSlot >= player.inventory.Length
			|| essenceSlot < 0 || essenceSlot >= player.inventory.Length)
		{
			return false;
		}

		Item weapon = player.inventory[weaponSlot];
		Item essence = player.inventory[essenceSlot];
		if (weapon.type != imbuement.InputItemType || weapon.stack <= 0
			|| essence.type != imbuement.EssenceItemType || essence.stack <= 0)
		{
			return false;
		}

		int preservedPrefix = weapon.prefix;
		ConsumeOne(weapon);
		ConsumeOne(essence);
		if (Main.netMode == NetmodeID.Server)
		{
			NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, player.whoAmI, weaponSlot);
			NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, player.whoAmI, essenceSlot);
		}

		Vector2 ritualCenter = shrinePosition.ToWorldCoordinates(TerraShrineTile.Width * 8f, -12f);
		Projectile.NewProjectile(new EntitySource_Misc("SoulsOfTerra:EssenceImbuement"), ritualCenter, Vector2.Zero,
			ModContent.ProjectileType<EssenceBindingRitualProjectile>(), 0, 0f, player.whoAmI,
			imbuementIndex, preservedPrefix);
		CondensationSoulWispProjectile.Spawn(player, shrinePosition);
		return true;
	}

	private static void ConsumeOne(Item item)
	{
		if (--item.stack <= 0)
		{
			item.TurnToAir();
		}
	}

	public static bool TryTransformAnvil(Player player, Point16 anvilTopLeft)
	{
		if (!player.active || player.dead || Vector2.DistanceSquared(player.Center, anvilTopLeft.ToWorldCoordinates(16f, 8f)) > InteractionRange * InteractionRange)
		{
			return false;
		}

		Item heldItem = player.inventory[player.selectedItem];
		if (heldItem.type != ModContent.ItemType<BrokenTerraBladeCore>() || heldItem.stack <= 0
			|| !TryGetAnvilTransformation(anvilTopLeft, out Point16 shrineTopLeft, out int anvilItemType))
		{
			return false;
		}

		int shrineStyle = anvilItemType == ItemID.LeadAnvil ? 1 : 0;
		ushort shrineType = (ushort)ModContent.TileType<TerraShrineTile>();
		for (int x = 0; x < TerraShrineTile.Width; x++)
		{
			for (int y = 0; y < TerraShrineTile.Height; y++)
			{
				Tile tile = Framing.GetTileSafely(shrineTopLeft.X + x, shrineTopLeft.Y + y);
				tile.HasTile = true;
				tile.TileType = shrineType;
				// The horizontal style remembers whether the ritual consumed iron or lead.
				tile.TileFrameX = (short)((shrineStyle * TerraShrineTile.Width + x) * 18);
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
			NetMessage.SendTileSquare(-1, shrineTopLeft.X + 2, shrineTopLeft.Y + 1, TerraShrineTile.Width, TerraShrineTile.Height);
			NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, player.whoAmI, player.selectedItem);
		}

		return true;
	}

	public static Point16 GetAnvilTopLeft(int i, int j)
	{
		Tile tile = Framing.GetTileSafely(i, j);
		return new Point16(i - tile.TileFrameX / 18 % 2, j);
	}

	public static bool TryGetAnvilTransformation(Point16 anvilTopLeft, out Point16 shrineTopLeft, out int anvilItemType)
	{
		shrineTopLeft = new Point16(anvilTopLeft.X - 1, anvilTopLeft.Y - (TerraShrineTile.Height - 1));
		anvilItemType = ItemID.None;
		if (!WorldGen.InWorld(shrineTopLeft.X, shrineTopLeft.Y, 2)
			|| !WorldGen.InWorld(shrineTopLeft.X + TerraShrineTile.Width - 1, shrineTopLeft.Y + TerraShrineTile.Height, 2))
		{
			return false;
		}

		Tile leftAnvil = Framing.GetTileSafely(anvilTopLeft.X, anvilTopLeft.Y);
		int anvilStyle = leftAnvil.TileFrameX / 36;
		if (anvilStyle is < 0 or > 1)
		{
			return false;
		}

		for (int x = 0; x < 2; x++)
		{
			Tile tile = Framing.GetTileSafely(anvilTopLeft.X + x, anvilTopLeft.Y);
			if (!tile.HasTile || tile.TileType != TileID.Anvils || GetAnvilTopLeft(anvilTopLeft.X + x, anvilTopLeft.Y) != anvilTopLeft
				|| tile.TileFrameX / 36 != anvilStyle)
			{
				return false;
			}
		}

		for (int x = 0; x < TerraShrineTile.Width; x++)
		{
			for (int y = 0; y < TerraShrineTile.Height; y++)
			{
				bool isSourceAnvil = y == TerraShrineTile.Height - 1 && x is 1 or 2;
				if (!isSourceAnvil && Framing.GetTileSafely(shrineTopLeft.X + x, shrineTopLeft.Y + y).HasTile)
				{
					return false;
				}
			}

			if (!WorldGen.SolidTile(shrineTopLeft.X + x, shrineTopLeft.Y + TerraShrineTile.Height))
			{
				return false;
			}
		}

		anvilItemType = anvilStyle == 1 ? ItemID.LeadAnvil : ItemID.IronAnvil;
		return true;
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

	private static bool IsValidShrineInteraction(Player player, Point16 shrinePosition)
	{
		Tile tile = Framing.GetTileSafely(shrinePosition.X, shrinePosition.Y);
		return player.active && !player.dead && tile.HasTile && tile.TileType == ModContent.TileType<TerraShrineTile>()
			&& Vector2.DistanceSquared(player.Center, shrinePosition.ToWorldCoordinates(8f, 8f)) <= InteractionRange * InteractionRange;
	}
}
