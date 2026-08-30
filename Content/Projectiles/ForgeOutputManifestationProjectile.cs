using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Common.Rendering;
using SoulsOfTerra.Content.Tiles;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class ForgeOutputManifestationProjectile : ModProjectile
{
	private const int ManifestDuration = 54;
	private int age;

	public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.PurificationPowder}";

	public override void SetDefaults()
	{
		Projectile.width = 20;
		Projectile.height = 20;
		Projectile.timeLeft = 120;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.penetrate = -1;
		Projectile.netImportant = true;
	}

	public override bool ShouldUpdatePosition() => false;

	public override void AI()
	{
		if (age++ < (int)Projectile.ai[1])
		{
			Projectile.Opacity = 0f;
			return;
		}

		int visibleAge = age - (int)Projectile.ai[1];
		Projectile.Opacity = Utils.GetLerpValue(0f, 8f, visibleAge, true);
		Lighting.AddLight(Projectile.Center, 0.12f, 0.34f, 0.2f);
		if (visibleAge == 1 && Main.netMode != NetmodeID.Server)
		{
			SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.65f, Pitch = 0.2f }, Projectile.Center);
		}

		if (visibleAge >= ManifestDuration)
		{
			Projectile.Kill();
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		int visibleAge = age - (int)Projectile.ai[1];
		if (visibleAge < 0)
		{
			return false;
		}

		Player player = Main.player[Projectile.owner];
		float returnProgress = Utils.GetLerpValue(30f, ManifestDuration, visibleAge, true);
		float eased = returnProgress * returnProgress;
		Vector2 bobbedCenter = Projectile.Center + new Vector2(0f, System.MathF.Sin(visibleAge * 0.16f) * 3f);
		Vector2 center = player.active ? Vector2.Lerp(bobbedCenter, player.Center, eased) : bobbedCenter;
		int itemType = (int)Projectile.ai[0];
		float revealScale = MathHelper.SmoothStep(0.25f, 1f, Utils.GetLerpValue(0f, 10f, visibleAge, true));
		Color color = Color.White * (1f - Utils.GetLerpValue(45f, ManifestDuration, visibleAge, true));
		if (EssenceEchoRenderer.TryDraw(Main.spriteBatch, itemType, center - Main.screenPosition,
			44f * revealScale, color))
		{
			return false;
		}

		Texture2D texture = TextureAssets.Item[itemType].Value;
		Rectangle frame = ItemAnimationDrawing.GetFrame(itemType, texture);
		float fitScale = System.MathF.Min(44f / frame.Width, 44f / frame.Height);
		float scale = fitScale * revealScale;
		Main.EntitySpriteDraw(texture, center - Main.screenPosition, frame, color, 0f,
			frame.Size() * 0.5f, scale, SpriteEffects.None);
		return false;
	}

	public static void Spawn(Player player, Point16 forgeTopLeft, int itemType, int delay)
	{
		Vector2 center = forgeTopLeft.ToWorldCoordinates(TerraforgeTile.Width * 8f, -10f);
		IEntitySource source = new EntitySource_Misc("SoulsOfTerra:ForgeOutputManifestation");
		Projectile.NewProjectile(source, center, Vector2.Zero,
			ModContent.ProjectileType<ForgeOutputManifestationProjectile>(), 0, 0f, player.whoAmI, itemType, delay);
	}
}
