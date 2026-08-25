using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class MoonstoneShardRocketProjectile : ModProjectile
{
	private static readonly VertexStrip TrailStrip = new();
	private const int SeparationTime = 6;
	private const float TargetRange = 900f;
	private const float CruisingSpeed = 28f;
	private const float MaximumTurn = 0.26f;

	public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.RainbowRodBullet}";

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.TrailCacheLength[Type] = 10;
		ProjectileID.Sets.TrailingMode[Type] = 3;
	}

	public override void SetDefaults()
	{
		Projectile.width = 14;
		Projectile.height = 14;
		Projectile.friendly = true;
		Projectile.DamageType = DamageClass.Magic;
		Projectile.penetrate = 1;
		Projectile.timeLeft = 150;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
	}

	public override void AI()
	{
		Projectile.ai[0]++;
		Projectile.rotation = Projectile.velocity.ToRotation();
		Projectile.tileCollide = Projectile.ai[0] > SeparationTime;
		Lighting.AddLight(Projectile.Center, 0.12f, 0.35f, 0.55f);

		if (Projectile.ai[0] > SeparationTime)
		{
			float speed = MathHelper.Lerp(Projectile.velocity.Length(), CruisingSpeed, 0.14f);
			if (FindNearestTarget() is NPC target)
			{
				float currentAngle = Projectile.velocity.ToRotation();
				float desiredAngle = (target.Center - Projectile.Center).ToRotation();
				float turn = MathHelper.Clamp(MathHelper.WrapAngle(desiredAngle - currentAngle), -MaximumTurn, MaximumTurn);
				Projectile.velocity = (currentAngle + turn).ToRotationVector2() * speed;
			}
			else
			{
				Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * speed;
			}
		}
	}

	// Briefly disarm each dart so its three-way scatter remains visible.
	public override bool? CanDamage() => Projectile.ai[0] > SeparationTime ? null : false;

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => Detonate();

	public override bool OnTileCollide(Vector2 oldVelocity)
	{
		Detonate();
		return true;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		GameShaders.Misc["MagicMissile"].UseSaturation(-2.5f).UseOpacity(2.5f).Apply();
		TrailStrip.PrepareStripWithProceduralPadding(Projectile.oldPos, Projectile.oldRot, TrailColor, TrailWidth,
			-Main.screenPosition + Projectile.Size * 0.5f, includeBacksides: false, tryStoppingOddBug: true);
		TrailStrip.DrawTrail();
		Main.pixelShader.CurrentTechnique.Passes[0].Apply();

		Texture2D texture = TextureAssets.Projectile[ProjectileID.RainbowRodBullet].Value;
		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Color.White,
			Projectile.rotation, texture.Size() * 0.5f, 0.42f, SpriteEffects.None);
		return false;
	}

	private NPC FindNearestTarget()
	{
		NPC nearest = null;
		float nearestDistanceSquared = TargetRange * TargetRange;
		foreach (NPC npc in Main.ActiveNPCs)
		{
			if (!npc.CanBeChasedBy(Projectile))
			{
				continue;
			}

			float distanceSquared = Vector2.DistanceSquared(Projectile.Center, npc.Center);
			if (distanceSquared < nearestDistanceSquared)
			{
				nearest = npc;
				nearestDistanceSquared = distanceSquared;
			}
		}

		return nearest;
	}

	private void Detonate()
	{
		if (Projectile.localAI[0] != 0f || Projectile.owner != Main.myPlayer)
		{
			return;
		}

		Projectile.localAI[0] = 1f;
		Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
			ModContent.ProjectileType<MoonstoneChildExplosionProjectile>(), Projectile.damage,
			Projectile.knockBack, Projectile.owner, 60f);
	}

	private static Color TrailColor(float progress)
	{
		Color color = Color.Lerp(Color.White, new Color(110, 215, 255), progress);
		color *= 1f - progress;
		color.A = 0;
		return color;
	}

	private static float TrailWidth(float progress) => MathHelper.Lerp(8f, 0f, progress);
}
