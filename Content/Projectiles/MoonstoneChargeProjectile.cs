using Microsoft.Xna.Framework;
using SoulsOfTerra.Content.Items.Weapons.Magic;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class MoonstoneChargeProjectile : ModProjectile
{
	private static readonly VertexStrip ChargeStrip = new();
	private static readonly VertexStrip FragmentStrip = new();
	public const int ChargeDuration = 75;
	private const float StaffTipDistance = 74f;
	private readonly Vector2[] chargePositions = new Vector2[10];
	private readonly float[] chargeRotations = new float[10];
	private readonly Vector2[] fragmentPositions = new Vector2[3];
	private readonly float[] fragmentRotations = new float[3];
	private float chargeHalfWidth;
	private float fragmentHalfWidth;

	public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.RainbowRodBullet}";

	public override void SetDefaults()
	{
		Projectile.width = 2;
		Projectile.height = 2;
		Projectile.friendly = false;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.timeLeft = ChargeDuration + 10;
		Projectile.netImportant = true;
	}

	public override bool ShouldUpdatePosition() => false;

	public override void AI()
	{
		Player player = Main.player[Projectile.owner];
		if (!player.active || player.dead || player.HeldItem.type != ModContent.ItemType<MoonstoneStaff>())
		{
			Projectile.Kill();
			return;
		}

		if (Projectile.owner == Main.myPlayer)
		{
			Vector2 aim = (Main.MouseWorld - player.MountedCenter).SafeNormalize(new Vector2(player.direction, 0f));
			if (Vector2.Dot(aim, Projectile.velocity) < 0.9995f)
			{
				Projectile.velocity = aim;
				Projectile.netUpdate = true;
			}
		}

		Projectile.Center = player.MountedCenter;
		player.ChangeDir(Projectile.velocity.X >= 0f ? 1 : -1);
		player.heldProj = Projectile.whoAmI;
		player.itemTime = 2;
		player.itemAnimation = 2;
		player.itemRotation = (Projectile.velocity * player.direction).ToRotation();

		Projectile.ai[0]++;
		float progress = MathHelper.Clamp(Projectile.ai[0] / ChargeDuration, 0f, 1f);
		Vector2 tip = GetStaffTip(player);
		float compression = Utils.GetLerpValue(ChargeDuration - 8f, ChargeDuration, Projectile.ai[0], clamped: true);
		Lighting.AddLight(tip, 0.45f * progress + compression * 0.5f, 0.9f * progress + compression, 1.1f * progress + compression);
		CreateGatheringMotes(tip, progress);

		if (Projectile.ai[0] == 1f && Main.netMode != NetmodeID.Server)
		{
			SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.55f, Pitch = -0.35f }, tip);
		}

		if (Projectile.ai[0] < ChargeDuration)
		{
			if (Projectile.ai[0] == ChargeDuration - 6f)
			{
				CreateCompressionBurst(tip);
			}

			return;
		}

		if (Projectile.owner == Main.myPlayer)
		{
			Projectile.NewProjectile(Projectile.GetSource_FromThis(), tip, Projectile.velocity * 18f,
				ModContent.ProjectileType<MoonstoneBoltProjectile>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
		}

		if (Main.netMode != NetmodeID.Server)
		{
			SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.9f, Pitch = -0.15f }, tip);
		}

		Projectile.Kill();
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Player player = Main.player[Projectile.owner];
		if (!player.active)
		{
			return false;
		}

		float progress = MathHelper.Clamp(Projectile.ai[0] / ChargeDuration, 0f, 1f);
		float compression = Utils.GetLerpValue(ChargeDuration - 8f, ChargeDuration, Projectile.ai[0], clamped: true);
		Vector2 tip = GetStaffTip(player);
		Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
		float pulse = 0.92f + System.MathF.Sin(Projectile.ai[0] * 0.22f) * 0.08f;
		float length = MathHelper.Lerp(10f, 82f, progress) * pulse;
		chargeHalfWidth = MathHelper.Lerp(2f, 15f, progress) * pulse;
		for (int index = 0; index < chargePositions.Length; index++)
		{
			float alongLance = index / (float)(chargePositions.Length - 1);
			chargePositions[index] = tip + direction * length * (1f - alongLance);
			chargeRotations[index] = direction.ToRotation();
		}

		// The charge is a sharp crystal lance rather than the vanilla star projectile.
		GameShaders.Misc["MagicMissile"]
			.UseSaturation(-2.8f)
			.UseOpacity(4f)
			.Apply();
		ChargeStrip.PrepareStripWithProceduralPadding(chargePositions, chargeRotations, ChargeColor, ChargeWidth,
			-Main.screenPosition, includeBacksides: false, tryStoppingOddBug: true);
		ChargeStrip.DrawTrail();
		DrawFragmentRing(tip, direction, progress, compression, 6, 31f, 1f);
		DrawFragmentRing(tip, direction, progress, compression, 8, 46f, -1f);
		Main.pixelShader.CurrentTechnique.Passes[0].Apply();
		return false;
	}

	private static Color ChargeColor(float progress)
	{
		Color color = Color.Lerp(new Color(250, 255, 255), new Color(145, 225, 255), progress);
		color.A = 0;
		return color;
	}

	private float ChargeWidth(float progress)
	{
		if (progress <= 0.38f)
		{
			return MathHelper.SmoothStep(0f, chargeHalfWidth, progress / 0.38f);
		}

		return MathHelper.SmoothStep(chargeHalfWidth, chargeHalfWidth * 0.14f, (progress - 0.38f) / 0.62f);
	}

	private void DrawFragmentRing(Vector2 center, Vector2 aim, float chargeProgress, float compression, int fragmentCount,
		float maximumRadius, float rotationDirection)
	{
		float radius = MathHelper.Lerp(8f, maximumRadius, chargeProgress) * MathHelper.Lerp(1f, 0.38f, compression);
		float phase = Projectile.ai[0] * 0.055f * rotationDirection;
		fragmentHalfWidth = MathHelper.Lerp(0.8f, 3.8f, chargeProgress);
		for (int index = 0; index < fragmentCount; index++)
		{
			float angle = phase + MathHelper.TwoPi * index / fragmentCount;
			Vector2 radial = angle.ToRotationVector2();
			Vector2 tangent = new Vector2(-radial.Y, radial.X) * rotationDirection;
			// Flattening the ring along the aim axis makes it read as a magical chamber.
			Vector2 ringPoint = center + radial * radius + aim * radial.X * radius * 0.18f;
			float fragmentLength = MathHelper.Lerp(5f, 14f, chargeProgress);
			fragmentPositions[0] = ringPoint + tangent * fragmentLength * 0.5f;
			fragmentPositions[1] = ringPoint;
			fragmentPositions[2] = ringPoint - tangent * fragmentLength * 0.5f;
			for (int rotationIndex = 0; rotationIndex < fragmentRotations.Length; rotationIndex++)
			{
				fragmentRotations[rotationIndex] = tangent.ToRotation();
			}

			FragmentStrip.PrepareStripWithProceduralPadding(fragmentPositions, fragmentRotations, FragmentColor, FragmentWidth,
				-Main.screenPosition, includeBacksides: false, tryStoppingOddBug: true);
			FragmentStrip.DrawTrail();
		}
	}

	private static Color FragmentColor(float progress)
	{
		Color color = Color.Lerp(new Color(210, 248, 255), new Color(95, 190, 255), progress);
		color.A = 0;
		return color;
	}

	private float FragmentWidth(float progress)
	{
		return fragmentHalfWidth * (1f - System.MathF.Abs(progress * 2f - 1f));
	}

	private Vector2 GetStaffTip(Player player)
	{
		return player.MountedCenter + Projectile.velocity.SafeNormalize(Vector2.UnitX) * StaffTipDistance;
	}

	private static void CreateGatheringMotes(Vector2 tip, float progress)
	{
		if (Main.netMode == NetmodeID.Server)
		{
			return;
		}

		int attempts = 1 + (int)(progress * 3f);
		for (int index = 0; index < attempts; index++)
		{
			if (Main.rand.NextFloat() > 0.38f + progress * 0.52f)
			{
				continue;
			}

			float radius = MathHelper.Lerp(76f, 34f, progress) * Main.rand.NextFloat(0.8f, 1.2f);
			Vector2 offset = Main.rand.NextVector2Unit() * radius;
			Vector2 tangent = new(-offset.Y, offset.X);
			Vector2 velocity = -offset * 0.14f + tangent.SafeNormalize(Vector2.Zero) * 0.9f;
			Dust dust = Dust.NewDustPerfect(tip + offset, DustID.BlueCrystalShard, velocity, 55,
				new Color(205, 245, 255), MathHelper.Lerp(0.65f, 1.15f, progress));
			dust.noGravity = true;
		}
	}

	private static void CreateCompressionBurst(Vector2 tip)
	{
		if (Main.netMode == NetmodeID.Server)
		{
			return;
		}

		SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.55f, Pitch = 0.45f }, tip);
		for (int index = 0; index < 28; index++)
		{
			Vector2 offset = Main.rand.NextVector2Unit() * Main.rand.NextFloat(44f, 72f);
			Dust dust = Dust.NewDustPerfect(tip + offset, DustID.BlueCrystalShard, -offset * 0.18f, 35,
				new Color(230, 252, 255), Main.rand.NextFloat(0.85f, 1.3f));
			dust.noGravity = true;
		}
	}
}
