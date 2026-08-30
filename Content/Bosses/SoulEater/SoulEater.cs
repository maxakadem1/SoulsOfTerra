using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Systems;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Bosses.SoulEater;

public class SoulEater : ModNPC
{
	private enum MovementState
	{
		Scuttle,
		LeapWindup,
		Leaping
	}

	private const int LeapWindupDuration = 24;
	private const int LeapCooldownDuration = 90;
	private const float MaximumRunSpeed = 3.7f;
	private const float RunAcceleration = 0.16f;
	private const float BodyScale = 2f;
	private const float LegScale = 2.5f;
	private const float UpperLegLength = 42.6f * LegScale;
	private const float LowerLegLength = 28f * LegScale;
	private const string UpperLegTexturePath = "SoulsOfTerra/Content/Bosses/SoulEater/SoulEater_leg_upper";
	private const string LowerLegTexturePath = "SoulsOfTerra/Content/Bosses/SoulEater/SoulEater_leg_down";
	private static readonly Vector2 UpperLegStart = new(2f, 3f);
	private static readonly Vector2 UpperLegEnd = new(29f, 36f);
	private static readonly Vector2 LowerLegStart = new(5f, 1f);
	private static readonly Vector2 LowerLegEnd = new(5f, 29f);
	private float visualStepPhase;

	private MovementState State
	{
		get => (MovementState)(int)NPC.ai[0];
		set => NPC.ai[0] = (float)value;
	}

	private ref float StateTimer => ref NPC.ai[1];
	private ref float LeapCooldown => ref NPC.ai[2];

	public override string Texture => "SoulsOfTerra/Content/Bosses/SoulEater/SoulEater_body";

	public override void SetStaticDefaults()
	{
		Main.npcFrameCount[Type] = 1;
		NPCID.Sets.MPAllowedEnemies[Type] = true;
	}

	public override void SetDefaults()
	{
		NPC.width = 92;
		NPC.height = 96;
		NPC.damage = 24;
		NPC.defense = 10;
		NPC.lifeMax = 2_400;
		NPC.knockBackResist = 0f;
		NPC.value = Item.buyPrice(gold: 2);
		NPC.npcSlots = 10f;
		NPC.boss = true;
		NPC.netAlways = true;
		NPC.aiStyle = -1;
		NPC.HitSound = SoundID.NPCHit1;
		NPC.DeathSound = SoundID.NPCDeath12;
		Music = MusicID.Boss1;
	}

	public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
	{
		NPC.lifeMax = (int)(NPC.lifeMax * 0.8f * balance);
		NPC.damage = (int)(NPC.damage * 0.9f);
	}

	public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
	{
		State = MovementState.Scuttle;
		LeapCooldown = 45f;
	}

	public override void AI()
	{
		NPC.TargetClosest(false);
		Player target = Main.player[NPC.target];
		if (!target.active || target.dead || Main.dayTime)
		{
			RunDespawn();
			return;
		}

		NPC.timeLeft = Math.Max(NPC.timeLeft, 300);
		if (LeapCooldown > 0f)
		{
			LeapCooldown--;
		}
		StateTimer++;
		switch (State)
		{
			case MovementState.Scuttle:
				RunScuttle(target);
				break;
			case MovementState.LeapWindup:
				RunLeapWindup(target);
				break;
			case MovementState.Leaping:
				RunLeap(target);
				break;
		}

		// The visual phase is cosmetic and can remain local to each client.
		if (!Main.dedServ)
		{
			float pace = MathF.Abs(NPC.velocity.X) * 0.075f;
			visualStepPhase += MathF.Max(0.025f, pace);
		}

		Lighting.AddLight(NPC.Center, new Vector3(0.05f, 0.26f, 0.24f));
		CreateCoreMote();
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		Vector2 bodyCenter = NPC.Center + new Vector2(0f, GetBodyBob());
		DrawLegs(spriteBatch, screenPos, drawColor, bodyCenter);

		Texture2D bodyTexture = TextureAssets.Npc[Type].Value;
		Main.EntitySpriteDraw(bodyTexture, bodyCenter - screenPos, null, NPC.GetAlpha(drawColor), 0f,
			bodyTexture.Size() * 0.5f, BodyScale, SpriteEffects.None);
		DrawCoreEffect(spriteBatch, screenPos, bodyTexture, bodyCenter);
		return false;
	}

