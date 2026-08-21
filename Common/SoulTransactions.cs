using Microsoft.Xna.Framework;
using SoulsOfTerra.Content.Items;
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
	public const long SlimeEssenceCost = 2_500;
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

	public static bool TryCondenseSlimeEssence(Player player, Point16 shrinePosition)
	{
		if (!NPC.downedSlimeKing || !IsValidShrineInteraction(player, shrinePosition) || !player.GetModPlayer<SoulPlayer>().TrySpendSouls(SlimeEssenceCost))
		{
			return false;
		}

		player.QuickSpawnItem(new EntitySource_Misc("SoulsOfTerra:SlimeCondensation"), ModContent.ItemType<SlimeEssence>());
		return true;
	}

	public static bool TryTransformCampfire(Player player, Point16 topLeft)
	{
		if (!player.active || player.dead || Vector2.DistanceSquared(player.Center, topLeft.ToWorldCoordinates(24f, 16f)) > InteractionRange * InteractionRange)
		{
			return false;
		}

		Item heldItem = player.inventory[player.selectedItem];
		if (heldItem.type != ModContent.ItemType<BrokenTerraBladeCore>() || heldItem.stack <= 0 || !IsCampfire(topLeft))
		{
			return false;
		}

		ushort shrineType = (ushort)ModContent.TileType<TerraShrineTile>();
		for (int x = 0; x < 3; x++)
		{
			for (int y = 0; y < 2; y++)
			{
				Tile tile = Framing.GetTileSafely(topLeft.X + x, topLeft.Y + y);
				tile.HasTile = true;
				tile.TileType = shrineType;
				tile.TileFrameX = (short)(x * 18);
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
			NetMessage.SendTileSquare(-1, topLeft.X + 1, topLeft.Y + 1, 3, 2);
			NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, player.whoAmI, player.selectedItem);
		}

		return true;
	}

	public static Point16 GetCampfireTopLeft(int i, int j)
	{
		Tile tile = Framing.GetTileSafely(i, j);
		return new Point16(i - tile.TileFrameX / 18 % 3, j - tile.TileFrameY / 18 % 2);
	}

	private static bool IsCampfire(Point16 topLeft)
	{
		for (int x = 0; x < 3; x++)
		{
			for (int y = 0; y < 2; y++)
			{
				Tile tile = Framing.GetTileSafely(topLeft.X + x, topLeft.Y + y);
				if (!tile.HasTile || tile.TileType != TileID.Campfire || GetCampfireTopLeft(topLeft.X + x, topLeft.Y + y) != topLeft)
				{
					return false;
				}
			}
		}

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
