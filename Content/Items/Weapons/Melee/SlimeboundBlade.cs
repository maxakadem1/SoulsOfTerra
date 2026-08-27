using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SoulsOfTerra.Content.Items.Weapons;
using SoulsOfTerra.Content.Projectiles;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Items.Weapons.Melee;

public class SlimeboundBlade : ImbuementWeaponItem
{
	private const int SwingsPerVolley = 3;
	private int swingCounter;

	public override void SetDefaults()
	{
		Item.width = 40;
		Item.height = 40;
		Item.damage = 30;
		Item.DamageType = DamageClass.Melee;
		Item.useTime = 45;
		Item.useAnimation = 45;
		Item.useStyle = ItemUseStyleID.Shoot;
		Item.knockBack = 6.5f;
		Item.scale = 1.6f;
		Item.UseSound = null;
		Item.autoReuse = true;
		Item.rare = ItemRarityID.Blue;
		Item.value = Item.buyPrice(silver: 50);
		Item.noMelee = true;
		Item.noUseGraphic = true;
		Item.shoot = ModContent.ProjectileType<SlimeboundBladeSwingProjectile>();
		Item.shootSpeed = 1f;
	}

	public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		// Spawn the custom swing projectile
		Vector2 aimDirection = (Main.MouseWorld - player.MountedCenter).SafeNormalize(new Vector2(player.direction, 0f));
		Projectile.NewProjectile(source, player.MountedCenter, aimDirection, type, damage, knockback, player.whoAmI);

		// Fire gel balls with 1-1-3 volley cycle
		swingCounter = (swingCounter + 1) % SwingsPerVolley;
		bool royalVolley = swingCounter == 0;
		int firstSpread = royalVolley ? -1 : 0;
		int lastSpread = royalVolley ? 1 : 0;
		int gelBallType = ModContent.ProjectileType<RoyalGelBallProjectile>();

		for (int spread = firstSpread; spread <= lastSpread; spread++)
		{
			Vector2 ballVelocity = aimDirection * 6.2f;
			ballVelocity = ballVelocity.RotatedBy(spread * 0.2f);
			Projectile.NewProjectile(source, position, ballVelocity, gelBallType, (int)(damage * 0.45f), knockback * 0.7f, player.whoAmI);
		}

		if (royalVolley && player.whoAmI == Main.myPlayer)
		{
			SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.6f, Pitch = 0.25f }, player.Center);
		}

		return false;
	}

	public override void ModifyTooltips(List<TooltipLine> tooltips)
	{
		int swingsUntilVolley = SwingsPerVolley - swingCounter;
		TooltipLine chargeLine = new(Mod, "GelVolley", $"Royal volley in {swingsUntilVolley} swing{(swingsUntilVolley == 1 ? string.Empty : "s")}")
		{
			OverrideColor = swingsUntilVolley == 1 ? new Color(255, 205, 75) : new Color(80, 225, 205)
		};
		tooltips.Add(chargeLine);
	}
}
