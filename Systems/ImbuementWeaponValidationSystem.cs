using System;
using System.Collections.Generic;
using SoulsOfTerra.Common;
using SoulsOfTerra.Content.Items.Weapons;
using Terraria;
using Terraria.ModLoader;

namespace SoulsOfTerra.Systems;

public class ImbuementWeaponValidationSystem : ModSystem
{
	private const string WeaponNamespace = "SoulsOfTerra.Content.Items.Weapons";

	public override void PostSetupContent()
	{
		HashSet<string> ids = new(StringComparer.Ordinal);
		HashSet<int> outputs = new();
		HashSet<(int Input, int Essence)> combinations = new();
		Dictionary<int, ModItem> modItems = new();
		foreach (ModItem item in Mod.GetContent<ModItem>())
		{
			modItems[item.Type] = item;
		}

		foreach (EssenceImbuementDefinition definition in EssenceImbuementRegistry.Definitions)
		{
			if (!ids.Add(definition.Id))
			{
				throw new InvalidOperationException($"Duplicate imbuement ID '{definition.Id}'.");
			}

			if (definition.InputItemTypes.Length == 0)
			{
				throw new InvalidOperationException($"Imbuement '{definition.Id}' has no input weapons.");
			}

			if (!outputs.Add(definition.OutputItemType))
			{
				throw new InvalidOperationException($"Multiple imbuements produce item type {definition.OutputItemType}.");
			}

			if (!SoulEssenceRegistry.TryFindByItemType(definition.EssenceItemType, out _))
			{
				throw new InvalidOperationException($"Imbuement '{definition.Id}' references an unregistered essence.");
			}

			if (!modItems.TryGetValue(definition.OutputItemType, out ModItem output)
				|| output is not ImbuementWeaponItem)
			{
				throw new InvalidOperationException($"Imbuement '{definition.Id}' must produce an ImbuementWeaponItem.");
			}

			foreach (int inputType in definition.InputItemTypes)
			{
				if (!combinations.Add((inputType, definition.EssenceItemType)))
				{
					throw new InvalidOperationException($"Input {inputType} and essence {definition.EssenceItemType} are registered more than once.");
				}
			}
		}

		foreach (ModItem item in modItems.Values)
		{
			bool livesInWeaponNamespace = item.GetType().Namespace?.StartsWith(WeaponNamespace, StringComparison.Ordinal) == true;
			if (livesInWeaponNamespace && item is not ImbuementWeaponItem)
			{
				throw new InvalidOperationException($"Weapon '{item.FullName}' must inherit ImbuementWeaponItem.");
			}

			if (item is ImbuementWeaponItem && !outputs.Contains(item.Type))
			{
				throw new InvalidOperationException($"Weapon '{item.FullName}' is missing an imbuement registry entry.");
			}
		}
	}

	public override void PostAddRecipes()
	{
		for (int recipeIndex = 0; recipeIndex < Recipe.numRecipes; recipeIndex++)
		{
			Recipe recipe = Main.recipe[recipeIndex];
			if (EssenceImbuementRegistry.IsRegisteredOutput(recipe.createItem.type))
			{
				throw new InvalidOperationException($"Imbuement weapon '{recipe.createItem.Name}' cannot have a crafting recipe.");
			}
		}
	}
}
