using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace SoulsOfTerra.Common.Rendering;

internal static class ItemAnimationDrawing
{
	public static Rectangle GetFrame(int itemType, Texture2D texture)
	{
		// Custom UI and ritual draws must follow the same frame as Terraria's item renderer.
		return Main.itemAnimations[itemType]?.GetFrame(texture) ?? texture.Frame();
	}
}
