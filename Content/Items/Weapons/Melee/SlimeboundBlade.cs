using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SoulsOfTerra.Common.Swings;
using SoulsOfTerra.Content.Items.Weapons;
using SoulsOfTerra.Content.Projectiles;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Items.Weapons.Melee;

public class SlimeboundBlade : ImbuementWeaponItem, ISoulSwingItem
{
	private const int SwingsPerVolley = 3;
	private const float GelBallSpeed = 6.2f;
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
		Item.noMelee = true;
		Item.noUseGraphic = true;
		Item.rare = ItemRarityID.Blue;
		Item.value = Item.buyPrice(silver: 50);
		Item.shoot = SoulSwing.ProjectileType;
		Item.shootSpeed = 1f;
	}

	public SoulSwingStyle GetSwingStyle(Player player) => new()
	{
		Duration = Item.useAnimation,
		Path = SoulSwingPath.AlternatingLateral,
		ArcSpan = 2.95f,
		Reach = 90f,
		HitWidth = 42f,
		Scale = Item.scale,
		RibbonColor = new Color(45, 230, 210),
		RibbonLifetime = 16,
		RibbonWidth = 24f,
		AfterimageCount = 5
	};

	public override bool CanUseItem(Player player) => SoulSwing.CanStart(player);

	public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		SoulSwing.Shoot(player, source, damage, knockback);
		return false;
	}

	public void OnSwingCut(Player player, Projectile swing, Vector2 tip, Vector2 aim)
	{
		swingCounter = (swingCounter + 1) % SwingsPerVolley;
		bool royalVolley = swingCounter == 0;
		int ballDamage = Math.Max(1, (int)(swing.damage * 0.45f));
		float ballKnockback = swing.knockBack * 0.7f;
		int ballType = ModContent.ProjectileType<RoyalGelBallProjectile>();
		int firstSpread = royalVolley ? -1 : 0;
		int lastSpread = royalVolley ? 1 : 0;
		for (int spread = firstSpread; spread <= lastSpread; spread++)
		{
			Vector2 ballVelocity = aim.RotatedBy(spread * 0.2f) * GelBallSpeed;
			Projectile.NewProjectile(player.GetSource_ItemUse(Item), tip, ballVelocity, ballType, ballDamage, ballKnockback, player.whoAmI);
		}

		if (royalVolley && player.whoAmI == Main.myPlayer)
		{
			SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.6f, Pitch = 0.25f }, tip);
		}
	}

	public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
	{
		// A restrained splash keeps direct melee hits tactile.
		for (int i = 0; i < 4; i++)
		{
			Dust dust = Dust.NewDustPerfect(target.Center, DustID.BlueCrystalShard, Main.rand.NextVector2Circular(1.4f, 1.4f), 120, new Color(45, 230, 210), 0.7f);
			dust.noGravity = true;
		}
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
