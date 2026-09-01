using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using SoulsOfTerra.Common;
using SoulsOfTerra.Content.Buffs;
using SoulsOfTerra.Players;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace SoulsOfTerra.Systems;

public class SoulSpellBookSystem : ModSystem
{
	internal static Asset<Texture2D> BookTexture { get; private set; }
	internal static Asset<Texture2D> SoulIconTexture { get; private set; }

	private static UserInterface bookInterface;
	private static SoulSpellBookState bookState;

	public override void Load()
	{
		if (Main.dedServ)
		{
			return;
		}

		BookTexture = ModContent.Request<Texture2D>("SoulsOfTerra/Content/UI/SoulspellUI");
		SoulIconTexture = ModContent.Request<Texture2D>("SoulsOfTerra/Content/UI/SoulCounterIcon");
		bookInterface = new UserInterface();
		bookState = new SoulSpellBookState();
		bookState.Activate();
	}

	public override void Unload()
	{
		BookTexture = null;
		SoulIconTexture = null;
		bookInterface = null;
		bookState = null;
	}

	public static bool IsOpen => bookInterface?.CurrentState == bookState && bookState is not null;

	public static void Toggle()
	{
		if (Main.dedServ || bookState is null)
		{
			return;
		}

		if (IsOpen)
		{
			Close();
			return;
		}

		SoulMenuSystem.Close();
		Main.playerInventory = false;
		bookState.Open();
		bookInterface.SetState(bookState);
	}

	public static void Close()
	{
		bookInterface?.SetState(null);
	}

	public override void UpdateUI(GameTime gameTime)
	{
		if (bookInterface?.CurrentState is not null)
		{
			bookInterface.Update(gameTime);
		}
	}

	public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
	{
		int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
		if (mouseTextIndex < 0)
		{
			return;
		}

		layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
			"SoulsOfTerra: Soulspell Book",
			() =>
			{
				bookInterface?.Draw(Main.spriteBatch, new GameTime());
				return true;
			},
			InterfaceScaleType.UI));
	}
}

internal sealed class SoulSpellBookState : UIState
{
	internal const int TextureWidth = 216;
	internal const int TextureHeight = 141;
	internal const int BookScale = 3;
	internal const int BookWidth = TextureWidth * BookScale;
	internal const int BookHeight = TextureHeight * BookScale;
	internal const int IconSize = 32;
	internal const int IconGap = 8;
	internal const int PagePad = 12;
	internal const int SeparatorHeight = 2;
	internal const int SeparatorGap = 5;
	internal const int StatusGap = 10;
	internal const int StatusHeight = 48;
	internal const int StatusInset = 12;

	// 1x parchment interiors, inset from the inner decorative border.
	private static readonly Rectangle LeftPageSrc = new(22, 20, 70, 98);
	private static readonly Rectangle RightPageSrc = new(123, 20, 70, 98);

	private UIElement root;
	private SoulSpellBookPanel book;
	private SoulSpellStatusPlaque statusPlaque;
	private string lastStatus;

	public override void OnInitialize()
	{
		root = new UIElement();
		root.Width.Set(BookWidth, 0f);
		root.Height.Set(BookHeight + StatusGap + StatusHeight, 0f);
		root.HAlign = 0.5f;
		root.VAlign = 0.5f;
		Append(root);

		book = new SoulSpellBookPanel();
		book.Width.Set(BookWidth, 0f);
		book.Height.Set(BookHeight, 0f);
		book.HAlign = 0.5f;
		root.Append(book);

		statusPlaque = new SoulSpellStatusPlaque();
		statusPlaque.Width.Set(BookWidth - StatusInset * 2f, 0f);
		statusPlaque.Height.Set(StatusHeight, 0f);
		statusPlaque.HAlign = 0.5f;
		statusPlaque.Top.Set(BookHeight + StatusGap, 0f);
		root.Append(statusPlaque);
	}

