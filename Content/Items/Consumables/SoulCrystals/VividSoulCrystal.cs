using Terraria.ID;

namespace SoulsOfTerra.Content.Items.Consumables.SoulCrystals;

public class VividSoulCrystal : SoulCrystalItem
{
	public override long SoulValue => 5_000;
	protected override int CrystalTier => 2;
	protected override int CrystalRarity => ItemRarityID.Green;
}
