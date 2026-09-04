using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Common;

public enum PathEffectKind : byte
{
	Debuff,
	Crit,
	Lifesteal,
	ArmorPenetration,
	DoubleDamage
}

/// <summary>
/// A weapon is raised along one essence path. Temper supplies magnitude; the path supplies character,
/// and its effect strengthens with every level fed into it.
/// </summary>
public sealed record EssencePathDefinition(PathEffectKind Kind, int DebuffType, string EffectName, Color RimColor)
{
	/// <summary>Debuff length in ticks at the given temper level.</summary>
	public int DebuffDuration(int level) => 60 + 30 * Math.Max(0, level);

	public float CritBonus(int level) => 2f * Math.Max(0, level);

	/// <summary>Share of damage dealt returned as health.</summary>
	public float LifestealShare(int level) => 0.005f * Math.Max(0, level);

	public int ArmorPenetration(int level) => 2 * Math.Max(0, level);

	public float DoubleDamageChance(int level) => 0.03f * Math.Max(0, level);

	/// <summary>Level-agnostic line for essence item tooltips.</summary>
	public string DescribeInventory()
	{
		string effect = Kind switch
		{
			PathEffectKind.Crit => "bonus critical strike chance",
			PathEffectKind.Lifesteal => "heals on hit",
			PathEffectKind.ArmorPenetration => "ignores enemy defence",
			PathEffectKind.DoubleDamage => "chance to deal double damage",
			_ => $"inflicts {Lang.GetBuffName(DebuffType)}"
		};
		return $"{EffectName} — {effect}";
	}

	public string DescribeAtLevel(int level) => $"{EffectName}: {DescribeEffect(level, verbose: true)}";

	public string DescribeCompact(int level) => $"{EffectName}: {DescribeEffect(level, verbose: false)}";

	public Color GetRimColor(float time)
	{
		return DebuffType == BuffID.BetsysCurse
			? Main.hslToRgb(time * 0.28f % 1f, 0.75f, 0.72f)
			: RimColor;
	}

	/// <summary>Short enough for the Temper tab; verbose form is the item tooltip.</summary>
	public string DescribeEffect(int level, bool verbose)
	{
		return Kind switch
		{
			PathEffectKind.Crit => verbose
				? $"+{CritBonus(level):0}% critical strike chance"
				: $"+{CritBonus(level):0}% crit",
			PathEffectKind.Lifesteal => verbose
				? $"heals {LifestealShare(level) * 100f:0.#}% of damage dealt"
				: $"heals {LifestealShare(level) * 100f:0.#}% of damage",
			PathEffectKind.ArmorPenetration => verbose
				? $"ignores {ArmorPenetration(level)} enemy defence"
				: $"ignores {ArmorPenetration(level)} defence",
			PathEffectKind.DoubleDamage => verbose
				? $"{DoubleDamageChance(level) * 100f:0}% chance to deal double damage"
				: $"{DoubleDamageChance(level) * 100f:0}% double damage",
			_ => verbose
				? $"inflicts {Lang.GetBuffName(DebuffType)} for {DebuffDuration(level) / 60f:0.#} seconds"
				: $"{Lang.GetBuffName(DebuffType)} {DebuffDuration(level) / 60f:0.#}s"
		};
	}
}

public static class EssencePathRegistry
{
	// Parallel to SoulEssenceRegistry.Definitions. The shared index is the saved and networked path ID.
	public static EssencePathDefinition[] Definitions { get; } =
	{
		Debuff(BuffID.Slow, "Viscous", new Color(140, 210, 255)),
		new EssencePathDefinition(PathEffectKind.Crit, 0, "Watchful", new Color(255, 110, 105)),
		Debuff(BuffID.Frostburn, "Frostbitten", new Color(190, 245, 255)),
		Debuff(BuffID.Ichor, "Corrupting", new Color(255, 235, 90)),
		new EssencePathDefinition(PathEffectKind.Lifesteal, 0, "Devouring", new Color(255, 130, 165)),
		Debuff(BuffID.Venom, "Envenomed", new Color(150, 255, 110)),
		Debuff(BuffID.BrokenArmor, "Sundering", new Color(210, 180, 255)),
		Debuff(BuffID.Confused, "Discordant", new Color(110, 255, 235)),
		Debuff(BuffID.OnFire3, "Infernal", new Color(255, 150, 80)),
		Debuff(BuffID.Electrified, "Crystalline", new Color(255, 165, 230)),
		Debuff(BuffID.CursedInferno, "Relentless", new Color(220, 255, 110)),
		Debuff(BuffID.ShadowFlame, "Twinned", new Color(210, 140, 255)),
		Debuff(BuffID.Bleeding, "Serrated", new Color(255, 95, 110)),
		Debuff(BuffID.Poisoned, "Overgrown", new Color(110, 255, 145)),
		Debuff(BuffID.Daybreak, "Ancient", new Color(255, 200, 90)),
		Debuff(BuffID.Frostburn2, "Tempestuous", new Color(110, 175, 255)),
		Debuff(BuffID.BetsysCurse, "Prismatic", new Color(255, 160, 220)),
		new EssencePathDefinition(PathEffectKind.DoubleDamage, 0, "Ritualistic", new Color(155, 170, 255)),
		new EssencePathDefinition(PathEffectKind.ArmorPenetration, 0, "Celestial", new Color(235, 245, 255))
	};

	public const byte NoPath = byte.MaxValue;

	public static bool TryGet(int pathIndex, out EssencePathDefinition definition)
	{
		definition = pathIndex >= 0 && pathIndex < Definitions.Length ? Definitions[pathIndex] : null;
		return definition is not null;
	}

	/// <summary>Path index for an essence item, matching its soul essence registry position.</summary>
	public static int IndexOfEssence(int essenceItemType)
	{
		SoulEssenceDefinition[] essences = SoulEssenceRegistry.Definitions;
		for (int index = 0; index < essences.Length && index < Definitions.Length; index++)
		{
			if (essences[index].ItemType == essenceItemType)
			{
				return index;
			}
		}
		return -1;
	}

	public static int EssenceItemType(int pathIndex)
	{
		return SoulEssenceRegistry.TryGet(pathIndex, out SoulEssenceDefinition essence) ? essence.ItemType : ItemID.None;
	}

	public static string PathName(int pathIndex)
	{
		return TryGet(pathIndex, out EssencePathDefinition path) ? path.EffectName : string.Empty;
	}

	public static string GetInventorySummary(int essenceItemType)
	{
		int pathIndex = IndexOfEssence(essenceItemType);
		return TryGet(pathIndex, out EssencePathDefinition path)
			? $"Weapon Infusion: {path.DescribeInventory()}"
			: "Weapon Infusion: Unknown";
	}

	private static EssencePathDefinition Debuff(int buffType, string effectName, Color rim) =>
		new(PathEffectKind.Debuff, buffType, effectName, rim);
}

/// <summary>Fails the load rather than letting the two registries drift silently out of alignment.</summary>
public sealed class EssencePathRegistryValidator : ModSystem
{
	public override void PostSetupContent()
	{
		if (EssencePathRegistry.Definitions.Length != SoulEssenceRegistry.Definitions.Length)
		{
			throw new InvalidOperationException(
				$"Essence paths ({EssencePathRegistry.Definitions.Length}) must stay parallel to soul essences "
				+ $"({SoulEssenceRegistry.Definitions.Length}); the shared index is saved and networked.");
		}
	}
}
