using Microsoft.Xna.Framework;
using Terraria;

namespace SoulsOfTerra.Common.Swings;

public interface ISoulSwingItem
{
	SoulSwingStyle GetSwingStyle(Player player);

	void OnSwingCut(Player player, Projectile swing, Vector2 tip, Vector2 aim)
	{
	}
}
