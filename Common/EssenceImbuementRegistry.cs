using System;
using SoulsOfTerra.Content.Items.Materials;
using SoulsOfTerra.Content.Items.Weapons.Magic;
using SoulsOfTerra.Content.Items.Weapons.Melee;
using SoulsOfTerra.Content.Items.Weapons.Summon;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Common;

public static class EssenceImbuementRegistry
{
	private static readonly int[] MetalBroadswords =
	{
		ItemID.CopperBroadsword,
		ItemID.TinBroadsword,
		ItemID.IronBroadsword,
		ItemID.LeadBroadsword,
		ItemID.SilverBroadsword,
		ItemID.TungstenBroadsword,
		ItemID.GoldBroadsword,
		ItemID.PlatinumBroadsword
	};

	public static EssenceImbuementDefinition[] Definitions { get; } =
	{
		new(
			"slimeboundBlade",
			MetalBroadswords,
			"Any Metal Broadsword",
			ModContent.ItemType<SlimeEssence>(),
			ModContent.ItemType<SlimeboundBlade>(),
			"Slimebound Blade"),
		new(
			"servantsGaze",
			new int[] { ItemID.RubyStaff },
			"Ruby Staff",
			ModContent.ItemType<EyeEssence>(),
			ModContent.ItemType<ServantsGaze>(),
			"Servant's Gaze"),
		new(
			"breakerBlade",
			new int[] { ItemID.BreakerBlade },
			"Breaker Blade",
			ModContent.ItemType<WallOfFleshEssence>(),
			ModContent.ItemType<EssenceboundBreakerBlade>(),
			"Essencebound Breaker Blade"),
		new(
			"compeditus",
			new int[] { ItemID.ImpStaff },
			"Imp Staff",
			ModContent.ItemType<CongregationEssence>(),
			ModContent.ItemType<Compeditus>(),
			"Compeditus"),
		new(
			"moonstoneStaff",
			new int[] { ItemID.DiamondStaff },
			"Diamond Staff",
			ModContent.ItemType<MoonLordEssence>(),
			ModContent.ItemType<MoonstoneStaff>(),
			"Moonstone Staff")
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
			if (candidate.AcceptsInput(inputItemType) && candidate.EssenceItemType == essenceItemType)
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

	public static bool IsRegisteredOutput(int itemType)
	{
		foreach (EssenceImbuementDefinition definition in Definitions)
		{
			if (definition.OutputItemType == itemType)
			{
				return true;
			}
		}

		return false;
	}
}

public sealed record EssenceImbuementDefinition(
	string Id,
	int[] InputItemTypes,
	string InputDisplayName,
	int EssenceItemType,
	int OutputItemType,
	string OutputName)
{
	public int PreviewInputItemType => InputItemTypes.Length > 0 ? InputItemTypes[0] : ItemID.None;

	public bool AcceptsInput(int itemType) => Array.IndexOf(InputItemTypes, itemType) >= 0;
}
