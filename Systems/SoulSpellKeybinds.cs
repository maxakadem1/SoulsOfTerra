using Terraria.ModLoader;

namespace SoulsOfTerra.Systems;

public class SoulSpellKeybinds : ModSystem
{
	public static ModKeybind Book { get; private set; }
	public static ModKeybind Stance { get; private set; }

	public override void Load()
	{
		Book = KeybindLoader.RegisterKeybind(Mod, "Soulspell Book", "K");
		Stance = KeybindLoader.RegisterKeybind(Mod, "Soulspell Stance", "LeftAlt");
	}

	public override void Unload()
	{
		Book = null;
		Stance = null;
	}
}
