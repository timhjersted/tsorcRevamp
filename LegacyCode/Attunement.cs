using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Summon;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.LegacyCode
{
    //Tim, you seem to have forgotten to actually add the stacks on projectile hits here.
    //If you want to readd this as some item or whatever, you're gonna have to add a globalprojectile class to this. 
    
    // PLACEHOLDER sprite (copy of Buffs/Runeterra/Summon/Conqueror) — replace with bespoke art.
    // Both Attunement.png and AttunementSheet.png are copies; the sheet is the 10-frame stack strip.

    /// <summary>
    /// Bearer of the Curse magic mechanic. Landing magic hits builds stacks that make casting cheaper,
    /// rather than hitting harder — magic's scarcity in Souls mode is mana, not damage, since passive
    /// regen is pinned off and every point comes from a Cerulean charge.
    ///
    /// Structurally a sibling of <see cref="Conqueror"/> (same stack cap, same decay-one-at-a-time
    /// falloff, same animated stack icon) but with its own counter so the two never share a tooltip or
    /// pay a hybrid build twice for one stack. The payout lives in tsorcRevampPlayer.ModifyManaCost.
    /// </summary>
    public class AttunementBuff : ModBuff
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
            var attunementPlayer = player.GetModPlayer<AttunementPlayer>();
            if (attunementPlayer.AttunementStacks == 0)
            {
                attunementPlayer.AttunementStacks = 1;
            }
            if (player.buffTime[buffIndex] == 1)
            {
                // Falls off one stack at a time rather than all at once, so a brief gap in casting
                // costs you a little efficiency instead of the whole ramp.
                if (attunementPlayer.AttunementStacks > 1)
                {
                    attunementPlayer.AttunementStacks--;
                    player.buffTime[buffIndex] = (int)(((float)attunementPlayer.AttunementDuration / 6f) * 60f);
                }
                else
                {
                    attunementPlayer.AttunementStacks = 0;
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
            var attunementPlayer = Main.LocalPlayer.GetModPlayer<AttunementPlayer>();
            int stacks = (int)attunementPlayer.AttunementStacks;
            int perStack = (int)(attunementPlayer.AttunementManaReduction * 100f);

            tip = LangUtils.GetTextValue(
                "Buffs.Attunement.Description",
                perStack,
                perStack * attunementPlayer.AttunementMaxStacks,
                attunementPlayer.AttunementMaxStacks,
                stacks,
                perStack * stacks);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, int buffIndex, ref BuffDrawParams drawParams)
        {
            // Frame of the stack strip matching the current stack count, same as Conqueror's icon.
            Texture2D ourTexture = animatedTexture.Value;
            Rectangle ourSourceRectangle = animatedTexture.Frame(
                verticalFrames: FrameCount,
                frameY: (int)Main.LocalPlayer.GetModPlayer<AttunementPlayer>().AttunementStacks - 1);

            drawParams.Texture = ourTexture;
            drawParams.SourceRectangle = ourSourceRectangle;
            return true;
        }

        public override bool ReApply(Player player, int time, int buffIndex)
        {
            var attunementPlayer = player.GetModPlayer<AttunementPlayer>();
            if (attunementPlayer.AttunementStacks < attunementPlayer.AttunementMaxStacks)
            {
                attunementPlayer.AttunementStacks++;
            }

            return false;
        }
    }

    public class AttunementPlayer : ModPlayer
    {
        public bool Enabled = false;

        public override void ResetEffects()
        {
            Enabled = false;
        }

        // Attunement (magic). Its own counter rather than sharing Conqueror's, so the two buffs can
        // each describe themselves accurately and a magic/summon hybrid isn't paid twice per stack.
        /// <summary>Mana cost reduction per stack. At the 10-stack cap this is 30% off, roughly 1.43x
        /// the casts out of one Cerulean charge — a real stretch without trivialising the flask economy.</summary>
        public float AttunementManaReduction = 0.03f;
        public int AttunementDuration = 4;
        public float AttunementStacks = 0;
        public int AttunementMaxStacks = 10;
        /// <summary>
        /// Attunement payout: Conqueror stacks make magic cheaper to cast.
        ///
        /// Restricted to magic so it can't quietly discount summon staves, which already benefit from
        /// the same stacks through Conqueror's damage bonus and would otherwise be paid twice.
        /// </summary>
        public override void ModifyManaCost(Item item, ref float reduce, ref float mult)
        {
            if (!Enabled || AttunementStacks <= 0 || item.DamageType != DamageClass.Magic)
            {
                return;
            }

            float reduction = Math.Min(
                AttunementStacks * AttunementManaReduction,
                AttunementMaxStacks * AttunementManaReduction);
            mult *= 1f - reduction;
        }
    }

    public class AttunementItem : GlobalItem
    {
        public override void OnHitNPC(Item item, Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Attunement: magic builds the same Conqueror stacks, but cashes them out as a mana-cost
            // reduction instead of summon damage (see tsorcRevampPlayer.ModifyManaCost). Magic's
            // scarcity in Souls mode is mana, not damage — passive regen is pinned off and every point
            // comes from a Cerulean charge — so the reward is more casts per charge, not bigger hits.
            var attunementPlayer = player.GetModPlayer<AttunementPlayer>();
            if (item.DamageType == DamageClass.Magic && player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                if (attunementPlayer.AttunementStacks < attunementPlayer.AttunementMaxStacks - 1)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/ConquerorStack") with { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.0054f }, player.Center);
                }
                else if (attunementPlayer.AttunementStacks == attunementPlayer.AttunementMaxStacks - 1)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/ConquerorFullyStacked") with { Volume = ModContent.GetInstance<tsorcRevampConfig>().BotCMechanicsVolume * 0.007f }, player.Center);
                }
                player.AddBuff(ModContent.BuffType<AttunementBuff>(), attunementPlayer.AttunementDuration * 60);
                if (hit.Crit)
                {
                    player.AddBuff(ModContent.BuffType<AttunementBuff>(), attunementPlayer.AttunementDuration * 60);
                }
            }
        }
    }
}
