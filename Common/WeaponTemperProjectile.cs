using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SoulsOfTerra.Common;

/// <summary>
/// Carries a tempered weapon's essence path onto everything it fires, so a path behaves identically
/// whether the weapon lands its hit by swinging or by shooting.
/// </summary>
public sealed class WeaponTemperProjectile : GlobalProjectile
{
	private int level;
	private int pathIndex = -1;

	public override bool InstancePerEntity => true;

	public override void OnSpawn(Projectile projectile, IEntitySource source)
	{
		switch (source)
		{
			case EntitySource_ItemUse itemUse:
				Inherit(WeaponTemperItem.LevelOf(itemUse.Item), WeaponTemperItem.PathOf(itemUse.Item));
				break;
			// Chained projectiles keep the path of whatever originally fired them.
			case EntitySource_Parent { Entity: Projectile parent }:
				if (parent.TryGetGlobalProjectile(out WeaponTemperProjectile parentTemper))
				{
					Inherit(parentTemper.level, parentTemper.pathIndex);
				}
				break;
		}
	}

	private void Inherit(int sourceLevel, int sourcePath)
	{
		level = sourceLevel;
		pathIndex = sourcePath;
	}

	public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
	{
		binaryWriter.Write((byte)level);
		binaryWriter.Write((byte)(pathIndex < 0 ? EssencePathRegistry.NoPath : pathIndex));
	}

	public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
	{
		level = binaryReader.ReadByte();
		int path = binaryReader.ReadByte();
		pathIndex = path == EssencePathRegistry.NoPath ? -1 : path;
	}

	public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
	{
		if (TryGetPath(out EssencePathDefinition path))
		{
			WeaponTemperItem.ApplyHitModifiers(path, level, ref modifiers);
		}
	}

	public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (TryGetPath(out EssencePathDefinition path))
		{
			WeaponTemperItem.ApplyOnHit(path, level, Main.player[projectile.owner], target, damageDone);
		}
	}

	private bool TryGetPath(out EssencePathDefinition path)
	{
		path = null;
		return level > 0 && EssencePathRegistry.TryGet(pathIndex, out path);
	}
}
