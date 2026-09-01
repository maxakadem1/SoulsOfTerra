using System;
using System.Collections.Generic;
using System.Linq;
using SoulsOfTerra.Content.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SoulsOfTerra.Common;

public enum SoulSpellId : byte
{
	Dash = 0, Shine = 1, Flight = 2,
	Archery, Battle, Builder, Calming, Dangersense, Featherfall, Gills, Hunter, Invisibility,
	Ironskin, Mining, NightOwl, Regeneration, Swiftness, WaterWalking,
	AmmoReservation, Crate, Fishing, Flipper, Heartreach, LesserLuck, Luck, GreaterLuck, Sonar, Spelunker, Summoning,
	BiomeSight, Endurance, Gravitation, Inferno, MagicPower, ManaRegeneration, ObsidianSkin, Rage, Thorns, Titan, Warmth, Wrath,
	Lifeforce
}

public enum SoulSpellCategory : byte
{
	Exploration = 0,
	Combat = 1,
	Gathering = 2,
	Building = 3
}

public readonly record struct SoulSpellDefinition(
	SoulSpellId Id,
	SoulSpellCategory Category,
	bool IsFree,
	double SoulsPerSecond,
	string NameKey,
	string DescriptionKey,
	int PotionItemType,
	int BuffType,
	int EssenceItemType)
{
	public bool IsPotionSpell => PotionItemType > ItemID.None;
	public string Name => Id switch
	{
		// All luck potions share one vanilla buff name, so retain its strength qualifier.
		SoulSpellId.LesserLuck => $"{Lang.GetBuffName(BuffType)} (Lesser)",
		SoulSpellId.GreaterLuck => $"{Lang.GetBuffName(BuffType)} (Greater)",
		_ => BuffType > 0 ? Lang.GetBuffName(BuffType) : Language.GetTextValue(NameKey)
	};
	public string Description => BuffType > 0 ? Lang.GetBuffDescription(BuffType) : Language.GetTextValue(DescriptionKey);
	public string CostText => IsFree || SoulsPerSecond <= 0d
		? Language.GetTextValue("Mods.SoulsOfTerra.UI.SoulspellFree")
		: Language.GetTextValue("Mods.SoulsOfTerra.UI.SoulspellDrainPerSecond", SoulsPerSecond.ToString("0.##"));
}

public static class SoulSpellRegistry
{
	private static SoulSpellDefinition Innate(SoulSpellId id, SoulSpellCategory category, string nameKey, string descriptionKey)
	{
		return new SoulSpellDefinition(id, category, true, 0d, nameKey, descriptionKey, ItemID.None, 0, ItemID.None);
	}

	private static SoulSpellDefinition Potion(SoulSpellId id, SoulSpellCategory category, double soulsPerSecond,
		int potionItemType, int buffType, int essenceItemType)
	{
		return new SoulSpellDefinition(id, category, false, soulsPerSecond, string.Empty, string.Empty,
			potionItemType, buffType, essenceItemType);
	}

	public static readonly SoulSpellDefinition Dash = Innate(SoulSpellId.Dash, SoulSpellCategory.Exploration,
		"Mods.SoulsOfTerra.UI.SoulspellDashName", "Mods.SoulsOfTerra.UI.SoulspellDashDescription");
	public static readonly SoulSpellDefinition Flight = Innate(SoulSpellId.Flight, SoulSpellCategory.Exploration,
		"Mods.SoulsOfTerra.UI.SoulspellFlightName", "Mods.SoulsOfTerra.UI.SoulspellFlightDescription");

