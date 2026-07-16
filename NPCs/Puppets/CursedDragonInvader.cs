using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Armors;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Weapons.Enemy;
using tsorcRevamp.Items.Weapons.Melee.Spears;
using tsorcRevamp.NPCs.AI;

namespace tsorcRevamp.NPCs.Puppets
{
    /// <summary>
    /// Winged spear-and-spellcaster invader built on the Cursed Dragon armor set.  Fights on the
    /// ground with PilgrimSpontoon halberd combos, kites with three ranged weapons (arcane ball =
    /// primary, an enemy Venom Staff = secondary with burst patterns + a 5-meteor pentagram finisher,
    /// a MeteorStorm = magic with a rare 7-second meteor rain), and breathes red Ancient-Demon fire.
    /// Breath, hyper-armor, sustained magic, and the cross-weapon finisher all run through the shared
    /// <see cref="PuppetNPC"/> state machine.
    /// </summary>
    [AutoloadBossHead]
    public class CursedDragonInvader : PuppetNPC
    {
        public override string BossHeadTexture => "tsorcRevamp/NPCs/Puppets/CursedDragonInvader_Head_Boss";

        // Index into SecondaryRangedBurstPatterns that ends with the meteor pentagram finisher.
        private const int VenomFinisherPattern = 3;
        private const int BreathDamage = 30;
        private const int MeteorRainChance = 14;   // % of magic casts that become the 7 s rain
        private const int MeteorRainTicks  = 420;  // ~7 s sustained rain

        private bool _meteorRainActive;

        // ── Multi-blast meteor triad state (non-rain magic casts) ──────────────────
        private int   _meteorBurstsRemaining;  // blasts left to fire after the one just cast
        private int   _meteorBurstGapTicks;    // ticks between blasts
        private int   _meteorBurstNextInTicks; // countdown to the next blast
        private float _meteorBurstSpread;      // flank x-offset
        private float _meteorBurstFlankJitter;
        private float _meteorBurstCenterJitter;

        protected override string InvaderTitle => "Cursed Dragon";
        protected override bool IsGreatInvader => true;

        // ── Wings / flight ──────────────────────────────────────────────────────────
        protected override bool HasWings => true;
        protected override int WingsAccessoryItemType => ItemID.TatteredFairyWings;
        protected override EnemyFlightConfig FlightConfig => new EnemyFlightConfig
        {
            HoverAltitude = 230f,
            HoverSideOffset = 110f,
            HoverTopSpeed = 4.7f,
            TakeOffSpeed = 8.5f,
            DiveAcceleration = 0.55f,
            DiveTopSpeed = 12f,
            LandSpeed = 6.5f,
            MaxFlightTicks = 660,
            CooldownTicks = 110,
            TakeOffTicks = 32,
            HoverDwellTicks = 90,
            StrafeTicks = 60,
            DiveAttackTicks = 46,
            LandTicks = 80,
            WingFlapSpeed = 0.085f,
            StrafeArcHeight = 55f, // breath-sweep strafe rises through the middle for a natural swoop
        };
        protected override int RandomTakeoffChance => 17; // takes to the air more readily
        protected override float FlightHpEscalationFrac => 0.60f;

        // ── Loadout ───────────────────────────────────────────────────────────────
        protected override int HeadArmorItemType => ModContent.ItemType<CursedDragonHelmet>();
        protected override int BodyArmorItemType => ModContent.ItemType<CursedDragonArmor>();
        protected override int LegsArmorItemType => ModContent.ItemType<CursedDragonGreaves>();

        // Spear drives melee combos (Halberd archetype) AND the spear poke; its sprite is also shown
        // while casting the arcane ball (primary ranged), since the spear is the arcane ball's caster.
        protected override int MeleeWeaponItemType => ModContent.ItemType<EnemyPilgrimSpontoon>();
        protected override int SpearWeaponItemType => ModContent.ItemType<EnemyPilgrimSpontoon>();
        protected override int RangedWeaponItemType => ModContent.ItemType<EnemyPilgrimSpontoon>();
        protected override int SecondaryRangedWeaponItemType => ModContent.ItemType<EnemyVenomStaff>();
        protected override int MagicWeaponItemType => ModContent.ItemType<EnemyMeteorStorm>();

