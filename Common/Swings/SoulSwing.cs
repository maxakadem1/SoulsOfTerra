using Microsoft.Xna.Framework;
using SoulsOfTerra.Content.Projectiles;
using SoulsOfTerra.Players;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace SoulsOfTerra.Common.Swings;

public static class SoulSwing
{
	public static int ProjectileType => ModContent.ProjectileType<SoulSwingProjectile>();

	public static bool CanStart(Player player) => player.ownedProjectileCounts[ProjectileType] <= 0;

	public static void Shoot(Player player, IEntitySource source, int damage, float knockback)
	{
		Vector2 aim = (Main.MouseWorld - player.MountedCenter).SafeNormalize(new Vector2(player.direction, 0f));
		SoulPlayer soulPlayer = player.GetModPlayer<SoulPlayer>();
		int sign = (soulPlayer.SoulSwingIndex++ & 1) == 0 ? 1 : -1;
		Projectile.NewProjectile(source, player.MountedCenter, aim, ProjectileType, damage, knockback, player.whoAmI,
			player.HeldItem.type, sign);
	}
}
