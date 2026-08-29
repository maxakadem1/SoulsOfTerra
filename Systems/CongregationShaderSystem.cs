using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace SoulsOfTerra.Systems;

public class CongregationShaderSystem : ModSystem
{
	private const string ShockwaveKey = "SoulsOfTerra:CongregationShockwave";
	private const string ShrineRefractionKey = "SoulsOfTerra:ShrineRefraction";
	private const string BeamKey = "SoulsOfTerra:CongregationBeam";
	private const string CruxKey = "SoulsOfTerra:CruxSentence";
	private static bool registered;
	private static CongregationShockwaveShaderData shaderData;
	private static ShrineRefractionShaderData shrineShaderData;
	private static Asset<Effect> beamEffect;
	private static Asset<Effect> cruxEffect;
	private static MiscShaderData beamShaderData;
	private static Texture2D beamNoiseTexture;

	public override void Load()
	{
		if (Main.dedServ)
		{
			return;
		}

		Asset<Effect> effect = Mod.Assets.Request<Effect>("Effects/CongregationShockwave", AssetRequestMode.ImmediateLoad);
		shaderData = new CongregationShockwaveShaderData(effect, "ScreenPass");
		Filters.Scene[ShockwaveKey] = new Filter(shaderData, EffectPriority.VeryHigh);
		shrineShaderData = new ShrineRefractionShaderData(effect, "ScreenPass");
		Filters.Scene[ShrineRefractionKey] = new Filter(shrineShaderData, EffectPriority.Low);
		beamEffect = Mod.Assets.Request<Effect>("Effects/CongregationBeam", AssetRequestMode.ImmediateLoad);
		beamShaderData = new MiscShaderData(beamEffect, "BeamPass");
		GameShaders.Misc[BeamKey] = beamShaderData;
		cruxEffect = Mod.Assets.Request<Effect>("Effects/CruxSentence", AssetRequestMode.ImmediateLoad);
		GameShaders.Misc[CruxKey] = new MiscShaderData(cruxEffect, "SentencePass");
		registered = true;
	}

	public override void Unload()
	{
		Texture2D noiseToDispose = beamNoiseTexture;
		beamNoiseTexture = null;
		if (registered && !Main.dedServ)
		{
			Filters.Scene[ShockwaveKey].Deactivate();
			Filters.Scene[ShrineRefractionKey].Deactivate();
		}

		registered = false;
		shaderData = null;
		shrineShaderData = null;
		beamShaderData = null;
		beamEffect = null;
		cruxEffect = null;
		if (noiseToDispose is not null)
		{
			// Reloads may unload content off-thread, so graphics resources return to Terraria's main thread.
			Main.QueueMainThreadAction(() =>
			{
				if (!noiseToDispose.IsDisposed)
				{
					noiseToDispose.Dispose();
				}
			});
		}
	}

	public static bool ApplyBeam(float time, float intensity, float seed, float mode)
	{
		if (!registered || Main.dedServ || beamEffect is null || beamShaderData is null)
		{
			return false;
		}

		// Custom beam controls are set immediately before the primitive draw call.
		Effect effect = beamEffect.Value;
		// ApplyBeam is called from PreDraw, making lazy texture creation graphics-thread safe.
		beamNoiseTexture ??= CreateBeamNoiseTexture();
		effect.Parameters["beamTime"].SetValue(time);
		effect.Parameters["beamIntensity"].SetValue(intensity);
		effect.Parameters["beamSeed"].SetValue(seed);
		effect.Parameters["beamMode"].SetValue(mode);
		effect.CurrentTechnique.Passes["BeamPass"].Apply();
		Main.instance.GraphicsDevice.Textures[1] = beamNoiseTexture;
		Main.instance.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
		return true;
	}

	public static Effect GetBeamEffect()
	{
		return registered && !Main.dedServ && beamEffect is not null ? beamEffect.Value : null;
	}

	public static Effect GetCruxEffect()
	{
		return registered && !Main.dedServ && cruxEffect is not null ? cruxEffect.Value : null;
	}

	public static bool ApplyCruxSentence(float writeProgress, float time, float intensity, float mode)
	{
		if (!registered || Main.dedServ || cruxEffect is null)
		{
			return false;
		}

		Effect effect = cruxEffect.Value;
		effect.Parameters["writeProgress"].SetValue(writeProgress);
		effect.Parameters["sentenceTime"].SetValue(time);
		effect.Parameters["sentenceIntensity"].SetValue(intensity);
		effect.Parameters["sentenceMode"].SetValue(mode);
		effect.CurrentTechnique.Passes["SentencePass"].Apply();
		return true;
	}

