using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Buffs.Runeterra.Magic
{
    // PLACEHOLDER sprite (copy of Buffs/Runeterra/Summon/Conqueror) — replace with bespoke art.
    // Both Attunement.png and AttunementSheet.png are copies; the sheet is the 10-frame stack strip.

    /// <summary>
    /// Bearer of the Curse magic mechanic. Landing magic hits builds stacks that make casting cheaper,
    /// rather than hitting harder — magic's scarcity in Souls mode is mana, not damage, since passive
    /// regen is pinned off and every point comes from a Cerulean charge.
    ///
    /// Structurally a sibling of <see cref="Summon.Conqueror"/> (same stack cap, same decay-one-at-a-time
    /// falloff, same animated stack icon) but with its own counter so the two never share a tooltip or
    /// pay a hybrid build twice for one stack. The payout lives in tsorcRevampPlayer.ModifyManaCost.
    /// </summary>
    public class Attunement : ModBuff
    {
        public const int FrameCount = 10;
        private Asset<Texture2D> animatedTexture;

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
            if (Main.netMode != NetmodeID.Server)
            {
                // Do NOT load textures on the server!
                animatedTexture = ModContent.Request<Texture2D>(Texture + "Sheet");
            }
        }

        public override void Update(Player player, ref int buffIndex)
        {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            if (modPlayer.BotCAttunementStacks == 0)
            {
                modPlayer.BotCAttunementStacks = 1;
            }
            if (player.buffTime[buffIndex] == 1)
            {
                // Falls off one stack at a time rather than all at once, so a brief gap in casting
                // costs you a little efficiency instead of the whole ramp.
                if (modPlayer.BotCAttunementStacks > 1)
                {
                    modPlayer.BotCAttunementStacks--;
                    player.buffTime[buffIndex] = (int)(((float)modPlayer.BotCAttunementDuration / 6f) * 60f);
                }
                else
                {
                    modPlayer.BotCAttunementStacks = 0;
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/ConquerorFallOff") with { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.0107f }, player.Center);
                }
            }
        }

        /// <summary>
        /// Built at display time rather than baked into the localization string, so the numbers can
        /// never drift from the constants and the last line can report the player's live stack count
        /// and current discount.
        /// </summary>
        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            var modPlayer = Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>();
            int stacks = (int)modPlayer.BotCAttunementStacks;
            int perStack = (int)(modPlayer.BotCAttunementManaReduction * 100f);

            tip = LangUtils.GetTextValue(
                "Buffs.Attunement.Description",
                perStack,
                perStack * modPlayer.BotCAttunementMaxStacks,
                modPlayer.BotCAttunementMaxStacks,
                stacks,
                perStack * stacks);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, int buffIndex, ref BuffDrawParams drawParams)
        {
            // Frame of the stack strip matching the current stack count, same as Conqueror's icon.
            Texture2D ourTexture = animatedTexture.Value;
            Rectangle ourSourceRectangle = animatedTexture.Frame(
                verticalFrames: FrameCount,
                frameY: (int)Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().BotCAttunementStacks - 1);

            drawParams.Texture = ourTexture;
            drawParams.SourceRectangle = ourSourceRectangle;
            return true;
        }

        public override bool ReApply(Player player, int time, int buffIndex)
        {
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            if (modPlayer.BotCAttunementStacks < modPlayer.BotCAttunementMaxStacks)
            {
                modPlayer.BotCAttunementStacks++;
            }

            return false;
        }
    }
}
