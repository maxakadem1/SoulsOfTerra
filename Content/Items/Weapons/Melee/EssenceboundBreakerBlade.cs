using SoulsOfTerra.Content.Projectiles;
using SoulsOfTerra.Content.Items.Weapons;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Items.Weapons.Melee;

public class EssenceboundBreakerBlade : EssenceboundItem
{
	public override string Texture => $"Terraria/Images/Item_{ItemID.BreakerBlade}";

	protected override void SetEssenceboundDefaults()
	{
		Item.CloneDefaults(ItemID.BreakerBlade);
		Item.damage = (int)System.MathF.Round(Item.damage * 1.1f);
		Item.autoReuse = true;
	}

	public override bool AltFunctionUse(Player player) => SoulBandageTetherProjectile.HasConnections(player.whoAmI);

	public override bool CanUseItem(Player player)
	{
		if (player.altFunctionUse == 2)
		{
			int executionType = ModContent.ProjectileType<BandageExecutionProjectile>();
			if (!SoulBandageTetherProjectile.HasConnections(player.whoAmI)
				|| player.ownedProjectileCounts[executionType] > 0)
			{
				ConfigureNormalUse();
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

		ConfigureNormalUse();
		return true;
	}

	public override void HoldItem(Player player)
	{
		// Failed or completed alternate uses must never leave the vanilla held blade hidden.
		int executionType = ModContent.ProjectileType<BandageExecutionProjectile>();
		if (player.ownedProjectileCounts[executionType] == 0 && player.itemAnimation == 0)
		{
			ConfigureNormalUse();
		}
	}

	private void ConfigureNormalUse()
	{
		Item.useStyle = ItemUseStyleID.Swing;
		Item.useTime = 45;
		Item.useAnimation = 45;
		Item.noMelee = false;
		Item.noUseGraphic = false;
		Item.autoReuse = true;
		Item.shoot = ProjectileID.None;
		Item.shootSpeed = 0f;
	}

	public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source,
		Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		if (player.altFunctionUse != 2 || !SoulBandageTetherProjectile.HasConnections(player.whoAmI)
			|| player.ownedProjectileCounts[ModContent.ProjectileType<BandageExecutionProjectile>()] > 0)
		{
			return false;
		}

		Vector2 aimDirection = (Main.MouseWorld - player.MountedCenter).SafeNormalize(new Vector2(player.direction, 0f));
		int projectileIndex = Projectile.NewProjectile(source, player.MountedCenter, aimDirection, type,
			System.Math.Max(1, (int)(damage * 1.6f)), knockback, player.whoAmI);
		if (projectileIndex < 0 || projectileIndex >= Main.maxProjectiles)
		{
			ConfigureNormalUse();
		}
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