	private static Texture2D CreateBeamNoiseTexture()
	{
		const int size = 96;
		Texture2D texture = new(Main.instance.GraphicsDevice, size, size, false, SurfaceFormat.Color);
		Color[] pixels = new Color[size * size];
		Random random = new(0x5EEDC0DE);
		for (int index = 0; index < pixels.Length; index++)
		{
			// Independent channels provide two reusable noise fields without an authored texture asset.
			pixels[index] = new Color(random.Next(256), random.Next(256), random.Next(256), 255);
		}

		texture.SetData(pixels);
		return texture;
	}

	public static void UpdateShockwave(Vector2 worldCenter, float progress)
	{
		if (!registered || Main.dedServ)
		{
			return;
		}

		Filter filter = Filters.Scene[ShockwaveKey];
		if (!filter.IsActive())
		{
			Filters.Scene.Activate(ShockwaveKey, worldCenter);
		}

		float fade = MathHelper.Clamp((1f - progress) / 0.18f, 0f, 1f);
		shaderData.Configure(worldCenter, progress, fade);
		filter.GetShader()
			.UseTargetPosition(worldCenter)
			.UseProgress(progress)
			.UseIntensity(MathHelper.Lerp(1.2f, 0.55f, progress))
			.UseOpacity(fade);
	}

	public static void UpdateShrineRefraction(Vector2 worldCenter, float intensity)
	{
		if (!registered || Main.dedServ || shrineShaderData is null)
		{
			return;
		}

		Filter filter = Filters.Scene[ShrineRefractionKey];
		if (!filter.IsActive())
		{
			Filters.Scene.Activate(ShrineRefractionKey, worldCenter);
		}

		shrineShaderData.Configure(worldCenter, intensity);
		filter.GetShader()
			.UseTargetPosition(worldCenter)
			.UseIntensity(intensity)
			.UseOpacity(intensity);
	}

	private sealed class CongregationShockwaveShaderData : ScreenShaderData
	{
		private Vector2 epicenter;
		private float radius;
		private float strength;
		private float interpolation;

		public CongregationShockwaveShaderData(Asset<Effect> shader, string passName) : base(shader, passName)
		{
		}

		public void Configure(Vector2 worldCenter, float progress, float fade)
		{
			Vector2 resolution = new(Main.screenWidth, Main.screenHeight);
			Vector2 screenCenter = resolution * 0.5f;
			Vector2 zoom = Main.GameViewMatrix.Zoom;
			Vector2 targetOnScreen = screenCenter + (worldCenter - Main.screenPosition - screenCenter) * zoom;
			epicenter = targetOnScreen / resolution;

			float easedProgress = 1f - MathF.Pow(1f - progress, 3f);
			radius = MathHelper.Lerp(18f, 820f, easedProgress) * zoom.Y / resolution.Y;
			strength = MathHelper.Lerp(0.011f, 0.005f, progress);
			interpolation = fade;
		}

		public override void Apply()
		{
			// Custom parameters are applied before the standard screen shader pass.
			Shader.Parameters["epicenter"].SetValue(epicenter);
			Shader.Parameters["radius"].SetValue(radius);
			Shader.Parameters["strength"].SetValue(strength);
			Shader.Parameters["interp"].SetValue(interpolation);
			base.Apply();
		}
	}

	private sealed class ShrineRefractionShaderData : ScreenShaderData
	{
		private Vector2 epicenter;
		private float radius;
		private float strength;
		private float interpolation;

		public ShrineRefractionShaderData(Asset<Effect> shader, string passName) : base(shader, passName)
		{
		}

		public void Configure(Vector2 worldCenter, float intensity)
		{
			Vector2 resolution = new(Main.screenWidth, Main.screenHeight);
			Vector2 screenCenter = resolution * 0.5f;
			Vector2 zoom = Main.GameViewMatrix.Zoom;
			Vector2 targetOnScreen = screenCenter + (worldCenter - Main.screenPosition - screenCenter) * zoom;
			epicenter = targetOnScreen / resolution;
			radius = 42f * zoom.Y / resolution.Y;
			strength = 0.0022f * intensity;
			interpolation = intensity;
		}

		public override void Apply()
		{
			// A narrow refractive halo makes the dormant socket feel spatial without obscuring tiles.
			Shader.Parameters["epicenter"].SetValue(epicenter);
			Shader.Parameters["radius"].SetValue(radius);
			Shader.Parameters["strength"].SetValue(strength);
			Shader.Parameters["interp"].SetValue(interpolation);
			base.Apply();
		}
	}

	public static void StopShockwave()
	{
		if (registered && !Main.dedServ && Filters.Scene[ShockwaveKey].IsActive())
		{
			Filters.Scene.Deactivate(ShockwaveKey);
		}
	}

	public static void StopShrineRefraction()
	{
		if (registered && !Main.dedServ && Filters.Scene[ShrineRefractionKey].IsActive())
		{
			Filters.Scene[ShrineRefractionKey].Deactivate();
		}
	}
}
