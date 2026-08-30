using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Common.Rendering;
using SoulsOfTerra.Content.Projectiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class SoulCrystalReleaseProjectile : ModProjectile, IPixelatedDrawable
{
	private const int TrailLength = 12;
	private const int TravelDuration = 24;
	private readonly Vector2[] trailPositions = new Vector2[TrailLength];
	private Vector2 startPosition;
	private Vector2 controlPoint;
	private int age;
	private bool initialized;

	public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.LostSoulFriendly}";

	public override void SetDefaults()
	{
		Projectile.width = 10;
		Projectile.height = 10;
		Projectile.timeLeft = 60;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.penetrate = -1;
	}

	public override bool ShouldUpdatePosition() => false;

	public override void AI()
	{
		if (!initialized)
		{
			InitializePath();
		}

		int delay = (int)Projectile.ai[2] * 2;
		if (age++ < delay)
		{
			Projectile.Opacity = 0f;
			return;
		}

		Player player = Main.player[Projectile.owner];
		if (!player.active)
		{
			Projectile.Kill();
			return;
		}

		float progress = MathHelper.Clamp((age - delay) / (float)TravelDuration, 0f, 1f);
		float easedProgress = progress * progress * (3f - 2f * progress);
		Vector2 previousCenter = Projectile.Center;
		Projectile.Center = QuadraticBezier(startPosition, controlPoint, player.Center, easedProgress);
		RecordTrail(previousCenter);
		Projectile.Opacity = MathHelper.Clamp(progress * 6f, 0f, 1f) * (1f - progress * 0.4f);
		Lighting.AddLight(Projectile.Center, 0.08f, 0.16f, 0.28f);

		if (progress >= 1f)
		{
			Projectile.Kill();
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		return false;
	}

	public void DrawPixelated(SpriteBatch spriteBatch)
	{
		int tier = (int)Projectile.ai[0];
		long visualValue = tier switch
		{
			1 => 100,
			2 => 1_000,
			_ => 10_000
		};
		float scale = 0.55f + tier * 0.15f;
		SoulOrbProjectile.DrawSoulVisual(Projectile, visualValue, false, Projectile.Opacity, scale, trailPositions, 1.4f);
	}

	public static void Spawn(Player player, int tier)
	{
		int wispCount = 2 + tier;
		IEntitySource source = new EntitySource_Misc("SoulsOfTerra:SoulCrystalRelease");
		for (int index = 0; index < wispCount; index++)
		{
			float angle = MathHelper.TwoPi * index / wispCount - MathHelper.PiOver2;
			Vector2 start = player.Center + angle.ToRotationVector2() * (30f + tier * 6f);
			Projectile.NewProjectile(source, start, Vector2.Zero, ModContent.ProjectileType<SoulCrystalReleaseProjectile>(),
				0, 0f, player.whoAmI, tier, 0f, index);
		}
	}

	private void InitializePath()
	{
		initialized = true;
		startPosition = Projectile.Center;
		Player player = Main.player[Projectile.owner];
		Vector2 direction = (player.Center - startPosition).SafeNormalize(Vector2.UnitY);
		Vector2 perpendicular = new(-direction.Y, direction.X);
		float side = (int)Projectile.ai[2] % 2 == 0 ? 1f : -1f;
		controlPoint = Vector2.Lerp(startPosition, player.Center, 0.5f) + perpendicular * side * 24f;
	}

	private void RecordTrail(Vector2 previousCenter)
	{
		for (int index = trailPositions.Length - 1; index > 0; index--)
		{
			trailPositions[index] = trailPositions[index - 1];
		}

		trailPositions[0] = previousCenter - Projectile.Size * 0.5f;
	}

	private static Vector2 QuadraticBezier(Vector2 start, Vector2 control, Vector2 end, float progress)
	{
		float inverse = 1f - progress;
		return inverse * inverse * start + 2f * inverse * progress * control + progress * progress * end;
	}
}
