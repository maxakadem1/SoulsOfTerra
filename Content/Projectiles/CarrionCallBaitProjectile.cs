using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class CarrionCallBaitProjectile : ModProjectile
{
	public const int RumbleDuration = 48;

	private enum BaitState
	{
		Flying = 0,
		Planted = 1,
		Committed = 2
	}

	private BaitState State
	{
		get => (BaitState)(int)Projectile.ai[0];
		set => Projectile.ai[0] = (float)value;
	}

	private int StuckNpcIndex => (int)Projectile.ai[1] - 1;

	private ref float RumbleTimer => ref Projectile.localAI[0];

	public override string Texture => $"Terraria/Images/Item_{ItemID.WormFood}";

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.DrawScreenCheckFluff[Type] = 480;
	}

	public override void SetDefaults()
	{
		Projectile.width = 18;
		Projectile.height = 18;
		Projectile.friendly = false;
		Projectile.DamageType = DamageClass.Ranged;
		Projectile.penetrate = -1;
		Projectile.timeLeft = 420;
		Projectile.tileCollide = true;
		Projectile.ignoreWater = true;
		Projectile.netImportant = true;
	}

	public override bool? CanDamage() => false;

	public override bool? CanCutTiles() => false;

	public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
	{
		fallThrough = Projectile.velocity.Y < 0f;
		width = 12;
		height = 12;
		return true;
	}

	public override bool OnTileCollide(Vector2 oldVelocity)
	{
		if (State == BaitState.Flying)
		{
			PlantOnTile();
		}

		return false;
	}

	public override void AI()
	{
		Player owner = Main.player[Projectile.owner];
		if (!owner.active || owner.dead)
		{
			Projectile.Kill();
			return;
		}

		switch (State)
		{
			case BaitState.Flying:
				UpdateFlight();
				break;
			case BaitState.Planted:
				UpdatePlanted();
				break;
			case BaitState.Committed:
				// Marker stays at the freeze point so the dive path stays readable.
				Projectile.velocity = Vector2.Zero;
				Projectile.tileCollide = false;
				break;
		}

		Lighting.AddLight(Projectile.Center, 0.18f, 0.32f, 0.08f);
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Main.instance.LoadItem(ItemID.WormFood);
		Texture2D texture = TextureAssets.Item[ItemID.WormFood].Value;
		Vector2 draw = Projectile.Center - Main.screenPosition;
		float bob = State == BaitState.Flying ? 0f : MathF.Sin(Main.GlobalTimeWrappedHourly * 9f + Projectile.whoAmI) * 1.6f;
		float rumble = State == BaitState.Planted ? MathF.Sin(RumbleTimer * 0.9f) * 1.2f : 0f;
		Main.EntitySpriteDraw(texture, draw + new Vector2(rumble, bob), null, Projectile.GetAlpha(lightColor),
			Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None);
		return false;
	}

	private void UpdateFlight()
	{
		Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + 0.22f, -16f, 16f);
		Projectile.rotation += Projectile.velocity.X * 0.04f;
		TryStickToEnemy();
	}

	private void UpdatePlanted()
	{
		FollowStuckNpc();
		RumbleTimer++;
		CreateRumbleDust();
		if (RumbleTimer == 1f)
		{
			SoundEngine.PlaySound(SoundID.WormDig with { Volume = 0.7f, Pitch = -0.2f }, Projectile.Center);
		}
		else if (RumbleTimer % 16f < 0.5f)
		{
			SoundEngine.PlaySound(SoundID.WormDig with { Volume = 0.28f, Pitch = -0.45f }, Projectile.Center);
		}

		if (RumbleTimer < RumbleDuration)
		{
			return;
		}

		CommitHunt();
	}

	private void TryStickToEnemy()
	{
		for (int index = 0; index < Main.maxNPCs; index++)
		{
			NPC npc = Main.npc[index];
			if (!npc.CanBeChasedBy(Projectile) || !Projectile.Hitbox.Intersects(npc.Hitbox))
			{
				continue;
			}

			State = BaitState.Planted;
			Projectile.ai[1] = index + 1;
			Projectile.velocity = Vector2.Zero;
			Projectile.tileCollide = false;
			Projectile.Center = npc.Center;
			Projectile.netUpdate = true;
			SoundEngine.PlaySound(SoundID.Item17 with { Volume = 0.45f, Pitch = -0.4f }, Projectile.Center);
			return;
		}
	}

	private void FollowStuckNpc()
	{
		int npcIndex = StuckNpcIndex;
		if (npcIndex < 0)
		{
			Projectile.velocity = Vector2.Zero;
			return;
		}

		if (npcIndex >= Main.maxNPCs || !Main.npc[npcIndex].active)
		{
			Projectile.ai[1] = 0f;
			Projectile.netUpdate = true;
			return;
		}

		Projectile.Center = Main.npc[npcIndex].Center;
		Projectile.velocity = Vector2.Zero;
	}

	private void PlantOnTile()
	{
		State = BaitState.Planted;
		Projectile.ai[1] = 0f;
		Projectile.velocity = Vector2.Zero;
		Projectile.tileCollide = false;
		Projectile.netUpdate = true;
		SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.4f, Pitch = -0.25f }, Projectile.Center);
	}

	private void CommitHunt()
	{
		State = BaitState.Committed;
		int tagged = StuckNpcIndex;
		if (tagged >= 0 && tagged < Main.maxNPCs && Main.npc[tagged].active)
		{
			Projectile.Center = Main.npc[tagged].Center;
		}

		Projectile.velocity = Vector2.Zero;
		Projectile.tileCollide = false;
		Projectile.netUpdate = true;
		SoundEngine.PlaySound(SoundID.Roar with { Volume = 0.75f, Pitch = -0.15f }, Projectile.Center);

		if (Projectile.owner != Main.myPlayer)
		{
			return;
		}

		Player owner = Main.player[Projectile.owner];
		int dir = owner.Center.X <= Projectile.Center.X ? 1 : -1;
		int worm = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
			ModContent.ProjectileType<CarrionCallEaterProjectile>(), Projectile.damage, Projectile.knockBack,
			Projectile.owner, Projectile.Center.X, Projectile.Center.Y, tagged + 1);
		if (worm >= 0 && worm < Main.maxProjectiles)
		{
			Main.projectile[worm].direction = dir;
			Main.projectile[worm].netUpdate = true;
		}
	}

	private void CreateRumbleDust()
	{
		if (Main.dedServ)
		{
			return;
		}

		float intensity = MathHelper.Clamp(RumbleTimer / RumbleDuration, 0.2f, 1f);
		int count = 1 + (int)(intensity * 2f);
		for (int index = 0; index < count; index++)
		{
			Vector2 origin = Projectile.Center + new Vector2(Main.rand.NextFloat(-48f, 48f), Main.rand.NextFloat(24f, 90f));
			Dust dust = Dust.NewDustPerfect(origin, DustID.Corruption,
				-Vector2.UnitY * Main.rand.NextFloat(1.4f, 4.2f) * intensity, 140,
				new Color(90, 160, 40), Main.rand.NextFloat(0.7f, 1.25f));
			dust.noGravity = true;
		}
	}
}
