using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SoulsOfTerra.Content.Projectiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SoulsOfTerra.Systems;

public class SoulWorldSystem : ModSystem
{
	private readonly List<SavedBloodstain> pendingBloodstains = new();

	public override void OnWorldLoad()
	{
		pendingBloodstains.Clear();
	}

	public override void OnWorldUnload()
	{
		pendingBloodstains.Clear();
	}

	public override void SaveWorldData(TagCompound tag)
	{
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
	}

	public override void LoadWorldData(TagCompound tag)
	{
		pendingBloodstains.Clear();
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

	private readonly record struct SavedBloodstain(Vector2 Position, long Souls, string CharacterId);
}
