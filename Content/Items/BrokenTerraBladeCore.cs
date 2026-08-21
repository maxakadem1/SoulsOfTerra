using SoulsOfTerra.Common;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Items;

public class BrokenTerraBladeCore : ModItem
{
	public override string Texture => $"Terraria/Images/Item_{ItemID.BrokenHeroSword}";

	public override void SetDefaults()
	{
		Item.width = 30;
		Item.height = 30;
		Item.maxStack = 99;
		Item.rare = ItemRarityID.Green;
		Item.value = 0;
	}

	public override void HoldItem(Player player)
	{
		if (player.whoAmI != Main.myPlayer || !Main.mouseRight || !Main.mouseRightRelease || player.lastMouseInterface)
		{
			return;
		}

		int tileX = Main.SmartInteractX >= 0 ? Main.SmartInteractX : (int)(Main.MouseWorld.X / 16f);
		int tileY = Main.SmartInteractY >= 0 ? Main.SmartInteractY : (int)(Main.MouseWorld.Y / 16f);
		Tile tile = Framing.GetTileSafely(tileX, tileY);
		if (!tile.HasTile || tile.TileType != TileID.Campfire)
		{
			return;
		}

		Main.mouseRightRelease = false;
		Point16 topLeft = SoulTransactions.GetCampfireTopLeft(tileX, tileY);
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			ModPacket packet = Mod.GetPacket();
			packet.Write((byte)SoulMessageType.RequestCampfireTransformation);
			packet.Write(topLeft.X);
			packet.Write(topLeft.Y);
			packet.Send();
		}
		else
		{
			SoulTransactions.TryTransformCampfire(player, topLeft);
		}
	}
}
