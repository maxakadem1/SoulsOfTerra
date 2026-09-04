using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Common.Rendering;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SoulsOfTerra.Common;

/// <summary>
/// Temper and essence path live on the individual item instance, so a specific weapon becomes worth
/// protecting rather than unlocking an upgrade for every copy of its type.
/// </summary>
public sealed class WeaponTemperItem : GlobalItem
{
	public int Level { get; private set; }
	public int PathIndex { get; private set; } = -1;

	public bool IsTempered => Level > 0;

	public override bool InstancePerEntity => true;

	public override bool AppliesToEntity(Item entity, bool lateInstantiation)
	{
		// Defaults are only reliable once the item is fully instantiated.
		return lateInstantiation && WeaponTemper.CanTemper(entity);
	}

	public static WeaponTemperItem Get(Item item)
	{
		return item is not null && !item.IsAir && item.TryGetGlobalItem(out WeaponTemperItem temper) ? temper : null;
	}

	public static int LevelOf(Item item) => Get(item)?.Level ?? 0;

	public static int PathOf(Item item) => Get(item)?.PathIndex ?? -1;

	/// <summary>Applies a temper state directly. Callers are responsible for validation and authority.</summary>
	public void SetState(int level, int pathIndex)
	{
		Level = Math.Clamp(level, 0, WeaponTemper.MaxLevel);
		PathIndex = Level > 0 && pathIndex >= 0 && pathIndex < EssencePathRegistry.Definitions.Length
			? pathIndex
			: -1;
	}

	public static void CopyTo(Item source, Item destination)
	{
		WeaponTemperItem from = Get(source);
		WeaponTemperItem to = Get(destination);
		if (from is null || to is null)
		{
			return;
		}

		to.SetState(from.Level, from.PathIndex);
	}

	public override GlobalItem Clone(Item from, Item to)
	{
		WeaponTemperItem clone = (WeaponTemperItem)base.Clone(from, to);
		clone.SetState(Level, PathIndex);
		return clone;
	}

	public override void SaveData(Item item, TagCompound tag)
	{
		if (Level <= 0)
		{
			return;
		}

		tag["temperLevel"] = (byte)Level;
		if (PathIndex >= 0)
		{
			tag["temperPath"] = (byte)PathIndex;
		}
	}

	public override void LoadData(Item item, TagCompound tag)
	{
		int level = tag.ContainsKey("temperLevel") ? tag.GetByte("temperLevel") : 0;
		int path = tag.ContainsKey("temperPath") ? tag.GetByte("temperPath") : -1;
		SetState(level, path);
	}

	public override void NetSend(Item item, BinaryWriter writer)
	{
		writer.Write((byte)Level);
		writer.Write((byte)(PathIndex < 0 ? EssencePathRegistry.NoPath : PathIndex));
	}

	public override void NetReceive(Item item, BinaryReader reader)
	{
		int level = reader.ReadByte();
		int path = reader.ReadByte();
		SetState(level, path == EssencePathRegistry.NoPath ? -1 : path);
	}

	protected override bool CloneNewInstances => true;

	public override bool PreDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame,
		Color drawColor, Color itemColor, Vector2 origin, float scale)
	{
		if (!IsTempered)
		{
			return true;
		}

		Main.instance.LoadItem(item.type);
		WeaponTemperOutline.Draw(spriteBatch, item, TextureAssets.Item[item.type].Value, position, frame,
			origin, 0f, scale, drawColor);
		return true;
	}

	public override bool PreDrawInWorld(Item item, SpriteBatch spriteBatch, Color lightColor, Color alphaColor,
		ref float rotation, ref float scale, int whoAmI)
	{
		if (!IsTempered)
		{
			return true;
		}

		Main.instance.LoadItem(item.type);
		Texture2D texture = TextureAssets.Item[item.type].Value;
		Rectangle frame = ItemAnimationDrawing.GetFrame(item.type, texture);
		Vector2 origin = frame.Size() * 0.5f;
		Vector2 position = item.Bottom - Main.screenPosition - new Vector2(0f, origin.Y);
		WeaponTemperOutline.Draw(spriteBatch, item, texture, position, frame, origin, rotation, scale, lightColor);
		return true;
	}

	public override void ModifyWeaponDamage(Item item, Player player, ref StatModifier damage)
	{
		if (!IsTempered)
		{
			return;
		}

		// Added to Base so any damage prefix keeps its full absolute value on top of the convergence.
		damage.Base += WeaponTemper.GetDamageBonus(item, Level);
	}

	public override void ModifyWeaponCrit(Item item, Player player, ref float crit)
	{
		if (IsTempered && TryGetPath(out EssencePathDefinition path) && path.Kind == PathEffectKind.Crit)
		{
			crit += path.CritBonus(Level);
		}
	}

	public override void ModifyHitNPC(Item item, Player player, NPC target, ref NPC.HitModifiers modifiers)
	{
		if (IsTempered && TryGetPath(out EssencePathDefinition path))
		{
			ApplyHitModifiers(path, Level, ref modifiers);
		}
	}

	public override void OnHitNPC(Item item, Player player, NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (IsTempered && TryGetPath(out EssencePathDefinition path))
		{
			ApplyOnHit(path, Level, player, target, damageDone);
		}
	}

	public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
	{
		if (!IsTempered)
		{
			return;
		}

		string pathName = EssencePathRegistry.PathName(PathIndex);
		foreach (TooltipLine line in tooltips)
		{
			if (line.Mod == "Terraria" && line.Name == "ItemName")
			{
				line.Text = string.IsNullOrEmpty(pathName)
					? $"{line.Text} +{Level}"
					: $"{pathName} {line.Text} +{Level}";
				line.OverrideColor = new Color(120, 230, 210);
				break;
			}
		}

		if (TryGetPath(out EssencePathDefinition path))
		{
			tooltips.Add(new TooltipLine(Mod, "SoulsOfTerraTemperPath", path.DescribeAtLevel(Level))
			{
				OverrideColor = new Color(150, 190, 220)
			});
		}
	}

	private bool TryGetPath(out EssencePathDefinition path) =>
		EssencePathRegistry.TryGet(PathIndex, out path);

	/// <summary>Shared with projectiles so a weapon's path behaves the same however it lands its hit.</summary>
	internal static void ApplyHitModifiers(EssencePathDefinition path, int level, ref NPC.HitModifiers modifiers)
	{
		switch (path.Kind)
		{
			case PathEffectKind.ArmorPenetration:
				modifiers.ArmorPenetration += path.ArmorPenetration(level);
				break;
			case PathEffectKind.DoubleDamage:
				if (Main.rand.NextFloat() < path.DoubleDamageChance(level))
				{
					modifiers.FinalDamage *= 2f;
				}
				break;
		}
	}

	internal static void ApplyOnHit(EssencePathDefinition path, int level, Player player, NPC target, int damageDone)
	{
		if (path.Kind == PathEffectKind.Debuff && path.DebuffType > 0)
		{
			target.AddBuff(path.DebuffType, path.DebuffDuration(level));
			return;
		}

		if (path.Kind != PathEffectKind.Lifesteal || player is null || player.whoAmI != Main.myPlayer)
		{
			return;
		}

		int healed = (int)Math.Round(damageDone * path.LifestealShare(level));
		if (healed <= 0 || player.statLife >= player.statLifeMax2)
		{
			return;
		}

		player.statLife = Math.Min(player.statLifeMax2, player.statLife + healed);
		player.HealEffect(healed);
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			NetMessage.SendData(MessageID.PlayerLifeMana, number: player.whoAmI);
		}
	}
}
