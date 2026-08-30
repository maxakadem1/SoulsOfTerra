using Microsoft.Xna.Framework.Graphics;

namespace SoulsOfTerra.Common.Rendering;

/// <summary>Draws world effects into the shared half-resolution pixel target.</summary>
public interface IPixelatedDrawable
{
	/// <summary>Draw using normal screen-space coordinates; the renderer handles downscaling.</summary>
	void DrawPixelated(SpriteBatch spriteBatch);
}
