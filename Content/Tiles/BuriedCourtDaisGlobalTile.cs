using Microsoft.Xna.Framework;
using SoulsOfTerra.Content.Items.Access;
using SoulsOfTerra.Systems;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Tiles;

public class BuriedCourtDaisGlobalTile : GlobalTile
{
	public override void RightClick(int i, int j, int type)
	{
		Player player = Main.LocalPlayer;
		if (type != ModContent.TileType<SoulShrineTile>() || !BuriedCourtSystem.IsDaisTile(i, j)
			|| player.HeldItem.type != ModContent.ItemType<WardensFragment>())
		{
			return;
		}

		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			ModPacket packet = ModContent.GetInstance<SoulsOfTerra>().GetPacket();
			packet.Write((byte)SoulMessageType.RequestCongregationSummon);
			packet.Write((short)i);
			packet.Write((short)j);
			packet.Send();
		}
		else
		{
			BuriedCourtSystem.TrySummonBoss(player, new Point16(i, j));
		}

	}

	public override bool CanKillTile(int i, int j, int type, ref bool blockDamaged)
	{
		return !BuriedCourtSystem.IsDaisStructureTile(i, j);
	}

	public override bool CanExplode(int i, int j, int type)
	{
		return !BuriedCourtSystem.IsDaisStructureTile(i, j);
	}

	public override bool CanReplace(int i, int j, int type, int tileTypeBeingPlaced)
	{
		return !BuriedCourtSystem.IsDaisStructureTile(i, j);
	}

	public override bool Slope(int i, int j, int type)
	{
		return !BuriedCourtSystem.IsDaisStructureTile(i, j);
	}
}
