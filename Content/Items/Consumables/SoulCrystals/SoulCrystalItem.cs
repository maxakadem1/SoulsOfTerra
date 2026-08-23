using SoulsOfTerra.Content.Projectiles;
using SoulsOfTerra.Players;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Items.Consumables.SoulCrystals;

public abstract class SoulCrystalItem : ModItem
{
	public abstract long SoulValue { get; }
	protected abstract int CrystalTier { get; }
	protected abstract int CrystalRarity { get; }

	public override void SetDefaults()
	{
		Item.width = 32;
		Item.height = 32;
		Item.maxStack = Item.CommonMaxStack;
		Item.consumable = true;
		Item.useStyle = ItemUseStyleID.HoldUp;
		Item.useTime = 30;
		Item.useAnimation = 30;
		Item.UseSound = SoundID.Item29 with { Volume = 0.72f, Pitch = -0.15f + CrystalTier * 0.15f };
		Item.rare = CrystalRarity;
		Item.value = 0;
	}

	public override bool? UseItem(Player player)
	{
		if (Main.netMode != NetmodeID.MultiplayerClient)
		{
			// The server owns both the balance gain and its shared release effect.
			player.GetModPlayer<SoulPlayer>().AddSouls(SoulValue);
			SoulCrystalReleaseProjectile.Spawn(player, CrystalTier);
		}

		return true;
	}
}
