using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Content.Items.BossBags;
using SoulsOfTerra.Content.Projectiles;
using SoulsOfTerra.Systems;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Bosses.SealedCongregation;

public class SealedCongregationBoss : ModNPC
{
	private enum AttackState
	{
		Awakening,
		CrossedSentence,
		ProcessionalArc,
		HollowBenediction,
		ReleaseTransition,
		ChoirOfJudgment,
		FinalConfession,
		CollapseOfTheMany,
		Retreat
	}

	private const int SealCount = 4;
	private const int RetreatDuration = 180;
	private const int SealedDashCycle = 60;
	private const int SealedDashStart = 26;
	private const int SealedDashEnd = 42;
	private const int ChoirChargeDuration = 90;
	private const int ChoirAimLockTime = 45;
	private const int ChoirBeamDuration = 150;
	private const int ChoirRecoveryDuration = 36;
	private int retreatTimer;
	private int initialSealLifeTotal;
	private int choirCastCount;

	private AttackState State
	{
		get => (AttackState)(int)NPC.ai[0];
		set => NPC.ai[0] = (float)value;
	}

	private ref float StateTimer => ref NPC.ai[1];
	private ref float AttackCycle => ref NPC.ai[2];
	public int CombinedLifeMax => NPC.lifeMax + initialSealLifeTotal;
	private static int SoulBoltDamage => DifficultyDamage(20, 26, 32);
	private static int LanceDamage => DifficultyDamage(28, 34, 40);
	private static int BenedictionDamage => DifficultyDamage(28, 34, 40);
	private static int ConfessionDamage => DifficultyDamage(32, 40, 48);
	private static int ChoirDamage => DifficultyDamage(38, 46, 54);
	public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.LostSoulHostile}";

	public override void SetStaticDefaults()
	{
		Main.npcFrameCount[Type] = 1;
		NPCID.Sets.TrailCacheLength[Type] = 14;
		NPCID.Sets.TrailingMode[Type] = 1;
		NPCID.Sets.MPAllowedEnemies[Type] = true;
	}

	public override void SetDefaults()
	{
		NPC.width = 104;
		NPC.height = 104;
		NPC.damage = 30;
		NPC.defense = 12;
		// Phase two is intense and mobile, so it owns the smaller share of the encounter's health budget.
		NPC.lifeMax = 4_200;
		NPC.knockBackResist = 0f;
		NPC.value = Item.buyPrice(gold: 12);
		NPC.npcSlots = 10f;
		NPC.boss = true;
		NPC.noGravity = true;
		NPC.noTileCollide = true;
		NPC.netAlways = true;
		NPC.HitSound = SoundID.NPCHit54;
		NPC.DeathSound = SoundID.NPCDeath52;
		NPC.aiStyle = -1;
		NPC.BossBar = ModContent.GetInstance<SealedCongregationBossBar>();
		Music = MusicID.Boss3;
	}

	public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
	{
		NPC.lifeMax = (int)(NPC.lifeMax * 0.8f * balance);
		NPC.damage = (int)(NPC.damage * 0.9f);
	}

	public override void OnSpawn(IEntitySource source)
	{
		State = AttackState.Awakening;
		NPC.dontTakeDamage = true;
		if (Main.netMode != NetmodeID.MultiplayerClient)
		{
			SpawnSeals();
		}
	}

	public override void AI()
	{
		NPC.TargetClosest(false);
		Player target = Main.player[NPC.target];
		UpdateArenaRetreat(target);
		if (State == AttackState.Retreat)
		{
			RunRetreat();
			return;
		}

		int livingSeals = CountLivingSeals();
		NPC.dontTakeDamage = livingSeals > 0 || State == AttackState.ReleaseTransition || State == AttackState.Awakening;
		if (livingSeals == 0 && State is >= AttackState.CrossedSentence and <= AttackState.HollowBenediction)
		{
			BeginState(AttackState.ReleaseTransition);
		}

		StateTimer++;
		switch (State)
		{
			case AttackState.Awakening:
				RunAwakening();
				break;
			case AttackState.CrossedSentence:
				RunCrossedSentence(target);
				break;
			case AttackState.ProcessionalArc:
				RunProcessionalArc(target);
				break;
			case AttackState.HollowBenediction:
				RunHollowBenediction(target);
				break;
			case AttackState.ReleaseTransition:
				RunReleaseTransition();
				break;
			case AttackState.ChoirOfJudgment:
				RunChoirOfJudgment(target);
				break;
			case AttackState.FinalConfession:
				RunFinalConfession(target);
				break;
			case AttackState.CollapseOfTheMany:
				RunCollapseOfTheMany();
				break;
		}

		Color soulColor = new(78, 235, 215);
		Lighting.AddLight(NPC.Center, soulColor.ToVector3() * 0.72f);
	}

	public Vector2 GetSealDestination(int sealNpcIndex)
	{
		Span<int> seals = stackalloc int[SealCount];
		int count = CollectLivingSeals(seals);
		int rank = 0;
		for (int i = 0; i < count; i++)
		{
			if (seals[i] == sealNpcIndex)
			{
				rank = i;
				break;
			}
		}

		float angle = -MathHelper.PiOver2 + MathHelper.TwoPi * rank / Math.Max(1, count);
		float radius = 105f;
		if (State == AttackState.ProcessionalArc)
		{
			angle += StateTimer * 0.027f;
			radius = 118f + 10f * MathF.Sin(StateTimer * 0.06f);
		}
		else if (State == AttackState.CrossedSentence)
		{
			angle += MathHelper.PiOver4 * SmoothStep(0f, 1f, StateTimer / 45f);
			radius = 122f;
		}
		else if (State == AttackState.HollowBenediction)
		{
			// The seals hold a stable cardinal formation that matches the wave's four safe gaps.
			int slot = (int)Main.npc[sealNpcIndex].ai[1];
			angle = -MathHelper.PiOver2 + MathHelper.TwoPi * slot / SealCount;
			radius = 150f;
		}
		else
		{
			angle += Main.GameUpdateCount * 0.004f;
		}

		return NPC.Center + angle.ToRotationVector2() * radius;
	}

	public override bool CanHitPlayer(Player target, ref int cooldownSlot)
	{
		// Phase two now deals damage through explicit projectiles rather than boss contact.
		return false;
	}

	public override void ModifyNPCLoot(NPCLoot npcLoot)
	{
		npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<SealedCongregationBag>()));

		// Classic receives the potion reward directly; higher difficulties receive it inside the bag.
		LeadingConditionRule classicLoot = new(new Conditions.NotExpert());
		classicLoot.OnSuccess(ItemDropRule.Common(ItemID.HealingPotion, 1, 5, 8));
		npcLoot.Add(classicLoot);
	}

	public override void OnKill()
	{
		BuriedCourtSystem.MarkBossDefeated();
		BuriedCourtSystem.BroadcastCourtMessage("Mods.SoulsOfTerra.Dialogue.Court.CongregationDeath",
			new Color(142, 220, 207));
		ClearEncounterProjectiles();
	}

	public override bool CheckActive() => false;

	public override void SendExtraAI(BinaryWriter writer)
	{
		writer.Write(initialSealLifeTotal);
		writer.Write((short)retreatTimer);
		writer.Write((short)choirCastCount);
	}

	public override void ReceiveExtraAI(BinaryReader reader)
	{
		initialSealLifeTotal = reader.ReadInt32();
		retreatTimer = reader.ReadInt16();
		choirCastCount = reader.ReadInt16();
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		DrawChains(spriteBatch, screenPos);
		DrawCoreAfterimages(spriteBatch, screenPos);
		DrawCore(spriteBatch, screenPos);
		return false;
	}

	private void SpawnSeals()
	{
		initialSealLifeTotal = 0;
		for (int slot = 0; slot < SealCount; slot++)
		{
			int index = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y,
				ModContent.NPCType<SealedCongregationSeal>(), ai0: NPC.whoAmI, ai1: slot);
			if (index >= 0 && index < Main.maxNPCs)
			{
				initialSealLifeTotal += Main.npc[index].lifeMax;
				Main.npc[index].netUpdate = true;
			}
		}

		NPC.netUpdate = true;
	}

	private void RunAwakening()
	{
		MoveToward(CourtHoverPosition(), 0.04f, 8f);
		NPC.velocity *= 0.88f;
		if (StateTimer == 1f)
		{
			SoundEngine.PlaySound(SoundID.Item117, NPC.Center);
		}

		if (StateTimer >= 120f)
		{
			BeginState(AttackState.CrossedSentence);
		}
	}

	private void RunCrossedSentence(Player target)
	{
		RunSealedDashMovement();
		if (Main.netMode != NetmodeID.MultiplayerClient && (StateTimer == 45f || StateTimer == 112f))
		{
			float rotation = StateTimer < 100f ? MathHelper.PiOver4 : 0f;
			SpawnCrossedLances(target.Center, rotation);
		}

		if (StateTimer >= 180f)
		{
			AdvancePhaseOne();
		}
	}

	private void RunProcessionalArc(Player target)
	{
		RunSealedDashMovement();
		if (Main.netMode != NetmodeID.MultiplayerClient && StateTimer is >= 48f and <= 165f && (int)StateTimer % 24 == 0)
		{
			SpawnSealBolts(target);
		}

		if (StateTimer >= 210f)
		{
			AdvancePhaseOne();
		}
	}

	private void RunHollowBenediction(Player target)
	{
		if (StateTimer < 34f)
		{
			// Stage the ritual close enough for its implosion and seal streams to remain visible.
			MoveToward(target.Center + new Vector2(0f, -190f), 0.06f, 12f);
		}
		else
		{
			NPC.velocity *= 0.78f;
		}

		if (Main.netMode != NetmodeID.MultiplayerClient && StateTimer == 38f)
		{
			Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero,
				ModContent.ProjectileType<CongregationBenedictionWaveProjectile>(), BenedictionDamage, 0f,
				Main.myPlayer, NPC.whoAmI, -MathHelper.PiOver2);
		}

		// The projectile completes before this recovery hands control to the next attack.
		if (StateTimer >= 280f)
		{
			AdvancePhaseOne();
		}
	}

	private void RunReleaseTransition()
	{
		NPC.velocity *= 0.86f;
		if (StateTimer == 1f)
		{
			SoundEngine.PlaySound(SoundID.NPCDeath6 with { Pitch = -0.3f }, NPC.Center);
		}

		if (!Main.dedServ && StateTimer % 4f == 0f)
		{
			Dust dust = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(70f, 70f), DustID.DungeonSpirit,
				(NPC.Center - Main.rand.NextVector2Circular(12f, 12f)).SafeNormalize(Vector2.Zero), 100,
				new Color(85, 245, 220), 1.2f);
			dust.noGravity = true;
		}

		if (StateTimer >= 110f)
		{
			NPC.dontTakeDamage = false;
			AttackCycle = 0f;
			BeginState(AttackState.ChoirOfJudgment);
		}
	}

	private void RunChoirOfJudgment(Player target)
	{
		if (StateTimer < ChoirAimLockTime)
		{
			// Stage directly above the target before committing to an honest warning line.
			MoveToward(target.Center + new Vector2(0f, -300f), 0.065f, 12f);
		}
		else
		{
			NPC.velocity *= 0.72f;
		}

		if (StateTimer == 1f)
		{
			SoundEngine.PlaySound(SoundID.Item117 with { Pitch = -0.55f, Volume = 0.72f }, NPC.Center);
		}

		if (!Main.dedServ && StateTimer < ChoirChargeDuration && (int)StateTimer % 3 == 0)
		{
			SpawnChoirChargeDust();
		}

		if (Main.netMode != NetmodeID.MultiplayerClient && StateTimer == ChoirAimLockTime)
		{
			Vector2 predictedTarget = target.Center + target.velocity * 12f;
			Vector2 direction = (predictedTarget - NPC.Center).SafeNormalize(Vector2.UnitY);
			float lateralMovement = direction.X * target.velocity.Y - direction.Y * target.velocity.X;
			int sweepDirection = MathF.Abs(lateralMovement) > 0.35f
				? Math.Sign(lateralMovement)
				: (choirCastCount % 2 == 0 ? 1 : -1);
			choirCastCount++;
			Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, direction,
				ModContent.ProjectileType<CongregationJudgmentBeamProjectile>(), ChoirDamage, 0f,
				Main.myPlayer, NPC.whoAmI, sweepDirection);
			NPC.netUpdate = true;
		}

		if (StateTimer >= ChoirChargeDuration + ChoirBeamDuration + ChoirRecoveryDuration)
		{
			AdvancePhaseTwo();
		}
	}

	private void RunSealedDashMovement()
	{
		int dashTimer = (int)StateTimer % SealedDashCycle;
		int dashIndex = (int)StateTimer / SealedDashCycle;
		if (dashTimer < SealedDashStart - 6)
		{
			// Phase one rests between crisp relocations instead of constantly drifting.
			NPC.velocity *= 0.84f;
		}
		else if (dashTimer < SealedDashStart)
		{
			NPC.velocity *= 0.68f;
		}
		else if (dashTimer == SealedDashStart)
		{
			int sequence = (int)AttackCycle + dashIndex;
			float side = sequence % 2 == 0 ? 1f : -1f;
			float height = sequence % 3 == 2 ? 70f : -35f;
			Vector2 destination = CourtHoverPosition() + new Vector2(side * 225f, height);
			NPC.velocity = (destination - NPC.Center).SafeNormalize(Vector2.UnitX) * 13.5f;
			NPC.netUpdate = true;
			SoundEngine.PlaySound(SoundID.Item74 with { Pitch = 0.18f, Volume = 0.72f }, NPC.Center);
		}
		else if (dashTimer < SealedDashEnd)
		{
			NPC.velocity *= 0.985f;
		}
		else
		{
			NPC.velocity *= 0.78f;
		}
	}

	private void SpawnChoirChargeDust()
	{
		float progress = MathHelper.Clamp(StateTimer / ChoirChargeDuration, 0f, 1f);
		Vector2 offset = Main.rand.NextVector2CircularEdge(110f, 78f) * (1f - progress * 0.62f);
		Dust dust = Dust.NewDustPerfect(NPC.Center + offset, DustID.DungeonSpirit, -offset * 0.055f, 90,
			new Color(91, 236, 215), Main.rand.NextFloat(0.8f, 1.2f));
		dust.noGravity = true;
	}

	private void RunFinalConfession(Player target)
	{
		MoveToward(CourtHoverPosition() + new Vector2(0f, -80f), 0.035f, 8f);
		if (Main.netMode != NetmodeID.MultiplayerClient && StateTimer is >= 38f and <= 146f && (int)StateTimer % 22 == 16)
		{
			Vector2 position = target.Center - target.velocity * 5f + Main.rand.NextVector2Circular(36f, 22f);
			Projectile.NewProjectile(NPC.GetSource_FromAI(), position, Vector2.Zero,
				ModContent.ProjectileType<CongregationConfessionProjectile>(), ConfessionDamage, 0f, Main.myPlayer);
		}

		if (StateTimer >= 205f)
		{
			AdvancePhaseTwo();
		}
	}

	private void RunCollapseOfTheMany()
	{
		MoveToward(CourtHoverPosition(), 0.055f, 10f);
		NPC.velocity *= 0.9f;
		if (Main.netMode != NetmodeID.MultiplayerClient && (StateTimer == 105f || StateTimer == 136f))
		{
			int projectileCount = Main.masterMode ? 14 : Main.expertMode ? 12 : 10;
			float offset = StateTimer == 105f ? 0f : MathHelper.Pi / projectileCount;
			for (int index = 0; index < projectileCount; index++)
			{
				Vector2 velocity = (offset + MathHelper.TwoPi * index / projectileCount).ToRotationVector2() * 7.5f;
				SpawnSoulBolt(NPC.Center, velocity, index % 2 == 0 ? 0.45f : -0.45f);
			}
		}

		if (StateTimer >= 185f)
		{
			AdvancePhaseTwo();
		}
	}

	private void UpdateArenaRetreat(Player target)
	{
		bool validTarget = target.active && !target.dead && BuriedCourtSystem.IsInsideCombatArea(target.Center, 12);
		retreatTimer = validTarget ? 0 : retreatTimer + 1;
		if (retreatTimer >= RetreatDuration && State != AttackState.Retreat)
		{
			BeginState(AttackState.Retreat);
		}
	}

	private void RunRetreat()
	{
		NPC.dontTakeDamage = true;
		MoveToward(BuriedCourtSystem.DaisTopLeft.ToWorldCoordinates(24f, -30f), 0.08f, 14f);
		if (StateTimer++ < 120f)
		{
			return;
		}

		DeactivateSeals();
		ClearEncounterProjectiles();
		NPC.active = false;
		NPC.netUpdate = true;
	}

	private void AdvancePhaseOne()
	{
		AttackCycle = (AttackCycle + 1f) % 3f;
		BeginState(AttackCycle switch
		{
			0f => AttackState.CrossedSentence,
			1f => AttackState.ProcessionalArc,
			_ => AttackState.HollowBenediction
		});
	}

	private void AdvancePhaseTwo()
	{
		AttackCycle = (AttackCycle + 1f) % 3f;
		BeginState(AttackCycle switch
		{
			0f => AttackState.ChoirOfJudgment,
			1f => AttackState.FinalConfession,
			_ => AttackState.CollapseOfTheMany
		});
	}

	private void BeginState(AttackState nextState)
	{
		State = nextState;
		StateTimer = 0f;
		NPC.netUpdate = true;
	}

	private void SpawnCrossedLances(Vector2 targetPosition, float rotation)
	{
		for (int axis = 0; axis < 2; axis++)
		{
			Vector2 direction = (rotation + axis * MathHelper.PiOver2).ToRotationVector2();
			Vector2 start = targetPosition - direction * 900f;
			Projectile.NewProjectile(NPC.GetSource_FromAI(), start, direction * 23f,
				ModContent.ProjectileType<CongregationLanceProjectile>(), LanceDamage, 0f, Main.myPlayer);
		}
	}

	private void SpawnSealBolts(Player target)
	{
		foreach (NPC seal in Main.ActiveNPCs)
		{
			if (seal.type != ModContent.NPCType<SealedCongregationSeal>() || (int)seal.ai[0] != NPC.whoAmI)
			{
				continue;
			}

			Vector2 velocity = (target.Center - seal.Center).SafeNormalize(Vector2.UnitY) * 7.25f;
			float curve = ((int)seal.ai[1] % 2 == 0 ? 1f : -1f) * 0.55f;
			SpawnSoulBolt(seal.Center, velocity, curve);
		}
	}

	private void SpawnSoulBolt(Vector2 position, Vector2 velocity, float curve)
	{
		Projectile.NewProjectile(NPC.GetSource_FromAI(), position, velocity,
			ModContent.ProjectileType<CongregationSoulBoltProjectile>(), SoulBoltDamage, 0f, Main.myPlayer, curve);
	}

	private static int DifficultyDamage(int classic, int expert, int master)
	{
		// Hostile projectile damage is selected explicitly to keep difficulty scaling predictable.
		return Main.masterMode ? master : Main.expertMode ? expert : classic;
	}

	private int CountLivingSeals()
	{
		Span<int> seals = stackalloc int[SealCount];
		return CollectLivingSeals(seals);
	}

	private int CollectLivingSeals(Span<int> result)
	{
		int count = 0;
		foreach (NPC npc in Main.ActiveNPCs)
		{
			if (npc.type == ModContent.NPCType<SealedCongregationSeal>() && (int)npc.ai[0] == NPC.whoAmI)
			{
				if (count < result.Length)
				{
					result[count] = npc.whoAmI;
				}
				count++;
			}
		}

		return Math.Min(count, result.Length);
	}

	private void MoveToward(Vector2 destination, float inertia, float maximumSpeed)
	{
		Vector2 desiredVelocity = (destination - NPC.Center) * inertia;
		if (desiredVelocity.Length() > maximumSpeed)
		{
			desiredVelocity = desiredVelocity.SafeNormalize(Vector2.Zero) * maximumSpeed;
		}

		NPC.velocity = Vector2.Lerp(NPC.velocity, desiredVelocity, 0.1f);
	}

	private static Vector2 CourtHoverPosition()
	{
		return new Vector2(BuriedCourtSystem.CombatBounds.Center.X * 16f,
			(BuriedCourtSystem.CombatBounds.Top + 20) * 16f);
	}

	private void DrawChains(SpriteBatch spriteBatch, Vector2 screenPos)
	{
		Texture2D pixel = TextureAssets.MagicPixel.Value;
		Vector2 pixelOrigin = pixel.Size() * 0.5f;
		Vector2 pixelSize = pixel.Size();
		// Normalize scale so link dimensions stay measured in world pixels.
		foreach (NPC seal in Main.ActiveNPCs)
		{
			if (seal.type != ModContent.NPCType<SealedCongregationSeal>() || (int)seal.ai[0] != NPC.whoAmI)
			{
				continue;
			}

			Vector2 start = NPC.Center;
			Vector2 end = seal.Center;
			Vector2 bend = Vector2.Lerp(start, end, 0.5f) + new Vector2(0f, 12f + Vector2.Distance(start, end) * 0.05f);
			Vector2 previous = start;
			for (int segment = 1; segment <= 14; segment++)
			{
				float progress = segment / 14f;
				Vector2 point = QuadraticBezier(start, bend, end, progress);
				Vector2 difference = point - previous;
				float rotation = difference.ToRotation();
				Vector2 center = Vector2.Lerp(previous, point, 0.5f) - screenPos;
				spriteBatch.Draw(pixel, center, null, new Color(13, 20, 25, 230), rotation,
					pixelOrigin, new Vector2(difference.Length() / pixelSize.X, 4f / pixelSize.Y), SpriteEffects.None, 0f);
				spriteBatch.Draw(pixel, center, null, new Color(60, 194, 179, 165), rotation,
					pixelOrigin, new Vector2(difference.Length() * 0.78f / pixelSize.X, 1.25f / pixelSize.Y), SpriteEffects.None, 0f);
				previous = point;
			}
		}
	}

	private void DrawCoreAfterimages(SpriteBatch spriteBatch, Vector2 screenPos)
	{
		if (!IsDashActive())
		{
			return;
		}

		Texture2D glow = SoulOrbProjectile.GetGlowTexture();
		Vector2 origin = glow.Size() * 0.5f;
		for (int index = NPC.oldPos.Length - 1; index >= 2; index -= 2)
		{
			if (NPC.oldPos[index] == Vector2.Zero)
			{
				continue;
			}

			float strength = 1f - index / (float)NPC.oldPos.Length;
			Vector2 position = NPC.oldPos[index] + NPC.Size * 0.5f - screenPos;
			spriteBatch.Draw(glow, position, null, new Color(70, 225, 207, 0) * (strength * 0.22f),
				0f, origin, 1.5f * strength, SpriteEffects.None, 0f);
		}
	}

	private bool IsDashActive()
	{
		if (State is AttackState.CrossedSentence or AttackState.ProcessionalArc)
		{
			int dashTimer = (int)StateTimer % SealedDashCycle;
			return dashTimer is >= SealedDashStart and < SealedDashEnd;
		}

		return false;
	}

	private void DrawCore(SpriteBatch spriteBatch, Vector2 screenPos)
	{
		Texture2D glow = SoulOrbProjectile.GetGlowTexture();
		Texture2D ring = SoulOrbProjectile.GetRingTexture();
		Vector2 origin = glow.Size() * 0.5f;
		Vector2 center = NPC.Center - screenPos;
		float pulse = 1f + 0.055f * MathF.Sin(Main.GlobalTimeWrappedHourly * 3.4f);
		float collapse = State == AttackState.CollapseOfTheMany
			? MathHelper.Lerp(1f, 0.26f, SmoothStep(0f, 1f, StateTimer / 100f))
			: 1f;
		float releaseScale = State == AttackState.ReleaseTransition
			? 1f + 0.12f * MathF.Sin(StateTimer * 0.22f)
			: 1f;
		float choirChargeScale = State == AttackState.ChoirOfJudgment && StateTimer < ChoirChargeDuration
			? MathHelper.Lerp(1f, 1.2f, SmoothStep(0f, 1f, StateTimer / ChoirChargeDuration))
			: 1f;
		float scale = pulse * collapse * releaseScale * choirChargeScale;

		// Layered low-alpha discs retain a transparent interior beneath the bright membrane.
		spriteBatch.Draw(glow, center, null, new Color(34, 142, 143, 52), 0f, origin, 2.15f * scale, SpriteEffects.None, 0f);
		spriteBatch.Draw(ring, center, null, new Color(102, 255, 226, 225), 0f, origin, 1.88f * scale, SpriteEffects.None, 0f);
		spriteBatch.Draw(ring, center, null, new Color(225, 255, 248, 120), 0f, origin, 1.72f * scale, SpriteEffects.None, 0f);

		float time = Main.GlobalTimeWrappedHourly * (State >= AttackState.ChoirOfJudgment ? 3.9f : 2.1f);
		for (int wisp = 0; wisp < 7; wisp++)
		{
			float angle = time * (wisp % 2 == 0 ? 1f : -0.72f) + MathHelper.TwoPi * wisp / 7f;
			float radius = (20f + 17f * MathF.Sin(time * 0.55f + wisp)) * scale;
			Vector2 offset = new Vector2(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius * 0.62f);
			float wispScale = (0.18f + 0.06f * MathF.Sin(angle * 1.7f)) * scale;
			spriteBatch.Draw(glow, center + offset, null, new Color(175, 255, 238, 0) * 0.7f,
				0f, origin, wispScale, SpriteEffects.None, 0f);
		}

		// Three dim impressions suggest a congregation without literal face sprites.
		for (int face = 0; face < 3; face++)
		{
			float angle = -1.9f + face * 1.9f + MathF.Sin(time * 0.4f + face) * 0.18f;
			Vector2 offset = angle.ToRotationVector2() * 25f * scale;
			spriteBatch.Draw(glow, center + offset, null, new Color(220, 255, 246, 0) * 0.22f,
				0f, origin, new Vector2(0.11f, 0.18f) * scale, SpriteEffects.None, 0f);
		}

		spriteBatch.Draw(glow, center, null, new Color(232, 255, 249, 0), 0f, origin,
			new Vector2(0.16f, 0.32f) * scale, SpriteEffects.None, 0f);
	}

	private void DeactivateSeals()
	{
		foreach (NPC npc in Main.ActiveNPCs)
		{
			if (npc.type == ModContent.NPCType<SealedCongregationSeal>() && (int)npc.ai[0] == NPC.whoAmI)
			{
				npc.active = false;
				npc.netUpdate = true;
			}
		}
	}

	private static void ClearEncounterProjectiles()
	{
		int[] projectileTypes =
		{
			ModContent.ProjectileType<CongregationSoulBoltProjectile>(),
			ModContent.ProjectileType<CongregationLanceProjectile>(),
			ModContent.ProjectileType<CongregationBenedictionWaveProjectile>(),
			ModContent.ProjectileType<CongregationConfessionProjectile>(),
			ModContent.ProjectileType<CongregationJudgmentBeamProjectile>()
		};
		foreach (Projectile projectile in Main.ActiveProjectiles)
		{
			if (Array.IndexOf(projectileTypes, projectile.type) >= 0)
			{
				projectile.Kill();
			}
		}
	}

	private static float SmoothStep(float from, float to, float value)
	{
		return MathHelper.SmoothStep(from, to, MathHelper.Clamp(value, 0f, 1f));
	}

	private static Vector2 QuadraticBezier(Vector2 start, Vector2 control, Vector2 end, float progress)
	{
		float inverse = 1f - progress;
		return inverse * inverse * start + 2f * inverse * progress * control + progress * progress * end;
	}
}
