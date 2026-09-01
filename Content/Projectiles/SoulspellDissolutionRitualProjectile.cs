using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Common;
using SoulsOfTerra.Common.Rendering;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public sealed class SoulspellDissolutionRitualProjectile : ModProjectile
{
	private const int Duration = 78;
	private const int RevealTime = 54;

	public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.PurificationPowder}";

	public override void SetDefaults()
	{
		Projectile.width = 24;
		Projectile.height = 24;
		Projectile.timeLeft = Duration;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.penetrate = -1;
		Projectile.netImportant = true;
	}

	public override bool ShouldUpdatePosition() => false;

	public override void AI()
	{
		Projectile.localAI[0]++;
		float progress = Projectile.localAI[0] / Duration;
		Lighting.AddLight(Projectile.Center, 0.08f + progress * 0.18f, 0.32f + progress * 0.42f, 0.34f + progress * 0.42f);
		if (Main.netMode == NetmodeID.Server)
		{
			return;
		}

		if (Projectile.localAI[0] == 1f)
		{
			SoundEngine.PlaySound(SoundID.Item3 with { Volume = 0.65f, Pitch = -0.25f }, Projectile.Center);
		}
		else if (Projectile.localAI[0] == RevealTime)
		{
			SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.85f, Pitch = 0.35f }, Projectile.Center);
			for (int index = 0; index < 28; index++)
			{
				Dust dust = Dust.NewDustPerfect(Projectile.Center, index % 3 == 0 ? DustID.MagicMirror : DustID.BlueTorch,
					Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 5f), 80, new Color(80, 245, 225), 0.9f);
				dust.noGravity = true;
			}
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		// Both ingredients collapse before the learned vanilla buff icon manifests.
		int recipeIndex = (int)Projectile.ai[0];
		if (recipeIndex < 0 || recipeIndex >= SoulSpellRegistry.PotionSpells.Length)
		{
			return false;
		}

		SoulSpellDefinition spell = SoulSpellRegistry.PotionSpells[recipeIndex];
		float age = Projectile.localAI[0];
		Vector2 center = Projectile.Center - Main.screenPosition + new Vector2(0f, -22f + System.MathF.Sin(age * 0.12f) * 3f);
		if (age < RevealTime)
		{
			float collapse = Utils.GetLerpValue(RevealTime - 15f, RevealTime, age, true);
			float angle = age * 0.12f;
			Vector2 potionOffset = angle.ToRotationVector2() * 30f * (1f - collapse);
			Vector2 essenceOffset = (angle + MathHelper.Pi).ToRotationVector2() * 30f * (1f - collapse);
			DrawItem(spell.PotionItemType, center + potionOffset, 0.68f * (1f - collapse), Color.White);
			DrawItem(spell.EssenceItemType, center + essenceOffset, 0.58f * (1f - collapse), Color.White);
		}
		else
		{
			float reveal = MathHelper.SmoothStep(0.2f, 1f, Utils.GetLerpValue(RevealTime, RevealTime + 10f, age, true));
			Texture2D buffTexture = TextureAssets.Buff[spell.BuffType].Value;
			Main.EntitySpriteDraw(buffTexture, center, null, Color.White, 0f, buffTexture.Size() * 0.5f, reveal, SpriteEffects.None);
		}

		return false;
	}

	private static void DrawItem(int itemType, Vector2 center, float scaleMultiplier, Color color)
	{
		if (EssenceEchoRenderer.TryDraw(Main.spriteBatch, itemType, center, 44f * scaleMultiplier, color))
		{
			return;
		}

		Main.instance.LoadItem(itemType);
		Texture2D texture = TextureAssets.Item[itemType].Value;
		Rectangle frame = ItemAnimationDrawing.GetFrame(itemType, texture);
		float fitScale = System.MathF.Min(44f / frame.Width, 44f / frame.Height);
		Main.EntitySpriteDraw(texture, center, frame, color, 0f, frame.Size() * 0.5f,
			fitScale * scaleMultiplier, SpriteEffects.None);
	}
}