	private void CreateCoreMote()
	{
		if (Main.dedServ || !Main.rand.NextBool(12))
		{
			return;
		}

		Vector2 coreCenter = NPC.Center + new Vector2(0f, 14f);
		Vector2 offset = Main.rand.NextVector2Circular(13f, 16f);
		Vector2 velocity = offset.SafeNormalize(-Vector2.UnitY) * Main.rand.NextFloat(0.25f, 0.75f) - Vector2.UnitY * 0.2f;
		Dust mote = Dust.NewDustPerfect(coreCenter + offset, DustID.DungeonSpirit, velocity, 155,
			new Color(72, 235, 207), Main.rand.NextFloat(0.34f, 0.52f));
		mote.noGravity = true;
		mote.fadeIn = 0.55f;
	}

	private static void DrawCoreEffect(SpriteBatch spriteBatch, Vector2 screenPos, Texture2D bodyTexture,
		Vector2 bodyCenter)
	{
		Effect effect = SoulShaderSystem.GetSoulEaterCoreEffect();
		if (effect is null)
		{
			return;
		}

		float pulse = 0.88f + MathF.Sin(Main.GlobalTimeWrappedHourly * 2.6f) * 0.12f;
		SoulShaderSystem.ConfigureSoulEaterCore(bodyTexture, Main.GlobalTimeWrappedHourly, pulse);
		Vector2 drawPosition = bodyCenter - screenPos;
		Vector2 origin = bodyTexture.Size() * 0.5f;

		spriteBatch.End();
		spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointClamp,
			DepthStencilState.None, Main.Rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);

		// Offset passes form a restrained silhouette bloom around the animated core.
		for (int index = 0; index < 8; index++)
		{
			Vector2 offset = (MathHelper.TwoPi * index / 8f).ToRotationVector2() * 2.2f;
			Main.EntitySpriteDraw(bodyTexture, drawPosition + offset, null, new Color(255, 255, 255, 34),
				0f, origin, BodyScale, SpriteEffects.None);
		}

		Main.EntitySpriteDraw(bodyTexture, drawPosition, null, new Color(255, 255, 255, 178),
			0f, origin, BodyScale, SpriteEffects.None);

