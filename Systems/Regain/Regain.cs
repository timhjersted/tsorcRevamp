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

        /// <summary>How much of a weapon's own swing time must elapse before it can credit again, as a
        /// fraction of that weapon's ScaledUseAnimation. This replaces an earlier per-FRAME dedupe that
        /// only blocked two credits landing on the exact same tick: it did nothing for multishot pellets,
        /// a piercing hit ticking through several frames, or a channelled beam firing every tick, all of
        /// which could credit many times off one weapon activation.
        ///
        /// A flat tick-based cooldown was rejected for the same reason: measured use times cluster at
        /// 22-27 ticks across every class (melee 25, ranged 27, mage 22, summoner 24), so any single
        /// constant in that range clips whichever class's swing happens to be shorter than it. Gating on
        /// the CURRENT hit's own swing time sidesteps that - a weapon can never be blocked by its own next
        /// legitimate swing, only by a second hit landing before that weapon could have swung again, which
        /// is exactly what multishot/piercing/beam leftovers are: the attack already paid for, not a new
        /// one.
        ///
        /// 0.8 rather than 1.0 so a weapon that speeds up mid-fight (attack speed buff) is never blocked
        /// by a window measured before the buff applied.</summary>
        public const float CreditGateFraction = 0.8f;

        /// <summary>Hard floor on ticks between credits, applied on top of CreditGateFraction. Unlike that
        /// fraction - which only stops a SECOND credit from the SAME activation - this deliberately caps
        /// how many separate, legitimate activations can credit per second, for every weapon alike.
        ///
        /// The rest of this system is built so a weapon's own speed cancels out of its HP/sec (smaller
        /// credits, proportionally more often, same total) - that parity is intentional and correct for
        /// normal-speed weapons. This floor is the one place that parity is broken on purpose: a weapon
        /// faster than ~12 ticks/swing (attack-speed-scaled) stops converting extra speed into extra
        /// regain once it is credit-rate-limited by this floor instead of by its own swing time.
        ///
        /// 12 ticks (0.2s) is comfortably faster than every reference class's real swing time (22-27
        /// ticks), so it only ever engages for weapons well above normal attack speed - it is a speed
        /// limit on regain, not a nerf to typical play.</summary>
        public const int MinTicksBetweenCredits = 12;

        /// <summary>Ticks the pool sits at full before it starts decaying. 300 = 5 seconds.
        ///
        /// For scale: at ReferenceUseAnimationTicks (25), 4 credits is 100 ticks (1.67s) for a full
        /// recovery, so 5 seconds leaves real room to reposition, close distance, or wait out an attack
        /// before counter-attacking - rather than demanding an unbroken chain of hits from the instant you
        /// are struck. The decay window after it is further margin for catching a partial.</summary>
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
        /// gracefully outside them.
        ///
        /// MaxSwingTimeScale lowered from 2.5 to 1.5 after playtest data: Quad-Barrel Shotgun (useTime 55,
        /// scale ~2.2 at the old cap) was averaging 9.36 HP per credit against Laser Rifle's 4.41 in the
        /// same fight - a multi-pellet weapon's large useTime inflates BOTH the damage term and
        /// perCreditCap by the same factor, so a high useTime reads as "one big meaningful hit" even when
        /// it is really several smaller pellets landing together. 1.5 caps how far that inflation can go
        /// without touching anything at or below the reference speed.</summary>
        public const float MinSwingTimeScale = 0.2f;
        public const float MaxSwingTimeScale = 1.5f;

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

        /// <summary>Distance, in tiles, inside which a landed hit earns full regain value. Pulled back in
        /// from 16 to 9 after playtest - still past true melee/whip reach (~6), but 16 was giving
        /// comfortable mid-range ranged/magic play the same "danger zone" value as melee, which was too
        /// forgiving. The taper band (see NoRegainRangeTiles) shifted down by the same 7 tiles, so its
        /// width - and therefore its shape - is unchanged, only where it sits.</summary>
        public const float FullRegainRangeTiles = 9f;

        /// <summary>Distance, in tiles, beyond which regain value bottoms out at MinRangeMultiplier.
        /// Between this and FullRegainRangeTiles the value eases out along an S-curve rather than a
        /// straight line or a hard cliff - see RangeScale.</summary>
        public const float NoRegainRangeTiles = 23f;

        /// <summary>Floor on the falloff - never exactly zero, so a hit landed from far away still returns
        /// something rather than the mechanic silently doing nothing at extreme range.</summary>
        public const float MinRangeMultiplier = 0.05f;

        private const float TileSizeInPixels = 16f;

        /// <summary>How much of a landed hit's credit survives at the given distance from the target. This
        /// is the "stay in the danger zone" lever: full value out to FullRegainRangeTiles, an S-curve taper
        /// from there to NoRegainRangeTiles - gentle right past that range, steepest in the middle, gentle
        /// again near the floor, rather than an ease-out that punishes closing distance immediately - and
        /// a small floor beyond that so a true sniper still gets something, just not for free.</summary>
        public static float RangeScale(float distancePixels)
        {
            float distanceTiles = distancePixels / TileSizeInPixels;

            if (distanceTiles <= FullRegainRangeTiles)
            {
                return 1f;
            }

            if (distanceTiles >= NoRegainRangeTiles)
            {
                return MinRangeMultiplier;
            }

            float t = (distanceTiles - FullRegainRangeTiles) / (NoRegainRangeTiles - FullRegainRangeTiles);
            float smoothstep = 3f * t * t - 2f * t * t * t;
            float remaining = 1f - smoothstep;

            return MinRangeMultiplier + remaining * (1f - MinRangeMultiplier);
        }
    }
}
