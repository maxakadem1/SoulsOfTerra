using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace SoulsOfTerra.Systems;

public class BuriedCourtTitleSystem : ModSystem
{
	private const int FadeInTicks = 90;
	private const int HoldTicks = 150;
	private const int FadeOutTicks = 120;
	private const int RevealDurationTicks = FadeInTicks + HoldTicks + FadeOutTicks;
	private const int RetriggerCooldownTicks = 60 * 60 * 10;
	private const float TitleScale = 2f;

	private static Asset<Texture2D> titleTexture;
	private static int revealTime;
	private static int retriggerCooldown;
	private static bool wasInsideCourt;

	public override void Load()
	{
		if (!Main.dedServ)
		{
			titleTexture = ModContent.Request<Texture2D>("SoulsOfTerra/Content/UI/BuriedCourtTitle");
		}
	}

	public override void Unload()
	{
		titleTexture = null;
		ResetState();
	}

	public override void OnWorldLoad() => ResetState();
	public override void OnWorldUnload() => ResetState();

	public override void PostUpdatePlayers()
	{
		if (Main.dedServ || Main.gameMenu || Main.LocalPlayer is null || !Main.LocalPlayer.active)
		{
			return;
		}

		if (retriggerCooldown > 0)
		{
			retriggerCooldown--;
		}

		if (revealTime > 0)
		{
			revealTime--;
		}

		bool isInsideCourt = BuriedCourtSystem.IsInsideCourt(Main.LocalPlayer.Center);
		if (isInsideCourt && !wasInsideCourt && retriggerCooldown <= 0)
		{
			BeginReveal();
		}

		wasInsideCourt = isInsideCourt;
	}

	public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
	{
		int mouseTextIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
		if (mouseTextIndex < 0)
		{
			return;
		}

		layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
			"SoulsOfTerra: Buried Court Title",
			DrawTitle,
			InterfaceScaleType.UI));
	}

	private static void BeginReveal()
	{
		revealTime = RevealDurationTicks;
		retriggerCooldown = RetriggerCooldownTicks;
		SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.48f, Pitch = -0.42f }, Main.LocalPlayer.Center);
		SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.22f, Pitch = -0.68f }, Main.LocalPlayer.Center);
	}

	private static bool DrawTitle()
	{
		if (Main.gameMenu || titleTexture is null || revealTime <= 0)
		{
			return true;
		}

		float opacity = GetOpacity(RevealDurationTicks - revealTime);
		if (opacity <= 0f)
		{
			return true;
		}

		Texture2D texture = titleTexture.Value;
		Vector2 origin = texture.Size() * 0.5f;
		Vector2 position = new(Main.screenWidth * 0.5f, Math.Max(86f, Main.screenHeight * 0.12f));
		SpriteBatch spriteBatch = Main.spriteBatch;

		// A broad low-alpha silhouette creates bloom without obscuring combat beneath it.
		Color glowColor = new Color(68, 224, 214) * (opacity * 0.2f);
		for (int direction = 0; direction < 8; direction++)
		{
			float angle = MathHelper.TwoPi * direction / 8f;
			Vector2 offset = angle.ToRotationVector2() * 2.5f;
			spriteBatch.Draw(texture, position + offset, null, glowColor, 0f, origin,
				TitleScale * 1.025f, SpriteEffects.None, 0f);
		}

		spriteBatch.Draw(texture, position + new Vector2(2f, 3f), null, Color.Black * (opacity * 0.72f),
			0f, origin, TitleScale, SpriteEffects.None, 0f);
		spriteBatch.Draw(texture, position, null, Color.White * opacity,
			0f, origin, TitleScale, SpriteEffects.None, 0f);
		return true;
	}

	private static float GetOpacity(int elapsedTicks)
	{
		if (elapsedTicks < FadeInTicks)
		{
			return SmoothStep(elapsedTicks / (float)FadeInTicks);
		}

		if (elapsedTicks < FadeInTicks + HoldTicks)
		{
			return 1f;
		}

		float fadeOutProgress = (elapsedTicks - FadeInTicks - HoldTicks) / (float)FadeOutTicks;
		return 1f - SmoothStep(fadeOutProgress);
	}

	private static float SmoothStep(float value)
	{
		value = MathHelper.Clamp(value, 0f, 1f);
		return value * value * (3f - 2f * value);
	}

	private static void ResetState()
	{
		revealTime = 0;
		retriggerCooldown = 0;
		wasInsideCourt = false;
	}
}
