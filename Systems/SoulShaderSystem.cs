using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace SoulsOfTerra.Systems;

public class SoulShaderSystem : ModSystem
{
	private static Asset<Effect> bloodstainEffect;
	private static Asset<Effect> soulEaterCoreEffect;

	public override void Load()
	{
		if (!Main.dedServ)
		{
			bloodstainEffect = Mod.Assets.Request<Effect>("Effects/SoulBloodstain", AssetRequestMode.ImmediateLoad);
			soulEaterCoreEffect = Mod.Assets.Request<Effect>("Effects/SoulEaterCore", AssetRequestMode.ImmediateLoad);
		}
	}

	public override void Unload()
	{
		bloodstainEffect = null;
		soulEaterCoreEffect = null;
	}

	public static Effect GetBloodstainEffect()
	{
		return !Main.dedServ && bloodstainEffect is not null ? bloodstainEffect.Value : null;
	}

	public static Effect GetSoulEaterCoreEffect()
	{
		return !Main.dedServ && soulEaterCoreEffect is not null ? soulEaterCoreEffect.Value : null;
	}

	public static void ConfigureSoulEaterCore(Texture2D texture, float time, float intensity)
	{
		Effect effect = GetSoulEaterCoreEffect();
		if (effect is null)
		{
			return;
		}

		effect.Parameters["coreTime"].SetValue(time);
		effect.Parameters["coreIntensity"].SetValue(intensity);
		effect.Parameters["coreTextureSize"].SetValue(texture.Size());
	}

	public static void ApplyBloodstain(float time, float intensity, float seed, float reactive)
	{
		Effect effect = GetBloodstainEffect();
		if (effect is null)
		{
			return;
		}

		effect.Parameters["bloodstainTime"].SetValue(time);
		effect.Parameters["bloodstainIntensity"].SetValue(intensity);
		effect.Parameters["bloodstainSeed"].SetValue(seed);
		effect.Parameters["bloodstainReactive"].SetValue(reactive);
		effect.CurrentTechnique.Passes["BloodstainPass"].Apply();
	}
}
