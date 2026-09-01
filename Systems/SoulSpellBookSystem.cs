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
	internal static Asset<Texture2D> ArrowBaseTexture { get; private set; }
	internal static Asset<Texture2D> ArrowHoverTexture { get; private set; }
	internal static Asset<Texture2D> ArrowPressedTexture { get; private set; }

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
		ArrowBaseTexture = ModContent.Request<Texture2D>("SoulsOfTerra/Content/UI/SoulspellArrowBase");
		ArrowHoverTexture = ModContent.Request<Texture2D>("SoulsOfTerra/Content/UI/SoulspellArrowOnhover");
		ArrowPressedTexture = ModContent.Request<Texture2D>("SoulsOfTerra/Content/UI/SoulspellArrowOnpress");
		bookInterface = new UserInterface();
		bookState = new SoulSpellBookState();
		bookState.Activate();
	}

	public override void Unload()
	{
		BookTexture = null;
		SoulIconTexture = null;
		ArrowBaseTexture = null;
		ArrowHoverTexture = null;
		ArrowPressedTexture = null;
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
		SoulApparatusSystem.Close();
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
	private SoulSpellPageArrow leftArrow;
	private SoulSpellPageArrow rightArrow;
	private string lastStatus;
	private ulong lastLearnedMask;
	private int currentSpread;
	private int spreadCount = 1;

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

		leftArrow = new SoulSpellPageArrow(false, () => ChangeSpread(-1));
		leftArrow.Left.Set(-38f, 0f);
		leftArrow.Top.Set(BookHeight * 0.5f - 16f, 0f);
		root.Append(leftArrow);

		rightArrow = new SoulSpellPageArrow(true, () => ChangeSpread(1));
		rightArrow.Left.Set(BookWidth + 12f, 0f);
		rightArrow.Top.Set(BookHeight * 0.5f - 16f, 0f);
		root.Append(rightArrow);
	}

	public void Open()
	{
		currentSpread = 0;
		BuildLayout();
		lastLearnedMask = Main.LocalPlayer.GetModPlayer<SoulSpellPlayer>().LearnedMask;
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

		if (root.ContainsPoint(Main.MouseScreen) || leftArrow.ContainsPoint(Main.MouseScreen)
			|| rightArrow.ContainsPoint(Main.MouseScreen))
		{
			player.mouseInterface = true;
		}

		ulong learnedMask = player.GetModPlayer<SoulSpellPlayer>().LearnedMask;
		if (learnedMask != lastLearnedMask)
		{
			lastLearnedMask = learnedMask;
			BuildLayout();
		}

		RefreshStatus();
	}

	private void BuildLayout()
	{
		book.RemoveAllChildren();

		SoulSpellPlayer spellPlayer = Main.LocalPlayer.GetModPlayer<SoulSpellPlayer>();
		List<BookPage> pages = BuildPages(spellPlayer);
		spreadCount = Math.Max(1, (pages.Count + 1) / 2);
		currentSpread = Math.Clamp(currentSpread, 0, spreadCount - 1);
		Rectangle[] areas = { ScaleRect(LeftPageSrc), ScaleRect(RightPageSrc) };
		for (int side = 0; side < 2; side++)
		{
			int pageIndex = currentSpread * 2 + side;
			if (pageIndex < pages.Count)
			{
				RenderPage(pages[pageIndex], areas[side]);
			}
		}

		leftArrow.Enabled = currentSpread > 0;
		rightArrow.Enabled = currentSpread + 1 < spreadCount;
		Recalculate();
	}

	private static List<BookPage> BuildPages(SoulSpellPlayer spellPlayer)
	{
		const int columns = 4;
		const int rows = 7;
		List<BookPage> pages = new() { new BookPage() };
		BookPage page = pages[0];

		AddSection("Always", SoulSpellRegistry.All.Where(spell => spell.IsFree).ToList());
		foreach (IGrouping<SoulSpellCategory, SoulSpellDefinition> group in SoulSpellRegistry.All
			.Where(spell => !spell.IsFree && spellPlayer.HasLearned(spell.Id))
			.GroupBy(spell => spell.Category))
		{
			AddSection(SoulSpellRegistry.CategoryName(group.Key), group.ToList());
		}

		return pages;

		void AddSection(string heading, List<SoulSpellDefinition> spells)
		{
			if (page.Row > 0 && page.Row + 2 > rows)
			{
				page = new BookPage();
				pages.Add(page);
			}

			page.Entries.Add(new BookPageEntry(heading, default, page.Row++, -1));
			int spellIndex = 0;
			while (spellIndex < spells.Count)
			{
				if (page.Row >= rows)
				{
					page = new BookPage();
					pages.Add(page);
				}

				for (int column = 0; column < columns && spellIndex < spells.Count; column++)
				{
					page.Entries.Add(new BookPageEntry(null, spells[spellIndex++], page.Row, column));
				}
				page.Row++;
			}
		}
	}

	private void RenderPage(BookPage page, Rectangle area)
	{
		foreach (BookPageEntry entry in page.Entries)
		{
			int y = area.Y + PagePad + entry.Row * (IconSize + IconGap);
			if (entry.Column < 0)
			{
				UIText header = new(entry.Heading, 0.62f);
				header.Left.Set(area.X + PagePad, 0f);
				header.Top.Set(y + 6f, 0f);
				header.TextColor = new Color(72, 66, 55);
				book.Append(header);
			}
			else
			{
				AppendIcon(entry.Spell, area.X + PagePad + entry.Column * (IconSize + IconGap), y);
			}
		}
	}

	private void ChangeSpread(int direction)
	{
		int next = Math.Clamp(currentSpread + direction, 0, spreadCount - 1);
		if (next == currentSpread)
		{
			return;
		}

		currentSpread = next;
		SoundEngine.PlaySound(SoundID.MenuTick);
		BuildLayout();
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
		double checkedDrain = SoulSpellRegistry.GetCheckedPaidSoulsPerTick(spellPlayer.SelectionMask, spellPlayer.LearnedMask);
		double liveDrain = SoulSpellRegistry.GetSoulsPerTick(spellPlayer.SelectionMask, spellPlayer.LearnedMask, spellPlayer.StanceOn);
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

	private sealed class BookPage
	{
		public readonly List<BookPageEntry> Entries = new();
		public int Row;
	}

	private readonly record struct BookPageEntry(string Heading, SoulSpellDefinition Spell, int Row, int Column);
}

