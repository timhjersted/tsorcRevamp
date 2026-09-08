using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles;

namespace tsorcRevamp.Systems.Regain
{
    /// <summary>
    /// Bloodborne-style rally for Unkindled and Bearer of the Curse players: damage taken does not
    /// leave immediately, it sits as a recoverable pool that landing hits claws back.
    ///
    /// This is the counterweight to Souls Mode's healing scarcity. Without it the answer to "I'm low" is
    /// retreat and pray; with it the answer is fight back, and it costs no new heal button to say so.
    ///
    /// All tuning lives in Regain.cs. Gated by Regain.Active().
    /// </summary>
    public class RegainPlayer : ModPlayer
    {
        /// <summary>Source-level switch for this system's debug text, independent of the mod-wide
        /// DebugMode config toggle - another dev turning DebugMode on for something unrelated shouldn't
        /// also spam their feed with Regain diagnostics. Off by default; flip to true locally when
        /// actually diagnosing this system.</summary>
        private const bool ShowDebugMessages = false;

        /// <summary>HP currently recoverable. 0 means no pool is open.</summary>
        public float RegainPool;

        /// <summary>The pool's size when it opened. The per-credit cap is a fraction of THIS rather than
        /// of what remains - a fraction of the remainder would shrink toward zero without ever arriving,
        /// so the pool could never actually be refilled.</summary>
        public float RegainPoolOriginal;

        /// <summary>Ticks left at full before decay starts.</summary>
        public int HoldTicksRemaining;

        /// <summary>Ticks left of the linear decay. Zero (with HoldTicksRemaining zero) means expired.</summary>
        public int DecayTicksRemaining;

        /// <summary>Amount the pool loses per tick during decay. Fixed when decay begins so that credits
        /// taken mid-decay do not change how fast the rest drains.</summary>
        private float decayPerTick;

        /// <summary>Game frame of the last credit. One credit per frame is the whole dedupe rule: no
        /// weapon can attack twice within a single tick, so every swing, shot and cast still registers,
        /// while a piercing shot through five enemies or an AoE on a swarm resolves in one frame and
        /// therefore pays exactly once.
        ///
        /// ⚠ Do NOT replace this with attack identity derived from Player.itemAnimation. Channelled
        /// magic holds itemAnimation constant while casting and fast auto-reuse weapons reset it to max
        /// on the frame it hits zero, so the "new attack" edge never fires for them - melee keeps
        /// working while ranged and magic silently stop regaining entirely.</summary>
        private uint lastCreditFrame = uint.MaxValue;

        /// <summary>Hits taken in quick succession, driving the recoverable fraction.</summary>
        public int ConsecutiveHits;

        private int ticksSinceLastHurt;

        public bool HasPool => RegainPool > 0f;

        /// <summary>True once the hold has run out and the pool is draining. Drives the HUD colour.</summary>
        public bool IsDecaying => HasPool && HoldTicksRemaining <= 0;

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            if (!Regain.Active(Player))
            {
                return;
            }

            // Master Mode already multiplies enemy damage 3x against Expert's 2x - a 50% step up that
            // more than covers what this adds. Stacking both would put a Master player 70% over Expert,
            // which was never the intent: this exists to make regain matter in the difficulties that do
            // not already hit hard.
            if (Main.masterMode)
            {
                return;
            }

            modifiers.FinalDamage *= 1f + Regain.ExtraIncomingDamage;
        }

        public override void OnHurt(Player.HurtInfo info)
        {
            if (!Regain.Active(Player) || Player.whoAmI != Main.myPlayer)
            {
                return;
            }

            if (info.Damage <= 0)
            {
                return;
            }

            if (ticksSinceLastHurt <= Regain.ConsecutiveHitResetTicks)
            {
                ConsecutiveHits++;
            }
            else
            {
                ConsecutiveHits = 1;
            }

            ticksSinceLastHurt = 0;

            // A new hit REPLACES the pool rather than stacking onto it, and each hit in quick succession
            // makes less of the damage recoverable. Single trades stay fully recoverable; being comboed
            // does not.
            float recoverable = info.Damage * Regain.RecoverableFractionFor(ConsecutiveHits);
            float poolCap = Player.statLifeMax2 * Regain.MaxPoolFractionOfMaxLife;

            RegainPool = Math.Min(recoverable, poolCap);
            RegainPoolOriginal = RegainPool;

            HoldTicksRemaining = Regain.HoldTicks;
            DecayTicksRemaining = Regain.DecayTicks;
            decayPerTick = RegainPool / Regain.DecayTicks;

            if (ShowDebugMessages)
            {
                Main.NewText($"[Regain] pool opened: {(int)RegainPool} (hit for {info.Damage}, consecutive {ConsecutiveHits})", 160, 255, 200);
            }
        }

