using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Common;
using SoulsOfTerra.Players;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class SoulOrbProjectile : ModProjectile
{
	private const float CollectionRange = 12f * 16f;
	private const float MergeRange = 4f * 16f;
	private const int HomingDelay = 20;
	private const int MergeInterval = 15;

	private int age;

	public long StoredSouls { get; private set; }
	public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.LostSoulFriendly}";

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.DrawScreenCheckFluff[Type] = 240;
	}

	public override void SetDefaults()
	{
		Projectile.width = 16;
		Projectile.height = 16;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.netImportant = true;
		Projectile.penetrate = -1;
		Projectile.timeLeft = 2;
	}

	public override bool? CanDamage() => false;

	public override void AI()
	{
		Projectile.timeLeft = 2;
		Projectile.rotation += 0.04f;
		Lighting.AddLight(Projectile.Center, 0.08f, 0.32f, 0.38f);

		if (!Main.dedServ && Main.rand.NextBool(5))
		{
			Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.DungeonSpirit, -Projectile.velocity * 0.15f, 120, new Color(100, 230, 255), 0.75f);
			dust.noGravity = true;
		}

		age++;
		if (Main.netMode != NetmodeID.MultiplayerClient && age % MergeInterval == 0 && TryMerge())
		{
			return;
		}

		if (age < HomingDelay)
		{
			Projectile.velocity *= 0.94f;
			return;
		}

		Player target = FindNearestPlayer();
		if (target is null)
		{
			Projectile.velocity *= 0.94f;
			return;
		}

		Vector2 toTarget = target.Center - Projectile.Center;
		if (Main.netMode != NetmodeID.MultiplayerClient && toTarget.LengthSquared() <= 24f * 24f)
		{
			target.GetModPlayer<SoulPlayer>().AddSouls(StoredSouls);
			Projectile.Kill();
			return;
		}

		Vector2 desiredVelocity = toTarget.SafeNormalize(Vector2.Zero) * 10f;
		Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.12f);
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Texture2D texture = TextureAssets.Projectile[ProjectileID.LostSoulFriendly].Value;
		Vector2 origin = texture.Size() * 0.5f;
		float pulse = 0.8f + 0.12f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 5f + Projectile.whoAmI);
		Color color = new Color(105, 235, 255, 190);
		Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, color, Projectile.rotation, origin, pulse, SpriteEffects.None);
		return false;
	}

	public override void SendExtraAI(BinaryWriter writer)
	{
		writer.Write(StoredSouls);
		writer.Write(age);
	}

	public override void ReceiveExtraAI(BinaryReader reader)
	{
		StoredSouls = reader.ReadInt64();
		age = reader.ReadInt32();
	}

	public static void Spawn(IEntitySource source, Vector2 position, long souls)
	{
		if (souls <= 0 || Main.netMode == NetmodeID.MultiplayerClient)
		{
			return;
		}

		Vector2 velocity = Main.rand.NextVector2Circular(2.5f, 2.5f) - Vector2.UnitY;
		int index = Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<SoulOrbProjectile>(), 0, 0f, Main.myPlayer);
		if (index >= 0 && index < Main.maxProjectiles && Main.projectile[index].ModProjectile is SoulOrbProjectile orb)
		{
			orb.StoredSouls = souls;
			orb.Projectile.netUpdate = true;
		}
	}

	private Player FindNearestPlayer()
	{
		Player nearest = null;
		float nearestDistanceSquared = CollectionRange * CollectionRange;

		foreach (Player player in Main.ActivePlayers)
		{
			if (player.dead || player.ghost)
			{
				continue;
			}

			float distanceSquared = Vector2.DistanceSquared(Projectile.Center, player.Center);
			if (distanceSquared < nearestDistanceSquared)
			{
				nearest = player;
				nearestDistanceSquared = distanceSquared;
			}
		}

		return nearest;
	}

	private bool TryMerge()
	{
		foreach (Projectile other in Main.ActiveProjectiles)
		{
			if (other.whoAmI <= Projectile.whoAmI || other.type != Type || Vector2.DistanceSquared(Projectile.Center, other.Center) > MergeRange * MergeRange)
			{
				continue;
			}

			if (other.ModProjectile is SoulOrbProjectile otherOrb)
			{
				StoredSouls = SoulMath.SaturatingAdd(StoredSouls, otherOrb.StoredSouls);
				Projectile.netUpdate = true;
				other.Kill();
				return true;
			}
		}

		return false;
	}
}
