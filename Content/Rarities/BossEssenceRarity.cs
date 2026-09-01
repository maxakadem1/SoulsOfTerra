using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Rarities;

public sealed class BossEssenceRarity : ModRarity
{
	public override Color RarityColor
	{
		get
		{
			// One shared phase gives every boss Essence the same animated identity.
			float cycle = Main.GlobalTimeWrappedHourly % 3f;
			Color purple = new(176, 94, 218);
			Color teal = new(80, 225, 205);
			Color silver = new(205, 222, 220);
			return cycle < 1f
				? Color.Lerp(purple, teal, cycle)
				: cycle < 2f
					? Color.Lerp(teal, silver, cycle - 1f)
					: Color.Lerp(silver, purple, cycle - 2f);
		}
	}
}
