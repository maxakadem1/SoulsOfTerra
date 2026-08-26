using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class CongregationLanceProjectile : ModProjectile
{
	private const int TelegraphDuration = 45;
	public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.LostSoulHostile}";

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.TrailCacheLength[Type] = 22;
		ProjectileID.Sets.TrailingMode[Type] = 0;
	}

	public override void SetDefaults()
	{
		Projectile.width = 30;
		Projectile.height = 30;
		Projectile.hostile = true;
		Projectile.penetrate = -1;
		Projectile.timeLeft = 150;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
	}

	public override bool ShouldUpdatePosition() => Projectile.ai[0] >= TelegraphDuration;
	public override bool? CanDamage() => Projectile.ai[0] >= TelegraphDuration;

	public override void AI()
	{
		Projectile.ai[0]++;
		Projectile.rotation = Projectile.velocity.ToRotation();
		if (Projectile.ai[0] == TelegraphDuration)
		{
			Terraria.Audio.SoundEngine.PlaySound(SoundID.Item72 with { Pitch = 0.15f }, Projectile.Center);
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Texture2D glow = SoulOrbProjectile.GetGlowTexture();
		Vector2 glowOrigin = glow.Size() * 0.5f;
		Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
		if (Projectile.ai[0] < TelegraphDuration)
		{
			float strength = Projectile.ai[0] / TelegraphDuration;
			Vector2 start = Projectile.Center - Main.screenPosition;
			Vector2 end = start + direction * 1800f;
			DrawLine(pixel, start, end, new Color(88, 245, 220, 0) * (0.2f + strength * 0.5f), 2f + strength * 3f);
			return false;
		}

		for (int index = Projectile.oldPos.Length - 1; index >= 1; index--)
		{
			if (Projectile.oldPos[index] == Vector2.Zero)
			{
				continue;
			}

			float strength = 1f - index / (float)Projectile.oldPos.Length;
			Vector2 position = Projectile.oldPos[index] + Projectile.Size * 0.5f - Main.screenPosition;
			Main.EntitySpriteDraw(glow, position, null, new Color(96, 245, 222, 0) * (strength * 0.55f),
				0f, glowOrigin, new Vector2(0.55f * strength, 0.25f), SpriteEffects.None);
		}

		Vector2 center = Projectile.Center - Main.screenPosition;
		Main.EntitySpriteDraw(glow, center, null, new Color(235, 255, 250, 0), Projectile.rotation,
			glowOrigin, new Vector2(0.72f, 0.35f), SpriteEffects.None);
		return false;
	}

	private static void DrawLine(Texture2D texture, Vector2 start, Vector2 end, Color color, float width)
	{
		Vector2 difference = end - start;
		// Convert the world-space line size into texture-relative scale.
		Vector2 origin = new(0f, texture.Height * 0.5f);
		Main.EntitySpriteDraw(texture, start, null, color, difference.ToRotation(), origin,
			new Vector2(difference.Length() / texture.Width, width / texture.Height), SpriteEffects.None);
	}
}
