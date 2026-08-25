using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Content.Items.Weapons.Melee;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class SoulBandageTetherProjectile : ModProjectile
{
	public const float FullTensionDistance = 600f;
	public const float BreakingDistance = 720f;
	private const int MaximumConnections = 8;
	private const int AttachmentDuration = 12;
	private const int PullDuration = 8;
	private static Texture2D bandageSegmentTexture;

	private NPC Target => Projectile.ai[0] >= 0 && Projectile.ai[0] < Main.maxNPCs
		? Main.npc[(int)Projectile.ai[0]]
		: null;
	private bool Pulling => Projectile.ai[1] < 0f;

	public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.PurificationPowder}";

	public override void SetDefaults()
	{
		Projectile.width = 2;
		Projectile.height = 2;
		Projectile.friendly = false;
		Projectile.hostile = false;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.timeLeft = 2;
		Projectile.netImportant = true;
	}

	public override bool ShouldUpdatePosition() => false;
	public override bool? CanDamage() => false;

	public override void Unload()
	{
		Texture2D textureToDispose = bandageSegmentTexture;
		bandageSegmentTexture = null;
		if (textureToDispose is not null)
		{
			// FNA texture disposal must return to Terraria's graphics thread during reloads.
			Main.QueueMainThreadAction(() =>
			{
				if (!textureToDispose.IsDisposed)
				{
					textureToDispose.Dispose();
				}
			});
		}
	}

	public override void AI()
	{
		Player player = Main.player[Projectile.owner];
		NPC target = Target;
		if (!player.active || player.dead || target is null || !target.active || target.friendly || target.dontTakeDamage
			|| player.HeldItem.type != ModContent.ItemType<EssenceboundBreakerBlade>())
		{
			Projectile.Kill();
			return;
		}

		Projectile.Center = target.Center;
		Projectile.ai[2]++;
		if (!Pulling && Vector2.Distance(player.MountedCenter, target.Center) > BreakingDistance)
		{
			Projectile.Kill();
			return;
		}

		if (Pulling)
		{
			ApplyPull(player, target);
			if (++Projectile.ai[1] >= 0f)
			{
				Projectile.Kill();
				return;
			}
		}
		else if (Projectile.ai[1] > 0f)
		{
			Projectile.ai[1]--;
		}

		Projectile.timeLeft = 2;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Player player = Main.player[Projectile.owner];
		NPC target = Target;
		if (!player.active || target is null || !target.active)
		{
			return false;
		}

		Vector2 start = player.MountedCenter;
		Vector2 end = target.Center;
		float attachProgress = MathHelper.Clamp(Projectile.ai[2] / AttachmentDuration, 0f, 1f);
		if (attachProgress < 1f)
		{
			Vector2 perpendicular = (end - start).SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
			Vector2 control = Vector2.Lerp(start, end, 0.5f) + perpendicular * 42f;
			end = QuadraticBezier(start, control, end, EaseOutCubic(attachProgress));
		}

		float distance = Vector2.Distance(start, end);
		float tension = MathHelper.Clamp(distance / FullTensionDistance, 0f, 1f);
		float flash = MathHelper.Clamp(Projectile.ai[1] / 14f, 0f, 1f);
		float sag = Pulling ? 0f : MathHelper.Lerp(28f, 3f, tension);
		DrawBandage(Main.spriteBatch, start, end, sag, tension, flash, Projectile.identity);
		return false;
	}

	public override void OnKill(int timeLeft)
	{
		if (Main.netMode == NetmodeID.Server || Target is not NPC target)
		{
			return;
		}

		SoundEngine.PlaySound(SoundID.Item16 with { Volume = 0.3f, Pitch = 0.4f }, target.Center);
		for (int index = 0; index < 7; index++)
		{
			Dust dust = Dust.NewDustPerfect(target.Center, DustID.Web, Main.rand.NextVector2Circular(2.2f, 2.2f),
				100, new Color(205, 195, 173), Main.rand.NextFloat(0.65f, 0.95f));
			dust.noGravity = true;
		}
	}

	public static bool HasConnections(int owner)
	{
		int tetherType = ModContent.ProjectileType<SoulBandageTetherProjectile>();
		foreach (Projectile projectile in Main.ActiveProjectiles)
		{
			if (projectile.owner == owner && projectile.type == tetherType)
			{
				return true;
			}
		}

		return false;
	}

	public static void Attach(Player player, NPC target)
	{
		int tetherType = ModContent.ProjectileType<SoulBandageTetherProjectile>();
		Projectile oldest = null;
		int connectionCount = 0;
		foreach (Projectile projectile in Main.ActiveProjectiles)
		{
			if (projectile.owner != player.whoAmI || projectile.type != tetherType)
			{
				continue;
			}

			connectionCount++;
			if ((int)projectile.ai[0] == target.whoAmI)
			{
				// Repeated hits tighten the existing ribbon without adding hidden stacks.
				projectile.ai[1] = 14f;
				projectile.netUpdate = true;
				return;
			}

			if (oldest is null || projectile.ai[2] > oldest.ai[2])
			{
				oldest = projectile;
			}
		}

		if (connectionCount >= MaximumConnections)
		{
			oldest?.Kill();
		}

		Projectile.NewProjectile(player.GetSource_OnHit(target), player.MountedCenter, Vector2.Zero, tetherType,
			0, 0f, player.whoAmI, target.whoAmI);
	}

	public static void ActivateNetwork(Projectile executionProjectile)
	{
		Player player = Main.player[executionProjectile.owner];
		int tetherType = ModContent.ProjectileType<SoulBandageTetherProjectile>();
		foreach (Projectile projectile in Main.ActiveProjectiles)
		{
			if (projectile.owner != player.whoAmI || projectile.type != tetherType
				|| projectile.ModProjectile is not SoulBandageTetherProjectile tether || tether.Target is not NPC target)
			{
				continue;
			}

			float distance = Vector2.Distance(player.MountedCenter, target.Center);
			bool blocked = !Collision.CanHitLine(player.MountedCenter, 1, 1, target.position, target.width, target.height);
			if (distance > BreakingDistance || blocked)
			{
				projectile.Kill();
				continue;
			}

			float distanceProgress = MathHelper.Clamp(distance / FullTensionDistance, 0f, 1f);
			int damage = System.Math.Max(1, (int)(player.GetWeaponDamage(player.HeldItem)
				* MathHelper.Lerp(0.4f, 1.2f, distanceProgress)));
			Projectile.NewProjectile(executionProjectile.GetSource_FromThis(), target.Center, Vector2.Zero,
				ModContent.ProjectileType<SoulBandageImpactProjectile>(), damage, 0f, player.whoAmI, target.whoAmI);
			projectile.ai[1] = -PullDuration;
			projectile.netUpdate = true;
		}
	}

	private static void ApplyPull(Player player, NPC target)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient || target.boss || target.knockBackResist <= 0f)
		{
			return;
		}

		float distance = Vector2.Distance(player.MountedCenter, target.Center);
		float progress = MathHelper.Clamp(distance / FullTensionDistance, 0f, 1f);
		float resistance = MathHelper.Clamp(target.knockBackResist, 0.18f, 1f);
		Vector2 pullVelocity = (player.MountedCenter - target.Center).SafeNormalize(Vector2.Zero)
			* MathHelper.Lerp(7f, 22f, progress) * resistance;
		target.velocity = Vector2.Lerp(target.velocity, pullVelocity, 0.62f);
		if (Main.netMode == NetmodeID.Server)
		{
			NetMessage.SendData(MessageID.SyncNPC, number: target.whoAmI);
		}
	}

	private static void DrawBandage(SpriteBatch spriteBatch, Vector2 start, Vector2 end, float sag, float tension, float flash, int seed)
	{
		bandageSegmentTexture ??= CreateBandageSegmentTexture();
		Vector2 straight = end - start;
		float pathLength = straight.Length() + sag * 0.35f;
		int segmentCount = System.Math.Max(2, (int)System.MathF.Ceiling(pathLength / 5f));
		Vector2 perpendicular = straight.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
		Vector2 origin = bandageSegmentTexture.Size() * 0.5f;
		for (int index = 0; index < segmentCount; index++)
		{
			float progress = (index + 0.5f) / segmentCount;
			float nextProgress = System.Math.Min(1f, progress + 1f / segmentCount);
			Vector2 point = GetBandagePoint(start, end, perpendicular, sag, tension, seed, progress);
			Vector2 nextPoint = GetBandagePoint(start, end, perpendicular, sag, tension, seed, nextProgress);
			float alpha = tension > 0.86f && (index + (int)(Main.GlobalTimeWrappedHourly * 12f)) % 17 == 0 ? 0.18f : 1f;
			Color cloth = Color.Lerp(new Color(195, 181, 157), new Color(224, 239, 219),
				tension * 0.4f + flash * 0.42f) * alpha;
			float flutterRotation = MathF.Sin(index * 0.72f + Main.GlobalTimeWrappedHourly * 2.3f + seed) * (1f - tension) * 0.08f;
			// Overlapping unscaled cloth sprites create a continuous ribbon without unsafe pixel stretching.
			spriteBatch.Draw(bandageSegmentTexture, point - Main.screenPosition, null, cloth,
				(nextPoint - point).ToRotation() + flutterRotation, origin, 1f, SpriteEffects.None, 0f);
		}
	}

	private static Vector2 GetBandagePoint(Vector2 start, Vector2 end, Vector2 perpendicular, float sag,
		float tension, int seed, float progress)
	{
		Vector2 point = Vector2.Lerp(start, end, progress);
		point.Y += MathF.Sin(progress * MathHelper.Pi) * sag;
		point += perpendicular * MathF.Sin(progress * MathHelper.TwoPi * 3f
			+ Main.GlobalTimeWrappedHourly * 2f + seed) * (1.5f - tension);
		return point;
	}

	private static Texture2D CreateBandageSegmentTexture()
	{
		const int width = 12;
		const int height = 7;
		Texture2D texture = new(Main.instance.GraphicsDevice, width, height);
		Color[] pixels = new Color[width * height];
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				bool outside = y == 0 || y == height - 1;
				bool weave = (x + y * 3) % 9 == 0;
				byte value = outside ? (byte)70 : weave ? (byte)165 : y is 1 or 5 ? (byte)205 : (byte)245;
				pixels[y * width + x] = new Color(value, value, value, 255);
			}
		}

		texture.SetData(pixels);
		return texture;
	}

	private static Vector2 QuadraticBezier(Vector2 start, Vector2 control, Vector2 end, float progress)
	{
		float inverse = 1f - progress;
		return inverse * inverse * start + 2f * inverse * progress * control + progress * progress * end;
	}

	private static float EaseOutCubic(float progress) => 1f - MathF.Pow(1f - progress, 3f);
}
