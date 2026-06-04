using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Armors;
using tsorcRevamp.Items.Weapons.Melee.Runeterra;
using tsorcRevamp.Items.Weapons.Melee;
using tsorcRevamp.Items.Weapons.Ranged.Crossbows;
using tsorcRevamp.NPCs;
using tsorcRevamp.NPCs.AI;

namespace tsorcRevamp.NPCs.Invaders
{
    public class AbyssalNinjaInvader : InvaderNPC
    {
        // ── Config ────────────────────────────────────────────────────────────────
        /// <summary>Master config toggle: when true, every spawned Abyssal Ninja has wings
        /// and can take off, hover, strafe, and dive.  Flip to false to ship a ground-only
        /// variant without removing the wing code paths.  Wire to ModConfig later if desired.</summary>
        public static bool ConfigHasWings = true;

        /// <summary>Wing item type used when ConfigHasWings is true.  ItemID.DemonWings reads
        /// best on the Abyssal Ninja's dark palette; swap to taste.</summary>
        public static int ConfigWingsItemType = ItemID.DemonWings;

        // ── Invasion banner ───────────────────────────────────────────────────────
        protected override string InvaderTitle => "Abyssal Ninja";

        // ── Wings ─────────────────────────────────────────────────────────────────
        protected override bool HasWings              => ConfigHasWings;
        protected override int  WingsAccessoryItemType => ConfigWingsItemType;
        protected override EnemyFlightConfig FlightConfig => new EnemyFlightConfig
        {
            HoverAltitude     = 220f,
            HoverSideOffset   = 90f,
            HoverTopSpeed     = 5.0f,   // ninjas are quick
            TakeOffSpeed      = 9f,
            DiveAcceleration  = 0.65f,
            DiveTopSpeed      = 13f,
            LandSpeed         = 7f,
            MaxFlightTicks    = 600,
            CooldownTicks     = 180,
            TakeOffTicks      = 30,
            HoverDwellTicks   = 75,
            StrafeTicks       = 55,
            DiveAttackTicks   = 45,
            LandTicks         = 75,
            WingFlapSpeed     = 0.10f,  // faster flap for the ninja silhouette
        };
        // Lean into flight: random takeoff bursts are common, and the ninja escalates aggressively below half HP.
        protected override int   RandomTakeoffChance     => 12;
        protected override float FlightHpEscalationFrac  => 0.55f;

        // ── Movement AI ───────────────────────────────────────────────────────────
        // Use the project's SmartFighter4AI movement driver — A* span-graph pathfinding with
        // rope climbing, ledge self-catch, and stuck-recovery, far ahead of the legacy FighterAI
        // default.  Note: this path doesn't support canDodgeroll / canPounce — the ninja trades
        // those for cleaner pathfinding.  Override RunMovementAI again in a subclass to switch back.
        protected override void RunMovementAI(float speedMult)
        {
            // Smart hunter: full A* search window + investigates last-known position before giving up.
            var g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            g.NavSearchRadius = 80;
            g.RemembersLastKnownPos = true;
            SmartFighter4AI.Run(NPC,
                topSpeed:           TopSpeed * speedMult,
                acceleration:       Acceleration,
                doorBreakingDamage: 4,
                attackRange:        RangedRange);
        }

        // ── Loadout ───────────────────────────────────────────────────────────────
        protected override int HeadArmorItemType    => ModContent.ItemType<AbyssalNinjaMask>();
        protected override int BodyArmorItemType    => ModContent.ItemType<AbyssalNinjaTop>();
        protected override int LegsArmorItemType    => ModContent.ItemType<AbyssalNinjaBottoms>();

        // SteelTempest sprite for the held melee visual.
        // Primary ranged: AbyssalStar throwing stars (short/medium range, burst of 1–3).
        // Secondary ranged: HeavyCrossbow (long range, single bolt, planted stance).
        protected override int MeleeWeaponItemType  => ModContent.ItemType<SteelTempest>();
        protected override int RangedWeaponItemType => ModContent.ItemType<AbyssalStar>();

        protected override int MeleeDamage  => 40;
        protected override int RangedDamage => 30;

