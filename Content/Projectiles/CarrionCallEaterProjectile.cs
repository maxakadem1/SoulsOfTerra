using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class CarrionCallEaterProjectile : ModProjectile
{
	public const float HostChompMultiplier = 1.75f;
	public const float FloorBurstMultiplier = 0.8f;

	private const int SegmentCount = 40;
	private const int HeadTravelDuration = 108;
	private const int TailFollowDuration = 72;
	private const float SegmentT = 0.015f;
	private const float SegmentScale = 1.5f;
	private const float RunUp = 560f;
	private const float DropBelow = 980f;
	private const float CrestHeight = 420f;
	private const float CrestAhead = 240f;
	private const float MealT = 0.34f;
	private const float CrestT = 0.58f;
	private const float ChompRadius = 90f;
	private const float HostStayRadius = 100f;
	private const int HeadHitSize = 52;
	private const int BodyHitSize = 36;

	private ref float Age => ref Projectile.localAI[1];
	private ref float HasChomped => ref Projectile.localAI[0];

	private Vector2 Meal => new(Projectile.ai[0], Projectile.ai[1]);
	private int TaggedNpcIndex => (int)Projectile.ai[2] - 1;
	private float PathDir => Projectile.direction >= 0 ? 1f : -1f;

	public override string Texture => $"Terraria/Images/NPC_{NPCID.EaterofWorldsHead}";

	public override void SetStaticDefaults()
	{
		ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2_800;
	}

	public override void SetDefaults()
	{
		Projectile.width = HeadHitSize;
		Projectile.height = HeadHitSize;
		Projectile.friendly = true;
		Projectile.DamageType = DamageClass.Ranged;
		Projectile.penetrate = -1;
		Projectile.timeLeft = HeadTravelDuration + TailFollowDuration + 8;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.usesLocalNPCImmunity = true;
		// One scrape per enemy for the whole body; the chomp is a separate projectile.
		Projectile.localNPCHitCooldown = -1;
		Projectile.netImportant = true;
	}

	public override bool ShouldUpdatePosition() => false;

	public override bool? CanCutTiles() => false;

	public override bool CanHitPlayer(Player target) => false;

	public override void AI()
	{
		Age++;
		float headT = Age / HeadTravelDuration;
		Vector2 head = EvaluatePath(headT);
		Projectile.Center = head;
		Vector2 ahead = EvaluatePath(headT + 0.02f);
		Vector2 tangent = (ahead - head).SafeNormalize(Vector2.UnitY);
		Projectile.rotation = tangent.ToRotation() + MathHelper.PiOver2;
		Projectile.velocity = tangent;

		if (HasChomped == 0f && headT >= MealT)
		{
			TryResolveMeal();
		}

		if (Main.rand.NextBool(3))
		{
			CreateDigDust(head, tangent);
		}

		Lighting.AddLight(head, 0.22f, 0.4f, 0.08f);
		if (Age >= HeadTravelDuration + TailFollowDuration)
		{
			Projectile.Kill();
		}
	}

	public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
	{
		modifiers.HitDirectionOverride = Projectile.velocity.X >= 0f ? 1 : -1;
	}

	public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
	{
		float headT = Age / HeadTravelDuration;
		for (int index = 0; index < SegmentCount; index++)
		{
			float t = headT - index * SegmentT;
			Vector2 center = EvaluatePath(t);
			int size = index == 0 ? HeadHitSize : BodyHitSize;
			var hitbox = new Rectangle((int)(center.X - size * 0.5f), (int)(center.Y - size * 0.5f), size, size);
			if (hitbox.Intersects(targetHitbox))
			{
				return true;
			}
		}

		return false;
	}

	public override bool PreDraw(ref Color lightColor)
	{
		Main.instance.LoadNPC(NPCID.EaterofWorldsHead);
		Main.instance.LoadNPC(NPCID.EaterofWorldsBody);
		Main.instance.LoadNPC(NPCID.EaterofWorldsTail);
		Texture2D head = TextureAssets.Npc[NPCID.EaterofWorldsHead].Value;
		Texture2D body = TextureAssets.Npc[NPCID.EaterofWorldsBody].Value;
		Texture2D tail = TextureAssets.Npc[NPCID.EaterofWorldsTail].Value;
		float headT = Age / HeadTravelDuration;
		for (int index = SegmentCount - 1; index >= 0; index--)
		{
			float t = headT - index * SegmentT;
			Vector2 center = EvaluatePath(t);
			Vector2 tangent = (EvaluatePath(t + 0.012f) - center).SafeNormalize(Vector2.UnitY);
			float rotation = tangent.ToRotation() + MathHelper.PiOver2;
			Texture2D texture = index == 0 ? head : index == SegmentCount - 1 ? tail : body;
			Color color = Lighting.GetColor(center.ToTileCoordinates());
			color = Color.Lerp(color, new Color(70, 210, 120), 0.12f);
			Main.EntitySpriteDraw(texture, center - Main.screenPosition, null, color, rotation,
				texture.Size() * 0.5f, SegmentScale, SpriteEffects.None);
		}

		return false;
	}

	public override void OnKill(int timeLeft)
	{
		SoundEngine.PlaySound(SoundID.WormDig with { Volume = 0.55f, Pitch = -0.55f }, Projectile.Center);
		if (Main.dedServ)
		{
			return;
		}

		for (int index = 0; index < 18; index++)
		{
			Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Corruption, Main.rand.NextVector2Circular(4.5f, 4.5f),
				120, new Color(80, 150, 40), Main.rand.NextFloat(0.8f, 1.4f));
			dust.noGravity = true;
		}
	}

	private void TryResolveMeal()
	{
		HasChomped = 1f;
		Vector2 meal = Meal;
		Vector2 head = EvaluatePath(MealT);
		bool reachedMeal = Vector2.Distance(head, meal) <= ChompRadius;
		KillOwnerBait();
		if (!reachedMeal)
		{
			return;
		}

		int tagged = TaggedNpcIndex;
		bool hostStayed = tagged >= 0 && tagged < Main.maxNPCs && Main.npc[tagged].active
			&& Main.npc[tagged].life > 0
			&& Vector2.Distance(Main.npc[tagged].Center, meal) <= HostStayRadius;
		if (hostStayed)
		{
			SoundEngine.PlaySound(SoundID.NPCHit1 with { Volume = 0.85f, Pitch = -0.2f }, meal);
			if (Projectile.owner != Main.myPlayer)
			{
				return;
			}

			int damage = Math.Max(1, (int)(Projectile.damage * HostChompMultiplier));
			int chomp = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Main.npc[tagged].Center, Vector2.Zero,
				ModContent.ProjectileType<CarrionCallChompProjectile>(), damage, Projectile.knockBack * 1.15f,
				Projectile.owner, tagged + 1);
			if (chomp >= 0 && chomp < Main.maxProjectiles)
			{
				Main.projectile[chomp].originalDamage = damage;
			}

			return;
		}

		if (tagged < 0)
		{
			SoundEngine.PlaySound(SoundID.Item2 with { Volume = 0.7f, Pitch = -0.35f }, meal);
			if (Projectile.owner != Main.myPlayer)
			{
				return;
			}

			int damage = Math.Max(1, (int)(Projectile.damage * FloorBurstMultiplier));
			Projectile.NewProjectile(Projectile.GetSource_FromThis(), meal, Vector2.Zero,
				ModContent.ProjectileType<CarrionCallChompProjectile>(), damage, Projectile.knockBack * 0.7f,
				Projectile.owner, 0f);
		}
	}

	private void KillOwnerBait()
	{
		if (Projectile.owner != Main.myPlayer)
		{
			return;
		}

		Vector2 meal = Meal;
		for (int index = 0; index < Main.maxProjectiles; index++)
		{
			Projectile bait = Main.projectile[index];
			if (!bait.active || bait.owner != Projectile.owner
				|| bait.type != ModContent.ProjectileType<CarrionCallBaitProjectile>())
			{
				continue;
			}

			if (Vector2.DistanceSquared(bait.Center, meal) <= 80f * 80f)
			{
				bait.Kill();
			}
		}
	}

	// Frozen Eater hump: emerge below the player side, pass through the meal, crest, then bury.
	private Vector2 EvaluatePath(float t)
	{
		GetControlPoints(out Vector2 start, out Vector2 meal, out Vector2 crest, out Vector2 end);
		if (t < 0f)
		{
			Vector2 back = (start - meal).SafeNormalize(Vector2.UnitY);
			return start + back * (-t * DropBelow * 0.45f);
		}

		if (t > 1f)
		{
			Vector2 forward = (end - crest).SafeNormalize(Vector2.UnitY);
			return end + forward * ((t - 1f) * DropBelow * 0.45f);
		}

		if (t <= MealT)
		{
			float u = t / MealT;
			return CatmullRom(start + (start - meal), start, meal, crest, u);
		}

		if (t <= CrestT)
		{
			float u = (t - MealT) / (CrestT - MealT);
			return CatmullRom(start, meal, crest, end, u);
		}

		float v = (t - CrestT) / (1f - CrestT);
		return CatmullRom(meal, crest, end, end + (end - crest), v);
	}

	private void GetControlPoints(out Vector2 start, out Vector2 meal, out Vector2 crest, out Vector2 end)
	{
		meal = Meal;
		float dir = PathDir;
		start = meal + new Vector2(-dir * RunUp, DropBelow);
		crest = meal + new Vector2(dir * CrestAhead, -CrestHeight);
		end = meal + new Vector2(dir * RunUp * 1.15f, DropBelow);
	}

	private static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
	{
		t = MathHelper.Clamp(t, 0f, 1f);
		float t2 = t * t;
		float t3 = t2 * t;
		return 0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
			+ (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
	}

	private static void CreateDigDust(Vector2 origin, Vector2 tangent)
	{
		if (Main.dedServ)
		{
			return;
		}

		Dust dust = Dust.NewDustPerfect(origin, DustID.Corruption, -tangent * Main.rand.NextFloat(0.6f, 2.4f)
			+ Main.rand.NextVector2Circular(1.2f, 1.2f), 150, new Color(85, 155, 35), Main.rand.NextFloat(0.65f, 1.1f));
		dust.noGravity = true;
	}
}
