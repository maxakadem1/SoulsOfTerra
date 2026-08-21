using System.IO;
using Microsoft.Xna.Framework;
using SoulsOfTerra.Content.Projectiles;
using SoulsOfTerra.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra;

public enum SoulMessageType : byte
{
	SyncPlayer,
	RequestBloodstainRecovery
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
}
