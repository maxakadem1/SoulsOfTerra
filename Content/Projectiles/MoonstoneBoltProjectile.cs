using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class MoonstoneBoltProjectile : ModProjectile
{
	private static readonly VertexStrip TrailStrip = new();
	private static readonly VertexStrip HeadStrip = new();
	private const float TargetRange = 35f * 16f;
	private const float MaximumTurn = MathHelper.Pi / 120f;
	private readonly Vector2[] headPositions = new Vector2[10];
	private readonly float[] headRotations = new float[10];

	public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.RainbowRodBullet}";

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.TrailCacheLength[Type] = 24;
		ProjectileID.Sets.TrailingMode[Type] = 3;
	}

	public override void SetDefaults()
	{
		Projectile.width = 72;
		Projectile.height = 72;
		Projectile.friendly = true;
		Projectile.DamageType = DamageClass.Magic;
		Projectile.penetrate = 1;
		Projectile.timeLeft = 240;
		Projectile.tileCollide = true;
		Projectile.ignoreWater = true;
	}

	public override void AI()
	{
		Projectile.ai[0]++;
		Projectile.rotation = Projectile.velocity.ToRotation();
		Lighting.AddLight(Projectile.Center, 0.35f, 0.75f, 0.9f);

		if (Projectile.ai[0] > 8f && FindNearestTarget() is NPC target)
		{
			float currentAngle = Projectile.velocity.ToRotation();
			float desiredAngle = (target.Center - Projectile.Center).ToRotation();
			float turn = MathHelper.Clamp(MathHelper.WrapAngle(desiredAngle - currentAngle), -MaximumTurn, MaximumTurn);
			Projectile.velocity = (currentAngle + turn).ToRotationVector2() * Projectile.velocity.Length();
		}

		if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(3))
		{
			Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.BlueCrystalShard, -Projectile.velocity * 0.05f,
				80, new Color(215, 250, 255), 0.75f);
			dust.noGravity = true;
		}
	}

	public override Color? GetAlpha(Color lightColor) => new Color(225, 250, 255);

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		SpawnImpactEffects();
	}

	public override bool OnTileCollide(Vector2 oldVelocity)
	{
		SpawnImpactEffects();
		return true;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		GameShaders.Misc["RainbowRod"]
			.UseSaturation(-2.8f)
			.UseOpacity(4f)
			.Apply();
		TrailStrip.PrepareStripWithProceduralPadding(Projectile.oldPos, Projectile.oldRot, TrailColor, TrailWidth,
			-Main.screenPosition + Projectile.Size * 0.5f, includeBacksides: false, tryStoppingOddBug: true);
		TrailStrip.DrawTrail();

		Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
		for (int index = 0; index < headPositions.Length; index++)
		{
			float alongHead = index / (float)(headPositions.Length - 1);
			headPositions[index] = Vector2.Lerp(Projectile.Center + direction * 68f,
				Projectile.Center - direction * 60f, alongHead);
			headRotations[index] = direction.ToRotation();
		}

		// The fired spell keeps the same pointed crystal focus formed during charging.
		GameShaders.Misc["MagicMissile"]
			.UseSaturation(-2.8f)
			.UseOpacity(4f)
			.Apply();
		HeadStrip.PrepareStripWithProceduralPadding(headPositions, headRotations, HeadColor, HeadWidth,
			-Main.screenPosition, includeBacksides: false, tryStoppingOddBug: true);
		HeadStrip.DrawTrail();
		Main.pixelShader.CurrentTechnique.Passes[0].Apply();
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

	private void SpawnImpactEffects()
	{
		if (Projectile.localAI[0] != 0f || Projectile.owner != Main.myPlayer)
		{
			return;
		}

		Projectile.localAI[0] = 1f;
		Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
			ModContent.ProjectileType<MoonstoneExplosionProjectile>(), (int)(Projectile.damage * 0.6f),
			Projectile.knockBack, Projectile.owner);
		Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
			ModContent.ProjectileType<MoonstoneConvergenceProjectile>(), (int)(Projectile.damage * 0.8f),
			Projectile.knockBack, Projectile.owner);
	}

	private static Color TrailColor(float progress)
	{
		Color color = Color.Lerp(new Color(245, 255, 255), new Color(125, 220, 255),
			Utils.GetLerpValue(-0.2f, 0.65f, progress, clamped: true));
		color *= 1f - Utils.GetLerpValue(0f, 0.98f, progress, clamped: false);
		color.A = 0;
		return color;
	}

	private static float TrailWidth(float progress)
	{
		float opening = Utils.GetLerpValue(0f, 0.2f, progress, clamped: true);
		float curvedOpening = 1f - (1f - opening) * (1f - opening);
		return MathHelper.Lerp(0f, 40f, curvedOpening);
	}

	private static Color HeadColor(float progress)
	{
		Color color = Color.Lerp(new Color(255, 255, 255), new Color(155, 230, 255), progress);
		color.A = 0;
		return color;
	}

	private static float HeadWidth(float progress)
	{
		if (progress <= 0.42f)
		{
			return MathHelper.SmoothStep(0f, 30f, progress / 0.42f);
		}

		return MathHelper.SmoothStep(30f, 4f, (progress - 0.42f) / 0.58f);
	}
}
