using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Events;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Content.Items;
using tsorcRevamp.Content.Items.Lore;
using tsorcRevamp.Content.Items.Potions.RadiantLifegem;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using tsorcRevamp.Content.Projectiles.VFX;
using tsorcRevamp.NPCs.Invaders;
using tsorcRevamp.Systems;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses.Ornsmough
{
    public enum DuoRole : byte
    {
        Paired,
        Fallen,
        Empowering,
        Alone,
    }

    public enum WardenMove : byte
    {
        None,
        CutsceneIdle,
        LockCancel,

        // Ornstein
        ArcThrust,
        ReapingSweep,
        ColossalCleave,
        LungingSkewer,
        PincerDash,
        ColossusSkewer,
        LeapingPlunge,
        ThunderJavelin,
        ThunderLance,
        StormPillar,
        Constellation,
        DragonRush,
        ReapingVault,

        // Smough
        OverheadExecution,
        ThunderExecution,
        WideSweep,
        ChargedSweep,
        GrindingSpin,
        ExecutionersBound,
        StormBound,
        SpikedCharge,
        RubbleHeave,
    }

    /// <summary>
    /// Shared body of Ornstein and Smough: the duo rules (press token, 30t cross-NPC live-window lock, tandem queue, regroup),
    /// bag selection, anti-air trigger, cutscene holds, death handoff, growth and sync. Every decision runs on the server;
    /// the Custom-phase moves in the two subclasses advance stages on the server and every peer plays the stage's motion.
    /// </summary>
    public abstract class WardenPuppet : PuppetNPC
    {
        // ── Identity ────────────────────────────────────────────────────────────
        protected override bool AnnounceInvasion => false;
        protected override bool AnnounceInvaderDefeat => false;
        public override bool UsesPuppetDifficultyScaling => false;
        protected override int EstusChargesMax => 0;
        protected override int CasualStrollChance => 0;
        protected override Color DespawnFlavorColor => Color.Gold;
        protected override int DespawnDustType => DustID.GoldFlame;
        protected override int RangedWeaponItemType => -1;
        protected override int SpearWeaponItemType => -1;
        protected override int MagicWeaponItemType => -1;
        protected override int RangedDamage => 0;

        // Selection belongs to the director below, so the base neutral picker never runs.
        protected override bool HoldAttackSelection => true;
        protected override bool ShowWeaponDuringNeutral => true;
        protected override bool HoldNeutralWeaponOnCompositeArm => true;
        protected override bool ShowMeleeWeaponWhileClosingDistance => true;
        protected override bool UseCompositeArmSwing => true;
        protected override bool MirrorMeleeSwingRotationByFacing => true;
        protected override bool UseLogicalMeleeTelegraphs => true;
        protected override bool UseSwingEasing => true;
        protected override bool UseAuthoredComboSwingClock => true;
        protected override bool UseTwoHandedCompositeSwing => true;
        protected override bool UseEasedSpin => true;
        protected override bool SlowDownBeforeMelee => false;
        protected override int MeleeComboInterStepLingerTicks => 3;
        protected override int MeleeRecoveryLingerTicks => 10;
        protected override float ComboTelegraphMultiplier => 1f;
        protected override int MinComboTelegraphTicks => 20;
        protected override bool HyperArmorDuringTelegraph => Phase == AttackPhase.MeleeComboTelegraph;
        protected override bool SlowDownDuringCustom => false;
        protected override bool UseCompositeArmForAdditionalPhase => Phase == AttackPhase.Custom;
        protected override float PuppetJumpPower => 9.5f;
        protected override float PuppetJumpBoost => 5f;

        // ── Scale: 1.35 in phase one, growing to 1.5 through the empowerment scene ──
        public const float PairedScale = 1.35f;
        public const float EmpoweredScale = 1.5f;
        protected float BodyScale => MathHelper.Lerp(PairedScale, EmpoweredScale, GrowthProgress / 255f);
        protected override float PuppetDrawScale => BodyScale;

        // Hidden 0 = fully drawn. Stored inverted so an unspawned instance (the swing preview) draws normally.
        internal override float BodyOpacity => 1f - _hiddenAmount;

        // ── Synced duo state ─────────────────────────────────────────────────────
        public int PartnerWhoAmI = -1;
        public DuoRole Role = DuoRole.Paired;
        public bool CutsceneHold;
        public bool HasPressToken;
        public WardenMove CurrentMove;
        public bool Empowered;
        public byte GrowthProgress;
        public bool ScriptRaiseWeapon;
        public bool ScriptChannel;
        public bool ScriptGrow;
        public float ScriptWalkX = float.NaN;
        public float ScriptFaceX = float.NaN;
        public byte ScriptHiddenTarget;
        public bool QuakeArmed;
        public bool StartsHidden;

        protected byte MoveStage;
        protected int MoveTicks;
        protected Vector2 MoveAim = Vector2.UnitX;
        protected Vector2 MovePoint;
        protected Vector2 MoveStart;
        protected int MoveDirection = 1;
        protected bool MoveFlag;

        // ── Local presentation / server-only state ───────────────────────────────
        protected float CustomRotation = -0.3f;
        protected bool MoveBladeHit;
        protected int DamageOverride;
        private float _hiddenAmount;
        private bool _fadeInitialized;

        public uint LiveWindowEndTick;
        private uint _lastAntiAirTick;
        private string _queuedComboName;
        private AttackBag<WardenMove> _bag;
        private readonly int[] _highTicks = new int[Main.maxPlayers];
        private int _entryGrace;
        private int _neutralGap;
        private int _tokenHeldTicks;
        private int _tokenHoldLimit = 480;
        private int _flankCooldown;
        private int _antiAirCooldown;
        private int _separationTicks;
        private int _lockHoldTicks;
        private WardenMove _queuedMove;
        private int _queuedDelay = -1;
        private bool _wasNeutral = true;
        private int _lastTargetSide;
        private int _crossCount;
        private int _crossWindow;
        private WardenMove _approachMove;
        private int _approachTicks;

        // ── Tuning shared by both bosses ─────────────────────────────────────────
        public const int PostGapTicks = 30;
        public const float AntiAirRisePixels = 25f * 16f;
        private const float AntiAirMaxHorizontal = 100f * 16f;
        private const int AntiAirTriggerTicks = 30;
        private const int AntiAirCooldownTicks = 600;
        private const int AntiAirPartnerSpacing = 120;
        private const int LockHoldCapTicks = 60;
        private const int LockCancelTicks = 30;
        private const int NeutralEntryGrace = 45;
        // Idle time after any move, alone or paired. Owl Father-style pressure: the recoveries are the breathing room.
        private const int NeutralGapTicks = 20;
        private const int FlankCooldownTicks = 150;
        // A melee card drawn out of reach runs in first: up to MeleeApproachRange past the move's strike range, for at most
        // MeleeApproachMaxTicks at ApproachSpeedMult x walk speed, then swings once inside the strike range.
        protected const float MeleeApproachRange = 200f;
        private const int MeleeApproachMaxTicks = 90;
        private const float ApproachSpeedMult = 1.4f;
        private const float FlankDistance = 21f * 16f;
        private const float TokenCloserPixels = 160f;
        private const int TokenPassChancePercent = 40;
        private const float RegroupDistance = 45f * 16f;
        private const int RegroupTicks = 360;
        private const float GroundRiseLimit = 64f;
        private const int CustomSafetyTicks = 900;
        private const float ArmedSpeedShare = 0.30f;

        // ── Subclass contract ────────────────────────────────────────────────────
        protected abstract int PairedAuthoredLife { get; }
        public abstract int EmpoweredAuthoredLife { get; }
        protected abstract int EmpoweredDefense { get; }
        protected abstract float EmpoweredPoise { get; }
        protected abstract float EmpoweredKnockbackResist { get; }
        protected abstract WardenMove FlankerMove { get; }
        protected abstract WardenMove RegroupMove { get; }
        protected abstract int ReactiveCrossings { get; }
        protected abstract int ReactiveWindowTicks { get; }
        protected abstract float ReactiveRange { get; }
        protected abstract WardenMove ReactiveMove { get; }
        protected abstract void FillBag(List<WardenMove> cards);
        protected abstract bool IsMoveEligible(WardenMove move, float distance, float rise, bool clearLine);
        /// <summary>How close a melee card must be before it swings; 0 for moves that never run in first.</summary>
        protected abstract float MeleeStrikeRange(WardenMove move);
        /// <summary>Cards the partner without the press token may play from its flank.</summary>
        protected abstract bool IsFlankerCard(WardenMove move);
        protected abstract Color SwingArcBodyColor { get; }
        protected abstract Color SwingArcCoreColor { get; }
        protected abstract Color SwingArcDarkColor { get; }
        protected abstract string ComboNameFor(WardenMove move);
        protected abstract void TickBossMove(Player target, bool server);
        protected abstract void StartAntiAir(List<int> highPlayers);
        protected virtual void TickBossServerTimers() { }
        protected virtual void TryStartTandem(WardenPuppet partner, Player target) { }
        protected virtual void TickBossPresentation() { }

        // ── Helpers ──────────────────────────────────────────────────────────────
        public WardenPuppet Partner
        {
            get
            {
                if (PartnerWhoAmI < 0 || PartnerWhoAmI >= Main.maxNPCs)
                {
                    return null;
                }

                NPC partnerNpc = Main.npc[PartnerWhoAmI];
                if (!partnerNpc.active)
                {
                    return null;
                }

                return partnerNpc.ModNPC as WardenPuppet;
            }
        }

        protected Player TargetPlayer => Main.player[NPC.target];
        protected bool IsServerSide => Main.netMode != NetmodeID.MultiplayerClient;

        protected static MeleeComboStep WeightedStep(ComboMotion motion, int telegraph, int easeInTicks, int easeOutTicks, float decay, int pause, float damage, float reach, float stepIn = 0f)
        {
            int attackTicks = easeInTicks + easeOutTicks;
            float armedSettleTicks = easeOutTicks * (float)Math.Log(1f / ArmedSpeedShare) / decay;
            return new MeleeComboStep
            {
                Motion = motion,
                TelegraphTicks = telegraph,
                AttackTicks = attackTicks,
                PostStepPause = pause,
                DamageMult = damage,
                ReachMult = reach,
                Ease = SwingEaseStyle.Weighted,
                EaseInTicks = easeInTicks,
                EaseOutTicks = easeOutTicks,
                EaseOutDecay = decay,
                // Live for the whole swing, settle included: rolling is judged by how long the blade overlaps the player.
                HitWindowEnd = 1f,
                StepInDistance = stepIn,
            };
        }

        /// <summary>DrawPlayer scales every layer about the feet; gameplay points follow the drawn rig the same way.</summary>
        protected Vector2 ScaleAboutFeet(Vector2 unscaledPoint)
        {
            return NPC.Bottom + (unscaledPoint - NPC.Bottom) * BodyScale;
        }

        protected Vector2 WeaponPoint(float reach)
        {
            return ScaleAboutFeet(PuppetWeaponTipPosition(0f)) + PuppetWeaponDirection * reach;
        }

        protected override void ModifyFrontBladeCapsule(ref Vector2 origin, ref Vector2 tip)
        {
            Vector2 reachVector = tip - origin;
            origin = ScaleAboutFeet(origin);
            tip = origin + reachVector;
        }

        protected float BladeRotationToward(Vector2 worldDirection)
        {
            float worldAngle = worldDirection.ToRotation();
            float drawOffset = FrontHandWeapon.RotationOffset;
            float restRadians = MathHelper.ToRadians(MeleeNaturalRestAngleDeg);

            if (NPC.direction == 1)
            {
                return MathHelper.WrapAngle(worldAngle + restRadians) - drawOffset;
            }

            float mirroredRestRadians = MathHelper.Pi - restRadians;
            return -MathHelper.WrapAngle(worldAngle + mirroredRestRadians) - drawOffset;
        }

        protected void FaceX(float worldX)
        {
            int facing = 1;
            if (worldX < NPC.Center.X)
            {
                facing = -1;
            }

            NPC.direction = facing;
            NPC.spriteDirection = facing;
        }

        protected void ArmBlade(float reach, int expertDamage)
        {
            DamageOverride = EnemyDamage.Projectile(expertDamage);
            MoveBladeHit = false;
            TryMeleeHit(reach);
            DamageOverride = 0;
        }

        protected int ResolveMeleeDamage(int expertDamage)
        {
            if (DamageOverride > 0)
            {
                return DamageOverride;
            }

            return EnemyDamage.Projectile(expertDamage);
        }

        protected override void OnBladeHit(Player player)
        {
            base.OnBladeHit(player);
            MoveBladeHit = true;
        }

        protected void MarkLive()
        {
            LiveWindowEndTick = Main.GameUpdateCount;
        }

        /// <summary>The 30t cross-NPC lock: neither partner goes live inside the other's post-roll gap.</summary>
        protected bool CanGoLive
        {
            get
            {
                WardenPuppet partner = Partner;
                if (partner == null || Role != DuoRole.Paired || partner.Role != DuoRole.Paired)
                {
                    return true;
                }

                return Main.GameUpdateCount >= partner.LiveWindowEndTick + PostGapTicks;
            }
        }

        protected void SetStage(byte stage)
        {
            MoveStage = stage;
            MoveTicks = 0;
            NPC.netUpdate = true;
            RequestNetworkSnapshot();
        }

        protected void EndMove()
        {
            NPC.noGravity = false;
            StopMeleeHit();
            MoveStage = 0;
            MoveTicks = 0;
            DebugAttackLabel = null;

            if (CutsceneHold)
            {
                CurrentMove = WardenMove.CutsceneIdle;
            }
            else
            {
                CurrentMove = WardenMove.None;
                PhaseTimer = 1;
            }

            NPC.netUpdate = true;
            RequestNetworkSnapshot();
        }

        // ── Starting moves ───────────────────────────────────────────────────────
        protected bool StartMove(WardenMove move)
        {
            ClearApproach();
            string comboName = ComboNameFor(move);
            if (comboName != null)
            {
                _queuedComboName = comboName;
                bool started = TryStartMeleeCombo(NPC.Distance(TargetPlayer.Center));
                _queuedComboName = null;
                if (started)
                {
                    CurrentMove = move;
                    DebugAttackLabel = move.ToString();
                    NPC.netUpdate = true;
                }

                return started;
            }

            BeginCustomMove(move);
            return true;
        }

        protected void BeginCustomMove(WardenMove move)
        {
            ClearApproach();
            CurrentMove = move;
            MoveStage = 0;
            MoveTicks = 0;
            MoveFlag = false;
            MoveBladeHit = false;
            MovePoint = TargetPlayer.Center;
            MoveStart = NPC.Center;
            MoveAim = new Vector2(NPC.direction, 0f);
            MoveDirection = NPC.direction;
            CustomRotation = -0.3f;
            DebugAttackLabel = move.ToString();
            StartCustomAttack(CustomSafetyTicks, MeleeWeaponItemType, swingPose: false);
            NPC.netUpdate = true;
            RequestNetworkSnapshot();
        }

        private void ClearApproach()
        {
            if (_approachMove == WardenMove.None)
            {
                return;
            }

            _approachMove = WardenMove.None;
            _approachTicks = 0;
            NPC.netUpdate = true;
        }

        /// <summary>Path check for the scripted leaps: the body fits at every point and each leg between them is clear.
        /// Gating on this keeps a leap from being chosen when a wall or ceiling would stop it short.</summary>
        protected bool BodyPathClear(params Vector2[] centers)
        {
            Vector2 halfBody = new Vector2(NPC.width, NPC.height) * 0.5f;

            for (int i = 0; i < centers.Length; i++)
            {
                Vector2 bodyTopLeft = centers[i] - halfBody;
                if (Collision.SolidCollision(bodyTopLeft, NPC.width, NPC.height))
                {
                    return false;
                }

                bool legBlocked = i > 0 && !Collision.CanHitLine(centers[i - 1], 1, 1, centers[i], 1, 1);
                if (legBlocked)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Queues a move for this boss once it is next neutral, delayTicks from now (tandems, regroup).</summary>
        public void QueueMove(WardenMove move, int delayTicks)
        {
            _queuedMove = move;
            _queuedDelay = delayTicks;
        }

        protected override bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction)
        {
            return combo.Name == _queuedComboName;
        }

        protected override float? CustomWeaponRotation
        {
            get
            {
                if (CurrentMove == WardenMove.None)
                {
                    return null;
                }

                return CustomRotation;
            }
        }

        protected override void DoCustomTick(int ticksRemaining)
        {
            // Every move ends itself; keep the base Custom timer from expiring under it.
            if (CurrentMove != WardenMove.None)
            {
                PhaseTimer = CustomSafetyTicks;
            }

            MoveTicks++;
            bool server = IsServerSide;

            switch (CurrentMove)
            {
                case WardenMove.None:
                    break;

                case WardenMove.CutsceneIdle:
                    TickCutsceneIdle();
                    break;

                case WardenMove.LockCancel:
                    NPC.velocity.X *= 0.8f;
                    CustomRotation = CustomRotation.AngleLerp(-0.3f, 0.12f);
                    if (server && MoveTicks >= LockCancelTicks)
                    {
                        EndMove();
                    }
                    break;

                default:
                    TickBossMove(TargetPlayer, server);
                    break;
            }
        }

        private void TickCutsceneIdle()
        {
            NPC.noGravity = false;

            float walkSpeed = 0f;
            if (!float.IsNaN(ScriptWalkX) && Math.Abs(ScriptWalkX - NPC.Center.X) > 8f)
            {
                walkSpeed = TopSpeed * Math.Sign(ScriptWalkX - NPC.Center.X);
                FaceX(ScriptWalkX);
            }
            else if (!float.IsNaN(ScriptFaceX))
            {
                FaceX(ScriptFaceX);
            }

            NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, walkSpeed, 0.25f);

            float pose = -0.3f;
            if (ScriptRaiseWeapon)
            {
                pose = BladeRotationToward(-Vector2.UnitY);
            }

            CustomRotation = CustomRotation.AngleLerp(pose, 0.1f);
        }

        protected override bool CustomAttackCommitted => CurrentMove == WardenMove.CutsceneIdle;

        public override void OnStagger(NPC npc)
        {
            base.OnStagger(npc);
            NPC.noGravity = false;
            CurrentMove = WardenMove.None;
            MoveStage = 0;
            MoveTicks = 0;
            QuakeArmed = false;
        }

        // ── Movement ─────────────────────────────────────────────────────────────
        protected override void RunMovementAI(float speedMult)
        {
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.NavSearchRadius = 80;
            globalNPC.RemembersLastKnownPos = true;

            Vector2? waypoint = null;
            WardenPuppet partner = Partner;
            bool flanking = Role == DuoRole.Paired && partner != null && !HasPressToken && NPC.HasValidTarget;
            if (flanking)
            {
                // Far side of the player from the presser, just inside ranged-move distance: the two close from both sides.
                Player target = TargetPlayer;
                int side = Math.Sign(target.Center.X - partner.NPC.Center.X);
                if (side == 0)
                {
                    side = 1;
                }

                waypoint = new Vector2(target.Center.X + side * FlankDistance, target.Center.Y);
            }

            float approachMult = 1f;
            if (_approachMove != WardenMove.None)
            {
                approachMult = ApproachSpeedMult;
            }

            SmartFighter4AI.Run(NPC, topSpeed: TopSpeed * speedMult * approachMult, acceleration: Acceleration, doorBreakingDamage: 5,
                attackRange: 850f, movementWaypoint: waypoint);
        }

        // ── Per-tick flow ────────────────────────────────────────────────────────
        public override void AI()
        {
            bool server = IsServerSide;
            NPC.dontTakeDamage = CutsceneHold || Role == DuoRole.Fallen;

            if (Empowered)
            {
                NPC.defense = EmpoweredDefense;
                NPC.defDefense = EmpoweredDefense;
            }

            float hiddenRate = 1f / 20f;
            if (Role == DuoRole.Fallen)
            {
                hiddenRate = 1f / 120f;
            }

            // A Warden spawned by a scene starts invisible on every machine, then fades in.
            if (StartsHidden && !_fadeInitialized)
            {
                _fadeInitialized = true;
                _hiddenAmount = 1f;
            }

            float hiddenTarget = ScriptHiddenTarget / 255f;
            if (_hiddenAmount < hiddenTarget)
            {
                _hiddenAmount = Math.Min(hiddenTarget, _hiddenAmount + hiddenRate);
            }
            else
            {
                _hiddenAmount = Math.Max(hiddenTarget, _hiddenAmount - hiddenRate);
            }

            if (ScriptGrow && GrowthProgress < 255)
            {
                GrowthProgress = (byte)Math.Min(255, GrowthProgress + 5);
            }

            // Scripts only set CutsceneHold; the Custom phase is entered here, once the puppet is live.
            bool needsHoldEntry = CutsceneHold && (Phase != AttackPhase.Custom || CurrentMove == WardenMove.None);
            if (server && needsHoldEntry)
            {
                StopMeleeHit();
                NPC.noGravity = false;
                CurrentMove = WardenMove.CutsceneIdle;
                MoveStage = 0;
                MoveTicks = 0;
                StartCustomAttack(CustomSafetyTicks, MeleeWeaponItemType, swingPose: false);
                NPC.netUpdate = true;
            }

            if (server)
            {
                TickDirector();
                HoldTellForLock();
            }

            base.AI();
            NPC.damage = 0;
            TickChannelVisuals();
            TickBossPresentation();
        }

        // A combo tell or pause about to go live waits out the partner's window, then cancels at the 60t cap.
        private void HoldTellForLock()
        {
            bool aboutToGoLive = (Phase == AttackPhase.MeleeComboTelegraph || Phase == AttackPhase.MeleeComboPause) && PhaseTimer <= 1;
            if (!aboutToGoLive || CanGoLive)
            {
                if (Phase != AttackPhase.MeleeComboTelegraph && Phase != AttackPhase.MeleeComboPause)
                {
                    _lockHoldTicks = 0;
                }

                return;
            }

            _lockHoldTicks++;
            if (_lockHoldTicks > LockHoldCapTicks)
            {
                _lockHoldTicks = 0;
                BeginCustomMove(WardenMove.LockCancel);
                return;
            }

            PhaseTimer = 2;
            RequestNetworkSnapshot();
        }

        /// <summary>Custom moves ask this before each live stage: true while the tell should keep holding, capped at 60t.</summary>
        protected bool HoldCustomTellForLock()
        {
            if (CanGoLive)
            {
                _lockHoldTicks = 0;
                return false;
            }

            _lockHoldTicks++;
            if (_lockHoldTicks > LockHoldCapTicks)
            {
                _lockHoldTicks = 0;
                CurrentMove = WardenMove.LockCancel;
                SetStage(0);
            }

            return true;
        }

        private void TickDirector()
        {
            TickBossServerTimers();

            if (_antiAirCooldown > 0)
            {
                _antiAirCooldown--;
            }

            if (_flankCooldown > 0)
            {
                _flankCooldown--;
            }

            if (_crossWindow > 0)
            {
                _crossWindow--;
                if (_crossWindow == 0)
                {
                    _crossCount = 0;
                }
            }

            if (_queuedDelay > 0)
            {
                _queuedDelay--;
            }

            if (CutsceneHold || Role == DuoRole.Fallen)
            {
                ClearApproach();
                _wasNeutral = false;
                _entryGrace = NeutralEntryGrace;
                return;
            }

            WardenPuppet partner = Partner;
            if (Role == DuoRole.Paired && partner == null)
            {
                Role = DuoRole.Alone;
                NPC.netUpdate = true;
            }

            if (!NPC.HasValidTarget)
            {
                return;
            }

            Player target = TargetPlayer;
            List<int> highPlayers = UpdateHighPlayers();
            TrackSideCrossings(target);
            TickPressToken(partner, target);
            TickRegroup(partner, target);

            bool neutral = Phase == AttackPhase.Idle || Phase == AttackPhase.CasualStroll || Phase == AttackPhase.ClosingDistance;
            if (neutral && !_wasNeutral)
            {
                OnReturnedToNeutral(partner);
            }

            _wasNeutral = neutral;
            if (!neutral || NPC.velocity.Y != 0f)
            {
                return;
            }

            if (_entryGrace > 0)
            {
                _entryGrace--;
                return;
            }

            if (TryStartAntiAir(highPlayers, partner))
            {
                return;
            }

            if (_queuedMove != WardenMove.None && _queuedDelay <= 0)
            {
                WardenMove queued = _queuedMove;
                _queuedMove = WardenMove.None;
                _queuedDelay = -1;
                StartMove(queued);
                return;
            }

            float distance = NPC.Distance(target.Center);
            bool reactiveDue = ReactiveMove != WardenMove.None && _crossCount >= ReactiveCrossings && distance <= ReactiveRange;
            if (reactiveDue)
            {
                _crossCount = 0;
                StartMove(ReactiveMove);
                return;
            }

            float rise = NPC.Bottom.Y - target.Bottom.Y;
            bool clearLine = Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1);
            bool pairedFlanker = Role == DuoRole.Paired && partner != null && !HasPressToken;

            // Running in for a drawn melee card: swing once inside its strike range, give up after the cap or if the
            // player got away. Losing the press token hands the close fight to the partner, so the run-in stops too.
            if (_approachMove != WardenMove.None)
            {
                _approachTicks++;
                float strikeRange = MeleeStrikeRange(_approachMove);
                bool inStrikeRange = distance <= strikeRange && rise <= GroundRiseLimit;
                bool playerGotAway = distance > strikeRange + MeleeApproachRange + 80f;
                bool gaveUp = _approachTicks >= MeleeApproachMaxTicks || playerGotAway || pairedFlanker;

                if (inStrikeRange && !pairedFlanker)
                {
                    StartMove(_approachMove);
                }
                else if (gaveUp)
                {
                    ClearApproach();
                }

                return;
            }

            if (_neutralGap > 0)
            {
                _neutralGap--;
                return;
            }

            _bag ??= new AttackBag<WardenMove>(FillBag);

            // The flanker plays its own ranged cards from the shared deck on a slower clock; its signature move is the
            // fallback when none of them is eligible from where it stands.
            if (pairedFlanker)
            {
                if (_flankCooldown > 0)
                {
                    return;
                }

                bool drewFlankCard = _bag.TryDraw(move => IsFlankerCard(move) && IsMoveEligible(move, distance, rise, clearLine), out WardenMove flankMove);
                if (!drewFlankCard)
                {
                    flankMove = FlankerMove;
                }

                if (drewFlankCard || IsMoveEligible(FlankerMove, distance, rise, clearLine))
                {
                    _flankCooldown = FlankCooldownTicks;
                    StartMove(flankMove);
                }

                return;
            }

            if (Role == DuoRole.Paired && partner != null)
            {
                TryStartTandem(partner, target);
                if (Phase != AttackPhase.Idle && Phase != AttackPhase.CasualStroll && Phase != AttackPhase.ClosingDistance)
                {
                    return;
                }
            }

            if (_bag.TryDraw(move => IsMoveEligible(move, distance, rise, clearLine), out WardenMove drawn))
            {
                bool needsRunIn = ComboNameFor(drawn) != null && distance > MeleeStrikeRange(drawn);
                if (needsRunIn)
                {
                    _approachMove = drawn;
                    _approachTicks = 0;
                    NPC.netUpdate = true;
                    return;
                }

                StartMove(drawn);
            }
        }

        private void OnReturnedToNeutral(WardenPuppet partner)
        {
            ClearApproach();
            CurrentMove = WardenMove.None;
            _neutralGap = NeutralGapTicks;
            if (Role == DuoRole.Alone)
            {
                return;
            }

            if (HasPressToken && partner != null && Main.rand.Next(100) < TokenPassChancePercent)
            {
                PassToken(partner);
            }
        }

        private void PassToken(WardenPuppet partner)
        {
            HasPressToken = false;
            partner.HasPressToken = true;
            partner._tokenHeldTicks = 0;
            partner._tokenHoldLimit = Main.rand.Next(420, 541);
            NPC.netUpdate = true;
            partner.NPC.netUpdate = true;
        }

        private void TickPressToken(WardenPuppet partner, Player target)
        {
            if (Role != DuoRole.Paired || partner == null || !HasPressToken)
            {
                return;
            }

            _tokenHeldTicks++;
            float myDistance = NPC.Distance(target.Center);
            float partnerDistance = partner.NPC.Distance(target.Center);
            bool heldTooLong = _tokenHeldTicks >= _tokenHoldLimit;
            bool partnerMuchCloser = partnerDistance + TokenCloserPixels <= myDistance;
            bool partnerFree = partner.Phase == AttackPhase.Idle || partner.Phase == AttackPhase.CasualStroll || partner.Phase == AttackPhase.ClosingDistance;

            if ((heldTooLong || partnerMuchCloser) && partnerFree)
            {
                PassToken(partner);
            }
        }

        private void TickRegroup(WardenPuppet partner, Player target)
        {
            if (Role != DuoRole.Paired || partner == null)
            {
                _separationTicks = 0;
                return;
            }

            bool split = NPC.Distance(partner.NPC.Center) > RegroupDistance;
            if (!split)
            {
                _separationTicks = 0;
                return;
            }

            _separationTicks++;
            bool thisOneFarther = NPC.Distance(target.Center) >= partner.NPC.Distance(target.Center);
            if (_separationTicks >= RegroupTicks && thisOneFarther)
            {
                _separationTicks = 0;
                QueueMove(RegroupMove, 0);
            }
        }

        // A player passing from one side of the boss to the other at close range counts as a roll-through.
        private void TrackSideCrossings(Player target)
        {
            float gap = target.Center.X - NPC.Center.X;
            int side = Math.Sign(gap);
            bool crossed = side != 0 && _lastTargetSide != 0 && side != _lastTargetSide && Math.Abs(gap) <= ReactiveRange;
            if (crossed)
            {
                _crossCount++;
                _crossWindow = ReactiveWindowTicks;
            }

            if (side != 0)
            {
                _lastTargetSide = side;
            }
        }

        // ── Anti-air ─────────────────────────────────────────────────────────────
        private List<int> UpdateHighPlayers()
        {
            List<int> highPlayers = new List<int>();

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                bool eligible = player.active && !player.dead;
                bool high = eligible
                    && NPC.Bottom.Y - player.Bottom.Y >= AntiAirRisePixels
                    && Math.Abs(player.Center.X - NPC.Center.X) <= AntiAirMaxHorizontal;

                if (!high)
                {
                    _highTicks[i] = 0;
                    continue;
                }

                _highTicks[i]++;
                if (_highTicks[i] >= AntiAirTriggerTicks)
                {
                    highPlayers.Add(i);
                }
            }

            return highPlayers;
        }

        private bool TryStartAntiAir(List<int> highPlayers, WardenPuppet partner)
        {
            if (highPlayers.Count == 0 || _antiAirCooldown > 0)
            {
                return false;
            }

            bool tooSoonAfterPartner = partner != null && partner._lastAntiAirTick > 0
                && Main.GameUpdateCount < partner._lastAntiAirTick + AntiAirPartnerSpacing;
            if (tooSoonAfterPartner)
            {
                return false;
            }

            _antiAirCooldown = AntiAirCooldownTicks;
            _lastAntiAirTick = Main.GameUpdateCount;
            StartAntiAir(highPlayers);
            return true;
        }

        // ── Melee hooks ──────────────────────────────────────────────────────────
        protected override void OnMeleeComboAttackTick(MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            base.OnMeleeComboAttackTick(combo, step, elapsed, total);

            bool liveNow = step.HitWindowEnd <= 0f || elapsed < step.HitWindowEnd * total;
            if (liveNow)
            {
                MarkLive();
            }

            bool arcMotion = step.Motion == ComboMotion.OverheadArc || step.Motion == ComboMotion.UnderhandArc
                || step.Motion == ComboMotion.HorizontalSweep || step.Motion == ComboMotion.GroundSlam;
            if (elapsed == 0 && arcMotion && IsServerSide)
            {
                // The sweep comes from the authored arc endpoints, so underhand cuts run backwards.
                float arcStart = 0f;
                float arcEnd = 0f;
                ModifyMeleeArcEndpoints(step.Motion, ref arcStart, ref arcEnd);
                float sweep = (arcEnd - arcStart) * NPC.direction;
                float radius = ComboReachBase * 0.7f * step.ReachMult;

                VanillaSwordArcSettings settings = SwingArcSettings(sweep, radius, total);
                settings.Easing = VanillaSwordArcEasing.Weighted;
                settings.WeightedEaseInTicks = step.EaseInTicks;
                settings.WeightedEaseOutTicks = step.EaseOutTicks;
                settings.WeightedEaseOutDecay = step.EaseOutDecay;
                VanillaSwordArc.SpawnForNPC(NPC.GetSource_FromAI(), NPC, 0, 0f, Main.myPlayer, settings, Vector2.Zero, hostile: false);
            }
        }

        /// <summary>A tracked crescent riding the live weapon for swingTicks, in this boss's colours. sweep is world radians,
        /// signed (positive turns clockwise on screen). Callers pick the easing, spawn it on the server; the projectile syncs.</summary>
        protected VanillaSwordArcSettings SwingArcSettings(float sweep, float radius, int swingTicks)
        {
            return new VanillaSwordArcSettings
            {
                Texture = VanillaSwordArcTexture.Excalibur,
                Easing = VanillaSwordArcEasing.Linear,
                Duration = Math.Max(1, swingTicks),
                StartAngle = PuppetWeaponDirection.ToRotation(),
                SweepAngle = sweep,
                Radius = radius,
                Opacity = 0.7f,
                FadeInFraction = Math.Min(0.12f, 2f / swingTicks),
                FadeOutFraction = 0.35f,
                AfterimageLag = MathHelper.PiOver4,
                AfterimageOpacity = 0.55f,
                BodyOpacity = 0.8f,
                CoreOpacity = 0.3f,
                DrawTipSparkle = false,
                TintWithWorldLighting = false,
                DarkColor = SwingArcDarkColor,
                BodyColor = SwingArcBodyColor,
                CoreColor = SwingArcCoreColor,
                DustType = -1,
                DustCount = 0,
                TrackPuppetBlade = true,
                EnableCollision = false,
            };
        }

        protected override void DoMeleeAttack()
        {
            TryMeleeHit();
        }

        protected override void DoStabAttack()
        {
            TryMeleeHit();
        }

        protected override void DoRangedAttack()
        {
        }

        // ── Cutscene visuals ─────────────────────────────────────────────────────
        // The soul stream: gold and electric motes spiral from the fallen body into the raised weapon.
        private void TickChannelVisuals()
        {
            WardenPuppet partner = Partner;
            if (Main.dedServ || !ScriptChannel || partner == null)
            {
                return;
            }

            Vector2 from = partner.NPC.Center;
            Vector2 to = WeaponPoint(ComboReachBase * 0.7f);

            for (int i = 0; i < 3; i++)
            {
                float progress = Main.rand.NextFloat();
                Vector2 point = Vector2.Lerp(from, to, progress);
                float spiral = (float)Math.Sin(progress * 12f + Main.GameUpdateCount * 0.2f) * 18f * (1f - progress);
                point += new Vector2(0f, spiral);

                int dustType = DustID.AncientLight;
                if (i == 0)
                {
                    dustType = DustID.Electric;
                }

                Dust stream = Dust.NewDustPerfect(point, dustType, (to - from).SafeNormalize(Vector2.Zero) * 3f, 40, default, 1f);
                stream.noGravity = true;
            }
        }

        // ── Phase change ─────────────────────────────────────────────────────────
        /// <summary>Server: swaps in the phase-two life pool using the difficulty ratio already applied to phase one.</summary>
        public int ComputeEmpoweredLifeMax()
        {
            float difficultyScale = NPC.lifeMax / (float)PairedAuthoredLife;
            return (int)Math.Round(EmpoweredAuthoredLife * difficultyScale);
        }

        public void BecomeEmpowered()
        {
            Empowered = true;
            GrowthProgress = 255;
            _bag?.Clear();

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.PoiseMax = EmpoweredPoise;
            NPC.knockBackResist = EmpoweredKnockbackResist;
            NPC.netUpdate = true;
        }

        public void ReleaseFromCutscene()
        {
            CutsceneHold = false;
            ScriptRaiseWeapon = false;
            ScriptChannel = false;
            ScriptGrow = false;
            ScriptWalkX = float.NaN;
            ScriptFaceX = float.NaN;
            ScriptHiddenTarget = 0;
            _entryGrace = NeutralEntryGrace;
            _wasNeutral = false;
            CurrentMove = WardenMove.None;
            PhaseTimer = 1;
            NPC.netUpdate = true;
            RequestNetworkSnapshot();
        }

        // ── Death handoff ────────────────────────────────────────────────────────
        public override bool CheckDead()
        {
            if (!IsServerSide)
            {
                return base.CheckDead();
            }

            WardenPuppet partner = Partner;
            bool firstOfPair = Role == DuoRole.Paired && partner != null && partner.Role == DuoRole.Paired;
            if (!firstOfPair)
            {
                return base.CheckDead();
            }

            NPC.life = 1;
            NPC.dontTakeDamage = true;
            Role = DuoRole.Fallen;
            partner.Role = DuoRole.Empowering;
            OrnsmoughDirector.BeginEmpowerment(partner, this);
            NPC.netUpdate = true;
            partner.NPC.netUpdate = true;
            return false;
        }

        public override bool CheckActive()
        {
            return false;
        }

        // Only the final Warden ever reaches OnKill: the fallen partner is removed without a strike.
        public override void OnKill()
        {
            if (!IsServerSide)
            {
                return;
            }

            NPC.SetEventFlagCleared(ref NPC.downedGolemBoss, GameEventClearedID.DefeatedGolem);

            int[] slainTypes = { ModContent.NPCType<Ornstein>(), ModContent.NPCType<Smough>(), NPCID.Golem };
            foreach (int slainType in slainTypes)
            {
                tsorcRevampWorld.NewSlain.TryAdd(new NPCDefinition(slainType), 1);
            }

            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.EmpressOfLight.Forcefield"), Color.Cyan);
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            int soulCount = OrnsmoughBag.SoulsOfEachWarden;
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<OrnsmoughBag>()));

            LeadingConditionRule normalMode = new LeadingConditionRule(new Conditions.NotExpert());
            normalMode.OnSuccess(ItemDropRule.Common(ModContent.ItemType<SoulOfOrnstein>(), 1, soulCount, soulCount));
            normalMode.OnSuccess(ItemDropRule.Common(ModContent.ItemType<SoulOfSmough>(), 1, soulCount, soulCount));
            normalMode.OnSuccess(ItemDropRule.Common(ModContent.ItemType<CrestOfStone>(), 1, 2, 2));
            normalMode.OnSuccess(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.AdventureModeRule, ModContent.ItemType<BrokenPicksaw>()));
            npcLoot.Add(normalMode);

            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<RadiantLifegemItem>(), 1, 2, 4));
        }

        // ── Shared defaults and sync ─────────────────────────────────────────────
        protected void ApplySharedDefaults()
        {
            NPC.width = 30;
            NPC.height = 40;
            NPC.npcSlots = 10f;
            NPC.aiStyle = -1;
            NPC.damage = 0;
            NPC.knockBackResist = 0f;
            NPC.boss = true;
            NPC.rarity = 26;
            NPC.value = 293650f;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.lavaImmune = true;

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.NavSearchRadius = 80;
            globalNPC.NavGiveUpTicks = 180;
            globalNPC.CanUseRopes = true;
            globalNPC.Agility = 0f;
            globalNPC.PoiseStaggerResetsAI = true;
            globalNPC.CanTeleport = true;
            globalNPC.TeleportStyle = TeleportStyle.RecoveryOnly;
        }

        protected void ApplySharedStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.CursedInferno] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Venom] = true;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((short)PartnerWhoAmI);
            writer.Write((byte)Role);
            writer.Write(CutsceneHold);
            writer.Write(HasPressToken);
            writer.Write((byte)CurrentMove);
            writer.Write(Empowered);
            writer.Write(GrowthProgress);
            writer.Write(ScriptRaiseWeapon);
            writer.Write(ScriptChannel);
            writer.Write(ScriptGrow);
            writer.Write(ScriptWalkX);
            writer.Write(ScriptFaceX);
            writer.Write(ScriptHiddenTarget);
            writer.Write(QuakeArmed);
            writer.Write(StartsHidden);
            writer.Write(MoveStage);
            writer.Write((short)Math.Clamp(MoveTicks, 0, short.MaxValue));
            writer.WriteVector2(MoveAim);
            writer.WriteVector2(MovePoint);
            writer.WriteVector2(MoveStart);
            writer.Write((sbyte)MoveDirection);
            writer.Write(MoveFlag);
            writer.Write((byte)_approachMove);
            writer.Write(NPC.lifeMax);
            base.SendExtraAI(writer);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            PartnerWhoAmI = reader.ReadInt16();
            Role = (DuoRole)reader.ReadByte();
            CutsceneHold = reader.ReadBoolean();
            HasPressToken = reader.ReadBoolean();
            CurrentMove = (WardenMove)reader.ReadByte();
            Empowered = reader.ReadBoolean();
            GrowthProgress = reader.ReadByte();
            ScriptRaiseWeapon = reader.ReadBoolean();
            ScriptChannel = reader.ReadBoolean();
            ScriptGrow = reader.ReadBoolean();
            ScriptWalkX = reader.ReadSingle();
            ScriptFaceX = reader.ReadSingle();
            ScriptHiddenTarget = reader.ReadByte();
            QuakeArmed = reader.ReadBoolean();
            StartsHidden = reader.ReadBoolean();

            // A new stage adopts the server's clock; the same stage keeps this machine's running clock.
            byte stage = reader.ReadByte();
            int stageTicks = reader.ReadInt16();
            if (stage != MoveStage)
            {
                MoveStage = stage;
                MoveTicks = stageTicks;
            }

            MoveAim = reader.ReadVector2();
            MovePoint = reader.ReadVector2();
            MoveStart = reader.ReadVector2();
            MoveDirection = reader.ReadSByte();
            MoveFlag = reader.ReadBoolean();
            _approachMove = (WardenMove)reader.ReadByte();
            NPC.lifeMax = reader.ReadInt32();
            base.ReceiveExtraAI(reader);
        }
    }

    /// <summary>
    /// Server-side scripts of the encounter: swaps every Golem summon for the intro, drives the intro and the empowerment
    /// scene. Actors are ordinary synced NPCs, so clients only need the cutscene packet and the NPCs' own state.
    /// </summary>
    public class OrnsmoughDirector : ModSystem
    {
        private enum Script
        {
            None,
            Intro,
            Entrance,
            Empower,
        }

        private const string IntroScene = "OrnsmoughIntro";
        private const string EntranceScene = "OrnsmoughEntrance";
        private const string EmpowerScene = "OrnsmoughEmpower";
        private const int IntroTicks = 312;
        private const int EntranceTicks = 120;
        private const int EmpowerTicks = 300;
        private const float GolemDistance = 24f * 16f;
        private const float DuoPastGolem = 18f * 16f;
        private const float EntranceDistance = 22f * 16f;
        private const int RunwayTilesNeeded = 44;
        private const int HiddenFully = 255;
        private const int FallenDimmedHidden = 102; // 60% alpha at the start of the scene

        private static Script activeScript;
        private static int scriptTick;
        private static int golemIndex = -1;
        private static int ornsteinIndex = -1;
        private static int smoughIndex = -1;
        private static int survivorIndex = -1;
        private static int fallenIndex = -1;
        private static int empowerStartLife;
        private static int empowerTargetLife;
        private static Vector2 duoPoint;
        private static float floorY;
        private static int enterSide = 1;

        /// <summary>True while the duo, the cutscene Golem or one of their scenes is alive (Lihzahrd debuff exemption).</summary>
        public static bool EncounterActive
        {
            get
            {
                if (activeScript != Script.None)
                {
                    return true;
                }

                int ornsteinType = ModContent.NPCType<Ornstein>();
                int smoughType = ModContent.NPCType<Smough>();
                int golemType = ModContent.NPCType<CutsceneGolem>();
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    bool partOfEncounter = npc.active && (npc.type == ornsteinType || npc.type == smoughType || npc.type == golemType);
                    if (partOfEncounter)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public override void Load()
        {
            On_NPC.SpawnOnPlayer += SwapGolemSummon;
        }

        public override void Unload()
        {
            On_NPC.SpawnOnPlayer -= SwapGolemSummon;
        }

        public override void OnWorldLoad()
        {
            ResetScript();
        }

        public override void OnWorldUnload()
        {
            ResetScript();
        }

        private static void ResetScript()
        {
            activeScript = Script.None;
            scriptTick = 0;
            golemIndex = -1;
            ornsteinIndex = -1;
            smoughIndex = -1;
            survivorIndex = -1;
            fallenIndex = -1;
        }

        // Altar (SP and message 61), Essence of Terraria and the rematch selector all arrive here.
        private static void SwapGolemSummon(On_NPC.orig_SpawnOnPlayer orig, int playerIndex, int npcType)
        {
            bool swap = npcType == NPCID.Golem && Main.netMode != NetmodeID.MultiplayerClient;
            if (!swap)
            {
                orig(playerIndex, npcType);
                return;
            }

            if (EncounterActive)
            {
                return;
            }

            Player player = Main.player[playerIndex];
            enterSide = ChooseEntrySide(player);

            if (tsorcRevampWorld.OrnsmoughIntroSeen)
            {
                BeginEntrance(player);
            }
            else
            {
                tsorcRevampWorld.OrnsmoughIntroSeen = true;
                BeginIntro(player);
            }
        }

        // The Wardens come from the right whenever the room allows it; a wall close on that side flips the staging.
        private static int ChooseEntrySide(Player player)
        {
            int startTileX = (int)(player.Center.X / 16f);
            int tileY = (int)(player.Center.Y / 16f);

            for (int offset = 1; offset <= RunwayTilesNeeded; offset++)
            {
                int tileX = Utils.Clamp(startTileX + offset, 1, Main.maxTilesX - 2);
                if (WorldGen.SolidTile(tileX, tileY))
                {
                    return -1;
                }
            }

            return 1;
        }

        private static float FindFloorBelow(float worldX, float startY)
        {
            int tileX = Utils.Clamp((int)(worldX / 16f), 1, Main.maxTilesX - 2);
            int tileY = Utils.Clamp((int)(startY / 16f), 5, Main.maxTilesY - 10);

            for (int climb = 0; climb < 30 && WorldGen.SolidTile(tileX, tileY); climb++)
            {
                tileY--;
            }

            for (int drop = 0; drop < 80 && !WorldGen.SolidTile(tileX, tileY + 1); drop++)
            {
                tileY++;
            }

            return (tileY + 1) * 16f;
        }

        private static void BeginIntro(Player player)
        {
            float golemX = player.Center.X + enterSide * GolemDistance;
            floorY = FindFloorBelow(golemX, player.Bottom.Y);
            float duoX = golemX + enterSide * DuoPastGolem;
            duoPoint = new Vector2(duoX, FindFloorBelow(duoX, floorY - 32f));

            IEntitySource source = new EntitySource_BossSpawn(player);
            golemIndex = NPC.NewNPC(source, (int)golemX, (int)floorY, ModContent.NPCType<CutsceneGolem>(), 0, 0f, 0f, 0f, 0f, player.whoAmI);

            activeScript = Script.Intro;
            scriptTick = 0;

            Vector2 cameraPoint = (player.Center + new Vector2(golemX, floorY - 70f)) * 0.5f;
            CutsceneSystem.Start(IntroScene, IntroTicks, CutsceneFocus.AtPoint(cameraPoint));
        }

        private static void BeginEntrance(Player player)
        {
            float duoX = player.Center.X + enterSide * EntranceDistance;
            duoPoint = new Vector2(duoX, FindFloorBelow(duoX, player.Bottom.Y));

            activeScript = Script.Entrance;
            scriptTick = 0;

            Vector2 cameraPoint = (player.Center + duoPoint) * 0.5f;
            CutsceneSystem.Start(EntranceScene, EntranceTicks, CutsceneFocus.AtPoint(cameraPoint));
            SpawnWardens(player);
        }

        private static void SpawnWardens(Player player)
        {
            IEntitySource source = new EntitySource_BossSpawn(player);
            WardenLightning.Spawn(source, duoPoint + new Vector2(-26f, -24f), Vector2.Zero, WardenBoltMode.Burst, 0, 1f, 3f);
            WardenLightning.Spawn(source, duoPoint + new Vector2(enterSide * 40f, -24f), Vector2.Zero, WardenBoltMode.Burst, 0, 1f, 0f);

            ornsteinIndex = NPC.NewNPC(source, (int)(duoPoint.X - enterSide * 26f), (int)duoPoint.Y, ModContent.NPCType<Ornstein>(), 0, 0f, 0f, 0f, 0f, player.whoAmI);
            smoughIndex = NPC.NewNPC(source, (int)(duoPoint.X + enterSide * 40f), (int)duoPoint.Y, ModContent.NPCType<Smough>(), 0, 0f, 0f, 0f, 0f, player.whoAmI);

            WardenPuppet ornstein = (WardenPuppet)Main.npc[ornsteinIndex].ModNPC;
            WardenPuppet smough = (WardenPuppet)Main.npc[smoughIndex].ModNPC;
            ornstein.PartnerWhoAmI = smoughIndex;
            smough.PartnerWhoAmI = ornsteinIndex;
            smough.HasPressToken = true;

            foreach (WardenPuppet warden in new[] { ornstein, smough })
            {
                warden.CutsceneHold = true;
                warden.ScriptFaceX = player.Center.X;
                warden.NPC.netUpdate = true;
            }

            ornstein.StartsHidden = true;
            smough.StartsHidden = true;
        }

        public static void BeginEmpowerment(WardenPuppet survivor, WardenPuppet fallen)
        {
            survivorIndex = survivor.NPC.whoAmI;
            fallenIndex = fallen.NPC.whoAmI;
            activeScript = Script.Empower;
            scriptTick = 0;

            foreach (WardenPuppet warden in new[] { survivor, fallen })
            {
                // Clearing the move lets AI() replace whatever attack was running with the cutscene hold.
                warden.CutsceneHold = true;
                warden.CurrentMove = WardenMove.None;
                warden.ScriptFaceX = float.NaN;
                warden.QuakeArmed = false;
                warden.NPC.netUpdate = true;
            }

            int approachSide = Math.Sign(survivor.NPC.Center.X - fallen.NPC.Center.X);
            if (approachSide == 0)
            {
                approachSide = 1;
            }

            survivor.ScriptWalkX = fallen.NPC.Center.X + approachSide * 48f;
            survivor.ScriptFaceX = fallen.NPC.Center.X;
            fallen.ScriptHiddenTarget = FallenDimmedHidden;

            CutsceneSystem.Start(EmpowerScene, EmpowerTicks, CutsceneFocus.BetweenNpcs(survivorIndex, fallenIndex), survivorIndex);
        }

        public override void PostUpdateNPCs()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || activeScript == Script.None)
            {
                return;
            }

            scriptTick++;

            switch (activeScript)
            {
                case Script.Intro:
                    TickIntro();
                    break;
                case Script.Entrance:
                    TickEntrance();
                    break;
                case Script.Empower:
                    TickEmpower();
                    break;
            }
        }

        private static WardenPuppet WardenAt(int index)
        {
            if (index < 0 || index >= Main.maxNPCs || !Main.npc[index].active)
            {
                return null;
            }

            return Main.npc[index].ModNPC as WardenPuppet;
        }

        private static Player NearestPlayer(Vector2 point)
        {
            int index = Player.FindClosest(point - new Vector2(1f), 2, 2);
            return Main.player[index];
        }

        private void TickIntro()
        {
            const int BurstTick = 150;
            const int SkewerTellTick = 175;
            const int ShatterTick = 215;
            const int SettleTick = 240;

            NPC golem = null;
            if (golemIndex >= 0 && Main.npc[golemIndex].active && Main.npc[golemIndex].type == ModContent.NPCType<CutsceneGolem>())
            {
                golem = Main.npc[golemIndex];
            }

            if (scriptTick == BurstTick)
            {
                SpawnWardens(NearestPlayer(duoPoint));
            }

            Ornstein ornstein = WardenAt(ornsteinIndex) as Ornstein;
            WardenPuppet smough = WardenAt(smoughIndex);

            if (scriptTick == SkewerTellTick && ornstein != null && golem != null)
            {
                ornstein.StartScriptedSkewer(golem.Center);
            }

            if (scriptTick == ShatterTick && golem != null)
            {
                float direction = -enterSide;
                WardenLightning.Spawn(golem.GetSource_FromAI(), golem.Center, Vector2.Zero, WardenBoltMode.GolemShatter, 0, direction);
                golem.active = false;
                golem.life = 0;
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, golem.whoAmI);
                }
            }

            if (scriptTick == SettleTick && ornstein != null && smough != null)
            {
                Player viewer = NearestPlayer(ornstein.NPC.Center);
                smough.ScriptWalkX = ornstein.NPC.Center.X + enterSide * 56f;
                smough.ScriptFaceX = viewer.Center.X;
                ornstein.ScriptFaceX = viewer.Center.X;
                smough.NPC.netUpdate = true;
                ornstein.NPC.netUpdate = true;
            }

            if (scriptTick >= IntroTicks)
            {
                FinishOpening();
            }
        }

        private void TickEntrance()
        {
            if (scriptTick >= EntranceTicks)
            {
                FinishOpening();
            }
        }

        private static void FinishOpening()
        {
            WardenAt(ornsteinIndex)?.ReleaseFromCutscene();
            WardenAt(smoughIndex)?.ReleaseFromCutscene();
            ResetScript();
        }

        private void TickEmpower()
        {
            const int RaiseTick = 60;
            const int AbsorbTick = 180;
            const int GrowthEndTick = 240;

            WardenPuppet survivor = WardenAt(survivorIndex);
            WardenPuppet fallen = WardenAt(fallenIndex);

            if (survivor == null)
            {
                CutsceneSystem.Stop(EmpowerScene);
                ResetScript();
                return;
            }

            if (scriptTick == RaiseTick)
            {
                survivor.ScriptRaiseWeapon = true;
                survivor.ScriptChannel = true;
                survivor.NPC.netUpdate = true;
                if (fallen != null)
                {
                    fallen.ScriptHiddenTarget = HiddenFully;
                    fallen.NPC.netUpdate = true;
                }
            }

            if (scriptTick >= RaiseTick && scriptTick < AbsorbTick && (scriptTick - RaiseTick) % 30 == 0)
            {
                float pitch = (scriptTick - RaiseTick) / 120f - 0.4f;
                PlayChime(survivor.NPC.Center, pitch);
            }

            if (scriptTick == AbsorbTick)
            {
                AbsorbFallenPartner(survivor, fallen);
            }

            if (scriptTick > AbsorbTick && scriptTick <= GrowthEndTick)
            {
                float progress = (scriptTick - AbsorbTick) / (float)(GrowthEndTick - AbsorbTick);
                survivor.NPC.life = (int)MathHelper.Lerp(empowerStartLife, empowerTargetLife, progress);
                if (scriptTick % 6 == 0)
                {
                    survivor.NPC.netUpdate = true;
                }
            }

            if (scriptTick == GrowthEndTick)
            {
                survivor.BecomeEmpowered();
                survivor.ScriptGrow = false;
            }

            if (scriptTick >= EmpowerTicks)
            {
                survivor.Role = DuoRole.Alone;
                survivor.PartnerWhoAmI = -1;
                survivor.ReleaseFromCutscene();
                ResetScript();
            }
        }

        private static void PlayChime(Vector2 position, float pitch)
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.7f, Pitch = pitch }, position);
            }
        }

        // The fallen Warden is deactivated, never struck: no OnKill, no loot, no second death message.
        private static void AbsorbFallenPartner(WardenPuppet survivor, WardenPuppet fallen)
        {
            survivor.ScriptChannel = false;
            survivor.ScriptGrow = true;

            empowerStartLife = survivor.NPC.life;
            empowerTargetLife = survivor.ComputeEmpoweredLifeMax();
            survivor.NPC.lifeMax = empowerTargetLife;
            survivor.NPC.netUpdate = true;

            if (fallen == null)
            {
                return;
            }

            NPC fallenNpc = fallen.NPC;
            WardenLightning.Spawn(fallenNpc.GetSource_FromAI(), fallenNpc.Center, Vector2.Zero, WardenBoltMode.Burst, 0, 0f, 10f);
            fallenNpc.active = false;
            fallenNpc.life = 0;
            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, fallenNpc.whoAmI);
            }
        }
    }
}
