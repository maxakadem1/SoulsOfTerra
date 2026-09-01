using Microsoft.Xna.Framework;
using SoulsOfTerra.Content.Items.Weapons;
using SoulsOfTerra.Content.Projectiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Items.Weapons.Ranged;

public class CarrionCall : ImbuementWeaponItem
{
	// Placeholder until original bait art exists; the Musket is consumed in the ritual, not held.
	public override string Texture => $"Terraria/Images/Item_{ItemID.WormFood}";

	public override void SetDefaults()
	{
		Item.width = 22;
		Item.height = 22;
		Item.damage = 32;
		Item.DamageType = DamageClass.Ranged;
		Item.useTime = 84;
		Item.useAnimation = 84;
		Item.useStyle = ItemUseStyleID.Swing;
		Item.knockBack = 5f;
		Item.UseSound = SoundID.Item1 with { Pitch = -0.35f, Volume = 0.85f };
		Item.autoReuse = true;
		Item.noMelee = true;
		Item.noUseGraphic = true;
		Item.rare = ItemRarityID.Green;
		Item.value = Item.buyPrice(gold: 1);
		Item.shoot = ModContent.ProjectileType<CarrionCallBaitProjectile>();
		Item.shootSpeed = 12.4f;
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
		Vector2 velocity, int type, int damage, float knockback)
	{
		Vector2 spawn = player.MountedCenter + velocity.SafeNormalize(new Vector2(player.direction, 0f)) * 16f;
		Projectile.NewProjectile(source, spawn, velocity, type, damage, knockback, player.whoAmI);
		return false;
	}
}
