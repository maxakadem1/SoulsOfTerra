	using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.NPCs;

public sealed class MutationSlimeCoatingGlobalNPC : GlobalNPC
{
	public override bool InstancePerEntity => true;

	public bool Coated { get; set; }

	public override void ResetEffects(NPC npc)
	{
		Coated = false;
	}

	public override void PostAI(NPC npc)
	{
		if (!Coated || IsBossPart(npc) || npc.justHit)
		{
			return;
		}

		// Horizontal-only damping leaves gravity, jumps, and vertical knockback intact.
		npc.velocity.X *= 0.85f;
	}

	public override void DrawEffects(NPC npc, ref Color drawColor)
	{
		if (!Coated)
		{
			return;
		}

		drawColor = Color.Lerp(drawColor, new Color(90, 190, 145), 0.22f);
		if (Main.rand.NextBool(9))
		{
			Dust dust = Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.t_Slime,
				npc.velocity.X * 0.15f, -0.25f, 130, new Color(90, 215, 155), 0.75f);
			dust.noGravity = true;
		}
	}

	private static bool IsBossPart(NPC npc)
	{
		return npc.boss || npc.realLife >= 0 && npc.realLife < Main.maxNPCs && Main.npc[npc.realLife].boss;
	}
}
