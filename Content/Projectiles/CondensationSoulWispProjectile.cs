using Microsoft.Xna.Framework;
using SoulsOfTerra.Content.Tiles;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class CondensationSoulWispProjectile : ModProjectile
{
	private const int WispCount = 7;
	private const int TravelDuration = 36;
	private const int StaggerTicks = 3;
	private const int TrailLength = 18;
	private const long VisualSoulValue = 100;
	private const float VisualScale = 0.55f;
	private const float TrailScaleMultiplier = 2.5f;
	private readonly Vector2[] trailPositions = new Vector2[TrailLength];
	private Vector2 startPosition;
	private Vector2 controlPoint;
	private int age;
	private bool initialized;

	public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.LostSoulFriendly}";

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
		ProjectileID.Sets.TrailingMode[Type] = 0;
	}

	public override void SetDefaults()
	{
		Projectile.width = 10;
		Projectile.height = 10;
		Projectile.timeLeft = 90;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.penetrate = -1;
		Projectile.scale = 0.48f;
	}

	public override bool ShouldUpdatePosition() => false;

	public override void AI()
	{
		if (!initialized)
		{
			InitializePath();
		}

		int wispIndex = (int)Projectile.ai[2];
		int delay = wispIndex * StaggerTicks;
		if (age++ < delay)
		{
			Projectile.Opacity = 0f;
			return;
		}

		if (age == delay + 1 && wispIndex == 0 && Main.netMode != NetmodeID.Server)
		{
			SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.6f, Pitch = -0.45f }, startPosition);
		}

		float progress = MathHelper.Clamp((age - delay) / (float)TravelDuration, 0f, 1f);
		float easedProgress = progress * progress * (3f - 2f * progress);
		Vector2 target = new(Projectile.ai[0], Projectile.ai[1]);
		Vector2 previousPosition = Projectile.Center;
		Projectile.Center = QuadraticBezier(startPosition, controlPoint, target, easedProgress);
		RecordTrail(previousPosition);
		Projectile.velocity = Projectile.Center - previousPosition;
		Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
		Projectile.Opacity = MathHelper.Clamp(progress * 5f, 0f, 1f);
		Lighting.AddLight(Projectile.Center, 0.025f, 0.18f, 0.13f);

		if (Main.rand.NextBool(4))
		{
			Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.GreenTorch, -Projectile.velocity * 0.08f, 130, new Color(115, 255, 205), 0.55f);
			dust.noGravity = true;
		}

		if (progress >= 1f)
		{
			CreateArrivalBurst(wispIndex == WispCount - 1);
			Projectile.Kill();
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		// Condensation uses the same rim, transparent core, internal wisps, and trail as dropped souls.
		SoulOrbProjectile.DrawSoulVisual(Projectile, VisualSoulValue, false, Projectile.Opacity, VisualScale,
			trailPositions, TrailScaleMultiplier);
		return false;
	}

	public static void Spawn(Player player, Point16 shrineTopLeft)
	{
		Vector2 target = shrineTopLeft.ToWorldCoordinates(TerraShrineTile.Width * 8f, TerraShrineTile.Height * 8f);
		IEntitySource source = new EntitySource_Misc("SoulsOfTerra:CondensationEffect");
		for (int index = 0; index < WispCount; index++)
		{
			Projectile.NewProjectile(source, player.Center, Vector2.Zero, ModContent.ProjectileType<CondensationSoulWispProjectile>(),
				0, 0f, player.whoAmI, target.X, target.Y, index);
		}
	}

	private void InitializePath()
	{
		initialized = true;
		startPosition = Projectile.Center;
		Vector2 target = new(Projectile.ai[0], Projectile.ai[1]);
		Vector2 direction = (target - startPosition).SafeNormalize(Vector2.UnitX);
		Vector2 perpendicular = new(-direction.Y, direction.X);
		int wispIndex = (int)Projectile.ai[2];
		float side = wispIndex % 2 == 0 ? 1f : -1f;
		float arcHeight = 34f + wispIndex % 3 * 12f;
		controlPoint = Vector2.Lerp(startPosition, target, 0.5f) + perpendicular * side * arcHeight;
	}

	private void CreateArrivalBurst(bool finalWisp)
	{
		if (Main.netMode == NetmodeID.Server)
		{
			return;
		}

		for (int index = 0; index < (finalWisp ? 12 : 3); index++)
		{
			Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.GreenTorch, Main.rand.NextVector2Circular(2f, 2f),
				100, new Color(125, 255, 205), finalWisp ? 0.9f : 0.55f);
			dust.noGravity = true;
		}

		if (finalWisp)
		{
			SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.7f, Pitch = 0.35f }, Projectile.Center);
		}
	}

	private void RecordTrail(Vector2 previousCenter)
	{
		// Manual history remains reliable for a projectile positioned along a Bezier path.
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
