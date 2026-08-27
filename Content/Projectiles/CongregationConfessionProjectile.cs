using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class CongregationConfessionProjectile : ModProjectile
{
	private const int RuptureTime = 58;
	private const float RuptureRadius = 92f;
	public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.LostSoulHostile}";

	public override void SetDefaults()
	{
		Projectile.width = 2;
		Projectile.height = 2;
		Projectile.hostile = true;
		Projectile.penetrate = -1;
		Projectile.timeLeft = 76;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
	}

	public override bool ShouldUpdatePosition() => false;
	public override bool? CanDamage() => Projectile.ai[0] is >= RuptureTime and <= RuptureTime + 7;

	public override void AI()
	{
		Projectile.ai[0]++;
		if (Projectile.ai[0] == RuptureTime)
		{
			Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74 with { Pitch = 0.35f, Volume = 0.75f }, Projectile.Center);
		}
	}

	public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
	{
		float closestX = MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right);
		float closestY = MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom);
		return Vector2.DistanceSquared(Projectile.Center, new Vector2(closestX, closestY)) <= RuptureRadius * RuptureRadius;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D glow = SoulOrbProjectile.GetGlowTexture();
		Texture2D ring = SoulOrbProjectile.GetRingTexture();
		Vector2 origin = glow.Size() * 0.5f;
		Vector2 center = Projectile.Center - Main.screenPosition;
		float anticipation = MathHelper.Clamp(Projectile.ai[0] / RuptureTime, 0f, 1f);
		float rupture = MathHelper.Clamp((Projectile.ai[0] - RuptureTime) / 7f, 0f, 1f);
		float ringScale = MathHelper.Lerp(0.35f, 2.7f, anticipation);
		if (Projectile.ai[0] >= RuptureTime)
		{
			ringScale = MathHelper.Lerp(2.7f, 3.25f, rupture);
		}

		Main.EntitySpriteDraw(ring, center, null, new Color(108, 245, 219, 0) * (0.35f + anticipation * 0.6f),
			0f, origin, ringScale, SpriteEffects.None);
		for (int face = 0; face < 3; face++)
		{
			float angle = MathHelper.TwoPi * face / 3f + Main.GlobalTimeWrappedHourly * (face % 2 == 0 ? 0.8f : -0.8f);
			Vector2 offset = angle.ToRotationVector2() * (20f + anticipation * 34f);
			Main.EntitySpriteDraw(glow, center + offset, null, new Color(220, 255, 247, 0) * 0.38f,
				0f, origin, new Vector2(0.13f, 0.2f), SpriteEffects.None);
		}

		if (rupture > 0f)
		{
			Main.EntitySpriteDraw(glow, center, null, new Color(228, 255, 250, 0) * (1f - rupture),
				0f, origin, 3.1f * rupture, SpriteEffects.None);
		}
		return false;
	}
}
