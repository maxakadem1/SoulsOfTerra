using System.IO;
using Microsoft.Xna.Framework;
using SoulsOfTerra.Common;
using SoulsOfTerra.Content.Projectiles;
using SoulsOfTerra.Players;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra;

public enum SoulMessageType : byte
{
	SyncPlayer,
	RequestBloodstainRecovery,
	RequestCorePurchase,
	RequestShrineUpgrade,
	RequestEssenceCondensation,
	RequestEssenceImbuement,
	RequestSoulCrystalConversion,
	RequestAnvilTransformation
}

public class SoulsOfTerra : Mod
{
	public override void HandlePacket(BinaryReader reader, int whoAmI)
	{
		SoulMessageType messageType = (SoulMessageType)reader.ReadByte();

		switch (messageType)
		{
			case SoulMessageType.SyncPlayer:
				HandlePlayerSync(reader, whoAmI);
				break;
			case SoulMessageType.RequestBloodstainRecovery:
				HandleBloodstainRecovery(reader, whoAmI);
				break;
			case SoulMessageType.RequestCorePurchase:
				HandleCorePurchase(reader, whoAmI);
				break;
			case SoulMessageType.RequestShrineUpgrade:
				HandleShrineUpgrade(reader, whoAmI);
				break;
			case SoulMessageType.RequestEssenceCondensation:
				HandleEssenceCondensation(reader, whoAmI);
				break;
			case SoulMessageType.RequestEssenceImbuement:
				HandleEssenceImbuement(reader, whoAmI);
				break;
			case SoulMessageType.RequestSoulCrystalConversion:
				HandleSoulCrystalConversion(reader, whoAmI);
				break;
			case SoulMessageType.RequestAnvilTransformation:
				HandleAnvilTransformation(reader, whoAmI);
				break;
		}
	}

	private static void HandlePlayerSync(BinaryReader reader, int whoAmI)
	{
		int playerIndex = reader.ReadByte();
		long balance = reader.ReadInt64();
		string characterId = reader.ReadString();

		if (playerIndex < 0 || playerIndex >= Main.maxPlayers)
		{
			return;
		}

		// Clients can only provide data for their own character.
		if (Main.netMode == NetmodeID.Server && playerIndex != whoAmI)
		{
			return;
		}

		SoulPlayer soulPlayer = Main.player[playerIndex].GetModPlayer<SoulPlayer>();
		soulPlayer.ReceiveSync(balance, characterId);

		if (Main.netMode == NetmodeID.Server)
		{
			soulPlayer.SyncSoulData(-1, whoAmI);
		}
	}

	private static void HandleBloodstainRecovery(BinaryReader reader, int whoAmI)
	{
		int projectileIndex = reader.ReadInt16();
		if (Main.netMode != NetmodeID.Server || whoAmI < 0 || whoAmI >= Main.maxPlayers)
		{
			return;
		}

		if (projectileIndex < 0 || projectileIndex >= Main.maxProjectiles)
		{
			return;
		}

		Projectile projectile = Main.projectile[projectileIndex];
		Player player = Main.player[whoAmI];
		if (!projectile.active || projectile.ModProjectile is not SoulBloodstainProjectile bloodstain || !player.active || player.dead)
		{
			return;
		}

		Vector2 compareSpot = player.Center;
		if (player.IsProjectileInteractibleAndInInteractionRange(projectile, ref compareSpot))
		{
			bloodstain.Recover(player);
		}
	}

	private static Player GetRequestingPlayer(int whoAmI)
	{
		return Main.netMode == NetmodeID.Server && whoAmI >= 0 && whoAmI < Main.maxPlayers
			? Main.player[whoAmI]
			: null;
	}

	private static void HandleCorePurchase(BinaryReader reader, int whoAmI)
	{
		int npcIndex = reader.ReadInt16();
		Player player = GetRequestingPlayer(whoAmI);
		if (player is not null)
		{
			SoulTransactions.TryPurchaseCore(player, npcIndex);
		}
	}

	private static void HandleShrineUpgrade(BinaryReader reader, int whoAmI)
	{
		int npcIndex = reader.ReadInt16();
		Player player = GetRequestingPlayer(whoAmI);
		if (player is not null)
		{
			SoulTransactions.TryUpgradeShrine(player, npcIndex);
		}
	}

	private static void HandleEssenceCondensation(BinaryReader reader, int whoAmI)
	{
		int essenceIndex = reader.ReadByte();
		Point16 shrinePosition = new(reader.ReadInt16(), reader.ReadInt16());
		Player player = GetRequestingPlayer(whoAmI);
		if (player is not null)
		{
			SoulTransactions.TryCondenseEssence(player, shrinePosition, essenceIndex);
		}
	}

	private static void HandleEssenceImbuement(BinaryReader reader, int whoAmI)
	{
		int imbuementIndex = reader.ReadByte();
		int weaponSlot = reader.ReadByte();
		int essenceSlot = reader.ReadByte();
		Point16 shrinePosition = new(reader.ReadInt16(), reader.ReadInt16());
		Player player = GetRequestingPlayer(whoAmI);
		if (player is not null)
		{
			SoulTransactions.TryBeginEssenceImbuement(player, shrinePosition, imbuementIndex, weaponSlot, essenceSlot);
		}
	}

	private static void HandleSoulCrystalConversion(BinaryReader reader, int whoAmI)
	{
		int npcIndex = reader.ReadInt16();
		int crystalIndex = reader.ReadByte();
		Player player = GetRequestingPlayer(whoAmI);
		if (player is not null)
		{
			SoulTransactions.TryConvertSoulCrystal(player, npcIndex, crystalIndex);
		}
	}

	private static void HandleAnvilTransformation(BinaryReader reader, int whoAmI)
	{
		Point16 anvilPosition = new(reader.ReadInt16(), reader.ReadInt16());
		Player player = GetRequestingPlayer(whoAmI);
		if (player is not null)
		{
			SoulTransactions.TryTransformAnvil(player, anvilPosition);
		}
	}
}
