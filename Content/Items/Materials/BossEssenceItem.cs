using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Common.Rendering;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Items.Materials;

public abstract class BossEssenceItem : ModItem
{
	// The shared soul is only a fallback for contexts that bypass ModItem drawing hooks.
	public override string Texture => $"Terraria/Images/Item_{ItemID.SoulofLight}";

	public override void Unload()
	{
		EssenceEchoRenderer.Unload();
	}

	public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor,
		Color itemColor, Vector2 origin, float scale)
	{
		// Custom echoes already target an inventory slot, so the fallback texture's fit scale is irrelevant.
		EssenceEchoRenderer.TryDraw(spriteBatch, Type, position, 36f, Color.White);
		return false;
	}

	public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor,
		ref float rotation, ref float scale, int whoAmI)
	{
		Vector2 center = Item.Center - Main.screenPosition;
		EssenceEchoRenderer.TryDraw(spriteBatch, Type, center, 46f * scale, lightColor, rotation);
		return false;
	}
}
