using System;
using Microsoft.Xna.Framework;
using SoulsOfTerra.Common;
using SoulsOfTerra.Content.Projectiles;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SoulsOfTerra.Players;

public class SoulPlayer : ModPlayer
{
	private const int SafePositionRefreshTicks = 15;

	private int safePositionTimer;
	private Vector2 lastSafePosition;

	public long SoulBalance { get; private set; }
	public string CharacterId { get; private set; } = string.Empty;
	public long RecentGain { get; private set; }
	public int RecentGainTime { get; private set; }
	public int SoulSwingIndex;

	public override void Initialize()
	{
		SoulBalance = 0;
		CharacterId = Guid.NewGuid().ToString("N");
		RecentGain = 0;
		RecentGainTime = 0;
		SoulSwingIndex = 0;
	}

	public override void SaveData(TagCompound tag)
	{
		if (SoulBalance > 0)
		{
			tag["soulBalance"] = SoulBalance;
		}

		tag["characterId"] = CharacterId;
	}

	public override void LoadData(TagCompound tag)
	{
		SoulBalance = Math.Max(0, tag.GetLong("soulBalance"));
		string savedId = tag.GetString("characterId");
		if (!string.IsNullOrWhiteSpace(savedId))
		{
			CharacterId = savedId;
		}
	}

	public override void OnEnterWorld()
	{
		lastSafePosition = Player.Center;
	}

	public override void PostUpdate()
	{
		if (RecentGainTime > 0)
		{
			RecentGainTime--;
		}

		if (Player.dead || Player.lavaWet || ++safePositionTimer < SafePositionRefreshTicks)
		{
			return;
		}

		safePositionTimer = 0;
		// Grounded snapshots keep hazardous deaths recoverable.
		if (Player.velocity.Y == 0f && IsPositionInWorld(Player.Center))
		{
			lastSafePosition = Player.Center;
		}
	}

	public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			return;
		}

		SoulBloodstainProjectile.RemovePrevious(CharacterId);

		long droppedSouls = SoulBalance;
		SetBalanceAuthoritative(0, false);
		if (droppedSouls <= 0)
		{
			return;
		}

		Vector2 deathPosition = IsValidBloodstainPosition(Player.Center) ? Player.Center : lastSafePosition;
		SoulBloodstainProjectile.Spawn(Player.GetSource_Death(), deathPosition, droppedSouls, CharacterId);
	}

	public void AddSouls(long amount)
	{
		if (amount <= 0 || Main.netMode == NetmodeID.MultiplayerClient)
		{
			return;
		}

		SetBalanceAuthoritative(SoulMath.SaturatingAdd(SoulBalance, amount), true);
	}

	public bool TrySpendSouls(long amount)
	{
		if (amount <= 0 || Main.netMode == NetmodeID.MultiplayerClient || SoulBalance < amount)
		{
			return false;
		}

		SetBalanceAuthoritative(SoulBalance - amount, false);
		return true;
	}

	public void ReceiveSync(long balance, string characterId)
	{
		long sanitizedBalance = Math.Max(0, balance);
		bool showGain = sanitizedBalance > SoulBalance;
		ApplyBalance(sanitizedBalance, showGain);

		if (!string.IsNullOrWhiteSpace(characterId))
		{
			CharacterId = characterId;
		}
	}

	public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
	{
		SyncSoulData(toWho, fromWho);
	}

	public void SyncSoulData(int toWho = -1, int fromWho = -1)
	{
		ModPacket packet = Mod.GetPacket();
		packet.Write((byte)SoulMessageType.SyncPlayer);
		packet.Write((byte)Player.whoAmI);
		packet.Write(SoulBalance);
		packet.Write(CharacterId);
		packet.Send(toWho, fromWho);
	}

	private void SetBalanceAuthoritative(long balance, bool showGain)
	{
		ApplyBalance(Math.Max(0, balance), showGain);
		if (Main.netMode == NetmodeID.Server)
		{
			SyncSoulData();
		}
	}

	private void ApplyBalance(long balance, bool showGain)
	{
		long oldBalance = SoulBalance;
		SoulBalance = balance;

		if (!showGain || balance <= oldBalance || Player.whoAmI != Main.myPlayer)
		{
			return;
		}

		long gainedSouls = balance - oldBalance;
		// Nearby pickups join the visible total instead of replacing a large reward.
		RecentGain = RecentGainTime > 0
			? SoulMath.SaturatingAdd(RecentGain, gainedSouls)
			: gainedSouls;
		RecentGainTime = 120;
		if (!Main.dedServ)
		{
			SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.55f, Pitch = 0.25f });
		}
	}

	private bool IsValidBloodstainPosition(Vector2 position)
	{
		if (!IsPositionInWorld(position))
		{
			return false;
		}

		int tileX = (int)(position.X / 16f);
		int tileY = (int)(position.Y / 16f);
		Tile tile = Framing.GetTileSafely(tileX, tileY);
		bool dangerousLiquid = tile.LiquidAmount > 0 && (tile.LiquidType == LiquidID.Lava || tile.LiquidType == LiquidID.Shimmer);
		return !dangerousLiquid && !Collision.SolidCollision(position - Player.Size * 0.5f, Player.width, Player.height);
	}

	private static bool IsPositionInWorld(Vector2 position)
	{
		return WorldGen.InWorld((int)(position.X / 16f), (int)(position.Y / 16f), 10);
	}
}
