using System;
using SoulsOfTerra.Common;
using SoulsOfTerra.Config;
using SoulsOfTerra.Content.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.NPCs;

public class SoulRewardGlobalNPC : GlobalNPC
{
	private const double CopperPerSoul = 6d;

	public override void OnKill(NPC npc)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient || !CanDropSouls(npc))
		{
			return;
		}

		long reward = CalculateReward(npc);
		if (reward > 0)
		{
			SoulOrbProjectile.Spawn(npc.GetSource_Loot(), npc.Center, reward);
		}
	}

	public static bool CanDropSouls(NPC npc)
	{
		if (npc.friendly || npc.townNPC || npc.dontTakeDamage || npc.SpawnedFromStatue || npc.type == NPCID.TargetDummy)
		{
			return false;
		}

		// Multi-part enemies pay once from their controlling root.
		if (npc.realLife >= 0)
		{
			return false;
		}

		return npc.value > 0f || npc.boss;
	}

	public static long CalculateReward(NPC npc)
	{
		double multiplier = ModContent.GetInstance<SoulServerConfig>().SoulRewardMultiplier;
		if (multiplier <= 0d)
		{
			return 0;
		}

		double reward = npc.value > 0f
			? npc.value / CopperPerSoul
			: CalculateBossFallback(npc);

		return SoulMath.CeilingToLong(reward * multiplier);
	}

	private static double CalculateBossFallback(NPC npc)
	{
		double effectiveHealth = Math.Max(1, npc.lifeMax) * (1d + Math.Max(0, npc.defense) / 100d);
		double threat = Math.Sqrt(effectiveHealth * Math.Max(1, npc.damage)) * 5d;
		return Math.Max(1000d, threat);
	}
}
