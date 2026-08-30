using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using SoulsOfTerra.Common.WorldGeneration;
using SoulsOfTerra.Content.Bosses.SealedCongregation;
using SoulsOfTerra.Content.Items.Access;
using SoulsOfTerra.Content.Projectiles;
using SoulsOfTerra.Content.Tiles;
using SoulsOfTerra.Content.Walls;
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
	private const int EntranceDepth = 36;
	private const int StructurePadding = 12;
	private const string CourtStructurePath = "Assets/Structures/BuriedCourt.txt";

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
		int centerX = Math.Clamp(Main.spawnTileX, OuterWidth / 2 + EntranceDepth + 30,
			Main.maxTilesX - OuterWidth / 2 - 30);
		int preferredCenterY = Math.Clamp((int)((Main.worldSurface + Main.rockLayer) * 0.5), OuterHeight / 2 + EntranceHeight + 30,
			Main.maxTilesY - OuterHeight / 2 - 200);
		CourtBounds = FindCourtPlacement(centerX, preferredCenterY);
		CombatBounds = new Rectangle(CourtBounds.X + 12, CourtBounds.Y + 12, CombatWidth, CombatHeight);

		PlaceCourtStructure();
		BuildEntrance();
		PlaceDais();
		PlaceCourtLighting();
		FrameCourtTiles();

		Rectangle protectedBounds = GetGenerationBounds(CourtBounds);
		GenVars.structures.AddProtectedStructure(protectedBounds, StructurePadding);
		Generated = true;
	}

	private static void PlaceCourtStructure()
	{
		ModStructure structure = ModStructure.Load(ModContent.GetInstance<SoulsOfTerra>(), CourtStructurePath);
		if (structure.Width != OuterWidth || structure.Height != OuterHeight)
		{
			throw new InvalidDataException("The Buried Court structure asset does not match its declared world bounds.");
		}

		Dictionary<char, ushort> tilePalette = new()
		{
			['B'] = (ushort)ModContent.TileType<CourtBrickUnsafe>()
		};
		Dictionary<char, ushort> wallPalette = new()
		{
			['C'] = (ushort)ModContent.WallType<CourtWallUnsafe>(),
			['R'] = (ushort)ModContent.WallType<CourtReliefWallUnsafe>(),
			['A'] = (ushort)ModContent.WallType<CourtAccentWallUnsafe>()
		};
		structure.Place(CourtBounds.Location, tilePalette, wallPalette);
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
			Rectangle occupiedArea = GetGenerationBounds(candidate);
			if (GenVars.structures.CanPlace(occupiedArea, StructurePadding))
			{
				return candidate;
			}
		}

		return new Rectangle(centerX - OuterWidth / 2, preferredCenterY - OuterHeight / 2, OuterWidth, OuterHeight);
	}

	private static Rectangle GetGenerationBounds(Rectangle courtBounds)
	{
		return new Rectangle(courtBounds.Left - EntranceDepth, courtBounds.Top - EntranceHeight,
			courtBounds.Width + EntranceDepth, courtBounds.Height + EntranceHeight);
	}

	private static void BuildEntrance()
	{
		int passageLeft = CourtBounds.Left - EntranceDepth + 6;
		const int passageWidth = 10;
		int passageTop = CourtBounds.Top - EntranceHeight;
		int floorY = CombatBounds.Bottom;
		int antechamberTop = floorY - 17;
		ushort brickType = (ushort)ModContent.TileType<CourtBrickUnsafe>();
		ushort wallType = (ushort)ModContent.WallType<CourtWallUnsafe>();

		// A masonry shaft opens into an exterior antechamber rather than the combat chamber.
		for (int x = passageLeft; x < passageLeft + passageWidth; x++)
		{
			for (int y = passageTop; y < antechamberTop; y++)
			{
				Tile tile = Main.tile[x, y];
				tile.ClearEverything();
				tile.LiquidAmount = 0;
				tile.WallType = wallType;
				if (x < passageLeft + 2 || x >= passageLeft + passageWidth - 2)
				{
					SetTile(x, y, brickType);
				}
			}
		}

		int antechamberLeft = CourtBounds.Left - EntranceDepth;
		for (int x = antechamberLeft; x < CourtBounds.Left; x++)
		{
			for (int y = antechamberTop; y < floorY; y++)
			{
				Tile tile = Main.tile[x, y];
				tile.ClearEverything();
				tile.LiquidAmount = 0;
				tile.WallType = wallType;
			}

			for (int y = floorY; y < floorY + 4; y++)
			{
				SetTile(x, y, brickType);
			}
		}

		for (int x = antechamberLeft; x < CourtBounds.Left; x++)
		{
			bool shaftOpening = x >= passageLeft + 2 && x < passageLeft + passageWidth - 2;
			if (!shaftOpening)
			{
				for (int y = antechamberTop - 3; y < antechamberTop; y++)
				{
					SetTile(x, y, brickType);
				}
			}
		}

		for (int x = antechamberLeft; x < antechamberLeft + 3; x++)
		{
			for (int y = antechamberTop; y < floorY; y++)
			{
				SetTile(x, y, brickType);
			}
		}

		// The stair finishes outside the floor-level arch and never enters combat space.
		int stairStartX = passageLeft + passageWidth - 2;
		int stairEndX = CourtBounds.Left - 1;
		for (int x = stairStartX; x <= stairEndX; x++)
		{
			int stepY = Math.Min(floorY - 1, antechamberTop + 5 + (x - stairStartX) / 2);
			for (int y = stepY; y < floorY; y++)
			{
				SetTile(x, y, brickType);
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

		// The art supplies the apparent dais while the reliquary sits on the uninterrupted floor.
		DaisTopLeft = new Point16(centerX - 1, floorY - 2);
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
		// Boreal fixtures spread cool light across both the floor and upper vault.
		int[] lampOffsets = { 20, 40, 60, 76, 91, 107, 127, 147 };
		foreach (int offsetX in lampOffsets)
		{
			PlaceFurniture(ItemID.BorealWoodLamp, CourtBounds.Left + offsetX, CombatBounds.Bottom - 1);
		}

		int[] chandelierOffsets = { 24, 48, 72, 95, 119, 143 };
		int anchorY = CombatBounds.Top - 4;
		ushort brickType = (ushort)ModContent.TileType<CourtBrickUnsafe>();
		foreach (int offsetX in chandelierOffsets)
		{
			int centerX = CourtBounds.Left + offsetX;
			for (int x = centerX - 1; x <= centerX + 1; x++)
			{
				SetTile(x, anchorY, brickType);
			}

			PlaceFurniture(ItemID.BorealWoodChandelier, centerX, anchorY + 1);
		}
	}

	private static void PlaceFurniture(int itemType, int originX, int originY)
	{
		Item furniture = new(itemType);
		WorldGen.PlaceTile(originX, originY, furniture.createTile, true, true, -1, furniture.placeStyle);
	}

	private static void FrameCourtTiles()
	{
		int top = CourtBounds.Top - EntranceHeight;
		for (int x = CourtBounds.Left - EntranceDepth; x < CourtBounds.Right; x++)
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
