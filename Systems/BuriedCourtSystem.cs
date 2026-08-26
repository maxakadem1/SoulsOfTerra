using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using SoulsOfTerra.Content.Bosses.SealedCongregation;
using SoulsOfTerra.Content.Items.Access;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Generation;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.WorldBuilding;

namespace SoulsOfTerra.Systems;

public class BuriedCourtSystem : ModSystem
{
	public const int OuterWidth = 168;
	public const int OuterHeight = 84;
	public const int CombatWidth = 144;
	public const int CombatHeight = 60;
	private const int EntranceHeight = 40;

	public static Rectangle CourtBounds { get; private set; }
	public static Rectangle CombatBounds { get; private set; }
	public static Point16 DaisTopLeft { get; private set; }
	public static bool Generated { get; private set; }
	public static bool DownedSealedCongregation { get; private set; }

	public override void OnWorldLoad() => ResetWorldState();
	public override void OnWorldUnload() => ResetWorldState();
	public override void PreWorldGen() => ResetWorldState();

	public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
	{
		int insertionIndex = tasks.FindLastIndex(pass => pass.Name == "Final Cleanup");
		if (insertionIndex < 0)
		{
			insertionIndex = tasks.Count;
		}

		tasks.Insert(insertionIndex, new PassLegacy("Souls of Terra: Buried Court", GenerateBuriedCourt));
	}

	public override void SaveWorldData(TagCompound tag)
	{
		if (Generated)
		{
			tag["buriedCourtGenerated"] = true;
			tag["buriedCourtX"] = CourtBounds.X;
			tag["buriedCourtY"] = CourtBounds.Y;
			tag["buriedCourtDaisX"] = (int)DaisTopLeft.X;
			tag["buriedCourtDaisY"] = (int)DaisTopLeft.Y;
		}

		if (DownedSealedCongregation)
		{
			tag["downedSealedCongregation"] = true;
		}
	}

	public override void LoadWorldData(TagCompound tag)
	{
		Generated = tag.GetBool("buriedCourtGenerated");
		DownedSealedCongregation = tag.GetBool("downedSealedCongregation");
		if (!Generated)
		{
			return;
		}

		CourtBounds = new Rectangle(tag.GetInt("buriedCourtX"), tag.GetInt("buriedCourtY"), OuterWidth, OuterHeight);
		CombatBounds = new Rectangle(CourtBounds.X + 12, CourtBounds.Y + 12, CombatWidth, CombatHeight);
		DaisTopLeft = new Point16(tag.GetInt("buriedCourtDaisX"), tag.GetInt("buriedCourtDaisY"));
	}

	public override void NetSend(BinaryWriter writer)
	{
		writer.Write(Generated);
		writer.Write(DownedSealedCongregation);
		if (Generated)
		{
			writer.Write((short)CourtBounds.X);
			writer.Write((short)CourtBounds.Y);
			writer.Write(DaisTopLeft.X);
			writer.Write(DaisTopLeft.Y);
		}
	}

	public override void NetReceive(BinaryReader reader)
	{
		Generated = reader.ReadBoolean();
		DownedSealedCongregation = reader.ReadBoolean();
		if (!Generated)
		{
			return;
		}

		CourtBounds = new Rectangle(reader.ReadInt16(), reader.ReadInt16(), OuterWidth, OuterHeight);
		CombatBounds = new Rectangle(CourtBounds.X + 12, CourtBounds.Y + 12, CombatWidth, CombatHeight);
		DaisTopLeft = new Point16(reader.ReadInt16(), reader.ReadInt16());
	}

	public static bool IsDaisTile(int i, int j)
	{
		return Generated && i >= DaisTopLeft.X && i < DaisTopLeft.X + 3
			&& j >= DaisTopLeft.Y && j < DaisTopLeft.Y + 2;
	}

	public static bool IsInsideCombatArea(Vector2 worldPosition, int marginTiles = 0)
	{
		Rectangle expanded = new(
			(CombatBounds.X - marginTiles) * 16,
			(CombatBounds.Y - marginTiles) * 16,
			(CombatBounds.Width + marginTiles * 2) * 16,
			(CombatBounds.Height + marginTiles * 2) * 16);
		return Generated && expanded.Contains(worldPosition.ToPoint());
	}

