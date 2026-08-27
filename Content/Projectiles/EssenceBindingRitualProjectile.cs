using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Common;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class EssenceBindingRitualProjectile : ModProjectile
{
	private const int RitualDuration = 75;
	private const int RevealTime = 57;

	public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.PurificationPowder}";

	public override void SetDefaults()
	{
		Projectile.width = 32;
		Projectile.height = 32;
		Projectile.timeLeft = RitualDuration;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.penetrate = -1;
		Projectile.netImportant = true;
	}

	public override bool ShouldUpdatePosition() => false;

	public override void AI()
	{
		Projectile.localAI[0]++;
		float progress = Projectile.localAI[0] / RitualDuration;
		Lighting.AddLight(Projectile.Center, 0.12f + progress * 0.25f, 0.35f + progress * 0.45f,
			0.3f + progress * 0.35f);

		if (Projectile.localAI[0] == 1f && Main.netMode != NetmodeID.Server)
		{
			SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.7f, Pitch = -0.35f }, Projectile.Center);
		}
		else if (Projectile.localAI[0] == RevealTime && Main.netMode != NetmodeID.Server)
		{
			CreateRevealBurst();
			SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.85f, Pitch = 0.2f }, Projectile.Center);
		}

		if (Projectile.timeLeft == 1 && Main.netMode != NetmodeID.MultiplayerClient)
		{
			GrantOutput();
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		if (!EssenceImbuementRegistry.TryGet((int)Projectile.ai[0], out EssenceImbuementDefinition imbuement))
		{
			return false;
		}

		float age = Projectile.localAI[0];
		float rise = MathHelper.SmoothStep(10f, 0f, Utils.GetLerpValue(0f, 24f, age, true));
		float bob = System.MathF.Sin(age * 0.12f) * 3f;
		Vector2 center = Projectile.Center - Main.screenPosition + new Vector2(0f, rise + bob);
		int consumedInputType = (int)Projectile.ai[2];
		if (!imbuement.AcceptsInput(consumedInputType))
		{
			consumedInputType = imbuement.PreviewInputItemType;
		}
		int displayedItem = age < RevealTime ? consumedInputType : imbuement.OutputItemType;
		float revealScale = age < RevealTime ? 1f : MathHelper.SmoothStep(0.2f, 1f,
			Utils.GetLerpValue(RevealTime, RevealTime + 10f, age, true));
		DrawItem(displayedItem, center, revealScale, lightColor);

		if (age < RevealTime)
		{
			float orbitAngle = age * 0.11f;
			Vector2 essencePosition = center + orbitAngle.ToRotationVector2() * 28f;
			float implosion = Utils.GetLerpValue(RevealTime - 12f, RevealTime, age, true);
			DrawItem(imbuement.EssenceItemType, Vector2.Lerp(essencePosition, center, implosion),
				0.55f * (1f - implosion), Color.White);
		}

		return false;
	}

	private void GrantOutput()
	{
		if (!EssenceImbuementRegistry.TryGet((int)Projectile.ai[0], out EssenceImbuementDefinition imbuement))
		{
			return;
		}

		Item output = new(imbuement.OutputItemType);
		int prefix = (int)Projectile.ai[1];
		if (prefix > 0)
		{
			output.Prefix(prefix);
		}

		IEntitySource source = new EntitySource_Misc("SoulsOfTerra:EssenceBindingComplete");
		Player player = Main.player[Projectile.owner];
		if (player.active)
		{
			player.QuickSpawnItem(source, output, 1);
		}
		else
		{
			Item.NewItem(source, Projectile.Hitbox, imbuement.OutputItemType, prefixGiven: prefix);
		}
	}

	private void CreateRevealBurst()
	{
		for (int index = 0; index < 32; index++)
		{
			Vector2 direction = Main.rand.NextVector2Unit();
			Dust dust = Dust.NewDustPerfect(Projectile.Center, index % 3 == 0 ? DustID.SilverFlame : DustID.GreenTorch,
				direction * Main.rand.NextFloat(2f, 6f), 60, new Color(120, 215, 185), Main.rand.NextFloat(0.8f, 1.25f));
			dust.noGravity = true;
		}
	}

	private static void DrawItem(int itemType, Vector2 center, float scaleMultiplier, Color color)
	{
		Texture2D texture = TextureAssets.Item[itemType].Value;
		Rectangle frame = texture.Frame();
		float fitScale = System.MathF.Min(52f / frame.Width, 52f / frame.Height);
		Main.EntitySpriteDraw(texture, center, frame, color, 0f, frame.Size() * 0.5f,
			fitScale * scaleMultiplier, SpriteEffects.None);
	}
}
