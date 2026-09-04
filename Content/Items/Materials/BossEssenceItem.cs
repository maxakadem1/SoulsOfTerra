using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Common;
using SoulsOfTerra.Common.Rendering;
using SoulsOfTerra.Content.Rarities;
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

	public override void ModifyTooltips(List<TooltipLine> tooltips)
	{
		TooltipLine name = tooltips.Find(line => line.Mod == "Terraria" && line.Name == "ItemName");
		if (name is not null)
		{
			name.OverrideColor = ModContent.GetInstance<BossEssenceRarity>().RarityColor;
		}

		int firstLoreIndex = tooltips.FindIndex(line => line.Name.StartsWith("Tooltip"));
		if (firstLoreIndex < 0)
		{
			firstLoreIndex = tooltips.Count;
		}
		else
		{
			for (int index = firstLoreIndex; index < tooltips.Count; index++)
			{
				if (tooltips[index].Name.StartsWith("Tooltip"))
				{
					tooltips[index].OverrideColor = new Color(255, 24, 24);
				}
			}
		}

		tooltips.Insert(firstLoreIndex, new TooltipLine(Mod, "EssenceUses",
			"Used for grafting, imbuement, and Soulspells") { OverrideColor = Color.White });
		tooltips.Insert(firstLoreIndex + 1, new TooltipLine(Mod, "WeaponInfusion",
			EssencePathRegistry.GetInventorySummary(Type)) { OverrideColor = new Color(80, 225, 205) });
		tooltips.Insert(firstLoreIndex + 2, new TooltipLine(Mod, "Mutation",
			MutationRegistry.GetInventorySummary(Type)) { OverrideColor = new Color(190, 105, 235) });
	}
}
