using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Common;
using SoulsOfTerra.Common.Rendering;
using SoulsOfTerra.Content.Buffs;
using SoulsOfTerra.Content.Projectiles;
using SoulsOfTerra.Systems;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SoulsOfTerra.Players;

public class SoulSpellPlayer : ModPlayer, IPixelatedDrawable
{
	private const int DashCooldown = 45;
	private const int DashImpulseFrames = 7;
	private const int DashIFrames = 6;
	private const int DashTrailLinger = 8;
	private const int DashSnapFrames = 4;
	private const int DashPathCapacity = 16;
	private const float DashStartVelocity = 14f;
	private const float DashEndVelocity = 8f;
	private const int DashRight = 2;
	private const int DashLeft = 3;
	private const int SoulFlightDuration = 2 * 60;
	private const int SoulFlightCooldown = 3 * 60;
	private const int SoulFlightTransformFrames = 10;
	private const int SoulFlightTrailLength = 18;
	private const float SoulFlightSpeed = 9f;
	private const float SoulFlightSteering = 0.22f;
	private const float SoulFlightIdleDrag = 0.82f;
	private static readonly SoundStyle DashWhoosh = SoundID.Item72 with { Volume = 0.28f, Pitch = 0.55f };
	private static readonly SoundStyle DashSnap = SoundID.Item9 with { Volume = 0.16f, Pitch = 0.72f };

	private readonly Vector2[] dashPath = new Vector2[DashPathCapacity];
	private readonly Vector2[] soulFlightTrail = new Vector2[SoulFlightTrailLength];
	private int dashPathCount;
	private int dashCooldown;
	private int dashTimer;
	private int dashVisualTimer;
	private int dashDirection;
	private int dashDir = -1;
	private int dashTapWindow;
	private int dashReunionTimer;
	private int dashShakeTimer;
	private bool dashSnapPlayed;
	private bool rightHeld;
	private bool leftHeld;
	private int soulFlightTimer;
	private Vector2 dashStartPosition;
	private double drainAccumulator;

	public bool HasDashVisual => dashVisualTimer > 0 || SoulFlightActive;
	public bool SoulFlightActive => soulFlightTimer > 0;

	public uint SelectionMask { get; private set; } = SoulSpellRegistry.DefaultSelectionMask;
	public bool StanceOn { get; private set; }

	public bool DashEnabled => SoulSpellRegistry.IsSelected(SelectionMask, SoulSpellId.Dash);
	public bool FlightEnabled => SoulSpellRegistry.IsSelected(SelectionMask, SoulSpellId.Flight);
	public bool LightChecked => SoulSpellRegistry.IsSelected(SelectionMask, SoulSpellId.Light);
	public bool LightActive => StanceOn && LightChecked;

	public override void Initialize()
	{
		SelectionMask = SoulSpellRegistry.DefaultSelectionMask;
		StanceOn = false;
		dashCooldown = 0;
		dashTimer = 0;
		dashVisualTimer = 0;
		dashDirection = 0;
		dashDir = -1;
		dashTapWindow = 0;
		dashReunionTimer = 0;
		dashShakeTimer = 0;
		dashSnapPlayed = false;
		rightHeld = false;
		leftHeld = false;
		soulFlightTimer = 0;
		drainAccumulator = 0d;
		ClearDashPath();
		ClearSoulFlightTrail();
	}

	public override void PreUpdate()
	{
		dashDir = -1;
		if (dashTapWindow > 0)
		{
			dashTapWindow--;
		}
		else if (dashTapWindow < 0)
		{
			dashTapWindow++;
		}

		bool rightPressed = Player.controlRight && !rightHeld;
		bool leftPressed = Player.controlLeft && !leftHeld;
		rightHeld = Player.controlRight;
		leftHeld = Player.controlLeft;

		if (rightPressed)
		{
			if (dashTapWindow > 0)
			{
				dashDir = DashRight;
			}

			dashTapWindow = 15;
		}
		else if (leftPressed)
		{
			if (dashTapWindow < 0)
			{
				dashDir = DashLeft;
			}

			dashTapWindow = -15;
		}
	}

