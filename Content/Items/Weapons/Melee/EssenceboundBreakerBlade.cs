using SoulsOfTerra.Content.Projectiles;
using SoulsOfTerra.Content.Items.Weapons;
using SoulsOfTerra.NPCs;
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
	}

	public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (player.whoAmI != Main.myPlayer)
		{
			return;
		}

		// Reconstruct the pre-hit ratio so the opening strike retains the Breaker Blade identity.
		float lifeBeforeHit = System.Math.Min(target.lifeMax, target.life + damageDone);
		int addedStacks = lifeBeforeHit >= target.lifeMax * 0.9f ? 2 : 1;
		if (!target.GetGlobalNPC<FleshRuptureGlobalNPC>().AddStacks(player.whoAmI, addedStacks))
		{
			return;
		}

		int ruptureDamage = System.Math.Max(1, (int)(player.GetWeaponDamage(Item) * 0.75f));
		Projectile.NewProjectile(player.GetSource_OnHit(target), target.Center, Microsoft.Xna.Framework.Vector2.Zero,
			ModContent.ProjectileType<FleshRuptureProjectile>(), ruptureDamage, Item.knockBack * 0.5f, player.whoAmI);
	}
}
