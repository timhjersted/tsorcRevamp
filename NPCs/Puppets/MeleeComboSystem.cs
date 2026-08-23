using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Puppets
{
    /// <summary>
    /// Weapon archetypes that map an item's playstyle to a set of attack combos.
    /// Detected automatically from item useStyle/damage class, or overridden per puppet.
    /// </summary>
    public enum WeaponArchetype
    {
        None,
        // Melee
        Broadsword,
        Greatsword,
        Hammer,
        Axe,
        Dagger,
        Whip,
        Flail,
        Katana,
        Rapier,
        Halberd,
        TwinDaggers,
        // Ranged
        Throwables,
        Bow,
        Crossbow,
        Boomerang,
    }

    /// <summary>
    /// How the weapon moves through a single combo step.  Drives both the weapon-rotation
    /// lerp and the puppet body-row selection in PuppetNPC.
    /// </summary>
    public enum ComboMotion
    {
        OverheadArc,      // raised behind head -> swung down-forward
        UnderhandArc,     // dipped low -> swung up-forward
        HorizontalSweep,  // flat side-to-side sweep
        VerticalChop,     // straight overhead -> straight down (hammer)
        Thrust,           // dip -> snap horizontal (stab/spear)
        JoustDash,        // thrust + forward velocity push (lunge)
        Spin,             // looped 0->2pi rotation (flail/whirlwind)
        IaidoDraw,        // held low + behind -> fast snap forward
        GroundSlam,       // overhead -> past vertical with shockwave
        LeapSlam,         // jump toward player with the axe raised, slam down + hit on landing
        LeapThrust,       // jump toward player with spear leveled forward, thrust on landing
        ChargeChop,       // run at the player; ends on contact or a timeout, next step is the chop
        Feint,            // raise + flash as a bait, hold without swinging; the next step is the real (delayed) chop
        DoubleSpinSlam,   // two complete overhead circles, then a final downward ground strike
        ThrownWeaponRetrieve, // circular throw telegraph, wait for embed, leap to retrieve the weapon
        LowAxeRun,        // sprint with the arm and axe carried low and behind
        RisingUppercutLeap, // rising axe cut that launches the puppet, then holds overhead while falling
        BackstepRaise,    // hop away while visibly raising the weapon; next step is the re-entry strike
        ApexDiveCleave,   // high jump, limited tracking on ascent, then an active descending blade sweep
        // Ranged motions reuse RangedStyle on PuppetNPC; ranged combos pick at burst level
    }

    public enum ComboRangeBand
    {
        Close, // melee reach
        Mid,   // stab / dash range
        Far,   // ranged / spear
        Any
    }

    public struct MeleeComboStep
    {
        public ComboMotion Motion;
        public int   TelegraphTicks;   // only step 0's telegraph is "real"; later steps use PostStepPause as their telegraph
        public int   AttackTicks;
        public int   PostStepPause;    // pause AFTER this step before the next step's attack
        public float DamageMult;
        public float ReachMult;
        public float ForwardPushMult;  // 0 = stationary; >0 = hop/dash scaled by TopSpeed
        /// <summary>How fast the visual arc sweeps, for aim-swing pilot puppets only. >1 = faster
        /// (shorter swing window), &lt;1 = slower. The swing then plays over AttackTicks / SwingSpeedMult
        /// instead of being floored to the weapon's useAnimation. 1 = play over AttackTicks as-is.</summary>
        public float SwingSpeedMult;
        /// <summary>Easing shape of the arc, for aim-swing pilot puppets only. Lets different steps
        /// feel different (snappy flick vs heavy settle vs delayed whip) at the same duration.</summary>
        public SwingEaseStyle Ease;
        /// <summary>Per-step launch shaping for leap motions. Values at or below zero are treated as
        /// 1 so existing combo tables retain their current movement without migration.</summary>
        public float LeapHeightMult;
        public float LeapForwardSpeedMult;
    }

    public struct MeleeCombo
    {
        public string         Name;
        public MeleeComboStep[] Steps;
        /// <summary>Optional single-clock V2 clip. When present, PuppetNPC bypasses the legacy
        /// combo phase choreography for this attack while preserving the shared picker and renderer.</summary>
        public PuppetAttackClip RuntimeV2Clip;
        public int            BaseWeight;
        public ComboRangeBand Preferred;
        public Color          InitialFlashColor;
        public int            CooldownAfterUse;
        /// <summary>Recovery ticks after this combo's final step, i.e. how long the puppet is
        /// committed and punishable before it can act again. 0 = use the puppet's flat
        /// <c>MeleeRecoveryTicks</c>. This is the main lever for attack RHYTHM: a light swing that
        /// recovers in 14 ticks and re-threatens feels nothing like a heavy that leaves a 48-tick
        /// opening, and a moveset where every attack recovers identically reads as flat no matter
        /// how varied the swings themselves are.</summary>
        public int            RecoveryTicks;
        public bool           HeavyCommit;   // HP-escalation multiplier applies to these
        public bool           HyperArmor;    // commit (no stagger) holds through inter-step pauses, not just active frames
        /// <summary>Only eligible outside normal melee engagement range. Used for committed
        /// gap-closing or physical thrown-weapon openers that begin at range.</summary>
        public bool           RangedStartOnly;
        /// <summary>Per-tick horizontal velocity brake while this combo winds up / swings, for
        /// aim-swing pilot puppets only. 0 = keep full momentum (no slowdown), 0.4 = plant the feet
        /// for a heavy commit. Replaces the old blanket SlowDown() so light attacks stay mobile.</summary>
        public float          MoveBrake;
    }

    public struct RangedComboShot
    {
        public int   PauseBefore;    // pause before this shot fires (0 for first shot)
        public float SpreadDegrees;
        public float SpeedMult;      // multiplier on base shoot speed
        public int   ProjectileCount; // shots fired this step (fan)
    }

    public struct RangedCombo
    {
        public string         Name;
        public RangedComboShot[] Shots;
        public int            BaseWeight;
        public ComboRangeBand Preferred;
        public Color          InitialFlashColor;
        public int            TelegraphExtra; // added to base telegraph
        public int            CooldownAfterUse;
        public bool           HeavyCommit;
    }

    public static class WeaponArchetypeTables
    {
        // ─────────────────────────────────────────────────────────────────────
        // Item -> archetype detection
        // ─────────────────────────────────────────────────────────────────────

        public static WeaponArchetype DetectMelee(Item item)
        {
            if (item == null || item.IsAir) return WeaponArchetype.None;

            // Whips
            if (item.DamageType == DamageClass.SummonMeleeSpeed) return WeaponArchetype.Whip;

            // Thrown / throwable melee (Swing + noMelee + shoots projectile)
            if (item.useStyle == ItemUseStyleID.Swing && item.noMelee && item.shoot > ProjectileID.None)
                return WeaponArchetype.None; // ranged path will pick this up as Throwables

            if (item.useStyle == ItemUseStyleID.Swing)
            {
                if (item.hammer > 0) return WeaponArchetype.Hammer;
                if (item.damage >= 70 && item.useTime >= 28) return WeaponArchetype.Greatsword;
                if (item.useTime <= 14 && item.damage <= 30)  return WeaponArchetype.Dagger;
                return WeaponArchetype.Broadsword;
            }

            if (item.useStyle == ItemUseStyleID.Thrust)
            {
                // Vanilla "Rapier" useStyle is a forward thrust pose; long polearms also use this.
                if (item.useTime <= 18) return WeaponArchetype.Rapier;
                return WeaponArchetype.Halberd;
            }

            return WeaponArchetype.None;
        }

        public static WeaponArchetype DetectRanged(Item item)
        {
            if (item == null || item.IsAir) return WeaponArchetype.None;

            // Throwable melee (e.g. AbyssalStar): Swing + noMelee + shoots
            if (item.useStyle == ItemUseStyleID.Swing && item.noMelee && item.shoot > ProjectileID.None)
                return WeaponArchetype.Throwables;

            if (item.useStyle == ItemUseStyleID.Shoot)
            {
                if (item.useAmmo == AmmoID.Arrow)  return WeaponArchetype.Bow;
                if (item.useAmmo == AmmoID.Bullet) return WeaponArchetype.Crossbow;
                if (item.useAmmo > 0)              return WeaponArchetype.Crossbow;
                // No ammo: probably a magic staff or boomerang-style item.
                return WeaponArchetype.Throwables;
            }

            return WeaponArchetype.None;
        }

        public static bool IsMeleeArchetype(WeaponArchetype a) =>
            a >= WeaponArchetype.Broadsword && a <= WeaponArchetype.TwinDaggers;

        public static bool IsRangedArchetype(WeaponArchetype a) =>
            a >= WeaponArchetype.Throwables && a <= WeaponArchetype.Boomerang;

        // ─────────────────────────────────────────────────────────────────────
        // Combo lookup
        // ─────────────────────────────────────────────────────────────────────

        public static MeleeCombo[] GetMeleeCombos(WeaponArchetype a) => a switch
        {
            WeaponArchetype.Broadsword   => Broadsword,
            WeaponArchetype.Greatsword   => Greatsword,
            WeaponArchetype.Hammer       => Hammer,
            WeaponArchetype.Axe          => Axe,
            WeaponArchetype.Dagger       => Dagger,
            WeaponArchetype.Whip         => Whip,
            WeaponArchetype.Flail        => Flail,
            WeaponArchetype.Katana       => Katana,
            WeaponArchetype.Rapier       => Rapier,
            WeaponArchetype.Halberd      => Halberd,
            WeaponArchetype.TwinDaggers  => TwinDaggers,
            _ => null
        };

        public static RangedCombo[] GetRangedCombos(WeaponArchetype a) => a switch
        {
            WeaponArchetype.Throwables => ThrowablesCombos,
            WeaponArchetype.Bow        => BowCombos,
            WeaponArchetype.Crossbow   => CrossbowCombos,
            WeaponArchetype.Boomerang  => BoomerangCombos,
            _ => null
        };

        // ─────────────────────────────────────────────────────────────────────
        // Range/HP-aware combo picker
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the index of a melee combo from the archetype's pool, weighted by:
        ///   - Base weight
        ///   - Range band match (combos preferring the current band get x2.0, mismatched bands x0.4)
        ///   - HP escalation: hpFrac <= 0.66 -> HeavyCommit x1.5; <= 0.33 -> HeavyCommit x2.5
        /// Returns -1 if no combo is eligible (all weights zero).
        /// </summary>
        public static int PickCombo(MeleeCombo[] pool, float dist, float hpFrac, float closeMax, float midMax)
        {
            if (pool == null || pool.Length == 0) return -1;

            ComboRangeBand band = dist <= closeMax ? ComboRangeBand.Close
                               : dist <= midMax   ? ComboRangeBand.Mid
                               : ComboRangeBand.Far;

            float heavyMult = hpFrac <= 0.33f ? 2.5f
                            : hpFrac <= 0.66f ? 1.5f
                            : 1.0f;

            int total = 0;
            int[] weights = new int[pool.Length];
            for (int i = 0; i < pool.Length; i++)
            {
                float w = pool[i].BaseWeight;
                if (pool[i].Preferred == band || pool[i].Preferred == ComboRangeBand.Any)
                    w *= 2.0f;
                else
                    w *= 0.4f;
                if (pool[i].HeavyCommit)
                    w *= heavyMult;
                weights[i] = (int)w;
                total += weights[i];
            }
            if (total <= 0) return -1;

            int roll = Main.rand.Next(total);
            int cumulative = 0;
            for (int i = 0; i < pool.Length; i++)
            {
                cumulative += weights[i];
                if (roll < cumulative) return i;
            }
            return pool.Length - 1;
        }

        public static int PickRangedCombo(RangedCombo[] pool, float dist, float hpFrac, float closeMax, float midMax)
        {
            if (pool == null || pool.Length == 0) return -1;

            ComboRangeBand band = dist <= closeMax ? ComboRangeBand.Close
                               : dist <= midMax   ? ComboRangeBand.Mid
                               : ComboRangeBand.Far;

            float heavyMult = hpFrac <= 0.33f ? 2.5f
                            : hpFrac <= 0.66f ? 1.5f
                            : 1.0f;

            int total = 0;
            int[] weights = new int[pool.Length];
            for (int i = 0; i < pool.Length; i++)
            {
                float w = pool[i].BaseWeight;
                if (pool[i].Preferred == band || pool[i].Preferred == ComboRangeBand.Any)
                    w *= 2.0f;
                else
                    w *= 0.4f;
                if (pool[i].HeavyCommit)
                    w *= heavyMult;
                weights[i] = (int)w;
                total += weights[i];
            }
            if (total <= 0) return -1;

            int roll = Main.rand.Next(total);
            int cumulative = 0;
            for (int i = 0; i < pool.Length; i++)
            {
                cumulative += weights[i];
                if (roll < cumulative) return i;
            }
            return pool.Length - 1;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Step shorthand builders
        // ─────────────────────────────────────────────────────────────────────

        private static MeleeComboStep S(ComboMotion m, int tel, int atk, int pause,
                                        float dmg = 1f, float reach = 1f, float push = 0f,
                                        float swingMult = 1f, SwingEaseStyle ease = SwingEaseStyle.Smooth)
            => new MeleeComboStep
            {
                Motion = m, TelegraphTicks = tel, AttackTicks = atk,
                PostStepPause = pause, DamageMult = dmg, ReachMult = reach,
                ForwardPushMult = push, SwingSpeedMult = swingMult, Ease = ease
            };

        private static RangedComboShot R(int pause, float spread = 0f, float speed = 1f, int count = 1)
            => new RangedComboShot { PauseBefore = pause, SpreadDegrees = spread, SpeedMult = speed, ProjectileCount = count };

        // ─────────────────────────────────────────────────────────────────────
        // MELEE ARCHETYPE TABLES (5 combos each)
        // Colors: white=quick, cyan=combo, yellow=committed, orange=dash, red=heavy
        // ─────────────────────────────────────────────────────────────────────

        public static readonly MeleeCombo[] Broadsword = new[]
        {
            new MeleeCombo {
                Name = "Quickslash", BaseWeight = 100, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.White, CooldownAfterUse = 40,
                Steps = new[] { S(ComboMotion.OverheadArc, 16, 22, 0) }
            },
            new MeleeCombo {
                Name = "Under-Over", BaseWeight = 80, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.LightYellow, CooldownAfterUse = 90,
                Steps = new[] {
                    S(ComboMotion.UnderhandArc, 18, 18, 12),
                    S(ComboMotion.OverheadArc,  0,  20, 0,  1.1f),
                }
            },
            new MeleeCombo {
                Name = "Joust Poke", BaseWeight = 80, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.Orange, CooldownAfterUse = 110,
                Steps = new[] { S(ComboMotion.JoustDash, 22, 14, 0, 1.2f, 1.4f, 1.8f) }
            },
            new MeleeCombo {
                Name = "3-Hit Standard", BaseWeight = 60, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Cyan, CooldownAfterUse = 140,
                Steps = new[] {
                    S(ComboMotion.OverheadArc,     14, 18, 14, 0.9f),
                    S(ComboMotion.HorizontalSweep,  0, 18, 14, 0.9f),
                    S(ComboMotion.UnderhandArc,     0, 20, 0,  1.1f),
                }
            },
            new MeleeCombo {
                Name = "5-Hit Finisher", BaseWeight = 30, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Red, CooldownAfterUse = 240, HeavyCommit = true,
                Steps = new[] {
                    S(ComboMotion.OverheadArc,     40, 14, 8,  0.7f),
                    S(ComboMotion.UnderhandArc,     0, 14, 8,  0.7f),
                    S(ComboMotion.HorizontalSweep,  0, 14, 8,  0.7f),
                    S(ComboMotion.OverheadArc,      0, 14, 8,  0.7f),
                    S(ComboMotion.JoustDash,        0, 18, 0,  1.5f, 1.2f, 1.5f),
                }
            },
        };

        public static readonly MeleeCombo[] Greatsword = new[]
        {
            new MeleeCombo {
                Name = "Heavy Chop", BaseWeight = 70, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Orange, CooldownAfterUse = 160, HeavyCommit = true,
                Steps = new[] { S(ComboMotion.OverheadArc, 45, 26, 0, 1.8f, 1.2f) }
            },
            new MeleeCombo {
                Name = "Rising Slash", BaseWeight = 60, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.LightYellow, CooldownAfterUse = 140,
                Steps = new[] { S(ComboMotion.UnderhandArc, 35, 24, 0, 1.4f, 1.1f, 0.6f) }
            },
            new MeleeCombo {
                Name = "Spin-Dash", BaseWeight = 50, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.Yellow, CooldownAfterUse = 160,
                Steps = new[] {
                    S(ComboMotion.Spin,      30, 22, 10, 1.0f, 1.0f, 1.4f),
                    S(ComboMotion.JoustDash,  0, 16, 0,  1.3f, 1.3f, 1.6f),
                }
            },
            new MeleeCombo {
                Name = "Ground Pound", BaseWeight = 30, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Red, CooldownAfterUse = 240, HeavyCommit = true,
                Steps = new[] { S(ComboMotion.GroundSlam, 50, 24, 0, 2.0f, 1.3f) }
            },
            new MeleeCombo {
                Name = "Running Cleave", BaseWeight = 50, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.Cyan, CooldownAfterUse = 160,
                Steps = new[] {
                    S(ComboMotion.JoustDash,   22, 14, 10, 0.9f, 1.1f, 1.4f),
                    S(ComboMotion.OverheadArc,  0, 22, 0,  1.4f, 1.2f),
                }
            },
        };

        public static readonly MeleeCombo[] Hammer = new[]
        {
            new MeleeCombo {
                Name = "Overhead Smash", BaseWeight = 80, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.White, CooldownAfterUse = 90,
                Steps = new[] { S(ComboMotion.VerticalChop, 30, 22, 0, 1.5f, 1.0f) }
            },
            new MeleeCombo {
                Name = "Side-to-Side", BaseWeight = 60, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Cyan, CooldownAfterUse = 120,
                Steps = new[] {
                    S(ComboMotion.HorizontalSweep, 18, 18, 14),
                    S(ComboMotion.HorizontalSweep,  0, 18, 0, 1.1f),
                }
            },
            new MeleeCombo {
                Name = "Step-In Smash", BaseWeight = 60, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.LightYellow, CooldownAfterUse = 130,
                Steps = new[] { S(ComboMotion.VerticalChop, 25, 22, 0, 1.4f, 1.1f, 1.2f) }
            },
            new MeleeCombo {
                Name = "3-Bash Combo", BaseWeight = 50, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Cyan, CooldownAfterUse = 160,
                Steps = new[] {
                    S(ComboMotion.HorizontalSweep, 22, 16, 12, 0.9f),
                    S(ComboMotion.HorizontalSweep,  0, 16, 12, 0.9f),
                    S(ComboMotion.VerticalChop,     0, 22, 0,  1.3f),
                }
            },
            new MeleeCombo {
                Name = "Charged Slam", BaseWeight = 30, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Red, CooldownAfterUse = 260, HeavyCommit = true,
                Steps = new[] { S(ComboMotion.GroundSlam, 55, 26, 0, 2.2f, 1.5f) }
            },
        };

        // Axes deliberately use ONLY OverheadArc (downward chop) and UnderhandArc (rising cut) —
        // the two swings a player actually has with an axe.  No spins / sweeps / thrusts; variety
        // comes from damage, reach, step-in push, and commitment instead of exotic motions.
        public static readonly MeleeCombo[] Axe = new[]
        {
            // Bread-and-butter overhead: fast, snappy, barely slows the feet.
            new MeleeCombo {
                Name = "Chop", BaseWeight = 100, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.White, CooldownAfterUse = 70, MoveBrake = 0.10f,
                Steps = new[] { S(ComboMotion.OverheadArc, 30, 22, 0, 1.2f, 1.1f, swingMult: 1.4f, ease: SwingEaseStyle.Snap) }
            },
            // Rising slash: a touch more deliberate than the chop so the up-swing reads clearly.
            new MeleeCombo {
                Name = "Rising Cut", BaseWeight = 70, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.LightYellow, CooldownAfterUse = 80, MoveBrake = 0.10f,
                Steps = new[] { S(ComboMotion.UnderhandArc, 32, 22, 0, 1.2f, 1.1f, swingMult: 1.2f) }
            },
            // Lunging chop: keeps its forward momentum (no brake), quick committed strike.
            new MeleeCombo {
                Name = "Step-In Chop", BaseWeight = 60, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.Orange, CooldownAfterUse = 110, MoveBrake = 0f,
                Steps = new[] { S(ComboMotion.OverheadArc, 30, 22, 0, 1.3f, 1.1f, 1.2f, swingMult: 1.35f, ease: SwingEaseStyle.Snap) }
            },
            // Chop down, then rip straight back up. The handoff eases into the rising cut (no snap).
            new MeleeCombo {
                Name = "Down-Up", BaseWeight = 55, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Cyan, CooldownAfterUse = 150, MoveBrake = 0.12f,
                Steps = new[] {
                    S(ComboMotion.OverheadArc,  28, 22, 12, 1.0f, 1f, 0f, swingMult: 1.35f, ease: SwingEaseStyle.Snap),
                    S(ComboMotion.UnderhandArc,  0, 22, 0,  1.2f, 1f, 0f, swingMult: 1.2f),
                }
            },
            // Big menacing swing: holds the apex, then whips down slow and heavy; plants the feet.
            new MeleeCombo {
                Name = "Heavy Cleave", BaseWeight = 30, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Red, CooldownAfterUse = 240, HeavyCommit = true, MoveBrake = 0.40f,
                Steps = new[] { S(ComboMotion.OverheadArc, 48, 28, 0, 2.0f, 1.25f, 0f, swingMult: 0.85f, ease: SwingEaseStyle.Whip) }
            },
            // Triple step-in chop: three fast advancing chops with hyper-armor, the eased re-raise
            // between each chop serving as the (uninterruptible) wind-up for the next.
            new MeleeCombo {
                Name = "Triple Chop Advance", BaseWeight = 45, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.Orange, CooldownAfterUse = 220, HeavyCommit = true, HyperArmor = true, MoveBrake = 0f,
                Steps = new[] {
                    S(ComboMotion.OverheadArc, 36, 20, 14, 1.1f, 1.1f, 1.0f, swingMult: 1.5f, ease: SwingEaseStyle.Snap),
                    S(ComboMotion.OverheadArc,  0, 20, 14, 1.1f, 1.1f, 1.0f, swingMult: 1.5f, ease: SwingEaseStyle.Snap),
                    S(ComboMotion.OverheadArc,  0, 22,  0, 1.3f, 1.1f, 1.0f, swingMult: 1.4f, ease: SwingEaseStyle.Snap),
                }
            },
            // Leaping slam: wind up, jump toward the player with the axe held high, slam down +
            // hit on landing (single deferred-hit step; AttackTicks is the airtime cap).
            new MeleeCombo {
                Name = "Leaping Slam", BaseWeight = 40, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.Yellow, CooldownAfterUse = 200, HeavyCommit = true, HyperArmor = true, MoveBrake = 0f,
                Steps = new[] { S(ComboMotion.LeapSlam, 34, 90, 0, 1.7f, 1.25f) }
            },
            // Charge chop: run at the player (step 0, no hit) until in reach OR a 3 s timeout,
            // then the OverheadArc chop (step 1) lands.  The timeout lets it complete + reset to
            // idle even if the player keeps evading.
            new MeleeCombo {
                Name = "Charge Chop", BaseWeight = 40, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.Orange, CooldownAfterUse = 180, HyperArmor = true, MoveBrake = 0f,
                Steps = new[] {
                    S(ComboMotion.ChargeChop,  30, 180, 2),
                    S(ComboMotion.OverheadArc,  0, 22, 0, 1.3f, 1.1f, 0f, swingMult: 1.35f, ease: SwingEaseStyle.Snap),
                }
            },
            // Feint chop: raise the axe + fire the warning flash (the bait), hold WITHOUT swinging
            // so a panic-roll whiffs, then drop the real chop fast a beat later (the delayed swing).
            new MeleeCombo {
                Name = "Feint Chop", BaseWeight = 35, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.LightYellow, CooldownAfterUse = 160, MoveBrake = 0.15f,
                Steps = new[] {
                    S(ComboMotion.Feint,       30, 26, 6),
                    S(ComboMotion.OverheadArc,  0, 22, 0, 1.2f, 1.1f, 0f, swingMult: 1.45f, ease: SwingEaseStyle.Snap),
                }
            },
        };

        public static readonly MeleeCombo[] Dagger = new[]
        {
            new MeleeCombo {
                Name = "Quick Stab", BaseWeight = 110, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.White, CooldownAfterUse = 30,
                Steps = new[] { S(ComboMotion.Thrust, 8, 12, 0, 0.8f) }
            },
            new MeleeCombo {
                Name = "Double Slash", BaseWeight = 80, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Cyan, CooldownAfterUse = 70,
                Steps = new[] {
                    S(ComboMotion.UnderhandArc, 10, 14, 8, 0.7f),
                    S(ComboMotion.OverheadArc,   0, 14, 0, 0.8f),
                }
            },
            new MeleeCombo {
                Name = "Backstab Roll-In", BaseWeight = 50, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.Yellow, CooldownAfterUse = 130,
                Steps = new[] { S(ComboMotion.JoustDash, 14, 12, 0, 1.3f, 1.2f, 2.0f) }
            },
            new MeleeCombo {
                Name = "5-Jab Flurry", BaseWeight = 60, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Cyan, CooldownAfterUse = 160,
                Steps = new[] {
                    S(ComboMotion.Thrust, 18, 10, 6, 0.6f),
                    S(ComboMotion.Thrust,  0, 10, 6, 0.6f),
                    S(ComboMotion.Thrust,  0, 10, 6, 0.6f),
                    S(ComboMotion.Thrust,  0, 10, 6, 0.6f),
                    S(ComboMotion.Thrust,  0, 14, 0, 1.0f),
                }
            },
            new MeleeCombo {
                Name = "Lunge Finisher", BaseWeight = 30, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.Red, CooldownAfterUse = 220, HeavyCommit = true,
                Steps = new[] { S(ComboMotion.JoustDash, 30, 16, 0, 1.8f, 1.6f, 2.5f) }
            },
        };

        public static readonly MeleeCombo[] Whip = new[]
        {
            new MeleeCombo {
                Name = "Single Lash", BaseWeight = 90, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.White, CooldownAfterUse = 60,
                Steps = new[] { S(ComboMotion.HorizontalSweep, 22, 18, 0, 1.0f, 1.6f) }
            },
            new MeleeCombo {
                Name = "Crack-Back", BaseWeight = 60, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.Cyan, CooldownAfterUse = 130,
                Steps = new[] {
                    S(ComboMotion.HorizontalSweep, 20, 16, 14, 0.9f, 1.5f),
                    S(ComboMotion.HorizontalSweep,  0, 16, 0,  0.9f, 1.5f),
                }
            },
            new MeleeCombo {
                Name = "Vertical Wrap", BaseWeight = 50, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.LightYellow, CooldownAfterUse = 120,
                Steps = new[] { S(ComboMotion.OverheadArc, 25, 20, 0, 1.2f, 1.4f) }
            },
            new MeleeCombo {
                Name = "Triple Snap", BaseWeight = 50, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.Cyan, CooldownAfterUse = 170,
                Steps = new[] {
                    S(ComboMotion.HorizontalSweep, 18, 12, 10, 0.7f, 1.5f),
                    S(ComboMotion.HorizontalSweep,  0, 12, 10, 0.7f, 1.5f),
                    S(ComboMotion.HorizontalSweep,  0, 14, 0,  1.0f, 1.6f),
                }
            },
            new MeleeCombo {
                Name = "Spin-Build", BaseWeight = 30, Preferred = ComboRangeBand.Far,
                InitialFlashColor = Color.Red, CooldownAfterUse = 260, HeavyCommit = true,
                Steps = new[] {
                    S(ComboMotion.Spin,            50, 28, 8, 0.0f, 1.0f),
                    S(ComboMotion.HorizontalSweep,  0, 20, 0, 1.8f, 1.8f),
                }
            },
        };

        public static readonly MeleeCombo[] Flail = new[]
        {
            new MeleeCombo {
                Name = "Single Arc", BaseWeight = 90, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.White, CooldownAfterUse = 80,
                Steps = new[] { S(ComboMotion.OverheadArc, 26, 22, 0, 1.1f, 1.2f) }
            },
            new MeleeCombo {
                Name = "Wind-Up Spin", BaseWeight = 60, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.LightYellow, CooldownAfterUse = 150,
                Steps = new[] {
                    S(ComboMotion.Spin,        35, 18, 6, 0.0f),
                    S(ComboMotion.OverheadArc,  0, 22, 0, 1.5f, 1.3f),
                }
            },
            new MeleeCombo {
                Name = "Step-In Slam", BaseWeight = 50, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.LightYellow, CooldownAfterUse = 130,
                Steps = new[] { S(ComboMotion.VerticalChop, 24, 22, 0, 1.3f, 1.2f, 1.2f) }
            },
            new MeleeCombo {
                Name = "3-Swing Combo", BaseWeight = 50, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Cyan, CooldownAfterUse = 170,
                Steps = new[] {
                    S(ComboMotion.OverheadArc,  20, 16, 14, 0.8f),
                    S(ComboMotion.UnderhandArc,  0, 16, 14, 0.8f),
                    S(ComboMotion.OverheadArc,   0, 18, 0,  1.2f),
                }
            },
            new MeleeCombo {
                Name = "Long Spin Finisher", BaseWeight = 30, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.Red, CooldownAfterUse = 280, HeavyCommit = true,
                Steps = new[] {
                    S(ComboMotion.Spin, 60, 30, 0, 1.6f, 1.5f),
                }
            },
        };

        public static readonly MeleeCombo[] Katana = new[]
        {
            new MeleeCombo {
                Name = "Iaido Draw", BaseWeight = 70, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.White, CooldownAfterUse = 140,
                Steps = new[] { S(ComboMotion.IaidoDraw, 35, 14, 0, 1.4f, 1.4f, 1.8f) }
            },
            new MeleeCombo {
                Name = "Wide Sweep", BaseWeight = 90, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.White, CooldownAfterUse = 60,
                Steps = new[] { S(ComboMotion.HorizontalSweep, 16, 20, 0, 1.0f, 1.2f) }
            },
            new MeleeCombo {
                Name = "Cross-Cut", BaseWeight = 70, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Cyan, CooldownAfterUse = 100,
                Steps = new[] {
                    S(ComboMotion.OverheadArc,  14, 16, 10, 0.9f),
                    S(ComboMotion.UnderhandArc,  0, 18, 0,  1.0f),
                }
            },
            new MeleeCombo {
                Name = "4-Strike Dance", BaseWeight = 50, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Cyan, CooldownAfterUse = 180,
                Steps = new[] {
                    S(ComboMotion.HorizontalSweep, 18, 14, 8, 0.8f),
                    S(ComboMotion.OverheadArc,      0, 14, 8, 0.8f),
                    S(ComboMotion.HorizontalSweep,  0, 14, 8, 0.8f),
                    S(ComboMotion.UnderhandArc,     0, 16, 0, 1.0f),
                }
            },
            new MeleeCombo {
                Name = "Unsheathe Finisher", BaseWeight = 30, Preferred = ComboRangeBand.Far,
                InitialFlashColor = Color.Red, CooldownAfterUse = 260, HeavyCommit = true,
                Steps = new[] { S(ComboMotion.IaidoDraw, 70, 16, 0, 2.2f, 1.8f, 3.0f) }
            },
        };

        public static readonly MeleeCombo[] Rapier = new[]
        {
            new MeleeCombo {
                Name = "Single Jab", BaseWeight = 110, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.White, CooldownAfterUse = 30,
                Steps = new[] { S(ComboMotion.Thrust, 10, 10, 0, 0.8f, 1.0f) }
            },
            new MeleeCombo {
                Name = "Double Thrust", BaseWeight = 80, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Cyan, CooldownAfterUse = 80,
                Steps = new[] {
                    S(ComboMotion.Thrust, 12, 10, 8, 0.8f),
                    S(ComboMotion.Thrust,  0, 12, 0, 1.0f),
                }
            },
            new MeleeCombo {
                Name = "Riposte Stance", BaseWeight = 40, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Yellow, CooldownAfterUse = 160,
                Steps = new[] { S(ComboMotion.Thrust, 20, 14, 0, 1.6f, 1.1f) }
            },
            new MeleeCombo {
                Name = "Triple Flurry", BaseWeight = 60, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Cyan, CooldownAfterUse = 140,
                Steps = new[] {
                    S(ComboMotion.Thrust, 16, 10, 8, 0.7f),
                    S(ComboMotion.Thrust,  0, 10, 8, 0.7f),
                    S(ComboMotion.Thrust,  0, 12, 0, 1.0f),
                }
            },
            new MeleeCombo {
                Name = "Lunging Finisher", BaseWeight = 30, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.Red, CooldownAfterUse = 240, HeavyCommit = true,
                Steps = new[] {
                    S(ComboMotion.Thrust,    32, 10, 8, 0.7f),
                    S(ComboMotion.Thrust,     0, 10, 8, 0.7f),
                    S(ComboMotion.Thrust,     0, 10, 8, 0.7f),
                    S(ComboMotion.JoustDash,  0, 14, 0, 1.6f, 1.5f, 2.2f),
                }
            },
        };

        public static readonly MeleeCombo[] Halberd = new[]
        {
            new MeleeCombo {
                // Jump toward player with spear leveled; thrust lands on contact/touchdown.
                Name = "Leaping Lunge", BaseWeight = 90, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.Yellow, CooldownAfterUse = 200, HeavyCommit = true, HyperArmor = true,
                Steps = new[] { S(ComboMotion.LeapThrust, 32, 90, 0, 1.4f, 1.5f) }
            },
            new MeleeCombo {
                Name = "Forward Thrust", BaseWeight = 90, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.White, CooldownAfterUse = 70,
                Steps = new[] { S(ComboMotion.Thrust, 20, 14, 0, 1.1f, 1.4f) }
            },
            new MeleeCombo {
                Name = "Sweep-Thrust", BaseWeight = 60, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.Cyan, CooldownAfterUse = 140,
                Steps = new[] {
                    S(ComboMotion.HorizontalSweep, 22, 18, 14, 0.9f, 1.3f),
                    S(ComboMotion.Thrust,           0, 14, 0,  1.1f, 1.4f),
                }
            },
            new MeleeCombo {
                // Two forward pokes in a row (each extends + retracts via the spear-grip animation).
                Name = "Double Thrust", BaseWeight = 75, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.White, CooldownAfterUse = 110,
                Steps = new[] {
                    S(ComboMotion.Thrust, 18, 14, 12, 1.0f, 1.4f),
                    S(ComboMotion.Thrust,  0, 14, 0,  1.1f, 1.4f),
                }
            },
            new MeleeCombo {
                Name = "Spinning Windmill", BaseWeight = 40, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Yellow, CooldownAfterUse = 180,
                Steps = new[] { S(ComboMotion.Spin, 30, 32, 0, 1.4f, 1.3f) }
            },
            new MeleeCombo {
                // OverheadArc (not GroundSlam): GroundSlam's swing ends near-vertical (a hammer/axe
                // ground-pound read), which looked like the spear was stabbing straight down instead
                // of at the player. OverheadArc ends much closer to horizontal/forward.
                Name = "Charged Chop", BaseWeight = 30, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Red, CooldownAfterUse = 260, HeavyCommit = true,
                Steps = new[] { S(ComboMotion.OverheadArc, 55, 26, 0, 2.0f, 1.4f) }
            },
        };

        public static readonly MeleeCombo[] TwinDaggers = new[]
        {
            new MeleeCombo {
                Name = "Cross Slash", BaseWeight = 90, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Cyan, CooldownAfterUse = 50,
                Steps = new[] {
                    S(ComboMotion.OverheadArc,  12, 12, 4, 0.8f),
                    S(ComboMotion.UnderhandArc,  0, 12, 0, 0.8f),
                }
            },
            new MeleeCombo {
                Name = "Alternating Jabs", BaseWeight = 80, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Cyan, CooldownAfterUse = 120,
                Steps = new[] {
                    S(ComboMotion.Thrust, 14, 10, 6, 0.6f),
                    S(ComboMotion.Thrust,  0, 10, 6, 0.6f),
                    S(ComboMotion.Thrust,  0, 10, 6, 0.6f),
                    S(ComboMotion.Thrust,  0, 12, 0, 0.8f),
                }
            },
            new MeleeCombo {
                Name = "Spinning Dance", BaseWeight = 50, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.Yellow, CooldownAfterUse = 150,
                Steps = new[] { S(ComboMotion.Spin, 22, 28, 0, 1.2f, 1.2f, 1.6f) }
            },
            new MeleeCombo {
                Name = "Pounce Slash", BaseWeight = 50, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.LightYellow, CooldownAfterUse = 140,
                Steps = new[] {
                    S(ComboMotion.JoustDash,   18, 10, 6, 0.8f, 1.2f, 1.8f),
                    S(ComboMotion.OverheadArc,  0, 14, 0, 1.0f),
                }
            },
            new MeleeCombo {
                Name = "6-Hit Fury", BaseWeight = 30, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Red, CooldownAfterUse = 240, HeavyCommit = true,
                Steps = new[] {
                    S(ComboMotion.OverheadArc,     25, 10, 5, 0.6f),
                    S(ComboMotion.UnderhandArc,     0, 10, 5, 0.6f),
                    S(ComboMotion.HorizontalSweep,  0, 10, 5, 0.6f),
                    S(ComboMotion.OverheadArc,      0, 10, 5, 0.6f),
                    S(ComboMotion.UnderhandArc,     0, 10, 5, 0.6f),
                    S(ComboMotion.Thrust,           0, 14, 0, 1.2f),
                }
            },
        };

        // ─────────────────────────────────────────────────────────────────────
        // RANGED ARCHETYPE TABLES (5 combos each)
        // ─────────────────────────────────────────────────────────────────────

        public static readonly RangedCombo[] ThrowablesCombos = new[]
        {
            new RangedCombo {
                Name = "Single Toss", BaseWeight = 100, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.White, CooldownAfterUse = 90,
                Shots = new[] { R(0, 4f) }
            },
            new RangedCombo {
                Name = "Burst of 3", BaseWeight = 80, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.Cyan, CooldownAfterUse = 150,
                Shots = new[] { R(0, 8f), R(8, 8f), R(8, 8f) },
                TelegraphExtra = 4
            },
            new RangedCombo {
                Name = "Fan Spread", BaseWeight = 60, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.LightYellow, CooldownAfterUse = 180,
                Shots = new[] { R(0, 12f, 1f, 3) },
                TelegraphExtra = 10
            },
            new RangedCombo {
                Name = "Double Tap", BaseWeight = 70, Preferred = ComboRangeBand.Far,
                InitialFlashColor = Color.Cyan, CooldownAfterUse = 160,
                Shots = new[] { R(0, 2f, 1.2f), R(18, 2f, 1.2f) }
            },
            new RangedCombo {
                Name = "Barrage", BaseWeight = 30, Preferred = ComboRangeBand.Far,
                InitialFlashColor = Color.Red, CooldownAfterUse = 280, HeavyCommit = true,
                Shots = new[] { R(0, 6f), R(6, 6f), R(6, 6f), R(6, 6f), R(6, 6f) },
                TelegraphExtra = 20
            },
        };

        public static readonly RangedCombo[] BowCombos = new[]
        {
            new RangedCombo {
                Name = "Aimed Shot", BaseWeight = 100, Preferred = ComboRangeBand.Far,
                InitialFlashColor = Color.White, CooldownAfterUse = 120,
                Shots = new[] { R(0, 2f, 1.1f) }
            },
            new RangedCombo {
                Name = "Quick Draw", BaseWeight = 70, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.Cyan, CooldownAfterUse = 100,
                Shots = new[] { R(0, 6f, 0.9f) },
                TelegraphExtra = -10
            },
            new RangedCombo {
                Name = "Twin Arrows", BaseWeight = 60, Preferred = ComboRangeBand.Far,
                InitialFlashColor = Color.LightYellow, CooldownAfterUse = 180,
                Shots = new[] { R(0, 6f, 1f, 2) },
                TelegraphExtra = 8
            },
            new RangedCombo {
                Name = "Triple Volley", BaseWeight = 40, Preferred = ComboRangeBand.Far,
                InitialFlashColor = Color.Yellow, CooldownAfterUse = 220,
                Shots = new[] { R(0, 4f), R(22, 4f), R(22, 4f) },
                TelegraphExtra = 10
            },
            new RangedCombo {
                Name = "Power Shot", BaseWeight = 25, Preferred = ComboRangeBand.Far,
                InitialFlashColor = Color.Red, CooldownAfterUse = 280, HeavyCommit = true,
                Shots = new[] { R(0, 1f, 1.6f) },
                TelegraphExtra = 30
            },
        };

        public static readonly RangedCombo[] CrossbowCombos = new[]
        {
            new RangedCombo {
                Name = "Single Bolt", BaseWeight = 110, Preferred = ComboRangeBand.Far,
                InitialFlashColor = Color.White, CooldownAfterUse = 100,
                Shots = new[] { R(0, 3f) }
            },
            new RangedCombo {
                Name = "Controlled Burst", BaseWeight = 70, Preferred = ComboRangeBand.Far,
                InitialFlashColor = Color.Yellow, CooldownAfterUse = 200,
                Shots = new[] { R(0, 3f), R(30, 3f), R(60, 3f) },
                TelegraphExtra = 10
            },
            new RangedCombo {
                Name = "Quick Reload Pair", BaseWeight = 50, Preferred = ComboRangeBand.Far,
                InitialFlashColor = Color.Cyan, CooldownAfterUse = 160,
                Shots = new[] { R(0, 3f), R(20, 3f) }
            },
            new RangedCombo {
                Name = "Spread Volley", BaseWeight = 40, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.LightYellow, CooldownAfterUse = 200,
                Shots = new[] { R(0, 10f, 1f, 3) },
                TelegraphExtra = 12
            },
            new RangedCombo {
                Name = "Full Barrage", BaseWeight = 20, Preferred = ComboRangeBand.Far,
                InitialFlashColor = Color.Red, CooldownAfterUse = 300, HeavyCommit = true,
                Shots = new[] { R(0, 3f), R(30, 3f), R(30, 3f), R(30, 3f), R(5, 3f), R(5, 3f) },
                TelegraphExtra = 30
            },
        };

        public static readonly RangedCombo[] BoomerangCombos = new[]
        {
            new RangedCombo {
                Name = "Straight Throw", BaseWeight = 100, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.White, CooldownAfterUse = 120,
                Shots = new[] { R(0, 0f) }
            },
            new RangedCombo {
                Name = "Double Fan", BaseWeight = 70, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.Cyan, CooldownAfterUse = 180,
                Shots = new[] { R(0, 15f, 1f, 2) },
                TelegraphExtra = 6
            },
            new RangedCombo {
                Name = "High Arc Lob", BaseWeight = 50, Preferred = ComboRangeBand.Far,
                InitialFlashColor = Color.LightYellow, CooldownAfterUse = 200,
                Shots = new[] { R(0, 0f, 0.7f) },
                TelegraphExtra = 10
            },
            new RangedCombo {
                Name = "Orbit Throw", BaseWeight = 40, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.Yellow, CooldownAfterUse = 220,
                Shots = new[] { R(0, 0f, 0.6f) },
                TelegraphExtra = 18
            },
            new RangedCombo {
                Name = "Spread Barrage", BaseWeight = 25, Preferred = ComboRangeBand.Far,
                InitialFlashColor = Color.Red, CooldownAfterUse = 280, HeavyCommit = true,
                Shots = new[] { R(0, 20f, 1f, 4) },
                TelegraphExtra = 22
            },
        };
    }
}
