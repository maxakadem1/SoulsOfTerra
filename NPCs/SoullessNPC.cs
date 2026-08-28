using System;
using System.Collections.Generic;
using SoulsOfTerra.Systems;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SoulsOfTerra.NPCs;

[AutoloadHead]
public class SoullessNPC : ModNPC
{
	private const int WalkFrameCount = 5;
	private const int WalkFrameTicks = 8;

	public override string Texture => "SoulsOfTerra/NPCs/Soulless";

	public override void SetStaticDefaults()
	{
		Main.npcFrameCount[Type] = WalkFrameCount;
		NPCID.Sets.ExtraFramesCount[Type] = 0;
		NPCID.Sets.AttackFrameCount[Type] = 0;
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
		// Frame bottoms already sit on the town hitbox; a negative offset lifts the sprite off the floor.
		DrawOffsetY = -3;
	}

	public override void PostAI()
	{
		// NPC sheets face left; the engine flips them when spriteDirection is 1 (walking right).
		NPC.spriteDirection = NPC.direction;
	}

	public override void FindFrame(int frameHeight)
	{
		NPC.spriteDirection = NPC.direction;

		if (NPC.IsABestiaryIconDummy || Math.Abs(NPC.velocity.X) > 0.1f)
		{
			NPC.frameCounter += NPC.IsABestiaryIconDummy ? 1.0 : Math.Abs(NPC.velocity.X) * 1.6;
			if (NPC.frameCounter >= WalkFrameTicks)
			{
				NPC.frameCounter = 0.0;
				NPC.frame.Y += frameHeight;
				if (NPC.frame.Y >= WalkFrameCount * frameHeight)
				{
					NPC.frame.Y = 0;
				}
			}

			return;
		}

		NPC.frameCounter = 0.0;
		NPC.frame.Y = 0;
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
		if (NPC.downedBoss3 && !BuriedCourtSystem.DownedSealedCongregation)
		{
			return Language.GetTextValue("Mods.SoulsOfTerra.Dialogue.Soulless.BuriedCourtHint");
		}

		if (BuriedCourtSystem.DownedSealedCongregation)
		{
			return Language.GetTextValue("Mods.SoulsOfTerra.Dialogue.Soulless.AfterCongregation");
		}

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
}