	public override void SaveData(TagCompound tag)
	{
		tag["soulspellMask"] = (int)SelectionMask;
	}

	public override void LoadData(TagCompound tag)
	{
		SelectionMask = tag.ContainsKey("soulspellMask")
			? (uint)tag.GetInt("soulspellMask")
			: SoulSpellRegistry.DefaultSelectionMask;
		StanceOn = false;
		drainAccumulator = 0d;
	}

	public override void ProcessTriggers(TriggersSet triggersSet)
	{
		if (Player.whoAmI != Main.myPlayer || Player.dead || Main.drawingPlayerChat || Main.editSign || Main.editChest)
		{
			return;
		}

		if (SoulSpellKeybinds.Book.JustPressed)
		{
			Systems.SoulSpellBookSystem.Toggle();
		}

		if (SoulSpellKeybinds.Stance.JustPressed)
		{
			RequestStance(!StanceOn);
		}
	}

	public override void UpdateDead()
	{
		if (StanceOn)
		{
			ApplyStance(false);
		}

		drainAccumulator = 0d;
		dashTimer = 0;
		dashVisualTimer = 0;
		dashDir = -1;
		dashReunionTimer = 0;
		dashShakeTimer = 0;
		dashSnapPlayed = false;
		soulFlightTimer = 0;
		ClearDashPath();
		ClearSoulFlightTrail();
	}

	public override void PostUpdate()
	{
		if (Player.dead)
		{
			return;
		}

		TickDrain();
		ApplyBuffs();
		if (dashVisualTimer >= DashTrailLinger)
		{
			SetDashHead(DashTorso);
		}

		if (SoulFlightActive)
		{
			RecordSoulFlightPosition();
			AdvanceSoulFlight();
		}
	}

	public override void PreUpdateMovement()
	{
		if (dashCooldown > 0)
		{
			dashCooldown--;
		}

		TryStartDash();
		if (SoulFlightActive)
		{
			TickSoulFlight();
		}
		else
		{
			TickDash();
		}
		dashDir = -1;
	}

	public void DrawPixelated(SpriteBatch spriteBatch)
	{
		if (SoulFlightActive)
		{
			DrawSoulFlight();
			return;
		}

		GetWakePhase(out float retract, out float snapFlash, out float intensity);
		GetEchoPhase(out float echoProgress, out float echoOpacity);
		SoulDashTrailDraw.Draw(dashPath, dashPathCount, retract, snapFlash, intensity,
			Main.GlobalTimeWrappedHourly, Player.whoAmI, echoProgress, echoOpacity, dashDirection);
	}

	public override void DrawPlayer(Camera camera)
	{
		if (SoulFlightActive)
		{
			return;
		}

		GetEchoPhase(out float echoProgress, out float echoOpacity);
		if (echoOpacity <= 0.02f)
		{
			return;
		}

		// The abandoned body and two advancing copies chase the moving player.
		DrawSoulBody(camera, dashStartPosition, 0.42f + echoProgress * 0.4f);
		for (int index = 0; index < 2; index++)
		{
			float spacing = (index + 1f) / 3f;
			float advance = MathHelper.Clamp(echoProgress + spacing * 0.48f, 0f, 0.96f);
			Vector2 position = Vector2.Lerp(dashStartPosition, Player.position, advance);
			DrawSoulBody(camera, position, 0.5f + index * 0.14f);
		}
	}

	public override void DrawEffects(PlayerDrawSet drawInfo, ref float r, ref float g, ref float b, ref float a,
		ref bool fullBright)
	{
		if (drawInfo.shadow <= 0f || dashVisualTimer <= DashTrailLinger)
		{
			return;
		}

		// Extra player copies become saturated spectral bodies instead of ordinary dark shadows.
		r *= 0.22f;
		g = Math.Max(g, 0.95f);
		b = Math.Max(b, 0.78f);
		a *= 0.82f;
	}

