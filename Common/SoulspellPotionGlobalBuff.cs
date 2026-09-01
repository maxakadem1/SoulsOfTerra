using SoulsOfTerra.Players;
using Terraria;
using Terraria.ModLoader;

namespace SoulsOfTerra.Common;

public sealed class SoulspellPotionGlobalBuff : GlobalBuff
{
	public override bool RightClick(int type, int buffIndex)
	{
		SoulSpellPlayer spellPlayer = Main.LocalPlayer.GetModPlayer<SoulSpellPlayer>();
		if (!spellPlayer.StanceOn)
		{
			return true;
		}

		foreach (SoulSpellDefinition spell in SoulSpellRegistry.PotionSpells)
		{
			if (spell.BuffType == type && spellPlayer.HasLearned(spell.Id)
				&& SoulSpellRegistry.IsSelected(spellPlayer.SelectionMask, spell.Id))
			{
				// Canceling a sustained vanilla buff also removes its soulspell from Stance.
				spellPlayer.RequestSelection(spell.Id, false);
				break;
			}
		}

		return true;
	}
}
