using SoulsOfTerra.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Scenes;

public class BuriedCourtSceneEffect : ModSceneEffect
{
	public override int Music => MusicID.Dungeon;
	public override SceneEffectPriority Priority => SceneEffectPriority.Environment;

	public override bool IsSceneEffectActive(Player player)
	{
		return BuriedCourtSystem.IsInsideCourt(player.Center);
	}
}
