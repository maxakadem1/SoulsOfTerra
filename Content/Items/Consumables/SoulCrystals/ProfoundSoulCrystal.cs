using Terraria.ID;

namespace SoulsOfTerra.Content.Items.Consumables.SoulCrystals;

public class ProfoundSoulCrystal : SoulCrystalItem
{
	public override long SoulValue => 25_000;
	protected override int CrystalTier => 3;
	protected override int CrystalRarity => ItemRarityID.LightRed;
}
