using System;
using Microsoft.Xna.Framework;
using SoulsOfTerra.Content.Items.Accessories;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SoulsOfTerra.Players;

public class BorrowedSentencePlayer : ModPlayer
{
	public const int TrialDuration = 6 * 60;
	public const int CooldownDuration = 14 * 60;
	public const float DeferredDamagePortion = 0.4f;
	public const int RepaymentMultiplier = 12;

	private int storedDamage;
	private int requiredRepayment;
	private int repaidDamage;
	private bool finalWarningPlayed;

	public bool Equipped { get; set; }
	public bool SentenceActive { get; private set; }
	public int TrialTimeRemaining { get; private set; }
	public int CooldownTimeRemaining { get; private set; }
	public float RepaymentProgress => requiredRepayment <= 0 ? 0f : MathHelper.Clamp(repaidDamage / (float)requiredRepayment, 0f, 1f);

	public override void ResetEffects()
	{
		Equipped = false;
	}

	public override void UpdateDead()
	{
		SentenceActive = false;
		TrialTimeRemaining = 0;
		CooldownTimeRemaining = 0;
		storedDamage = 0;
		requiredRepayment = 0;
		repaidDamage = 0;
		finalWarningPlayed = false;
	}

	public override void PostUpdate()
	{
		if (Player.whoAmI != Main.myPlayer)
		{
			return;
		}

		if (CooldownTimeRemaining > 0)
		{
			CooldownTimeRemaining--;
		}

		if (!SentenceActive)
		{
			return;
		}

		// Removing the accessory cannot discard a sentence already pronounced.
		if (!Equipped)
		{
			ResolveSentence(false);
			return;
		}

		TrialTimeRemaining--;
		if (!finalWarningPlayed && TrialTimeRemaining <= 60)
		{
			finalWarningPlayed = true;
			SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.8f, Pitch = -0.45f }, Player.Center);
		}

		if (TrialTimeRemaining <= 0)
		{
			ResolveSentence(false);
		}
	}

	public override void ModifyHurt(ref Player.HurtModifiers modifiers)
	{
		if (!Equipped || SentenceActive || CooldownTimeRemaining > 0 || Player.whoAmI != Main.myPlayer)
		{
			return;
		}

		modifiers.ModifyHurtInfo += TryBorrowDamage;
	}

	public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
	{
		RegisterRepayment(target, damageDone);
	}

	public override void OnHitNPCWithProj(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
	{
		RegisterRepayment(target, damageDone);
	}

	private void TryBorrowDamage(ref Player.HurtInfo info)
	{
		int qualifyingDamage = (int)Math.Ceiling(Player.statLifeMax2 * 0.1f);
		if (info.Damage < qualifyingDamage || info.DamageSource.SourcePlayerIndex >= 0)
		{
			return;
		}

		storedDamage = Math.Max(1, (int)Math.Round(info.Damage * DeferredDamagePortion));
		requiredRepayment = storedDamage * RepaymentMultiplier;
		repaidDamage = 0;
		TrialTimeRemaining = TrialDuration;
		SentenceActive = true;
		finalWarningPlayed = false;
		info.Damage -= storedDamage;

		SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.7f, Pitch = -0.35f }, Player.Center);
		SpawnSealDust(new Color(84, 239, 218), 18);
	}

	private void RegisterRepayment(NPC target, int damageDone)
	{
		if (!SentenceActive || damageDone <= 0 || !IsHostileTarget(target))
		{
			return;
		}

		repaidDamage = Math.Min(requiredRepayment, repaidDamage + damageDone);
		if (repaidDamage >= requiredRepayment)
		{
			ResolveSentence(true);
		}
	}

	private static bool IsHostileTarget(NPC target)
	{
		if (target.friendly || target.type == NPCID.TargetDummy || NPCID.Sets.CountsAsCritter[target.type])
		{
			return false;
		}

		// A killing blow still counts after the target stops being chaseable.
		return target.CanBeChasedBy() || target.life <= 0;
	}

	private void ResolveSentence(bool absolved)
	{
		int judgmentDamage = storedDamage;
		SentenceActive = false;
		TrialTimeRemaining = 0;
		CooldownTimeRemaining = CooldownDuration;
		storedDamage = 0;
		requiredRepayment = 0;
		repaidDamage = 0;
		finalWarningPlayed = false;

		if (absolved)
		{
			CombatText.NewText(Player.Hitbox, new Color(115, 255, 220), Language.GetTextValue("Mods.SoulsOfTerra.UI.SentenceAbsolved"));
			SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.8f, Pitch = 0.35f }, Player.Center);
			SpawnSealDust(new Color(115, 255, 220), 28);
			return;
		}

		ApplyJudgment(judgmentDamage);
	}

	private void ApplyJudgment(int damage)
	{
		if (damage <= 0 || Player.dead)
		{
			return;
		}

		SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 0.85f, Pitch = -0.25f }, Player.Center);
		SpawnSealDust(new Color(255, 96, 112), 32);

		// Judgment removes the stored final damage exactly, bypassing a second defense calculation.
		Player.statLife -= damage;
		CombatText.NewText(Player.Hitbox, new Color(255, 80, 96), damage);
		if (Player.statLife <= 0)
		{
			Player.statLife = 0;
			Player.KillMe(PlayerDeathReason.ByCustomReason(
				NetworkText.FromKey("Mods.SoulsOfTerra.DeathMessage.BorrowedSentence", Player.name)), damage, 0);
			return;
		}

		if (Main.netMode == NetmodeID.MultiplayerClient)
		{
			NetMessage.SendData(MessageID.PlayerLifeMana, number: Player.whoAmI);
		}
	}

	private void SpawnSealDust(Color color, int amount)
	{
		for (int i = 0; i < amount; i++)
		{
			Vector2 velocity = Main.rand.NextVector2CircularEdge(3.5f, 3.5f) * Main.rand.NextFloat(0.45f, 1f);
			Dust dust = Dust.NewDustPerfect(Player.Center, DustID.GemSapphire, velocity, 0, color, Main.rand.NextFloat(0.8f, 1.3f));
			dust.noGravity = true;
		}
	}
}
