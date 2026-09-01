using Microsoft.Xna.Framework;

namespace SoulsOfTerra.Systems;

internal static class SoullessUIPalette
{
	// Core neutrals mirror Soulless's sprite ramp; cyan is reserved for meaningful interaction states.
	public static readonly Color Panel = new(16, 17, 22, 245);
	public static readonly Color PanelBorder = new(69, 71, 84);
	public static readonly Color Surface = new(26, 27, 33, 245);
	public static readonly Color SurfaceInset = new(20, 21, 27, 245);
	public static readonly Color SurfaceRaised = new(35, 36, 43, 245);
	public static readonly Color SurfaceHover = new(46, 48, 58, 245);
	public static readonly Color SurfaceDisabled = new(31, 32, 38, 232);
	public static readonly Color Steel = new(69, 71, 84);
	public static readonly Color SteelMuted = new(52, 54, 65);
	public static readonly Color SteelLow = new(45, 47, 56);

	public static readonly Color TextPrimary = new(226, 231, 237);
	public static readonly Color TextSecondary = new(166, 174, 186);
	public static readonly Color TextMuted = new(125, 132, 143);
	public static readonly Color TextDisabled = new(112, 118, 128);

	public static readonly Color AccentSurface = new(17, 67, 74, 242);
	public static readonly Color AccentSurfaceHover = new(20, 88, 94, 248);
	public static readonly Color AccentBorder = new(22, 134, 137);
	public static readonly Color AccentHoverBorder = new(34, 174, 176);
	public static readonly Color Accent = new(46, 232, 230);
	public static readonly Color AccentBright = new(87, 248, 246);
	public static readonly Color AccentAdditive = new(46, 232, 230, 0);
	public static readonly Color AccentTextAdditive = new(190, 249, 247, 0);
	public static readonly Color AccentText = new(190, 249, 247);
	public static readonly Color AccentMuted = new(105, 197, 198);

	public static readonly Color WarningSurface = new(73, 52, 50);
	public static readonly Color WarningSurfaceHover = new(104, 65, 59);
	public static readonly Color WarningBorder = new(123, 78, 71);
	public static readonly Color WarningText = new(236, 183, 171);
	public static readonly Color Warning = new(238, 154, 137);
	public static readonly Color Requirement = new(207, 164, 135);
}
