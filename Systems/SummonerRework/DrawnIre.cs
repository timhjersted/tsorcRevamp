using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Systems.SummonerRework
{
    /// <summary>
    /// An enemy currently engaged by one of the player's spirit ashes hits the player less hard. Two
    /// independent tiers, set from two different places:
    ///   MELEE (stronger) - a minion's own body is in physical contact with the enemy. Set by
    ///   SpiritAshesMinions.HandleEnemyContact, refreshed every frame of contact.
    ///   RANGED (weaker) - a minion or sentry's own ranged attack (not its body, not a whip) landed a
    ///   hit on the enemy. Set by SpiritAshesMinions.OnHitNPC.
    /// If both are active on the same enemy at once, only the stronger tier applies - see
    /// ModifyHitPlayer - rather than stacking, matching the existing "no number of ashes reaches
    /// immunity" rule that governs the rest of this system.
    ///
    /// This is what makes keeping minions alive worth the mana: it produces the felt result of aggro -
    /// your spirits keep the thing off you - without touching enemy targeting, which would mean
    /// threading an abstract target through SF4, FighterAI and tsorcRevampAIs with no tests behind them.
    ///
    /// A REDUCTION on the enemy, not a reassignment of the hit. Damage never teleports to a minion
    /// standing elsewhere, and stacking minions cannot produce immunity.
    /// </summary>
    public class DrawnIre : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        /// <summary>Ticks of the MELEE (stronger) mark left. Refreshed every frame an ash is touching
        /// this enemy, so it only decays once they disengage or die.</summary>
        public int MeleeMarkTicks;

        /// <summary>Ticks of the RANGED (weaker) mark left. Refreshed on every landed ranged hit, same
        /// decay behaviour as the melee tier.</summary>
        public int RangedMarkTicks;

        public bool IsMarked => MeleeMarkTicks > 0 || RangedMarkTicks > 0;

        public override void ResetEffects(NPC npc)
        {
            if (MeleeMarkTicks > 0)
            {
                MeleeMarkTicks--;
            }

            if (RangedMarkTicks > 0)
            {
                RangedMarkTicks--;
            }

            // Faint wisps so the player can tell which enemies their ashes have hold of, and which
            // tier: yellow for MELEE (the stronger, contact tier - matches its priority over ranged in
            // ModifyHitPlayer when both are active), white for RANGED alone. Sparse on purpose - this
            // can be true for several enemies at once in a crowd.
            if (IsMarked && !Main.dedServ && Main.rand.NextBool(14))
            {
                Color wispColor = Color.White;

                if (MeleeMarkTicks > 0)
                {
                    wispColor = Color.Yellow;
                }

                Dust wisp = Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.Cloud,
                    0f, -0.6f, 150, wispColor, 0.9f);
                wisp.noGravity = true;
            }
        }

        public override void ModifyHitPlayer(NPC npc, Player target, ref Player.HurtModifiers modifiers)
        {
            if (!SummonerRework.Active(target))
            {
                return;
            }

            // Melee takes priority over ranged rather than stacking with it - an enemy already being
            // engaged in melee gets the full reduction regardless of whether a ranged ash also tagged it
            // recently.
            if (MeleeMarkTicks > 0)
            {
                modifiers.FinalDamage *= 1f - SummonerRework.DrawnIreDamageReduction;
            }
            else if (RangedMarkTicks > 0)
            {
                modifiers.FinalDamage *= 1f - SummonerRework.DrawnIreRangedDamageReduction;
            }
        }

        public override void DrawEffects(NPC npc, ref Color drawColor)
        {
            if (!IsMarked)
            {
                return;
            }

            Color whiteWithOriginalAlpha = new Color(255, 255, 255, drawColor.A);
            drawColor = Color.Lerp(drawColor, whiteWithOriginalAlpha, SummonerRework.DrawnIreWhiteTintStrength);
        }
    }
}
