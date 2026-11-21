using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Armor.Enemy;
using tsorcRevamp.Content.Items.Weapons.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using tsorcRevamp.Content.Projectiles.Melee.Spears;
using tsorcRevamp.Content.Projectiles.VFX;
using tsorcRevamp.NPCs.Invaders;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses.Ornsmough
{
    /// <summary>
    /// Ornstein: fast lightning spear. Phase one is Lunging Skewer, the Arc Thrust string, Leaping Plunge and Thunder Javelin
    /// with Reaping Sweep as the roll-through punish. As Giant Ornstein (Smough fell first) he adds Colossus Skewer, Thunder
    /// Lance, Storm Pillar (the staggerable channel) and Colossal Cleave. Constellation is the anti-air answer in both phases.
    /// </summary>
    [AutoloadBossHead]
    public class Ornstein : WardenPuppet
    {
        protected override string InvaderTitle => "Dragon Slayer Ornstein";
        protected override string DespawnFlavorText => LangUtils.GetTextValue("NPCs.Ornstein.DespawnHandler");

        // ── Loadout ──────────────────────────────────────────────────────────────
        protected override int HeadArmorItemType => ModContent.ItemType<OrnsteinHead>();
        protected override int BodyArmorItemType => ModContent.ItemType<OrnsteinBody>();
        protected override int LegsArmorItemType => ModContent.ItemType<OrnsteinLegs>();
        protected override int MeleeWeaponItemType => ModContent.ItemType<OrnsteinSpear>();

        // ── Damage (Expert, before defense) ──────────────────────────────────────
        public const int LightExpert = 180;
        public const int GiantLightExpert = 210;
        public const int SkewerExpert = 200;
        public const int RushExpert = 220;
        public const int ColossusSkewerExpert = 240;
        public const int PlungeExpert = 200;
        public const int BoltExpert = 140;
        public const int SparkExpert = 120;
        public const int GiantSparkExpert = 130;
        public const int ConstellationExpert = 140;
        public const int GiantConstellationExpert = 160;
        public const int JavelinExpert = 140;
        public const int LanceExpert = 170;
        public const int PillarRingExpert = 160;
        public const int PillarStrikeExpert = 160;
        public const int CleaveCrawlerExpert = 130;
        public const int ReapSparkExpert = 140;

        protected override int MeleeDamage
        {
            get
            {
                if (Empowered)
                {
                    return ResolveMeleeDamage(GiantLightExpert);
                }

                return ResolveMeleeDamage(LightExpert);
            }
        }

        // ── Life (Master = authored x 1.275: 50k and 70k removed per kill) ───────
        public const int AuthoredLife = 39216;
        public const int GiantAuthoredLife = 54902;
        protected override int PairedAuthoredLife => AuthoredLife;
        public override int EmpoweredAuthoredLife => GiantAuthoredLife;
        protected override int EmpoweredDefense => 45;
        protected override float EmpoweredPoise => 180f;
        protected override float EmpoweredKnockbackResist => 0.08f;

        // ── Spear geometry: 104x104 held sprite, tip (103,0), butt (0,103) ───────
        public const float SpearDrawScale = 0.8f;
        private const float ReachPerBodyScale = 148f / PairedScale; // 104px reach at 1.35, 115px at 1.5
        public const float CarryGrip = 0.66f;
        public const float ExtendedGrip = 0.8f;
        private const float HeldSpriteSize = 104f;

        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Halberd;
        protected override bool DrawWeaponAsSpear => true;
        protected override bool MirrorSpearRotationByFacing => true;
        protected override string SpearDrawTexturePath => UsefulFunctions.RefactorableFilepath(typeof(OrnsteinSpearProj));
        protected override Vector2 SpearTextureSize => new Vector2(HeldSpriteSize);
        protected override Vector2 SpearHeadNorm => new Vector2(103f, 0f) / HeldSpriteSize;
        protected override Vector2 SpearBaseNorm => new Vector2(0f, 103f) / HeldSpriteSize;
        protected override float SpearDrawRotationOffset => 0f;
        protected override Vector2 MeleeHandleNorm => Vector2.Lerp(SpearHeadNorm, SpearBaseNorm, CarryGrip);
        protected override float MeleeWeaponDrawScale => SpearDrawScale;
        protected override float ComboReachBase => ReachPerBodyScale * BodyScale;
        protected override float MeleeBladeWidth => 14f;
        protected override float MeleeRange => 104f;
        protected override float StabRange => 190f;
        protected override float MeleeEngageRange => 170f;
        protected override float TwoHandedBackGripOffset => -14f;
        protected override bool AuthoredClockCoversJoustDash => true;
        protected override bool UseAuthoredSpearGrip => true;
        protected override Color MeleeTelegraphFlashColor => new Color(255, 225, 140);

        private float SpearReach => ComboReachBase * 0.7f;

        protected override float? OverrideSpearGrip(float swingClockT)
        {
            if (Phase == AttackPhase.MeleeComboAttack && ActiveMeleeComboName == ArcThrustName)
            {
                float pulse = (float)Math.Sin(MathHelper.Clamp(swingClockT, 0f, 1f) * Math.PI);
                return MathHelper.Lerp(CarryGrip, ExtendedGrip, pulse);
            }

            bool thrustingMove = CurrentMove == WardenMove.LungingSkewer || CurrentMove == WardenMove.PincerDash
                || CurrentMove == WardenMove.ColossusSkewer || CurrentMove == WardenMove.LeapingPlunge;
            if (Phase == AttackPhase.Custom && thrustingMove)
            {
                return ExtendedGrip;
            }

            return CarryGrip;
        }

        // ── Movement ─────────────────────────────────────────────────────────────
        protected override float TopSpeed => Empowered ? 3.6f : 4.2f;
        protected override float Acceleration => 0.18f;
        protected override float RunDistance => 320f;
        protected override float RunSpeedMult => 1.25f;
        protected override float ComboTelegraphAdvanceSpeedMult => 0.6f;
        protected override float ComboTelegraphAdvanceStopDistance => 90f;

        // ── Combos ───────────────────────────────────────────────────────────────
        private const string ArcThrustName = "Arc Thrust";
        private const string ReapingSweepName = "Reaping Sweep";
        private const string ColossalCleaveName = "Colossal Cleave";

        protected override MeleeCombo[] MeleeComboPoolOverride => OrnsteinCombos;

        /// <summary>A couched thrust: JoustDash Whip over 20t, live the whole thrust, push 1.0 x walk speed; the grip pulse carries the reach.</summary>
        private static MeleeComboStep ThrustStep(int telegraph, int pause)
        {
            return new MeleeComboStep
            {
                Motion = ComboMotion.JoustDash,
                TelegraphTicks = telegraph,
                AttackTicks = 20,
                PostStepPause = pause,
                DamageMult = 1f,
                ReachMult = 1f,
                ForwardPushMult = 1f,
                Ease = SwingEaseStyle.Whip,
                HitWindowEnd = 1f,
            };
        }

        /*
         * Timing sheet (attack-timing-design §3). On-screen tell = authored (multiplier 1).
         * Every step is live for its whole swing; rolling is judged by how long the moving blade overlaps the player.
         * Arc Thrust:      tell 24 | 3 thrusts of 20t, live 20t + 24t pause -> next live 24t after the last ended |
         *                  step 3 only on a hit or with the target within 140px in front; its bolt leaves on tick 6 | recovery 26
         * Reaping Sweep:   tell 26 | Horizontal 0.2 -> 3.7 rad under the body (200 deg), 8/36 k7, live 44t | recovery 24
         * Colossal Cleave: tell 40 | GroundSlam -1.62 -> 2.0 (210 deg), 12/40 k6, live 52t | 18t pause ->
         *                  rising reap (Underhand 2.0 -> -1.45, 10/36 k7), only on a hit or within 220px | recovery 60
         */
        private static readonly MeleeCombo[] OrnsteinCombos = new[]
        {
            new MeleeCombo
            {
                Name = ArcThrustName, BaseWeight = 100, Preferred = ComboRangeBand.Any, InitialFlashColor = Color.White,
                RecoveryTicks = 26, MoveBrake = 0f,
                Steps = new[] { ThrustStep(24, 24), ThrustStep(0, 24), ThrustStep(0, 0) }
            },
            new MeleeCombo
            {
                Name = ReapingSweepName, BaseWeight = 100, Preferred = ComboRangeBand.Any, InitialFlashColor = Color.Gold,
                RecoveryTicks = 24, MoveBrake = 0.1f,
                Steps = new[] { WeightedStep(ComboMotion.HorizontalSweep, 26, 8, 36, 7f, 0, 1f, 0.9f, 30f) }
            },
            new MeleeCombo
            {
                Name = ColossalCleaveName, BaseWeight = 100, Preferred = ComboRangeBand.Any, InitialFlashColor = Color.OrangeRed,
                RecoveryTicks = 60, HeavyCommit = true, HyperArmor = true, MoveBrake = 0.2f,
                Steps = new[]
                {
                    WeightedStep(ComboMotion.GroundSlam, 40, 12, 40, 6f, 18, 1.24f, 1f, 50f),
                    WeightedStep(ComboMotion.UnderhandArc, 0, 10, 36, 7f, 0, 1.05f, 1f, 30f),
                }
            },
        };

        protected override void ModifyMeleeArcEndpoints(ComboMotion motion, ref float a0, ref float a1)
        {
            switch (motion)
            {
                case ComboMotion.JoustDash:
                    a0 = a1 - 0.3f;
                    break;
                case ComboMotion.HorizontalSweep:
                    a0 = 0.2f;
                    a1 = 3.7f;
                    break;
                case ComboMotion.GroundSlam:
                    a0 = -1.62f;
                    a1 = 2.0f;
                    break;
                case ComboMotion.UnderhandArc:
                    a0 = 2.0f;
                    a1 = -1.45f;
                    break;
            }
        }

        protected override Color SwingArcBodyColor => new Color(255, 205, 90);
        protected override Color SwingArcCoreColor => new Color(255, 250, 225);
        protected override Color SwingArcDarkColor => new Color(110, 70, 15);

        protected override float MeleeStrikeRange(WardenMove move)
        {
            switch (move)
            {
                case WardenMove.ArcThrust:
                    return 190f;
                case WardenMove.ColossalCleave:
                    return 200f;
                case WardenMove.ReapingSweep:
                    return ReactiveRange;
                default:
                    return 0f;
            }
        }

        protected override bool IsFlankerCard(WardenMove move)
        {
            return move == WardenMove.ThunderJavelin || move == WardenMove.ThunderLance || move == WardenMove.LeapingPlunge;
        }

        protected override string ComboNameFor(WardenMove move)
        {
            switch (move)
            {
                case WardenMove.ArcThrust:
                    return ArcThrustName;
                case WardenMove.ReapingSweep:
                    return ReapingSweepName;
                case WardenMove.ColossalCleave:
                    return ColossalCleaveName;
                default:
                    return null;
            }
        }

        private bool TargetCloseInFront(Player target, float range)
        {
            float gap = (target.Center.X - NPC.Center.X) * NPC.direction;
            return gap >= 0f && NPC.Distance(target.Center) <= range;
        }

        protected override bool ShouldContinueMeleeCombo(string comboName, int nextStepIndex, Player target, bool previousStepHit)
        {
            if (comboName == ArcThrustName && nextStepIndex == 2)
            {
                return previousStepHit || TargetCloseInFront(target, 140f);
            }

            if (comboName == ColossalCleaveName)
            {
                return previousStepHit || TargetCloseInFront(target, 220f);
            }

            return true;
        }

        protected override void OnMeleeComboTelegraphTick(MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            base.OnMeleeComboTelegraphTick(combo, step, elapsed, total);

            if (Main.dedServ || elapsed % 2 != 0)
            {
                return;
            }

            Vector2 tip = WeaponPoint(SpearReach);
            Vector2 from = tip + Main.rand.NextVector2CircularEdge(26f, 26f);
            int dustType = DustID.Electric;
            if (combo.Name == ColossalCleaveName)
            {
                dustType = DustID.GoldFlame;
            }

            Dust gather = Dust.NewDustPerfect(from, dustType, (tip - from) * 0.12f, 80, default, 0.9f);
            gather.noGravity = true;
        }

        protected override void OnMeleeComboAttackTick(MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            base.OnMeleeComboAttackTick(combo, step, elapsed, total);

            if (!Main.dedServ && elapsed < step.HitWindowEnd * total)
            {
                Dust trail = Dust.NewDustPerfect(WeaponPoint(SpearReach * Main.rand.NextFloat(0.6f, 1f)), DustID.GoldFlame, Vector2.Zero, 60, default, 1f);
                trail.noGravity = true;
            }

            IEntitySource source = NPC.GetSource_FromThis();
            bool lastThrust = combo.Name == ArcThrustName && _completedComboSteps == 2;
            if (lastThrust && elapsed == 6)
            {
                float boltRange = Empowered ? 180f : 160f;
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.45f, Pitch = 0.4f }, NPC.Center);
                WardenLightning.Spawn(source, WeaponPoint(SpearReach), new Vector2(NPC.direction * 16f, 0f), WardenBoltMode.Bolt, BoltExpert, boltRange / 16f);
            }

            if (combo.Name != ColossalCleaveName || elapsed != step.EaseInTicks)
            {
                return;
            }

            if (step.Motion == ComboMotion.GroundSlam)
            {
                PlayCleaveImpact();
                Vector2 tip = WeaponPoint(SpearReach);
                WardenLightning.SpawnCrawlerPair(source, tip.X, NPC.Bottom.Y - 8f, 7f, 16, 10, CleaveCrawlerExpert);
                return;
            }

            float[] fanDegrees = { 50f, 70f, 90f };
            foreach (float degrees in fanDegrees)
            {
                float radians = MathHelper.ToRadians(degrees);
                Vector2 velocity = new Vector2((float)Math.Cos(radians) * NPC.direction, -(float)Math.Sin(radians)) * 12f;
                WardenLightning.Spawn(source, WeaponPoint(SpearReach), velocity, WardenBoltMode.Bolt, ReapSparkExpert, 30f);
            }
        }

        // Counts finished steps of the running combo on every peer, so only the third thrust throws its bolt.
        private int _completedComboSteps;

        protected override void OnMeleeComboStarted(MeleeCombo combo)
        {
            base.OnMeleeComboStarted(combo);
            _completedComboSteps = 0;
        }

        protected override void OnComboStepCompleted(MeleeComboStep step)
        {
            base.OnComboStepCompleted(step);
            _completedComboSteps++;
        }

        private void PlayCleaveImpact()
        {
            if (Main.dedServ)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.DeerclopsRubbleAttack with { Volume = 0.9f }, NPC.Bottom);
            SoundEngine.PlaySound(SoundID.Thunder with { Volume = 0.45f }, NPC.Bottom);
            UsefulFunctions.ScreenShake(NPC.Bottom, 10f, 18, distanceFalloff: 1800f);
        }

        // ── Selection ────────────────────────────────────────────────────────────
        protected override WardenMove FlankerMove => WardenMove.ThunderJavelin;
        protected override WardenMove RegroupMove => WardenMove.LungingSkewer;
        protected override WardenMove ReactiveMove => WardenMove.ReapingSweep;
        protected override int ReactiveCrossings => 1;
        protected override int ReactiveWindowTicks => 60;
        protected override float ReactiveRange => 120f;

        private int _pillarCooldown;

        protected override void TickBossServerTimers()
        {
            if (_pillarCooldown > 0)
            {
                _pillarCooldown--;
            }
        }

        protected override void FillBag(List<WardenMove> cards)
        {
            if (!Empowered)
            {
                cards.AddRange(new[]
                {
                    WardenMove.LungingSkewer, WardenMove.LungingSkewer, WardenMove.LungingSkewer,
                    WardenMove.ArcThrust, WardenMove.ArcThrust, WardenMove.ArcThrust, WardenMove.ArcThrust,
                    WardenMove.LeapingPlunge, WardenMove.LeapingPlunge,
                    WardenMove.ThunderJavelin, WardenMove.ThunderJavelin,
                    WardenMove.DragonRush, WardenMove.DragonRush,
                    WardenMove.ReapingVault, WardenMove.ReapingVault,
                });
                return;
            }

            cards.AddRange(new[]
            {
                WardenMove.LungingSkewer, WardenMove.LungingSkewer,
                WardenMove.ColossusSkewer, WardenMove.ColossusSkewer,
                WardenMove.ColossalCleave, WardenMove.ColossalCleave,
                WardenMove.ArcThrust, WardenMove.ArcThrust,
                WardenMove.LeapingPlunge, WardenMove.LeapingPlunge,
                WardenMove.ThunderJavelin,
                WardenMove.ThunderLance, WardenMove.ThunderLance,
                WardenMove.StormPillar,
                WardenMove.DragonRush, WardenMove.DragonRush,
                WardenMove.ReapingVault, WardenMove.ReapingVault,
            });
        }

        protected override bool IsMoveEligible(WardenMove move, float distance, float rise, bool clearLine)
        {
            bool grounded = rise <= 64f;
            float skewerMin = Empowered ? 240f : 220f;
            float skewerMax = Empowered ? 620f : 560f;
            float plungeMax = Empowered ? 700f : 640f;

            switch (move)
            {
                case WardenMove.LungingSkewer:
                case WardenMove.ColossusSkewer:
                    return grounded && clearLine && distance >= skewerMin && distance <= skewerMax;
                case WardenMove.ArcThrust:
                case WardenMove.ColossalCleave:
                    return grounded && distance <= MeleeStrikeRange(move) + MeleeApproachRange;
                case WardenMove.DragonRush:
                    return grounded && clearLine && distance >= RushMinRange && distance <= RushMaxRange;
                case WardenMove.ReapingVault:
                    return grounded && clearLine && distance >= VaultMinRange && distance <= VaultMaxRange && VaultPathClear(TargetPlayer);
                case WardenMove.LeapingPlunge:
                {
                    if (!clearLine || distance < 160f || distance > plungeMax)
                    {
                        return false;
                    }

                    // Same rise-hang-dive path the move flies: a ceiling or wall in the way would stop it short.
                    Vector2 targetCenter = TargetPlayer.Center;
                    Vector2 apex = PlungeApex(targetCenter);
                    return BodyPathClear(NPC.Center, apex, targetCenter - new Vector2(0f, 24f));
                }
                case WardenMove.ThunderJavelin:
                    return clearLine && distance >= 320f && distance <= 1100f;
                case WardenMove.ThunderLance:
                    return clearLine && distance >= 300f && distance <= 1100f;
                case WardenMove.StormPillar:
                    return _pillarCooldown <= 0 && distance <= 700f;
                default:
                    return false;
            }
        }

        // ── Tandem entry points used by Smough and the intro ─────────────────────
        public void QueueTandemPincer(int delayTicks)
        {
            QueueMove(WardenMove.PincerDash, delayTicks);
        }

        public void QueueTandemPlunge(int delayTicks)
        {
            QueueMove(WardenMove.LeapingPlunge, delayTicks);
        }

        /// <summary>Intro: the real Lunging Skewer, tell included, driven through a fixed world point.</summary>
        public void StartScriptedSkewer(Vector2 throughPoint)
        {
            BeginCustomMove(WardenMove.LungingSkewer);
            MoveFlag = true; // scripted: the point stays locked, no target tracking
            MovePoint = throughPoint;
            FaceX(throughPoint.X);
            MoveDirection = NPC.direction;
            NPC.netUpdate = true;
        }

        // ── Custom moves ─────────────────────────────────────────────────────────
        private const int SkewerTellTicks = 32;
        private const int SkewerAimLockTick = 26;
        private const int SkewerAccelTicks = 6;
        private const float SkewerDecelPixels = 110f;
        private const int SkewerMaxDashTicks = 70;
        private const int SkewerRecoveryTicks = 52;
        private const int ColossusPauseTicks = 20;
        private const int ColossusRetellTicks = 18;
        private const int ColossusRecoveryTicks = 60;

        /*
         * Dragon Rush: the long-range couched charge. Spear level and tip forward the whole way, like the Skewer.
         * Tell 46t (aim tracks to t38, gold aim line the full distance, tip charge, thunder at t1).
         * Dash: 6t squared ramp to 26 px/t (28 as Giant), overshoots 220px past the locked point, 110px slide-out,
         *   live the whole dash. 90 tiles + 220px = 1660px needs ~67t at 26 px/t, so the 84t cap only stops a blocked dash.
         * Counter: roll through it (the spear passes in ~3t at that speed) or jump it. Recovery 64t, the longest punish he has.
         * Selection: grounded, clear line, 30-90 tiles; two cards in each phase's deck.
         */
        private const float RushMinRange = 30f * 16f;
        private const float RushMaxRange = 90f * 16f;
        private const int RushTellTicks = 46;
        private const int RushAimLockTick = 38;
        private const float RushPeakSpeed = 26f;
        private const float GiantRushPeakSpeed = 28f;
        private const float RushOvershoot = 220f;
        private const int RushMaxDashTicks = 84;
        private const int RushRecoveryTicks = 64;
        private float _dashRemaining;
        private bool _secondDashDone;

        protected override bool CustomAttackCommitted
        {
            get
            {
                if (base.CustomAttackCommitted)
                {
                    return true;
                }

                switch (CurrentMove)
                {
                    case WardenMove.LungingSkewer:
                    case WardenMove.PincerDash:
                    case WardenMove.ColossusSkewer:
                    case WardenMove.DragonRush:
                        return MoveStage != SkewerRecover;
                    case WardenMove.LeapingPlunge:
                        return MoveStage != PlungeRecover;
                    case WardenMove.ReapingSweep:
                    case WardenMove.ReapingVault:
                        return MoveStage != ReapRecover;
                    case WardenMove.ThunderJavelin:
                    case WardenMove.ThunderLance:
                    case WardenMove.Constellation:
                        return MoveStage == 0;
                    case WardenMove.StormPillar:
                        return MoveStage == 0 && MoveTicks >= PillarCommitTick;
                    default:
                        return false;
                }
            }
        }

        protected override void TickBossMove(Player target, bool server)
        {
            switch (CurrentMove)
            {
                case WardenMove.LungingSkewer:
                case WardenMove.PincerDash:
                case WardenMove.ColossusSkewer:
                case WardenMove.DragonRush:
                    TickSkewer(target, server);
                    break;
                case WardenMove.LeapingPlunge:
                    TickPlunge(target, server);
                    break;
                case WardenMove.ReapingSweep:
                case WardenMove.ReapingVault:
                    TickReap(target, server);
                    break;
                case WardenMove.ThunderJavelin:
                case WardenMove.ThunderLance:
                    TickThrownBolt(target, server);
                    break;
                case WardenMove.StormPillar:
                    TickStormPillar(server);
                    break;
                case WardenMove.Constellation:
                    TickConstellation(server);
                    break;
                default:
                    if (server)
                    {
                        EndMove();
                    }
                    break;
            }
        }

        private const byte SkewerTell = 0;
        private const byte SkewerDash = 1;
        private const byte SkewerRecover = 2;
        private const byte SkewerPause = 3;
        private const byte SkewerRetell = 4;

        private void TickSkewer(Player target, bool server)
        {
            Vector2 forward = new Vector2(MoveDirection, 0f);
            float couchRotation = BladeRotationToward(forward);

            switch (MoveStage)
            {
                case SkewerTell:
                case SkewerRetell:
                {
                    NPC.velocity.X *= 0.8f;
                    bool rush = CurrentMove == WardenMove.DragonRush;
                    int tellTicks = SkewerTellTicks;
                    int lockTick = SkewerAimLockTick;
                    if (MoveStage == SkewerRetell)
                    {
                        tellTicks = ColossusRetellTicks;
                        lockTick = ColossusRetellTicks - 6;
                    }
                    else if (rush)
                    {
                        tellTicks = RushTellTicks;
                        lockTick = RushAimLockTick;
                    }

                    if (MoveTicks == 1 && MoveStage == SkewerTell)
                    {
                        _secondDashDone = false;
                        SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.6f, Pitch = -0.2f }, NPC.Center);

                        if (rush)
                        {
                            SoundEngine.PlaySound(SoundID.Thunder with { Volume = 0.5f, Pitch = 0.3f }, NPC.Center);
                        }
                    }

                    bool scripted = MoveFlag;
                    if (!scripted && MoveTicks <= lockTick)
                    {
                        MovePoint = target.Center;
                        FaceX(MovePoint.X);
                        MoveDirection = NPC.direction;
                    }

                    CustomRotation = CustomRotation.AngleLerp(couchRotation, 0.25f);
                    SpawnTipCharge(MoveTicks / (float)tellTicks);
                    if (MoveTicks >= 10 && MoveTicks <= lockTick)
                    {
                        SpawnAimLine(NPC.Center, MovePoint);
                    }

                    if (server && MoveTicks >= tellTicks && !HoldCustomTellForLock() && CurrentMove != WardenMove.LockCancel)
                    {
                        int damage = SkewerExpert;
                        if (CurrentMove == WardenMove.ColossusSkewer)
                        {
                            damage = ColossusSkewerExpert;
                        }
                        else if (rush)
                        {
                            damage = RushExpert;
                        }

                        ArmBlade(SpearReach * 1.1f, damage);
                        SetStage(SkewerDash);
                    }
                    break;
                }

                case SkewerDash:
                    TickSkewerDash(target, server, couchRotation);
                    break;

                case SkewerPause:
                    NPC.velocity.X *= 0.82f;
                    CustomRotation = CustomRotation.AngleLerp(-0.3f, 0.15f);
                    if (server && MoveTicks >= ColossusPauseTicks)
                    {
                        MovePoint = target.Center;
                        FaceX(MovePoint.X);
                        MoveDirection = NPC.direction;
                        SetStage(SkewerRetell);
                    }
                    break;

                case SkewerRecover:
                {
                    NPC.velocity.X *= 0.85f;
                    CustomRotation = CustomRotation.AngleLerp(-0.3f, 0.1f);
                    int recoveryTicks = SkewerRecoveryTicks;
                    if (CurrentMove == WardenMove.ColossusSkewer)
                    {
                        recoveryTicks = ColossusRecoveryTicks;
                    }
                    else if (CurrentMove == WardenMove.DragonRush)
                    {
                        recoveryTicks = RushRecoveryTicks;
                    }

                    if (server && MoveTicks >= recoveryTicks)
                    {
                        EndMove();
                    }
                    break;
                }
            }
        }

        private void TickSkewerDash(Player target, bool server, float couchRotation)
        {
            float peakSpeed = Empowered ? 24f : 22f;
            float overshoot = Empowered ? 140f : 120f;
            int maxDashTicks = SkewerMaxDashTicks;
            if (CurrentMove == WardenMove.PincerDash)
            {
                overshoot = 260f;
            }
            else if (CurrentMove == WardenMove.DragonRush)
            {
                peakSpeed = Empowered ? GiantRushPeakSpeed : RushPeakSpeed;
                overshoot = RushOvershoot;
                maxDashTicks = RushMaxDashTicks;
            }

            if (MoveTicks == 1)
            {
                _dashRemaining = Math.Abs(MovePoint.X - NPC.Center.X) + overshoot;
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.65f, Pitch = 0.2f }, NPC.Center);
            }

            // Squared ramp out of the tell, full speed, then a linear slide over the last 110px.
            float ramp = Math.Min(1f, MoveTicks / (float)SkewerAccelTicks);
            float speed = peakSpeed * ramp * ramp;
            if (_dashRemaining < SkewerDecelPixels)
            {
                speed = Math.Min(speed, Math.Max(2f, peakSpeed * _dashRemaining / SkewerDecelPixels));
            }

            NPC.velocity.X = MoveDirection * speed;
            StepUpLedges();
            _dashRemaining -= speed;
            CustomRotation = couchRotation;
            MarkLive();
            TickBladeHit();
            SpawnDashTrail();

            bool blocked = MoveTicks > 3 && NPC.collideX;
            bool finished = _dashRemaining <= 0f || blocked || MoveTicks >= maxDashTicks;
            if (!server || !finished)
            {
                return;
            }

            StopMeleeHit();

            bool wantsSecondDash = CurrentMove == WardenMove.ColossusSkewer && !_secondDashDone
                && (MoveBladeHit || NPC.Distance(target.Center) <= 400f);
            if (wantsSecondDash)
            {
                _secondDashDone = true;
                SetStage(SkewerPause);
                return;
            }

            SetStage(SkewerRecover);
        }

        private void StepUpLedges()
        {
            float stepSpeed = 1f;
            float gfxOffset = NPC.gfxOffY;
            Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref stepSpeed, ref gfxOffset);
            NPC.gfxOffY = gfxOffset;
        }

        private void SpawnTipCharge(float progress)
        {
            if (Main.dedServ)
            {
                return;
            }

            Vector2 tip = WeaponPoint(SpearReach);
            float ringRadius = MathHelper.Lerp(44f, 14f, MathHelper.Clamp(progress, 0f, 1f));
            Vector2 from = tip + Main.rand.NextVector2CircularEdge(ringRadius, ringRadius);
            Dust spark = Dust.NewDustPerfect(from, DustID.Electric, (tip - from) * 0.14f, 70, default, 0.9f);
            spark.noGravity = true;
            Lighting.AddLight(tip, 0.5f, 0.45f, 0.2f);
        }

        private void SpawnAimLine(Vector2 from, Vector2 to)
        {
            if (Main.dedServ || MoveTicks % 4 != 0)
            {
                return;
            }

            // One mote per ~48px, never fewer than 8, so a 90-tile rush line reads as solidly as a short one.
            int motes = Math.Max(8, (int)(Vector2.Distance(from, to) / 48f));
            for (int i = 1; i <= motes; i++)
            {
                Vector2 point = Vector2.Lerp(from, to, i / (float)motes);
                Dust line = Dust.NewDustPerfect(point, DustID.GoldFlame, Vector2.Zero, 100, Color.White, 0.8f);
                line.noGravity = true;
            }
        }

        private void SpawnDashTrail()
        {
            if (Main.dedServ)
            {
                return;
            }

            Lighting.AddLight(NPC.Center, 0.8f, 0.7f, 0.3f);
            Dust trail = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.GoldFlame, -NPC.velocity.X * 0.3f, 0f, 100, default, 1.3f);
            trail.noGravity = true;
            Dust streak = Dust.NewDustPerfect(WeaponPoint(SpearReach), DustID.Electric, -NPC.velocity * 0.15f, 60, default, 0.9f);
            streak.noGravity = true;
        }

        // ── Leaping Plunge ───────────────────────────────────────────────────────
        private const byte PlungeTell = 0;
        private const byte PlungeRise = 1;
        private const byte PlungeHang = 2;
        private const byte PlungeDive = 3;
        private const byte PlungeRecover = 4;
        private const int PlungeTellTicks = 24;
        private const int PlungeRiseTicks = 30;
        private const int PlungeHangTicks = 12;
        private const int PlungeAimLockTick = 6;
        private const float PlungeApexAboveTarget = 9f * 16f;
        private const float PlungeDiveSpeed = 18f;
        private const int PlungeMaxDiveTicks = 90;
        private const int PlungeRecoveryTicks = 48;

        private float FindCeilingCap(Vector2 start)
        {
            int tileX = (int)(start.X / 16f);
            int startTileY = (int)(start.Y / 16f);

            for (int tileY = startTileY; tileY > startTileY - 40 && tileY > 5; tileY--)
            {
                if (WorldGen.SolidTile(tileX, tileY))
                {
                    return (tileY + 4) * 16f;
                }
            }

            return start.Y - 40f * 16f;
        }

        /*
         * Reaping Sweep: one spear sweep from low in front, down under the body, to low behind (0.2 -> 3.7 rad, 200 deg),
         * so it covers both sides of him. Two ways in:
         *   Grounded (reactive): fires after the player crosses to his other side within 60t and 120px; it catches a
         *     second roll behind him. Tell 30t: a short hop back while the spear lifts high (-1.3), then it drops to the
         *     start in the last 10t. Strike 40t (Snap ease), live the whole sweep. Recovery 26t: 8t held, then the spear
         *     turns back over the top (the short way, ~131 deg) to the carry.
         *   Vault (deck card, 7-20 tiles): the same tell without the hop, aim locks at t24, then a 36t low arc (apex 96px)
         *     that lands 150px past the player. The sweep runs from 20% to 80% of the flight, so the blade passes through
         *     the player as he goes over. Live from 15% to 85% of the flight. Lands into a 36t recovery.
         * Counter: roll through the sweep (the blade crosses a standing player in ~12t), or for the vault, run under
         * the jump early. Neither has projectiles.
         */
        private const byte ReapTell = 0;
        private const byte ReapStrike = 1;
        private const byte ReapRecover = 2;
        private const int ReapTellTicks = 30;
        private const int ReapDropTicks = 10;
        private const int ReapStrikeTicks = 40;
        private const int ReapHoldTicks = 8;
        private const int ReapRecoveryTicks = 26;
        private const int VaultRecoveryTicks = 36;
        private const int VaultAimLockTick = 24;
        private const int VaultAirTicks = 36;
        private const float VaultApexLift = 96f;
        private const float VaultOvershoot = 150f;
        private const float VaultMinRange = 7f * 16f;
        private const float VaultMaxRange = 20f * 16f;
        private const float VaultSweepStart = 0.2f;
        private const float VaultSweepEnd = 0.8f;
        private const float VaultLiveStart = 0.15f;
        private const float VaultLiveEnd = 0.85f;
        private const float ReapStartRotation = 0.2f;
        private const float ReapEndRotation = 3.7f;
        private const float ReapCockedRotation = -1.3f;
        private const float ReapHopBackSpeed = 2.5f;
        private const int ReapHopTicks = 10;

        /// <summary>Where the vault lands: VaultOvershoot past the player on the ground under that point.</summary>
        private Vector2 VaultLanding(Player target, int direction)
        {
            float landingX = target.Center.X + direction * VaultOvershoot;
            float landingY = target.Bottom.Y;
            if (PuppetGroundDustWave.TryFindGroundY(landingX, target.Bottom.Y, out float groundY))
            {
                landingY = groundY;
            }

            return new Vector2(landingX, landingY);
        }

        /// <summary>Samples the vault's arc (the same chord and lift TickReap flies) so a low ceiling or a wall never picks it.</summary>
        private bool VaultPathClear(Player target)
        {
            int direction = Math.Sign(target.Center.X - NPC.Center.X);
            if (direction == 0)
            {
                direction = NPC.direction;
            }

            Vector2 startBottom = NPC.Bottom;
            Vector2 landingBottom = VaultLanding(target, direction);
            Vector2 halfHeight = new Vector2(0f, NPC.height * 0.5f + 2f);

            const int Samples = 8;
            Vector2[] centers = new Vector2[Samples + 1];
            for (int i = 0; i <= Samples; i++)
            {
                float progress = i / (float)Samples;
                Vector2 chord = Vector2.Lerp(startBottom, landingBottom, progress);
                float arc = 4f * VaultApexLift * progress * (1f - progress);
                centers[i] = chord - new Vector2(0f, arc) - halfHeight;
            }

            return BodyPathClear(centers);
        }

        private void TickReap(Player target, bool server)
        {
            bool vault = CurrentMove == WardenMove.ReapingVault;

            switch (MoveStage)
            {
                case ReapTell:
                {
                    bool aimOpen = !vault || MoveTicks <= VaultAimLockTick;
                    if (aimOpen)
                    {
                        FaceX(target.Center.X);
                        MoveDirection = NPC.direction;
                    }

                    // Grounded: a short hop back as the spear lifts, which reads as the coil. Vault: plant and coil.
                    bool hopping = !vault && MoveTicks <= ReapHopTicks;
                    if (hopping)
                    {
                        NPC.velocity.X = -MoveDirection * ReapHopBackSpeed;
                    }
                    else
                    {
                        NPC.velocity.X *= 0.8f;
                    }

                    // Lift high, then drop to the sweep's start in the last ReapDropTicks.
                    int dropStart = ReapTellTicks - ReapDropTicks;
                    if (MoveTicks < dropStart)
                    {
                        CustomRotation = CustomRotation.AngleLerp(ReapCockedRotation, 0.18f);
                    }
                    else
                    {
                        CustomRotation = CustomRotation.AngleLerp(ReapStartRotation, 0.35f);
                    }

                    SpawnTipCharge(MoveTicks / (float)ReapTellTicks);

                    if (MoveTicks == 1)
                    {
                        SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.55f, Pitch = 0.1f }, NPC.Center);
                    }

                    if (server && MoveTicks >= ReapTellTicks && !HoldCustomTellForLock() && CurrentMove != WardenMove.LockCancel)
                    {
                        MoveStart = NPC.Bottom;
                        if (vault)
                        {
                            MovePoint = VaultLanding(target, MoveDirection);
                        }

                        int damage = Empowered ? GiantLightExpert : LightExpert;
                        ArmBlade(SpearReach, damage);
                        SetStage(ReapStrike);
                    }

                    break;
                }

                case ReapStrike:
                {
                    int strikeTicks = vault ? VaultAirTicks : ReapStrikeTicks;
                    float progress = MathHelper.Clamp(MoveTicks / (float)strikeTicks, 0f, 1f);
                    float sweepT = progress;
                    bool live = true;

                    if (vault)
                    {
                        // Kinematic arc like Smough's Bound: chord from take-off to landing, lifted by a parabola.
                        NPC.noGravity = true;
                        Vector2 chord = Vector2.Lerp(MoveStart, MovePoint, progress);
                        float arc = 4f * VaultApexLift * progress * (1f - progress);
                        Vector2 desiredBottom = chord - new Vector2(0f, arc);
                        NPC.velocity = desiredBottom - NPC.Bottom;
                        SpawnDashTrail();

                        sweepT = MathHelper.Clamp((progress - VaultSweepStart) / (VaultSweepEnd - VaultSweepStart), 0f, 1f);
                        live = progress >= VaultLiveStart && progress <= VaultLiveEnd;
                    }
                    else
                    {
                        NPC.velocity.X *= 0.85f;
                    }

                    CustomRotation = SwingEase.Apply(ReapStartRotation, ReapEndRotation, sweepT, SwingEaseStyle.Snap);

                    if (MoveTicks == 1 && server)
                    {
                        // The crescent lasts as long as the sweep itself: the whole strike grounded, 60% of the flight in a vault.
                        int arcTicks = strikeTicks;
                        if (vault)
                        {
                            arcTicks = (int)(strikeTicks * (VaultSweepEnd - VaultSweepStart));
                        }

                        float sweep = (ReapEndRotation - ReapStartRotation) * MoveDirection;
                        VanillaSwordArcSettings settings = SwingArcSettings(sweep, SpearReach, arcTicks);
                        settings.Easing = VanillaSwordArcEasing.EaseOut;
                        VanillaSwordArc.SpawnForNPC(NPC.GetSource_FromAI(), NPC, 0, 0f, Main.myPlayer, settings, Vector2.Zero, hostile: false);
                    }

                    if (MoveTicks == 1 && !Main.dedServ)
                    {
                        SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, Pitch = -0.3f }, NPC.Center);
                    }

                    if (live)
                    {
                        MarkLive();
                        TickBladeHit();
                    }
                    else
                    {
                        StopMeleeHit();
                    }

                    bool landedEarly = vault && MoveTicks > VaultAirTicks / 2 && NPC.collideY;
                    if (server && (MoveTicks >= strikeTicks || landedEarly))
                    {
                        NPC.noGravity = false;
                        StopMeleeHit();
                        if (vault)
                        {
                            NPC.velocity = Vector2.Zero;
                            PlayVaultLanding();
                        }

                        SetStage(ReapRecover);
                    }

                    break;
                }

                case ReapRecover:
                {
                    NPC.noGravity = false;
                    NPC.velocity.X *= 0.8f;

                    // Hold the finished pose, then AngleLerp back to the carry, which takes the short way over the top
                    // instead of unwinding the whole sweep back under the body.
                    if (MoveTicks > ReapHoldTicks)
                    {
                        CustomRotation = CustomRotation.AngleLerp(-0.3f, 0.12f);
                    }

                    int recoveryTicks = vault ? VaultRecoveryTicks : ReapRecoveryTicks;
                    if (server && MoveTicks >= recoveryTicks)
                    {
                        EndMove();
                    }

                    break;
                }
            }
        }

        private void PlayVaultLanding()
        {
            if (Main.dedServ)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.Item70 with { Volume = 0.6f, Pitch = 0.2f }, NPC.Bottom);
            for (int i = 0; i < 14; i++)
            {
                Vector2 velocity = new Vector2(Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-2.5f, -0.5f));
                Dust spark = Dust.NewDustPerfect(NPC.Bottom + new Vector2(Main.rand.NextFloat(-16f, 16f), -2f), DustID.GoldFlame, velocity, 80, default, 1.1f);
                spark.noGravity = true;
            }
        }

        /// <summary>The hang point three quarters of the way over, 9 tiles above the target, kept under any ceiling.</summary>
        private Vector2 PlungeApex(Vector2 targetCenter)
        {
            float apexX = MathHelper.Lerp(NPC.Center.X, targetCenter.X, 0.75f);
            float apexY = Math.Min(NPC.Center.Y - 80f, targetCenter.Y - PlungeApexAboveTarget);
            apexY = Math.Max(apexY, FindCeilingCap(new Vector2(apexX, NPC.Center.Y)));
            return new Vector2(apexX, apexY);
        }

        private void TickPlunge(Player target, bool server)
        {
            switch (MoveStage)
            {
                case PlungeTell:
                    NPC.velocity.X *= 0.8f;
                    FaceX(target.Center.X);
                    CustomRotation = CustomRotation.AngleLerp(BladeRotationToward(-Vector2.UnitY), 0.2f);
                    SpawnTipCharge(MoveTicks / (float)PlungeTellTicks);

                    if (server && MoveTicks >= PlungeTellTicks)
                    {
                        MoveStart = NPC.Center;
                        MovePoint = PlungeApex(target.Center);
                        SetStage(PlungeRise);
                    }
                    break;

                case PlungeRise:
                {
                    NPC.noGravity = true;
                    float progress = MathHelper.Clamp(MoveTicks / (float)PlungeRiseTicks, 0f, 1f);
                    float eased = 1f - (1f - progress) * (1f - progress);
                    Vector2 desired = Vector2.Lerp(MoveStart, MovePoint, eased);
                    NPC.velocity = desired - NPC.Center;
                    SpawnDashTrail();

                    if (server && MoveTicks >= PlungeRiseTicks)
                    {
                        SetStage(PlungeHang);
                    }
                    break;
                }

                case PlungeHang:
                    NPC.noGravity = true;
                    NPC.velocity = Vector2.Zero;
                    FaceX(target.Center.X);

                    if (MoveTicks <= PlungeAimLockTick)
                    {
                        MoveAim = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitY);
                        MoveAim = new Vector2(MoveAim.X, Math.Max(MoveAim.Y, 0.25f)).SafeNormalize(Vector2.UnitY);
                    }

                    CustomRotation = CustomRotation.AngleLerp(BladeRotationToward(MoveAim), 0.35f);
                    SpawnAimLine(NPC.Center, NPC.Center + MoveAim * 320f);

                    if (server && MoveTicks >= PlungeHangTicks && !HoldCustomTellForLock() && CurrentMove != WardenMove.LockCancel)
                    {
                        ArmBlade(SpearReach * 1.1f, PlungeExpert);
                        SetStage(PlungeDive);
                    }
                    break;

                case PlungeDive:
                {
                    NPC.noGravity = true;
                    NPC.velocity = MoveAim * PlungeDiveSpeed;
                    CustomRotation = BladeRotationToward(MoveAim);
                    MarkLive();
                    TickBladeHit();
                    SpawnDashTrail();

                    bool landed = MoveTicks > 2 && (NPC.collideY || NPC.collideX);
                    bool expired = MoveTicks >= PlungeMaxDiveTicks;
                    if (server && (landed || expired))
                    {
                        LandPlunge();
                    }
                    break;
                }

                case PlungeRecover:
                    NPC.noGravity = false;
                    NPC.velocity.X *= 0.82f;
                    CustomRotation = CustomRotation.AngleLerp(-0.3f, 0.1f);
                    if (server && MoveTicks >= PlungeRecoveryTicks)
                    {
                        EndMove();
                    }
                    break;
            }
        }

        private void LandPlunge()
        {
            StopMeleeHit();
            NPC.noGravity = false;
            NPC.velocity = Vector2.Zero;

            float sparkSpeed = 7f;
            int sparkTiles = 18;
            int sparkDamage = SparkExpert;
            if (Empowered)
            {
                sparkSpeed = 9f;
                sparkTiles = 24;
                sparkDamage = GiantSparkExpert;
            }

            WardenLightning.SpawnCrawlerPair(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Bottom.Y - 8f, sparkSpeed, sparkTiles, 10, sparkDamage);
            WardenLightning.Spawn(NPC.GetSource_FromThis(), NPC.Bottom, Vector2.Zero, WardenBoltMode.Burst, 0, 0f, 5f);
            SetStage(PlungeRecover);
        }

        // ── Thunder Javelin / Thunder Lance ──────────────────────────────────────
        private void TickThrownBolt(Player target, bool server)
        {
            bool lance = CurrentMove == WardenMove.ThunderLance;
            int tellTicks = lance ? 36 : 30;
            int lockTick = lance ? 30 : 24;
            int recoveryTicks = lance ? 30 : 24;
            float speed = 16f;
            if (lance || Empowered)
            {
                speed = 18f;
            }

            NPC.velocity.X *= 0.8f;

            if (MoveStage == 1)
            {
                CustomRotation = CustomRotation.AngleLerp(-0.3f, 0.1f);
                if (server && MoveTicks >= recoveryTicks)
                {
                    EndMove();
                }

                return;
            }

            if (MoveTicks <= lockTick)
            {
                MoveAim = (target.Center - NPC.Center).SafeNormalize(new Vector2(NPC.direction, 0f));
                FaceX(target.Center.X);
            }

            CustomRotation = CustomRotation.AngleLerp(BladeRotationToward(MoveAim), 0.25f);
            SpawnTipCharge(MoveTicks / (float)tellTicks);

            if (MoveTicks >= 10)
            {
                SpawnAimLine(WeaponPoint(SpearReach), WeaponPoint(SpearReach) + MoveAim * 360f);
            }

            bool ready = server && MoveTicks >= tellTicks;
            if (!ready)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.7f, Pitch = 0.1f }, NPC.Center);
            Vector2 velocity = MoveAim * speed;
            Vector2 tip = WeaponPoint(SpearReach);
            if (lance)
            {
                WardenLightning.Spawn(NPC.GetSource_FromThis(), tip, velocity, WardenBoltMode.Lance, LanceExpert);
            }
            else
            {
                float lifeTicks = 1100f / speed;
                WardenLightning.Spawn(NPC.GetSource_FromThis(), tip, velocity, WardenBoltMode.Bolt, JavelinExpert, lifeTicks);
            }

            SetStage(1);
        }

        // ── Storm Pillar: the staggerable channel ────────────────────────────────
        private const int PillarChannelTicks = 100;
        private const int PillarCommitTick = 66;
        private const int PillarMarksTick = 40;
        private const int PillarStrikeTick = 120;
        private const int PillarRecoveryTicks = 60;
        private const int PillarCooldownTicks = 900;
        private static readonly int[] PillarMarkTiles = { 5, 11, 17 };

        private void TickStormPillar(bool server)
        {
            NPC.velocity.X *= 0.75f;

            if (MoveStage == 1)
            {
                CustomRotation = CustomRotation.AngleLerp(-0.3f, 0.06f);
                if (server && MoveTicks >= PillarRecoveryTicks)
                {
                    EndMove();
                }

                return;
            }

            if (MoveTicks == 1)
            {
                float plantX = NPC.Center.X + NPC.direction * 50f;
                PuppetGroundDustWave.TryFindGroundY(plantX, NPC.Bottom.Y - 8f, out float groundY);
                MovePoint = new Vector2(plantX, groundY);
                _pillarCooldown = PillarCooldownTicks;
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.5f, Pitch = -0.4f }, NPC.Center);
            }

            CustomRotation = CustomRotation.AngleLerp(BladeRotationToward(new Vector2(NPC.direction * 0.17f, 1f)), 0.2f);
            SpawnPillarChannelDust();

            if (MoveTicks == PillarCommitTick && !Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item29 with { Pitch = 0.5f }, NPC.Center);
                for (int i = 0; i < 24; i++)
                {
                    Dust flash = Dust.NewDustPerfect(NPC.Center, DustID.GoldFlame, Main.rand.NextVector2CircularEdge(5f, 5f), 0, default, 1.5f);
                    flash.noGravity = true;
                }
            }

            if (!server)
            {
                return;
            }

            IEntitySource source = NPC.GetSource_FromThis();
            if (MoveTicks == PillarMarksTick)
            {
                int markTicks = PillarStrikeTick - PillarMarksTick;
                foreach (int tiles in PillarMarkTiles)
                {
                    WardenLightning.SpawnSkyStrike(source, MovePoint.X - tiles * 16f, MovePoint.Y - 8f, markTicks, 2, PillarStrikeExpert);
                    WardenLightning.SpawnSkyStrike(source, MovePoint.X + tiles * 16f, MovePoint.Y - 8f, markTicks, 2, PillarStrikeExpert);
                }
            }

            if (MoveTicks >= PillarChannelTicks)
            {
                float encodedStartAndSpeed = 10f; // start radius 0, 10 px/t
                WardenLightning.Spawn(source, MovePoint, Vector2.Zero, WardenBoltMode.Annulus, PillarRingExpert, 480f, encodedStartAndSpeed);
                WardenLightning.Spawn(source, MovePoint, Vector2.Zero, WardenBoltMode.Burst, 0, 0f, 8f);
                SetStage(1);
            }
        }

        private void SpawnPillarChannelDust()
        {
            if (Main.dedServ)
            {
                return;
            }

            int perTick = 1 + 3 * MoveTicks / PillarChannelTicks;
            for (int i = 0; i < perTick; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 from = MovePoint + angle.ToRotationVector2() * 120f;
                Vector2 inward = (MovePoint - from) * 0.07f;
                Dust spiral = Dust.NewDustPerfect(from, DustID.Electric, inward.RotatedBy(0.6f), 60, default, 1f);
                spiral.noGravity = true;
            }
        }

        // ── Constellation (anti-air) ─────────────────────────────────────────────
        private const int ConstellationRecoveryTicks = 40;
        private const int MaxConstellations = 2;
        private List<int> _constellationTargets;

        protected override void StartAntiAir(List<int> highPlayers)
        {
            _constellationTargets = highPlayers;
            BeginCustomMove(WardenMove.Constellation);
        }

        private int ConstellationStarCount => Empowered ? 20 : 16;

        private void TickConstellation(bool server)
        {
            NPC.velocity.X *= 0.75f;
            int waves = ConstellationStarCount / 4;
            int tellTicks = WardenLightning.ConstellationLinesEnd + WardenLightning.ConstellationWaveGap * waves;

            if (MoveStage == 1)
            {
                CustomRotation = CustomRotation.AngleLerp(-0.3f, 0.1f);
                if (server && MoveTicks >= ConstellationRecoveryTicks)
                {
                    EndMove();
                }

                return;
            }

            CustomRotation = CustomRotation.AngleLerp(BladeRotationToward(-Vector2.UnitY), 0.2f);
            SpawnTipCharge(0.5f);

            if (MoveTicks == 1)
            {
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.7f, Pitch = -0.3f }, NPC.Center);
                if (server && _constellationTargets != null)
                {
                    SpawnConstellations();
                }
            }

            if (server && MoveTicks >= tellTicks)
            {
                SetStage(1);
            }
        }

        private void SpawnConstellations()
        {
            int damage = Empowered ? GiantConstellationExpert : ConstellationExpert;
            int spawned = 0;

            foreach (int playerIndex in _constellationTargets)
            {
                if (spawned >= MaxConstellations)
                {
                    break;
                }

                Player player = Main.player[playerIndex];
                float encodedStarsAndBoss = ConstellationStarCount * 1000f + NPC.whoAmI;
                WardenLightning.Spawn(NPC.GetSource_FromThis(), player.Center, Vector2.Zero, WardenBoltMode.Constellation, damage, playerIndex, encodedStarsAndBoss);
                spawned++;
            }
        }

        // ── Presentation ─────────────────────────────────────────────────────────
        protected override void TickBossPresentation()
        {
            if (Main.dedServ || !Empowered || Main.GameUpdateCount % 4 != 0)
            {
                return;
            }

            Dust plume = Dust.NewDustPerfect(NPC.Top + Main.rand.NextVector2Circular(10f, 6f) - new Vector2(0f, 14f), DustID.GoldFlame, new Vector2(-NPC.direction * 0.8f, -0.6f), 60, default, 1f);
            plume.noGravity = true;
            Dust spark = Dust.NewDustPerfect(WeaponPoint(SpearReach), DustID.Electric, Main.rand.NextVector2Circular(1f, 1f), 60, default, 0.8f);
            spark.noGravity = true;
        }

        // ── Stats ────────────────────────────────────────────────────────────────
        public override void SetStaticDefaults()
        {
            ApplySharedStaticDefaults();
        }

        public override void SetDefaults()
        {
            ApplySharedDefaults();
            NPC.lifeMax = AuthoredLife;
            NPC.defense = 40;
        }
    }
}
