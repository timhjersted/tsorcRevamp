using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Armor;
using tsorcRevamp.Content.Items.Weapons.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using tsorcRevamp.NPCs.Invaders;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses.Ornsmough
{
    /// <summary>
    /// Smough: slow great hammer. Overhead Execution, Wide and Back Sweep, Executioner's Bound, Spiked Charge and Rubble Heave,
    /// with Grinding Spin answering repeated roll-throughs. As Super Smough (Ornstein fell first) every hammer impact throws
    /// floor lightning and he adds Thunder Execution, Storm Bound and Charged Sweep. His anti-air turns the next slam into a quake.
    /// </summary>
    [AutoloadBossHead]
    public class Smough : WardenPuppet
    {
        protected override string InvaderTitle => "Executioner Smough";
        protected override string DespawnFlavorText => LangUtils.GetTextValue("NPCs.Smough.DespawnHandler");

        // ── Loadout ──────────────────────────────────────────────────────────────
        protected override int HeadArmorItemType => ModContent.ItemType<SmoughHelmet>();
        protected override int BodyArmorItemType => ModContent.ItemType<SmoughArmor>();
        protected override int LegsArmorItemType => ModContent.ItemType<SmoughGreaves>();
        protected override int MeleeWeaponItemType => ModContent.ItemType<SmoughGreatHammer>();

        // ── Damage (Expert, before defense) ──────────────────────────────────────
        public const int LightExpert = 200;
        public const int SuperLightExpert = 230;
        public const int BoundExpert = 240;
        public const int ChargeExpert = 220;
        public const int WaveExpert = 120;
        public const int CrawlerExpert = 130;
        public const int RockExpert = 140;
        public const int StrikeExpert = 160;
        public const int RingExpert = 150;
        public const int CrescentExpert = 150;

        protected override int MeleeDamage
        {
            get
            {
                if (Empowered)
                {
                    return ResolveMeleeDamage(SuperLightExpert);
                }

                return ResolveMeleeDamage(LightExpert);
            }
        }

        // ── Life (Master = authored x 1.275: 60k and 80k removed per kill) ───────
        public const int AuthoredLife = 47059;
        public const int SuperAuthoredLife = 62745;
        protected override int PairedAuthoredLife => AuthoredLife;
        public override int EmpoweredAuthoredLife => SuperAuthoredLife;
        protected override int EmpoweredDefense => 60;
        protected override float EmpoweredPoise => 260f;
        protected override float EmpoweredKnockbackResist => 0.03f;

        // ── Hammer geometry: 66x80 item sprite, butt (0,79), head centroid (42,23) ──
        public const float HammerDrawScale = 1.15f;
        private const float ReachPerBodyScale = 121f / PairedScale; // 85px grip to head at 1.35, 94px at 1.5
        private const float HammerGripFromButt = 0.22f;
        private static readonly Vector2 HammerButt = new Vector2(0f, 79f);
        private static readonly Vector2 HammerHead = new Vector2(42f, 23f);
        private static readonly Vector2 HammerSize = new Vector2(66f, 80f);

        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Hammer;
        protected override Vector2 MeleeHandleNorm => Vector2.Lerp(HammerButt, HammerHead, HammerGripFromButt) / HammerSize;
        protected override float MeleeWeaponDrawScale => HammerDrawScale;
        protected override float ComboReachBase => ReachPerBodyScale * BodyScale;
        protected override float MeleeBladeWidth => 24f;
        protected override float MeleeRange => 90f;
        protected override float StabRange => 160f;
        protected override float MeleeEngageRange => 130f;
        protected override float TwoHandedBackGripOffset => -12f;
        protected override Color MeleeTelegraphFlashColor => new Color(255, 190, 110);

        private float HammerReach => ComboReachBase * 0.7f;

        // ── Movement ─────────────────────────────────────────────────────────────
        protected override float TopSpeed => Empowered ? 1.85f : 1.6f;
        protected override float Acceleration => 0.1f;
        protected override float RunDistance => 99999f;
        protected override float ComboTelegraphAdvanceSpeedMult => 0.8f;
        protected override float ComboTelegraphAdvanceStopDistance => 60f;

        // ── Combos ───────────────────────────────────────────────────────────────
        private const string ExecutionName = "Overhead Execution";
        private const string ThunderExecutionName = "Thunder Execution";
        private const string WideSweepName = "Wide Sweep";
        private const string ChargedSweepName = "Charged Sweep";
        private const string GrindingSpinName = "Grinding Spin";

        protected override MeleeCombo[] MeleeComboPoolOverride => SmoughCombos;

        private static MeleeComboStep SpinStep()
        {
            return new MeleeComboStep
            {
                Motion = ComboMotion.Spin,
                TelegraphTicks = 30,
                AttackTicks = 40,
                DamageMult = 1f,
                ReachMult = 1.65f, // 140px radius at 1.35, 155px at 1.5 with the body scale
                ForwardPushMult = 0.8f,
                EaseOutTicks = 10,
            };
        }

        /*
         * Timing sheet (attack-timing-design §3). On-screen tell = authored (multiplier 1).
         * Every step is live for its whole swing; rolling is judged by how long the moving hammer overlaps the player.
         * Overhead Execution: tell 46 | GroundSlam -1.62 -> 2.0 (207 deg), 12/40 k6, live 52t, impact tick 12 | recovery 55
         * Thunder Execution:  the same strike with marching sky strikes inside a 75t recovery
         * Wide -> Back Sweep: tell 36 | Overhead -1.45 -> 2.2, Underhand 2.2 -> -1.45 (shared joint, 209 deg each), 10/40 k7,
         *                     live 50t, 18t pause -> second swing only on a hit or in range | recovery 30
         * Grinding Spin:      tell 30 | eased spin live 40t, deliberately not rollable in place | recovery 50
         */
        private static readonly MeleeCombo[] SmoughCombos = new[]
        {
            new MeleeCombo
            {
                Name = ExecutionName, BaseWeight = 100, Preferred = ComboRangeBand.Any, InitialFlashColor = Color.OrangeRed,
                RecoveryTicks = 55, HeavyCommit = true, HyperArmor = true, MoveBrake = 0.25f,
                Steps = new[] { WeightedStep(ComboMotion.GroundSlam, 46, 12, 40, 6f, 0, 1.3f, 1f, 70f) }
            },
            new MeleeCombo
            {
                Name = ThunderExecutionName, BaseWeight = 100, Preferred = ComboRangeBand.Any, InitialFlashColor = Color.LightCyan,
                RecoveryTicks = 75, HeavyCommit = true, HyperArmor = true, MoveBrake = 0.25f,
                Steps = new[] { WeightedStep(ComboMotion.GroundSlam, 46, 12, 40, 6f, 0, 1.3f, 1f, 70f) }
            },
            new MeleeCombo
            {
                Name = WideSweepName, BaseWeight = 100, Preferred = ComboRangeBand.Any, InitialFlashColor = Color.Gold,
                RecoveryTicks = 30, MoveBrake = 0.1f,
                Steps = new[]
                {
                    WeightedStep(ComboMotion.OverheadArc, 36, 10, 40, 7f, 18, 1f, 1.05f, 60f),
                    WeightedStep(ComboMotion.UnderhandArc, 0, 10, 40, 7f, 0, 1f, 1.05f, 40f),
                }
            },
            new MeleeCombo
            {
                Name = ChargedSweepName, BaseWeight = 100, Preferred = ComboRangeBand.Any, InitialFlashColor = Color.LightCyan,
                RecoveryTicks = 30, MoveBrake = 0.1f,
                Steps = new[]
                {
                    WeightedStep(ComboMotion.OverheadArc, 36, 10, 40, 7f, 18, 1f, 1.05f, 60f),
                    WeightedStep(ComboMotion.UnderhandArc, 0, 10, 40, 7f, 0, 1f, 1.05f, 40f),
                }
            },
            new MeleeCombo
            {
                Name = GrindingSpinName, BaseWeight = 100, Preferred = ComboRangeBand.Any, InitialFlashColor = Color.White,
                RecoveryTicks = 50, HyperArmor = true, MoveBrake = 0f,
                Steps = new[] { SpinStep() }
            },
        };

        protected override void ModifyMeleeArcEndpoints(ComboMotion motion, ref float a0, ref float a1)
        {
            switch (motion)
            {
                case ComboMotion.GroundSlam:
                    a0 = -1.62f;
                    a1 = 2.0f;
                    break;
                case ComboMotion.OverheadArc:
                    a0 = -1.45f;
                    a1 = 2.2f;
                    break;
                case ComboMotion.UnderhandArc:
                    a0 = 2.2f;
                    a1 = -1.45f;
                    break;
            }
        }

        protected override Color SwingArcBodyColor => new Color(230, 150, 60);
        protected override Color SwingArcCoreColor => new Color(255, 225, 160);
        protected override Color SwingArcDarkColor => new Color(80, 40, 12);

        protected override float MeleeStrikeRange(WardenMove move)
        {
            switch (move)
            {
                case WardenMove.OverheadExecution:
                case WardenMove.ThunderExecution:
                    return 160f;
                case WardenMove.WideSweep:
                    return 200f;
                case WardenMove.ChargedSweep:
                    return 220f;
                case WardenMove.GrindingSpin:
                    return ReactiveRange;
                default:
                    return 0f;
            }
        }

        protected override bool IsFlankerCard(WardenMove move)
        {
            return move == WardenMove.RubbleHeave || move == WardenMove.SpikedCharge;
        }

        protected override string ComboNameFor(WardenMove move)
        {
            switch (move)
            {
                case WardenMove.OverheadExecution:
                    return ExecutionName;
                case WardenMove.ThunderExecution:
                    return ThunderExecutionName;
                case WardenMove.WideSweep:
                    return WideSweepName;
                case WardenMove.ChargedSweep:
                    return ChargedSweepName;
                case WardenMove.GrindingSpin:
                    return GrindingSpinName;
                default:
                    return null;
            }
        }

        protected override bool ShouldContinueMeleeCombo(string comboName, int nextStepIndex, Player target, bool previousStepHit)
        {
            float gapInFront = (target.Center.X - NPC.Center.X) * NPC.direction;
            bool inFront = gapInFront >= 0f;
            float distance = NPC.Distance(target.Center);

            if (comboName == WideSweepName)
            {
                return previousStepHit || (inFront && distance <= 220f);
            }

            if (comboName == ChargedSweepName)
            {
                return previousStepHit || (inFront && distance <= 400f);
            }

            return true;
        }

        protected override void OnMeleeComboTelegraphTick(MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            base.OnMeleeComboTelegraphTick(combo, step, elapsed, total);

            if (combo.Name == GrindingSpinName && elapsed == 0)
            {
                SoundEngine.PlaySound(SoundID.Item71 with { Pitch = -0.4f }, NPC.Center);
            }

            if (QuakeArmed)
            {
                TickQuakeTell(elapsed);
            }

            if (Main.dedServ)
            {
                return;
            }

            if (combo.Name == ThunderExecutionName && elapsed >= 10)
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector2 head = WeaponPoint(HammerReach);
                    Vector2 from = head + Main.rand.NextVector2CircularEdge(36f, 36f);
                    Dust gather = Dust.NewDustPerfect(from, DustID.Electric, (head - from) * 0.13f, 60, default, 1f);
                    gather.noGravity = true;
                }
            }
        }

        protected override void OnMeleeComboAttackTick(MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            base.OnMeleeComboAttackTick(combo, step, elapsed, total);

            if (elapsed != step.EaseInTicks)
            {
                return;
            }

            if (step.Motion == ComboMotion.GroundSlam)
            {
                OnSlamImpact(combo.Name == ThunderExecutionName);
                return;
            }

            if (combo.Name == ChargedSweepName)
            {
                Vector2 chest = NPC.Center + new Vector2(NPC.direction * 30f, -6f);
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.45f, Pitch = 0.2f }, chest);
                WardenLightning.Spawn(NPC.GetSource_FromThis(), chest, new Vector2(NPC.direction * 12f, 0f), WardenBoltMode.Crescent, CrescentExpert, 400f);
            }
        }

        // ── Impacts ──────────────────────────────────────────────────────────────
        private const float NormalSlamShake = 8f;
        private const int MarchStepTicks = 18;
        private const int MarchMarkTicks = 30;
        private const int MarchSteps = 3;
        private const int MarchTileSpacing = 4;
        private int _marchTimer = -1;
        private Vector2 _marchOrigin;

        private void OnSlamImpact(bool thunder)
        {
            bool quake = QuakeArmed;
            Vector2 headPoint = WeaponPoint(HammerReach);

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.DeerclopsRubbleAttack, NPC.Bottom);
                if (!quake)
                {
                    UsefulFunctions.ScreenShake(NPC.Bottom, NormalSlamShake, 16, distanceFalloff: 1600f);
                }

                for (int i = 0; i < 26; i++)
                {
                    float offsetX = Main.rand.NextFloat(-96f, 96f);
                    Vector2 point = new Vector2(headPoint.X + offsetX, NPC.Bottom.Y - 4f);
                    Dust.NewDustPerfect(point, DustID.Stone, new Vector2(0f, Main.rand.NextFloat(-5f, -1f)), 0, default, 1.2f);
                }
            }

            if (!IsServerSide)
            {
                return;
            }

            ThrowImpactWaves(headPoint.X);

            if (quake)
            {
                WardenLightning.Spawn(NPC.GetSource_FromThis(), NPC.Bottom, Vector2.Zero, WardenBoltMode.QuakePulse, 0, NPC.Bottom.Y, NPC.Center.X);
                QuakeArmed = false;
                NPC.netUpdate = true;
            }

            if (thunder)
            {
                _marchOrigin = new Vector2(headPoint.X, NPC.Bottom.Y - 8f);
                _marchTimer = 0;
            }
        }

        // Phase one: short jumpable shockwaves. Super Smough: long lightning crawlers on every impact.
        private void ThrowImpactWaves(float originX)
        {
            IEntitySource source = NPC.GetSource_FromThis();
            if (Empowered)
            {
                WardenLightning.SpawnCrawlerPair(source, originX, NPC.Bottom.Y - 8f, 9f, 24, 12, CrawlerExpert);
            }
            else
            {
                WardenLightning.SpawnCrawlerPair(source, originX, NPC.Bottom.Y - 8f, 5f, 6, 8, WaveExpert);
            }
        }

        protected override void TickBossServerTimers()
        {
            if (_tandemTimer > 0)
            {
                _tandemTimer--;
            }

            if (_marchTimer < 0)
            {
                return;
            }

            bool markTick = _marchTimer % MarchStepTicks == 0;
            int stepIndex = _marchTimer / MarchStepTicks;
            if (markTick && stepIndex < MarchSteps)
            {
                float offset = (stepIndex + 1) * MarchTileSpacing * 16f;
                IEntitySource source = NPC.GetSource_FromThis();
                WardenLightning.SpawnSkyStrike(source, _marchOrigin.X - offset, _marchOrigin.Y, MarchMarkTicks, 2, StrikeExpert);
                WardenLightning.SpawnSkyStrike(source, _marchOrigin.X + offset, _marchOrigin.Y, MarchMarkTicks, 2, StrikeExpert);
            }

            _marchTimer++;
            if (_marchTimer >= MarchSteps * MarchStepTicks)
            {
                _marchTimer = -1;
            }
        }

        // ── Quake Slam (anti-air) ────────────────────────────────────────────────
        protected override void StartAntiAir(List<int> highPlayers)
        {
            QuakeArmed = true;
            NPC.netUpdate = true;

            WardenMove slam = Empowered ? WardenMove.ThunderExecution : WardenMove.OverheadExecution;
            StartMove(slam);
        }

        private void TickQuakeTell(int elapsed)
        {
            if (Main.dedServ)
            {
                return;
            }

            if (elapsed == 0)
            {
                SoundEngine.PlaySound(SoundID.DeerclopsRubbleAttack with { Volume = 0.7f, Pitch = -0.4f }, NPC.Center);
            }

            Vector2 head = WeaponPoint(HammerReach);
            Dust glow = Dust.NewDustPerfect(head + Main.rand.NextVector2Circular(14f, 14f), DustID.OrangeTorch, Vector2.Zero, 0, default, 1.4f);
            glow.noGravity = true;

            for (int i = 0; i < 3; i++)
            {
                int dustType = i == 0 ? DustID.GoldFlame : DustID.Stone;
                Vector2 floorPoint = new Vector2(NPC.Center.X + Main.rand.NextFloat(-70f, 70f), NPC.Bottom.Y - 2f);
                Dust rising = Dust.NewDustPerfect(floorPoint, dustType, new Vector2(0f, Main.rand.NextFloat(-4f, -1.5f)), 0, default, 1.1f);
                rising.noGravity = true;
            }

            // The warning reaches the player where they are: rubble trickling down from above.
            Player local = Main.LocalPlayer;
            bool localHigh = local.active && !local.dead && NPC.Bottom.Y - local.Bottom.Y >= AntiAirRisePixels;
            if (localHigh && elapsed % 2 == 0)
            {
                Vector2 rubblePoint = local.Center + new Vector2(Main.rand.NextFloat(-120f, 120f), -Main.rand.NextFloat(160f, 280f));
                Dust.NewDustPerfect(rubblePoint, DustID.Stone, new Vector2(0f, Main.rand.NextFloat(3f, 6f)), 0, default, 1.1f);
            }
        }

        // ── Selection ────────────────────────────────────────────────────────────
        protected override WardenMove FlankerMove => WardenMove.RubbleHeave;
        protected override WardenMove RegroupMove => WardenMove.ExecutionersBound;
        protected override WardenMove ReactiveMove => WardenMove.GrindingSpin;
        protected override int ReactiveCrossings => 2;
        protected override int ReactiveWindowTicks => 180;
        protected override float ReactiveRange => 160f;

        private int _tandemTimer = 900;

        protected override void FillBag(List<WardenMove> cards)
        {
            if (!Empowered)
            {
                cards.AddRange(new[]
                {
                    WardenMove.OverheadExecution, WardenMove.OverheadExecution, WardenMove.OverheadExecution, WardenMove.OverheadExecution,
                    WardenMove.WideSweep, WardenMove.WideSweep, WardenMove.WideSweep,
                    WardenMove.ExecutionersBound, WardenMove.ExecutionersBound,
                    WardenMove.SpikedCharge,
                    WardenMove.RubbleHeave, WardenMove.RubbleHeave,
                });
                return;
            }

            cards.AddRange(new[]
            {
                WardenMove.OverheadExecution, WardenMove.OverheadExecution, WardenMove.OverheadExecution,
                WardenMove.ThunderExecution, WardenMove.ThunderExecution,
                WardenMove.WideSweep,
                WardenMove.ChargedSweep, WardenMove.ChargedSweep,
                WardenMove.ExecutionersBound,
                WardenMove.StormBound, WardenMove.StormBound,
                WardenMove.SpikedCharge,
                WardenMove.RubbleHeave,
            });
        }

        protected override bool IsMoveEligible(WardenMove move, float distance, float rise, bool clearLine)
        {
            bool grounded = rise <= 64f;

            switch (move)
            {
                case WardenMove.OverheadExecution:
                case WardenMove.ThunderExecution:
                case WardenMove.WideSweep:
                case WardenMove.ChargedSweep:
                    return grounded && distance <= MeleeStrikeRange(move) + MeleeApproachRange;
                case WardenMove.ExecutionersBound:
                    return clearLine && distance >= 240f && distance <= 720f && BoundPathClear();
                case WardenMove.StormBound:
                    return clearLine && distance >= 260f && distance <= 760f && BoundPathClear();
                case WardenMove.SpikedCharge:
                    return grounded && clearLine && distance >= 300f && distance <= 900f;
                case WardenMove.RubbleHeave:
                    return clearLine && distance >= 280f && distance <= 900f;
                default:
                    return false;
            }
        }

        // Every 900-1,200t the presser opens a tandem; the live-window lock still spaces the two strikes.
        protected override void TryStartTandem(WardenPuppet partner, Player target)
        {
            bool partnerFree = partner.CurrentMove == WardenMove.None;
            if (_tandemTimer > 0 || !partnerFree || !(partner is Ornstein ornstein))
            {
                return;
            }

            float distance = NPC.Distance(target.Center);
            float rise = NPC.Bottom.Y - target.Bottom.Y;
            bool clearLine = Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1);

            bool slamInReach = distance <= MeleeStrikeRange(WardenMove.OverheadExecution);
            if (slamInReach && IsMoveEligible(WardenMove.OverheadExecution, distance, rise, clearLine))
            {
                // Anvil & Spark: Ornstein's tell starts at slam + 20, so his dash is live 32t after the slam window ends.
                const int SlamTellTicks = 46;
                const int PincerAfterSlamTicks = 20;
                StartMove(WardenMove.OverheadExecution);
                ornstein.QueueTandemPincer(SlamTellTicks + PincerAfterSlamTicks);
                _tandemTimer = Main.rand.Next(900, 1201);
            }
            else if (IsMoveEligible(WardenMove.ExecutionersBound, distance, rise, clearLine))
            {
                // Twin Bound: Ornstein's plunge starts late enough to land about 36t after Smough does.
                const int PlungeStartDelayTicks = 60;
                StartMove(WardenMove.ExecutionersBound);
                ornstein.QueueTandemPlunge(PlungeStartDelayTicks);
                _tandemTimer = Main.rand.Next(900, 1201);
            }
        }

        // ── Custom moves ─────────────────────────────────────────────────────────
        private const byte BoundTell = 0;
        private const byte BoundFlight = 1;
        private const byte BoundRecover = 2;
        private const int BoundTellTicks = 34;
        private const int BoundAirTicks = 52;
        private const int BoundRecoveryTicks = 60;
        private const float BoundApexHeight = 150f;
        private const int BoundSwingTicks = 10;
        private const float BoundDiskRadius = 90f;
        private const int BoundDiskLiveTicks = 8;

        private const byte ChargeTell = 0;
        private const byte ChargeRun = 1;
        private const byte ChargeUppercut = 2;
        private const byte ChargeRecover = 3;
        private const int ChargeTellTicks = 40;
        private const int ChargeMaxRunTicks = 150;
        private const float ChargeAcceleration = 0.24f;
        private const float ChargeUppercutRange = 120f;
        private const int ChargeUppercutTicks = 16;
        private const int ChargeRecoveryTicks = 45;
        private const int ChargeBonkRecoveryTicks = 70;
        private const int ChargeHitRefreshTicks = 45;

        private const int HeaveTellTicks = 40;
        private const int HeaveCommitTick = 27;
        private const int HeaveRockSpacing = 6;
        private const int HeaveRecoveryTicks = 30;
        private const float RockGravity = 0.25f;
        private static readonly float[] HeaveOffsets = { -60f, 0f, 60f, 120f, 180f };

        private float CarryRaised => -1.62f;
        private float SlamImpactRotation => 2.0f;
        private float ChargeTopSpeed => Empowered ? 8f : 7f;

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
                    case WardenMove.ExecutionersBound:
                    case WardenMove.StormBound:
                        return MoveStage != BoundRecover;
                    case WardenMove.SpikedCharge:
                        return MoveStage != ChargeRecover;
                    case WardenMove.RubbleHeave:
                        return MoveStage == 0 && MoveTicks >= HeaveCommitTick;
                    default:
                        return false;
                }
            }
        }

        protected override void TickBossMove(Player target, bool server)
        {
            switch (CurrentMove)
            {
                case WardenMove.ExecutionersBound:
                case WardenMove.StormBound:
                    TickBound(target, server);
                    break;
                case WardenMove.SpikedCharge:
                    TickCharge(target, server);
                    break;
                case WardenMove.RubbleHeave:
                    TickHeave(target, server);
                    break;
                default:
                    if (server)
                    {
                        EndMove();
                    }
                    break;
            }
        }

        private float FloorUnder(float worldX, float aroundY)
        {
            if (PuppetGroundDustWave.TryFindGroundY(worldX, aroundY, out float groundY))
            {
                return groundY;
            }

            return aroundY;
        }

        /// <summary>Samples the parabola TickBound flies (same chord, apex and landing point) and checks the body fits along it,
        /// so a low ceiling or a wall between the two never picks a Bound that would stop short.</summary>
        private bool BoundPathClear()
        {
            Player target = TargetPlayer;
            Vector2 startBottom = NPC.Bottom;
            Vector2 landingBottom = new Vector2(target.Center.X, FloorUnder(target.Center.X, target.Bottom.Y));
            float apexLift = Math.Max(BoundApexHeight, startBottom.Y - landingBottom.Y + 96f);
            Vector2 halfHeight = new Vector2(0f, NPC.height * 0.5f + 2f);

            const int Samples = 8;
            Vector2[] centers = new Vector2[Samples + 1];
            for (int i = 0; i <= Samples; i++)
            {
                float progress = i / (float)Samples;
                Vector2 chord = Vector2.Lerp(startBottom, landingBottom, progress);
                float arc = 4f * apexLift * progress * (1f - progress);
                centers[i] = chord - new Vector2(0f, arc) - halfHeight;
            }

            return BodyPathClear(centers);
        }

        private void TickBound(Player target, bool server)
        {
            switch (MoveStage)
            {
                case BoundTell:
                    NPC.velocity.X *= 0.8f;
                    FaceX(target.Center.X);
                    CustomRotation = CustomRotation.AngleLerp(CarryRaised, 0.15f);
                    SpawnHeadGlow(DustID.GoldFlame);

                    if (MoveTicks == 1)
                    {
                        SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.7f, Pitch = -0.6f }, NPC.Center);
                    }

                    if (server && MoveTicks >= BoundTellTicks && !HoldCustomTellForLock() && CurrentMove != WardenMove.LockCancel)
                    {
                        MoveStart = NPC.Bottom;
                        MovePoint = new Vector2(target.Center.X, FloorUnder(target.Center.X, target.Bottom.Y));
                        SetStage(BoundFlight);
                    }
                    break;

                case BoundFlight:
                {
                    NPC.noGravity = true;
                    float progress = MathHelper.Clamp(MoveTicks / (float)BoundAirTicks, 0f, 1f);
                    float apexLift = Math.Max(BoundApexHeight, MoveStart.Y - MovePoint.Y + 96f);
                    Vector2 chord = Vector2.Lerp(MoveStart, MovePoint, progress);
                    float arc = 4f * apexLift * progress * (1f - progress);
                    Vector2 desiredBottom = chord - new Vector2(0f, arc);
                    NPC.velocity = desiredBottom - NPC.Bottom;

                    float swingStart = 1f - BoundSwingTicks / (float)BoundAirTicks;
                    float swing = MathHelper.Clamp((progress - swingStart) / (1f - swingStart), 0f, 1f);
                    CustomRotation = MathHelper.Lerp(CarryRaised, SlamImpactRotation, swing * swing);

                    if (server && MoveTicks >= BoundAirTicks)
                    {
                        LandBound();
                    }
                    break;
                }

                case BoundRecover:
                    NPC.noGravity = false;
                    NPC.velocity.X *= 0.8f;
                    CustomRotation = CustomRotation.AngleLerp(-0.3f, 0.06f);
                    if (server && MoveTicks >= BoundRecoveryTicks)
                    {
                        EndMove();
                    }
                    break;
            }
        }

        private void LandBound()
        {
            NPC.noGravity = false;
            NPC.velocity = Vector2.Zero;
            MarkLive();

            IEntitySource source = NPC.GetSource_FromThis();
            bool storm = CurrentMove == WardenMove.StormBound;
            int diskDamage = BoundExpert;

            WardenLightning.Spawn(source, NPC.Bottom - new Vector2(0f, 20f), Vector2.Zero, WardenBoltMode.Shock, diskDamage, BoundDiskRadius, BoundDiskLiveTicks);
            WardenLightning.Spawn(source, NPC.Bottom, Vector2.Zero, WardenBoltMode.Burst, 0, 0f, NormalSlamShake);

            if (Empowered)
            {
                WardenLightning.SpawnCrawlerPair(source, NPC.Center.X, NPC.Bottom.Y - 8f, 9f, 24, 12, CrawlerExpert);
            }
            else
            {
                // The jumpable ground ring reaches 220px from the landing: 14 tiles of travel total at 5 px/t.
                WardenLightning.SpawnCrawlerPair(source, NPC.Center.X, NPC.Bottom.Y - 8f, 5f, 14, 8, WaveExpert);
            }

            if (storm)
            {
                float encodedStartAndSpeed = BoundDiskRadius * 100f + 9f;
                WardenLightning.Spawn(source, NPC.Center, Vector2.Zero, WardenBoltMode.Annulus, RingExpert, 400f, encodedStartAndSpeed);
            }

            SetStage(BoundRecover);
        }

        private void SpawnHeadGlow(int dustType)
        {
            if (Main.dedServ)
            {
                return;
            }

            Vector2 head = WeaponPoint(HammerReach);
            Dust glow = Dust.NewDustPerfect(head + Main.rand.NextVector2Circular(14f, 14f), dustType, Vector2.Zero, 60, default, 1f);
            glow.noGravity = true;
        }

        private void TickCharge(Player target, bool server)
        {
            switch (MoveStage)
            {
                case ChargeTell:
                    NPC.velocity.X *= 0.8f;
                    FaceX(target.Center.X);
                    CustomRotation = CustomRotation.AngleLerp(2.0f, 0.12f);
                    SpawnHeadGlow(DustID.Torch);

                    if (MoveTicks == 1)
                    {
                        SoundEngine.PlaySound(SoundID.DD2_OgreRoar with { Volume = 0.45f, Pitch = 0.2f }, NPC.Center);
                    }

                    if (server && MoveTicks >= ChargeTellTicks && !HoldCustomTellForLock() && CurrentMove != WardenMove.LockCancel)
                    {
                        ArmBlade(HammerReach, ChargeExpert);
                        SetStage(ChargeRun);
                    }
                    break;

                case ChargeRun:
                    TickChargeRun(target, server);
                    break;

                case ChargeUppercut:
                {
                    NPC.velocity.X *= 0.9f;
                    float swing = MathHelper.Clamp(MoveTicks / (float)ChargeUppercutTicks, 0f, 1f);
                    CustomRotation = MathHelper.Lerp(2.0f, -1.45f, 1f - (1f - swing) * (1f - swing));
                    MarkLive();
                    TickBladeHit();

                    if (server && MoveTicks >= ChargeUppercutTicks)
                    {
                        StopMeleeHit();
                        MoveFlag = false;
                        SetStage(ChargeRecover);
                    }
                    break;
                }

                case ChargeRecover:
                {
                    NPC.velocity.X *= 0.85f;
                    CustomRotation = CustomRotation.AngleLerp(-0.3f, 0.08f);
                    int recoveryTicks = MoveFlag ? ChargeBonkRecoveryTicks : ChargeRecoveryTicks;
                    if (server && MoveTicks >= recoveryTicks)
                    {
                        EndMove();
                    }
                    break;
                }
            }
        }

        private void TickChargeRun(Player target, bool server)
        {
            float wanted = Math.Sign(target.Center.X - NPC.Center.X) * ChargeTopSpeed;
            NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X + Math.Sign(wanted - NPC.velocity.X) * ChargeAcceleration, -ChargeTopSpeed, ChargeTopSpeed);

            if (NPC.velocity.X != 0f)
            {
                NPC.direction = Math.Sign(NPC.velocity.X);
                NPC.spriteDirection = NPC.direction;
            }

            float stepSpeed = 1f;
            float gfxOffset = NPC.gfxOffY;
            Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref stepSpeed, ref gfxOffset);
            NPC.gfxOffY = gfxOffset;

            CustomRotation = 2.0f;
            MarkLive();
            TickBladeHit();

            if (MoveTicks % 12 == 0 && !Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.DeerclopsStep with { Volume = 0.45f, MaxInstances = 3 }, NPC.Bottom);
                UsefulFunctions.ScreenShake(NPC.Bottom, 1.5f, 8, distanceFalloff: 900f);
            }

            if (!server)
            {
                return;
            }

            // The hit list clears periodically, so a long run can catch a player it passed earlier.
            if (MoveTicks % ChargeHitRefreshTicks == 0)
            {
                ArmBlade(HammerReach, ChargeExpert);
            }

            bool bonked = MoveTicks > 20 && NPC.collideX && Math.Abs(NPC.oldVelocity.X) > 3f;
            if (bonked)
            {
                StopMeleeHit();
                MoveFlag = true;
                NPC.velocity.X = -NPC.direction * 3f;
                NPC.velocity.Y = -3f;
                WardenLightning.Spawn(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, WardenBoltMode.Burst, 0, 0f, NormalSlamShake);
                ThrowImpactWavesIfEmpowered();
                SetStage(ChargeRecover);
                return;
            }

            bool reachedTarget = NPC.Distance(target.Center) <= ChargeUppercutRange && MoveTicks > 20;
            if (reachedTarget)
            {
                ArmBlade(HammerReach, ChargeExpert);
                SoundEngine.PlaySound(SoundID.Item71 with { Pitch = -0.2f }, NPC.Center);
                SetStage(ChargeUppercut);
                return;
            }

            if (MoveTicks >= ChargeMaxRunTicks)
            {
                StopMeleeHit();
                SetStage(ChargeRecover);
            }
        }

        private void ThrowImpactWavesIfEmpowered()
        {
            if (Empowered)
            {
                ThrowImpactWaves(NPC.Center.X);
            }
        }

        private void TickHeave(Player target, bool server)
        {
            NPC.velocity.X *= 0.75f;
            int rockCount = Empowered ? 5 : 4;

            if (MoveStage == 1)
            {
                CustomRotation = CustomRotation.AngleLerp(-0.3f, 0.08f);
                if (server && MoveTicks >= HeaveRecoveryTicks)
                {
                    EndMove();
                }

                return;
            }

            float swingProgress = MathHelper.Clamp((MoveTicks - (HeaveTellTicks - 8f)) / 8f, 0f, 1f);
            float tellPose = MathHelper.Lerp(CarryRaised, SlamImpactRotation, swingProgress * swingProgress);
            CustomRotation = CustomRotation.AngleLerp(tellPose, 0.3f);
            FaceX(MovePoint.X);

            if (MoveTicks < HeaveTellTicks)
            {
                MovePoint = target.Center;
            }

            if (MoveTicks == HeaveCommitTick && !Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.6f, Pitch = 0.1f }, NPC.Center);
                for (int i = 0; i < 16; i++)
                {
                    Dust flash = Dust.NewDustPerfect(NPC.Center, DustID.GoldFlame, Main.rand.NextVector2CircularEdge(4f, 4f), 0, default, 1.3f);
                    flash.noGravity = true;
                }
            }

            int ticksPastSlam = MoveTicks - HeaveTellTicks;
            if (ticksPastSlam == 0)
            {
                OnHeaveSlam();
            }

            bool rockTick = ticksPastSlam >= 0 && ticksPastSlam % HeaveRockSpacing == 0;
            int rockIndex = ticksPastSlam / HeaveRockSpacing;
            if (server && rockTick && rockIndex < rockCount)
            {
                ThrowRock(rockIndex);
            }

            if (server && rockIndex >= rockCount)
            {
                SetStage(1);
            }
        }

        private void OnHeaveSlam()
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.DeerclopsRubbleAttack with { Volume = 0.8f, Pitch = 0.1f }, NPC.Bottom);
                UsefulFunctions.ScreenShake(NPC.Bottom, 5f, 12, distanceFalloff: 1200f);
            }

            if (IsServerSide)
            {
                ThrowImpactWavesIfEmpowered();
            }
        }

        // Gravity-solved lob onto the mark: flight time from distance, then the launch that lands exactly there.
        private void ThrowRock(int rockIndex)
        {
            int awayFromBoss = Math.Sign(MovePoint.X - NPC.Center.X);
            if (awayFromBoss == 0)
            {
                awayFromBoss = NPC.direction;
            }

            float markX = MovePoint.X + HeaveOffsets[rockIndex] * awayFromBoss;
            float markY = FloorUnder(markX, MovePoint.Y);
            Vector2 start = WeaponPoint(HammerReach) - new Vector2(0f, 12f);
            Vector2 toMark = new Vector2(markX, markY) - start;

            float flightTicks = MathHelper.Clamp(Math.Abs(toMark.X) / 7f, 40f, 80f);
            float velocityX = toMark.X / flightTicks;
            float velocityY = (toMark.Y - 0.5f * RockGravity * flightTicks * flightTicks) / flightTicks;

            WardenBoltMode mode = Empowered ? WardenBoltMode.ChargedRock : WardenBoltMode.Rock;
            WardenLightning.Spawn(NPC.GetSource_FromThis(), start, new Vector2(velocityX, velocityY), mode, RockExpert, markX, markY);
        }

        // ── Presentation ─────────────────────────────────────────────────────────
        protected override void TickBossPresentation()
        {
            if (Main.dedServ || !Empowered || Main.GameUpdateCount % 4 != 0)
            {
                return;
            }

            Dust crackle = Dust.NewDustPerfect(WeaponPoint(HammerReach) + Main.rand.NextVector2Circular(12f, 12f), DustID.Electric, Vector2.Zero, 60, default, 0.9f);
            crackle.noGravity = true;
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
            NPC.defense = 55;
        }
    }
}