	public static readonly SoulSpellDefinition[] All =
	{
		Dash,
		Flight,
		// Shine is the default paid spell and teaches the potion-soulspell loop.
		Potion(SoulSpellId.Shine, SoulSpellCategory.Exploration, 0.08d, ItemID.ShinePotion, BuffID.Shine, ModContent.ItemType<EyeEssence>()),
		Potion(SoulSpellId.Archery, SoulSpellCategory.Combat, 0.10d, ItemID.ArcheryPotion, BuffID.Archery, ModContent.ItemType<EyeEssence>()),
		Potion(SoulSpellId.Battle, SoulSpellCategory.Combat, 0.10d, ItemID.BattlePotion, BuffID.Battle, ModContent.ItemType<EyeEssence>()),
		Potion(SoulSpellId.Builder, SoulSpellCategory.Building, 0.08d, ItemID.BuilderPotion, BuffID.Builder, ModContent.ItemType<EyeEssence>()),
		Potion(SoulSpellId.Calming, SoulSpellCategory.Exploration, 0.08d, ItemID.CalmingPotion, BuffID.Calm, ModContent.ItemType<EyeEssence>()),
		Potion(SoulSpellId.Dangersense, SoulSpellCategory.Exploration, 0.08d, ItemID.TrapsightPotion, BuffID.Dangersense, ModContent.ItemType<EyeEssence>()),
		Potion(SoulSpellId.Featherfall, SoulSpellCategory.Exploration, 0.08d, ItemID.FeatherfallPotion, BuffID.Featherfall, ModContent.ItemType<EyeEssence>()),
		Potion(SoulSpellId.Gills, SoulSpellCategory.Exploration, 0.08d, ItemID.GillsPotion, BuffID.Gills, ModContent.ItemType<EyeEssence>()),
		Potion(SoulSpellId.Hunter, SoulSpellCategory.Exploration, 0.10d, ItemID.HunterPotion, BuffID.Hunter, ModContent.ItemType<EyeEssence>()),
		Potion(SoulSpellId.Invisibility, SoulSpellCategory.Exploration, 0.08d, ItemID.InvisibilityPotion, BuffID.Invisibility, ModContent.ItemType<EyeEssence>()),
		Potion(SoulSpellId.Ironskin, SoulSpellCategory.Combat, 0.13d, ItemID.IronskinPotion, BuffID.Ironskin, ModContent.ItemType<EyeEssence>()),
		Potion(SoulSpellId.Mining, SoulSpellCategory.Gathering, 0.08d, ItemID.MiningPotion, BuffID.Mining, ModContent.ItemType<EyeEssence>()),
		Potion(SoulSpellId.NightOwl, SoulSpellCategory.Exploration, 0.08d, ItemID.NightOwlPotion, BuffID.NightOwl, ModContent.ItemType<EyeEssence>()),
		Potion(SoulSpellId.Regeneration, SoulSpellCategory.Combat, 0.10d, ItemID.RegenerationPotion, BuffID.Regeneration, ModContent.ItemType<EyeEssence>()),
		Potion(SoulSpellId.Swiftness, SoulSpellCategory.Exploration, 0.10d, ItemID.SwiftnessPotion, BuffID.Swiftness, ModContent.ItemType<EyeEssence>()),
		Potion(SoulSpellId.WaterWalking, SoulSpellCategory.Exploration, 0.08d, ItemID.WaterWalkingPotion, BuffID.WaterWalking, ModContent.ItemType<EyeEssence>()),

		Potion(SoulSpellId.AmmoReservation, SoulSpellCategory.Combat, 0.15d, ItemID.AmmoReservationPotion, BuffID.AmmoReservation, ModContent.ItemType<QueenBeeEssence>()),
		Potion(SoulSpellId.Crate, SoulSpellCategory.Gathering, 0.11d, ItemID.CratePotion, BuffID.Crate, ModContent.ItemType<QueenBeeEssence>()),
		Potion(SoulSpellId.Fishing, SoulSpellCategory.Gathering, 0.11d, ItemID.FishingPotion, BuffID.Fishing, ModContent.ItemType<QueenBeeEssence>()),
		Potion(SoulSpellId.Flipper, SoulSpellCategory.Exploration, 0.11d, ItemID.FlipperPotion, BuffID.Flipper, ModContent.ItemType<QueenBeeEssence>()),
		Potion(SoulSpellId.Heartreach, SoulSpellCategory.Combat, 0.15d, ItemID.HeartreachPotion, BuffID.Heartreach, ModContent.ItemType<QueenBeeEssence>()),
		Potion(SoulSpellId.LesserLuck, SoulSpellCategory.Gathering, 0.11d, ItemID.LuckPotionLesser, BuffID.Lucky, ModContent.ItemType<QueenBeeEssence>()),
		Potion(SoulSpellId.Luck, SoulSpellCategory.Gathering, 0.15d, ItemID.LuckPotion, BuffID.Lucky, ModContent.ItemType<QueenBeeEssence>()),
		Potion(SoulSpellId.GreaterLuck, SoulSpellCategory.Gathering, 0.19d, ItemID.LuckPotionGreater, BuffID.Lucky, ModContent.ItemType<QueenBeeEssence>()),
		Potion(SoulSpellId.Sonar, SoulSpellCategory.Gathering, 0.11d, ItemID.SonarPotion, BuffID.Sonar, ModContent.ItemType<QueenBeeEssence>()),
		Potion(SoulSpellId.Spelunker, SoulSpellCategory.Gathering, 0.15d, ItemID.SpelunkerPotion, BuffID.Spelunker, ModContent.ItemType<QueenBeeEssence>()),
		Potion(SoulSpellId.Summoning, SoulSpellCategory.Combat, 0.19d, ItemID.SummoningPotion, BuffID.Summoning, ModContent.ItemType<QueenBeeEssence>()),

		Potion(SoulSpellId.BiomeSight, SoulSpellCategory.Gathering, 0.15d, ItemID.BiomeSightPotion, BuffID.BiomeSight, ModContent.ItemType<SkeletronEssence>()),
		Potion(SoulSpellId.Endurance, SoulSpellCategory.Combat, 0.25d, ItemID.EndurancePotion, BuffID.Endurance, ModContent.ItemType<SkeletronEssence>()),
		Potion(SoulSpellId.Gravitation, SoulSpellCategory.Exploration, 0.20d, ItemID.GravitationPotion, BuffID.Gravitation, ModContent.ItemType<SkeletronEssence>()),
		Potion(SoulSpellId.Inferno, SoulSpellCategory.Combat, 0.25d, ItemID.InfernoPotion, BuffID.Inferno, ModContent.ItemType<SkeletronEssence>()),
		Potion(SoulSpellId.MagicPower, SoulSpellCategory.Combat, 0.25d, ItemID.MagicPowerPotion, BuffID.MagicPower, ModContent.ItemType<SkeletronEssence>()),
		Potion(SoulSpellId.ManaRegeneration, SoulSpellCategory.Combat, 0.15d, ItemID.ManaRegenerationPotion, BuffID.ManaRegeneration, ModContent.ItemType<SkeletronEssence>()),
		Potion(SoulSpellId.ObsidianSkin, SoulSpellCategory.Exploration, 0.20d, ItemID.ObsidianSkinPotion, BuffID.ObsidianSkin, ModContent.ItemType<SkeletronEssence>()),
		Potion(SoulSpellId.Rage, SoulSpellCategory.Combat, 0.25d, ItemID.RagePotion, BuffID.Rage, ModContent.ItemType<SkeletronEssence>()),
		Potion(SoulSpellId.Thorns, SoulSpellCategory.Combat, 0.20d, ItemID.ThornsPotion, BuffID.Thorns, ModContent.ItemType<SkeletronEssence>()),
		Potion(SoulSpellId.Titan, SoulSpellCategory.Combat, 0.20d, ItemID.TitanPotion, BuffID.Titan, ModContent.ItemType<SkeletronEssence>()),
		Potion(SoulSpellId.Warmth, SoulSpellCategory.Exploration, 0.15d, ItemID.WarmthPotion, BuffID.Warmth, ModContent.ItemType<SkeletronEssence>()),
		Potion(SoulSpellId.Wrath, SoulSpellCategory.Combat, 0.25d, ItemID.WrathPotion, BuffID.Wrath, ModContent.ItemType<SkeletronEssence>()),

		Potion(SoulSpellId.Lifeforce, SoulSpellCategory.Combat, 0.38d, ItemID.LifeforcePotion, BuffID.Lifeforce, ModContent.ItemType<WallOfFleshEssence>())
	};

