using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace SoulsOfTerra.Systems;

public class SoulSwingShaderSystem : ModSystem
{
	public const string RibbonKey = "SoulsOfTerra:SoulSwingRibbon";

	private static bool registered;
	private static Asset<Effect> ribbonEffect;

	public override void Load()
	{
		if (Main.dedServ)
		{
			return;
		}

		ribbonEffect = Mod.Assets.Request<Effect>("Effects/SoulSwingRibbon", AssetRequestMode.ImmediateLoad);
		GameShaders.Misc[RibbonKey] = new MiscShaderData(ribbonEffect, "RibbonPass");
		registered = true;
	}

	public override void Unload()
	{
		registered = false;
		ribbonEffect = null;
	}

	public static bool ApplyRibbon(Color color, float time, float intensity)
	{
		if (!registered || Main.dedServ || ribbonEffect is null)
		{
			return false;
		}

		Effect effect = ribbonEffect.Value;
		effect.Parameters["uColor"].SetValue(color.ToVector3());
		effect.Parameters["uOpacity"].SetValue(1f);
		effect.Parameters["ribbonTime"].SetValue(time);
		effect.Parameters["ribbonIntensity"].SetValue(intensity);
		effect.Parameters["uTransform"].SetValue(Main.GameViewMatrix.NormalizedTransformationmatrix);
		effect.CurrentTechnique.Passes["RibbonPass"].Apply();
		return true;
	}
}
