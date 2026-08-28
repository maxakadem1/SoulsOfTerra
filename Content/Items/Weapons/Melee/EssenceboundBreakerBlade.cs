using Microsoft.Xna.Framework;
using SoulsOfTerra.Common.Swings;
using SoulsOfTerra.Content.Items.Weapons;
using SoulsOfTerra.Content.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Items.Weapons.Melee;

public class EssenceboundBreakerBlade : EssenceboundItem, ISoulSwingItem
{
	private const int SmashDuration = 52;

	protected override void SetEssenceboundDefaults()
	{
		Item.CloneDefaults(ItemID.BreakerBlade);
		Item.damage = (int)System.MathF.Round(Item.damage * 1.1f);
		Item.UseSound = null;
		ConfigureSwingUse();
	}

	public SoulSwingStyle GetSwingStyle(Player player) => new()
	{
		Duration = SmashDuration,
		WindUpPortion = 0.32f,
		CutPortion = 0.28f,
		Path = SoulSwingPath.Falling,
		ArcSpan = 3.7f,
		Reach = 124f,
		HitWidth = 56f,
		Scale = Item.scale,
		RibbonColor = Color.Lerp(new Color(195, 181, 157), new Color(224, 239, 219), 0.65f),
		RibbonLifetime = 16,
		RibbonWidth = 28f,
		AfterimageCount = 5
	};

	public override bool AltFunctionUse(Player player) => SoulBandageTetherProjectile.HasConnections(player.whoAmI);

	public override bool CanUseItem(Player player)
	{
		int executionType = ModContent.ProjectileType<BandageExecutionProjectile>();
		bool executionActive = player.ownedProjectileCounts[executionType] > 0;
		if (player.altFunctionUse == 2)
		{
			if (!SoulBandageTetherProjectile.HasConnections(player.whoAmI)
				|| executionActive
				|| !SoulSwing.CanStart(player))
			{
				ConfigureSwingUse();
				return false;
			}

			Item.useStyle = ItemUseStyleID.Shoot;
			Item.useTime = BandageExecutionProjectile.ExecutionDuration;
			Item.useAnimation = BandageExecutionProjectile.ExecutionDuration;
			Item.noMelee = true;
			Item.noUseGraphic = true;
			Item.autoReuse = false;
			Item.shoot = executionType;
			Item.shootSpeed = 1f;
			return true;
		}

		if (!SoulSwing.CanStart(player) || executionActive)
		{
			ConfigureSwingUse();
			return false;
		}

		ConfigureSwingUse();
		return true;
	}

	public override void HoldItem(Player player)
	{
		// Restore the smash setup once neither authored projectile is still holding the blade.
		int executionType = ModContent.ProjectileType<BandageExecutionProjectile>();
		if (player.ownedProjectileCounts[executionType] == 0
			&& player.ownedProjectileCounts[SoulSwing.ProjectileType] == 0
			&& player.itemAnimation == 0)
		{
			ConfigureSwingUse();
		}
	}

	private void ConfigureSwingUse()
	{
		Item.useStyle = ItemUseStyleID.Shoot;
		Item.useTime = SmashDuration;
		Item.useAnimation = SmashDuration;
		Item.noMelee = true;
		Item.noUseGraphic = true;
		Item.autoReuse = true;
		Item.shoot = SoulSwing.ProjectileType;
		Item.shootSpeed = 1f;
	}

	public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source,
		Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		if (player.altFunctionUse == 2)
		{
			int executionType = ModContent.ProjectileType<BandageExecutionProjectile>();
			if (!SoulBandageTetherProjectile.HasConnections(player.whoAmI)
				|| player.ownedProjectileCounts[executionType] > 0
				|| !SoulSwing.CanStart(player))
			{
				return false;
			}

			Vector2 aimDirection = (Main.MouseWorld - player.MountedCenter).SafeNormalize(new Vector2(player.direction, 0f));
			int projectileIndex = Projectile.NewProjectile(source, player.MountedCenter, aimDirection, executionType,
				System.Math.Max(1, (int)(damage * 1.6f)), knockback, player.whoAmI);
			if (projectileIndex < 0 || projectileIndex >= Main.maxProjectiles)
			{
				ConfigureSwingUse();
			}

			return false;
		}

		if (!SoulSwing.CanStart(player)
			|| player.ownedProjectileCounts[ModContent.ProjectileType<BandageExecutionProjectile>()] > 0)
		{
			return false;
		}

		SoulSwing.Shoot(player, source, damage, knockback);
		return false;
	}

	public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (player.whoAmI != Main.myPlayer)
		{
			return;
		}

		SoulBandageTetherProjectile.Attach(player, target);
	}
}