        // ── Damage (late hardmode: post-mech, ~Golem/Plantera tier; no SHM scaling) ──
        protected override int MeleeDamage => 60;   // spear combos
        protected override int SpearDamage => 55;   // spear poke / dive thrust
        protected override int RangedDamage => 45;  // arcane ball
        protected override int SecondaryRangedDamage => 40; // venom fang (multi-projectile)
        protected override int MagicDamage => 50;   // meteor

        // ── Melee ───────────────────────────────────────────────────────────────────
        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Halberd;
        protected override int MeleeComboChance => 60;
        protected override float ComboMaxStartRange => 260f;
        protected override float MeleeRange => 88f;
        protected override float StabRange => 170f;
        protected override float SpearRange => 245f;
        protected override int SpearTelegraphTicks => 42;
        protected override int SpearAttackTicks => 14;
        protected override int SpearRecoveryTicks => 34;
        protected override int SpearCooldownAfterUse => 80;
        protected override float SpearPushSpeedMult => 0.75f;

        // ── Movement ──────────────────────────────────────────────────────────────
        protected override float TopSpeed => 3.0f;
        protected override float Acceleration => 0.10f;
        protected override int CasualStrollChance => 10;
        protected override float PuppetJumpPower => 10f;
        protected override float PuppetJumpBoost => 7f;
        protected override bool PuppetCanDoubleJump => true;
        protected override float PuppetDoubleJumpPower => 7f;

        // ── Primary ranged: arcane ball (also the aerial hover shot) ────────────────
        protected override RangedStyle RangedAnimStyle => RangedStyle.Throw;
        protected override float RangedRange => 560f;
        protected override float MinRangedRange => 150f;
        protected override int RangedTelegraphTicks => 40;
        protected override int RangedCooldownAfterUse => 180;
        protected override int MaxRangedBurst => 1;
        protected override int StandingRangedChance => 45;
        protected override int RangedComboChance => 0; // no ranged combos from the spear archetype

        // Arcane ball ground volleys.  Pattern 3's final "shot" is a 12-ball circle burst.
        private const int ArcaneCirclePattern = 3;
        protected override int[][] PrimaryRangedBurstPatterns => new int[][]
        {
            new int[] { 30 },                                       // 0: 2 shots, 30 apart
            new int[] { 30, 30 },                                   // 1: 3 shots, 30 apart
            new int[] { 60, 20, 20, 20, 20, 20, 20, 20 },           // 2: 9 shots (2@60, then 7@20)
            new int[] { 10, 60 },                                   // 3: 2 shots (10 apart), 60, then a 12-ball circle
        };
        protected override int[] PrimaryRangedBurstChances => new int[] { 35, 30, 20, 15 };
        protected override int[] PrimaryRangedBurstTelegraphExtras => new int[] { 0, 0, 10, 12 };
        protected override Color[] PrimaryRangedBurstFlashColors => new Color[]
        {
            new Color(120, 180, 255),
            new Color(120, 180, 255),
            new Color(150, 160, 255),
            new Color(180, 120, 255), // circle-burst finisher — brighter
        };

        // ── Secondary ranged: enemy Venom Staff (shotgun blasts + pattern bursts) ───
        protected override RangedStyle SecondaryRangedAnimStyle => RangedStyle.Crossbow;
        protected override float SecondaryRangedRange => 640f;
        protected override float SecondaryRangedMinRange => 150f;
        protected override int SecondaryRangedTelegraphTicks => 36;
        protected override int SecondaryRangedCooldownAfterUse => 160;
        protected override int SecondaryRangedChance => 50;
        protected override int SecondaryStandingRangedChance => 70;
        protected override Color SecondaryRangedFlashColor => new Color(180, 90, 255);

        // Player got in close → hop back and fire a single venom blast to reset spacing.
        protected override float SecondaryRangedBackhopRange => 120f;  // ~7.5 tiles
        protected override int SecondaryRangedBackhopChance => 40;     // competes with melee combos at close range
        protected override float SecondaryRangedBackhopSpeed => 6.5f;
        protected override float SecondaryRangedBackhopUpSpeed => 5.5f;

