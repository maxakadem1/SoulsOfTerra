using SoulsOfTerra.Content.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Buffs;

public class CompeditusBuff : ModBuff
{
	public override string Texture => $"Terraria/Images/Buff_{BuffID.ImpMinion}";

	public override void SetStaticDefaults()
	{
		Main.buffNoSave[Type] = true;
		Main.buffNoTimeDisplay[Type] = true;
	}

	public override void Update(Player player, ref int buffIndex)
	{
		if (CompeditusCoreProjectile.CountOwnedSeals(player.whoAmI) > 0)
		{
			player.buffTime[buffIndex] = 18_000;
			return;
		}

		player.DelBuff(buffIndex);
		buffIndex--;
	}
}
