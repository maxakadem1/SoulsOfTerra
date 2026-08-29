using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using SoulsOfTerra.Content.Projectiles;
using SoulsOfTerra.Content.Tiles;
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
	private static readonly long[] TemperCosts =
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
	public static bool TerraBladeFragmentPurchased { get; private set; }
	public static bool HasActiveTerraforge { get; private set; }
	public static Point16 ActiveTerraforgePosition { get; private set; }
	public static int TerraforgeTemper { get; private set; }

	public override void OnWorldLoad()
	{
		pendingBloodstains.Clear();
		SoullessSpawnedOnce = false;
		TerraBladeFragmentPurchased = false;
		HasActiveTerraforge = false;
		ActiveTerraforgePosition = new Point16(-1, -1);
		TerraforgeTemper = 0;
		soullessSpawnTimer = 0;
	}

	public override void OnWorldUnload()
	{
		pendingBloodstains.Clear();
		SoullessSpawnedOnce = false;
		TerraBladeFragmentPurchased = false;
		HasActiveTerraforge = false;
		ActiveTerraforgePosition = new Point16(-1, -1);
		TerraforgeTemper = 0;
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

		if (TerraBladeFragmentPurchased)
		{
			tag["terraBladeFragmentPurchased"] = true;
		}

		if (HasActiveTerraforge)
		{
			tag["activeTerraforgeX"] = ActiveTerraforgePosition.X;
			tag["activeTerraforgeY"] = ActiveTerraforgePosition.Y;
		}

		if (TerraforgeTemper > 0)
		{
			tag["terraforgeTemper"] = TerraforgeTemper;
		}

	}

	public override void LoadWorldData(TagCompound tag)
	{
		pendingBloodstains.Clear();
		SoullessSpawnedOnce = tag.GetBool("soullessSpawned");
		TerraBladeFragmentPurchased = tag.GetBool("terraBladeFragmentPurchased");
		HasActiveTerraforge = tag.ContainsKey("activeTerraforgeX") && tag.ContainsKey("activeTerraforgeY");
		ActiveTerraforgePosition = HasActiveTerraforge
			? new Point16(tag.GetShort("activeTerraforgeX"), tag.GetShort("activeTerraforgeY"))
			: new Point16(-1, -1);
		TerraforgeTemper = System.Math.Clamp(tag.GetInt("terraforgeTemper"), 0, TemperCosts.Length);
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
		ValidateActiveTerraforge();
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
		writer.Write(TerraBladeFragmentPurchased);
		writer.Write(HasActiveTerraforge);
		writer.Write(ActiveTerraforgePosition.X);
		writer.Write(ActiveTerraforgePosition.Y);
		writer.Write((byte)TerraforgeTemper);
	}

	public override void NetReceive(BinaryReader reader)
	{
		SoullessSpawnedOnce = reader.ReadBoolean();
		TerraBladeFragmentPurchased = reader.ReadBoolean();
		HasActiveTerraforge = reader.ReadBoolean();
		ActiveTerraforgePosition = new Point16(reader.ReadInt16(), reader.ReadInt16());
		TerraforgeTemper = reader.ReadByte();
	}

	public static long GetNextTemperCost()
	{
		return TerraforgeTemper >= TemperCosts.Length ? 0 : TemperCosts[TerraforgeTemper];
	}

	public static bool IsNextTemperUnlocked()
	{
		return TerraforgeTemper switch
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

	public static bool TryTemperTerraforge(Player player)
	{
		long cost = GetNextTemperCost();
		if (!TerraBladeFragmentPurchased || cost <= 0 || !IsNextTemperUnlocked()
			|| !player.GetModPlayer<SoulPlayer>().TrySpendSouls(cost))
		{
			return false;
		}

		TerraforgeTemper++;
		if (Main.netMode == NetmodeID.Server)
		{
			NetMessage.SendData(MessageID.WorldData);
		}

		return true;
	}

	public static bool TryPurchaseTerraBladeFragment(Player player, long cost)
	{
		if (TerraBladeFragmentPurchased || !player.GetModPlayer<SoulPlayer>().TrySpendSouls(cost))
		{
			return false;
		}

		TerraBladeFragmentPurchased = true;
		SyncWorldData();
		return true;
	}

	public static bool TryActivateTerraforge(Point16 position)
	{
		if (HasActiveTerraforge)
		{
			return false;
		}

		TerraBladeFragmentPurchased = true;
		HasActiveTerraforge = true;
		ActiveTerraforgePosition = position;
		SyncWorldData();
		return true;
	}

	public static void ClearActiveTerraforge(Point16 position)
	{
		if (!HasActiveTerraforge || position != ActiveTerraforgePosition)
		{
			return;
		}

		HasActiveTerraforge = false;
		ActiveTerraforgePosition = new Point16(-1, -1);
		SyncWorldData();
	}

	private static void ValidateActiveTerraforge()
	{
		if (!HasActiveTerraforge)
		{
			return;
		}

		Tile tile = Framing.GetTileSafely(ActiveTerraforgePosition.X, ActiveTerraforgePosition.Y);
		if (!tile.HasTile || tile.TileType != ModContent.TileType<TerraforgeTile>())
		{
			HasActiveTerraforge = false;
			ActiveTerraforgePosition = new Point16(-1, -1);
		}
	}

	private static void SyncWorldData()
	{
		if (Main.netMode == NetmodeID.Server)
		{
			NetMessage.SendData(MessageID.WorldData);
		}
	}

	private readonly record struct SavedBloodstain(Vector2 Position, long Souls, string CharacterId);
}
