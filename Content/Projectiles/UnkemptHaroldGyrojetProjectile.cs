using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Common.Rendering;
using SoulsOfTerra.Systems;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class UnkemptHaroldGyrojetProjectile : ModProjectile, IPixelatedDrawable
{
	private static readonly VertexStrip TrailStrip = new();
	private static readonly VertexStrip HeadStrip = new();
	// Limiting instances turns a seven-gyrojet impact into one compact, heavy report.
	private static readonly SoundStyle ExplosionSound = SoundID.Item14 with
	{
		Volume = 0.42f,
		Pitch = -0.12f,
		PitchVariance = 0.08f,
		MaxInstances = 2,
		SoundLimitBehavior = SoundLimitBehavior.IgnoreNew
	};
	public const float ForwardSpeed = 12f;
	public const float FirstSplitDistance = 6f * 16f;
	public const float SecondSplitDistance = 12f * 16f;
	private const float CoreBase = 3f;
	private const float CoreRate = 0.055f;
	private const float PeelGap = 28f;
	private const float PeelRate = 0.18f;
	private const float PostPeelRate = 0.07f;
	private readonly Vector2[] headPositions = new Vector2[8];
	private readonly float[] headRotations = new float[8];
	private Vector2 muzzle;
	private Vector2 aim;
	private float distance;
	private bool initialized;

	public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.RocketI}";

	// New pellets spawn on top of the current outers, then slide out. Core stays nearly parallel.
	public static float OffsetAt(int lane, float traveled)
	{
		int abs = Math.Abs(lane);
		if (abs == 0)
		{
			return 0f;
		}

		float offset = CoreGap(traveled);
		if (abs >= 2)
		{
			offset += SplitGap(traveled, FirstSplitDistance);
		}

		if (abs >= 3)
		{
			offset += SplitGap(traveled, SecondSplitDistance);
		}

		return Math.Sign(lane) * offset;
	}

	private static float CoreGap(float traveled) => CoreBase + CoreRate * traveled;

	private static float SplitGap(float traveled, float splitAt)
	{
		float t = Math.Max(0f, traveled - splitAt);
		float peelTravel = PeelGap / PeelRate;
		if (t <= peelTravel)
		{
			return t * PeelRate;
		}

		return PeelGap + PostPeelRate * (t - peelTravel);
	}

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.TrailCacheLength[Type] = 16;
		// Mode 0 stores positions only; left shots need oldRot or RainbowRod collapses.
		ProjectileID.Sets.TrailingMode[Type] = 3;
		ProjectileID.Sets.DrawScreenCheckFluff[Type] = 180;
	}

	public override void SetDefaults()
	{
		Projectile.width = 16;
		Projectile.height = 16;
		Projectile.friendly = true;
		Projectile.DamageType = DamageClass.Ranged;
		Projectile.penetrate = 1;
		Projectile.timeLeft = 120;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.netImportant = true;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = -1;
	}

	public override bool ShouldUpdatePosition() => false;

	public override bool CanHitPlayer(Player target) => false;

	public override void OnSpawn(IEntitySource source) => InitializeFromSpawn();

	public override void SendExtraAI(BinaryWriter writer)
	{
		writer.WriteVector2(muzzle);
		writer.WriteVector2(aim);
		writer.Write(distance);
	}

	public override void ReceiveExtraAI(BinaryReader reader)
	{
		muzzle = reader.ReadVector2();
		aim = reader.ReadVector2();
		distance = reader.ReadSingle();
		initialized = true;
	}

	public override void AI()
	{
		if (!initialized)
		{
			InitializeFromSpawn();
		}

		float nextDistance = distance + ForwardSpeed;
		Vector2 nextCenter = PositionAt(nextDistance);
		Vector2 nextTopLeft = nextCenter - Projectile.Size * 0.5f;
		if (Collision.SolidCollision(nextTopLeft, Projectile.width, Projectile.height))
		{
			Detonate();
			return;
		}

		Vector2 previousCenter = Projectile.Center;
		distance = nextDistance;
		Projectile.ai[2] = distance;
		Projectile.Center = nextCenter;
		Vector2 travel = nextCenter - previousCenter;
		Projectile.velocity = travel;
		Projectile.rotation = travel.ToRotation();
		Projectile.spriteDirection = travel.X >= 0f ? 1 : -1;
		Lighting.AddLight(Projectile.Center, 1.05f, 0.78f, 0.28f);

		TrySplit();

		if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(3))
		{
			Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, -travel * 0.12f, 80,
				new Color(255, 230, 140), 0.85f);
			dust.noGravity = true;
		}
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => Detonate();

	public override bool PreDraw(ref Color lightColor) => false;

	public void DrawPixelated(SpriteBatch spriteBatch)
	{
		Vector2 pixelGridOffset = PixelatedRenderSystem.CameraRemainder;
		GameShaders.Misc["MagicMissile"]
			.UseSaturation(-2.6f)
			.UseOpacity(3.8f)
			.Apply();
		TrailStrip.PrepareStripWithProceduralPadding(Projectile.oldPos, Projectile.oldRot, TrailColor, TrailWidth,
			-Main.screenPosition + pixelGridOffset + Projectile.Size * 0.5f, includeBacksides: false,
			tryStoppingOddBug: true);
		TrailStrip.DrawTrail();

		Vector2 direction = Projectile.velocity.LengthSquared() > 0.01f
			? Projectile.velocity.SafeNormalize(Vector2.UnitX)
			: aim;
		for (int index = 0; index < headPositions.Length; index++)
		{
			float alongHead = index / (float)(headPositions.Length - 1);
			headPositions[index] = Vector2.Lerp(Projectile.Center + direction * 14f,
				Projectile.Center - direction * 16f, alongHead);
			headRotations[index] = direction.ToRotation();
		}

		GameShaders.Misc["MagicMissile"]
			.UseSaturation(-2.8f)
			.UseOpacity(4f)
			.Apply();
		HeadStrip.PrepareStripWithProceduralPadding(headPositions, headRotations, HeadColor, HeadWidth,
			-Main.screenPosition + pixelGridOffset, includeBacksides: false, tryStoppingOddBug: true);
		HeadStrip.DrawTrail();
		Main.pixelShader.CurrentTechnique.Passes[0].Apply();
	}

	private void InitializeFromSpawn()
	{
		aim = Projectile.velocity.SafeNormalize(Vector2.UnitX);
		distance = Projectile.ai[2];
		int lane = Lane();
		muzzle = Projectile.Center - aim * distance - Perp() * OffsetAt(lane, distance);
		initialized = true;
	}

	private void TrySplit()
	{
		int nextSplit = (int)Projectile.ai[1];
		if (nextSplit == 0 || Projectile.localAI[0] != 0f)
		{
			return;
		}

		float required = nextSplit == 1 ? FirstSplitDistance : SecondSplitDistance;
		if (distance < required)
		{
			return;
		}

		Projectile.localAI[0] = 1f;
		Projectile.netUpdate = true;
		int lane = Lane();
		int childLane = lane + Math.Sign(lane);
		Vector2 spawn = PositionAt(distance, childLane);
		SpawnPeelDust(spawn);
		if (Projectile.owner != Main.myPlayer)
		{
			return;
		}

		float childNextSplit = nextSplit == 1 ? 2f : 0f;
		Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawn, aim * ForwardSpeed,
			Type, Projectile.damage, Projectile.knockBack, Projectile.owner, childLane, childNextSplit, distance);
	}

	private void Detonate()
	{
		if (Projectile.localAI[1] != 0f)
		{
			return;
		}

		Projectile.localAI[1] = 1f;
		if (Projectile.owner == Main.myPlayer)
		{
			int blastDamage = Math.Max(1, (int)(Projectile.damage * 0.4f));
			Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
				ModContent.ProjectileType<UnkemptHaroldExplosionProjectile>(), blastDamage,
				Projectile.knockBack * 0.5f, Projectile.owner);
		}

		if (Main.netMode != NetmodeID.Server)
		{
			SoundEngine.PlaySound(ExplosionSound, Projectile.Center);
			CongregationCameraSystem.AddShake(Projectile.Center, 1.5f);
		}

		Projectile.Kill();
	}

	private Vector2 PositionAt(float traveled) => PositionAt(traveled, Lane());

	private Vector2 PositionAt(float traveled, int lane) =>
		muzzle + aim * traveled + Perp() * OffsetAt(lane, traveled);

	private Vector2 Perp() => aim.RotatedBy(MathHelper.PiOver2);

	private int Lane() => (int)MathF.Round(Projectile.ai[0]);

	private void SpawnPeelDust(Vector2 spawn)
	{
		if (Main.netMode == NetmodeID.Server)
		{
			return;
		}

		Vector2 outward = Perp() * Math.Sign(Lane());
		for (int index = 0; index < 6; index++)
		{
			Dust dust = Dust.NewDustPerfect(spawn, DustID.GoldFlame,
				aim * Main.rand.NextFloat(1.2f, 3.5f) + outward * Main.rand.NextFloat(0.6f, 2.4f),
				60, new Color(255, 230, 140), Main.rand.NextFloat(0.8f, 1.2f));
			dust.noGravity = true;
		}
	}

	private static Color TrailColor(float progress)
	{
		Color color = Color.Lerp(new Color(255, 210, 90), new Color(255, 70, 16),
			Utils.GetLerpValue(-0.1f, 0.7f, progress, clamped: true));
		color *= 1f - Utils.GetLerpValue(0f, 0.96f, progress, clamped: false);
		color.A = 0;
		return color;
	}

	private static float TrailWidth(float progress)
	{
		float opening = Utils.GetLerpValue(0f, 0.22f, progress, clamped: true);
		return MathHelper.Lerp(0f, 18f, 1f - (1f - opening) * (1f - opening));
	}

	private static Color HeadColor(float progress)
	{
		Color color = Color.Lerp(new Color(255, 250, 210), new Color(255, 140, 40), progress);
		color.A = 0;
		return color;
	}

	private static float HeadWidth(float progress)
	{
		if (progress <= 0.4f)
		{
			return MathHelper.SmoothStep(0f, 12f, progress / 0.4f);
		}

		return MathHelper.SmoothStep(12f, 3f, (progress - 0.4f) / 0.6f);
	}
}
