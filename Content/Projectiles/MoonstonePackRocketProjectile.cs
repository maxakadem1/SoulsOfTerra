using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class MoonstonePackRocketProjectile : ModProjectile
{
	private static readonly VertexStrip TrailStrip = new();
	private const int SeparationTime = 12;
	private const float TargetRange = 800f;
	private const float CruisingSpeed = 22f;
	private const float MaximumTurn = 0.16f;

	public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.RainbowRodBullet}";

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.TrailCacheLength[Type] = 14;
		ProjectileID.Sets.TrailingMode[Type] = 3;
	}

	public override void SetDefaults()
	{
		Projectile.width = 24;
		Projectile.height = 24;
		Projectile.friendly = true;
		Projectile.DamageType = DamageClass.Magic;
		Projectile.penetrate = 1;
		Projectile.timeLeft = 180;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
	}

	public override void AI()
	{
		Projectile.ai[0]++;
		Projectile.rotation = Projectile.velocity.ToRotation();
		Projectile.tileCollide = Projectile.ai[0] > SeparationTime;
		Lighting.AddLight(Projectile.Center, 0.2f, 0.55f, 0.75f);

		if (Projectile.ai[0] > SeparationTime)
		{
			HomeAggressively(CruisingSpeed, MaximumTurn);
		}

		if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(2))
		{
			Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.BlueCrystalShard,
				-Projectile.velocity * 0.08f, 40, new Color(190, 240, 255), 0.85f);
			dust.noGravity = true;
		}
	}

	// The launch bloom must clear the original target before the warhead arms.
	public override bool? CanDamage() => Projectile.ai[0] > SeparationTime ? null : false;

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => Detonate();

	public override bool OnTileCollide(Vector2 oldVelocity)
	{
		Detonate();
		return true;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		GameShaders.Misc["MagicMissile"].UseSaturation(-2.2f).UseOpacity(2.2f).Apply();
		TrailStrip.PrepareStripWithProceduralPadding(Projectile.oldPos, Projectile.oldRot, TrailColor, TrailWidth,
			-Main.screenPosition + Projectile.Size * 0.5f, includeBacksides: false, tryStoppingOddBug: true);
		TrailStrip.DrawTrail();
		Main.pixelShader.CurrentTechnique.Passes[0].Apply();

		Texture2D texture = TextureAssets.Projectile[ProjectileID.RainbowRodBullet].Value;
		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, new Color(225, 250, 255),
			Projectile.rotation, texture.Size() * 0.5f, 0.7f, SpriteEffects.None);
		return false;
	}

	private void HomeAggressively(float speed, float maximumTurn)
	{
		float currentSpeed = MathHelper.Lerp(Projectile.velocity.Length(), speed, 0.1f);
		if (FindNearestTarget() is not NPC target)
		{
			Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * currentSpeed;
			return;
		}

		float currentAngle = Projectile.velocity.ToRotation();
		float desiredAngle = (target.Center - Projectile.Center).ToRotation();
		float turn = MathHelper.Clamp(MathHelper.WrapAngle(desiredAngle - currentAngle), -maximumTurn, maximumTurn);
		Projectile.velocity = (currentAngle + turn).ToRotationVector2() * currentSpeed;
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
			Projectile.knockBack, Projectile.owner, 104f);

		// Three evenly spaced darts make each secondary impact readable as a fresh cascade.
		float angleOffset = Projectile.velocity.ToRotation() + Projectile.identity * 0.41f;
		for (int index = 0; index < 3; index++)
		{
			Vector2 velocity = (angleOffset + MathHelper.TwoPi * index / 3f).ToRotationVector2() * 12f;
			Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity,
				ModContent.ProjectileType<MoonstoneShardRocketProjectile>(),
				System.Math.Max(1, (int)(Projectile.damage * 0.4f)), Projectile.knockBack * 0.5f, Projectile.owner);
		}
	}

	private static Color TrailColor(float progress)
	{
		Color color = Color.Lerp(new Color(245, 255, 255), new Color(95, 195, 255), progress);
		color *= 1f - progress;
		color.A = 0;
		return color;
	}

	private static float TrailWidth(float progress) => MathHelper.Lerp(15f, 0f, progress);
}
