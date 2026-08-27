using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class CompeditusLanceProjectile : ModProjectile
{
	private const int HomingDuration = 30;
	private int age;

	public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.LostSoulFriendly}";

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.MinionShot[Type] = true;
		ProjectileID.Sets.TrailCacheLength[Type] = 14;
		ProjectileID.Sets.TrailingMode[Type] = 0;
	}

	public override void SetDefaults()
	{
		Projectile.width = 16;
		Projectile.height = 16;
		Projectile.friendly = true;
		Projectile.DamageType = DamageClass.Summon;
		Projectile.penetrate = 1;
		Projectile.timeLeft = 120;
		Projectile.tileCollide = true;
		Projectile.ignoreWater = true;
	}

	public override void AI()
	{
		if (age++ == 0)
		{
			SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.32f, Pitch = 0.42f }, Projectile.Center);
		}

		int targetIndex = (int)Projectile.ai[0] - 1;
		if (age <= HomingDuration && targetIndex >= 0 && targetIndex < Main.maxNPCs)
		{
			NPC target = Main.npc[targetIndex];
			if (target.active && target.CanBeChasedBy(Projectile)
				&& Collision.CanHitLine(Projectile.Center, 2, 2, target.Center, 2, 2))
			{
				float speed = Projectile.velocity.Length();
				Vector2 desiredVelocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * speed;
				Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.075f);
			}
		}

		Projectile.rotation = Projectile.velocity.ToRotation();
		Lighting.AddLight(Projectile.Center, new Vector3(0.02f, 0.2f, 0.18f));
	}

	public override bool OnTileCollide(Vector2 oldVelocity)
	{
		CreateImpactDust(5);
		return true;
	}

	public override void OnKill(int timeLeft) => CreateImpactDust(7);

	private void CreateImpactDust(int count)
	{
		if (Main.dedServ)
		{
			return;
		}

		for (int index = 0; index < count; index++)
		{
			Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.DungeonSpirit,
				Main.rand.NextVector2Circular(1.7f, 1.7f), 120, new Color(64, 235, 214), 0.55f);
			dust.noGravity = true;
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D glow = SoulOrbProjectile.GetGlowTexture();
		Vector2 origin = glow.Size() * 0.5f;
		for (int index = Projectile.oldPos.Length - 1; index >= 1; index--)
		{
			if (Projectile.oldPos[index] == Vector2.Zero)
			{
				continue;
			}

			float strength = 1f - index / (float)Projectile.oldPos.Length;
			Vector2 position = Projectile.oldPos[index] + Projectile.Size * 0.5f - Main.screenPosition;
			Main.EntitySpriteDraw(glow, position, null, new Color(54, 226, 207, 0) * (strength * 0.42f),
				Projectile.rotation, origin, new Vector2(0.24f * strength, 0.12f), SpriteEffects.None);
		}

		Vector2 center = Projectile.Center - Main.screenPosition;
		Main.EntitySpriteDraw(glow, center, null, new Color(218, 255, 248, 0), Projectile.rotation,
			origin, new Vector2(0.42f, 0.16f), SpriteEffects.None);
		return false;
	}
}
