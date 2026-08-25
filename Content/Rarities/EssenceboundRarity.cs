using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Rarities;

public class EssenceboundRarity : ModRarity
{
	public override Color RarityColor
	{
		get
		{
			// A slow three-color cycle gives dark equipment a restrained metallic glint.
			float cycle = Main.GlobalTimeWrappedHourly % 3f;
			Color obsidianPurple = new(104, 78, 128);
			Color soulTeal = new(82, 174, 158);
			Color silver = new(196, 205, 202);
			return cycle < 1f
				? Color.Lerp(obsidianPurple, soulTeal, cycle)
				: cycle < 2f
					? Color.Lerp(soulTeal, silver, cycle - 1f)
					: Color.Lerp(silver, obsidianPurple, cycle - 2f);
		}
	}
}
