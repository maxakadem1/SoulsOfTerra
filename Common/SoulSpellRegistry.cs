using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.Localization;

namespace SoulsOfTerra.Common;

public enum SoulSpellId : byte
{
	Dash = 0,
	Light = 1,
	Flight = 2
}

public enum SoulSpellCategory : byte
{
	Exploration = 0,
	Combat = 1
}

public readonly record struct SoulSpellDefinition(
	SoulSpellId Id,
	SoulSpellCategory Category,
	bool IsFree,
	int TicksPerSoul,
	string NameKey,
	string DescriptionKey)
{
	public string Name => Language.GetTextValue(NameKey);

	public string Description => Language.GetTextValue(DescriptionKey);

	public string CostText
	{
		get
		{
			if (IsFree || TicksPerSoul <= 0)
			{
				return Language.GetTextValue("Mods.SoulsOfTerra.UI.SoulspellFree");
			}

			int seconds = Math.Max(1, TicksPerSoul / 60);
			return Language.GetTextValue("Mods.SoulsOfTerra.UI.SoulspellCostInterval", seconds);
		}
	}
}

public static class SoulSpellRegistry
{
	public static readonly SoulSpellDefinition Dash = new(
		SoulSpellId.Dash,
		SoulSpellCategory.Exploration,
		true,
		0,
		"Mods.SoulsOfTerra.UI.SoulspellDashName",
		"Mods.SoulsOfTerra.UI.SoulspellDashDescription");

	public static readonly SoulSpellDefinition Light = new(
		SoulSpellId.Light,
		SoulSpellCategory.Exploration,
		false,
		5 * 60,
		"Mods.SoulsOfTerra.UI.SoulspellLightName",
		"Mods.SoulsOfTerra.UI.SoulspellLightDescription");

	public static readonly SoulSpellDefinition Flight = new(
		SoulSpellId.Flight,
		SoulSpellCategory.Exploration,
		true,
		0,
		"Mods.SoulsOfTerra.UI.SoulspellFlightName",
		"Mods.SoulsOfTerra.UI.SoulspellFlightDescription");

	public static readonly SoulSpellDefinition[] All = { Dash, Flight, Light };

	public const uint DefaultSelectionMask = (1u << (int)SoulSpellId.Dash) | (1u << (int)SoulSpellId.Light);

	public static SoulSpellDefinition Get(SoulSpellId id)
	{
		return id switch
		{
			SoulSpellId.Dash => Dash,
			SoulSpellId.Light => Light,
			SoulSpellId.Flight => Flight,
			_ => Dash
		};
	}

	public static uint WithExclusiveSelection(uint mask, SoulSpellId id, bool selected)
	{
		uint nextMask = WithSelection(mask, id, selected);
		if (!selected)
		{
			return nextMask;
		}

		// The two dash inputs are alternatives, while every other spell remains independent.
		return id switch
		{
			SoulSpellId.Dash => WithSelection(nextMask, SoulSpellId.Flight, false),
			SoulSpellId.Flight => WithSelection(nextMask, SoulSpellId.Dash, false),
			_ => nextMask
		};
	}

	public static bool IsSelected(uint mask, SoulSpellId id)
	{
		return (mask & (1u << (int)id)) != 0;
	}

	public static uint WithSelection(uint mask, SoulSpellId id, bool selected)
	{
		uint bit = 1u << (int)id;
		return selected ? mask | bit : mask & ~bit;
	}

	public static IEnumerable<IGrouping<SoulSpellCategory, SoulSpellDefinition>> AlwaysByCategory()
	{
		return All.Where(spell => spell.IsFree).GroupBy(spell => spell.Category);
	}

	public static IEnumerable<IGrouping<SoulSpellCategory, SoulSpellDefinition>> StanceByCategory()
	{
		return All.Where(spell => !spell.IsFree).GroupBy(spell => spell.Category);
	}

	public static string CategoryName(SoulSpellCategory category)
	{
		return Language.GetTextValue($"Mods.SoulsOfTerra.UI.SoulspellCategory.{category}");
	}

	public static double GetSoulsPerTick(uint selectionMask, bool stanceOn)
	{
		if (!stanceOn)
		{
			return 0d;
		}

		double soulsPerTick = 0d;
		foreach (SoulSpellDefinition spell in All)
		{
			if (spell.IsFree || spell.TicksPerSoul <= 0 || !IsSelected(selectionMask, spell.Id))
			{
				continue;
			}

			soulsPerTick += 1d / spell.TicksPerSoul;
		}

		return soulsPerTick;
	}

	public static double GetCheckedPaidSoulsPerTick(uint selectionMask)
	{
		return GetSoulsPerTick(selectionMask, true);
	}

	public static string FormatDrain(double soulsPerTick)
	{
		if (soulsPerTick <= 0d)
		{
			return Language.GetTextValue("Mods.SoulsOfTerra.UI.SoulspellNoDrain");
		}

		double ticksPerSoul = 1d / soulsPerTick;
		int seconds = Math.Max(1, (int)Math.Round(ticksPerSoul / 60d));
		if (Math.Abs(ticksPerSoul - seconds * 60d) < 0.51d)
		{
			return Language.GetTextValue("Mods.SoulsOfTerra.UI.SoulspellCostInterval", seconds);
		}

		return Language.GetTextValue("Mods.SoulsOfTerra.UI.SoulspellDrainPerSecond", (soulsPerTick * 60d).ToString("0.##"));
	}

	public static string FormatTimeToEmpty(long souls, double soulsPerTick)
	{
		if (soulsPerTick <= 0d)
		{
			return Language.GetTextValue("Mods.SoulsOfTerra.UI.SoulspellNoDrain");
		}

		if (souls <= 0)
		{
			return Language.GetTextValue("Mods.SoulsOfTerra.UI.SoulspellEmptyNow");
		}

		long seconds = Math.Max(1L, (long)Math.Ceiling(souls / soulsPerTick / 60d));
		long days = seconds / 86_400;
		long hours = seconds / 3_600;
		long minutes = seconds / 60;
		if (days > 0)
		{
			return Language.GetTextValue("Mods.SoulsOfTerra.UI.SoulspellEmptyInDays", days, hours % 24);
		}

		if (hours > 0)
		{
			return Language.GetTextValue("Mods.SoulsOfTerra.UI.SoulspellEmptyInHours", hours, minutes % 60);
		}

		return minutes > 0
			? Language.GetTextValue("Mods.SoulsOfTerra.UI.SoulspellEmptyInMinutes", minutes, seconds % 60)
			: Language.GetTextValue("Mods.SoulsOfTerra.UI.SoulspellEmptyInSeconds", seconds);
	}
}
