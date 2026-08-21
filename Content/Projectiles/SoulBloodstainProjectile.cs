using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Players;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
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
		Lighting.AddLight(Projectile.Center, 0.24f, 0.08f, 0.34f);

		if (!Main.dedServ && Main.rand.NextBool(6))
		{
			Vector2 dustPosition = Projectile.Center + Main.rand.NextVector2Circular(18f, 8f);
			Dust dust = Dust.NewDustPerfect(dustPosition, DustID.Shadowflame, -Vector2.UnitY * 0.35f, 120, new Color(170, 80, 230), 0.8f);
			dust.noGravity = true;
		}
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Vector2 drawPosition = Projectile.Center - Main.screenPosition;
		float pulse = 0.75f + 0.15f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 4f + Projectile.whoAmI);
		Color glow = new Color(115, 35, 165, 0) * pulse;

		Main.EntitySpriteDraw(pixel, drawPosition, null, glow, 0f, new Vector2(0.5f), new Vector2(42f, 12f), SpriteEffects.None);
		Texture2D soulTexture = TextureAssets.Item[ItemID.SoulofNight].Value;
		Main.EntitySpriteDraw(soulTexture, drawPosition - Vector2.UnitY * 8f, null, new Color(210, 150, 255, 220), 0f, soulTexture.Size() * 0.5f, 0.8f + pulse * 0.2f, SpriteEffects.None);

		TryInteracting();
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

		foreach (Projectile projectile in Main.ActiveProjectiles)
		{
			if (projectile.ModProjectile is SoulBloodstainProjectile bloodstain && bloodstain.OriginCharacterId == characterId)
			{
				projectile.Kill();
			}
		}
	}

	private void TryInteracting()
	{
		if (Main.gamePaused || Main.gameMenu || StoredSouls <= 0)
		{
			return;
		}

		Player player = Main.LocalPlayer;
		Vector2 compareSpot = player.Center;
		if (!player.IsProjectileInteractibleAndInInteractionRange(Projectile, ref compareSpot))
		{
			return;
		}

		bool directlyHovered = Projectile.Hitbox.Contains(Main.MouseWorld.ToPoint());
		bool selected = directlyHovered || Main.SmartInteractProj == Projectile.whoAmI;
		if (!selected || player.lastMouseInterface)
		{
			return;
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
			return;
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

		SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.7f, Pitch = -0.1f });
	}
}