	public static readonly SoulSpellDefinition[] PotionSpells = All.Where(spell => spell.IsPotionSpell).ToArray();
	public static readonly ulong KnownSpellMask = All.Aggregate(0UL, (mask, spell) => mask | Bit(spell.Id));
	public static readonly ulong DefaultLearnedMask = Bit(SoulSpellId.Dash) | Bit(SoulSpellId.Flight) | Bit(SoulSpellId.Shine);
	public static readonly ulong DefaultSelectionMask = Bit(SoulSpellId.Dash) | Bit(SoulSpellId.Shine);

	public static SoulSpellDefinition Get(SoulSpellId id) => TryGet(id, out SoulSpellDefinition definition) ? definition : Dash;

	public static bool TryGet(SoulSpellId id, out SoulSpellDefinition definition)
	{
		foreach (SoulSpellDefinition spell in All)
		{
			if (spell.Id == id)
			{
				definition = spell;
				return true;
			}
		}

		definition = default;
		return false;
	}

	public static ulong WithExclusiveSelection(ulong mask, SoulSpellId id, bool selected)
	{
		ulong nextMask = WithSelection(mask, id, selected);
		if (!selected)
		{
			return nextMask;
		}

		// The two dash inputs are alternatives.
		nextMask = id switch
		{
			SoulSpellId.Dash => WithSelection(nextMask, SoulSpellId.Flight, false),
			SoulSpellId.Flight => WithSelection(nextMask, SoulSpellId.Dash, false),
			_ => nextMask
		};

		// Multiple potion strengths that share one vanilla buff cannot double-dip.
		SoulSpellDefinition selectedSpell = Get(id);
		if (selectedSpell.BuffType > 0)
		{
			foreach (SoulSpellDefinition spell in PotionSpells)
			{
				if (spell.Id != id && spell.BuffType == selectedSpell.BuffType)
				{
					nextMask = WithSelection(nextMask, spell.Id, false);
				}
			}
		}

		return nextMask;
	}