	public override void TransformDrawData(ref PlayerDrawSet drawInfo)
	{
		if (!SoulFlightActive)
		{
			return;
		}

		float bodyOpacity = GetSoulFlightBodyOpacity();
		if (bodyOpacity <= 0.01f)
		{
			// Clearing the completed draw cache also catches armor and modded accessory layers.
			drawInfo.DrawDataCache.Clear();
			return;
		}

		Vector2 center = Player.Center - Main.screenPosition;
		float compression = MathHelper.Lerp(0.45f, 1f, bodyOpacity);
		for (int index = 0; index < drawInfo.DrawDataCache.Count; index++)
		{
			DrawData data = drawInfo.DrawDataCache[index];
			Vector2 offset = data.position - center;
			data.position = center + new Vector2(offset.X * compression, offset.Y * compression);
			data.scale *= compression;
			data.color = Color.Lerp(data.color, new Color(105, 255, 205, data.color.A), 0.58f) * bodyOpacity;
			drawInfo.DrawDataCache[index] = data;
		}
	}

	public override bool ImmuneTo(PlayerDeathReason damageSource, int cooldownCounter, bool dodgeable)
	{
		return SoulFlightActive;
	}

	public override bool CanUseItem(Item item) => !SoulFlightActive;

	public override bool CanHitNPC(NPC target) => !SoulFlightActive;

	public override bool? CanHitNPCWithItem(Item item, NPC target) => SoulFlightActive ? false : null;

	public override bool? CanHitNPCWithProj(Projectile proj, NPC target) => SoulFlightActive ? false : null;

	public override bool CanHitPvp(Item item, Player target) => !SoulFlightActive;

	public override bool CanHitPvpWithProj(Projectile proj, Player target) => !SoulFlightActive;

	public override void UpdateBadLifeRegen()
	{
		if (SoulFlightActive && Player.lifeRegen < 0)
		{
			// Debuffs keep ticking, but their damage cannot pierce the soul form.
			Player.lifeRegen = 0;
			Player.lifeRegenTime = 0;
		}
	}

	public override void ModifyScreenPosition()
	{
		if (dashShakeTimer <= 0 || Player.whoAmI != Main.myPlayer)
		{
			return;
		}

		float strength = dashShakeTimer * 0.55f;
		Main.screenPosition += Main.rand.NextVector2Circular(strength, strength);
		dashShakeTimer--;
	}

	public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
	{
		SendState(toWho, fromWho);
	}

