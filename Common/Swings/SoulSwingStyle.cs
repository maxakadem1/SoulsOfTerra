using System;
using Microsoft.Xna.Framework;

namespace SoulsOfTerra.Common.Swings;

public sealed class SoulSwingStyle
{
	public int Duration { get; init; } = 45;
	public float WindUpPortion { get; init; } = 0.22f;
	public float CutPortion { get; init; } = 0.38f;
	public SoulSwingPath Path { get; init; } = SoulSwingPath.AlternatingLateral;
	public float ArcSpan { get; init; } = 2.9f;
	public float Reach { get; init; } = 86f;
	public float HitWidth { get; init; } = 40f;
	public float Scale { get; init; } = 1f;
	public Color RibbonColor { get; init; } = new(80, 225, 205);
	public int RibbonLifetime { get; init; } = 14;
	public float RibbonWidth { get; init; } = 22f;
	public int AfterimageCount { get; init; } = 5;
	public Vector2? GripOrigin { get; init; }

	public SoulSwingPose Evaluate(int age, float aim, float sign)
	{
		int duration = Math.Max(1, Duration);
		int windUpEnd = Math.Max(1, (int)MathF.Round(duration * WindUpPortion));
		int cutLength = Math.Max(1, (int)MathF.Round(duration * CutPortion));
		int cutEnd = Math.Min(duration, windUpEnd + cutLength);
		GetArc(aim, sign, out float startAngle, out float endAngle);

		if (age < windUpEnd)
		{
			float progress = EaseInQuadratic(age / (float)windUpEnd);
			float restAngle = MathHelper.Lerp(aim, startAngle, 0.22f);
			return new SoulSwingPose
			{
				Angle = MathHelper.Lerp(restAngle, startAngle, progress),
				ReachMultiplier = Path == SoulSwingPath.Thrust
					? MathHelper.Lerp(0.46f, 0.32f, progress)
					: MathHelper.Lerp(0.88f, 0.94f, progress),
				CanDamage = false,
				InCut = false,
				CutProgress = 0f
			};
		}

		if (age < cutEnd)
		{
			float progress = EaseOutCubic((age - windUpEnd) / (float)Math.Max(1, cutEnd - windUpEnd));
			return new SoulSwingPose
			{
				Angle = MathHelper.Lerp(startAngle, endAngle, progress),
				ReachMultiplier = Path == SoulSwingPath.Thrust
					? MathHelper.Lerp(0.32f, 1.05f, progress)
					: MathHelper.Lerp(0.94f, 1f, progress),
				CanDamage = true,
				InCut = true,
				CutProgress = progress
			};
		}

		float recover = (age - cutEnd) / (float)Math.Max(1, duration - cutEnd);
		float overshoot = endAngle + sign * 0.16f;
		float rest = endAngle + sign * 0.05f;
		float recoverAngle = recover < 0.35f
			? MathHelper.Lerp(endAngle, overshoot, EaseOutCubic(recover / 0.35f))
			: MathHelper.Lerp(overshoot, rest, EaseInOut((recover - 0.35f) / 0.65f));
		return new SoulSwingPose
		{
			Angle = Path == SoulSwingPath.Thrust ? MathHelper.Lerp(endAngle, aim, EaseOutCubic(recover)) : recoverAngle,
			ReachMultiplier = Path == SoulSwingPath.Thrust
				? MathHelper.Lerp(1.05f, 0.92f, EaseOutCubic(recover))
				: MathHelper.Lerp(1f, 0.97f, recover),
			CanDamage = false,
			InCut = false,
			CutProgress = 1f
		};
	}

	private void GetArc(float aim, float sign, out float startAngle, out float endAngle)
	{
		float halfSpan = ArcSpan * 0.5f;
		switch (Path)
		{
			case SoulSwingPath.Rising:
				startAngle = aim + sign * halfSpan * 0.86f;
				endAngle = aim - sign * halfSpan * 0.94f;
				return;
			case SoulSwingPath.Falling:
				startAngle = aim - sign * halfSpan * 0.94f;
				endAngle = aim + sign * halfSpan * 0.86f;
				return;
			case SoulSwingPath.Thrust:
				startAngle = aim - sign * 0.12f;
				endAngle = aim + sign * 0.04f;
				return;
			default:
				startAngle = aim - sign * halfSpan;
				endAngle = aim + sign * halfSpan;
				return;
		}
	}

	private static float EaseInQuadratic(float progress) => progress * progress;
	private static float EaseOutCubic(float progress) => 1f - MathF.Pow(1f - progress, 3f);
	private static float EaseInOut(float progress) => progress * progress * (3f - 2f * progress);
}

public readonly struct SoulSwingPose
{
	public float Angle { get; init; }
	public float ReachMultiplier { get; init; }
	public bool CanDamage { get; init; }
	public bool InCut { get; init; }
	public float CutProgress { get; init; }
}