	public static Vector2 GetBossSpawnPosition()
	{
		return new Vector2(CombatBounds.Center.X * 16f, (CombatBounds.Top + 20) * 16f);
	}

	public static bool TrySummonBoss(Player player, Point16 clickedTile)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient || !Generated || !NPC.downedBoss3
			|| !player.active || player.dead || !IsDaisTile(clickedTile.X, clickedTile.Y)
			|| player.HeldItem.type != ModContent.ItemType<WardensFragment>()
			|| Vector2.DistanceSquared(player.Center, clickedTile.ToWorldCoordinates()) > 12f * 16f * 12f * 16f
			|| NPC.AnyNPCs(ModContent.NPCType<SealedCongregationBoss>()))
		{
			return false;
		}

		int index = NPC.NewNPC(player.GetSource_TileInteraction(clickedTile.X, clickedTile.Y),
			(int)GetBossSpawnPosition().X, (int)GetBossSpawnPosition().Y, ModContent.NPCType<SealedCongregationBoss>());
		if (index < 0 || index >= Main.maxNPCs)
		{
			return false;
		}

		Main.npc[index].target = player.whoAmI;
		Main.npc[index].netUpdate = true;
		if (Main.netMode == NetmodeID.Server)
		{
			NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, index);
		}

		return true;
	}

	public static void MarkBossDefeated()
	{
		DownedSealedCongregation = true;
		if (Main.netMode == NetmodeID.Server)
		{
			NetMessage.SendData(MessageID.WorldData);
		}
	}

	private static void ResetWorldState()
	{
		CourtBounds = Rectangle.Empty;
		CombatBounds = Rectangle.Empty;
		DaisTopLeft = Point16.NegativeOne;
		Generated = false;
		DownedSealedCongregation = false;
	}

	private static void GenerateBuriedCourt(GenerationProgress progress, GameConfiguration configuration)
	{
		progress.Message = "Burying a forgotten court";
		int centerX = Math.Clamp(Main.spawnTileX, OuterWidth / 2 + 30, Main.maxTilesX - OuterWidth / 2 - 30);
		int centerY = Math.Clamp((int)((Main.worldSurface + Main.rockLayer) * 0.5), OuterHeight / 2 + EntranceHeight + 30,
			Main.maxTilesY - OuterHeight / 2 - 200);
		CourtBounds = new Rectangle(centerX - OuterWidth / 2, centerY - OuterHeight / 2, OuterWidth, OuterHeight);
		CombatBounds = new Rectangle(CourtBounds.X + 12, CourtBounds.Y + 12, CombatWidth, CombatHeight);

		CarveCastleShell();
		BuildRuins();
		BuildEntrance();
		FrameCourtTiles();
		PlaceDais();
		Generated = true;
	}

	private static void CarveCastleShell()
	{
		for (int x = CourtBounds.Left; x < CourtBounds.Right; x++)
		{
			for (int y = CourtBounds.Top; y < CourtBounds.Bottom; y++)
			{
				Tile tile = Main.tile[x, y];
				tile.ClearEverything();
				tile.WallType = WallID.GrayBrick;
				bool shell = x < CourtBounds.Left + 4 || x >= CourtBounds.Right - 4
					|| y < CourtBounds.Top + 4 || y >= CourtBounds.Bottom - 8;
				if (shell)
				{
					SetTile(x, y, TileID.GrayBrick);
				}
			}
		}
	}

	private static void BuildRuins()
	{
		int floorY = CombatBounds.Bottom;
		int[] pillarOffsets = { 8, 30, CombatBounds.Width - 33, CombatBounds.Width - 11 };
		foreach (int offset in pillarOffsets)
		{
			int pillarX = CombatBounds.Left + offset;
			bool outerPillar = offset == 8 || offset == CombatBounds.Width - 11;
			int pillarTop = outerPillar ? CombatBounds.Top + 14 : CombatBounds.Top + 25;
			for (int x = pillarX; x < pillarX + 3; x++)
			{
				for (int y = pillarTop; y < floorY; y++)
				{
					if ((y + x) % 17 != 0)
					{
						SetTile(x, y, TileID.StoneSlab);
					}
				}
			}
		}

		// Broken side galleries frame the fight without obstructing the central floor.
		for (int x = CombatBounds.Left + 4; x < CombatBounds.Left + 38; x++)
		{
			if (x % 9 != 0)
			{
				SetTile(x, CombatBounds.Top + 31, TileID.GrayBrick);
			}
		}

		for (int x = CombatBounds.Right - 38; x < CombatBounds.Right - 4; x++)
		{
			if (x % 10 != 0)
			{
				SetTile(x, CombatBounds.Top + 27, TileID.GrayBrick);
			}
		}

		// A fractured central arch foreshadows the later Soulless encounter.
		int centerX = CombatBounds.Center.X;
		for (int y = CombatBounds.Top + 4; y < CombatBounds.Top + 18; y++)
		{
			SetTile(centerX - 18, y, TileID.StoneSlab);
			SetTile(centerX + 18, y, TileID.StoneSlab);
		}
		for (int x = centerX - 18; x <= centerX + 12; x++)
		{
			if (x < centerX - 2 || x > centerX + 4)
			{
				SetTile(x, CombatBounds.Top + 4 + Math.Abs(x - centerX) / 5, TileID.StoneSlab);
			}
		}
	}

	private static void BuildEntrance()
	{
		int passageLeft = CourtBounds.Left + 22;
		int passageTop = CourtBounds.Top - EntranceHeight;
		for (int x = passageLeft; x < passageLeft + 12; x++)
		{
			for (int y = passageTop; y < CourtBounds.Top + 18; y++)
			{
				Tile tile = Main.tile[x, y];
				tile.ClearEverything();
				tile.WallType = WallID.GrayBrick;
				if (x < passageLeft + 2 || x >= passageLeft + 10)
				{
					SetTile(x, y, TileID.GrayBrick);
				}
			}
		}

		// Breakable rubble hides the passage without progression-locking it.
		for (int x = passageLeft + 2; x < passageLeft + 10; x++)
		{
			for (int y = passageTop; y < passageTop + 5; y++)
			{
				if (WorldGen.genRand.NextBool(3, 4))
				{
					SetTile(x, y, TileID.Stone);
				}
			}
		}
	}

	private static void PlaceDais()
	{
		DaisTopLeft = new Point16(CombatBounds.Center.X - 1, CombatBounds.Bottom - 2);
		for (int x = DaisTopLeft.X - 4; x < DaisTopLeft.X + 7; x++)
		{
			SetTile(x, CombatBounds.Bottom, TileID.StoneSlab);
		}

		// Direct framing keeps DaisTopLeft stable; vanilla Place3x2 uses an anchor rather than a top-left point.
		for (int x = 0; x < 3; x++)
		{
			for (int y = 0; y < 2; y++)
			{
				Tile tile = Main.tile[DaisTopLeft.X + x, DaisTopLeft.Y + y];
				tile.ClearEverything();
				tile.HasTile = true;
				tile.TileType = TileID.DemonAltar;
				tile.TileFrameX = (short)(x * 18);
				tile.TileFrameY = (short)(y * 18);
			}
		}
	}

	private static void FrameCourtTiles()
	{
		int top = CourtBounds.Top - EntranceHeight;
		for (int x = CourtBounds.Left; x < CourtBounds.Right; x++)
		{
			for (int y = top; y < CourtBounds.Bottom; y++)
			{
				WorldGen.SquareTileFrame(x, y, true);
				WorldGen.SquareWallFrame(x, y, true);
			}
		}
	}

	private static void SetTile(int x, int y, ushort tileType)
	{
		Tile tile = Main.tile[x, y];
		tile.HasTile = true;
		tile.TileType = tileType;
		tile.TileFrameX = 0;
		tile.TileFrameY = 0;
		tile.IsHalfBlock = false;
		tile.Slope = 0;
	}
}
