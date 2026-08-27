using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class CongregationSoulBoltProjectile : ModProjectile
{
	public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.LostSoulHostile}";

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.TrailCacheLength[Type] = 18;
		ProjectileID.Sets.TrailingMode[Type] = 0;
	}

	public override void SetDefaults()
	{
		Projectile.width = 18;
		Projectile.height = 18;
		Projectile.hostile = true;
		Projectile.penetrate = 1;
		Projectile.timeLeft = 260;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
	}

	public override void AI()
	{
		Projectile.localAI[0]++;
		if (Projectile.localAI[0] <= 75f)
		{
			// A constant signed turn creates predictable arcs rather than random wobble.
			Projectile.velocity = Projectile.velocity.RotatedBy(Projectile.ai[0] * 0.012f);
		}

		Projectile.rotation = Projectile.velocity.ToRotation();
		Lighting.AddLight(Projectile.Center, new Vector3(0.08f, 0.32f, 0.29f));
	}

	public override Color? GetAlpha(Color lightColor) => new Color(150, 255, 230, 210);

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D glow = SoulOrbProjectile.GetGlowTexture();
		Texture2D ring = SoulOrbProjectile.GetRingTexture();
		Vector2 origin = glow.Size() * 0.5f;
		for (int index = Projectile.oldPos.Length - 1; index >= 1; index--)
		{
			if (Projectile.oldPos[index] == Vector2.Zero)
			{
				continue;
			}

			float strength = 1f - index / (float)Projectile.oldPos.Length;
			Vector2 position = Projectile.oldPos[index] + Projectile.Size * 0.5f - Main.screenPosition;
			Main.EntitySpriteDraw(glow, position, null, new Color(60, 220, 200, 0) * (strength * 0.42f),
				0f, origin, 0.2f * strength, SpriteEffects.None);
		}

		Vector2 center = Projectile.Center - Main.screenPosition;
		float pulse = 0.28f + 0.035f * MathF.Sin(Main.GlobalTimeWrappedHourly * 10f + Projectile.identity);
		Main.EntitySpriteDraw(glow, center, null, new Color(80, 235, 212, 0), 0f, origin, pulse, SpriteEffects.None);
		Main.EntitySpriteDraw(ring, center, null, new Color(220, 255, 246, 220), 0f, origin, 0.2f, SpriteEffects.None);
		return false;
	}
}
