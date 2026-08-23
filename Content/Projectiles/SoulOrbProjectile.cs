using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Common;
using SoulsOfTerra.Players;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class SoulOrbProjectile : ModProjectile
{
	private const float CollectionRange = 12f * 16f;
	private const float MergeRange = 4f * 16f;
	private const int HomingDelay = 20;
	private const int MergeInterval = 15;
	private const int TrailLength = 24;

	private static readonly Color[] SoulGradient =
	{
		new(245, 250, 255),
		new(80, 235, 115),
		new(65, 145, 255),
		new(180, 75, 245)
	};
	private static readonly Color BossSoulColor = new(255, 190, 45);

	private static Texture2D glowTexture;
	private static Texture2D ringTexture;
	private int age;

	public long StoredSouls { get; private set; }
	public bool ContainsBossReward { get; private set; }
	public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.LostSoulFriendly}";

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.DrawScreenCheckFluff[Type] = 240;
		ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
		ProjectileID.Sets.TrailingMode[Type] = 0;
	}

	public override void SetDefaults()
	{
		Projectile.width = 16;
		Projectile.height = 16;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.netImportant = true;
		Projectile.penetrate = -1;
		Projectile.timeLeft = 2;
	}

	public override bool? CanDamage() => false;

	public override void Unload()
	{
		Texture2D glowToDispose = glowTexture;
		Texture2D ringToDispose = ringTexture;
		glowTexture = null;
		ringTexture = null;
		if (glowToDispose is null && ringToDispose is null)
		{
			return;
		}

		// FNA graphics resources must be released on the main thread.
		Main.QueueMainThreadAction(() =>
		{
			if (glowToDispose is not null && !glowToDispose.IsDisposed)
			{
				glowToDispose.Dispose();
			}

			if (ringToDispose is not null && !ringToDispose.IsDisposed)
			{
				ringToDispose.Dispose();
			}
		});
	}

	public override void AI()
	{
		Projectile.timeLeft = 2;
		Projectile.rotation += 0.025f;
		Color soulColor = GetSoulColor((float)System.Math.Log10(System.Math.Max(1, StoredSouls)), ContainsBossReward);
		Lighting.AddLight(Projectile.Center, soulColor.ToVector3() * 0.48f);

		if (!Main.dedServ && Main.rand.NextBool(8))
		{
			Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.DungeonSpirit, -Projectile.velocity * 0.08f, 150, soulColor, 0.55f);
			dust.noGravity = true;
		}

		age++;
		if (Main.netMode != NetmodeID.MultiplayerClient && age % MergeInterval == 0 && TryMerge())
		{
			return;
		}

		if (age < HomingDelay)
		{
			Projectile.velocity *= 0.94f;
			return;
		}

		Player target = FindNearestPlayer();
		if (target is null)
		{
			Projectile.velocity *= 0.94f;
			return;
		}

		Vector2 toTarget = target.Center - Projectile.Center;
		if (Main.netMode != NetmodeID.MultiplayerClient && toTarget.LengthSquared() <= 24f * 24f)
		{
			target.GetModPlayer<SoulPlayer>().AddSouls(StoredSouls);
			Projectile.Kill();
			return;
		}

		float distance = toTarget.Length();
		Vector2 direction = toTarget.SafeNormalize(Vector2.Zero);
		Vector2 perpendicular = new(-direction.Y, direction.X);
		float approach = 1f - MathHelper.Clamp(distance / CollectionRange, 0f, 1f);
		float speed = MathHelper.Lerp(4.25f, 11.5f, approach);
		float curveStrength = MathHelper.Clamp(distance / 80f, 0f, 1f) * 1.8f;
		float curve = (float)System.Math.Sin(age * 0.11f + Projectile.whoAmI * 0.73f) * curveStrength;
		Vector2 desiredVelocity = direction * speed + perpendicular * curve;
		Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.085f);
	}

	public override bool PreDraw(ref Color lightColor)
	{
		DrawSoulVisual(Projectile, StoredSouls, ContainsBossReward);
		return false;
	}

	internal static void DrawSoulVisual(Projectile projectile, long visualSouls, bool containsBossReward, float opacity = 1f,
		float scaleMultiplier = 1f, Vector2[] trailPositions = null, float trailScaleMultiplier = 1f)
	{
		glowTexture ??= CreateGlowTexture();
		ringTexture ??= CreateRingTexture();
		Vector2 origin = glowTexture.Size() * 0.5f;
		float pulse = 1f + 0.06f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 5f + projectile.whoAmI);
		float valueLog = (float)System.Math.Log10(System.Math.Max(1, visualSouls));
		float visualProgress = MathHelper.Clamp((valueLog - 1f) / 4f, 0f, 1f);
		float valueIntensity = MathHelper.Lerp(0.82f, 1.52f, visualProgress);
		Color soulColor = GetSoulColor(valueLog, containsBossReward);

		// One smooth trail keeps the reward readable during crowded fights.
		Vector2[] positions = trailPositions ?? projectile.oldPos;
		for (int i = positions.Length - 1; i >= 1; i--)
		{
			if (positions[i] == Vector2.Zero)
			{
				continue;
			}

			float trailStrength = 1f - i / (float)positions.Length;
			Vector2 trailPosition = positions[i] + projectile.Size * 0.5f - Main.screenPosition;
			float trailScale = MathHelper.Lerp(0.1f, 0.35f, trailStrength) * valueIntensity * scaleMultiplier * trailScaleMultiplier;
			Color trailColor = WithAlpha(soulColor, 95) * (trailStrength * 0.76f);
			Main.EntitySpriteDraw(glowTexture, trailPosition, null, trailColor * opacity, 0f, origin, trailScale, SpriteEffects.None);
		}

		Vector2 drawPosition = projectile.Center - Main.screenPosition;
		// A bright annular border surrounds a nearly transparent interior.
		Main.EntitySpriteDraw(glowTexture, drawPosition, null, WithAlpha(soulColor, 24) * opacity, 0f, origin, 0.52f * pulse * valueIntensity * scaleMultiplier, SpriteEffects.None);
		Main.EntitySpriteDraw(ringTexture, drawPosition, null, WithAlpha(soulColor, 225) * opacity, 0f, origin, 0.38f * pulse * valueIntensity * scaleMultiplier, SpriteEffects.None);

		// Counter-rotating wisps create a small vortex inside the soul.
		float spinSpeed = containsBossReward ? 2.1f : 3.25f;
		float spinTime = Main.GlobalTimeWrappedHourly * spinSpeed + projectile.whoAmI * 0.37f;
		float orbitRadius = MathHelper.Lerp(5.5f, 9.5f, visualProgress) * (containsBossReward ? 1.18f : 1f) * scaleMultiplier;
		Color wispColor = Color.Lerp(Color.White, soulColor, 0.55f);
		for (int wisp = 0; wisp < 2; wisp++)
		{
			float direction = wisp == 0 ? 1f : -1f;
			float phase = spinTime * direction + wisp * MathHelper.PiOver2;
			float depth = (float)System.Math.Sin(phase);
			Vector2 orbit = new((float)System.Math.Cos(phase) * orbitRadius, depth * orbitRadius * 0.42f);
			orbit = orbit.RotatedBy(direction * 0.38f);
			float depthFactor = MathHelper.Lerp(0.55f, 1f, (depth + 1f) * 0.5f);
			Color animatedWispColor = WithAlpha(wispColor, (byte)(245f * depthFactor)) * opacity;
			float wispScale = MathHelper.Lerp(0.13f, 0.205f, visualProgress) * depthFactor * scaleMultiplier;
			Main.EntitySpriteDraw(glowTexture, drawPosition + orbit, null, animatedWispColor, 0f, origin, wispScale, SpriteEffects.None);
		}
	}

	public override void OnKill(int timeLeft)
	{
		if (Main.dedServ)
		{
			return;
		}

		// A soft bloom makes collection and merging feel responsive.
		Color soulColor = GetSoulColor((float)System.Math.Log10(System.Math.Max(1, StoredSouls)), ContainsBossReward);
		for (int i = 0; i < 10; i++)
		{
			Vector2 velocity = Main.rand.NextVector2CircularEdge(1.8f, 1.8f);
			Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.DungeonSpirit, velocity, 100, soulColor, 0.75f);
			dust.noGravity = true;
		}
	}

	public override void SendExtraAI(BinaryWriter writer)
	{
		writer.Write(StoredSouls);
		writer.Write(age);
		writer.Write(ContainsBossReward);
	}

	public override void ReceiveExtraAI(BinaryReader reader)
	{
		StoredSouls = reader.ReadInt64();
		age = reader.ReadInt32();
		ContainsBossReward = reader.ReadBoolean();
	}

	public static void Spawn(IEntitySource source, Vector2 position, long souls, bool isBossReward = false)
	{
		if (souls <= 0 || Main.netMode == NetmodeID.MultiplayerClient)
		{
			return;
		}

		Vector2 velocity = Main.rand.NextVector2Circular(2.5f, 2.5f) - Vector2.UnitY;
		int index = Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<SoulOrbProjectile>(), 0, 0f, Main.myPlayer);
		if (index >= 0 && index < Main.maxProjectiles && Main.projectile[index].ModProjectile is SoulOrbProjectile orb)
		{
			orb.StoredSouls = souls;
			orb.ContainsBossReward = isBossReward;
			orb.Projectile.netUpdate = true;
		}
	}

	private Player FindNearestPlayer()
	{
		Player nearest = null;
		float nearestDistanceSquared = CollectionRange * CollectionRange;

		foreach (Player player in Main.ActivePlayers)
		{
			if (player.dead || player.ghost)
			{
				continue;
			}

			float distanceSquared = Vector2.DistanceSquared(Projectile.Center, player.Center);
			if (distanceSquared < nearestDistanceSquared)
			{
				nearest = player;
				nearestDistanceSquared = distanceSquared;
			}
		}

		return nearest;
	}

	private bool TryMerge()
	{
		// The lower projectile slot absorbs higher slots deterministically.
		foreach (Projectile other in Main.ActiveProjectiles)
		{
			if (other.whoAmI <= Projectile.whoAmI || other.type != Type || Vector2.DistanceSquared(Projectile.Center, other.Center) > MergeRange * MergeRange)
			{
				continue;
			}

			if (other.ModProjectile is SoulOrbProjectile otherOrb)
			{
				StoredSouls = SoulMath.SaturatingAdd(StoredSouls, otherOrb.StoredSouls);
				ContainsBossReward |= otherOrb.ContainsBossReward;
				Projectile.netUpdate = true;
				other.Kill();
				return true;
			}
		}

		return false;
	}

	private static Texture2D CreateGlowTexture()
	{
		const int size = 64;
		Texture2D texture = new(Main.instance.GraphicsDevice, size, size);
		Color[] pixels = new Color[size * size];
		Vector2 center = new((size - 1) * 0.5f);
		float radius = size * 0.5f;

		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
				float alpha = MathHelper.Clamp(1f - distance, 0f, 1f);
				alpha = alpha * alpha * (3f - 2f * alpha);
				pixels[y * size + x] = Color.FromNonPremultiplied(255, 255, 255, (int)(alpha * 255f));
			}
		}

		texture.SetData(pixels);
		return texture;
	}

	private static Texture2D CreateRingTexture()
	{
		const int size = 64;
		Texture2D texture = new(Main.instance.GraphicsDevice, size, size);
		Color[] pixels = new Color[size * size];
		Vector2 center = new((size - 1) * 0.5f);
		float radius = size * 0.5f;

		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
				float alpha = MathHelper.Clamp(1f - System.Math.Abs(distance - 0.68f) / 0.18f, 0f, 1f);
				alpha = alpha * alpha * (3f - 2f * alpha);
				pixels[y * size + x] = Color.FromNonPremultiplied(255, 255, 255, (int)(alpha * 255f));
			}
		}

		texture.SetData(pixels);
		return texture;
	}

	private static Color GetSoulColor(float valueLog, bool containsBossReward)
	{
		if (containsBossReward)
		{
			return BossSoulColor;
		}

		float gradientPosition = MathHelper.Clamp(valueLog - 1f, 0f, SoulGradient.Length - 1);
		int lowerIndex = (int)System.Math.Floor(gradientPosition);
		int upperIndex = System.Math.Min(lowerIndex + 1, SoulGradient.Length - 1);
		float blend = gradientPosition - lowerIndex;
		blend = blend * blend * (3f - 2f * blend);
		return Color.Lerp(SoulGradient[lowerIndex], SoulGradient[upperIndex], blend);
	}

	private static Color WithAlpha(Color color, byte alpha)
	{
		// SpriteBatch uses premultiplied alpha; scale RGB to preserve transparency.
		return Color.FromNonPremultiplied(color.R, color.G, color.B, alpha);
	}
}
