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
	RequestFragmentPurchase,
	RequestFragmentRecall,
	RequestTerraforgeTemper,
	RequestEssenceCondensation,
	RequestEssenceImbuement,
	RequestSoulCrystalConversion,
	RequestTerraforgeFormation,
	TerraforgeFormationFailed,
	RequestWardenFragmentPurchase,
	RequestCongregationSummon
}

public class SoulsOfTerra : Mod
{
	private static readonly (string Key, string DefaultValue)[] CustomLocalizations =
	{
		("UI.RecoverBloodstain", "Right-click to recover {0} souls"),
		("UI.Commune", "Commune"),
		("Dialogue.Soulless.Introduction", "So another bearer wakes. Keep close the souls you gather; death is eager to loosen your grasp."),
		("Dialogue.Soulless.BloodstainHint", "What spills from you does not vanish. Return to the stain, reach into it, and take back what remains."),
		("Dialogue.Soulless.AfterKingSlime", "Even a crown of gel leaves an echo. The Terraforge can press that echo into useful form."),
		("Dialogue.Soulless.FragmentSale", "This edge remembers a blade older than either of us. Drive it into an anvil, and the metal will learn to shape what flesh leaves behind."),
		("Dialogue.Soulless.BuriedCourtHint", "Take this fragment. The reliquary remembers the office, not the hand. Let the prisoners mistake you for their keeper."),
		("Dialogue.Soulless.AfterCongregation", "So the court is silent. Strange... I remember every voice you freed, though none of them were mine."),
		("Dialogue.Court.ReliquaryAccepts", "The reliquary accepts an authority it should not remember."),
		("Dialogue.Court.CongregationDeath", "The final voices fade: The hand behind the mark... is hollow.")
	};

	public override void Load()
	{
		// Custom keys must be registered while the mod loads for HJSON values to bind to them.
		foreach ((string key, string defaultValue) in CustomLocalizations)
		{
			GetLocalization(key, () => defaultValue);
		}
	}

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
			case SoulMessageType.RequestFragmentPurchase:
				HandleFragmentPurchase(reader, whoAmI);
				break;
			case SoulMessageType.RequestFragmentRecall:
				HandleFragmentRecall(reader, whoAmI);
				break;
			case SoulMessageType.RequestTerraforgeTemper:
				HandleTerraforgeTemper(reader, whoAmI);
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
			case SoulMessageType.RequestTerraforgeFormation:
				HandleTerraforgeFormation(reader, whoAmI);
				break;
			case SoulMessageType.TerraforgeFormationFailed:
				HandleTerraforgeFormationFailed(reader);
				break;
			case SoulMessageType.RequestWardenFragmentPurchase:
				HandleWardenFragmentPurchase(reader, whoAmI);
				break;
			case SoulMessageType.RequestCongregationSummon:
				HandleCongregationSummon(reader, whoAmI);
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

	private static void HandleFragmentPurchase(BinaryReader reader, int whoAmI)
	{
		int npcIndex = reader.ReadInt16();
		Player player = GetRequestingPlayer(whoAmI);
		if (player is not null)
		{
			SoulTransactions.TryPurchaseTerraBladeFragment(player, npcIndex);
		}
	}

	private static void HandleFragmentRecall(BinaryReader reader, int whoAmI)
	{
		int npcIndex = reader.ReadInt16();
		Player player = GetRequestingPlayer(whoAmI);
		if (player is not null)
		{
			SoulTransactions.TryRecallTerraBladeFragment(player, npcIndex);
		}
	}

	private static void HandleTerraforgeTemper(BinaryReader reader, int whoAmI)
	{
		int npcIndex = reader.ReadInt16();
		Player player = GetRequestingPlayer(whoAmI);
		if (player is not null)
		{
			SoulTransactions.TryTemperTerraforge(player, npcIndex);
		}
	}

	private static void HandleEssenceCondensation(BinaryReader reader, int whoAmI)
	{
		int essenceIndex = reader.ReadByte();
		Point16 terraforgePosition = new(reader.ReadInt16(), reader.ReadInt16());
		Player player = GetRequestingPlayer(whoAmI);
		if (player is not null)
		{
			SoulTransactions.TryCondenseEssence(player, terraforgePosition, essenceIndex);
		}
	}

	private static void HandleEssenceImbuement(BinaryReader reader, int whoAmI)
	{
		int imbuementIndex = reader.ReadByte();
		int weaponSlot = reader.ReadByte();
		int essenceSlot = reader.ReadByte();
		Point16 terraforgePosition = new(reader.ReadInt16(), reader.ReadInt16());
		Player player = GetRequestingPlayer(whoAmI);
		if (player is not null)
		{
			SoulTransactions.TryBeginEssenceImbuement(player, terraforgePosition, imbuementIndex, weaponSlot, essenceSlot);
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

	private static void HandleTerraforgeFormation(BinaryReader reader, int whoAmI)
	{
		Point16 anvilPosition = new(reader.ReadInt16(), reader.ReadInt16());
		Player player = GetRequestingPlayer(whoAmI);
		if (player is not null && !SoulTransactions.TryFormTerraforge(player, anvilPosition))
		{
			SoulTransactions.TryGetTerraforgePlacement(anvilPosition, out _, out _, out TerraforgePlacementFailure failure);
			ModPacket packet = ModContent.GetInstance<SoulsOfTerra>().GetPacket();
			packet.Write((byte)SoulMessageType.TerraforgeFormationFailed);
			packet.Write((byte)failure);
			packet.Send(whoAmI);
		}
	}

	private static void HandleTerraforgeFormationFailed(BinaryReader reader)
	{
		TerraforgePlacementFailure failure = (TerraforgePlacementFailure)reader.ReadByte();
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			Main.NewText(SoulTransactions.GetPlacementFailureMessage(failure), 110, 190, 160);
		}
	}

	private static void HandleWardenFragmentPurchase(BinaryReader reader, int whoAmI)
	{
		int npcIndex = reader.ReadInt16();
		Player player = GetRequestingPlayer(whoAmI);
		if (player is not null)
		{
			SoulTransactions.TryPurchaseWardensFragment(player, npcIndex);
		}
	}

	private static void HandleCongregationSummon(BinaryReader reader, int whoAmI)
	{
		Point16 clickedTile = new(reader.ReadInt16(), reader.ReadInt16());
		Player player = GetRequestingPlayer(whoAmI);
		if (player is not null)
		{
			Systems.BuriedCourtSystem.TrySummonBoss(player, clickedTile);
		}
	}
}
