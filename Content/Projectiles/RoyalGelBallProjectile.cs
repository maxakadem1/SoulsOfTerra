using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class RoyalGelBallProjectile : ModProjectile
{
	private const int TrailLength = 8;
	private const int MaximumBounces = 3;

	// The sprite name follows the weapon while this class keeps its behavior-focused name.
	public override string Texture => "SoulsOfTerra/Content/Projectiles/SlimeboundBladeProjectile";

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
		ProjectileID.Sets.TrailingMode[Type] = 0;
	}

	public override void SetDefaults()
	{
		Projectile.width = 16;
		Projectile.height = 16;
		Projectile.friendly = true;
		Projectile.DamageType = DamageClass.Melee;
		Projectile.penetrate = 5;
		Projectile.timeLeft = 210;
		Projectile.tileCollide = true;
		Projectile.ignoreWater = false;
		Projectile.scale = 0.9f;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = 14;
	}

	public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
	{
		// Tile collision follows the opaque gel core instead of its transparent corners.
		width = 14;
		height = 14;
		return true;
	}

	public override void AI()
	{
		Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + 0.11f, -12f, 10f);
		Projectile.rotation += Projectile.velocity.X * 0.035f;
		Lighting.AddLight(Projectile.Center, 0.03f, 0.25f, 0.21f);
		if (Main.rand.NextBool(4))
		{
			Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.BlueCrystalShard, -Projectile.velocity * 0.04f, 145, new Color(45, 225, 195), 0.55f);
			dust.noGravity = true;
		}
	}

	public override bool OnTileCollide(Vector2 oldVelocity)
	{
		if (Projectile.ai[0] >= MaximumBounces)
		{
			return true;
		}

		Projectile.ai[0]++;
		if (Projectile.velocity.X != oldVelocity.X)
		{
			Projectile.velocity.X = -oldVelocity.X * 0.84f;
		}

		if (Projectile.velocity.Y != oldVelocity.Y)
		{
			Projectile.velocity.Y = -oldVelocity.Y * 0.82f;
			if (System.Math.Abs(Projectile.velocity.Y) < 2.2f)
			{
				Projectile.velocity.Y = oldVelocity.Y > 0f ? -2.2f : 2.2f;
			}
		}

		SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = 0.35f, Pitch = 0.45f }, Projectile.Center);
		Projectile.netUpdate = true;
		return false;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D texture = TextureAssets.Projectile[Type].Value;
		Vector2 origin = texture.Size() * 0.5f;
		for (int i = Projectile.oldPos.Length - 1; i >= 1; i--)
		{
			if (Projectile.oldPos[i] == Vector2.Zero)
			{
				continue;
			}

			float strength = 1f - i / (float)Projectile.oldPos.Length;
			Vector2 position = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
			Color trailColor = Color.FromNonPremultiplied(35, 205, 180, (int)(85f * strength));
			Main.EntitySpriteDraw(texture, position, null, trailColor, Projectile.rotation, origin, Projectile.scale * strength, SpriteEffects.None);
		}

		// Preserve the authored sword palette while still respecting world lighting.
		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
		return false;
	}
}
