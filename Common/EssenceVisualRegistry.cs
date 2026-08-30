using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SoulsOfTerra.Content.Items.Materials;
using Terraria.ID;

namespace SoulsOfTerra.Common;

internal enum EssenceComposition
{
	Single,
	Split,
	CoreAndSatellites
}

internal sealed record EssenceVisualSource(int NpcType = NPCID.None, string TexturePath = null,
	Vector4? NormalizedCrop = null);

internal sealed record EssenceVisualDefinition(string Id, string OutputName, EssenceVisualSource PrimarySource,
	EssenceComposition Composition, int Seed, int SatelliteCount = 0,
	IReadOnlyList<EssenceVisualSource> SecondarySources = null, Color? AccentOverride = null);

internal static class EssenceVisualRegistry
{
	// Boss heads are the default source because they are already authored for recognition at small sizes.
	public static IReadOnlyList<EssenceVisualDefinition> Definitions { get; } = new EssenceVisualDefinition[]
	{
		Single("slime", nameof(SlimeEssence), NPCID.KingSlime, 1103),
		Single("eye", nameof(EyeEssence), NPCID.EyeofCthulhu, 1217),
		Single("deerclops", nameof(DeerclopsEssence), NPCID.Deerclops, 1321),
		Core("eater", nameof(EaterOfWorldsEssence), NPCID.EaterofWorldsHead, 1409, 6,
			new Color(117, 65, 164)),
		Single("brain", nameof(BrainOfCthulhuEssence), NPCID.BrainofCthulhu, 1511),
		Single("queenBee", nameof(QueenBeeEssence), NPCID.QueenBee, 1601),
		Core("skeletron", nameof(SkeletronEssence), NPCID.SkeletronHead, 1723, 2),
		new EssenceVisualDefinition("congregation", nameof(CongregationEssence),
			new EssenceVisualSource(TexturePath:
				"SoulsOfTerra/Content/Bosses/SealedCongregation/SealedCongregation_seal"),
			EssenceComposition.CoreAndSatellites, 1831, 4),
		Single("wall", nameof(WallOfFleshEssence), NPCID.WallofFlesh, 1907),
		Single("queenSlime", nameof(QueenSlimeEssence), NPCID.QueenSlimeBoss, 2011),
		Core("destroyer", nameof(DestroyerEssence), NPCID.TheDestroyer, 2111, 6),
		new EssenceVisualDefinition("twins", nameof(TwinsEssence),
			new EssenceVisualSource(NPCID.Retinazer), EssenceComposition.Split, 2221,
			SecondarySources: new[] { new EssenceVisualSource(NPCID.Spazmatism) }),
		Core("prime", nameof(SkeletronPrimeEssence), NPCID.SkeletronPrime, 2311, 4),
		Core("plantera", nameof(PlanteraEssence), NPCID.Plantera, 2417, 6),
		Core("golem", nameof(GolemEssence), NPCID.GolemHead, 2521, 2),
		Single("fishron", nameof(DukeFishronEssence), NPCID.DukeFishron, 2617),
		Single("empress", nameof(EmpressOfLightEssence), NPCID.HallowBoss, 2711),
		Single("cultist", nameof(LunaticCultistEssence), NPCID.CultistBoss, 2801),
		Core("moonLord", nameof(MoonLordEssence), NPCID.MoonLordHead, 2903, 3)
	};

	private static EssenceVisualDefinition Single(string id, string outputName, int npcType, int seed)
	{
		return new EssenceVisualDefinition(id, outputName, new EssenceVisualSource(npcType),
			EssenceComposition.Single, seed);
	}

	private static EssenceVisualDefinition Core(string id, string outputName, int npcType, int seed,
		int satelliteCount, Color? accentOverride = null)
	{
		return new EssenceVisualDefinition(id, outputName, new EssenceVisualSource(npcType),
			EssenceComposition.CoreAndSatellites, seed, satelliteCount, AccentOverride: accentOverride);
	}
}
