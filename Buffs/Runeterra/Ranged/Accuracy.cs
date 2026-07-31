using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Buffs.Runeterra.Ranged
{
    // PLACEHOLDER sprite (copy of Buffs/Runeterra/Magic/Attunement) — replace with bespoke art.
    // Both Accuracy.png and AccuracySheet.png are copies; the sheet is the 10-frame band strip.

    /// <summary>
    /// Readout for Bearer of the Curse's ranged mechanic. Unlike its three siblings (Conqueror, Lethal Tempo,
    /// Attunement) the underlying mechanic is NOT stack-based — accuracy is a continuously varying 0-100%
    /// tracked in tsorcRevampPlayer.BotCCurrentAccuracyPercent, gained on every projectile that connects and
    /// lost at double rate on every one that doesn't.
    ///
    /// That continuous value is why it had no buff for so long: a 0-100% meter doesn't map onto a stack icon.
    /// It's displayed here in ten 10% BANDS, which fit the same 10-frame strip the other mechanics use, and
    /// which each correspond to exactly 1% flat crit. The exact percentage lives in the tooltip below, so it's
    /// permanently readable rather than only visible in combat text as it scrolls past.
    ///
    /// This buff is display-only. The payout lives in tsorcRevampPlayer (crit chance and crit damage both
    /// scale off the meter), and the meter itself is driven from tsorcGlobalProjectile.
    /// </summary>
    public class Accuracy : ModBuff
    {
        public const int FrameCount = 10;
        private Asset<Texture2D> animatedTexture;

        /// <summary>Which 10% band a raw 0-1 accuracy value falls into. 100% clamps into the top band rather
        /// than overflowing to a non-existent 11th frame.</summary>
        public static int BandOf(float accuracyPercent)
        {
            int band = (int)(accuracyPercent * FrameCount);
            return band < 0 ? 0 : (band > FrameCount - 1 ? FrameCount - 1 : band);
        }

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = true; // a permanent meter has no meaningful countdown to show
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
            if (Main.netMode != NetmodeID.Server)
            {
                // Do NOT load textures on the server!
                animatedTexture = ModContent.Request<Texture2D>(Texture + "Sheet");
            }
        }

        /// <summary>
        /// Built at display time rather than baked into the localization string, so the numbers can never drift
        /// from the constants and the last line can report the player's live accuracy and what it's currently
        /// granting — the persistent readout this mechanic never had.
        /// </summary>
        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            var modPlayer = Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>();
            float accuracy = modPlayer.BotCCurrentAccuracyPercent;

            tip = LangUtils.GetTextValue(
                "Buffs.Accuracy.Description",
                (int)(modPlayer.BotCAccuracyGain * 100f),
                (int)(modPlayer.BotCAccuracyLoss * 100f),
                (int)modPlayer.BotCAccuracyMaxFlatCrit,
                (int)(modPlayer.BotCAccuracyMaxCritMult * 100f),
                (int)(accuracy * 100f),
                (accuracy * modPlayer.BotCAccuracyMaxFlatCrit).ToString("0.0"),
                (int)(accuracy * modPlayer.BotCAccuracyMaxCritMult * 100f));
        }

        public override bool PreDraw(SpriteBatch spriteBatch, int buffIndex, ref BuffDrawParams drawParams)
        {
            // Frame of the band strip matching the current 10% band, same idea as Conqueror's stack icon.
            Texture2D ourTexture = animatedTexture.Value;
            Rectangle ourSourceRectangle = animatedTexture.Frame(
                verticalFrames: FrameCount,
                frameY: BandOf(Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().BotCCurrentAccuracyPercent));

            drawParams.Texture = ourTexture;
            drawParams.SourceRectangle = ourSourceRectangle;
            return true;
        }
    }
}
