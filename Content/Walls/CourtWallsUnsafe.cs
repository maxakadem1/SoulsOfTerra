using Microsoft.Xna.Framework;
using SoulsOfTerra.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Walls;

public abstract class CourtWallUnsafeBase : ModWall
{
	protected abstract Color MapColor { get; }

	public override void SetStaticDefaults()
	{
		Main.wallHouse[Type] = false;
		DustType = DustID.Stone;
		AddMapEntry(MapColor);
	}

	public override bool CanExplode(int i, int j) => false;

	public override void KillWall(int i, int j, ref bool fail)
	{
		// The generated Court must survive repeat encounters and its later story use.
		fail = true;
	}
}

public sealed class CourtWallUnsafe : CourtWallUnsafeBase
{
	// Vanilla dungeon wall is temporary graybox art for the final masonry wall.
	public override string Texture => $"Terraria/Images/Wall_{WallID.BlueDungeonUnsafe}";
	protected override Color MapColor => new(35, 42, 55);
}

public sealed class CourtReliefWallUnsafe : CourtWallUnsafeBase
{
	// Stone slab distinguishes collision-free columns and vault ribs in the graybox.
	public override string Texture => $"Terraria/Images/Wall_{WallID.StoneSlab}";
	protected override Color MapColor => new(50, 58, 68);
}

public sealed class CourtAccentWallUnsafe : CourtWallUnsafeBase
{
	// Gold brick temporarily marks the future tarnished-brass inlay layer.
	public override string Texture => $"Terraria/Images/Wall_{WallID.GoldBrick}";
	protected override Color MapColor => new(94, 72, 38);

	public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
	{
		int localY = j - BuriedCourtSystem.CourtBounds.Top;
		if (!BuriedCourtSystem.Generated || localY < 39 || localY > 57)
		{
			return;
		}

		// Only the mid-wall seals and chains glow; capitals and lower trim remain plain brass.
		r = 0.035f;
		g = 0.085f;
		b = 0.16f;
	}
}
