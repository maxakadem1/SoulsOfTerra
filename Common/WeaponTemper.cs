using System;
using System.Collections.Generic;
using SoulsOfTerra.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Common;

/// <summary>
/// Convergence math and pricing for weapon temper. Temper raises any weapon toward a damage-per-second
/// target defined by the world's Terraforge Temper, so a weak weapon climbs far and a current-era weapon
/// climbs a little, but both end in the same league.
/// </summary>
public static class WeaponTemper
{
	public const int MaxLevel = 9;

	/// <summary>Levels surrendered when temper is moved to another weapon.</summary>
	public const int TransferLevelLoss = 2;

	/// <summary>Share of the retained levels' cost charged to move temper between weapons.</summary>
	private const double TransferCostShare = 0.4d;

	// Target single-target damage per second for each temper level, sitting slightly above the strongest
	// comparable weapon obtainable at that stage. Index 0 is untempered and never applies a target.
	// Prototype values: they must be measured against real weapons before release.
	private static readonly double[] EraDamagePerSecond =
	{
		0d, 55d, 80d, 115d, 200d, 360d, 520d, 720d, 950d, 1_400d
	};

	// Souls charged for each level. Index 0 is unused.
	private static readonly long[] LevelCosts =
	{
		0, 5_000, 12_000, 25_000, 50_000, 110_000, 200_000, 320_000, 500_000, 800_000
	};

	// Weapons whose real output is not damage times rate. The value is how many effective hits one use
	// produces, so the convergence target is divided by it. An exception list, not per-weapon authoring.
	private static readonly Dictionary<int, double> OutputPerUse = new()
	{
		// Multi-projectile firearms.
		[ItemID.Shotgun] = 3d,
		[ItemID.QuadBarrelShotgun] = 4d,
		[ItemID.Boomstick] = 3d,
		[ItemID.OnyxBlaster] = 3d,
		[ItemID.TacticalShotgun] = 4d,
		[ItemID.Xenopopper] = 3d,
		[ItemID.ChainGun] = 1.5d,
		[ItemID.Uzi] = 1.5d,
		[ItemID.Megashark] = 1.5d,
		[ItemID.SDMG] = 1.5d,
		// Repeating and multi-shot bows.
		[ItemID.Tsunami] = 5d,
		[ItemID.Phantasm] = 4d,
		[ItemID.DaedalusStormbow] = 3d,
		// Channelled or continuously ticking magic.
		[ItemID.LastPrism] = 6d,
		[ItemID.LaserMachinegun] = 3d,
		[ItemID.NettleBurst] = 3d,
		[ItemID.LeafBlower] = 2d,
		[ItemID.RainbowGun] = 4d,
		// Flails and yoyos tick far more often than their use time suggests.
		[ItemID.Sunfury] = 4d,
		[ItemID.DaoofPow] = 3d,
		[ItemID.FlowerPow] = 4d,
		[ItemID.Terrarian] = 6d,
		[ItemID.TheEyeOfCthulhu] = 5d,
		[ItemID.Kraken] = 5d,
		[ItemID.HelFire] = 4d,
		[ItemID.Yelets] = 4d,
		[ItemID.Amarok] = 4d,
		// Minion staves persist, so listed damage is dealt repeatedly rather than once per use.
		[ItemID.StardustDragonStaff] = 8d,
		[ItemID.StardustCellStaff] = 6d,
		[ItemID.XenoStaff] = 5d,
		[ItemID.RavenStaff] = 5d,
		[ItemID.OpticStaff] = 5d,
		[ItemID.PirateStaff] = 5d,
		[ItemID.SpiderStaff] = 5d,
		[ItemID.ImpStaff] = 4d,
		[ItemID.HornetStaff] = 4d,
		[ItemID.SlimeStaff] = 4d
	};

	/// <summary>
	/// Whether this item is a legitimate temper subject. Tools, ammunition, accessories and stackables
	/// are excluded so temper only ever applies to a weapon the player actually swings or fires.
	/// </summary>
	public static bool CanTemper(Item item)
	{
		return item is not null && !item.IsAir && item.damage > 0 && item.maxStack == 1
			&& !item.accessory && item.useStyle != ItemUseStyleID.None
			&& item.pick <= 0 && item.axe <= 0 && item.hammer <= 0
			&& item.ammo == AmmoID.None && !item.consumable;
	}

	/// <summary>The highest level this world's Terraforge can pull a weapon to.</summary>
	public static int LevelCeiling() => Math.Clamp(SoulWorldSystem.TerraforgeTemper, 0, MaxLevel);

	public static long GetLevelCost(int level)
	{
		return level >= 1 && level < LevelCosts.Length ? LevelCosts[level] : 0;
	}

	/// <summary>Total souls spent reaching a level from untempered.</summary>
	public static long GetCumulativeCost(int level)
	{
		long total = 0;
		for (int step = 1; step <= Math.Min(level, MaxLevel); step++)
		{
			total += LevelCosts[step];
		}
		return total;
	}

	/// <summary>Level the destination weapon receives when temper is moved onto it.</summary>
	public static int GetTransferredLevel(int sourceLevel) => Math.Max(0, sourceLevel - TransferLevelLoss);

	public static long GetTransferCost(int sourceLevel)
	{
		return SoulMath.CeilingToLong(GetCumulativeCost(GetTransferredLevel(sourceLevel)) * TransferCostShare);
	}

	/// <summary>Soul fee to change a weapon's essence path without refunding the essences already fed.</summary>
	public static long GetReinfuseCost(int level)
	{
		return SoulMath.CeilingToLong(GetCumulativeCost(level) * 0.25d);
	}

	/// <summary>
	/// Damage the weapon should deal at this temper level. Untempered weapons keep their vanilla damage,
	/// and temper never reduces a weapon that already exceeds its target.
	/// </summary>
	public static int GetTemperedDamage(Item item, int level)
	{
		if (item is null || item.IsAir || level <= 0 || level >= EraDamagePerSecond.Length)
		{
			return item?.damage ?? 0;
		}

		double target = EraDamagePerSecond[level] / GetOutputPerUse(item);
		int converged = (int)Math.Round(target * GetEffectiveUseTime(item) / 60d);
		return Math.Max(GetUnprefixedDamage(item), converged);
	}

	/// <summary>Flat damage temper contributes on top of the weapon's own base damage.</summary>
	public static int GetDamageBonus(Item item, int level)
	{
		return Math.Max(0, GetTemperedDamage(item, level) - GetUnprefixedDamage(item));
	}

	/// <summary>
	/// Convergence measures the weapon without its prefix, so a damage prefix keeps its full absolute
	/// value on top of the temper bonus instead of being erased by the target.
	/// </summary>
	private static int GetUnprefixedDamage(Item item)
	{
		return item.OriginalDamage > 0 ? item.OriginalDamage : item.damage;
	}

	private static double GetEffectiveUseTime(Item item)
	{
		// Reuse delay is real downtime between swings and belongs in the rate.
		return Math.Max(2, item.useTime + item.reuseDelay);
	}

	private static double GetOutputPerUse(Item item)
	{
		if (OutputPerUse.TryGetValue(item.type, out double overrideValue))
		{
			return Math.Max(0.1d, overrideValue);
		}

		// Minions and sentries persist after the cast, so their listed damage is dealt repeatedly rather than once per use.
		if (item.DamageType.CountsAsClass(DamageClass.Summon))
		{
			return 4d;
		}

		return 1d;
	}
}
