using SoulsOfTerra.Content.Bosses.SoulEater;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Items.Consumables.BossSummons;

public class PulsingBait : ModItem
{
	// The Eye summon is a temporary icon until Pulsing Bait receives original art.
	public override string Texture => $"Terraria/Images/Item_{ItemID.SuspiciousLookingEye}";

	public override void SetStaticDefaults()
	{
		Item.ResearchUnlockCount = 3;
	}

	public override void SetDefaults()
	{
		Item.width = 20;
		Item.height = 20;
		Item.maxStack = Item.CommonMaxStack;
		Item.consumable = true;
		Item.useStyle = ItemUseStyleID.HoldUp;
		Item.useTime = 45;
		Item.useAnimation = 45;
		Item.UseSound = SoundID.Roar;
		Item.rare = ItemRarityID.Blue;
		Item.value = Item.buyPrice(silver: 20);
	}

	public override bool CanUseItem(Player player)
	{
		return !Main.dayTime && player.ZoneOverworldHeight
			&& !NPC.AnyNPCs(ModContent.NPCType<SoulEater>());
	}

	public override bool? UseItem(Player player)
	{
		int bossType = ModContent.NPCType<SoulEater>();
		if (player.whoAmI != Main.myPlayer)
		{
			return true;
		}

		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, number: player.whoAmI, number2: bossType);
		}
		else
		{
			NPC.SpawnOnPlayer(player.whoAmI, bossType);
		}

		return true;
	}

	public override void AddRecipes()
	{
		CreateRecipe()
			.AddIngredient(ItemID.Gel, 5)
			.AddIngredient(ItemID.Lens, 2)
			.AddIngredient(ItemID.FallenStar)
			.AddTile(TileID.WorkBenches)
			.Register();
	}
}
