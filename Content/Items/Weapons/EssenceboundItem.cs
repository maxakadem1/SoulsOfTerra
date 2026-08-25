using SoulsOfTerra.Content.Rarities;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Items.Weapons;

public abstract class EssenceboundItem : ModItem
{
	public sealed override void SetDefaults()
	{
		SetEssenceboundDefaults();
		// Central enforcement keeps every future Essencebound name visually consistent.
		Item.rare = ModContent.RarityType<EssenceboundRarity>();
	}

	protected abstract void SetEssenceboundDefaults();
}