	public void RequestSelection(SoulSpellId id, bool selected)
	{
		uint nextMask = SoulSpellRegistry.WithExclusiveSelection(SelectionMask, id, selected);
		ApplySelection(nextMask);
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			ModPacket packet = Mod.GetPacket();
			packet.Write((byte)SoulMessageType.RequestSoulSpellToggle);
			packet.Write((byte)id);
			packet.Write(selected);
			packet.Send();
		}
		else
		{
			SendState();
		}
	}

	public void RequestStance(bool on)
	{
		if (on && !CanAffordStance(SelectionMask))
		{
			NotifyNeedSouls();
			return;
		}

		ApplyStance(on);
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			ModPacket packet = Mod.GetPacket();
			packet.Write((byte)SoulMessageType.RequestSoulSpellStance);
			packet.Write(on);
			packet.Send();
		}
		else
		{
			SendState();
		}
	}

	public static void HandleToggleRequest(BinaryReader reader, int whoAmI)
	{
		SoulSpellId id = (SoulSpellId)reader.ReadByte();
		bool selected = reader.ReadBoolean();
		if (Main.netMode != NetmodeID.Server || !TryGetPlayer(whoAmI, out Player player))
		{
			return;
		}

		SoulSpellPlayer spellPlayer = player.GetModPlayer<SoulSpellPlayer>();
		spellPlayer.ApplySelection(SoulSpellRegistry.WithExclusiveSelection(spellPlayer.SelectionMask, id, selected));
		spellPlayer.SendState();
	}

	public static void HandleStanceRequest(BinaryReader reader, int whoAmI)
	{
		bool on = reader.ReadBoolean();
		if (Main.netMode != NetmodeID.Server || !TryGetPlayer(whoAmI, out Player player))
		{
			return;
		}

		SoulSpellPlayer spellPlayer = player.GetModPlayer<SoulSpellPlayer>();
		if (on && !spellPlayer.CanAffordStance(spellPlayer.SelectionMask))
		{
			spellPlayer.ApplyStance(false);
			spellPlayer.SendState();
			return;
		}

		spellPlayer.ApplyStance(on);
		spellPlayer.SendState();
	}

	public static void HandleStateSync(BinaryReader reader)
	{
		if (Main.netMode == NetmodeID.Server)
		{
			return;
		}

		int playerIndex = reader.ReadByte();
		uint mask = reader.ReadUInt32();
		bool stance = reader.ReadBoolean();
		if (playerIndex < 0 || playerIndex >= Main.maxPlayers)
		{
			return;
		}

		SoulSpellPlayer spellPlayer = Main.player[playerIndex].GetModPlayer<SoulSpellPlayer>();
		spellPlayer.SelectionMask = mask;
		spellPlayer.ApplyStance(stance);
	}

	public static void HandleSoulFlightRequest(BinaryReader reader, int whoAmI)
	{
		int direction = reader.ReadSByte();
		if (Main.netMode != NetmodeID.Server || direction is not (-1 or 1)
			|| !TryGetPlayer(whoAmI, out Player player))
		{
			return;
		}

		SoulSpellPlayer spellPlayer = player.GetModPlayer<SoulSpellPlayer>();
		if (!spellPlayer.FlightEnabled || spellPlayer.SoulFlightActive || spellPlayer.dashCooldown > 0
			|| player.mount.Active || player.CCed || player.pulley || player.dashType != 0 || player.setSolar)
		{
			return;
		}

		spellPlayer.StartSoulFlight(direction, false);
	}

	public static void HandleSoulFlightSync(BinaryReader reader)
	{
		int playerIndex = reader.ReadByte();
		int direction = reader.ReadSByte();
		Vector2 velocity = new(reader.ReadSingle(), reader.ReadSingle());
		if (Main.netMode == NetmodeID.Server || playerIndex < 0 || playerIndex >= Main.maxPlayers
			|| playerIndex == Main.myPlayer)
		{
			return;
		}

		Player player = Main.player[playerIndex];
		SoulSpellPlayer spellPlayer = player.GetModPlayer<SoulSpellPlayer>();
		player.velocity = velocity;
		spellPlayer.StartSoulFlight(direction, false);
	}

	private void ApplySelection(uint mask)
	{
		SelectionMask = mask;
		if (StanceOn && !CanAffordStance(SelectionMask))
		{
			ApplyStance(false);
			NotifyNeedSouls();
		}

		if (SoulSpellRegistry.GetCheckedPaidSoulsPerTick(SelectionMask) <= 0d)
		{
			drainAccumulator = 0d;
		}
	}

	private void ApplyStance(bool on)
	{
		if (StanceOn == on)
		{
			return;
		}

		StanceOn = on;
		drainAccumulator = 0d;
	}

	private bool CanAffordStance(uint mask)
	{
		return SoulSpellRegistry.GetCheckedPaidSoulsPerTick(mask) <= 0d
			|| Player.GetModPlayer<SoulPlayer>().SoulBalance >= 1;
	}

	private void TickDrain()
	{
		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			return;
		}

		double soulsPerTick = SoulSpellRegistry.GetSoulsPerTick(SelectionMask, StanceOn);
		if (soulsPerTick <= 0d)
		{
			drainAccumulator = 0d;
			return;
		}

		SoulPlayer soulPlayer = Player.GetModPlayer<SoulPlayer>();
		if (soulPlayer.SoulBalance < 1)
		{
			ApplyStance(false);
			SendState();
			return;
		}

		drainAccumulator += soulsPerTick;
		while (drainAccumulator >= 1d)
		{
			if (!soulPlayer.TrySpendSouls(1))
			{
				ApplyStance(false);
				SendState();
				return;
			}

			drainAccumulator -= 1d;
			if (soulPlayer.SoulBalance < 1)
			{
				ApplyStance(false);
				SendState();
				return;
			}
		}
	}

	private void ApplyBuffs()
	{
		if (DashEnabled)
		{
			Player.AddBuff(ModContent.BuffType<SoulDashBuff>(), 2);
		}

		if (FlightEnabled)
		{
			Player.AddBuff(ModContent.BuffType<SoulFlightBuff>(), 2);
		}

		if (LightActive)
		{
			Player.AddBuff(ModContent.BuffType<SoulLightBuff>(), 2);
		}
	}

	private void TryStartDash()
	{
		if ((!DashEnabled && !FlightEnabled) || dashDir < 0 || dashCooldown > 0 || SoulFlightActive
			|| Player.mount.Active || Player.CCed || Player.pulley
			|| Player.dashType != 0 || Player.setSolar)
		{
			return;
		}

		int direction = dashDir == DashRight ? 1 : dashDir == DashLeft ? -1 : 0;
		if (direction == 0)
		{
			return;
		}

		if (DashEnabled && ((direction > 0 && Player.velocity.X >= DashStartVelocity)
			|| (direction < 0 && Player.velocity.X <= -DashStartVelocity)))
		{
			return;
		}

		if (FlightEnabled)
		{
			StartSoulFlight(direction);
		}
		else
		{
			StartDash(direction);
		}
	}

	private Vector2 DashTorso => Player.Center + new Vector2(0f, 8f);

	private void StartDash(int direction)
	{
		dashDirection = direction;
		dashCooldown = DashCooldown;
		dashTimer = DashImpulseFrames;
		dashVisualTimer = DashImpulseFrames + DashTrailLinger;
		dashReunionTimer = 0;
		dashSnapPlayed = false;
		dashStartPosition = Player.position;
		Player.velocity.X = DashStartVelocity * direction;
		Player.direction = direction;
		Player.SetImmuneTimeForAllTypes(DashIFrames);
		ClearDashPath();
		dashPath[0] = DashTorso;
		dashPathCount = 1;
		if (!Main.dedServ)
		{
			SoundEngine.PlaySound(DashWhoosh, Player.Center);
			SpawnCastFragments();
		}
	}

	private void TickDash()
	{
		if (dashReunionTimer > 0)
		{
			dashReunionTimer--;
		}

		if (dashVisualTimer > 0)
		{
			AddWakeLight();
			dashVisualTimer--;
			if (dashVisualTimer <= 0)
			{
				ClearDashPath();
			}
		}

		if (dashTimer <= 0)
		{
			return;
		}

		// The burst eases out so control returns naturally without touching vertical momentum.
		float progress = 1f - (dashTimer - 1f) / DashImpulseFrames;
		float speed = MathHelper.Lerp(DashStartVelocity, DashEndVelocity, MathHelper.SmoothStep(0f, 1f, progress));
		Player.velocity.X = speed * dashDirection;
		dashTimer--;
		if (dashTimer == 0)
		{
			TriggerReunion();
		}
	}

	private void StartSoulFlight(int direction, bool sendNetworkRequest = true)
	{
		dashDirection = direction;
		soulFlightTimer = SoulFlightDuration;
		Player.direction = direction;
		Player.itemAnimation = 0;
		Player.itemTime = 0;
		Player.RemoveAllGrapplingHooks();
		ClearDashPath();
		ClearSoulFlightTrail();
		RecordSoulFlightPosition();

		if (!Main.dedServ)
		{
			SoundEngine.PlaySound(DashWhoosh with { Pitch = 0.1f }, Player.Center);
			SpawnSoulFlightBurst();
		}

		if (sendNetworkRequest && Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI == Main.myPlayer)
		{
			ModPacket packet = Mod.GetPacket();
			packet.Write((byte)SoulMessageType.RequestSoulFlight);
			packet.Write((sbyte)direction);
			packet.Send();
		}
		else if (Main.netMode == NetmodeID.Server)
		{
			SendSoulFlight(Player.whoAmI);
		}
	}

	private void TickSoulFlight()
	{
		Vector2 input = new(
			(Player.controlRight ? 1f : 0f) - (Player.controlLeft ? 1f : 0f),
			(Player.controlDown ? 1f : 0f) - (Player.controlUp || Player.controlJump ? 1f : 0f));

		if (input != Vector2.Zero)
		{
			input.Normalize();
			Player.velocity = Vector2.Lerp(Player.velocity, input * SoulFlightSpeed, SoulFlightSteering);
			if (input.X != 0f)
			{
				Player.direction = Math.Sign(input.X);
			}
		}
		else
		{
			// A short glide preserves momentum before settling into a hover.
			Player.velocity *= SoulFlightIdleDrag;
			if (Player.velocity.LengthSquared() < 0.01f)
			{
				Player.velocity = Vector2.Zero;
			}
		}

		Lighting.AddLight(Player.Center, 0.2f, 0.65f, 0.42f);
		// Falling resumes from the return point instead of accumulating through controlled flight.
		Player.fallStart = (int)(Player.position.Y / 16f);
	}

	private void AdvanceSoulFlight()
	{
		soulFlightTimer--;
		if (soulFlightTimer > 0)
		{
			return;
		}

		dashCooldown = SoulFlightCooldown;
		if (!Main.dedServ)
		{
			SoundEngine.PlaySound(DashSnap with { Pitch = 0.35f }, Player.Center);
			SpawnSoulFlightBurst();
		}
	}

	private void RecordSoulFlightPosition()
	{
		for (int index = soulFlightTrail.Length - 1; index > 0; index--)
		{
			soulFlightTrail[index] = soulFlightTrail[index - 1];
		}

		soulFlightTrail[0] = Player.Center;
	}

	private void DrawSoulFlight()
	{
		float bodyOpacity = GetSoulFlightBodyOpacity();
		float soulOpacity = 1f - bodyOpacity;
		DrawSoulFlightTrail(soulOpacity);

		float soulScale = 1.35f * MathHelper.SmoothStep(0f, 1f, soulOpacity);
		if (soulScale > 0.02f)
		{
			SoulOrbProjectile.DrawSoulVisualAt(Player.Center - Main.screenPosition, 1, soulOpacity,
				soulScale, Player.whoAmI);
		}

		DrawSoulFlightTransitionWisps(bodyOpacity);
	}

	private void DrawSoulFlightTrail(float opacity)
	{
		if (opacity <= 0.02f)
		{
			return;
		}

		Texture2D glow = SoulOrbProjectile.GetGlowTexture();
		Vector2 origin = glow.Size() * 0.5f;
		for (int index = soulFlightTrail.Length - 2; index >= 0; index--)
		{
			Vector2 start = soulFlightTrail[index + 1];
			Vector2 end = soulFlightTrail[index];
			if (start == Vector2.Zero || end == Vector2.Zero)
			{
				continue;
			}

			Vector2 delta = end - start;
			float length = delta.Length();
			if (length <= 0.1f)
			{
				continue;
			}

			float strength = 1f - index / (float)(soulFlightTrail.Length - 1);
			Vector2 midpoint = (start + end) * 0.5f - Main.screenPosition;
			Vector2 scale = new(length / glow.Width + 0.05f, MathHelper.Lerp(0.035f, 0.12f, strength));
			Color color = new Color(80, 235, 115, 0) * (opacity * strength * 0.72f);
			Main.EntitySpriteDraw(glow, midpoint, null, color, delta.ToRotation(), origin, scale,
				SpriteEffects.None);
		}
	}

	private void DrawSoulFlightTransitionWisps(float bodyOpacity)
	{
		float transitionStrength = 4f * bodyOpacity * (1f - bodyOpacity);
		if (transitionStrength <= 0.02f)
		{
			return;
		}

		Texture2D glow = SoulOrbProjectile.GetGlowTexture();
		Vector2 origin = glow.Size() * 0.5f;
		float radius = MathHelper.Lerp(4f, 30f, bodyOpacity);
		Vector2 travelDirection = Player.velocity.SafeNormalize(Vector2.UnitX * Player.direction);
		float travelStretch = 0.2f + Math.Min(Player.velocity.Length(), SoulFlightSpeed) * 0.025f;
		Main.EntitySpriteDraw(glow, Player.Center - travelDirection * 5f - Main.screenPosition, null,
			new Color(100, 255, 210, 0) * transitionStrength, travelDirection.ToRotation(), origin,
			new Vector2(travelStretch, 0.09f), SpriteEffects.None);

		for (int index = 0; index < 6; index++)
		{
			float phase = Main.GlobalTimeWrappedHourly * 5.5f + MathHelper.TwoPi * index / 6f;
			Vector2 offset = phase.ToRotationVector2() * radius;
			offset.Y *= 0.62f;
			Color color = new Color(130, 255, 215, 0) * (transitionStrength * 0.8f);
			Main.EntitySpriteDraw(glow, Player.Center + offset - Main.screenPosition, null, color,
				phase, origin, new Vector2(0.15f, 0.055f), SpriteEffects.None);
		}
	}

	private float GetSoulFlightBodyOpacity()
	{
		int elapsed = SoulFlightDuration - soulFlightTimer;
		if (elapsed < SoulFlightTransformFrames)
		{
			float progress = elapsed / (float)SoulFlightTransformFrames;
			return 1f - MathHelper.SmoothStep(0f, 1f, progress);
		}

		if (soulFlightTimer <= SoulFlightTransformFrames)
		{
			float progress = 1f - soulFlightTimer / (float)SoulFlightTransformFrames;
			return MathHelper.SmoothStep(0f, 1f, progress);
		}

		return 0f;
	}

	private void ClearSoulFlightTrail()
	{
		Array.Clear(soulFlightTrail);
	}

	private void GetEchoPhase(out float progress, out float opacity)
	{
		int chaseLeft = dashVisualTimer - DashTrailLinger;
		if (chaseLeft <= 0)
		{
			progress = 1f;
			opacity = 0f;
			return;
		}

		float chase = 1f - chaseLeft / (float)DashImpulseFrames;
		progress = MathHelper.SmoothStep(0f, 1f, chase);
		opacity = MathHelper.Lerp(0.72f, 0.18f, progress);
	}

	private void GetWakePhase(out float retract, out float snapFlash, out float intensity)
	{
		snapFlash = dashReunionTimer / (float)DashSnapFrames;
		if (dashVisualTimer > DashTrailLinger)
		{
			retract = 0f;
			intensity = 1f;
			return;
		}

		float retractT = 1f - dashVisualTimer / (float)DashTrailLinger;
		retract = MathHelper.SmoothStep(0f, 0.94f, retractT);
		intensity = MathHelper.Lerp(1.15f, 0.22f, retractT);
	}

	private void AddWakeLight()
	{
		if (dashPathCount <= 0)
		{
			return;
		}

		float glow = dashReunionTimer > 0 ? 0.9f : 0.62f;
		Lighting.AddLight(dashPath[0], 0.08f * glow, 0.28f * glow, 0.24f * glow);
		Lighting.AddLight(dashPath[dashPathCount - 1], 0.18f * glow, 0.55f * glow, 0.48f * glow);
		if (dashPathCount > 2)
		{
			Lighting.AddLight(dashPath[dashPathCount / 2], 0.12f * glow, 0.4f * glow, 0.35f * glow);
		}
	}

	private void TriggerReunion()
	{
		if (dashSnapPlayed || Main.dedServ)
		{
			return;
		}

		dashSnapPlayed = true;
		dashReunionTimer = DashSnapFrames;
		dashShakeTimer = 3;
		SoundEngine.PlaySound(DashSnap, dashPathCount > 0 ? dashPath[dashPathCount - 1] : Player.Center);
		SpawnReunionFragments();
	}

	private void DrawSoulBody(Camera camera, Vector2 position, float shadow)
	{
		Main.PlayerRenderer.DrawPlayer(camera, Player, position, Player.fullRotation, Player.fullRotationOrigin,
			shadow, 1f);
	}

	private void SpawnCastFragments()
	{
		for (int index = 0; index < 6; index++)
		{
			Vector2 velocity = Main.rand.NextVector2Circular(2.1f, 2.1f) - Vector2.UnitX * dashDirection;
			Dust dust = Dust.NewDustPerfect(Player.Center, DustID.TintableDustLighted, velocity, 60,
				new Color(155, 255, 225), Main.rand.NextFloat(0.4f, 0.7f));
			dust.noGravity = true;
		}
	}

	private void SpawnReunionFragments()
	{
		for (int index = 0; index < 10; index++)
		{
			Vector2 velocity = (MathHelper.TwoPi * index / 10f).ToRotationVector2()
				* Main.rand.NextFloat(2.5f, 4.6f);
			Dust dust = Dust.NewDustPerfect(Player.Center, DustID.TintableDustLighted, velocity, 35,
				new Color(205, 255, 240), Main.rand.NextFloat(0.45f, 0.75f));
			dust.noGravity = true;
		}
	}

	private void SpawnSoulFlightBurst()
	{
		for (int index = 0; index < 12; index++)
		{
			Vector2 velocity = Main.rand.NextVector2CircularEdge(3.2f, 3.2f);
			Dust dust = Dust.NewDustPerfect(Player.Center, DustID.DungeonSpirit, velocity, 90,
				new Color(80, 235, 115), Main.rand.NextFloat(0.55f, 0.85f));
			dust.noGravity = true;
		}
	}

	private void SetDashHead(Vector2 torso)
	{
		if (dashPathCount < 2)
		{
			dashPath[0] = dashPathCount == 0 ? torso : dashPath[0];
			dashPath[1] = torso;
			dashPathCount = 2;
			return;
		}

		Vector2 previous = dashPath[dashPathCount - 2];
		if (dashPathCount < DashPathCapacity && Vector2.DistanceSquared(previous, torso) > 64f)
		{
			dashPath[dashPathCount - 1] = Vector2.Lerp(previous, torso, 0.5f);
			dashPath[dashPathCount++] = torso;
			return;
		}

		dashPath[dashPathCount - 1] = torso;
	}

	private void ClearDashPath()
	{
		Array.Clear(dashPath);
		dashPathCount = 0;
	}

	private void NotifyNeedSouls()
	{
		if (Player.whoAmI != Main.myPlayer)
		{
			return;
		}

		Main.NewText(Language.GetTextValue("Mods.SoulsOfTerra.UI.SoulspellNeedSouls"), 238, 154, 137);
	}

	private void SendState(int toWho = -1, int fromWho = -1)
	{
		if (Main.netMode == NetmodeID.SinglePlayer)
		{
			return;
		}

		ModPacket packet = Mod.GetPacket();
		packet.Write((byte)SoulMessageType.SyncSoulSpellState);
		packet.Write((byte)Player.whoAmI);
		packet.Write(SelectionMask);
		packet.Write(StanceOn);
		packet.Send(toWho, fromWho);
	}

	private void SendSoulFlight(int ignoreClient)
	{
		ModPacket packet = Mod.GetPacket();
		packet.Write((byte)SoulMessageType.SyncSoulFlight);
		packet.Write((byte)Player.whoAmI);
		packet.Write((sbyte)dashDirection);
		packet.Write(Player.velocity.X);
		packet.Write(Player.velocity.Y);
		packet.Send(-1, ignoreClient);
	}

	private static bool TryGetPlayer(int whoAmI, out Player player)
	{
		player = null;
		if (whoAmI < 0 || whoAmI >= Main.maxPlayers)
		{
			return false;
		}

		player = Main.player[whoAmI];
		return player.active;
	}
}
