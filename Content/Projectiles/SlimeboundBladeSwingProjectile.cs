using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

/// <summary>
/// Slimebound Blade's horizontal slash with gel trail and hitstop impact.
/// </summary>
public class SlimeboundBladeSwingProjectile : BaseCustomSwingProjectile
{
	protected override int SwingDuration => 45;
	protected override int WindupEnd => 10;      // Short crisp coil
	protected override int SnapStart => 11;
	protected override int SnapEnd => 20;        // Fast snap through target
	protected override float SwingReach => 110f;
	protected override float CollisionWidth => 42f;
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

		// Snap sounds - heavy slash
		if (age == SnapStart && Main.netMode != NetmodeID.Server)
		{
			SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.9f, Pitch = -0.2f }, player.Center);
			SoundEngine.PlaySound(SoundID.Item95 with { Volume = 0.35f, Pitch = 0.3f }, player.Center);
		}

		// Gel slash dust trail during snap
		if (age >= SnapStart && age <= SnapEnd && Main.rand.NextBool(2))
		{
			Vector2 trailPosition = player.MountedCenter + swordAngle.ToRotationVector2() * Main.rand.NextFloat(35f, SwingReach);
			Dust dust = Dust.NewDustPerfect(trailPosition, DustID.BlueCrystalShard,
				swordAngle.ToRotationVector2().RotatedByRandom(0.3f) * Main.rand.NextFloat(0.8f, 2.2f),
				140, new Color(45, 230, 210), Main.rand.NextFloat(0.7f, 1.1f));
			dust.noGravity = true;
		}
	}

	protected override float GetWindupWobble(float windupProgress, int direction)
	{
		// Subtle gel wobble during short coil
		return MathF.Sin(windupProgress * MathHelper.Pi * 2f) * 0.04f * (1f - windupProgress);
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

	protected override Color GetTrailColor(float strength)
	{
		// Cyan gel afterimage trail
		return new Color(60, 220, 195) * (0.15f + strength * 0.45f);
	}
}