        // ── Secondary ranged: HeavyCrossbow ──────────────────────────────────────
        protected override int         SecondaryRangedWeaponItemType  => ModContent.ItemType<HeavyCrossbow>();
        protected override int         SecondaryRangedDamage          => 35;  // bolt hits harder than stars
        protected override RangedStyle SecondaryRangedAnimStyle       => RangedStyle.Crossbow;
        protected override float       SecondaryRangedRange           => 600f;
        protected override float       SecondaryRangedMinRange        => 350f; // must be far enough to bother aiming
        protected override int         SecondaryRangedTelegraphTicks  => 32;   // base crossbow telegraph (+10 over original 22)
        protected override int         SecondaryRangedCooldownAfterUse => 240; // ~4 s between crossbow shots
        protected override int         SecondaryMaxRangedBurst        => 1;    // one bolt per trigger
        protected override int         SecondaryRangedChance          => 60;   // 60% chance when both in range — favor the crossbow when at range
        protected override int         SecondaryStandingRangedChance  => 80;   // almost always planted
        // SecondaryRangedFlashColor is OVERRIDDEN per-pattern by SecondaryRangedBurstFlashColors below.
        // The value here is kept as a fallback for subclasses that don't define burst patterns.
        protected override Color       SecondaryRangedFlashColor      => new Color(160, 220, 255); // pale cyan

        // ── Crossbow burst patterns ───────────────────────────────────────────────
        //
        //  Pattern 0 — single shot (white flash, no extra telegraph)
        //    fire × 1
        //
        //  Pattern 1 — controlled double-tap + finisher (yellow flash, +10 frames telegraph)
        //    fire, wait 30, fire, wait 60, fire × 3 total
        //
        //  Pattern 2 — full barrage (red flash, +30 frames telegraph)
        //    fire, wait 30, fire, wait 30, fire, wait 30, fire, wait 5, fire, wait 5, fire × 6 total
        //
        // Inter-shot pauses are defined as arrays passed to CrossbowBurstPause; total shots
        // in each pattern = pauses.Length + 1.
        // The flash fires 30 frames before the initial shot (CheckAndFireFlash threshold = 30).
        protected override int[][] SecondaryRangedBurstPatterns => new int[][]
        {
            new int[] { },                        // pattern 0: single shot — no pauses
            new int[] { 30, 60 },                 // pattern 1: 3 shots  (pause 30, pause 60)
            new int[] { 30, 30, 30, 5, 5 },       // pattern 2: 6 shots  (3 × pause 30, 2 × pause 5)
        };

        protected override int[] SecondaryRangedBurstTelegraphExtras => new int[] { 0, 10, 30 };

        protected override Color[] SecondaryRangedBurstFlashColors => new Color[]
        {
            Color.White,            // pattern 0 — single shot: clean white
            Color.Yellow,           // pattern 1 — controlled burst: warning yellow
            Color.Red,              // pattern 2 — full barrage: danger red
        };

        // Relative selection weights.  Single shot is most common; barrage is rare.
        protected override int[] SecondaryRangedBurstChances => new int[] { 55, 35, 10 };

        // ── Combat tuning ─────────────────────────────────────────────────────────
        protected override float TopSpeed       => 3.2f;
        protected override float Acceleration   => 0.11f;
        protected override float MeleeRange     => 90f;
        protected override float StabRange      => 200f;
        protected override float RangedRange    => 480f;
        protected override float MinRangedRange => 280f;  // close range → prefer melee/stab

        // Melee/stab telegraphs raised to satisfy the 30-tick fair-wind-up floor.
        // Slash: 35 ticks lets the sword-raise + apex-hold animation read clearly.
        // Stab: 40 ticks for the dipped-sword "cocked thrust" pose with brief hold.
        protected override int MeleeTelegraphTicks  => 35;
        protected override int StabTelegraphTicks   => 40;
        protected override int StabAttackTicks      => 9;
        protected override int StabRecoveryTicks    => 36;
        protected override int RangedTelegraphTicks  => 45;  // 45-frame arm-raise before each throw (+10 over original 35)
        protected override int RangedCooldownAfterUse => 180; // 3 s of forced melee/stab first
        protected override int MaxRangedBurst        => 3;
        protected override int SingleRangedBurstChance => 25;
        protected override int StandingRangedChance  => 15;   // stars — mostly mobile throws
        protected override int TeleportTelegraphTicks => 120;
        protected override int TeleportDustCount => 28;
        protected override int TeleportDustTypeId => DustID.Smoke;
        protected override Color TeleportDustTint => new Color(165, 90, 255);
        protected override float TeleportDustScale => 0.95f;