        // Dying is handled in UpdateDead(), not here - vanilla's Player.Update(int) checks
        // `if (dead) { UpdateDead(); ...; return; }` BEFORE the line that calls PostUpdate, so PostUpdate
        // never runs at all while Player.dead is true. Checking Player.dead here was silent dead code:
        // the pool just sat frozen (not decaying, not clearable) for as long as the player stayed dead,
        // reopening a small window to cash in a pre-death pool immediately on respawn.
        public override void UpdateDead()
        {
            ClearPool();
        }

        public override void PostUpdate()
        {
            if (ticksSinceLastHurt < int.MaxValue - 1)
            {
                ticksSinceLastHurt++;
            }

            // Leaving the mode should not leave a pool sitting around to be cashed in later. The dying
            // half of this used to live here too - see UpdateDead() above for why that never fired.
            if (!Regain.Active(Player))
            {
                ClearPool();
                return;
            }

            if (!HasPool)
            {
                return;
            }

            // The pool can never be worth more than the health actually missing. Without this, healing
            // from another source (Estus, a lifesteal weapon) while a pool stands would leave a segment
            // drawn past the right-hand end of the health bar, and credits that heal nothing.
            float missingLife = Player.statLifeMax2 - Player.statLife;

            if (RegainPool > missingLife)
            {
                RegainPool = Math.Max(0f, missingLife);

                if (!HasPool)
                {
                    ClearPool();
                    return;
                }
            }

            if (HoldTicksRemaining > 0)
            {
                HoldTicksRemaining--;
                return;
            }

            RegainPool -= decayPerTick;
            DecayTicksRemaining--;

            if (RegainPool <= 0f || DecayTicksRemaining <= 0)
            {
                ClearPool();
            }
        }

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            TryCredit(item.damage, ScaledUseAnimation(item), target, isMinionOrSentry: false);
        }

        /// <summary>
        /// A weapon's real time between swings, in ticks - its listed useAnimation divided by the
        /// player's live attack speed for that damage class.
        ///
        /// Scaled rather than raw so attack-speed bonuses do not turn into faster recovery: they shorten
        /// the swing AND shrink the credit by the same factor, which is what keeps regain neutral to how
        /// fast you happen to be attacking.
        /// </summary>
        private float ScaledUseAnimation(Item weapon)
        {
            float attackSpeed = Player.GetAttackSpeed(weapon.DamageType);

            if (attackSpeed <= 0f)
            {
                return weapon.useAnimation;
            }

            return weapon.useAnimation / attackSpeed;
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Minions and sentries deal damage but must never heal their owner - your pets tank for you,
            // only you can heal you. This is the one exclusion the damage-based model does not get for
            // free, since (unlike stamina cost) minions plainly do have damage.
            bool isMinionOrSentry = proj.minion
                || proj.sentry
                || proj.GetGlobalProjectile<tsorcGlobalProjectile>().WeaponStaminaSourceIsSummon;

            int sourceItemType = proj.GetGlobalProjectile<tsorcGlobalProjectile>().WeaponStaminaSourceItemType;

            if (sourceItemType > 0 && ContentSamples.ItemsByType.TryGetValue(sourceItemType, out Item sourceWeapon))
            {
                TryCredit(sourceWeapon.damage, ScaledUseAnimation(sourceWeapon), target, isMinionOrSentry);
                return;
            }

            // No originating weapon - a projectile spawned by an accessory or buff rather than an item
            // use. Credit it at reference speed off its own damage; rare, and the per-hit cap bounds it.
            TryCredit(proj.damage, Regain.ReferenceUseAnimationTicks, target, isMinionOrSentry);
        }

        /// <summary>
        /// Converts a landed hit into recovered health, if everything lines up: the rework is on, a pool
        /// is open, the credit cooldown has elapsed, the target is a real threat, and the hit did not
        /// come from a minion.
        /// </summary>
        /// <summary>
        /// Diagnostic for "regain isn't firing". Prints the first refusal reason per hit while
        /// ShowDebugMessages is on, because every gate in TryCredit fails silently by design and there
        /// is otherwise no way to tell "the rework is off" from "the pool expired" from "this target is
        /// filtered out".
        /// </summary>
        private static void DebugRefusal(string reason)
        {
            if (!ShowDebugMessages)
            {
                return;
            }

            Main.NewText($"[Regain] no credit: {reason}", 255, 160, 160);
        }

        private void TryCredit(int statedDamage, float scaledUseAnimation, NPC target, bool isMinionOrSentry)
        {
            if (Player.whoAmI != Main.myPlayer)
            {
                return;
            }

            if (isMinionOrSentry)
            {
                DebugRefusal("hit came from a minion/sentry");
                return;
            }

            if (!Regain.Active(Player))
            {
                DebugRefusal("Regain inactive (config off, or not Unkindled/Bearer of the Curse)");
                return;
            }

            if (!HasPool)
            {
                DebugRefusal("no pool open (take a hit first, or it already expired)");
                return;
            }

            // One credit per frame. Every attack the player makes registers; a single attack that touches
            // several enemies resolves in one frame and so still pays once.
            if (Main.GameUpdateCount == lastCreditFrame)
            {
                return;
            }

            // Threat filter: stop a punching bag parked in the player's arena from being a free heal.
            //
            // ⚠ Do NOT use `npc.damage <= 0` as the "is this real" test, however tempting. This mod's
            // puppet/invader enemies (DreadWraith, Hero of Lumelia, Gwyn, Witchking and the rest of
            // NPCs/Puppets) deliberately set NPC.damage = 0 and deliver everything through weapon
            // hitboxes and projectiles instead - so a damage-based filter silently excludes some of the
            // hardest fights in the game. That is exactly what it did on first playtest.
            //
            // Name what is actually being excluded instead: target dummies and critters.
            if (!UsefulFunctions.IsHostileThreat(target))
            {
                DebugRefusal($"target filtered out (friendly={target?.friendly}, invulnerable={target?.dontTakeDamage}, critter={target?.CountsAsACritter})");
                return;
            }

            if (statedDamage <= 0)
            {
                DebugRefusal("weapon reported 0 stated damage");
                return;
            }

            // Swing time scales the cap as well as the damage term. Scaling only the damage would achieve
            // nothing: the cap clips almost every weapon to the same value, and whatever survives that
            // clip is then multiplied by attack rate - which is precisely how fast weapons ended up
            // recovering 2-3x faster than slow ones. Scaling both makes HP-per-second come out equal.
            float swingScale = Regain.SwingTimeScale(scaledUseAnimation);

            float perCreditCap = RegainPoolOriginal * Regain.MaxPoolFractionPerHit * swingScale;
            float credit = Math.Min(statedDamage * Regain.DamageToRegainRate * swingScale, perCreditCap);
            credit = Math.Min(credit, RegainPool);

            int healed = (int)Math.Round(credit);

            if (healed <= 0)
            {
                return;
            }

            RegainPool -= healed;
            lastCreditFrame = Main.GameUpdateCount;

            Player.statLife = Math.Min(Player.statLife + healed, Player.statLifeMax2);
            Player.HealEffect(healed, true);

            if (RegainPool <= 0f)
            {
                ClearPool();
            }
        }

        /// <summary>
        /// Removes health from the standing pool without opening one or disturbing its timers. Called by
        /// shield chip damage, which bypasses Player.Hurt entirely: blocking should bleed away recovery
        /// rather than preserve it, but it must not reset the window or count toward the consecutive-hit
        /// penalty the way a real hit does.
        /// </summary>
        public void ReducePool(int amount)
        {
            if (amount <= 0 || !HasPool)
            {
                return;
            }

            RegainPool -= amount;

            if (RegainPool <= 0f)
            {
                ClearPool();
            }
        }

        private void ClearPool()
        {
            RegainPool = 0f;
            RegainPoolOriginal = 0f;
            HoldTicksRemaining = 0;
            DecayTicksRemaining = 0;
            decayPerTick = 0f;
        }
    }
}
