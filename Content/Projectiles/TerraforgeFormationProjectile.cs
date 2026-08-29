using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Content.Items.Access;
using SoulsOfTerra.Content.Tiles;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class TerraforgeFormationProjectile : ModProjectile
{
	public const int Duration = 90;
	private const int ImpactTime = 48;
	private Vector2 startPosition;
	private int age;
	private bool initialized;

	public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.PurificationPowder}";

	public Point16 ForgeTopLeft => new((short)Projectile.ai[0], (short)Projectile.ai[1]);
	public float Progress => age / (float)Duration;

	public override void SetDefaults()
	{
		Projectile.width = 16;
		Projectile.height = 16;
		Projectile.timeLeft = Duration;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.penetrate = -1;
		Projectile.netImportant = true;
	}

	public override bool ShouldUpdatePosition() => false;

	public override void AI()
	{
		if (!initialized)
		{
			initialized = true;
			startPosition = Projectile.Center;
		}

		age++;
		Vector2 target = GetImpactPosition(ForgeTopLeft);
		float charge = Utils.GetLerpValue(0f, ImpactTime, age, true);
		Lighting.AddLight(Vector2.Lerp(startPosition, target, charge), 0.18f, 0.42f, 0.2f);

		if (Main.netMode != NetmodeID.Server && age < ImpactTime && Main.rand.NextBool(2))
		{
			SpawnChargeDust(Vector2.Lerp(startPosition, target, charge));
		}

		if (age == ImpactTime && Main.netMode != NetmodeID.Server)
		{
			CreateImpact(target);
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		if (age >= ImpactTime)
		{
			DrawImpactWave();
			return false;
		}

		float progress = Utils.GetLerpValue(0f, ImpactTime, age, true);
		float eased = progress * progress * (3f - 2f * progress);
		Vector2 target = GetImpactPosition(ForgeTopLeft);
		Vector2 arc = new(0f, -36f * System.MathF.Sin(progress * MathHelper.Pi));
		Vector2 position = Vector2.Lerp(startPosition, target, eased) + arc - Main.screenPosition;
		Texture2D texture = TextureAssets.Item[ModContent.ItemType<TerraBladeFragment>()].Value;
		float scale = System.MathF.Min(34f / texture.Width, 42f / texture.Height);
		float rotation = MathHelper.Lerp(-0.35f, 0f, eased);
		Main.EntitySpriteDraw(texture, position, null, Color.White, rotation,
			texture.Size() * 0.5f, scale, SpriteEffects.None);
		return false;
	}

	private void DrawImpactWave()
	{
		float progress = Utils.GetLerpValue(ImpactTime, ImpactTime + 24f, age, true);
		float opacity = 1f - progress;
		Vector2 center = GetImpactPosition(ForgeTopLeft) - Main.screenPosition;
		Texture2D ring = SoulOrbProjectile.GetRingTexture();
		Texture2D glow = SoulOrbProjectile.GetGlowTexture();
		float scale = MathHelper.Lerp(0.18f, 1.15f, progress);
		Main.EntitySpriteDraw(glow, center, null, new Color(80, 245, 180, 0) * opacity,
			0f, glow.Size() * 0.5f, scale * 0.7f, SpriteEffects.None);
		Main.EntitySpriteDraw(ring, center, null, new Color(245, 220, 110, 0) * opacity,
			0f, ring.Size() * 0.5f, scale, SpriteEffects.None);
	}

	public static void Spawn(Player player, Point16 forgeTopLeft)
	{
		IEntitySource source = new EntitySource_Misc("SoulsOfTerra:TerraforgeFormation");
		Projectile.NewProjectile(source, player.Center, Vector2.Zero,
			ModContent.ProjectileType<TerraforgeFormationProjectile>(), 0, 0f, player.whoAmI,
			forgeTopLeft.X, forgeTopLeft.Y);
	}

	public static bool IsFormingAt(Point16 topLeft) => TryGetProgress(topLeft, out _);

	public static bool TryGetProgress(Point16 topLeft, out float progress)
	{
		foreach (Projectile projectile in Main.ActiveProjectiles)
		{
			if (projectile.ModProjectile is TerraforgeFormationProjectile formation
				&& formation.ForgeTopLeft == topLeft)
			{
				progress = formation.Progress;
				return true;
			}
		}

		progress = 1f;
		return false;
	}

	private static Vector2 GetImpactPosition(Point16 topLeft)
	{
		return topLeft.ToWorldCoordinates(TerraforgeTile.Width * 8f, 22f);
	}

	private static void SpawnChargeDust(Vector2 position)
	{
		Dust dust = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(8f, 8f), DustID.GreenTorch,
			Main.rand.NextVector2Circular(0.7f, 0.7f), 110, new Color(90, 245, 175), 0.75f);
		dust.noGravity = true;
	}

	private static void CreateImpact(Vector2 target)
	{
		SoundEngine.PlaySound(SoundID.Item37 with { Volume = 0.9f, Pitch = -0.45f }, target);
		SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.75f, Pitch = 0.1f }, target);
		Main.instance.CameraModifiers.Add(new PunchCameraModifier(target, Vector2.UnitY, 5f, 7f, 16, 800f,
			"SoulsOfTerra:TerraforgeFormation"));

		for (int index = 0; index < 42; index++)
		{
			Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(1.5f, 6f);
			int dustType = index % 3 == 0 ? DustID.GoldFlame : DustID.GreenTorch;
			Dust dust = Dust.NewDustPerfect(target, dustType, velocity, 70,
				index % 3 == 0 ? new Color(245, 205, 85) : new Color(75, 235, 175), Main.rand.NextFloat(0.8f, 1.3f));
			dust.noGravity = true;
		}
	}
}
