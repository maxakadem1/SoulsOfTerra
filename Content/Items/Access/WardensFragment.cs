using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Items.Access;

public class WardensFragment : ModItem
{
	private const float InventoryScale = 1.18f;

	public override void SetDefaults()
	{
		Item.width = 24;
		Item.height = 24;
		Item.maxStack = 1;
		Item.rare = ItemRarityID.Orange;
		Item.value = 0;
	}

	public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor,
		Color itemColor, Vector2 origin, float scale)
	{
		Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;

		// Enlarge the icon without changing its world-space pickup dimensions.
		spriteBatch.Draw(texture, position, frame, drawColor, 0f, origin, scale * InventoryScale, SpriteEffects.None, 0f);
		return false;
	}
}
