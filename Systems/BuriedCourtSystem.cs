using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using SoulsOfTerra.Content.Bosses.SealedCongregation;
using SoulsOfTerra.Content.Items.Access;
using SoulsOfTerra.Content.Projectiles;
using SoulsOfTerra.Content.Tiles;
using Terraria;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.GameContent.Generation;
using Terraria.ID;
using Terraria.IO;
using Terraria.Localization;
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
	private const int StructurePadding = 12;

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
		MigrateLegacyDais();
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

	public static bool IsDaisStructureTile(int i, int j)
	{
		// Protect the 3x2 reliquary and the complete three-block support row beneath it.
		return Generated && i >= DaisTopLeft.X && i < DaisTopLeft.X + 3
			&& j >= DaisTopLeft.Y && j < DaisTopLeft.Y + 3;
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

	public static bool IsInsideCourt(Vector2 worldPosition)
	{
		Rectangle worldBounds = new(CourtBounds.X * 16, CourtBounds.Y * 16,
			CourtBounds.Width * 16, CourtBounds.Height * 16);
		return Generated && worldBounds.Contains(worldPosition.ToPoint());
	}

	public static Vector2 GetBossSpawnPosition()
	{
		return new Vector2(CombatBounds.Center.X * 16f, (CombatBounds.Top + 20) * 16f);
	}

	public static Vector2 GetDaisEffectPosition()
	{
		// Keep every shader and ritual layer aligned to the measured socket in the custom-drawn art.
		return DaisTopLeft.ToWorldCoordinates(SoulShrineTile.SocketWorldOffset.X, SoulShrineTile.SocketWorldOffset.Y);
	}

	public static bool TrySummonBoss(Player player, Point16 clickedTile)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient || !Generated || !NPC.downedBoss3
			|| !player.active || player.dead || !IsDaisTile(clickedTile.X, clickedTile.Y)
			|| player.HeldItem.type != ModContent.ItemType<WardensFragment>()
			|| Vector2.DistanceSquared(player.Center, clickedTile.ToWorldCoordinates()) > 12f * 16f * 12f * 16f
			|| NPC.AnyNPCs(ModContent.NPCType<SealedCongregationBoss>())
			|| CongregationSummonRitualProjectile.IsRitualActive())
		{
			return false;
		}

		int index = Projectile.NewProjectile(player.GetSource_TileInteraction(clickedTile.X, clickedTile.Y),
			GetDaisEffectPosition(), Vector2.Zero, ModContent.ProjectileType<CongregationSummonRitualProjectile>(),
			0, 0f, player.whoAmI, 0f, player.whoAmI);
		if (index < 0 || index >= Main.maxProjectiles)
		{
			return false;
		}

		BroadcastCourtMessage("Mods.SoulsOfTerra.Dialogue.Court.ReliquaryAccepts", new Color(112, 232, 211));
		return true;
	}

	public static bool SpawnBossFromRitual(int preferredPlayer)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient || NPC.AnyNPCs(ModContent.NPCType<SealedCongregationBoss>()))
		{
			return false;
		}

		Vector2 spawnPosition = GetBossSpawnPosition();
		int index = NPC.NewNPC(new EntitySource_Misc("SoulsOfTerra:CongregationRitual"),
			(int)spawnPosition.X, (int)spawnPosition.Y, ModContent.NPCType<SealedCongregationBoss>());
		if (index < 0 || index >= Main.maxNPCs)
		{
			return false;
		}

		bool preferredPlayerIsValid = preferredPlayer >= 0 && preferredPlayer < Main.maxPlayers
			&& Main.player[preferredPlayer].active && !Main.player[preferredPlayer].dead;
		Main.npc[index].target = preferredPlayerIsValid
			? preferredPlayer
			: Player.FindClosest(spawnPosition, 1, 1);
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

	public static void BroadcastCourtMessage(string localizationKey, Color color)
	{
		if (Main.netMode == NetmodeID.Server)
		{
			ChatHelper.BroadcastChatMessage(NetworkText.FromKey(localizationKey), color);
		}
		else if (Main.netMode == NetmodeID.SinglePlayer)
		{
			Main.NewText(Language.GetTextValue(localizationKey), color);
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
		int preferredCenterY = Math.Clamp((int)((Main.worldSurface + Main.rockLayer) * 0.5), OuterHeight / 2 + EntranceHeight + 30,
			Main.maxTilesY - OuterHeight / 2 - 200);
		CourtBounds = FindCourtPlacement(centerX, preferredCenterY);
		CombatBounds = new Rectangle(CourtBounds.X + 12, CourtBounds.Y + 12, CombatWidth, CombatHeight);

		CarveCastleShell();
		BuildGrandHall();
		BuildEntrance();
		PlaceDais();
		PlaceCourtLighting();
		FrameCourtTiles();

		Rectangle protectedBounds = new(CourtBounds.X, CourtBounds.Y - EntranceHeight, CourtBounds.Width,
			CourtBounds.Height + EntranceHeight);
		GenVars.structures.AddProtectedStructure(protectedBounds, StructurePadding);
		Generated = true;
	}

	private static Rectangle FindCourtPlacement(int centerX, int preferredCenterY)
	{
		// Stay directly beneath spawn, but avoid overwriting another protected world structure when possible.
		int[] verticalOffsets = { 0, -24, 24, -48, 48, -72, 72 };
		foreach (int offset in verticalOffsets)
		{
			int centerY = Math.Clamp(preferredCenterY + offset, OuterHeight / 2 + EntranceHeight + 30,
				Main.maxTilesY - OuterHeight / 2 - 200);
			Rectangle candidate = new(centerX - OuterWidth / 2, centerY - OuterHeight / 2, OuterWidth, OuterHeight);
			Rectangle occupiedArea = new(candidate.X, candidate.Y - EntranceHeight, candidate.Width,
				candidate.Height + EntranceHeight);
			if (GenVars.structures.CanPlace(occupiedArea, StructurePadding))
			{
				return candidate;
			}
		}

		return new Rectangle(centerX - OuterWidth / 2, preferredCenterY - OuterHeight / 2, OuterWidth, OuterHeight);
	}

	private static void CarveCastleShell()
	{
		int floorY = CombatBounds.Bottom;
		for (int x = CourtBounds.Left; x < CourtBounds.Right; x++)
		{
			for (int y = CourtBounds.Top; y < CourtBounds.Bottom; y++)
			{
				Tile tile = Main.tile[x, y];
				tile.ClearEverything();
				tile.LiquidAmount = 0;
				tile.WallType = WallID.GrayBrick;
				bool shell = x < CourtBounds.Left + 5 || x >= CourtBounds.Right - 5
					|| y < CourtBounds.Top + 5 || y >= floorY;
				if (shell)
				{
					SetTile(x, y, y >= floorY ? TileID.StoneSlab : TileID.GrayBrick);
				}
			}
		}
	}

	private static void BuildGrandHall()
	{
		BuildRecessedWallBays();
		BuildVaultRibs();
		BuildSideGalleries();
		BuildThroneRecess();
		BuildControlledCollapse();
	}

	private static void BuildRecessedWallBays()
	{
		int bayTop = CombatBounds.Top + 7;
		int shoulderY = CombatBounds.Top + 18;
		const int bayWidth = 18;
		for (int left = CombatBounds.Left + 5; left + bayWidth < CombatBounds.Right - 5; left += 22)
		{
			int right = left + bayWidth;
			int center = (left + right) / 2;
			for (int y = shoulderY; y < CombatBounds.Bottom; y++)
			{
				SetWall(left, y, WallID.StoneSlab);
				SetWall(right, y, WallID.StoneSlab);
			}

			for (int distance = 0; distance <= bayWidth / 2; distance++)
			{
				int y = bayTop + distance * (shoulderY - bayTop) / (bayWidth / 2);
				SetWall(center - distance, y, WallID.StoneSlab);
				SetWall(center + distance, y, WallID.StoneSlab);
				SetWall(center - distance, y + 1, WallID.StoneSlab);
				SetWall(center + distance, y + 1, WallID.StoneSlab);
			}
		}
	}

	private static void BuildVaultRibs()
	{
		int apexY = CombatBounds.Top - 3;
		int springY = CombatBounds.Top + 15;
		int centerX = CombatBounds.Center.X;
		for (int distance = 0; distance <= 43; distance++)
		{
			int y = apexY + distance * (springY - apexY) / 43;
			SetTile(centerX - distance, y, TileID.StoneSlab);
			SetTile(centerX + distance, y, TileID.StoneSlab);
		}

		// Short hanging ribs suggest repeated vaults without obstructing the fight below.
		int[] ribXs = { CombatBounds.Left + 27, CombatBounds.Left + 49, CombatBounds.Right - 50, CombatBounds.Right - 28 };
		foreach (int ribX in ribXs)
		{
			for (int y = CombatBounds.Top - 1; y < CombatBounds.Top + 10; y++)
			{
				SetTile(ribX, y, TileID.GrayBrick);
			}
		}
	}

	private static void BuildSideGalleries()
	{
		int galleryY = CombatBounds.Top + 30;
		BuildGallery(CombatBounds.Left + 5, CombatBounds.Left + 36, galleryY, false);
		BuildGallery(CombatBounds.Right - 36, CombatBounds.Right - 5, galleryY - 3, true);

		BuildColumn(CombatBounds.Left + 7, CombatBounds.Top + 17, CombatBounds.Bottom);
		BuildColumn(CombatBounds.Left + 31, galleryY, CombatBounds.Bottom);
		BuildColumn(CombatBounds.Right - 34, galleryY - 3, CombatBounds.Bottom);
		BuildColumn(CombatBounds.Right - 10, CombatBounds.Top + 17, CombatBounds.Bottom);
	}

	private static void BuildGallery(int startX, int endX, int y, bool damaged)
	{
		for (int x = startX; x <= endX; x++)
		{
			bool missing = damaged && x > endX - 11 && ((x - startX) % 3 != 0);
			if (!missing)
			{
				SetTile(x, y, TileID.Platforms);
			}
		}

		for (int x = startX; x <= endX; x += 6)
		{
			for (int supportY = y + 1; supportY <= y + 3; supportY++)
			{
				SetTile(x, supportY, TileID.StoneSlab);
			}
		}
	}

	private static void BuildColumn(int centerX, int topY, int floorY)
	{
		for (int x = centerX - 1; x <= centerX + 1; x++)
		{
			for (int y = topY; y < floorY; y++)
			{
				SetTile(x, y, TileID.StoneSlab);
			}
		}

		for (int x = centerX - 3; x <= centerX + 3; x++)
		{
			SetTile(x, topY, TileID.GrayBrick);
			SetTile(x, floorY - 1, TileID.GrayBrick);
		}
	}

	private static void BuildThroneRecess()
	{
		int centerX = CombatBounds.Center.X;
		int floorY = CombatBounds.Bottom;
		int archTop = floorY - 33;
		int shoulderY = floorY - 20;
		const int halfWidth = 15;

		for (int y = shoulderY; y < floorY; y++)
		{
			SetWall(centerX - halfWidth, y, WallID.StoneSlab);
			SetWall(centerX + halfWidth, y, WallID.StoneSlab);
		}

		for (int distance = 0; distance <= halfWidth; distance++)
		{
			int y = archTop + distance * (shoulderY - archTop) / halfWidth;
			SetWall(centerX - distance, y, WallID.StoneSlab);
			SetWall(centerX + distance, y, WallID.StoneSlab);
			SetWall(centerX - distance, y + 1, WallID.StoneSlab);
			SetWall(centerX + distance, y + 1, WallID.StoneSlab);
		}
	}

	private static void BuildControlledCollapse()
	{
		// Damage is concentrated at the edges so the combat floor remains readable and fair.
		BuildRubblePile(CombatBounds.Left + 2, CombatBounds.Bottom - 1, 10, 4);
		BuildRubblePile(CombatBounds.Right - 13, CombatBounds.Bottom - 1, 11, 5);

		for (int x = CombatBounds.Right - 30; x < CombatBounds.Right - 20; x++)
		{
			int y = CombatBounds.Top + 5 + (x - (CombatBounds.Right - 30)) / 2;
			ClearTile(x, y);
			ClearTile(x, y + 1);
		}
	}

	private static void BuildRubblePile(int startX, int floorY, int width, int maxHeight)
	{
		for (int x = 0; x < width; x++)
		{
			int edgeDistance = Math.Min(x, width - 1 - x);
			int height = Math.Min(maxHeight, 1 + edgeDistance / 2 + WorldGen.genRand.Next(2));
			for (int y = 0; y < height; y++)
			{
				SetTile(startX + x, floorY - y, WorldGen.genRand.NextBool() ? TileID.GrayBrick : TileID.StoneSlab);
			}
		}
	}

	private static void BuildEntrance()
	{
		int passageLeft = CourtBounds.Left + 10;
		const int passageWidth = 10;
		int passageTop = CourtBounds.Top - EntranceHeight;
		for (int x = passageLeft; x < passageLeft + passageWidth; x++)
		{
			for (int y = passageTop; y < CourtBounds.Top + 19; y++)
			{
				Tile tile = Main.tile[x, y];
				tile.ClearEverything();
				tile.LiquidAmount = 0;
				tile.WallType = WallID.GrayBrick;
				if (x < passageLeft + 2 || x >= passageLeft + passageWidth - 2)
				{
					SetTile(x, y, TileID.StoneSlab);
				}
			}
		}

		// The shaft opens onto a descending masonry stair and gives the hall a deliberate reveal.
		int stairStartX = passageLeft + passageWidth - 2;
		int stairEndX = CombatBounds.Left + 29;
		int stairStartY = CourtBounds.Top + 17;
		for (int x = stairStartX; x <= stairEndX; x++)
		{
			int stepY = stairStartY + (x - stairStartX) / 2;
			for (int y = stepY; y <= CombatBounds.Top + 32; y++)
			{
				SetTile(x, y, TileID.GrayBrick);
			}

			for (int y = stepY - 4; y < stepY; y++)
			{
				ClearTile(x, y);
			}
		}

		// Breakable rubble conceals the discovery entrance without consuming the reusable key.
		for (int x = passageLeft + 2; x < passageLeft + passageWidth - 2; x++)
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
		int centerX = CombatBounds.Center.X;
		int floorY = CombatBounds.Bottom;
		for (int x = centerX - 12; x <= centerX + 12; x++)
		{
			SetTile(x, floorY - 1, TileID.StoneSlab);
		}
		for (int x = centerX - 8; x <= centerX + 8; x++)
		{
			SetTile(x, floorY - 2, TileID.StoneSlab);
		}

		DaisTopLeft = new Point16(centerX - 1, floorY - 4);
		PlaceShrineTiles();
	}

	private static void PlaceShrineTiles()
	{
		// Direct framing keeps DaisTopLeft stable for world persistence and summon validation.
		int shrineTileType = ModContent.TileType<SoulShrineTile>();
		for (int x = 0; x < 3; x++)
		{
			for (int y = 0; y < 2; y++)
			{
				Tile tile = Main.tile[DaisTopLeft.X + x, DaisTopLeft.Y + y];
				tile.ClearEverything();
				tile.HasTile = true;
				tile.TileType = (ushort)shrineTileType;
				tile.TileFrameX = (short)(x * 18);
				tile.TileFrameY = (short)(y * 18);
			}
		}
	}

	private static void MigrateLegacyDais()
	{
		if (!WorldGen.InWorld(DaisTopLeft.X, DaisTopLeft.Y, 2)
			|| Main.tile[DaisTopLeft.X, DaisTopLeft.Y].TileType != TileID.DemonAltar)
		{
			return;
		}

		// Existing courts retain their coordinates; only the old protected altar is replaced.
		PlaceShrineTiles();
		for (int x = 0; x < 3; x++)
		{
			for (int y = 0; y < 2; y++)
			{
				WorldGen.SquareTileFrame(DaisTopLeft.X + x, DaisTopLeft.Y + y, true);
			}
		}
	}

	private static void PlaceCourtLighting()
	{
		int[] torchXs = { CombatBounds.Left + 19, CombatBounds.Left + 43, CombatBounds.Center.X - 24,
			CombatBounds.Center.X + 24, CombatBounds.Right - 44, CombatBounds.Right - 20 };
		foreach (int x in torchXs)
		{
			WorldGen.PlaceTile(x, CombatBounds.Bottom - 18, TileID.Torches, true, true);
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

	private static void ClearTile(int x, int y)
	{
		Tile tile = Main.tile[x, y];
		tile.HasTile = false;
		tile.TileFrameX = 0;
		tile.TileFrameY = 0;
		tile.IsHalfBlock = false;
		tile.Slope = 0;
	}

	private static void SetWall(int x, int y, ushort wallType)
	{
		Main.tile[x, y].WallType = wallType;
	}
}
