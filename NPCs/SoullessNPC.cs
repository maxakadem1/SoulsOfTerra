using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SoulsOfTerra.Systems;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SoulsOfTerra.NPCs;

[AutoloadHead]
public class SoullessNPC : ModNPC
{
	public override string Texture => $"Terraria/Images/NPC_{NPCID.TaxCollector}";
	public override string HeadTexture => $"Terraria/Images/NPC_Head_{NPCHeadID.TaxCollector}";

	public override void SetStaticDefaults()
	{
		Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.TaxCollector];
		NPCID.Sets.ExtraFramesCount[Type] = 9;
		NPCID.Sets.AttackFrameCount[Type] = 4;
		NPCID.Sets.DangerDetectRange[Type] = 500;
		NPCID.Sets.AttackType[Type] = 2;
		NPCID.Sets.AttackTime[Type] = 30;
		NPCID.Sets.AttackAverageChance[Type] = 20;
	}

	public override void SetDefaults()
	{
		NPC.townNPC = true;
		NPC.friendly = true;
		NPC.width = 18;
		NPC.height = 40;
		NPC.aiStyle = NPCAIStyleID.Passive;
		NPC.damage = 10;
		NPC.defense = 12;
		NPC.lifeMax = 250;
		NPC.HitSound = SoundID.NPCHit1;
		NPC.DeathSound = SoundID.NPCDeath1;
		NPC.knockBackResist = 0.5f;
		AnimationType = NPCID.TaxCollector;
	}

	public override bool CanTownNPCSpawn(int numTownNPCs)
	{
		// The first instance is forced near spawn; later instances require housing.
		return SoulWorldSystem.SoullessSpawnedOnce;
	}

	public override List<string> SetNPCNameList()
	{
		return new List<string> { "Soulless" };
	}

	public override string GetChat()
	{
		if (NPC.downedSlimeKing)
		{
			return Language.GetTextValue("Mods.SoulsOfTerra.Dialogue.Soulless.AfterKingSlime");
		}

		return Main.rand.NextBool()
			? Language.GetTextValue("Mods.SoulsOfTerra.Dialogue.Soulless.Introduction")
			: Language.GetTextValue("Mods.SoulsOfTerra.Dialogue.Soulless.BloodstainHint");
	}

	public override void SetChatButtons(ref string button, ref string button2)
	{
		button = Language.GetTextValue("Mods.SoulsOfTerra.UI.Commune");
		button2 = string.Empty;
	}

	public override void OnChatButtonClicked(bool firstButton, ref string shop)
	{
		if (firstButton)
		{
			SoulMenuSystem.OpenSoulless(NPC.whoAmI);
		}
	}

	public override void TownNPCAttackStrength(ref int damage, ref float knockback)
	{
		damage = 18;
		knockback = 3f;
	}

	public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown)
	{
		cooldown = 30;
		randExtraCooldown = 30;
	}

	public override void TownNPCAttackProj(ref int projType, ref int attackDelay)
	{
		projType = ProjectileID.DiamondBolt;
		attackDelay = 1;
	}

	public override void TownNPCAttackProjSpeed(ref float multiplier, ref float gravityCorrection, ref float randomOffset)
	{
		multiplier = 9f;
		randomOffset = 2f;
	}

	public override Color? GetAlpha(Color drawColor)
	{
		return Color.Lerp(drawColor, new Color(120, 105, 145), 0.22f);
	}
}