	public void Open()
	{
		BuildLayout();
		lastStatus = null;
		RefreshStatus();
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		Player player = Main.LocalPlayer;
		if (!player.active || player.dead || Main.keyState.IsKeyDown(Keys.Escape))
		{
			SoulSpellBookSystem.Close();
			return;
		}

		if (root.ContainsPoint(Main.MouseScreen))
		{
			player.mouseInterface = true;
		}

		RefreshStatus();
	}

	private void BuildLayout()
	{
		book.RemoveAllChildren();

		// Always spells occupy a short top row on the left page; paid spells fill both pages after the rule.
		List<SoulSpellDefinition> freeSpells = SoulSpellRegistry.All.Where(spell => spell.IsFree).ToList();
		List<SoulSpellDefinition> paidSpells = SoulSpellRegistry.All.Where(spell => !spell.IsFree).ToList();

		Rectangle left = ScaleRect(LeftPageSrc);
		Rectangle right = ScaleRect(RightPageSrc);

		int freeX = left.X + PagePad;
		int freeY = left.Y + PagePad;
		foreach (SoulSpellDefinition spell in freeSpells)
		{
			AppendIcon(spell, freeX, freeY);
			freeX += IconSize + IconGap;
		}

		int paidStartY = left.Y + PagePad;
		if (freeSpells.Count > 0)
		{
			int separatorWidth = freeSpells.Count * IconSize + Math.Max(0, freeSpells.Count - 1) * IconGap;
			int separatorY = left.Y + PagePad + IconSize + SeparatorGap;
			SoulSpellSeparator separator = new();
			separator.Left.Set(left.X + PagePad, 0f);
			separator.Top.Set(separatorY, 0f);
			separator.Width.Set(separatorWidth, 0f);
			separator.Height.Set(SeparatorHeight, 0f);
			book.Append(separator);
			paidStartY = separatorY + SeparatorHeight + SeparatorGap;
		}

		int paidIndex = PlaceIcons(paidSpells, 0, left, paidStartY);
		PlaceIcons(paidSpells, paidIndex, right, right.Y + PagePad);
		Recalculate();
	}

	private int PlaceIcons(List<SoulSpellDefinition> spells, int startIndex, Rectangle page, int startY)
	{
		int innerWidth = page.Width - PagePad * 2;
		int columns = Math.Max(1, (innerWidth + IconGap) / (IconSize + IconGap));
		int maxRows = Math.Max(0, (page.Bottom - PagePad - startY + IconGap) / (IconSize + IconGap));
		int col = 0;
		int row = 0;
		int index = startIndex;
		while (index < spells.Count && row < maxRows)
		{
			int x = page.X + PagePad + col * (IconSize + IconGap);
			int y = startY + row * (IconSize + IconGap);
			AppendIcon(spells[index], x, y);
			index++;
			col++;
			if (col >= columns)
			{
				col = 0;
				row++;
			}
		}

		return index;
	}

	private void AppendIcon(SoulSpellDefinition spell, int x, int y)
	{
		SoulSpellIcon icon = new(spell);
		icon.Left.Set(x, 0f);
		icon.Top.Set(y, 0f);
		icon.Width.Set(IconSize, 0f);
		icon.Height.Set(IconSize, 0f);
		book.Append(icon);
	}

	private void RefreshStatus()
	{
		SoulPlayer soulPlayer = Main.LocalPlayer.GetModPlayer<SoulPlayer>();
		SoulSpellPlayer spellPlayer = Main.LocalPlayer.GetModPlayer<SoulSpellPlayer>();
		double checkedDrain = SoulSpellRegistry.GetCheckedPaidSoulsPerTick(spellPlayer.SelectionMask);
		double liveDrain = SoulSpellRegistry.GetSoulsPerTick(spellPlayer.SelectionMask, spellPlayer.StanceOn);
		string stance = Language.GetTextValue(spellPlayer.StanceOn
			? "Mods.SoulsOfTerra.UI.SoulspellOn"
			: "Mods.SoulsOfTerra.UI.SoulspellOff");
		string balance = soulPlayer.SoulBalance.ToString("N0");
		string drain = SoulSpellRegistry.FormatDrain(spellPlayer.StanceOn ? liveDrain : checkedDrain);
		string runtime = SoulSpellRegistry.FormatTimeToEmpty(soulPlayer.SoulBalance, checkedDrain);
		string status = $"{stance}|{balance}|{drain}|{runtime}|{spellPlayer.StanceOn}";
		if (status == lastStatus)
		{
			return;
		}

		lastStatus = status;
		statusPlaque.SetStatus(stance, balance, drain, runtime, spellPlayer.StanceOn);
	}