        // Keep the test invader aggressive: after attacks, immediately resume chase/re-engage.
        protected override int CasualStrollChance => 0;

        protected override bool CanStab => true;

        // Melee/stab flash white; throwing stars flash pink; crossbow flash is set on SecondaryRangedFlashColor.
        protected override Color MeleeTelegraphFlashColor  => Color.White;
        protected override Color RangedTelegraphFlashColor => new Color(255, 100, 200); // pink for stars

        // ── Navigation ────────────────────────────────────────────────────────────
        // Ninjas are agile: high jump, long gap-boost, and double-jump.
        // JumpPower 10 clears ~4-tile gaps; 12 was overshooting most platforms.
        protected override float InvaderJumpPower      => 10f;
        protected override float InvaderJumpBoost      => 7f;
        protected override bool  InvaderCanDoubleJump  => true;
        protected override float InvaderDoubleJumpPower => 7f;

        // ── Attack tuning reference ───────────────────────────────────────────────
        // MELEE SLASH  — dist ≤ 90px, on-ground, heightDiff < 36px
        //   Telegraph: 18 ticks (slow down, arm raised; white flash at 30 ticks remaining)
        //   Attack:    WeaponAnimMax ticks (swing arc + melee hitbox spawned)
        //   Recovery:  25 ticks
        //   Damage:    MeleeDamage = 40
        //
        // STAB / LUNGE — 90px < dist ≤ 200px, on-ground, heightDiff < 48px, no cooldown
        //   Telegraph: 24 ticks (slow down, arm dipped — "cocked" read; flash fires immediately)
        //   Attack:    9 ticks  (lunge: TopSpeed × StabLungeSpeedMult = 3.2 × 2.0 = 6.4 px/f)
        //   Recovery:  36 ticks
        //   Cooldown:  120 ticks after each stab
        //   Damage:    MeleeDamage = 40, reach = StabRange × 0.55 = 110px
        //
        // PRIMARY RANGED: THROWING STARS — 280px ≤ dist ≤ 480px, _rangedCooldown == 0
        //   Style:     RangedStyle.Throw — arm lifts up then swings forward
        //   Telegraph: 45 ticks (15% standing, 85% drifting; pink flash at 30 ticks remaining)
        //   Attack:    10 ticks (1–3 InvaderThrowingStars, speed 10, spread ±8°)
        //   Burst:     rolls 1–3; 25% chance of single shot
        //   Recovery:  55 ticks
        //   Cooldown:  180 ticks (forces melee/stab first)
        //   Damage:    RangedDamage = 30
        //
        // SECONDARY RANGED: CROSSBOW — 350px ≤ dist ≤ 600px, _secondaryRangedCooldown == 0
        //   35% chance to use when both primary and secondary are in range.
        //   Style:     RangedStyle.Crossbow — arm extends horizontally, quick click-fire
        //   Base telegraph: 32 ticks (80% standing); bolt speed 14, gravity 0.02f (flat trajectory)
        //   Recovery:  50 ticks
        //   Cooldown:  240 ticks (~4 s between bursts)
        //   Damage:    SecondaryRangedDamage = 35, knockback 4
        //
        //   PATTERN 0 — single shot (55% chance)
        //     Telegraph: 32 ticks; white flash at 30 ticks remaining
        //     Shots: 1
        //
        //   PATTERN 1 — controlled burst (35% chance)
        //     Telegraph: 42 ticks (+10 extra); yellow flash at 30 ticks remaining
        //     Shots: fire, wait 30, fire, wait 60, fire  (3 total)
        //
        //   PATTERN 2 — full barrage (10% chance)
        //     Telegraph: 62 ticks (+30 extra); red flash at 30 ticks remaining
        //     Shots: fire×3 with 30-tick pauses, then fire×3 with 5-tick pauses  (6 total)
        //
        // POUNCE (via GlobalNPC FighterAI) — triggered when dist² > 40000/Aggression, has LOS
        //   Launch:    TopSpeed × 5 = 16 px/f toward (player.Center + 100px up)
        //   Telegraph: 30 ticks before launch
        //   Cooldown:  300 ticks

