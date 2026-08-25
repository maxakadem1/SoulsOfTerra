using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.NPCs;

public class FleshRuptureGlobalNPC : GlobalNPC
{
	private const int RuptureDuration = 4 * 60;
	private readonly byte[] stacks = new byte[Main.maxPlayers];
	private readonly short[] timers = new short[Main.maxPlayers];

	public override bool InstancePerEntity => true;

	public override void SetDefaults(NPC entity)
	{
		Array.Clear(stacks);
		Array.Clear(timers);
	}

	public bool AddStacks(int playerIndex, int amount)
	{
		if (playerIndex < 0 || playerIndex >= Main.maxPlayers || amount <= 0)
		{
			return false;
		}

		stacks[playerIndex] = (byte)System.Math.Min(3, stacks[playerIndex] + amount);
		timers[playerIndex] = RuptureDuration;
		if (stacks[playerIndex] < 3)
		{
			return false;
		}

		stacks[playerIndex] = 0;
		timers[playerIndex] = 0;
		return true;
	}

	public override void PostAI(NPC npc)
	{
		for (int playerIndex = 0; playerIndex < timers.Length; playerIndex++)
		{
			if (timers[playerIndex] > 0 && --timers[playerIndex] == 0)
			{
				stacks[playerIndex] = 0;
			}
		}

		if (Main.netMode == NetmodeID.Server || Main.myPlayer < 0 || Main.myPlayer >= Main.maxPlayers
			|| stacks[Main.myPlayer] == 0 || !Main.rand.NextBool(12))
		{
			return;
		}

		// Sparse blood motes keep the active wound readable without covering the enemy.
		Dust dust = Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.Blood,
			Scale: 0.65f + stacks[Main.myPlayer] * 0.1f);
		dust.noGravity = true;
		dust.velocity *= 0.25f;
	}
}
