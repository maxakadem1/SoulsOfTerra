using System;

namespace SoulsOfTerra.Common;

public static class SoulMath
{
	public static long SaturatingAdd(long left, long right)
	{
		if (right > 0 && left > long.MaxValue - right)
		{
			return long.MaxValue;
		}

		if (right < 0 && left < long.MinValue - right)
		{
			return long.MinValue;
		}

		return left + right;
	}

	public static long CeilingToLong(double value)
	{
		if (double.IsNaN(value) || value <= 0d)
		{
			return 0;
		}

		return value >= long.MaxValue ? long.MaxValue : (long)Math.Ceiling(value);
	}
}
