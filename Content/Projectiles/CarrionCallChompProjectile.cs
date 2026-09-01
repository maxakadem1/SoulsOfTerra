using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Projectiles;

public class CarrionCallChompProjectile : ModProjectile
{
	private const int FloorSize = 160;
	private const int HostSize = 36;
	private const int DamageTicks = 4;

	private int TaggedNpcIndex => (int)Projectile.ai[0] - 1;
	private bool IsFloorBurst => Projectile.ai[0] <= 0f;

	public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.PurificationPowder}";

	public override void SetDefaults()
	{
		Projectile.width = FloorSize;
		Projectile.height = FloorSize;
		Projectile.friendly = true;
		Projectile.DamageType = DamageClass.Ranged;
		Projectile.penetrate = -1;
		Projectile.timeLeft = 12;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = -1;
	}

	public override void OnSpawn(IEntitySource source)
	{
		int size = IsFloorBurst ? FloorSize : HostSize;
		Projectile.position = Projectile.Center - new Vector2(size * 0.5f);
		Projectile.width = size;
		Projectile.height = size;
	}

	public override bool ShouldUpdatePosition() => false;

	public override bool PreDraw(ref Color lightColor) => false;

	public override bool CanHitPlayer(Player target) => false;

	public override bool? CanHitNPC(NPC target)
	{
		if (Projectile.localAI[0] > DamageTicks)
		{
			return false;
		}

		if (IsFloorBurst)
		{
			return null;
		}

		return target.whoAmI == TaggedNpcIndex ? null : false;
	}

	public override void AI()
	{
		if (!IsFloorBurst)
		{
			int npcIndex = TaggedNpcIndex;
			if (npcIndex < 0 || npcIndex >= Main.maxNPCs || !Main.npc[npcIndex].active)
			{
				Projectile.Kill();
				return;
			}

			Projectile.Center = Main.npc[npcIndex].Center;
		}

		if (Projectile.localAI[0] == 0f)
		{
			CreateBurstDust();
		}

		Projectile.localAI[0]++;
		Lighting.AddLight(Projectile.Center, 0.28f, 0.45f, 0.07f);
	}

	private void CreateBurstDust()
	{
		if (Main.dedServ)
		{
			return;
		}

		int count = IsFloorBurst ? 22 : 14;
		float spread = IsFloorBurst ? 5.5f : 3.2f;
		for (int index = 0; index < count; index++)
		{
			Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Corruption,
				Main.rand.NextVector2Circular(spread, spread), 90, new Color(95, 170, 40),
				Main.rand.NextFloat(0.8f, 1.45f));
			dust.noGravity = true;
		}
	}
}