		spriteBatch.End();
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
			DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
	}

	private void RunScuttle(Player target)
	{
		int direction = Math.Sign(target.Center.X - NPC.Center.X);
		if (direction == 0)
		{
			direction = NPC.direction;
		}

		NPC.direction = direction;
		NPC.spriteDirection = direction;
		NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X + direction * RunAcceleration,
			-MaximumRunSpeed, MaximumRunSpeed);

		bool grounded = IsGrounded();
		bool targetIsHigh = target.Bottom.Y < NPC.Top.Y - 64f;
		if (grounded && LeapCooldown <= 0f && (NPC.collideX || targetIsHigh))
		{
			BeginState(MovementState.LeapWindup);
		}
	}

	private void RunLeapWindup(Player target)
	{
		NPC.velocity.X *= 0.78f;
		if (StateTimer == 1f)
		{
			SoundEngine.PlaySound(SoundID.NPCHit13 with { Pitch = -0.35f, Volume = 0.7f }, NPC.Center);
		}

		if (StateTimer < LeapWindupDuration)
		{
			return;
		}

		int direction = Math.Sign(target.Center.X - NPC.Center.X);
		NPC.velocity = new Vector2(direction * 4.2f, -8.5f);
		LeapCooldown = LeapCooldownDuration;
		BeginState(MovementState.Leaping);
	}

	private void RunLeap(Player target)
	{
		int direction = Math.Sign(target.Center.X - NPC.Center.X);
		NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X + direction * 0.055f, -5f, 5f);

		if ((StateTimer > 10f && IsGrounded()) || StateTimer > 150f)
		{
			SoundEngine.PlaySound(SoundID.Item70 with { Pitch = -0.2f, Volume = 0.55f }, NPC.Bottom);
			BeginState(MovementState.Scuttle);
		}
	}

	private void RunDespawn()
	{
		NPC.noTileCollide = true;
		NPC.velocity.X *= 0.94f;
		NPC.velocity.Y -= 0.12f;
		NPC.EncourageDespawn(30);
	}

	private bool IsGrounded()
	{
		// A thin probe supplements collideY on slopes and immediately after landing.
		Vector2 probePosition = new(NPC.position.X + 2f, NPC.Bottom.Y - 1f);
		return NPC.collideY || (NPC.velocity.Y == 0f
			&& Collision.SolidCollision(probePosition, NPC.width - 4, 3));
	}

	private void BeginState(MovementState nextState)
	{
		State = nextState;
		StateTimer = 0f;
		NPC.netUpdate = true;
	}

	private float GetBodyBob()
	{
		if (State == MovementState.LeapWindup)
		{
			return MathHelper.Lerp(0f, 5f, MathHelper.Clamp(StateTimer / LeapWindupDuration, 0f, 1f));
		}

		return State == MovementState.Scuttle && IsGrounded()
			? 1.5f * BodyScale * MathF.Sin(visualStepPhase * 2f)
			: 0f;
	}

	private void DrawLegs(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor, Vector2 bodyCenter)
	{
		Texture2D upperTexture = ModContent.Request<Texture2D>(UpperLegTexturePath).Value;
		Texture2D lowerTexture = ModContent.Request<Texture2D>(LowerLegTexturePath).Value;
		Color legColor = NPC.GetAlpha(drawColor);

		for (int legIndex = 0; legIndex < 4; legIndex++)
		{
			int side = legIndex < 2 ? -1 : 1;
			int pair = legIndex % 2;
			Vector2 hip = bodyCenter + new Vector2(side * 20f, pair == 0 ? -10f : 10f) * BodyScale;
			Vector2 foot = GetFootPosition(legIndex, side, pair);
			SolveLegJoints(hip, foot, side, out Vector2 knee, out Vector2 clampedFoot);

			SpriteEffects effects = side < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			DrawLegSegment(spriteBatch, lowerTexture, knee, clampedFoot, LowerLegStart, LowerLegEnd,
				effects, screenPos, legColor);
			DrawLegSegment(spriteBatch, upperTexture, hip, knee, UpperLegStart, UpperLegEnd,
				effects, screenPos, legColor);
		}
	}

	private Vector2 GetFootPosition(int legIndex, int side, int pair)
	{
		// Opposing phase offsets make the diagonal leg pairs step together.
		bool firstDiagonal = legIndex is 0 or 3;
		float phase = visualStepPhase + (firstDiagonal ? 0f : MathHelper.Pi);
		float strideDirection = Math.Sign(NPC.velocity.X);
		float baseReach = (pair == 0 ? 55f : 43f) * LegScale;
		float footX = NPC.Center.X + side * baseReach
			+ MathF.Cos(phase) * 9f * LegScale * strideDirection;
		float lift = MathF.Max(0f, MathF.Sin(phase)) * 9f * LegScale;
		float groundY = FindGroundY(footX, NPC.Bottom.Y + 18f);
		return new Vector2(footX, groundY - lift);
	}

	private float FindGroundY(float worldX, float fallbackY)
	{
		int tileX = Utils.Clamp((int)(worldX / 16f), 1, Main.maxTilesX - 2);
		int startY = Utils.Clamp((int)((NPC.Top.Y - 16f) / 16f), 1, Main.maxTilesY - 2);
		int endY = Utils.Clamp((int)((NPC.Bottom.Y + 72f) / 16f), startY, Main.maxTilesY - 2);

		for (int tileY = startY; tileY <= endY; tileY++)
		{
			Tile tile = Framing.GetTileSafely(tileX, tileY);
			if (!tile.HasTile || tile.IsActuated)
			{
				continue;
			}

			ushort type = tile.TileType;
			if (Main.tileSolid[type] || Main.tileSolidTop[type])
			{
				return tileY * 16f;
			}
		}

		return fallbackY;
	}

	private static void SolveLegJoints(Vector2 hip, Vector2 requestedFoot, int side,
		out Vector2 knee, out Vector2 foot)
	{
		Vector2 hipToFoot = requestedFoot - hip;
		float requestedDistance = MathF.Max(1f, hipToFoot.Length());
		float distance = MathHelper.Clamp(requestedDistance, 8f, UpperLegLength + LowerLegLength - 0.5f);
		Vector2 direction = hipToFoot / requestedDistance;
		foot = hip + direction * distance;

		float along = (UpperLegLength * UpperLegLength - LowerLegLength * LowerLegLength + distance * distance)
			/ (2f * distance);
		float height = MathF.Sqrt(MathF.Max(0f, UpperLegLength * UpperLegLength - along * along));
		Vector2 perpendicular = new(-direction.Y, direction.X);
		knee = hip + direction * along + perpendicular * (-side * height);
	}

	private static void DrawLegSegment(SpriteBatch spriteBatch, Texture2D texture, Vector2 start, Vector2 end,
		Vector2 sourceStart, Vector2 sourceEnd, SpriteEffects effects, Vector2 screenPos, Color drawColor)
	{
		Vector2 sourceAxis = sourceEnd - sourceStart;
		if ((effects & SpriteEffects.FlipHorizontally) != 0)
		{
			sourceAxis.X *= -1f;
		}

		float rotation = (end - start).ToRotation() - sourceAxis.ToRotation();
		Vector2 origin = sourceStart;
		if ((effects & SpriteEffects.FlipHorizontally) != 0)
		{
			// Flipped geometry needs the mirrored source pivot to keep every joint connected.
			origin.X = texture.Width - sourceStart.X;
		}

		Main.EntitySpriteDraw(texture, start - screenPos, null, drawColor, rotation,
			origin, LegScale, effects);
	}
}
