using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace SoulsOfTerra.Common.WorldGeneration;

/// <summary>Loads Souls of Terra's dependency-free, tile-grid structure format.</summary>
public sealed class ModStructure
{
	private const string Header = "SOTSTRUCT 1";
	private readonly string[] tileRows;
	private readonly string[] wallRows;

	public int Width { get; }
	public int Height { get; }

	private ModStructure(int width, int height, string[] tileRows, string[] wallRows)
	{
		Width = width;
		Height = height;
		this.tileRows = tileRows;
		this.wallRows = wallRows;
	}

	public static ModStructure Load(Mod mod, string assetPath)
	{
		using StringReader reader = new(Encoding.UTF8.GetString(mod.GetFileBytes(assetPath)));
		RequireLine(reader, Header, assetPath);

		string[] size = (reader.ReadLine() ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
		if (size.Length != 3 || size[0] != "size" || !int.TryParse(size[1], out int width)
			|| !int.TryParse(size[2], out int height) || width <= 0 || height <= 0)
		{
			throw new InvalidDataException($"{assetPath} has an invalid size declaration.");
		}

		RequireLine(reader, "[Tiles]", assetPath);
		string[] tileRows = ReadLayer(reader, width, height, assetPath, "tile");
		RequireLine(reader, "[Walls]", assetPath);
		string[] wallRows = ReadLayer(reader, width, height, assetPath, "wall");
		return new ModStructure(width, height, tileRows, wallRows);
	}

	public void Place(Point origin, IReadOnlyDictionary<char, ushort> tilePalette,
		IReadOnlyDictionary<char, ushort> wallPalette)
	{
		for (int localY = 0; localY < Height; localY++)
		{
			for (int localX = 0; localX < Width; localX++)
			{
				int worldX = origin.X + localX;
				int worldY = origin.Y + localY;
				if (!WorldGen.InWorld(worldX, worldY, 2))
				{
					throw new InvalidOperationException("A structure placement extended outside the world.");
				}

				Tile tile = Main.tile[worldX, worldY];
				tile.ClearEverything();
				tile.LiquidAmount = 0;

				char tileSymbol = tileRows[localY][localX];
				if (tileSymbol != '.')
				{
					tile.ResetToType(ResolveSymbol(tilePalette, tileSymbol, "tile"));
				}

				char wallSymbol = wallRows[localY][localX];
				if (wallSymbol != '.')
				{
					tile.WallType = ResolveSymbol(wallPalette, wallSymbol, "wall");
				}
			}
		}
	}

	private static string[] ReadLayer(StringReader reader, int width, int height, string assetPath, string layerName)
	{
		string[] rows = new string[height];
		for (int row = 0; row < height; row++)
		{
			rows[row] = reader.ReadLine() ?? throw new InvalidDataException(
				$"{assetPath} ended inside its {layerName} layer.");
			if (rows[row].Length != width)
			{
				throw new InvalidDataException(
					$"{assetPath} {layerName} row {row} is {rows[row].Length} cells wide; expected {width}.");
			}
		}

		return rows;
	}

	private static ushort ResolveSymbol(IReadOnlyDictionary<char, ushort> palette, char symbol, string layerName)
	{
		if (!palette.TryGetValue(symbol, out ushort type))
		{
			throw new InvalidDataException($"Unknown {layerName} symbol '{symbol}'.");
		}

		return type;
	}

	private static void RequireLine(StringReader reader, string expected, string assetPath)
	{
		string actual = reader.ReadLine() ?? string.Empty;
		if (actual != expected)
		{
			throw new InvalidDataException($"{assetPath} expected '{expected}' but found '{actual}'.");
		}
	}
}
