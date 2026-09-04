using SoulsOfTerra.Content.Items.Materials;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SoulsOfTerra.Common;

public enum MutationId : byte
{
	Slime = 0,
	Eye,
	Deerclops,
	EaterOfWorlds,
	BrainOfCthulhu,
	QueenBee,
	Skeletron,
	Congregation,
	WallOfFlesh,
	QueenSlime,
	Destroyer,
	Twins,
	SkeletronPrime,
	Plantera,
	Golem,
	DukeFishron,
	EmpressOfLight,
	LunaticCultist,
	MoonLord,
	None = byte.MaxValue
}

public sealed record MutationDefinition(MutationId Id, int EssenceItemType, bool Implemented,
	string InventorySummary, string DetailedDescription)
{
	// Mutation names are separate from item names so the UI can name the embedded effect cleanly.
	public string DisplayName => Language.GetTextValue($"Mods.SoulsOfTerra.UI.MutationNames.{Id}");
}

public static class MutationRegistry
{
	public const string UndiscoveredSummary = "Player Mutation: No graft has been discovered yet";

	public static MutationDefinition[] Definitions { get; } =
	{
		// Stable registry order doubles as the compact character-save and network ID.
		Create<SlimeEssence>(MutationId.Slime, true,
			"Player Mutation: Allied slimes and a weakening spray; you move 10% slower",
			"Every 5 seconds, buds an allied slime for 15 seconds (maximum 3).\n" +
			"Being hurt sprays 6 sticky globules with a 1-second cooldown.\n" +
			"Slime coating lasts 5 seconds, reduces defense by 4 + 10% (maximum 15),\n" +
			"and slows non-boss enemies by 15%.\n" +
			"Drawback: 10% reduced movement speed. Each slime consumes a minion slot."),
		Create<EyeEssence>(MutationId.Eye),
		Create<DeerclopsEssence>(MutationId.Deerclops),
		Create<EaterOfWorldsEssence>(MutationId.EaterOfWorlds),
		Create<BrainOfCthulhuEssence>(MutationId.BrainOfCthulhu),
		Create<QueenBeeEssence>(MutationId.QueenBee),
		Create<SkeletronEssence>(MutationId.Skeletron, true,
			"Player Mutation: Grafted hands slap and drag; you take more knockback and lose defense",
			"Two Skeletron hands hang from your shoulders.\n" +
			"They alternately lunge at enemies within 32 tiles.\n" +
			"Slaps deal 25 + 15% of maximum life as generic damage,\n" +
			"cleave along the hand's path, and pass through tiles.\n" +
			"Struck enemies are hauled toward you unless they resist knockback.\n" +
			"Drawback: -8 defense and 40% more knockback. Each hand consumes a minion slot."),
		Create<CongregationEssence>(MutationId.Congregation),
		Create<WallOfFleshEssence>(MutationId.WallOfFlesh),
		Create<QueenSlimeEssence>(MutationId.QueenSlime),
		Create<DestroyerEssence>(MutationId.Destroyer),
		Create<TwinsEssence>(MutationId.Twins),
		Create<SkeletronPrimeEssence>(MutationId.SkeletronPrime),
		Create<PlanteraEssence>(MutationId.Plantera),
		Create<GolemEssence>(MutationId.Golem),
		Create<DukeFishronEssence>(MutationId.DukeFishron),
		Create<EmpressOfLightEssence>(MutationId.EmpressOfLight),
		Create<LunaticCultistEssence>(MutationId.LunaticCultist),
		Create<MoonLordEssence>(MutationId.MoonLord)
	};

	public static bool TryGet(MutationId id, out MutationDefinition definition)
	{
		int index = (int)id;
		definition = index >= 0 && index < Definitions.Length ? Definitions[index] : null;
		return definition is not null;
	}

	public static bool TryFindByItemType(int itemType, out MutationDefinition definition)
	{
		foreach (MutationDefinition candidate in Definitions)
		{
			if (candidate.EssenceItemType == itemType)
			{
				definition = candidate;
				return true;
			}
		}

		definition = null;
		return false;
	}

	public static string GetInventorySummary(int itemType)
	{
		return TryFindByItemType(itemType, out MutationDefinition definition) && definition.Implemented
			? definition.InventorySummary
			: UndiscoveredSummary;
	}

	private static MutationDefinition Create<T>(MutationId id, bool implemented = false,
		string inventorySummary = UndiscoveredSummary, string detailedDescription = UndiscoveredSummary)
		where T : ModItem
	{
		return new MutationDefinition(id, ModContent.ItemType<T>(), implemented, inventorySummary,
			detailedDescription);
	}
}
