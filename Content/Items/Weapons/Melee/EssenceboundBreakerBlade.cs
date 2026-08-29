using Microsoft.Xna.Framework;
using SoulsOfTerra.Content.Items.Weapons;
using SoulsOfTerra.Content.Projectiles;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Items.Weapons.Melee;

public class EssenceboundBreakerBlade : EssenceboundItem
{
	private const int MaximumDeployedBlades = 5;
	private const int ThrowUseTime = 24;
	private const float ThrowSpeed = 16f;

	protected override void SetEssenceboundDefaults()
	{
		Item.CloneDefaults(ItemID.BreakerBlade);
		Item.damage = (int)System.MathF.Round(Item.damage * 1.1f);
		ConfigureThrow();
	}

	public override bool AltFunctionUse(Player player) => ThrownBreakerBladeProjectile.HasDeployedBlades(player.whoAmI);

	public override bool CanUseItem(Player player)
	{
		if (player.altFunctionUse == 2)
		{
			if (!ThrownBreakerBladeProjectile.HasDeployedBlades(player.whoAmI))
			{
				ConfigureThrow();
				return false;
			}

			ConfigureRecall();
			return true;
		}

		ConfigureThrow();
		return ThrownBreakerBladeProjectile.CountDeployedBlades(player.whoAmI) < MaximumDeployedBlades
			|| ThrownBreakerBladeProjectile.AreAllBladesLodged(player.whoAmI, MaximumDeployedBlades);
	}

	public override void HoldItem(Player player)
	{
		if (player.itemAnimation == 0)
		{
			ConfigureThrow();
		}
	}

	public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source,
		Vector2 position, Vector2 velocity, int type, int damage, float knockback)
	{
		if (player.altFunctionUse == 2)
		{
			ThrownBreakerBladeProjectile.RecallAll(player.whoAmI);
			PlayRecallSound(player);
			return false;
		}

		if (ThrownBreakerBladeProjectile.AreAllBladesLodged(player.whoAmI, MaximumDeployedBlades))
		{
			// A sixth attack cashes out a fully embedded five-blade setup.
			ThrownBreakerBladeProjectile.RecallAll(player.whoAmI);
			PlayRecallSound(player);
			return false;
		}

		Vector2 aim = (Main.MouseWorld - player.MountedCenter).SafeNormalize(new Vector2(player.direction, 0f));
		Projectile.NewProjectile(source, player.MountedCenter, aim * ThrowSpeed, type, damage, knockback,
			player.whoAmI, 0f, -1f);
		if (Main.netMode != NetmodeID.Server)
		{
			SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.72f, Pitch = -0.28f }, player.Center);
		}

		return false;
	}

	private static void PlayRecallSound(Player player)
	{
		if (Main.netMode != NetmodeID.Server)
		{
			SoundEngine.PlaySound(SoundID.Item16 with { Volume = 0.7f, Pitch = -0.2f }, player.Center);
		}
	}

	private void ConfigureThrow()
	{
		Item.useStyle = ItemUseStyleID.Shoot;
		Item.useTime = ThrowUseTime;
		Item.useAnimation = ThrowUseTime;
		Item.noMelee = true;
		Item.noUseGraphic = true;
		Item.autoReuse = true;
		Item.UseSound = null;
		Item.shoot = ModContent.ProjectileType<ThrownBreakerBladeProjectile>();
		Item.shootSpeed = ThrowSpeed;
	}

	private void ConfigureRecall()
	{
		ConfigureThrow();
		Item.useTime = 12;
		Item.useAnimation = 12;
		Item.autoReuse = false;
	}
}
