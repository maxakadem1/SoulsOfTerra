using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace SoulsOfTerra.Systems;

public class SoulShaderSystem : ModSystem
{
	private static Asset<Effect> bloodstainEffect;
	private static Asset<Effect> soulEaterCoreEffect;
	private static Asset<Effect> dashWakeEffect;

	public override void Load()
	{
		if (!Main.dedServ)
		{
			bloodstainEffect = Mod.Assets.Request<Effect>("Effects/SoulBloodstain", AssetRequestMode.ImmediateLoad);
			soulEaterCoreEffect = Mod.Assets.Request<Effect>("Effects/SoulEaterCore", AssetRequestMode.ImmediateLoad);
			dashWakeEffect = Mod.Assets.Request<Effect>("Effects/SoulDashWake", AssetRequestMode.ImmediateLoad);
		}
	}

	public override void Unload()
	{
		bloodstainEffect = null;
		soulEaterCoreEffect = null;
		dashWakeEffect = null;
	}

	public static Effect GetBloodstainEffect()
	{
		return !Main.dedServ && bloodstainEffect is not null ? bloodstainEffect.Value : null;
	}

	public static Effect GetSoulEaterCoreEffect()
	{
		return !Main.dedServ && soulEaterCoreEffect is not null ? soulEaterCoreEffect.Value : null;
	}

	public static Effect GetDashWakeEffect()
	{
		return !Main.dedServ && dashWakeEffect is not null ? dashWakeEffect.Value : null;
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

	public static bool ApplyDashWake(float intensity, float snapFlash, float alongStart, float alongEnd, float time,
		float seed, float mode)
	{
		Effect effect = GetDashWakeEffect();
		if (effect is null)
		{
			return false;
		}

		effect.Parameters["wakeIntensity"].SetValue(intensity);
		effect.Parameters["snapFlash"].SetValue(snapFlash);
		effect.Parameters["alongStart"].SetValue(alongStart);
		effect.Parameters["alongEnd"].SetValue(alongEnd);
		effect.Parameters["wakeTime"].SetValue(time);
		effect.Parameters["wakeSeed"].SetValue(seed);
		effect.Parameters["wakeMode"].SetValue(mode);
		effect.CurrentTechnique.Passes["WakePass"].Apply();
		return true;
	}
}