	public static bool IsSelected(ulong mask, SoulSpellId id) => (mask & Bit(id)) != 0;
	public static ulong WithSelection(ulong mask, SoulSpellId id, bool selected)
	{
		ulong bit = Bit(id);
		return selected ? mask | bit : mask & ~bit;
	}

	public static ulong Bit(SoulSpellId id) => 1UL << (int)id;
	public static IEnumerable<IGrouping<SoulSpellCategory, SoulSpellDefinition>> AlwaysByCategory() =>
		All.Where(spell => spell.IsFree).GroupBy(spell => spell.Category);
	public static IEnumerable<IGrouping<SoulSpellCategory, SoulSpellDefinition>> StanceByCategory() =>
		All.Where(spell => !spell.IsFree).GroupBy(spell => spell.Category);
	public static string CategoryName(SoulSpellCategory category) =>
		Language.GetTextValue($"Mods.SoulsOfTerra.UI.SoulspellCategory.{category}");

	public static double GetSoulsPerTick(ulong selectionMask, ulong learnedMask, bool stanceOn)
	{
		if (!stanceOn)
		{
			return 0d;
		}

		double soulsPerSecond = 0d;
		foreach (SoulSpellDefinition spell in All)
		{
			if (!spell.IsFree && spell.SoulsPerSecond > 0d && IsSelected(selectionMask & learnedMask, spell.Id))
			{
				soulsPerSecond += spell.SoulsPerSecond;
			}
		}

		return soulsPerSecond / 60d;
	}

	public static double GetCheckedPaidSoulsPerTick(ulong selectionMask, ulong learnedMask) =>
		GetSoulsPerTick(selectionMask, learnedMask, true);

	public static string FormatDrain(double soulsPerTick)
	{
		if (soulsPerTick <= 0d)
		{
			return Language.GetTextValue("Mods.SoulsOfTerra.UI.SoulspellNoDrain");
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