internal sealed class SoulSpellPageArrow : UIElement
{
	private readonly bool pointsRight;
	private readonly Action action;

	public bool Enabled { get; set; }

	public SoulSpellPageArrow(bool pointsRight, Action action)
	{
		this.pointsRight = pointsRight;
		this.action = action;
		Width.Set(26f, 0f);
		Height.Set(33f, 0f);
		OnLeftClick += (_, _) =>
		{
			if (Enabled)
			{
				action();
			}
		};
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		Asset<Texture2D> asset = IsMouseHovering && Main.mouseLeft
			? SoulSpellBookSystem.ArrowPressedTexture
			: IsMouseHovering ? SoulSpellBookSystem.ArrowHoverTexture : SoulSpellBookSystem.ArrowBaseTexture;
		if (asset is null || !asset.IsLoaded)
		{
			return;
		}

		CalculatedStyle dimensions = GetDimensions();
		SpriteEffects effects = pointsRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
		spriteBatch.Draw(asset.Value, dimensions.Center(), null, Color.White * (Enabled ? 1f : 0.25f), 0f,
			asset.Value.Size() * 0.5f, 1f, effects, 0f);
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
		int buffType = spell.BuffType;
		if (buffType <= 0)
		{
			buffType = spell.Id switch
			{
				SoulSpellId.Dash => ModContent.BuffType<SoulDashBuff>(),
				SoulSpellId.Flight => ModContent.BuffType<SoulFlightBuff>(),
				_ => 0
			};
		}

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
