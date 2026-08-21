using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace SoulsOfTerra.Config;

public class SoulServerConfig : ModConfig
{
	// Server scope keeps every player's economy on the same scale.
	public override ConfigScope Mode => ConfigScope.ServerSide;

	[DefaultValue(1f)]
	[Range(0f, 100f)]
	[Increment(0.1f)]
	public float SoulRewardMultiplier { get; set; } = 1f;
}
