using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace SoulsOfTerra.Systems;

public class SoulShaderSystem : ModSystem
{
	private static Asset<Effect> bloodstainEffect;

	public override void Load()
	{
		if (!Main.dedServ)
		{
			bloodstainEffect = Mod.Assets.Request<Effect>("Effects/SoulBloodstain", AssetRequestMode.ImmediateLoad);
		}
	}

	public override void Unload()
	{
		bloodstainEffect = null;
	}

	public static Effect GetBloodstainEffect()
	{
		return !Main.dedServ && bloodstainEffect is not null ? bloodstainEffect.Value : null;
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