        // Pattern 0 — single shot (close band / spacing reset)
        // Pattern 1 — shoot, 30, shoot, 60, shoot            (3 shots)
        // Pattern 2 — shoot, 60, shoot, 30, shoot            (3 shots)
        // Pattern 3 — 6 shots 30 apart, then 60 → meteor pentagram finisher (7th "shot")
        protected override int[][] SecondaryRangedBurstPatterns => new int[][]
        {
            new int[] { },
            new int[] { 30, 60 },
            new int[] { 60, 30 },
            new int[] { 30, 30, 30, 30, 30, 60 },
        };
        protected override int[] SecondaryRangedBurstTelegraphExtras => new int[] { 0, 8, 8, 20 };
        protected override Color[] SecondaryRangedBurstFlashColors => new Color[]
        {
            new Color(180, 90, 255),  // single — violet
            new Color(180, 90, 255),  // pattern 1 — violet
            new Color(150, 60, 230),  // pattern 2 — deeper violet
            Color.Red,                // pattern 3 — finisher danger red
        };
        protected override int[] SecondaryRangedBurstChances => new int[] { 40, 25, 25, 10 };

        // ── Magic: MeteorStorm (instant 3-storm, rare 7 s rain) ─────────────────────
        protected override float MagicRange => 860f;
        protected override float MinMagicRange => 240f;   // usable across more of the fight
        protected override int MagicTelegraphTicks => 60;
        protected override int MagicRecoveryTicks => 75;
        protected override int MagicCooldownAfterUse => 180; // fires more often
        protected override int MagicPreferenceChance => 55;  // meteors compete with arcane ball, not just fill gaps

        // ── Fire breath: red Ancient-Demon flame, usable grounded or in a flying sweep ──
        protected override bool CanBreathe => true;
        protected override bool BreathAllowedAirborne => true;
        protected override float BreathRange => 520f;
        protected override float MinBreathRange => 60f;
        protected override int BreathTelegraphTicks => 110;
        protected override int BreathDurationTicks => 70;
        protected override int BreathRecoveryTicks => 45;
        protected override int BreathCooldownAfterUse => 360;
        protected override int BreathChance => 4;
        protected override Color BreathTelegraphFlashColor => Color.OrangeRed;

        // ── HP gating for the venom staff / cursed knives unlock ────────────────────
        private bool BelowEnrageHpThreshold => NPC.life <= NPC.lifeMax * 0.70f;

        // Venom staff (secondary ranged) is locked until the dragon drops below 70% HP.
        protected override bool SecondaryRangedAvailable => BelowEnrageHpThreshold;

        // ── Agility: proactive projectile evasion + preemptive quick-step ───────────
        protected override bool EvadesProjectiles => true;          // jump / roll vs incoming ranged
        protected override int PreemptiveQuickStepChance => 35;     // dash THROUGH a meleeing player
        // Land past the player with room, and a recovery window so it can't instantly re-attack — but short
        // enough that the dash still buys tempo.  (~0.6 s recovery, then the next attack still telegraphs.)
        protected override int QuickStepRecoveryTicks => 36;
        protected override float QuickStepForwardRoom => 56f;

        // ── Cursed Knives (unlocks below 70% HP; usable grounded or flying) ──────────
        protected override bool CanThrowCursedKnives => BelowEnrageHpThreshold;
        protected override int CursedKnivesWeaponItemType => ModContent.ItemType<EnemyCursedKnife>();
        protected override int CursedKnivesChance => 18;
        protected override float CursedKnivesRange => 780f;
        protected override float CursedKnivesMinRange => 0f;
        protected override float CursedKnivesCloseRange => 220f; // ≤ this: 1 volley, tight 45° spread
        protected override float CursedKnivesMidRange => 470f;   // ≤ this: 2 back-to-back volleys
        protected override int CursedKnivesTelegraphTicks => 45;
        protected override int CursedKnivesBackToBackGap => 8;
        protected override int CursedKnivesFarGap => 30;
        protected override int CursedKnivesCooldownAfterUse => 420;
        private const int CursedKnivesDamage = 35;

        // ── Hyper-armor across the whole telegraph+attack window ────────────────────
        protected override bool HyperArmorDuringTelegraph => true;

