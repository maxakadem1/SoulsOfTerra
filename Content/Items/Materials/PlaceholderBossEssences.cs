using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Items.Materials;

public abstract class PlaceholderBossEssence : BossEssenceItem
{
	// One shared vanilla texture keeps every unfinished essence immediately recognizable.
	public override string Texture => $"Terraria/Images/Item_{ItemID.SoulofFlight}";

	public override void SetDefaults()
	{
		Item.width = 16;
		Item.height = 16;
		Item.maxStack = Item.CommonMaxStack;
		Item.rare = ItemRarityID.Orange;
		Item.value = 0;
	}
}

public class DeerclopsEssence : PlaceholderBossEssence { }
public class EaterOfWorldsEssence : PlaceholderBossEssence { }
public class BrainOfCthulhuEssence : PlaceholderBossEssence { }
public class QueenBeeEssence : PlaceholderBossEssence { }
public class SkeletronEssence : PlaceholderBossEssence { }
public class QueenSlimeEssence : PlaceholderBossEssence { }
public class DestroyerEssence : PlaceholderBossEssence { }
public class TwinsEssence : PlaceholderBossEssence { }
public class SkeletronPrimeEssence : PlaceholderBossEssence { }
public class PlanteraEssence : PlaceholderBossEssence { }
public class GolemEssence : PlaceholderBossEssence { }
public class DukeFishronEssence : PlaceholderBossEssence { }
public class EmpressOfLightEssence : PlaceholderBossEssence { }
public class LunaticCultistEssence : PlaceholderBossEssence { }

