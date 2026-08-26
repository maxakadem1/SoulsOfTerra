using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulsOfTerra.Content.Bosses.SealedCongregation;

public class SealedCongregationSeal : ModNPC
{
	private const float BrokenThreshold = 0.35f;
	private NPC Parent => NPC.ai[0] >= 0f && NPC.ai[0] < Main.maxNPCs ? Main.npc[(int)NPC.ai[0]] : null;
	public override string Texture => "SoulsOfTerra/Content/Bosses/SealedCongregation/SealedCongregation_seal";

	public override void SetStaticDefaults()
	{
		Main.npcFrameCount[Type] = 1;
	}

	public override void SetDefaults()
	{
		NPC.width = 56;
		NPC.height = 72;
		NPC.damage = 20;
		NPC.defense = 10;
		// Four seals provide a substantial 3,000-health first phase without exceeding the total budget.
		NPC.lifeMax = 750;
		NPC.knockBackResist = 0f;
		NPC.value = 0f;
		NPC.noGravity = true;
		NPC.noTileCollide = true;
		NPC.netAlways = true;
		NPC.aiStyle = -1;
		NPC.HitSound = SoundID.NPCHit4;
		NPC.DeathSound = SoundID.NPCDeath6;
	}

	public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
	{
		NPC.lifeMax = (int)(NPC.lifeMax * 0.8f * balance);
		NPC.damage = (int)(NPC.damage * 0.9f);
	}

	public override void AI()
	{
		NPC parent = Parent;
		if (parent is null || !parent.active || parent.ModNPC is not SealedCongregationBoss congregation)
		{
			NPC.active = false;
			return;
		}

		Vector2 destination = congregation.GetSealDestination(NPC.whoAmI);
		NPC.Center = Vector2.Lerp(NPC.Center, destination, 0.16f);
		NPC.velocity = Vector2.Zero;
		NPC.rotation = (NPC.Center - parent.Center).ToRotation() + MathHelper.PiOver2;
		Lighting.AddLight(NPC.Center, new Vector3(0.08f, 0.34f, 0.31f));
	}

	public override bool CheckActive() => false;
	public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;
	public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position) => false;

	public override void HitEffect(NPC.HitInfo hit)
	{
		if (Main.dedServ || NPC.life > 0)
		{
			return;
		}

		for (int index = 0; index < 24; index++)
		{
			Dust dust = Dust.NewDustPerfect(NPC.Center, index % 2 == 0 ? DustID.Stone : DustID.DungeonSpirit,
				Main.rand.NextVector2Circular(5f, 5f), 100, new Color(78, 225, 205), 1.1f);
			dust.noGravity = index % 2 != 0;
		}
		SoundEngine.PlaySound(SoundID.Shatter, NPC.Center);
	}

	public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
	{
		bool broken = NPC.life <= NPC.lifeMax * BrokenThreshold;
		Texture2D texture = ModContent.Request<Texture2D>(broken
			? "SoulsOfTerra/Content/Bosses/SealedCongregation/SealedCongregation_seal_broken"
			: Texture).Value;
		Vector2 drawPosition = NPC.Center - screenPos;
		Vector2 origin = texture.Size() * 0.5f;
		if (broken)
		{
			float flicker = 0.18f + 0.1f * MathF.Sin(Main.GlobalTimeWrappedHourly * 12f + NPC.whoAmI);
			for (int direction = 0; direction < 4; direction++)
			{
				Vector2 offset = (MathHelper.TwoPi * direction / 4f).ToRotationVector2() * 2f;
				spriteBatch.Draw(texture, drawPosition + offset, null, new Color(54, 230, 207, 0) * flicker,
					NPC.rotation, origin, 1f, SpriteEffects.None, 0f);
			}
		}

		spriteBatch.Draw(texture, drawPosition, null, drawColor, NPC.rotation, origin, 1f, SpriteEffects.None, 0f);
		return false;
	}
}
