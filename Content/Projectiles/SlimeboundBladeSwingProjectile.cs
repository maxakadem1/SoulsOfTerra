using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

/// <summary>
/// Slimebound Blade's forward-traveling gel slash with hitstop impact.
/// </summary>
public class SlimeboundBladeSwingProjectile : BaseCustomSwingProjectile
{
	protected override int SwingDuration => 45;
	protected override int WindupEnd => 10;      // Short crisp coil
	protected override int SnapStart => 11;
	protected override int SnapEnd => 20;        // Fast slash through target
	protected override float SlashReach => 130f;  // Slash travels forward this far
	protected override float SlashWidth => 50f;   // Width of slash hitbox
	protected override float SwordScale => 1.6f;
	protected override int SwordItemType => ModContent.ItemType<Items.Weapons.Melee.SlimeboundBlade>();

	public override string Texture => "SoulsOfTerra/Content/Items/Weapons/Melee/SlimeboundBlade";

	protected override void OnSwingTick(Player player, int age, int direction, float swordAngle)
	{
		// Coil sound
		if (age == 0 && Main.netMode != NetmodeID.Server)
		{
			SoundEngine.PlaySound(SoundID.Item152 with { Volume = 0.45f, Pitch = -0.25f }, player.Center);
		}

		// Slash sound
		if (age == SnapStart && Main.netMode != NetmodeID.Server)
		{
			SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.9f, Pitch = -0.2f }, player.Center);
			SoundEngine.PlaySound(SoundID.Item95 with { Volume = 0.35f, Pitch = 0.3f }, player.Center);
		}

		// Gel dust trail following the forward slash
		if (age >= SnapStart && age <= SnapEnd && Main.rand.NextBool(2))
		{
			float slashProgress = (age - SnapStart) / (float)(SnapEnd - SnapStart);
			Vector2 slashPos = player.MountedCenter + AimAngle.ToRotationVector2() * (40f + SlashReach * slashProgress * 0.8f);
			Vector2 perpOffset = AimAngle.ToRotationVector2().RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-SlashWidth * 0.4f, SlashWidth * 0.4f);
			
			Dust dust = Dust.NewDustPerfect(slashPos + perpOffset, DustID.BlueCrystalShard,
				AimAngle.ToRotationVector2().RotatedByRandom(0.4f) * Main.rand.NextFloat(1.5f, 3.5f),
				140, new Color(45, 230, 210), Main.rand.NextFloat(0.8f, 1.2f));
			dust.noGravity = true;
		}
	}

	protected override void OnFirstHit(NPC target)
	{
		if (Main.netMode != NetmodeID.Server)
		{
			SoundEngine.PlaySound(SoundID.DD2_MonkStaffSwing with { Volume = 0.5f, Pitch = 0.25f }, target.Center);
		}
	}

	protected override void OnImpact(NPC target, NPC.HitInfo hit, int damageDone, bool alreadyHit)
	{
		// Enhanced gel splash on first impact with hitstop, smaller on subsequent hits
		int splashCount = alreadyHit ? 4 : 12;
		float splashSize = alreadyHit ? 1.4f : 3.5f;
		float dustScale = alreadyHit ? 0.7f : 1.1f;

		for (int i = 0; i < splashCount; i++)
		{
			Vector2 velocity = Main.rand.NextVector2Circular(splashSize, splashSize);
			Dust dust = Dust.NewDustPerfect(target.Center, DustID.BlueCrystalShard, velocity, 120,
				new Color(45, 230, 210), dustScale);
			dust.noGravity = true;
		}
	}

	protected override void DrawSlashTrail(Player player, SpriteBatch spriteBatch)
	{
		// Draw wide gel slash traveling forward through enemies
		float slashProgress = (Age - SnapStart) / (float)(SnapEnd - SnapStart);
		int segments = 12;
		
		for (int i = 0; i < segments; i++)
		{
			float segmentRatio = i / (float)segments;
			if (segmentRatio > slashProgress) break;
			
			float distance = 20f + SlashReach * segmentRatio;
			Vector2 slashPos = player.MountedCenter + AimAngle.ToRotationVector2() * distance;
			
			float fadeStrength = 1f - (slashProgress - segmentRatio) / slashProgress;
			fadeStrength = MathHelper.Clamp(fadeStrength, 0f, 1f);
			
			float width = SlashWidth * 0.6f * (1f - segmentRatio * 0.3f);
			
			// Draw gel slash as elongated particles
			for (int j = 0; j < 3; j++)
			{
				Vector2 perpOffset = AimAngle.ToRotationVector2().RotatedBy(MathHelper.PiOver2) * 
					Main.rand.NextFloat(-width * 0.5f, width * 0.5f);
				
				Color slashColor = new Color(60, 220, 195) * (0.3f * fadeStrength);
				Dust dust = Dust.NewDustPerfect(slashPos + perpOffset, DustID.BlueCrystalShard, Vector2.Zero, 0, slashColor, 1.2f);
				dust.noGravity = true;
				dust.velocity = Vector2.Zero;
			}
		}
	}
}
