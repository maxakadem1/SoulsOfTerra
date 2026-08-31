using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Common.Rendering;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class UnkemptHaroldExplosionProjectile : ModProjectile, IPixelatedDrawable
{
	public const int BlastSize = 40;
	private const int VisualLifetime = 10;
	private const int DamageDuration = 3;

	public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.PurificationPowder}";

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.DrawScreenCheckFluff[Type] = 80;
	}

	public override void SetDefaults()
	{
		Projectile.width = BlastSize;
		Projectile.height = BlastSize;
		Projectile.friendly = true;
		Projectile.DamageType = DamageClass.Ranged;
		Projectile.penetrate = -1;
		Projectile.timeLeft = VisualLifetime;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = -1;
	}

	public override bool ShouldUpdatePosition() => false;

	public override bool CanHitPlayer(Player target) => false;

	public override bool? CanHitNPC(NPC target)
	{
		if (Projectile.ai[0] > DamageDuration)
		{
			return false;
		}

		return Collision.CanHitLine(Projectile.Center, 1, 1, target.position, target.width, target.height)
			? null
			: false;
	}

	public override void AI()
	{
		Projectile.ai[0]++;
		Projectile.friendly = Projectile.ai[0] <= DamageDuration;
		float fade = 1f - Projectile.ai[0] / VisualLifetime;
		Lighting.AddLight(Projectile.Center, 1.4f * fade, 0.5f * fade, 0.08f * fade);

		if (Main.netMode == NetmodeID.Server || Projectile.localAI[0] != 0f)
		{
			return;
		}

		Projectile.localAI[0] = 1f;
		for (int index = 0; index < 10; index++)
		{
			Vector2 direction = Main.rand.NextVector2Unit();
			Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Torch, direction * Main.rand.NextFloat(1.8f, 5.2f),
				50, new Color(255, 150, 50), Main.rand.NextFloat(0.7f, 1.15f));
			dust.noGravity = true;
		}
	}

	public override bool PreDraw(ref Color lightColor) => false;

	public void DrawPixelated(SpriteBatch spriteBatch)
	{
		Texture2D glow = SoulOrbProjectile.GetGlowTexture();
		Texture2D ring = SoulOrbProjectile.GetRingTexture();
		Vector2 origin = glow.Size() * 0.5f;
		Vector2 ringOrigin = ring.Size() * 0.5f;
		Vector2 center = Projectile.Center - Main.screenPosition;
		float progress = Projectile.ai[0] / VisualLifetime;
		float bloom = MathHelper.Lerp(0.42f, 0.18f, progress);
		float opacity = 1f - progress * progress;

		Main.EntitySpriteDraw(glow, center, null, new Color(255, 70, 10, 0) * (0.85f * opacity), 0f, origin,
			bloom * 1.15f, SpriteEffects.None);
		Main.EntitySpriteDraw(glow, center, null, new Color(255, 170, 40, 0) * opacity, 0f, origin, bloom * 0.72f,
			SpriteEffects.None);
		Main.EntitySpriteDraw(glow, center, null, new Color(255, 240, 170, 0) * opacity, 0f, origin, bloom * 0.28f,
			SpriteEffects.None);
		Main.EntitySpriteDraw(ring, center, null, new Color(255, 110, 20, 0) * (0.7f * opacity), progress * 2.4f,
			ringOrigin, bloom * 0.55f, SpriteEffects.None);
	}
}
