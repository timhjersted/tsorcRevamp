using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Systems.Regain
{
    /// <summary>
    /// Every tunable for the regain (Bloodborne rally) system. Retuning the whole mechanic means
    /// editing this file and nothing else.
    ///
    /// Has its own config flag, independent of Spirit Ashes/SummonerRework - regain applies to any
    /// weapon on any class and has nothing to do with minions, so tying it to the summoner-specific
    /// toggle would hide it from the melee/ranged/mage players it is actually for.
    /// </summary>
    public static class Regain
    {
        /// <summary>True when regain should apply to this player: the config toggle is on AND they are
        /// Unkindled or a Bearer of the Curse. Classic mode is deliberately excluded - it stays the
        /// closest thing to vanilla combat, with no rally mechanic to lean on.</summary>
        public static bool Active(Player player)
        {
            if (player == null || !player.active)
            {
                return false;
            }

            if (!ModContent.GetInstance<tsorcRevampConfig>().RegainSystem)
            {
                return false;
            }

            tsorcRevampPlayer soulsPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            return soulsPlayer.Unkindled || soulsPlayer.BearerOfTheCurse;
        }

        /// <summary>HP recovered per point of the weapon's STATED damage (item.damage, not damage
        /// actually dealt). Overall generosity knob. Only binds when the weapon is weak relative to the
        /// pool - past early game the per-credit cap below is what limits recovery instead.
        ///
        /// Stated damage rather than damage dealt is deliberate: it ignores crits, buffs, enemy defense
        /// and multi-hit, so a damage-stacking build does not also stack healing into a feedback loop,
        /// and the number is stable enough to put in a tooltip.</summary>
        public const float DamageToRegainRate = 0.5f;

        /// <summary>Most of the pool a single credit can return, as a fraction of the pool's ORIGINAL
        /// size (not what currently remains - taking a fraction of the remainder would approach zero
        /// without ever arriving, and the pool would never fully refill).
        ///
        /// 0.25 = exactly 4 credits for a full recovery, at every point in progression. This is what
        /// makes the system progression-proof: weapon damage grows ~32x across a playthrough while max
        /// HP grows ~4x, so no flat rate could span that range on its own.</summary>
        public const float MaxPoolFractionPerHit = 0.25f;

        // There is deliberately NO tick-based cooldown between credits. Measured use times across this
        // mod's roster cluster at 22-27 ticks for EVERY class (melee 25, ranged 27, mage 22, summoner
        // 24), so any gate in that range silently swallows a large share of ordinary swings - which a
        // player reads as the mechanic failing to fire, not as a throttle.
        //
        // RegainPlayer dedupes per FRAME instead, which keeps the property a cooldown was wanted for (a
        // piercing shot or an AoE on a swarm credits once, not once per enemy) without ever skipping an
        // attack the player made.
        //
        // Fast weapons are therefore NOT penalised at all. Deliberate, pending playtest: item.damage
        // already gives heavy weapons more per credit, which may be sufficient on its own.

        /// <summary>Ticks the pool sits at full before it starts decaying. 300 = 5 seconds.
        ///
        /// For scale: 4 credits at one per 40 ticks is 160 ticks (2.67s) for a full recovery, so 5
        /// seconds leaves real room to reposition, close distance, or wait out an attack before
        /// counter-attacking - rather than demanding an unbroken chain of hits from the instant you are
        /// struck. The decay window after it is further margin for catching a partial.</summary>
        public const int HoldTicks = 300;

        /// <summary>Ticks the pool takes to decay from full to nothing once the hold expires. 90 = 1.5s.
        ///
        /// Linear, not exponential: linear is what makes "half decayed means half the pool" literally
        /// true. The vanilla-style health lag this sits next to drains exponentially and would be
        /// unreadable as a recoverable amount.</summary>
        public const int DecayTicks = 90;

        /// <summary>Largest pool a single hit can open, as a fraction of max HP. Stops one enormous boss
        /// hit from becoming a full heal.</summary>
        public const float MaxPoolFractionOfMaxLife = 0.25f;

        /// <summary>Extra damage a regain-eligible player takes, as a fraction. **Currently 0 - switched
        /// off, not deleted**, on the view that regain alone changes the fight enough without also making
        /// every hit hurt more.
        ///
        /// Raising it here is all that is needed to bring it back; RegainPlayer.ModifyHurt still applies
        /// it and still suppresses it in Master Mode, which already hits 50% harder than Expert. It
        /// belongs WITH regain if it returns - the increase alone is just a difficulty slider, and regain
        /// alone is a straight buff.</summary>
        public const float ExtraIncomingDamage = 0f;

        /// <summary>Ticks without being hit before the consecutive-hit counter resets. 240 = 4s.</summary>
        public const int ConsecutiveHitResetTicks = 240;

        /// <summary>The swing time a weapon is measured against. A weapon at exactly this speed behaves
        /// as the raw numbers describe (a quarter of the pool per hit, four hits to refill); anything
        /// slower recovers more per hit and anything faster less. 25 ticks is the roster's median across
        /// every class (melee 25, ranged 27, mage 22, summoner 24), so a typical weapon is the
        /// reference.</summary>
        public const int ReferenceUseAnimationTicks = 25;

        /// <summary>Clamp on the swing-time scale, so a pathologically slow or fast weapon cannot claim
        /// an absurd share of the pool in one hit. Parity holds exactly between these bounds and degrades
        /// gracefully outside them.</summary>
        public const float MinSwingTimeScale = 0.2f;
        public const float MaxSwingTimeScale = 2.5f;

        /// <summary>
        /// How much of a credit a weapon earns for its swing time, relative to a median weapon.
        ///
        /// This exists so attack speed does NOT decide how fast you recover. Credit scales with the time
        /// between swings while opportunities to credit scale inversely with it, so the two cancel and
        /// HP-per-second comes out the same whether you are swinging a greatsword or a machine gun. A
        /// slow weapon takes fewer, bigger bites; a fast one takes more, smaller ones; both refill a pool
        /// in about the same time.
        ///
        /// Without this the per-hit cap flattens every weapon's credit to the same value, at which point
        /// raw attack speed decides recovery outright and fast weapons win by 2-3x.
        ///
        /// Pass the ATTACK-SPEED-SCALED animation, not the raw one, or attack-speed buffs reintroduce the
        /// same advantage they are meant to be neutral against.
        /// </summary>
        public static float SwingTimeScale(float scaledUseAnimation)
        {
            if (scaledUseAnimation <= 0f)
            {
                return 1f;
            }

            float scale = scaledUseAnimation / ReferenceUseAnimationTicks;

            if (scale < MinSwingTimeScale)
            {
                return MinSwingTimeScale;
            }

            if (scale > MaxSwingTimeScale)
            {
                return MaxSwingTimeScale;
            }

            return scale;
        }

        /// <summary>How much of a hit is recoverable, by how many hits you have taken in quick
        /// succession. A single trade is fully recoverable - the behaviour to encourage. Being comboed
        /// is not - the behaviour to punish. One big hit you can answer, three in a row you cannot.</summary>
        public static float RecoverableFractionFor(int consecutiveHits)
        {
            if (consecutiveHits <= 1)
            {
                return 1f;
            }

            if (consecutiveHits == 2)
            {
                return 0.7f;
            }

            return 0.4f;
        }
    }
}
