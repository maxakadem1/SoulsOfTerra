using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using SoulsOfTerra.Content.Projectiles;
using SoulsOfTerra.NPCs;
using SoulsOfTerra.Players;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SoulsOfTerra.Systems;

public class SoulWorldSystem : ModSystem
{
	private static readonly long[] ShrineUpgradeCosts =
	{
		10_000,
		20_000,
		30_000,
		50_000,
		120_000,
		200_000,
		300_000,
		450_000,
		750_000
	};

	private readonly List<SavedBloodstain> pendingBloodstains = new();
	private int soullessSpawnTimer;

	public static bool SoullessSpawnedOnce { get; private set; }
	public static int TerraShrineTier { get; private set; }

	public override void OnWorldLoad()
	{
		pendingBloodstains.Clear();
		SoullessSpawnedOnce = false;
		TerraShrineTier = 0;
		soullessSpawnTimer = 0;
	}

	public override void OnWorldUnload()
	{
		pendingBloodstains.Clear();
		SoullessSpawnedOnce = false;
		TerraShrineTier = 0;
		soullessSpawnTimer = 0;
	}

	public override void SaveWorldData(TagCompound tag)
	{
		// Temporary enemy orbs intentionally remain session-only.
		List<TagCompound> saved = new();
		foreach (Projectile projectile in Main.ActiveProjectiles)
		{
			if (projectile.ModProjectile is not SoulBloodstainProjectile bloodstain || bloodstain.StoredSouls <= 0)
			{
				continue;
			}

			saved.Add(new TagCompound
			{
				["x"] = projectile.Center.X,
				["y"] = projectile.Center.Y,
				["souls"] = bloodstain.StoredSouls,
				["characterId"] = bloodstain.OriginCharacterId
			});
		}

		if (saved.Count > 0)
		{
			tag["bloodstains"] = saved;
		}

		if (SoullessSpawnedOnce)
		{
			tag["soullessSpawned"] = true;
		}

		if (TerraShrineTier > 0)
		{
			tag["terraShrineTier"] = TerraShrineTier;
		}

	}

	public override void LoadWorldData(TagCompound tag)
	{
		pendingBloodstains.Clear();
		SoullessSpawnedOnce = tag.GetBool("soullessSpawned");
		TerraShrineTier = System.Math.Clamp(tag.GetInt("terraShrineTier"), 0, ShrineUpgradeCosts.Length);
		foreach (TagCompound saved in tag.GetList<TagCompound>("bloodstains"))
		{
			long souls = saved.GetLong("souls");
			if (souls > 0)
			{
				pendingBloodstains.Add(new SavedBloodstain(
					new Vector2(saved.GetFloat("x"), saved.GetFloat("y")),
					souls,
					saved.GetString("characterId")));
			}
		}
	}

	public override void PostWorldLoad()
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			return;
		}

		IEntitySource source = new EntitySource_Misc("SoulsOfTerra:BloodstainLoad");
		foreach (SavedBloodstain saved in pendingBloodstains)
		{
			SoulBloodstainProjectile.Spawn(source, saved.Position, saved.Souls, saved.CharacterId);
		}

		pendingBloodstains.Clear();
	}

	public override void PostUpdateWorld()
	{
		if (SoullessSpawnedOnce || NPC.AnyNPCs(ModContent.NPCType<SoullessNPC>()) || ++soullessSpawnTimer < 120)
		{
			return;
		}

		foreach (Player player in Main.ActivePlayers)
		{
			int index = NPC.NewNPC(new EntitySource_Misc("SoulsOfTerra:InitialSoulless"), (int)player.Center.X, (int)player.Center.Y, ModContent.NPCType<SoullessNPC>());
			if (index >= 0 && index < Main.maxNPCs)
			{
				Main.npc[index].homeless = true;
				SoullessSpawnedOnce = true;
			}

			break;
		}
	}

	public override void NetSend(BinaryWriter writer)
	{
		writer.Write(SoullessSpawnedOnce);
		writer.Write((byte)TerraShrineTier);
	}

	public override void NetReceive(BinaryReader reader)
	{
		SoullessSpawnedOnce = reader.ReadBoolean();
		TerraShrineTier = reader.ReadByte();
	}

	public static long GetNextUpgradeCost()
	{
		return TerraShrineTier >= ShrineUpgradeCosts.Length ? 0 : ShrineUpgradeCosts[TerraShrineTier];
	}

	public static bool IsNextUpgradeUnlocked()
	{
		return TerraShrineTier switch
		{
			0 => NPC.downedBoss1,
			1 => NPC.downedBoss2,
			2 => NPC.downedBoss3,
			3 => Main.hardMode,
			4 => NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3,
			5 => NPC.downedPlantBoss,
			6 => NPC.downedGolemBoss,
			7 => NPC.downedAncientCultist,
			8 => NPC.downedMoonlord,
			_ => false
		};
	}

	public static bool TryUpgradeShrine(Player player)
	{
		long cost = GetNextUpgradeCost();
		if (cost <= 0 || !IsNextUpgradeUnlocked() || !player.GetModPlayer<SoulPlayer>().TrySpendSouls(cost))
		{
			return false;
		}

		TerraShrineTier++;
		if (Main.netMode == NetmodeID.Server)
		{
			NetMessage.SendData(MessageID.WorldData);
		}

		return true;
	}

	private readonly record struct SavedBloodstain(Vector2 Position, long Souls, string CharacterId);
}
