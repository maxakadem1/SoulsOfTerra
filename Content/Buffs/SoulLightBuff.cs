using Microsoft.Xna.Framework;
using SoulsOfTerra.Common;
using SoulsOfTerra.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Buffs;

public class SoulLightBuff : ModBuff
{
	public override string Texture => $"Terraria/Images/Buff_{BuffID.Shine}";

	public override void SetStaticDefaults()
	{
		Main.buffNoSave[Type] = true;
		Main.buffNoTimeDisplay[Type] = true;
	}

	public override void Update(Player player, ref int buffIndex)
	{
		if (!player.GetModPlayer<SoulSpellPlayer>().LightActive)
		{
			player.DelBuff(buffIndex);
			buffIndex--;
			return;
		}

		player.buffTime[buffIndex] = 2;
		Lighting.AddLight(player.Center, 0.45f, 0.95f, 0.88f);
	}

	public override bool RightClick(int buffIndex)
	{
		Main.LocalPlayer.GetModPlayer<SoulSpellPlayer>().RequestSelection(SoulSpellId.Light, false);
		return true;
	}
}
