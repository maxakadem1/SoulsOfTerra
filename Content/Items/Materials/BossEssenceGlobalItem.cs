using SoulsOfTerra.Content.Rarities;
using Terraria;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Items.Materials;

public sealed class BossEssenceGlobalItem : GlobalItem
{
	public override void SetDefaults(Item entity)
	{
		if (entity.ModItem is BossEssenceItem)
		{
			entity.rare = ModContent.RarityType<BossEssenceRarity>();
		}
	}
}
