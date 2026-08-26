using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Bosses.SealedCongregation;

public class SealedCongregationBossBar : ModBossBar
{
	public override Asset<Texture2D> GetIconTexture(ref Rectangle? iconFrame)
	{
		return ModContent.Request<Texture2D>("SoulsOfTerra/Content/Items/Materials/MoonLordEssence");
	}

	public override bool? ModifyInfo(ref BigProgressBarInfo info, ref float life, ref float lifeMax,
		ref float shield, ref float shieldMax)
	{
		if (info.npcIndexToAimAt < 0 || info.npcIndexToAimAt >= Main.maxNPCs)
		{
			return false;
		}

		NPC boss = Main.npc[info.npcIndexToAimAt];
		if (!boss.active || boss.ModNPC is not SealedCongregationBoss congregation)
		{
			return false;
		}

		life = boss.life;
		lifeMax = congregation.CombinedLifeMax;
		shield = 0f;
		shieldMax = 0f;
		foreach (NPC npc in Main.ActiveNPCs)
		{
			if (npc.type == ModContent.NPCType<SealedCongregationSeal>() && (int)npc.ai[0] == boss.whoAmI)
			{
				life += npc.life;
			}
		}

		return true;
	}
}
