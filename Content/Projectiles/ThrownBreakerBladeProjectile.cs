using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Content.Items.Weapons.Melee;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class ThrownBreakerBladeProjectile : ModProjectile
{
	public const float MaximumTetherDistance = 400f;
	private const float ReturnSpeed = 20f;
	private const float ReturnDamageMultiplier = 0.7f;
	private const float ExtractionDamageMultiplier = 1.25f;
	private const int TrailLength = 6;
	private static Texture2D bandageSegmentTexture;

	private readonly HashSet<int> returnHits = new();

	private BladeState State
	{
		get => (BladeState)(int)Projectile.ai[0];
		set => Projectile.ai[0] = (float)value;
	}

	private int TargetIndex
	{
		get => (int)Projectile.ai[1];
		set => Projectile.ai[1] = value;
	}

	public override string Texture => "SoulsOfTerra/Content/Items/Weapons/Melee/EssenceboundBreakerBlade";

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
		ProjectileID.Sets.TrailingMode[Type] = 2;
		ProjectileID.Sets.DrawScreenCheckFluff[Type] = 480;
	}

	public override void SetDefaults()
	{
		Projectile.width = 42;
		Projectile.height = 42;
		Projectile.friendly = true;
		Projectile.DamageType = DamageClass.Melee;
		Projectile.penetrate = -1;
		Projectile.timeLeft = 3600;
		Projectile.tileCollide = true;
		Projectile.ignoreWater = true;
		Projectile.ownerHitCheck = false;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = -1;
		Projectile.netImportant = true;
	}

	public override bool ShouldUpdatePosition() => State is BladeState.Outbound or BladeState.Returning;

	public override void Unload()
	{
		Texture2D textureToDispose = bandageSegmentTexture;
		bandageSegmentTexture = null;
		if (textureToDispose is not null)
		{
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
		if (!player.active || player.dead)
		{
			Projectile.Kill();
			return;
		}

		if (State != BladeState.Returning
			&& player.HeldItem.type != ModContent.ItemType<EssenceboundBreakerBlade>())
		{
			BeginReturn(true);
		}

		if (State != BladeState.Returning
			&& Vector2.DistanceSquared(player.MountedCenter, Projectile.Center)
			> MaximumTetherDistance * MaximumTetherDistance)
		{
			BeginReturn(true);
		}

		switch (State)
		{
			case BladeState.Outbound:
				Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
				break;
			case BladeState.LodgedEnemy:
				UpdateLodgedEnemy();
				break;
			case BladeState.LodgedTile:
				Projectile.velocity = Vector2.Zero;
				break;
			case BladeState.Returning:
				UpdateReturn(player);
				break;
		}

		Lighting.AddLight(Projectile.Center, 0.12f, 0.17f, 0.14f);
	}

	public override bool? CanDamage()
	{
		return State is BladeState.Outbound or BladeState.Returning ? null : false;
	}

	public override bool? CanHitNPC(NPC target)
	{
		if (State == BladeState.Returning)
		{
			return target.whoAmI != TargetIndex && !returnHits.Contains(target.whoAmI) ? null : false;
		}

		return State == BladeState.Outbound ? null : false;
	}

	public override bool? Colliding(Rectangle projectileHitbox, Rectangle targetHitbox)
	{
		if (State == BladeState.Returning)
		{
			Rectangle discHitbox = new((int)Projectile.Center.X - 30, (int)Projectile.Center.Y - 30, 60, 60);
			return discHitbox.Intersects(targetHitbox);
		}

		Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
		Vector2 start = Projectile.Center - direction * 34f;
		Vector2 end = Projectile.Center + direction * 34f;
		float collisionPoint = 0f;
		return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end,
			18f, ref collisionPoint);
	}

	public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
	{
		if (State == BladeState.Returning)
		{
			modifiers.SourceDamage *= ReturnDamageMultiplier;
		}
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (State == BladeState.Outbound)
		{
			LodgeInEnemy(target);
			return;
		}

		if (State == BladeState.Returning)
		{
			returnHits.Add(target.whoAmI);
		}
	}

	public override bool OnTileCollide(Vector2 oldVelocity)
	{
		if (State != BladeState.Outbound)
		{
			return false;
		}

		State = BladeState.LodgedTile;
		Projectile.velocity = Vector2.Zero;
		Projectile.tileCollide = false;
		Projectile.netUpdate = true;
		PlayImpactEffects(Projectile.Center);
		return false;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Player player = Main.player[Projectile.owner];
		if (player.active)
		{
			float distance = Vector2.Distance(player.MountedCenter, Projectile.Center);
			float tension = MathHelper.Clamp(distance / MaximumTetherDistance, 0f, 1f);
			float sag = State == BladeState.Returning ? 0f : MathHelper.Lerp(24f, 3f, tension);
			DrawBandage(player.MountedCenter, Projectile.Center, sag, tension);
		}

		Texture2D texture = TextureAssets.Projectile[Type].Value;
		Vector2 origin = texture.Size() * 0.5f;
		if (State == BladeState.Returning)
		{
			for (int index = Projectile.oldPos.Length - 1; index >= 1; index--)
			{
				if (Projectile.oldPos[index] == Vector2.Zero)
				{
					continue;
				}

				float strength = 1f - index / (float)Projectile.oldPos.Length;
				Vector2 position = Projectile.oldPos[index] + Projectile.Size * 0.5f - Main.screenPosition;
				Main.EntitySpriteDraw(texture, position, null, new Color(170, 222, 207) * (strength * 0.22f),
					Projectile.oldRot[index], origin, 0.86f, SpriteEffects.None);
			}
		}

		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor,
			Projectile.rotation, origin, 0.86f, SpriteEffects.None);
		return false;
	}

	public static int CountDeployedBlades(int owner)
	{
		int count = 0;
		int bladeType = ModContent.ProjectileType<ThrownBreakerBladeProjectile>();
		foreach (Projectile projectile in Main.ActiveProjectiles)
		{
			if (projectile.owner == owner && projectile.type == bladeType)
			{
				count++;
			}
		}

		return count;
	}

	public static bool HasDeployedBlades(int owner)
	{
		int bladeType = ModContent.ProjectileType<ThrownBreakerBladeProjectile>();
		foreach (Projectile projectile in Main.ActiveProjectiles)
		{
			if (projectile.owner == owner && projectile.type == bladeType
				&& projectile.ModProjectile is ThrownBreakerBladeProjectile blade
				&& blade.State != BladeState.Returning)
			{
				return true;
			}
		}

		return false;
	}

	public static bool AreAllBladesLodged(int owner, int requiredCount)
	{
		int lodgedCount = 0;
		int bladeType = ModContent.ProjectileType<ThrownBreakerBladeProjectile>();
		foreach (Projectile projectile in Main.ActiveProjectiles)
		{
			if (projectile.owner != owner || projectile.type != bladeType
				|| projectile.ModProjectile is not ThrownBreakerBladeProjectile blade)
			{
				continue;
			}

			if (blade.State is not (BladeState.LodgedEnemy or BladeState.LodgedTile))
			{
				return false;
			}

			lodgedCount++;
		}

		return lodgedCount == requiredCount;
	}

	public static void RecallAll(int owner)
	{
		int bladeType = ModContent.ProjectileType<ThrownBreakerBladeProjectile>();
		foreach (Projectile projectile in Main.ActiveProjectiles)
		{
			if (projectile.owner == owner && projectile.type == bladeType
				&& projectile.ModProjectile is ThrownBreakerBladeProjectile blade)
			{
				blade.BeginReturn(true);
			}
		}
	}

	private void LodgeInEnemy(NPC target)
	{
		State = BladeState.LodgedEnemy;
		TargetIndex = target.whoAmI;
		Vector2 impactDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX);
		float spacing = (Projectile.identity % 5 - 2) * 2.5f;
		Projectile.velocity = Projectile.Center - target.Center
			+ impactDirection.RotatedBy(MathHelper.PiOver2) * spacing;
		Projectile.tileCollide = false;
		Projectile.netUpdate = true;
		PlayImpactEffects(Projectile.Center);
	}

	private void UpdateLodgedEnemy()
	{
		if (TargetIndex < 0 || TargetIndex >= Main.maxNPCs || !Main.npc[TargetIndex].active)
		{
			BeginReturn(false);
			return;
		}

		Projectile.Center = Main.npc[TargetIndex].Center + Projectile.velocity;
	}

	private void UpdateReturn(Player player)
	{
		Vector2 toPlayer = player.MountedCenter - Projectile.Center;
		if (toPlayer.LengthSquared() <= 28f * 28f)
		{
			Projectile.Kill();
			return;
		}

		Vector2 desiredVelocity = toPlayer.SafeNormalize(Vector2.UnitX) * ReturnSpeed;
		Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.32f);
		float spinDirection = Projectile.velocity.X >= 0f ? 1f : -1f;
		Projectile.rotation += spinDirection * 0.48f;
	}

	private void BeginReturn(bool extractHost)
	{
		if (State == BladeState.Returning)
		{
			return;
		}

		int formerTarget = State == BladeState.LodgedEnemy ? TargetIndex : -1;
		if (extractHost && formerTarget >= 0 && formerTarget < Main.maxNPCs && Main.npc[formerTarget].active
			&& Projectile.owner == Main.myPlayer)
		{
			int extractionDamage = Math.Max(1, (int)MathF.Round(Projectile.damage * ExtractionDamageMultiplier));
			Projectile.NewProjectile(Projectile.GetSource_FromThis(), Main.npc[formerTarget].Center, Vector2.Zero,
				ModContent.ProjectileType<BreakerBladeExtractionProjectile>(), extractionDamage,
				Projectile.knockBack, Projectile.owner, formerTarget);
		}

		State = BladeState.Returning;
		TargetIndex = formerTarget;
		Projectile.tileCollide = false;
		Projectile.timeLeft = Math.Max(Projectile.timeLeft, 180);
		Projectile.velocity = (Main.player[Projectile.owner].MountedCenter - Projectile.Center)
			.SafeNormalize(Vector2.UnitX) * ReturnSpeed;
		Projectile.netUpdate = true;
		if (Main.netMode != NetmodeID.Server)
		{
			SoundEngine.PlaySound(SoundID.Item16 with { Volume = 0.38f, Pitch = 0.25f }, Projectile.Center);
		}
	}

	private static void PlayImpactEffects(Vector2 position)
	{
		if (Main.netMode == NetmodeID.Server)
		{
			return;
		}

		SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.5f, Pitch = -0.25f }, position);
		for (int index = 0; index < 6; index++)
		{
			Dust dust = Dust.NewDustPerfect(position, DustID.Web, Main.rand.NextVector2Circular(2f, 2f),
				100, new Color(205, 192, 168), 0.68f);
			dust.noGravity = true;
		}
	}

	private void DrawBandage(Vector2 start, Vector2 end, float sag, float tension)
	{
		bandageSegmentTexture ??= CreateBandageSegmentTexture();
		Vector2 straight = end - start;
		float pathLength = straight.Length() + sag * 0.35f;
		int segmentCount = Math.Max(2, (int)MathF.Ceiling(pathLength / 6f));
		Vector2 origin = bandageSegmentTexture.Size() * 0.5f;
		for (int index = 0; index < segmentCount; index++)
		{
			float progress = (index + 0.5f) / segmentCount;
			float nextProgress = Math.Min(1f, progress + 1f / segmentCount);
			Vector2 point = GetBandagePoint(start, end, sag, tension, progress);
			Vector2 nextPoint = GetBandagePoint(start, end, sag, tension, nextProgress);
			Color cloth = Color.Lerp(new Color(170, 157, 138), new Color(221, 230, 210), tension * 0.5f);
			Main.spriteBatch.Draw(bandageSegmentTexture, point - Main.screenPosition, null, cloth,
				(nextPoint - point).ToRotation(), origin, 1f, SpriteEffects.None, 0f);
		}
	}

	private Vector2 GetBandagePoint(Vector2 start, Vector2 end, float sag, float tension, float progress)
	{
		Vector2 point = Vector2.Lerp(start, end, progress);
		point.Y += MathF.Sin(progress * MathHelper.Pi) * sag;
		Vector2 perpendicular = (end - start).SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
		point += perpendicular * MathF.Sin(progress * MathHelper.TwoPi * 2f
			+ Main.GlobalTimeWrappedHourly * 2f + Projectile.identity) * (1.4f - tension);
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

	private enum BladeState
	{
		Outbound,
		LodgedEnemy,
		LodgedTile,
		Returning
	}
}