	private static Rectangle ScaleRect(Rectangle source)
	{
		return new Rectangle(
			source.X * BookScale,
			source.Y * BookScale,
			source.Width * BookScale,
			source.Height * BookScale);
	}
}

internal sealed class SoulSpellBookPanel : UIElement
{
	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		Asset<Texture2D> texture = SoulSpellBookSystem.BookTexture;
		if (texture is null || !texture.IsLoaded)
		{
			return;
		}

		CalculatedStyle dimensions = GetDimensions();
		Rectangle destination = new(
			(int)dimensions.X,
			(int)dimensions.Y,
			SoulSpellBookState.BookWidth,
			SoulSpellBookState.BookHeight);

		// Integer 3x scale stays sharp only with point sampling.
		spriteBatch.End();
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
			DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);
		spriteBatch.Draw(texture.Value, destination, Color.White);
		spriteBatch.End();
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
			DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);
	}
}

internal sealed class SoulSpellStatusPlaque : UIElement
{
	private const float LabelScale = 0.58f;
	private const float ValueScale = 0.82f;
	private const float BalanceScale = 0.9f;
	private static readonly Color LabelColor = new(115, 156, 151);
	private static readonly Color ValueColor = new(232, 226, 205);
	private static readonly Color ActiveColor = new(126, 238, 207);
	private static readonly Color InactiveColor = new(143, 151, 148);

	private string stance = string.Empty;
	private string balance = string.Empty;
	private string drain = string.Empty;
	private string runtime = string.Empty;
	private bool stanceOn;

	public void SetStatus(string stanceValue, string balanceValue, string drainValue, string runtimeValue, bool isOn)
	{
		stance = stanceValue;
		balance = balanceValue;
		drain = drainValue;
		runtime = runtimeValue;
		stanceOn = isOn;
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		CalculatedStyle dimensions = GetDimensions();
		// The opaque plaque keeps its text readable over bright world backgrounds.
		Utils.DrawInvBG(spriteBatch, (int)dimensions.X, (int)dimensions.Y, (int)dimensions.Width, (int)dimensions.Height,
			new Color(24, 37, 39) * 0.99f);

		Texture2D pixel = TextureAssets.MagicPixel.Value;
		spriteBatch.Draw(pixel, new Rectangle((int)dimensions.X + 7, (int)dimensions.Y + 3,
			(int)dimensions.Width - 14, 1), new Color(91, 128, 122) * 0.6f);

		float columnWidth = dimensions.Width / 4f;
		for (int i = 1; i < 4; i++)
		{
			int dividerX = (int)(dimensions.X + columnWidth * i);
			spriteBatch.Draw(pixel, new Rectangle(dividerX, (int)dimensions.Y + 8, 1, (int)dimensions.Height - 16),
				new Color(91, 128, 122) * 0.38f);
		}

		string[] labels =
		{
			Language.GetTextValue("Mods.SoulsOfTerra.UI.SoulspellFooterStance"),
			Language.GetTextValue("Mods.SoulsOfTerra.UI.SoulspellFooterSouls"),
			Language.GetTextValue("Mods.SoulsOfTerra.UI.SoulspellFooterDrain"),
			Language.GetTextValue("Mods.SoulsOfTerra.UI.SoulspellFooterRuntime")
		};

		for (int i = 0; i < labels.Length; i++)
		{
			float centerX = dimensions.X + columnWidth * (i + 0.5f);
			DrawCenteredText(spriteBatch, labels[i], centerX, dimensions.Y + 5f, LabelColor, LabelScale);
		}

		float valueY = dimensions.Y + 23f;
		DrawCenteredText(spriteBatch, stance, dimensions.X + columnWidth * 0.5f, valueY,
			stanceOn ? ActiveColor : InactiveColor, ValueScale);
		DrawSoulBalance(spriteBatch, dimensions.X + columnWidth * 1.5f, valueY);

		Color costColor = stanceOn ? ValueColor : ValueColor * 0.48f;
		DrawCenteredText(spriteBatch, drain, dimensions.X + columnWidth * 2.5f, valueY, costColor, ValueScale);
		DrawCenteredText(spriteBatch, runtime, dimensions.X + columnWidth * 3.5f, valueY, costColor, ValueScale);
	}

