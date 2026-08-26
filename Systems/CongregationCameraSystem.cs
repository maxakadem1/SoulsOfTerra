using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace SoulsOfTerra.Systems;

public class CongregationCameraSystem : ModSystem
{
	private static float shakeStrength;

	public static void AddShake(Vector2 worldPosition, float strength)
	{
		if (Main.dedServ || Main.LocalPlayer is null)
		{
			return;
		}

		// Distance attenuation keeps remote multiplayer attacks from shaking an uninvolved player's view.
		float distance = Vector2.Distance(Main.LocalPlayer.Center, worldPosition);
		float attenuation = MathHelper.Clamp(1f - distance / 1_800f, 0f, 1f);
		shakeStrength = Math.Max(shakeStrength, strength * attenuation);
	}

	public override void ModifyScreenPosition()
	{
		if (shakeStrength <= 0.05f)
		{
			shakeStrength = 0f;
			return;
		}

		Main.screenPosition += Main.rand.NextVector2Circular(shakeStrength, shakeStrength);
		shakeStrength *= 0.86f;
	}

	public override void OnWorldUnload()
	{
		shakeStrength = 0f;
	}

	public override void Unload()
	{
		shakeStrength = 0f;
	}
}