        // ── Telegraph flash colors / weapon draw tuning ─────────────────────────────
        protected override Color MeleeTelegraphFlashColor => new Color(255, 230, 180);
        protected override Color SpearTelegraphFlashColor => new Color(140, 210, 255);
        protected override Color RangedTelegraphFlashColor => new Color(120, 180, 255);
        protected override Color MagicTelegraphFlashColor => new Color(255, 80, 60);
        protected override Vector2 MeleeHandleNorm => new Vector2(0.18f, 0.78f);
        protected override float MeleeWeaponRotationOffset => MathHelper.ToRadians(-8f);

        // ── Spear draw: grip slides along the shaft so the spear extends/retracts like the player's ──
        protected override bool DrawWeaponAsSpear => true;
        // Use the holdout-projectile sprite (full shaft + head) instead of the small item icon.
        protected override string SpearDrawTexturePath => "tsorcRevamp/Projectiles/Melee/Spears/PilgrimSpontoonProj";
        // Measured directly from the PilgrimSpontoonProj.png pixels (110x110): the ornate head/tip
        // sits at the top-left corner and the shaft runs straight to the bottom-right corner — a
        // NW-SE diagonal, not the NE-SW diagonal the broadsword convention assumes.
        protected override Vector2 SpearHeadNorm => new Vector2(0.03f, 0.03f); // tip, top-left
        protected override Vector2 SpearBaseNorm => new Vector2(0.95f, 0.95f); // butt, bottom-right
        // TickWeaponAnim's rotation targets assume the standard broadsword natural angle (-45°,
        // handle lower-left / blade upper-right).  This sprite's natural angle is -135° (90° off in
        // the other direction), so a +90° draw-only correction realigns it — after which every
        // existing rotation target (thrust, telegraph, overhead/underhand arcs, ranged throw, etc.)
        // reads correctly without further per-phase tuning.
        protected override float SpearDrawRotationOffset => MathHelper.PiOver2;
        // Combo hitboxes reach out to roughly the spear's length so swings/thrusts connect at their visual reach.
        protected override float ComboReachBase => 230f;
        protected override float MeleeEngageRange => 200f; // start spear combos from spear distance, not point-blank
        // Keep pursuing through the wind-up so the player can't just step out of range.
        protected override bool SlowDownBeforeMelee => false;
        // Meteor staff is gripped low on the shaft during the cast (≈30% from the bottom).
        protected override Vector2 MagicGripNorm => new Vector2(0.5f, 0.72f);

        private Vector2 MouthPosition => NPC.Center + new Vector2(NPC.spriteDirection * 16f, -16f);

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
        }

        public override void SetDefaults()
        {
            NPC.width = 20;
            NPC.height = 42;
            NPC.lifeMax = 25000;
            NPC.defense = 40;
            NPC.damage = 0;       // all damage via weapon hitboxes / projectiles
            NPC.knockBackResist = 0.12f;
            NPC.aiStyle = -1;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.value = 55000f;
            NPC.boss = true;
            NPC.npcSlots = 8f;
            NPC.lavaImmune = true;

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.PoiseMax = 60f;                 // heavy armored dragon — slow to stagger
            globalNPC.PoiseStaggerResetsAI = true;
            globalNPC.NavGiveUpTicks = 180;
            globalNPC.CanUseRopes = true;

            // Agile: moderate proactive projectile dodging (jumps / i-frame rolls at incoming shots).
            globalNPC.Agility = 0.35f;

            // Aggressive teleport with the black/purple plague style (leaves a curse cloud on arrival).
            globalNPC.CanTeleport = true;
            globalNPC.TeleportStyle = TeleportStyle.Aggressive;
            globalNPC.TeleportVisualStyle = TeleportVisualStyle.Plague;

            // Aggressive spacing reactions: punish ranged kiting with a LeapForward, charge back in with
            // a hyper-armored RunningDash, RetreatDash to reset, plus an i-frame QuickStep dodge.
            EvasiveProfile.CursedDragon(globalNPC);
        }

