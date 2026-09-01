using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using SoulsOfTerra.Common;
using SoulsOfTerra.Content.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SoulsOfTerra.Players;

public sealed class MutationPlayer : ModPlayer
{
	public const int SlotCount = 3;
	private const int SlimeSpawnInterval = 5 * 60;
	private const int SlimeSprayCooldown = 60;

	// Mutation slots belong to the body and intentionally ignore equipment loadouts.
	private readonly MutationId[] mutations = new MutationId[SlotCount];
	private int slimeSpawnTimer;
	private int slimeSprayCooldown;

	public override void Initialize()
	{
		Array.Fill(mutations, MutationId.None);
	}

	public override void SaveData(TagCompound tag)
	{
		List<byte> saved = new(SlotCount);
		foreach (MutationId mutation in mutations)
		{
			saved.Add((byte)mutation);
		}
		tag["mutations"] = saved;
	}

	public override void LoadData(TagCompound tag)
	{
		Array.Fill(mutations, MutationId.None);
		IList<byte> saved = tag.GetList<byte>("mutations");
		for (int slot = 0; slot < Math.Min(SlotCount, saved.Count); slot++)
		{
			MutationId id = (MutationId)saved[slot];
			if (MutationRegistry.TryGet(id, out MutationDefinition definition) && definition.Implemented
				&& Array.IndexOf(mutations, id, 0, slot) < 0)
			{
				mutations[slot] = id;
			}
		}
	}

	public override void PostUpdate()
	{
		if (slimeSprayCooldown > 0)
		{
			slimeSprayCooldown--;
		}

		if (!HasActive(MutationId.Slime) || Player.dead)
		{
			slimeSpawnTimer = 0;
			return;
		}

		if (++slimeSpawnTimer >= SlimeSpawnInterval && Player.whoAmI == Main.myPlayer)
		{
			slimeSpawnTimer = 0;
			SpawnAlliedSlime();
		}
	}

	public override void OnHurt(Player.HurtInfo info)
	{
		if (!HasActive(MutationId.Slime) || slimeSprayCooldown > 0 || Player.whoAmI != Main.myPlayer)
		{
			return;
		}

		slimeSprayCooldown = SlimeSprayCooldown;
		for (int index = 0; index < 6; index++)
		{
			float angle = MathHelper.TwoPi * index / 6f + Main.rand.NextFloat(-0.22f, 0.22f);
			Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(3.2f, 5.1f);
			velocity.Y -= Main.rand.NextFloat(1.4f, 2.6f);
			Projectile.NewProjectile(Player.GetSource_Misc("SlimeMutationSpray"), Player.Center,
				velocity, ModContent.ProjectileType<MutationSlimeGlobProjectile>(), 1, 0f, Player.whoAmI);
		}
	}

	public MutationId GetMutation(int slot)
	{
		return slot >= 0 && slot < SlotCount ? mutations[slot] : MutationId.None;
	}

	public bool IsSlotAvailable(int slot) => slot is 0 or 1 || slot == 2 && Player.extraAccessory;

	public bool HasActive(MutationId id)
	{
		for (int slot = 0; slot < SlotCount; slot++)
		{
			if (IsSlotAvailable(slot) && mutations[slot] == id)
			{
				return true;
			}
		}
		return false;
	}

	public bool Contains(MutationId id, int exceptSlot = -1)
	{
		for (int slot = 0; slot < SlotCount; slot++)
		{
			if (slot != exceptSlot && mutations[slot] == id)
			{
				return true;
			}
		}
		return false;
	}

	public bool TrySetMutation(int slot, MutationId id)
	{
		if (!IsSlotAvailable(slot) || !MutationRegistry.TryGet(id, out MutationDefinition definition)
			|| !definition.Implemented || Contains(id, slot))
		{
			return false;
		}

		mutations[slot] = id;
		return true;
	}

	public bool TryPurge(int slot)
	{
		if (!IsSlotAvailable(slot) || mutations[slot] == MutationId.None)
		{
			return false;
		}

		mutations[slot] = MutationId.None;
		return true;
	}

	public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) => SendState(toWho, fromWho);

	public override void OnEnterWorld()
	{
		if (Player.whoAmI == Main.myPlayer && Main.netMode == NetmodeID.MultiplayerClient)
		{
			SendState();
		}
	}

	public void SendState(int toWho = -1, int fromWho = -1)
	{
		// Local mutations already share this instance; packets only exist in multiplayer.
		if (Main.netMode == NetmodeID.SinglePlayer)
		{
			return;
		}

		ModPacket packet = Mod.GetPacket();
		packet.Write((byte)SoulMessageType.SyncMutations);
		packet.Write((byte)Player.whoAmI);
		for (int slot = 0; slot < SlotCount; slot++)
		{
			packet.Write((byte)mutations[slot]);
		}
		packet.Send(toWho, fromWho);
	}

	public static void HandleStateSync(BinaryReader reader, int whoAmI)
	{
		int playerIndex = reader.ReadByte();
		MutationId[] received = new MutationId[SlotCount];
		for (int slot = 0; slot < SlotCount; slot++)
		{
			received[slot] = (MutationId)reader.ReadByte();
		}

		if (playerIndex < 0 || playerIndex >= Main.maxPlayers
			|| Main.netMode == NetmodeID.Server && playerIndex != whoAmI)
		{
			return;
		}

		MutationPlayer mutationPlayer = Main.player[playerIndex].GetModPlayer<MutationPlayer>();
		Array.Fill(mutationPlayer.mutations, MutationId.None);
		for (int slot = 0; slot < SlotCount; slot++)
		{
			MutationId id = received[slot];
			if (MutationRegistry.TryGet(id, out MutationDefinition definition) && definition.Implemented
				&& !mutationPlayer.Contains(id))
			{
				mutationPlayer.mutations[slot] = id;
			}
		}

		if (Main.netMode == NetmodeID.Server)
		{
			mutationPlayer.SendState(-1, whoAmI);
		}
	}

	private void SpawnAlliedSlime()
	{
		int projectileType = ModContent.ProjectileType<MutationAlliedSlimeProjectile>();
		if (Player.ownedProjectileCounts[projectileType] >= 3)
		{
			Projectile oldest = null;
			foreach (Projectile projectile in Main.ActiveProjectiles)
			{
				if (projectile.owner == Player.whoAmI && projectile.type == projectileType
					&& (oldest is null || projectile.timeLeft < oldest.timeLeft))
				{
					oldest = projectile;
				}
			}
			oldest?.Kill();
		}

		int baseDamage = 6 + (int)(Player.statLifeMax2 * 0.04f);
		int damage = Math.Max(1, (int)Player.GetTotalDamage(DamageClass.Generic).ApplyTo(baseDamage));
		Vector2 spawn = Player.Bottom + new Vector2(Main.rand.NextFloat(-20f, 20f), -12f);
		Projectile.NewProjectile(Player.GetSource_Misc("SlimeMutationBud"), spawn, Vector2.Zero,
			projectileType, damage, 1.5f, Player.whoAmI);
	}
}