	private void DrawSoulBalance(SpriteBatch spriteBatch, float centerX, float y)
	{
		const float iconSize = 17f;
		const float gap = 4f;
		Vector2 textSize = FontAssets.MouseText.Value.MeasureString(balance) * BalanceScale;
		float groupWidth = iconSize + gap + textSize.X;
		float left = centerX - groupWidth * 0.5f;

		Asset<Texture2D> icon = SoulSpellBookSystem.SoulIconTexture;
		if (icon?.IsLoaded == true)
		{
			spriteBatch.Draw(icon.Value, new Rectangle((int)left, (int)y, (int)iconSize, (int)iconSize), Color.White);
		}

		Utils.DrawBorderString(spriteBatch, balance, new Vector2(left + iconSize + gap, y - 1f),
			ValueColor, BalanceScale);
	}

	private static void DrawCenteredText(SpriteBatch spriteBatch, string value, float centerX, float y, Color color, float scale)
	{
		Vector2 size = FontAssets.MouseText.Value.MeasureString(value) * scale;
		Utils.DrawBorderString(spriteBatch, value, new Vector2(centerX - size.X * 0.5f, y), color, scale);
	}
}

internal sealed class SoulSpellSeparator : UIElement
{
	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		CalculatedStyle dimensions = GetDimensions();
		Rectangle line = new((int)dimensions.X, (int)dimensions.Y, (int)dimensions.Width, Math.Max(1, (int)dimensions.Height));
		spriteBatch.Draw(TextureAssets.MagicPixel.Value, line, new Color(68, 84, 78, 220));
	}
}

internal sealed class SoulSpellIcon : UIElement
{
	private static readonly Color SelectedBorder = new(90, 168, 148);
	private static readonly Color LiveBorder = new(160, 238, 210);
	private static readonly Color HoverBorder = new(74, 88, 84);
	private static readonly Color TooltipNameColor = new(255, 196, 74);
	private static readonly Color TooltipCostColor = new(80, 224, 196);

	private readonly SoulSpellDefinition spell;

	public SoulSpellIcon(SoulSpellDefinition definition)
	{
		spell = definition;
		OnLeftClick += (_, _) => Toggle();
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		SoulSpellPlayer spellPlayer = Main.LocalPlayer.GetModPlayer<SoulSpellPlayer>();
		bool selected = SoulSpellRegistry.IsSelected(spellPlayer.SelectionMask, spell.Id);
		bool live = !spell.IsFree && selected && spellPlayer.StanceOn;
		CalculatedStyle dimensions = GetDimensions();
		Rectangle iconRect = new((int)dimensions.X, (int)dimensions.Y, SoulSpellBookState.IconSize, SoulSpellBookState.IconSize);

		// Off is dim, selected uses a teal border, live drain uses a brighter border.
		float opacity = selected ? 1f : IsMouseHovering ? 0.55f : 0.42f;
		Color iconColor = live ? new Color(210, 255, 240) : Color.White;
		Texture2D texture = GetIconTexture();
		if (texture == TextureAssets.MagicPixel.Value)
		{
			spriteBatch.Draw(texture, iconRect, new Color(70, 140, 126) * opacity);
		}
		else
		{
			spriteBatch.Draw(texture, iconRect, iconColor * opacity);
		}

		Color border = live ? LiveBorder : selected ? SelectedBorder : IsMouseHovering ? HoverBorder : Color.Transparent;
		if (border.A > 0)
		{
			DrawBorder(spriteBatch, iconRect, border, live ? 2 : 1);
		}

		if (IsMouseHovering)
		{
			DrawSpellTooltip(BuildTooltip(live));
		}
	}

