using System.Collections.Generic;
using SoulsOfTerra.Systems;
using Terraria.ModLoader;

namespace SoulsOfTerra.NPCs;

public sealed class BuriedCourtSpawnGlobalNPC : GlobalNPC
{
	public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
	{
		if (BuriedCourtSystem.IsInsideCourt(spawnInfo.Player.Center))
		{
			// The sealed interior stays quiet outside its authored encounters.
			pool.Clear();
		}
	}
}
