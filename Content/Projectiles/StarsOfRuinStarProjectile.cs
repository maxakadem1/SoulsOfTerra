using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Common.Rendering;
using SoulsOfTerra.Systems;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class StarsOfRuinStarProjectile : ModProjectile, IPixelatedDrawable
{
	private static readonly VertexStrip TrailStrip = new();
	private static readonly VertexStrip HeadStrip = new();
	private static Texture2D cosmicMistTexture;
	private const float FlightSpeed = 20f;
	private const float HomingInertia = 5f;
	private const float WaveAmplitude = 2.2f;
	private const float WaveFrequency = 0.34f;
	private const int OpeningCurveDuration = 26;
	private const int MaximumFlightTime = 90;
	private const int FadeDuration = 15;
	private const int MaxLife = StarsOfRuinCastProjectile.ConjureDuration +
		StarsOfRuinCastProjectile.SpawnInterval * (StarsOfRuinCastProjectile.StarCount - 1) + MaximumFlightTime + 20;
	private readonly Vector2[] headPositions = new Vector2[8];
	private readonly float[] headRotations = new float[8];
	private Vector2 openingOrigin;
	private bool openingOriginInitialized;

	private bool Launched => Projectile.velocity.LengthSquared() > FlightSpeed * FlightSpeed * 0.25f;

	public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.RainbowRodBullet}";

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.CultistIsResistantTo[Type] = true;
		ProjectileID.Sets.TrailCacheLength[Type] = 28;
		ProjectileID.Sets.TrailingMode[Type] = 3;
	}

	public override void SetDefaults()
	{
		Projectile.width = 14;
		Projectile.height = 14;
		Projectile.friendly = true;
		Projectile.DamageType = DamageClass.Magic;
		Projectile.penetrate = 1;
		Projectile.timeLeft = MaxLife;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = -1;
	}

	public override Color? GetAlpha(Color lightColor) => new Color(220, 235, 255, 0) * Projectile.Opacity;

	public override bool? CanDamage() => Launched;

	public override bool ShouldUpdatePosition() => Launched && Projectile.localAI[1] >= OpeningCurveDuration;

	public override void SendExtraAI(BinaryWriter writer)
	{
		writer.WriteVector2(openingOrigin);
		writer.Write(openingOriginInitialized);
	}

	public override void ReceiveExtraAI(BinaryReader reader)
	{
		openingOrigin = reader.ReadVector2();
		openingOriginInitialized = reader.ReadBoolean();
	}

	public override void AI()
	{
		Player player = Main.player[Projectile.owner];
		int index = (int)Projectile.ai[1];

		if (!Launched)
		{
			if (!player.active || player.dead)
			{
				Projectile.Kill();
				return;
			}

			Vector2 aim = Projectile.ai[2].ToRotationVector2();
			Vector2 staffTip = StarsOfRuinCastProjectile.GetStaffTip(player, aim);
			Projectile.Center = Vector2.Lerp(Projectile.Center, staffTip, 0.72f);
			Projectile.velocity = Vector2.Zero;
			Projectile.rotation += 0.16f;
			Lighting.AddLight(Projectile.Center, 0.08f, 0.22f, 0.68f);
			SpawnHangSparkle();

			Projectile.localAI[0]++;
			int launchDelay = StarsOfRuinCastProjectile.ConjureDuration +
				StarsOfRuinCastProjectile.GetLaunchRank(index) * StarsOfRuinCastProjectile.SpawnInterval;
			if (Projectile.localAI[0] >= launchDelay && Projectile.owner == Main.myPlayer)
			{
				Launch(aim);
			}

			return;
		}

		float flight = Projectile.localAI[1]++;
		if (flight >= MaximumFlightTime)
		{
			Projectile.Kill();
			return;
		}

		if (flight < OpeningCurveDuration)
		{
			FollowOpeningCurve(flight);
		}
		else
		{
			Projectile.tileCollide = true;
			Vector2 fallback = Projectile.velocity.SafeNormalize(Projectile.ai[2].ToRotationVector2());
			Vector2 desiredDirection = Projectile.ai[2].ToRotationVector2();
			if (GetLockedTarget() is NPC target)
			{
				desiredDirection = (target.Center - Projectile.Center).SafeNormalize(fallback);
			}

			// Independent phases add a slight weave without overpowering the target-seeking direction.
			Vector2 waveNormal = new(-desiredDirection.Y, desiredDirection.X);
			float wavePhase = (flight - OpeningCurveDuration) * WaveFrequency + Projectile.identity * 1.37f;
			Vector2 desiredVelocity = desiredDirection * FlightSpeed + waveNormal * MathF.Sin(wavePhase) * WaveAmplitude;
			Projectile.velocity = (Projectile.velocity * (HomingInertia - 1f) + desiredVelocity) / HomingInertia;
			Projectile.velocity = Projectile.velocity.SafeNormalize(fallback) * FlightSpeed;
		}

		Projectile.rotation = Projectile.velocity.ToRotation();
		Projectile.Opacity = MathHelper.Clamp((MaximumFlightTime - flight) / FadeDuration, 0f, 1f);
		Lighting.AddLight(Projectile.Center, 0.1f * Projectile.Opacity, 0.3f * Projectile.Opacity,
			0.9f * Projectile.Opacity);

		if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(2))
		{
			Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(3f, 3f),
				DustID.BlueCrystalShard, -Projectile.velocity * Main.rand.NextFloat(0.06f, 0.16f), 50,
				new Color(105, 165, 255), Main.rand.NextFloat(0.45f, 0.85f));
			dust.noGravity = true;
		}
	}

	public override bool OnTileCollide(Vector2 oldVelocity)
	{
		return true;
	}

	public override void OnKill(int timeLeft)
	{
		if (Main.netMode == NetmodeID.Server)
		{
			return;
		}

		for (int i = 0; i < 8; i++)
		{
			Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.BlueCrystalShard,
				Main.rand.NextVector2Circular(2.8f, 2.8f), 35, new Color(130, 185, 255),
				Main.rand.NextFloat(0.7f, 1.15f));
			dust.noGravity = true;
		}

		SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.12f, Pitch = 0.35f }, Projectile.Center);
	}

	public override bool PreDraw(ref Color lightColor) => false;

	public void DrawPixelated(SpriteBatch spriteBatch)
	{
		// Both formation and flight share the same stable pixel grid.
		if (Launched)
		{
			DrawComet();
		}
		else
		{
			DrawSpark();
		}
	}

	private void DrawComet()
	{
		Texture2D mist = GetCosmicMistTexture();
		DrawCosmicMist(mist, mist.Size() * 0.5f);

		GameShaders.Misc["RainbowRod"]
			.UseSaturation(-2.8f)
			.UseOpacity(4.6f)
			.Apply();
		TrailStrip.PrepareStripWithProceduralPadding(Projectile.oldPos, Projectile.oldRot, TrailColor, TrailWidth,
			-Main.screenPosition + PixelatedRenderSystem.CameraRemainder + Projectile.Size * 0.5f,
			includeBacksides: false, tryStoppingOddBug: true);
		TrailStrip.DrawTrail();

		Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
		for (int index = 0; index < headPositions.Length; index++)
		{
			float alongHead = index / (float)(headPositions.Length - 1);
			headPositions[index] = Vector2.Lerp(Projectile.Center + direction * 18f,
				Projectile.Center - direction * 22f, alongHead);
			headRotations[index] = direction.ToRotation();
		}

		GameShaders.Misc["MagicMissile"]
			.UseSaturation(-2.8f)
			.UseOpacity(4.2f)
			.Apply();
		HeadStrip.PrepareStripWithProceduralPadding(headPositions, headRotations, HeadColor, HeadWidth,
			-Main.screenPosition + PixelatedRenderSystem.CameraRemainder, includeBacksides: false,
			tryStoppingOddBug: true);
		HeadStrip.DrawTrail();
		Main.pixelShader.CurrentTechnique.Passes[0].Apply();

		Texture2D glow = SoulOrbProjectile.GetGlowTexture();
		Vector2 origin = glow.Size() * 0.5f;
		Vector2 center = Projectile.Center - Main.screenPosition;
		DrawTrailMotes(glow, origin);
		Main.EntitySpriteDraw(glow, center, null, new Color(80, 125, 255, 0) * Projectile.Opacity,
			direction.ToRotation(), origin, new Vector2(0.58f, 0.21f), SpriteEffects.None);
		Main.EntitySpriteDraw(glow, center, null, new Color(225, 240, 255, 0) * Projectile.Opacity,
			direction.ToRotation(), origin, new Vector2(0.42f, 0.12f), SpriteEffects.None);
		Main.EntitySpriteDraw(glow, center, null, new Color(255, 255, 255, 0), 0f, origin, 0.16f, SpriteEffects.None);
	}

	private void DrawCosmicMist(Texture2D mist, Vector2 origin)
	{
		for (int index = 2; index < Projectile.oldPos.Length; index += 4)
		{
			if (Projectile.oldPos[index] == Vector2.Zero)
			{
				continue;
			}

			float trailProgress = index / (float)Projectile.oldPos.Length;
			float strength = MathF.Pow(1f - trailProgress, 0.72f) * Projectile.Opacity;
			float phase = Projectile.identity * 1.713f + index * 2.381f;
			Vector2 direction = Projectile.oldRot[index].ToRotationVector2();
			Vector2 normal = new(-direction.Y, direction.X);
			Vector2 center = Projectile.oldPos[index] + Projectile.Size * 0.5f - Main.screenPosition;
			float drift = MathF.Sin(phase + Main.GlobalTimeWrappedHourly * 0.8f) * 14f;
			float stretch = 0.58f + 0.18f * MathF.Sin(phase * 0.73f);

			// Two offset low-opacity clouds blend into the blue-violet nebula between lanes.
			Main.EntitySpriteDraw(mist, center + normal * drift, null,
				new Color(32, 82, 225, 0) * (strength * 0.24f), direction.ToRotation(), origin,
				new Vector2(stretch * 2.15f, stretch * 1.18f), SpriteEffects.None);
			Main.EntitySpriteDraw(mist, center - normal * drift * 0.55f, null,
				new Color(132, 62, 235, 0) * (strength * 0.14f), direction.ToRotation() - 0.28f, origin,
				new Vector2(stretch * 1.55f, stretch), SpriteEffects.None);
		}
	}

	private void DrawTrailMotes(Texture2D glow, Vector2 origin)
	{
		for (int index = 4; index < Projectile.oldPos.Length; index += 5)
		{
			if (Projectile.oldPos[index] == Vector2.Zero)
			{
				continue;
			}

			float strength = (1f - index / (float)Projectile.oldPos.Length) * Projectile.Opacity;
			Vector2 normal = Projectile.oldRot[index].ToRotationVector2().RotatedBy(MathHelper.PiOver2);
			float scatter = MathF.Sin(Projectile.identity * 1.91f + index * 2.43f) * 5f;
			Vector2 position = Projectile.oldPos[index] + Projectile.Size * 0.5f + normal * scatter - Main.screenPosition;
			Main.EntitySpriteDraw(glow, position, null, new Color(75, 145, 255, 0) * (strength * 0.72f), 0f,
				origin, 0.055f + strength * 0.045f, SpriteEffects.None);
		}
	}

	private void DrawSpark()
	{
		Texture2D glow = SoulOrbProjectile.GetGlowTexture();
		Vector2 origin = glow.Size() * 0.5f;
		Vector2 center = Projectile.Center - Main.screenPosition;
		float pulse = 0.85f + 0.15f * MathF.Sin(Main.GlobalTimeWrappedHourly * 14f + Projectile.identity);
		Main.EntitySpriteDraw(glow, center, null, new Color(35, 75, 235, 0) * 0.72f, 0f, origin, 0.34f * pulse,
			SpriteEffects.None);
		Main.EntitySpriteDraw(glow, center, null, new Color(145, 195, 255, 0), Projectile.rotation, origin,
			new Vector2(0.22f, 0.06f) * pulse, SpriteEffects.None);
		Main.EntitySpriteDraw(glow, center, null, new Color(145, 195, 255, 0), Projectile.rotation + MathHelper.PiOver2,
			origin, new Vector2(0.22f, 0.06f) * pulse, SpriteEffects.None);
		Main.EntitySpriteDraw(glow, center, null, new Color(255, 255, 255, 0), 0f, origin, 0.09f * pulse,
			SpriteEffects.None);
	}

	private void Launch(Vector2 aim)
	{
		Projectile.localAI[1] = 0f;
		int index = (int)Projectile.ai[1];
		Vector2 curveAim = aim;
		if (GetLockedTarget() is NPC target)
		{
			curveAim = (target.Center - Projectile.Center).SafeNormalize(aim);
		}

		// The shared origin is synchronized so every client renders the same authored lane.
		openingOrigin = Projectile.Center;
		openingOriginInitialized = true;
		Projectile.ai[2] = curveAim.ToRotation();
		Projectile.velocity = curveAim * FlightSpeed;
		Projectile.tileCollide = true;
		Projectile.netUpdate = true;
		if (Main.netMode != NetmodeID.Server)
		{
			float pitch = 0.16f + StarsOfRuinCastProjectile.GetLaunchRank(index) * 0.018f;
			SoundEngine.PlaySound(SoundID.Item9 with { Volume = 0.26f, Pitch = pitch }, Projectile.Center);
		}
	}

	private void FollowOpeningCurve(float flight)
	{
		if (!openingOriginInitialized)
		{
			openingOrigin = Projectile.Center;
			openingOriginInitialized = true;
		}

		int index = (int)Projectile.ai[1];
		int pairIndex = index / 2;
		float pairProgress = pairIndex / 5f;
		float side = index % 2 == 0 ? -1f : 1f;
		Vector2 forward = Projectile.ai[2].ToRotationVector2();
		Vector2 normal = new(-forward.Y, forward.X);
		float depth = MathHelper.Lerp(62f, 172f, pairProgress);
		float endOffset = MathHelper.Lerp(10f, 104f, pairProgress);
		float reach = MathHelper.Lerp(330f, 440f, pairProgress);

		// Six mirrored pairs fill both halves of the teardrop without changing the forward curve profile.
		Vector2 controlOne = openingOrigin + forward * MathHelper.Lerp(48f, 72f, pairProgress) +
			normal * side * depth * 0.22f;
		Vector2 controlTwo = openingOrigin + forward * reach * 0.42f + normal * side * depth;
		Vector2 end = openingOrigin + forward * reach + normal * side * endOffset;
		float progress = MathHelper.Clamp((flight + 1f) / OpeningCurveDuration, 0f, 1f);
		float previousProgress = MathHelper.Clamp(flight / OpeningCurveDuration, 0f, 1f);
		Vector2 previous = CubicBezier(openingOrigin, controlOne, controlTwo, end, previousProgress);
		Vector2 next = CubicBezier(openingOrigin, controlOne, controlTwo, end, progress);
		Vector2 movement = next - previous;
		Vector2 allowedMovement = Collision.TileCollision(previous - Projectile.Size * 0.5f, movement,
			Projectile.width, Projectile.height);
		if (Vector2.DistanceSquared(movement, allowedMovement) > 0.01f)
		{
			// Manual curve movement needs an explicit swept check to prevent tunneling through thin walls.
			Projectile.Center = previous + allowedMovement;
			Projectile.Kill();
			return;
		}

		Projectile.Center = next;
		Projectile.velocity = movement.SafeNormalize(forward) * FlightSpeed;
	}

	private static Vector2 CubicBezier(Vector2 start, Vector2 controlOne, Vector2 controlTwo, Vector2 end, float progress)
	{
		float inverse = 1f - progress;
		return inverse * inverse * inverse * start +
			3f * inverse * inverse * progress * controlOne +
			3f * inverse * progress * progress * controlTwo +
			progress * progress * progress * end;
	}

	private NPC GetLockedTarget()
	{
		int targetIndex = (int)Projectile.ai[0] - 1;
		if (targetIndex < 0 || targetIndex >= Main.maxNPCs)
		{
			return null;
		}

		NPC target = Main.npc[targetIndex];
		return target.active && target.CanBeChasedBy(this) ? target : null;
	}

	private void SpawnHangSparkle()
	{
		if (Main.netMode == NetmodeID.Server || !Main.rand.NextBool(4))
		{
			return;
		}

		Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
			DustID.BlueCrystalShard, Main.rand.NextVector2Circular(0.4f, 0.4f), 70, new Color(115, 170, 255), 0.45f);
		dust.noGravity = true;
	}

	private static Color TrailColor(float progress)
	{
		Color color = Color.Lerp(new Color(250, 250, 255), new Color(55, 125, 255),
			Utils.GetLerpValue(0f, 0.35f, progress, clamped: true));
		color = Color.Lerp(color, new Color(15, 30, 135), Utils.GetLerpValue(0.45f, 1f, progress, clamped: true));
		color *= 1f - Utils.GetLerpValue(0.12f, 1f, progress, clamped: true);
		color.A = 0;
		return color;
	}

	private static float TrailWidth(float progress)
	{
		float tip = Utils.GetLerpValue(0f, 0.08f, progress, clamped: true);
		float taper = 1f - Utils.GetLerpValue(0.1f, 1f, progress, clamped: true);
		return MathHelper.Lerp(0f, 11f, tip) * taper;
	}

	private static Color HeadColor(float progress)
	{
		Color color = Color.Lerp(new Color(255, 255, 255), new Color(90, 155, 255), progress);
		color.A = 0;
		return color;
	}

	private static float HeadWidth(float progress)
	{
		if (progress <= 0.35f)
		{
			return MathHelper.SmoothStep(0f, 7.5f, progress / 0.35f);
		}

		return MathHelper.SmoothStep(7.5f, 1.2f, (progress - 0.35f) / 0.65f);
	}

	private static Texture2D GetCosmicMistTexture()
	{
		cosmicMistTexture ??= CreateCosmicMistTexture();
		return cosmicMistTexture;
	}

	private static Texture2D CreateCosmicMistTexture()
	{
		const int size = 96;
		Texture2D texture = new(Main.instance.GraphicsDevice, size, size);
		Color[] pixels = new Color[size * size];
		Vector2 center = new((size - 1) * 0.5f);
		float radius = size * 0.5f;

		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				float radialFade = MathHelper.Clamp(1f - Vector2.Distance(new Vector2(x, y), center) / radius, 0f, 1f);
				float broadNoise = 0.5f + 0.5f * MathF.Sin(x * 0.17f + MathF.Sin(y * 0.11f) * 2.3f);
				float fineNoise = 0.5f + 0.5f * MathF.Sin(x * 0.43f - y * 0.31f + MathF.Sin(x * 0.09f) * 1.7f);
				float density = radialFade * radialFade * MathHelper.Lerp(0.24f, 1f, broadNoise * 0.68f + fineNoise * 0.32f);
				pixels[y * size + x] = Color.FromNonPremultiplied(255, 255, 255, (int)(density * 220f));
			}
		}

		texture.SetData(pixels);
		return texture;
	}
}
