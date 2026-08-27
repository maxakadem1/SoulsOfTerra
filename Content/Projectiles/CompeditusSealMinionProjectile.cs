using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Content.Buffs;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class CompeditusSealMinionProjectile : ModProjectile
{
	// The compact summon seal is authored separately from the much larger boss seal.
	public override string Texture => "SoulsOfTerra/Content/Items/Weapons/Summon/Compeditus_summon";

	public override void SetStaticDefaults()
	{
		Main.projFrames[Type] = 1;
		ProjectileID.Sets.MinionTargettingFeature[Type] = true;
		ProjectileID.Sets.MinionSacrificable[Type] = true;
		ProjectileID.Sets.CultistIsResistantTo[Type] = true;
	}

	public override void SetDefaults()
	{
		Projectile.width = 23;
		Projectile.height = 35;
		Projectile.friendly = true;
		Projectile.DamageType = DamageClass.Summon;
		Projectile.minion = true;
		Projectile.minionSlots = 1f;
		Projectile.penetrate = -1;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.netImportant = true;
		Projectile.timeLeft = 2;
	}

	public override bool MinionContactDamage() => false;
	public override bool? CanDamage() => false;

	public override void AI()
	{
		Player owner = Main.player[Projectile.owner];
		if (!owner.active || owner.dead || !owner.HasBuff(ModContent.BuffType<CompeditusBuff>()))
		{
			Projectile.Kill();
			return;
		}

		int coreIndex = CompeditusCoreProjectile.FindOwnedCore(Projectile.owner);
		if (coreIndex < 0)
		{
			Projectile.Kill();
			return;
		}

		Projectile.timeLeft = 2;
		Projectile coreProjectile = Main.projectile[coreIndex];
		CompeditusCoreProjectile core = coreProjectile.ModProjectile as CompeditusCoreProjectile;
		if (core is null)
		{
			Projectile.Kill();
			return;
		}

		List<Projectile> seals = CompeditusCoreProjectile.GetOwnedSeals(Projectile.owner);
		int formationIndex = seals.FindIndex(seal => seal.whoAmI == Projectile.whoAmI);
		if (formationIndex < 0)
		{
			return;
		}

		Vector2 destination = core.GetSealDestination(formationIndex, seals.Count);
		Vector2 offset = destination - Projectile.Center;
		Projectile.velocity = Vector2.Lerp(Projectile.velocity, offset * 0.22f, core.IsJudging ? 0.32f : 0.2f);
		Projectile.rotation = (Projectile.Center - coreProjectile.Center).ToRotation() + MathHelper.PiOver2;
		Lighting.AddLight(Projectile.Center, new Vector3(0.03f, 0.2f, 0.18f));

		if (!Main.dedServ && core.IsJudging && Main.rand.NextBool(9))
		{
			Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.DungeonSpirit,
				(Projectile.Center - coreProjectile.Center).SafeNormalize(Vector2.UnitY) * 0.45f,
				130, new Color(73, 230, 211), 0.55f);
			dust.noGravity = true;
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		int coreIndex = CompeditusCoreProjectile.FindOwnedCore(Projectile.owner);
		CompeditusCoreProjectile core = coreIndex >= 0
			? Main.projectile[coreIndex].ModProjectile as CompeditusCoreProjectile
			: null;
		if (core is not null && core.IsJudging)
		{
			NPC target = core.TargetIndex >= 0 && core.TargetIndex < Main.maxNPCs ? Main.npc[core.TargetIndex] : null;
			if (target is not null && target.active)
			{
				float progress = MathHelper.Clamp((core.CycleTimer - CompeditusCoreProjectile.VerseDuration) / 36f, 0f, 1f);
				float opacity = MathF.Sin(progress * MathHelper.Pi) * 0.7f;
				DrawThread(Projectile.Center - Main.screenPosition, target.Center - Main.screenPosition, opacity);
			}
		}

		Texture2D texture = TextureAssets.Projectile[Type].Value;
		Texture2D glow = SoulOrbProjectile.GetGlowTexture();
		Vector2 position = Projectile.Center - Main.screenPosition;
		Vector2 textureOrigin = texture.Size() * 0.5f;
		Vector2 glowOrigin = glow.Size() * 0.5f;
		float pulse = 1f + 0.06f * MathF.Sin(Main.GlobalTimeWrappedHourly * 6f + Projectile.identity);
		Main.EntitySpriteDraw(glow, position, null, new Color(52, 230, 210, 0) * 0.32f,
			0f, glowOrigin, 0.32f * pulse, SpriteEffects.None);
		Main.EntitySpriteDraw(texture, position, null, Color.Lerp(lightColor, Color.White, 0.25f),
			Projectile.rotation, textureOrigin, 1f, SpriteEffects.None);
		return false;
	}

	private static void DrawThread(Vector2 start, Vector2 end, float opacity)
	{
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Vector2 difference = end - start;
		if (difference.LengthSquared() < 1f)
		{
			return;
		}

		Vector2 origin = new(0f, pixel.Height * 0.5f);
		Main.EntitySpriteDraw(pixel, start, null, new Color(34, 230, 211, 0) * (opacity * 0.3f),
			difference.ToRotation(), origin, new Vector2(difference.Length() / pixel.Width, 3f / pixel.Height), SpriteEffects.None);
		Main.EntitySpriteDraw(pixel, start, null, new Color(210, 255, 248, 0) * opacity,
			difference.ToRotation(), origin, new Vector2(difference.Length() / pixel.Width, 1f / pixel.Height), SpriteEffects.None);
	}
}
