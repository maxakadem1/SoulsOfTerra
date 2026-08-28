using System.IO;
using Microsoft.Xna.Framework;
using SoulsOfTerra.Common;
using SoulsOfTerra.Players;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class SoulBloodstainProjectile : ModProjectile
{
	public long StoredSouls { get; private set; }
	public string OriginCharacterId { get; private set; } = string.Empty;
	public override string Texture => $"Terraria/Images/Item_{ItemID.SoulofNight}";

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.IsInteractable[Type] = true;
		ProjectileID.Sets.DrawScreenCheckFluff[Type] = 240;
	}

	public override void SetDefaults()
	{
		Projectile.width = 38;
		Projectile.height = 28;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.netImportant = true;
		Projectile.penetrate = -1;
		Projectile.timeLeft = 2;
	}

	public override bool? CanDamage() => false;
	public override bool ShouldUpdatePosition() => false;

	public override void AI()
	{
		Projectile.timeLeft = 2;
		Main.CurrentFrameFlags.HadAnActiveInteractibleProjectile = true;
		int tier = SoulBloodstainDraw.GetVisualTier(StoredSouls);
		float tierStrength = 0.82f + tier * 0.1f;
		Lighting.AddLight(Projectile.Center + Vector2.UnitY * 4f, 0.18f * tierStrength, 0.08f * tierStrength,
			0.32f * tierStrength);

		if (!Main.dedServ && Main.rand.NextBool(12 - tier))
		{
			Vector2 dustPosition = Projectile.Center + new Vector2(Main.rand.NextFloat(-18f, 18f), Main.rand.NextFloat(5f, 13f));
			Color color = Color.Lerp(new Color(156, 68, 214), new Color(62, 204, 224), Main.rand.NextFloat(0.15f, 0.5f));
			Dust dust = Dust.NewDustPerfect(dustPosition, DustID.DungeonSpirit,
				new Vector2(Main.rand.NextFloat(-0.12f, 0.12f), Main.rand.NextFloat(-0.5f, -0.25f)), 130, color, 0.62f);
			dust.noGravity = true;
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		bool reactive = TryInteracting();
		if (Projectile.active)
		{
			SoulBloodstainDraw.DrawMarker(Projectile, StoredSouls, reactive);
		}

		return false;
	}

	public override void SendExtraAI(BinaryWriter writer)
	{
		writer.Write(StoredSouls);
		writer.Write(OriginCharacterId);
	}

	public override void ReceiveExtraAI(BinaryReader reader)
	{
		StoredSouls = reader.ReadInt64();
		OriginCharacterId = reader.ReadString();
	}

	public void Recover(Player player)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient || StoredSouls <= 0 || !Projectile.active)
		{
			return;
		}

		SoulBloodstainRecoveryProjectile.Spawn(Projectile.GetSource_FromThis(), Projectile.Center, player.whoAmI,
			SoulBloodstainDraw.GetVisualTier(StoredSouls));
		player.GetModPlayer<SoulPlayer>().AddSouls(StoredSouls);
		Projectile.Kill();
	}

	public static void Spawn(IEntitySource source, Vector2 position, long souls, string characterId)
	{
		if (souls <= 0 || Main.netMode == NetmodeID.MultiplayerClient)
		{
			return;
		}

		int index = Projectile.NewProjectile(source, position, Vector2.Zero, ModContent.ProjectileType<SoulBloodstainProjectile>(), 0, 0f, Main.myPlayer);
		if (index >= 0 && index < Main.maxProjectiles && Main.projectile[index].ModProjectile is SoulBloodstainProjectile bloodstain)
		{
			bloodstain.StoredSouls = souls;
			bloodstain.OriginCharacterId = characterId ?? string.Empty;
			bloodstain.Projectile.netUpdate = true;
		}
	}

	public static void RemovePrevious(string characterId)
	{
		if (string.IsNullOrWhiteSpace(characterId))
		{
			return;
		}

		// Character identity is tracked now so owner-only rules can be added later.
		foreach (Projectile projectile in Main.ActiveProjectiles)
		{
			if (projectile.ModProjectile is SoulBloodstainProjectile bloodstain && bloodstain.OriginCharacterId == characterId)
			{
				projectile.Kill();
			}
		}
	}

	private bool TryInteracting()
	{
		if (Main.gamePaused || Main.gameMenu || StoredSouls <= 0)
		{
			return false;
		}

		Player player = Main.LocalPlayer;
		Vector2 compareSpot = player.Center;
		if (!player.IsProjectileInteractibleAndInInteractionRange(Projectile, ref compareSpot))
		{
			return false;
		}

		bool directlyHovered = Projectile.Hitbox.Contains(Main.MouseWorld.ToPoint());
		bool selected = directlyHovered || Main.SmartInteractProj == Projectile.whoAmI;
		if (!selected || player.lastMouseInterface)
		{
			return false;
		}

		Main.HasInteractibleObjectThatIsNotATile = true;
		player.noThrow = 2;
		Main.hoverItemName = Language.GetTextValue("Mods.SoulsOfTerra.UI.RecoverBloodstain", StoredSouls.ToString("N0"));

		if (PlayerInput.UsingGamepad)
		{
			player.GamepadEnableGrappleCooldown();
		}

		if (!Main.mouseRight || !Main.mouseRightRelease || Player.BlockInteractionWithProjectiles != 0)
		{
			return true;
		}

		Main.mouseRightRelease = false;
		player.tileInteractAttempted = true;
		player.tileInteractionHappened = true;
		player.releaseUseTile = false;

		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			ModPacket packet = Mod.GetPacket();
			packet.Write((byte)SoulMessageType.RequestBloodstainRecovery);
			packet.Write((short)Projectile.whoAmI);
			packet.Send();
		}
		else
		{
			Recover(player);
		}

		return true;
	}
}