        protected override void RunMovementAI(float speedMult)
        {
            var g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            g.NavSearchRadius = 80;
            g.RemembersLastKnownPos = true;
            SmartFighter4AI.Run(NPC,
                topSpeed: TopSpeed * speedMult,
                acceleration: Acceleration,
                doorBreakingDamage: 5,
                attackRange: RangedRange);
        }

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitByItem(player, item, hit, damageDone);
            // EvasiveOnHit self-suppresses while mid-attack (InAttack) / mid-evasion, so breath and
            // committed swings are safe — no manual phase guard needed.
            tsorcRevampAIs.EvasiveOnHit(NPC, true);
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitByProjectile(projectile, hit, damageDone);
            tsorcRevampAIs.EvasiveOnHit(NPC, projectile.DamageType == DamageClass.Melee);
        }

        // ── Melee / spear ───────────────────────────────────────────────────────────
        protected override void DoMeleeAttack()
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.55f, PitchVariance = 0.25f }, NPC.Center);
            TryMeleeHit();
        }

        protected override void DoStabAttack()
        {
            SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.65f, PitchVariance = 0.15f }, NPC.Center);
            TryMeleeHit(reach: StabRange * 0.65f);
        }

        protected override void DoSpearAttack()
        {
            SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.75f, PitchVariance = 0.12f }, NPC.Center);
            TryMeleeHit(reach: SpearRange * 0.85f); // reach matches the trigger range so the poke connects
        }

        // ── Ranged: arcane ball (primary) + venom staff (secondary) ─────────────────
        protected override void DoRangedAttack()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Player target = Main.player[NPC.target];

            if (IsSecondaryRangedActive)
            {
                // Venom staff.  Pattern 3's final shot is the 5-meteor pentagram finisher.
                if (ActiveBurstPatternIndex == VenomFinisherPattern && IsFinalBurstShot)
                {
                    FireMeteorPentagram(target);
                }
                else
                {
                    FireVenomBlast(target);
                }
                return;
            }

            // Primary: arcane ball.  Pattern 3's final shot is a 12-ball circle burst.
            // (The aerial hover shot uses shotsOverride:1 — no pattern — so it stays a single ball.)
            if (ActiveBurstPatternIndex == ArcaneCirclePattern && IsFinalBurstShot)
            {
                FireArcaneCircle(target);
            }
            else
            {
                FireArcaneBall(target);
            }
        }

        private void FireArcaneCircle(Player target)
        {
            SoundEngine.PlaySound(SoundID.Item25 with { Volume = 0.85f, Pitch = -0.2f }, NPC.Center);
            Vector2 origin = NPC.Center + new Vector2(0f, -8f);
            const int count = 12;
            float baseAngle = (target.Center - origin).ToRotation(); // first ball aimed at the player
            for (int i = 0; i < count; i++)
            {
                float ang = baseAngle + MathHelper.TwoPi * i / count;
                Vector2 vel = ang.ToRotationVector2() * 8f;
                Projectile.NewProjectile(
                    NPC.GetSource_FromThis(), origin, vel,
                    ModContent.ProjectileType<Projectiles.Enemy.Weapons.EnemyPilgrimArcaneBall>(),
                    RangedDamage, 1f, Main.myPlayer);
            }
        }

        private void FireArcaneBall(Player target)
        {
            Vector2 muzzle = NPC.Center + new Vector2(NPC.direction * 18f, -8f);
            Vector2 aimAt = target.Center + target.velocity * 8f;
            Vector2 vel = (aimAt - muzzle).SafeNormalize(new Vector2(NPC.direction, 0f)).RotatedByRandom(MathHelper.ToRadians(5f)) * 10.5f;

            SoundEngine.PlaySound(SoundID.Item25 with { Volume = 0.6f, PitchVariance = 0.12f }, NPC.Center);
            Projectile.NewProjectile(
                NPC.GetSource_FromThis(), muzzle, vel,
                ModContent.ProjectileType<Projectiles.Enemy.Weapons.EnemyPilgrimArcaneBall>(),
                RangedDamage, 2f, Main.myPlayer);
        }

        private void FireVenomBlast(Player target)
        {
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.65f, PitchVariance = 0.15f }, NPC.Center);
            Vector2 muzzle = NPC.Center + new Vector2(NPC.direction * 18f, -6f);
            Vector2 baseAim = (target.Center + target.velocity * 6f - muzzle).SafeNormalize(new Vector2(NPC.direction, 0f));
            for (int i = 0; i < 4; i++) // shotgun spread of purple fangs
            {
                float spread = MathHelper.ToRadians(Main.rand.NextFloat(-11f, 11f));
                Vector2 vel = baseAim.RotatedBy(spread) * Main.rand.NextFloat(9.5f, 12f);
                Projectile.NewProjectile(
                    NPC.GetSource_FromThis(), muzzle, vel,
                    ModContent.ProjectileType<Projectiles.Enemy.Weapons.EnemyVenomStaffProj>(),
                    SecondaryRangedDamage, 2f, Main.myPlayer);
            }
        }

        // 5 meteors spawned at the tips of a pentagram above the dragon, all converging on the player.
        private void FireMeteorPentagram(Player target)
        {
            SoundEngine.PlaySound(SoundID.Item88 with { Volume = 0.85f, Pitch = -0.2f }, NPC.Center);
            Vector2 center = new Vector2(NPC.Center.X, NPC.Center.Y - 440f);
            Vector2 aimAt = target.Center;
            float radius = 16f * 5f; // 5 tiles
            for (int i = 0; i < 5; i++)
            {
                // 144° steps trace a 5-point star (pentagram); start at the top tip.
                float ang = MathHelper.ToRadians(-90f + i * 144f);
                Vector2 spawn = center + new Vector2((float)Math.Cos(ang), (float)Math.Sin(ang)) * radius;
                Vector2 vel = (aimAt - spawn).SafeNormalize(Vector2.UnitY) * 3.9f; // ~70% slower than before
                Projectile.NewProjectile(
                    NPC.GetSource_FromThis(), spawn, vel,
                    ModContent.ProjectileType<Projectiles.Enemy.EnemyMeteorStormMeteor>(),
                    MagicDamage, 2f, Main.myPlayer);
            }
        }

        // ── Magic: meteor triad burst (HP-scaled blast count/spacing/spread) + rare sustained rain ──
        // >80% HP: 2 blasts, 60 ticks apart, baseline spread.
        // 50-80% HP: 3 blasts, 45 ticks apart, wider spread.
        // <50% HP: 4 blasts, 30 ticks apart, slightly wider spread still.
        protected override void DoMagicAttack()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Player target = Main.player[NPC.target];

            if (Main.rand.Next(100) < MeteorRainChance)
            {
                // Rare: channel a 7-second meteor rain (DoMagicTick spawns over the extended phase).
                SoundEngine.PlaySound(SoundID.Item88 with { Volume = 0.35f, PitchVariance = 0.08f }, NPC.Center);
                _meteorRainActive = true;
                _magicAttackTicksOverride = MeteorRainTicks;
                return;
            }

            _meteorRainActive = false;

            float hpFrac = (float)NPC.life / NPC.lifeMax;
            int totalBursts;
            if (hpFrac < 0.50f)
            {
                totalBursts = 4; _meteorBurstGapTicks = 30;
                _meteorBurstSpread = 420f; _meteorBurstFlankJitter = 42f; _meteorBurstCenterJitter = 28f;
            }
            else if (hpFrac <= 0.80f)
            {
                totalBursts = 3; _meteorBurstGapTicks = 45;
                _meteorBurstSpread = 380f; _meteorBurstFlankJitter = 38f; _meteorBurstCenterJitter = 25f;
            }
            else
            {
                totalBursts = 2; _meteorBurstGapTicks = 60;
                _meteorBurstSpread = 300f; _meteorBurstFlankJitter = 30f; _meteorBurstCenterJitter = 20f;
            }

            // First blast fires immediately; DoMagicTick fires the rest at _meteorBurstGapTicks apart.
            FireMeteorTriad(target);
            _meteorBurstsRemaining = totalBursts - 1;
            _meteorBurstNextInTicks = _meteorBurstGapTicks;

            // Channel length: enough ticks for every remaining blast, plus a small buffer so the last
            // blast's meteors spawn before MagicAttack ends and hands off to recovery.
            _magicAttackTicksOverride = _meteorBurstsRemaining * _meteorBurstGapTicks + 14;
        }

        protected override void DoMagicTick(int ticksRemaining)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            if (_meteorRainActive)
            {
                if (ticksRemaining % 14 == 0)
                {
                    // Rain spreads randomly across a wide band over the player.  Unlike the triad burst
                    // (one cast sound per blast), the sustained rain gets its own sound per meteor so
                    // each of the ~20 shots reads as a distinct impact-incoming cue.
                    SoundEngine.PlaySound(SoundID.Item88 with { Volume = 0.35f, PitchVariance = 0.08f }, NPC.Center);
                    SpawnSkyMeteor(Main.player[NPC.target], xOffset: Main.rand.NextFloat(-260f, 260f), jitter: 0f, leadMult: 10f);
                }
                return;
            }

            if (_meteorBurstsRemaining <= 0)
            {
                return;
            }
            if (--_meteorBurstNextInTicks <= 0)
            {
                FireMeteorTriad(Main.player[NPC.target]);
                _meteorBurstsRemaining--;
                _meteorBurstNextInTicks = _meteorBurstGapTicks;
            }
        }

        // Three meteors: left and right flanks land near the player's current position; the center
        // meteor leads aggressively in the player's run direction so simply kiting one way doesn't
        // guarantee a dodge.  Spread/jitter come from the HP-scaled tier set in DoMagicAttack.
        private void FireMeteorTriad(Player target)
        {
            SoundEngine.PlaySound(SoundID.Item88 with { Volume = 0.35f, PitchVariance = 0.08f }, NPC.Center);
            SpawnSkyMeteor(target, xOffset: -_meteorBurstSpread, jitter: _meteorBurstFlankJitter, leadMult: 4f);
            SpawnSkyMeteor(target, xOffset:  0f,                 jitter: _meteorBurstCenterJitter, leadMult: 80f);
            SpawnSkyMeteor(target, xOffset:  _meteorBurstSpread, jitter: _meteorBurstFlankJitter, leadMult: 4f);
        }

        private void SpawnSkyMeteor(Player target, float xOffset, float jitter, float leadMult)
        {
            Vector2 targetPos = target.Center + target.velocity * leadMult;
            Vector2 spawn = new Vector2(
                targetPos.X + xOffset + Main.rand.NextFloat(-jitter, jitter),
                NPC.Center.Y - Main.rand.NextFloat(520f, 680f));
            Vector2 vel = (targetPos - spawn).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(3.5f, 4.4f); // ~70% slower than before
            Projectile.NewProjectile(
                NPC.GetSource_FromThis(), spawn, vel,
                ModContent.ProjectileType<Projectiles.Enemy.EnemyMeteorStormMeteor>(),
                MagicDamage, 2f, Main.myPlayer);
        }

        // ── Fire breath (red Ancient-Demon flame) ───────────────────────────────────
        protected override void DoBreathWindup(int elapsed)
        {
            if (Main.dedServ)
            {
                return;
            }
            float t = MathHelper.Clamp(elapsed / (float)BreathTelegraphTicks, 0f, 1f);
            float scale = t < 0.4f ? 0.45f : MathHelper.Lerp(0.6f, 1.9f, (t - 0.4f) / 0.6f);
            Vector2 mouth = MouthPosition;
            int count = t < 0.4f ? 1 : 2;
            for (int i = 0; i < count; i++)
            {
                Dust d = Dust.NewDustPerfect(mouth + Main.rand.NextVector2Circular(3f, 3f), DustID.RedTorch,
                    Main.rand.NextVector2Circular(0.6f, 0.6f), 0, default, scale);
                d.noGravity = true;
            }
            Lighting.AddLight(mouth, 0.6f * scale, 0.18f * scale, 0.03f);
        }

        protected override void OnBreathStart()
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.5f, Pitch = -0.1f }, NPC.Center);
            }
        }

        protected override void DoBreathTick(int ticksRemaining)
        {
            Lighting.AddLight(MouthPosition, 0.9f, 0.35f, 0.05f);
            if (Main.netMode == NetmodeID.MultiplayerClient || ticksRemaining % 4 != 0)
            {
                return;
            }
            Player target = Main.player[NPC.target];
            Vector2 mouth = MouthPosition;
            Vector2 vel = UsefulFunctions.Aim(mouth, target.Center, 9f) + Main.rand.NextVector2Circular(1.5f, 1.5f);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), mouth, vel,
                ModContent.ProjectileType<Projectiles.Enemy.CursedDragonInvaderBreath>(), BreathDamage, 0f, Main.myPlayer);
        }

        // ── Cursed Knives: throw 3 sticky knives in a tight 45° spread ──────────────
        protected override void DoCursedKnivesThrow()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.5f, PitchVariance = 0.2f }, NPC.Center);
            Player target = Main.player[NPC.target];
            Vector2 muzzle = NPC.Center + new Vector2(NPC.direction * 14f, -10f);
            Vector2 baseAim = (target.Center - muzzle).SafeNormalize(new Vector2(NPC.direction, 0f));
            // 3 knives across a 45° total spread.
            for (int i = -1; i <= 1; i++)
            {
                Vector2 vel = baseAim.RotatedBy(MathHelper.ToRadians(22.5f) * i) * 11f;
                Projectile.NewProjectile(
                    NPC.GetSource_FromThis(), muzzle, vel,
                    ModContent.ProjectileType<Projectiles.Enemy.Weapons.CursedDragonKnife>(),
                    CursedKnivesDamage, 2f, Main.myPlayer);
            }
        }

        // ── Aerial dive becomes a spear thrust ──────────────────────────────────────
        protected override void DoAerialDiveTelegraph()
        {
            base.DoAerialDiveTelegraph(); // shows the spear + orange flash
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.5f, PitchVariance = 0.12f }, NPC.Center);
            }
        }

        protected override void DoAerialDiveHit()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            // Forward spear-thrust hitbox in the dive direction.
            int boxW = 150, boxH = 70;
            Vector2 center = NPC.Center + new Vector2(NPC.direction * 30f, 0f);
            Projectile.NewProjectile(
                NPC.GetSource_FromThis(), center, Vector2.Zero,
                ModContent.ProjectileType<Projectiles.Enemy.Weapons.PuppetMeleeHitbox>(),
                (int)(SpearDamage * 1.25f), 4f, Main.myPlayer, boxW, boxH);
        }

        // ── Visual: white dust + light building at the spear tip through the whole ranged
        // telegraph, with one strong pulse right before the shot fires ─────────────────
        public override void PostAI()
        {
            base.PostAI();
            if (Phase != AttackPhase.RangedTelegraph || IsSecondaryRangedActive || Main.dedServ)
                return;

            Vector2 spearTip = GetSpearTipWorldPosition(92f);
            float t = RangedTelegraphTicks > 0
                ? 1f - (float)PhaseTimer / RangedTelegraphTicks // 0 at telegraph start, 1 at the shot
                : 1f;

            // Steady sparkle for the full telegraph, growing denser as the shot nears.
            int dustCount = 2 + (int)(t * 3f); // 2 -> 5 particles/tick
            for (int i = 0; i < dustCount; i++)
            {
                Dust d = Dust.NewDustPerfect(
                    spearTip + Main.rand.NextVector2Circular(7f, 7f),
                    DustID.WhiteTorch,
                    Main.rand.NextVector2Circular(1.5f, 1.5f),
                    0, Color.White,
                    Main.rand.NextFloat(1.2f, 2.0f));
                d.noGravity = true;
                d.fadeIn = 0.9f;
            }

            // Strong pulse burst in the final few ticks — the clearest "it's about to fire" cue.
            if (PhaseTimer <= 6)
            {
                for (int i = 0; i < 4; i++)
                {
                    Dust d = Dust.NewDustPerfect(spearTip, DustID.WhiteTorch,
                        Main.rand.NextVector2Circular(3.5f, 3.5f), 0, Color.White, 2.4f);
                    d.noGravity = true;
                }
            }

            Lighting.AddLight(spearTip, MathHelper.Lerp(0.5f, 1.4f, t), MathHelper.Lerp(0.5f, 1.4f, t), MathHelper.Lerp(0.6f, 1.5f, t));
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<PilgrimSpontoon>(), 2));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<CursedSoul>(), 1, 4, 7));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<DragonEssence>(), 2));
        }
    }
}
