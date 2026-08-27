// EXAMPLE: How to use the custom swing system for a new melee weapon
//
// 1. Create a swing projectile that derives from BaseCustomSwingProjectile:

/*
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using SoulsOfTerra.Content.Projectiles;

namespace SoulsOfTerra.Content.Projectiles;

public class MyWeaponSwingProjectile : BaseCustomSwingProjectile
{
	// Define timing profile
	protected override int SwingDuration => 40;      // Total swing length
	protected override int WindupEnd => 18;          // When windup ends
	protected override int SnapStart => 19;          // When damage starts
	protected override int SnapEnd => 32;            // When damage ends
	protected override float SwingReach => 120f;     // Reach distance
	protected override float CollisionWidth => 48f;  // Hit width
	protected override float SwordScale => 1.4f;     // Visual scale
	
	protected override int SwordItemType => ModContent.ItemType<Items.Weapons.Melee.MyWeapon>();
	public override string Texture => "SoulsOfTerra/Content/Items/Weapons/Melee/MyWeapon";

	// Customize trail appearance
	protected override Color GetTrailColor(float strength)
	{
		return new Color(255, 120, 60) * (0.2f + strength * 0.5f);  // Orange trail
	}

	// Add weapon-specific VFX
	protected override void OnSwingTick(Player player, int age, int direction, float swordAngle)
	{
		if (age == SnapStart && Main.netMode != NetmodeID.Server)
		{
			SoundEngine.PlaySound(SoundID.Item1, player.Center);
		}
		
		// Add dust during snap
		if (age >= SnapStart && age <= SnapEnd && Main.rand.NextBool(3))
		{
			Vector2 pos = player.MountedCenter + swordAngle.ToRotationVector2() * Main.rand.NextFloat(30f, SwingReach);
			Dust.NewDust(pos, 4, 4, DustID.Torch);
		}
	}

	// Add impact VFX
	protected override void OnImpact(NPC target, NPC.HitInfo hit, int damageDone, bool alreadyHit)
	{
		for (int i = 0; i < 8; i++)
		{
			Dust.NewDust(target.Center, 16, 16, DustID.Torch, Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-2f, 2f));
		}
	}

	// Optional: customize first-hit sound
	protected override void OnFirstHit(NPC target)
	{
		if (Main.netMode != NetmodeID.Server)
		{
			SoundEngine.PlaySound(SoundID.NPCHit1, target.Center);
		}
	}
}
*/

// 2. Update your weapon item to use the custom swing:

/*
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using SoulsOfTerra.Content.Projectiles;

namespace SoulsOfTerra.Content.Items.Weapons.Melee;

public class MyWeapon : ImbuementWeaponItem
{
	public override void SetDefaults()
	{
		Item.damage = 35;
		Item.DamageType = DamageClass.Melee;
		Item.width = 48;
		Item.height = 48;
		Item.useTime = 40;
		Item.useAnimation = 40;
		Item.useStyle = ItemUseStyleID.Shoot;  // Use Shoot style
		Item.knockBack = 5f;
		Item.scale = 1.4f;
		Item.autoReuse = true;
		Item.noMelee = true;        // Hide vanilla damage
		Item.noUseGraphic = true;   // Hide vanilla swing graphic
		Item.shoot = ModContent.ProjectileType<MyWeaponSwingProjectile>();
		Item.shootSpeed = 1f;
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		// Spawn the custom swing
		Vector2 aimDirection = (Main.MouseWorld - player.MountedCenter).SafeNormalize(new Vector2(player.direction, 0f));
		Projectile.NewProjectile(source, player.MountedCenter, aimDirection, type, damage, knockback, player.whoAmI);
		
		// Add any additional projectiles (like Slimebound's gel balls) here
		
		return false;
	}
}
*/

// That's it! The base system handles:
// - Player arm positioning
// - Hitbox collision
// - Hitstop on first hit
// - Trail rendering
// - Multiplayer safety
// - Vanilla swing suppression
