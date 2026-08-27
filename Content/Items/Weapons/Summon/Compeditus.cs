using Microsoft.Xna.Framework;
using SoulsOfTerra.Content.Buffs;
using SoulsOfTerra.Content.Items.Weapons;
using SoulsOfTerra.Content.Projectiles;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Items.Weapons.Summon;

public class Compeditus : ImbuementWeaponItem
{
	private const int MaximumSeals = 4;

	// The held relic uses its dedicated bell artwork rather than the class-name texture convention.
	public override string Texture => "SoulsOfTerra/Content/Items/Weapons/Summon/Compeditus_item";

	public override void SetStaticDefaults()
	{
		ItemID.Sets.GamepadWholeScreenUseRange[Type] = true;
		ItemID.Sets.LockOnIgnoresCollision[Type] = true;
	}

	public override void SetDefaults()
	{
		Item.width = 22;
		Item.height = 32;
		Item.damage = 22;
		Item.DamageType = DamageClass.Summon;
		Item.mana = 10;
		Item.useTime = 30;
		Item.useAnimation = 30;
		Item.useStyle = ItemUseStyleID.HoldUp;
		Item.noMelee = true;
		Item.knockBack = 2f;
		Item.UseSound = SoundID.Item44 with { Volume = 0.7f, Pitch = 0.15f };
		Item.rare = ItemRarityID.Orange;
		Item.value = Item.buyPrice(gold: 2);
		Item.buffType = ModContent.BuffType<CompeditusBuff>();
		Item.shoot = ModContent.ProjectileType<CompeditusSealMinionProjectile>();
	}

	public override bool CanUseItem(Player player)
	{
		int sealCount = CompeditusCoreProjectile.CountOwnedSeals(player.whoAmI);
		return sealCount >= MaximumSeals || player.slotsMinions + 1f <= player.maxMinions + 0.001f;
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity,
		int type, int damage, float knockback)
	{
		player.AddBuff(Item.buffType, 2);
		int coreIndex = CompeditusCoreProjectile.FindOwnedCore(player.whoAmI);
		if (coreIndex < 0)
		{
			coreIndex = Projectile.NewProjectile(source, player.Center, Vector2.Zero,
				ModContent.ProjectileType<CompeditusCoreProjectile>(), damage, knockback, player.whoAmI);
			if (coreIndex >= 0 && coreIndex < Main.maxProjectiles)
			{
				Main.projectile[coreIndex].originalDamage = Item.damage;
			}
		}

		if (CompeditusCoreProjectile.CountOwnedSeals(player.whoAmI) < MaximumSeals)
		{
			int sealIndex = Projectile.NewProjectile(source, player.Center, Vector2.Zero, type, damage, knockback, player.whoAmI);
			if (sealIndex >= 0 && sealIndex < Main.maxProjectiles)
			{
				Main.projectile[sealIndex].originalDamage = Item.damage;
			}
		}
		else if (coreIndex >= 0 && coreIndex < Main.maxProjectiles)
		{
			// Reusing a complete formation recalls it without consuming another minion slot.
			Main.projectile[coreIndex].Center = player.Center;
			Main.projectile[coreIndex].netUpdate = true;
			SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.45f, Pitch = 0.35f }, player.Center);
		}

		return false;
	}
}
