using Microsoft.Xna.Framework;
using SoulsOfTerra.Common;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class SoulBloodstainRecoveryProjectile : ModProjectile
{
	public const int Lifetime = 18;

	private int TargetPlayerIndex => (int)Projectile.ai[0];
	private int VisualTier => (int)Projectile.ai[1];

	public override string Texture => $"Terraria/Images/Item_{ItemID.SoulofNight}";

	public override void SetDefaults()
	{
		Projectile.width = 2;
		Projectile.height = 2;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.netImportant = true;
		Projectile.penetrate = -1;
		Projectile.timeLeft = Lifetime;
	}

	public override bool? CanDamage() => false;
	public override bool ShouldUpdatePosition() => false;

	public override void AI()
	{
		if (TargetPlayerIndex < 0 || TargetPlayerIndex >= Main.maxPlayers || !Main.player[TargetPlayerIndex].active)
		{
			Projectile.Kill();
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		if (TargetPlayerIndex >= 0 && TargetPlayerIndex < Main.maxPlayers)
		{
			Player target = Main.player[TargetPlayerIndex];
			float progress = 1f - Projectile.timeLeft / (float)Lifetime;
			SoulBloodstainDraw.DrawRecovery(Projectile, target, VisualTier, progress);
		}

		return false;
	}

	public override void OnKill(int timeLeft)
	{
		SoulBloodstainDraw.SpawnRecoveryBurst(Projectile.Center, VisualTier);
	}

	public static void Spawn(IEntitySource source, Vector2 position, int playerIndex, int visualTier)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			return;
		}

		Projectile.NewProjectile(source, position, Vector2.Zero, ModContent.ProjectileType<SoulBloodstainRecoveryProjectile>(),
			0, 0f, playerIndex, playerIndex, visualTier);
	}
}
