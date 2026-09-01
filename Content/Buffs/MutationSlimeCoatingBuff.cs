using SoulsOfTerra.NPCs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Buffs;

public sealed class MutationSlimeCoatingBuff : ModBuff
{
	public override string Texture => $"Terraria/Images/Buff_{BuffID.Slimed}";

	public override void SetStaticDefaults()
	{
		Main.debuff[Type] = true;
		Main.pvpBuff[Type] = true;
	}

	public override void Update(NPC npc, ref int buffIndex)
	{
		npc.GetGlobalNPC<MutationSlimeCoatingGlobalNPC>().Coated = true;
		int defenseReduction = System.Math.Min(15, 4 + npc.defDefense / 10);
		npc.defense = System.Math.Max(0, npc.defense - defenseReduction);
	}
}
