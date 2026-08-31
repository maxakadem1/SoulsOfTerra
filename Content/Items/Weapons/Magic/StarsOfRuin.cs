using Microsoft.Xna.Framework;
using SoulsOfTerra.Content.Items.Weapons;
using SoulsOfTerra.Content.Projectiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Items.Weapons.Magic;

public class StarsOfRuin : ImbuementWeaponItem
{
	public override void SetStaticDefaults()
	{
		// Staff handling keeps the diagonal sprite aligned with the casting pose.
		Item.staff[Type] = true;
	}

	public override void SetDefaults()
	{
		Item.width = 40;
		Item.height = 40;
		Item.damage = 18;
		Item.DamageType = DamageClass.Magic;
		Item.mana = 26;
		Item.useTime = StarsOfRuinCastProjectile.VerseDuration;
		Item.useAnimation = StarsOfRuinCastProjectile.VerseDuration;
		Item.useStyle = ItemUseStyleID.Shoot;
		Item.knockBack = 2.5f;
		Item.UseSound = null;
		Item.autoReuse = true;
		Item.noMelee = true;
		Item.rare = ItemRarityID.Orange;
		Item.value = Item.buyPrice(gold: 2);
		Item.shoot = ModContent.ProjectileType<StarsOfRuinCastProjectile>();
		Item.shootSpeed = 1f;
	}

	public override bool CanUseItem(Player player) =>
		player.ownedProjectileCounts[ModContent.ProjectileType<StarsOfRuinCastProjectile>()] <= 0;

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
		Vector2 velocity, int type, int damage, float knockback)
	{
		Vector2 aim = velocity.SafeNormalize(new Vector2(player.direction, 0f));
		Projectile.NewProjectile(source, player.MountedCenter, aim, type, damage, knockback, player.whoAmI);
		return false;
	}
}
