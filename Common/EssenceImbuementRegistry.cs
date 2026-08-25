using SoulsOfTerra.Content.Items.Materials;
using SoulsOfTerra.Content.Items.Weapons.Melee;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Common;

public static class EssenceImbuementRegistry
{
	public static EssenceImbuementDefinition[] Definitions { get; } = new EssenceImbuementDefinition[]
	{
		new(
			"breakerBlade",
			ItemID.BreakerBlade,
			ModContent.ItemType<WallOfFleshEssence>(),
			ModContent.ItemType<EssenceboundBreakerBlade>(),
			"Essencebound Breaker Blade")
	};

	public static bool TryGet(int index, out EssenceImbuementDefinition definition)
	{
		definition = index >= 0 && index < Definitions.Length ? Definitions[index] : null;
		return definition is not null;
	}

	public static bool TryFind(int inputItemType, int essenceItemType, out int index, out EssenceImbuementDefinition definition)
	{
		for (int candidateIndex = 0; candidateIndex < Definitions.Length; candidateIndex++)
		{
			EssenceImbuementDefinition candidate = Definitions[candidateIndex];
			if (candidate.InputItemType == inputItemType && candidate.EssenceItemType == essenceItemType)
			{
				index = candidateIndex;
				definition = candidate;
				return true;
			}
		}

		index = -1;
		definition = null;
		return false;
	}
}

public sealed record EssenceImbuementDefinition(
	string Id,
	int InputItemType,
	int EssenceItemType,
	int OutputItemType,
	string OutputName);
