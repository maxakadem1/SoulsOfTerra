using System;
using Microsoft.Xna.Framework;
using SoulsOfTerra.Content.Bosses.SealedCongregation;
using SoulsOfTerra.Content.Projectiles;
using Terraria;
using Terraria.ModLoader;

namespace SoulsOfTerra.Systems;

public class SoulShrineEffectsSystem : ModSystem
{
	private const float RefractionRange = 1_200f;

	public override void PostUpdateEverything()
	{
		if (Main.dedServ || Main.gameMenu || !BuriedCourtSystem.Generated || Main.LocalPlayer is null
			|| !Main.LocalPlayer.active || NPC.AnyNPCs(ModContent.NPCType<SealedCongregationBoss>())
			|| CongregationSummonRitualProjectile.IsRitualActive())
		{
			CongregationShaderSystem.StopShrineRefraction();
			return;
		}

		Vector2 socketPosition = BuriedCourtSystem.GetDaisEffectPosition();
		if (Vector2.DistanceSquared(Main.LocalPlayer.Center, socketPosition) > RefractionRange * RefractionRange)
		{
			CongregationShaderSystem.StopShrineRefraction();
			return;
		}

		float pulse = 0.72f + MathF.Sin(Main.GlobalTimeWrappedHourly * 1.55f) * 0.12f;
		CongregationShaderSystem.UpdateShrineRefraction(socketPosition, pulse);
	}

	public override void OnWorldUnload() => CongregationShaderSystem.StopShrineRefraction();
}