        // ─────────────────────────────────────────────────────────────────────────
        // Setup
        // ─────────────────────────────────────────────────────────────────────────

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
        }

        public override void SetDefaults()
        {
            NPC.width    = 20;
            NPC.height   = 42;
            NPC.lifeMax  = 6000;
            NPC.defense  = 28;
            NPC.damage   = 0; // No contact damage — all damage delivered via weapon hitbox projectiles + dive
            NPC.knockBackResist = 0.3f;
            NPC.aiStyle  = -1;
            NPC.HitSound   = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.value    = 50000f;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Attacks
        // ─────────────────────────────────────────────────────────────────────────

        protected override void DoMeleeAttack()
        {
            // The sword sprite swings visually via _weaponAnim (handled by base class).
            // Damage is dealt through a short-lived invisible hostile hitbox at the blade tip.
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.6f, PitchVariance = 0.3f }, NPC.Center);
            TryMeleeHit();
        }

        protected override void DoStabAttack()
        {
            // Thrust — the lunge velocity is applied by the base StabAttack phase.
            // Hit at a longer reach to match the extended stab range.
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, PitchVariance = 0.15f }, NPC.Center);
            TryMeleeHit(reach: StabRange * 0.55f);
        }

        protected override void DoRangedAttack()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Player target = Main.player[NPC.target];

            if (IsSecondaryRangedActive)
            {
                // ── Heavy crossbow: single bolt, tight spread, high speed ──────────
                // Aim from the ninja's SHOULDER toward the target's UPPER CHEST. The bolt has very
                // low gravity, so a center→center shot fired from a low NPC center against a target
                // on raised / sloping ground tends to clip the slope. Raising both muzzle and aim
                // point puts the trajectory above the slope line. The bolt still hits roughly
                // chest-level on a standing player.
                SoundEngine.PlaySound(SoundID.Item98 with { Volume = 0.8f, PitchVariance = 0.1f }, NPC.Center);
                Vector2 muzzle = NPC.Center + new Vector2(0f, -NPC.height * 0.20f); // raised shoulder
                Vector2 aimAt  = target.Center + new Vector2(0f, -target.height * 0.25f); // upper chest
                Vector2 toTarget = aimAt - muzzle;
                if (toTarget == Vector2.Zero) toTarget = new Vector2(NPC.direction, 0f);
                toTarget.Normalize();
                float spread = MathHelper.ToRadians(Main.rand.NextFloat(-3f, 3f));
                Vector2 vel  = toTarget.RotatedBy(spread) * 14f; // HeavyCrossbow shootSpeed (higher than friendly bolt to compensate for reduced gravity)
                Projectile.NewProjectile(
                    NPC.GetSource_FromThis(), muzzle, vel,
                    ModContent.ProjectileType<Projectiles.Enemy.InvaderCrossbowBolt>(),
                    SecondaryRangedDamage, 4f, Main.myPlayer);
            }
            else
            {
                // ── Throwing stars: 1–3 per trigger, wider spread ─────────────────
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.5f, PitchVariance = 0.2f }, NPC.Center);
                int count = Main.rand.Next(1, 4);
                for (int i = 0; i < count; i++)
                {
                    Vector2 toTarget = target.Center - NPC.Center;
                    toTarget.Normalize();
                    float spread = MathHelper.ToRadians(Main.rand.NextFloat(-8f, 8f));
                    Vector2 vel  = toTarget.RotatedBy(spread) * 10f;
                    Projectile.NewProjectile(
                        NPC.GetSource_FromThis(), NPC.Center, vel,
                        ModContent.ProjectileType<Projectiles.Enemy.InvaderThrowingStar>(),
                        RangedDamage, 2f, Main.myPlayer);
                }
            }
        }
    }
}
