using SoulsOfTerra.Common;
using SoulsOfTerra.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Buffs;

public class SoulDashBuff : ModBuff
{
	public override string Texture => $"Terraria/Images/Buff_{BuffID.Swiftness}";

	public override void SetStaticDefaults()
	{
		Main.buffNoSave[Type] = true;
		Main.buffNoTimeDisplay[Type] = true;
	}

	public override void Update(Player player, ref int buffIndex)
	{
		if (player.GetModPlayer<SoulSpellPlayer>().DashEnabled)
		{
			player.buffTime[buffIndex] = 2;
			return;
		}

		player.DelBuff(buffIndex);
		buffIndex--;
	}

	public override bool RightClick(int buffIndex)
	{
		Main.LocalPlayer.GetModPlayer<SoulSpellPlayer>().RequestSelection(SoulSpellId.Dash, false);
		return true;
	}
}
