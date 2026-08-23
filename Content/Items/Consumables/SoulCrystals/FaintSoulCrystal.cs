using Terraria.ID;

namespace SoulsOfTerra.Content.Items.Consumables.SoulCrystals;

public class FaintSoulCrystal : SoulCrystalItem
{
	public override long SoulValue => 1_000;
	protected override int CrystalTier => 1;
	protected override int CrystalRarity => ItemRarityID.Blue;
}
