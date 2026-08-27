using System;
using SoulsOfTerra.Content.Items.Materials;
using SoulsOfTerra.Systems;
using Terraria;
using Terraria.ModLoader;

namespace SoulsOfTerra.Common;

public static class SoulEssenceRegistry
{
	public static SoulEssenceDefinition[] Definitions { get; } = new SoulEssenceDefinition[]
	{
		Create<SlimeEssence>("slime", "Slime Essence", 2_500, "A royal, viscous echo condensed into matter.",
			() => NPC.downedSlimeKing, "King Slime", 0),
		Create<EyeEssence>("eye", "Eye Essence", 5_000, "A watchful crimson echo bound into matter.",
			() => NPC.downedBoss1, "Eye of Cthulhu", 1),
		Create<DeerclopsEssence>("deerclops", "Deerclops Essence", 5_000, "A frozen echo of hunger and fury.",
			() => NPC.downedDeerclops, "Deerclops", 1),
		Create<EaterOfWorldsEssence>("eater", "Eater of Worlds Essence", 10_000, "A devouring corruption bound into matter.",
			() => NPC.downedBoss2, "Eater of Worlds", 2, () => !WorldGen.crimson, "Requires a Corruption world"),
		Create<BrainOfCthulhuEssence>("brain", "Brain of Cthulhu Essence", 10_000, "A crimson consciousness bound into matter.",
			() => NPC.downedBoss2, "Brain of Cthulhu", 2, () => WorldGen.crimson, "Requires a Crimson world"),
		Create<QueenBeeEssence>("queenBee", "Queen Bee Essence", 10_000, "A furious sovereign's honeyed echo.",
			() => NPC.downedQueenBee, "Queen Bee", 2),
		Create<SkeletronEssence>("skeletron", "Skeletron Essence", 15_000, "A dungeon curse condensed into matter.",
			() => NPC.downedBoss3, "Skeletron", 3),
		Create<CongregationEssence>("congregation", "Congregation Essence", 20_000, "A chorus of imprisoned souls condensed into one echo.",
			() => BuriedCourtSystem.DownedSealedCongregation, "The Sealed Congregation", 3),
		Create<WallOfFleshEssence>("wall", "Wall of Flesh Essence", 25_000, "An infernal prison's ravenous echo condensed into matter.",
			() => Main.hardMode, "Wall of Flesh", 4),
		Create<QueenSlimeEssence>("queenSlime", "Queen Slime Essence", 25_000, "A crystalline royal echo bound into matter.",
			() => NPC.downedQueenSlime, "Queen Slime", 4),
		Create<DestroyerEssence>("destroyer", "Destroyer Essence", 60_000, "A mechanical serpent's relentless echo.",
			() => NPC.downedMechBoss1, "The Destroyer", 5),
		Create<TwinsEssence>("twins", "Twins Essence", 60_000, "Two murderous gazes fused into one echo.",
			() => NPC.downedMechBoss2, "The Twins", 5),
		Create<SkeletronPrimeEssence>("prime", "Skeletron Prime Essence", 60_000, "A mechanized curse condensed into matter.",
			() => NPC.downedMechBoss3, "Skeletron Prime", 5),
		Create<PlanteraEssence>("plantera", "Plantera Essence", 100_000, "The jungle's wrath condensed into matter.",
			() => NPC.downedPlantBoss, "Plantera", 6),
		Create<GolemEssence>("golem", "Golem Essence", 150_000, "An ancient temple guardian's echo.",
			() => NPC.downedGolemBoss, "Golem", 7),
		Create<DukeFishronEssence>("fishron", "Duke Fishron Essence", 150_000, "A tempestuous mutant echo bound into matter.",
			() => NPC.downedFishron, "Duke Fishron", 7),
		Create<EmpressOfLightEssence>("empress", "Empress of Light Essence", 150_000, "A prismatic sovereign's echo condensed into matter.",
			() => NPC.downedEmpressOfLight, "Empress of Light", 7),
		Create<LunaticCultistEssence>("cultist", "Lunatic Cultist Essence", 225_000, "A forbidden ritual's echo bound into matter.",
			() => NPC.downedAncientCultist, "Lunatic Cultist", 8),
		Create<MoonLordEssence>("moonLord", "Moon Lord Essence", 100_000, "A celestial sovereign's echo condensed into matter.",
			() => NPC.downedMoonlord, "Moon Lord", 9)
	};

	public static bool TryGet(int index, out SoulEssenceDefinition definition)
	{
		definition = index >= 0 && index < Definitions.Length ? Definitions[index] : null;
		return definition is not null;
	}

	public static bool TryFindByItemType(int itemType, out SoulEssenceDefinition definition)
	{
		foreach (SoulEssenceDefinition candidate in Definitions)
		{
			if (candidate.ItemType == itemType)
			{
				definition = candidate;
				return true;
			}
		}

		definition = null;
		return false;
	}

	private static SoulEssenceDefinition Create<T>(string id, string name, long cost, string description,
		Func<bool> bossDefeated, string bossName, int shrineTier, Func<bool> isAvailable = null,
		string unavailableRequirement = null) where T : ModItem
	{
		return new SoulEssenceDefinition(id, ModContent.ItemType<T>(), name, cost, description, bossDefeated,
			bossName, shrineTier, isAvailable, unavailableRequirement);
	}
}

public sealed class SoulEssenceDefinition
{
	private readonly Func<bool> bossDefeated;
	private readonly Func<bool> isAvailable;
	private readonly string bossName;
	private readonly string unavailableRequirement;

	public string Id { get; }
	public int ItemType { get; }
	public string Name { get; }
	public long Cost { get; }
	public string Description { get; }
	public int ShrineTier { get; }

	public SoulEssenceDefinition(string id, int itemType, string name, long cost, string description,
		Func<bool> bossDefeated, string bossName, int shrineTier, Func<bool> isAvailable,
		string unavailableRequirement)
	{
		Id = id;
		ItemType = itemType;
		Name = name;
		Cost = cost;
		Description = description;
		this.bossDefeated = bossDefeated;
		this.bossName = bossName;
		ShrineTier = shrineTier;
		this.isAvailable = isAvailable;
		this.unavailableRequirement = unavailableRequirement;
	}

	public bool IsUnlocked()
	{
		return IsDiscovered() && SoulWorldSystem.TerraShrineTier >= ShrineTier;
	}

	public bool IsDiscovered() => (isAvailable is null || isAvailable()) && bossDefeated();

	public string GetRequirement()
	{
		if (isAvailable is not null && !isAvailable())
		{
			return unavailableRequirement;
		}

		if (!bossDefeated())
		{
			return $"Requires {bossName}";
		}

		return SoulWorldSystem.TerraShrineTier < ShrineTier
			? $"Requires Terra Shrine tier {ShrineTier}"
			: string.Empty;
	}
}
