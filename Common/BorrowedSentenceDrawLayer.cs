using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SoulsOfTerra.Players;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace SoulsOfTerra.Common;

public class BorrowedSentenceDrawLayer : PlayerDrawLayer
{
	public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.BackAcc);

	public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
	{
		return drawInfo.drawPlayer.GetModPlayer<BorrowedSentencePlayer>().SentenceActive;
	}

	protected override void Draw(ref PlayerDrawSet drawInfo)
	{
		Player player = drawInfo.drawPlayer;
		BorrowedSentencePlayer sentence = player.GetModPlayer<BorrowedSentencePlayer>();
		Texture2D intactTexture = ModContent.Request<Texture2D>(
			"SoulsOfTerra/Content/Bosses/SealedCongregation/SealedCongregation_seal").Value;
		Texture2D brokenTexture = ModContent.Request<Texture2D>(
			"SoulsOfTerra/Content/Bosses/SealedCongregation/SealedCongregation_seal_broken").Value;

		float urgency = 1f - sentence.TrialTimeRemaining / (float)BorrowedSentencePlayer.TrialDuration;
		float pulse = 1f + (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * (5f + urgency * 8f)) * (0.035f + urgency * 0.045f);
		float opacity = MathHelper.Lerp(0.7f, 1f, urgency);
		Color color = Color.Lerp(new Color(86, 226, 210), new Color(255, 105, 120), urgency * urgency) * opacity;
		Vector2 position = player.Center - Main.screenPosition + new Vector2(0f, -48f);

		// Repayment opens the seal while the accelerating pulse communicates its deadline.
		float scale = MathHelper.Lerp(0.5f, 0.34f, sentence.RepaymentProgress) * pulse;
		float rotation = Main.GlobalTimeWrappedHourly * 0.22f;
		Color intactColor = color * (1f - sentence.RepaymentProgress * 0.8f);
		drawInfo.DrawDataCache.Add(new DrawData(intactTexture, position, null, intactColor, rotation,
			intactTexture.Size() * 0.5f, scale, SpriteEffects.None, 0));

		// The authored broken variant increasingly replaces the intact seal as absolution nears.
		Color brokenColor = color * sentence.RepaymentProgress;
		drawInfo.DrawDataCache.Add(new DrawData(brokenTexture, position, null, brokenColor, rotation,
			brokenTexture.Size() * 0.5f, scale, SpriteEffects.None, 0));
	}
}
