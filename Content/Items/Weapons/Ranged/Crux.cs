using Microsoft.Xna.Framework;
using SoulsOfTerra.Content.Items.Weapons;
using SoulsOfTerra.Content.Projectiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Items.Weapons.Ranged;

public class Crux : ImbuementWeaponItem
{
	public override void SetStaticDefaults()
	{
		ItemID.Sets.LockOnIgnoresCollision[Type] = true;
	}

	public override void SetDefaults()
	{
		Item.width = 33;
		Item.height = 22;
		Item.scale = 1.2f;
		Item.damage = 34;
		Item.DamageType = DamageClass.Ranged;
		Item.useTime = CruxVolleyProjectile.VolleyDuration;
		Item.useAnimation = CruxVolleyProjectile.VolleyDuration;
		Item.useStyle = ItemUseStyleID.Shoot;
		Item.knockBack = 4f;
		Item.UseSound = null;
		Item.autoReuse = true;
		Item.noMelee = true;
		Item.rare = ItemRarityID.Orange;
		Item.value = Item.buyPrice(gold: 2);
		Item.useAmmo = AmmoID.Bullet;
		Item.shoot = ModContent.ProjectileType<CruxVolleyProjectile>();
		Item.shootSpeed = 1f;
	}

	// Seat the grip forward and slightly above the default hand position.
	public override Vector2? HoldoutOffset() => new Vector2(2f, -2f);

	public override bool CanUseItem(Player player) =>
		player.ownedProjectileCounts[ModContent.ProjectileType<CruxVolleyProjectile>()] <= 0;

	public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type,
		ref int damage, ref float knockback)
	{
		// Ammo would otherwise replace the volley with a vanilla musket ball.
		type = ModContent.ProjectileType<CruxVolleyProjectile>();
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
		Vector2 velocity, int type, int damage, float knockback)
	{
		Vector2 lockPoint = Main.MouseWorld;
		int index = Projectile.NewProjectile(source, lockPoint, Vector2.Zero,
			ModContent.ProjectileType<CruxVolleyProjectile>(), damage, knockback, player.whoAmI,
			lockPoint.X, lockPoint.Y);
		if (index >= 0 && index < Main.maxProjectiles)
		{
			Main.projectile[index].Center = lockPoint;
			Main.projectile[index].velocity = Vector2.Zero;
		}

		return false;
	}
}