	private void Toggle()
	{
		SoulSpellPlayer spellPlayer = Main.LocalPlayer.GetModPlayer<SoulSpellPlayer>();
		bool selected = SoulSpellRegistry.IsSelected(spellPlayer.SelectionMask, spell.Id);
		spellPlayer.RequestSelection(spell.Id, !selected);
		SoundEngine.PlaySound(SoundID.MenuTick);
	}

	private static void DrawSpellTooltip(string text)
	{
		UICommon.TooltipMouseText(text);
		// Dummy is Iron Pickaxe (type 1) so vanilla draws the boxed tooltip; it is also a crafting ingredient.
		Main.HoverItem.material = false;
	}

	private string BuildTooltip(bool live)
	{
		string band = Language.GetTextValue(spell.IsFree
			? "Mods.SoulsOfTerra.UI.SoulspellAlwaysHeader"
			: "Mods.SoulsOfTerra.UI.SoulspellStanceHeader");
		string tooltip = $"{Colorize(TooltipNameColor, spell.Name)}\n{spell.Description}\n{Colorize(TooltipCostColor, spell.CostText)}\n{band}";
		if (live)
		{
			tooltip += "\n" + Colorize(TooltipCostColor, Language.GetTextValue("Mods.SoulsOfTerra.UI.SoulspellDraining"));
		}

		return tooltip;
	}

	private static string Colorize(Color color, string text)
	{
		return $"[c/{color.R:X2}{color.G:X2}{color.B:X2}:{text}]";
	}

	private Texture2D GetIconTexture()
	{
		int buffType = GetBuffType(spell.Id);
		if (buffType > 0)
		{
			Asset<Texture2D> asset = TextureAssets.Buff[buffType];
			if (asset is not null)
			{
				return asset.Value;
			}
		}

		return TextureAssets.MagicPixel.Value;
	}

	// Book icons reuse the matching buff sprites so tray and page stay in sync.
	private static int GetBuffType(SoulSpellId id)
	{
		return id switch
		{
			SoulSpellId.Dash => ModContent.BuffType<SoulDashBuff>(),
			SoulSpellId.Flight => ModContent.BuffType<SoulFlightBuff>(),
			SoulSpellId.Light => ModContent.BuffType<SoulLightBuff>(),
			_ => 0
		};
	}

	private static void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
	{
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		spriteBatch.Draw(pixel, new Rectangle(rect.X - thickness, rect.Y - thickness, rect.Width + thickness * 2, thickness), color);
		spriteBatch.Draw(pixel, new Rectangle(rect.X - thickness, rect.Bottom, rect.Width + thickness * 2, thickness), color);
		spriteBatch.Draw(pixel, new Rectangle(rect.X - thickness, rect.Y, thickness, rect.Height), color);
		spriteBatch.Draw(pixel, new Rectangle(rect.Right, rect.Y, thickness, rect.Height), color);
	}
}

// TooltipMouseText fakes Iron Pickaxe for the boxed tooltip; hide its crafting tag on book hover.
public sealed class SoulSpellBookHoverItem : GlobalItem
{
	public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
	{
		if (!SoulSpellBookSystem.IsOpen || item.value != -1 || item.scale != 0f)
		{
			return;
		}

		foreach (TooltipLine line in tooltips)
		{
			if (line.Name == "Material")
			{
				line.Hide();
			}
		}
	}
}
