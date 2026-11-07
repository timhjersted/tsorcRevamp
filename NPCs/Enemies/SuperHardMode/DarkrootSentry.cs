using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Content.Items;
using tsorcRevamp.Content.Items.Accessories.Defensive.Shields;
using tsorcRevamp.Content.Items.Armor.DarkrootSentrySet;
using tsorcRevamp.Content.Items.Materials.Titanite;
using tsorcRevamp.Content.Items.Weapons.Enemy;
using tsorcRevamp.Content.Items.Weapons.Melee.Halberds;
using tsorcRevamp.Content.Projectiles.Enemy.DarkrootSentrySpells;
using tsorcRevamp.NPCs.Puppets;
using tsorcRevamp.Systems;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    /// <summary>
    /// Darkroot Sentry: a forest oathkeeper with a halberd and the Dragon Crest Shield. Paces its post, raises a
    /// frontal guard while closing, and fights with three halberd moves and three living-wood spells.
    ///
    /// Loop: guarded approach -> visible commitment -> halberd or wood attack -> exposed recovery.
    /// One server-side AttackBag picks the next action; every action is one owner clock:
    ///   Briar Hew (+ Oathkeeper form below half)   legacy MeleeCombo (Weighted arc clock), pierce as a Custom phase
    ///   Sentry's Measure, Rootbound Advance         Custom phase, axial thrust (spear grip slides along the shaft)
    ///   Thorncast, Rootwake, Briar Lash            Custom phase casts that own DarkrootThorn / Root / Vine hazards
    /// Multiplayer: picks, locks, root floor points, lash paths and releases are server decisions, synced in
    /// Send/ReceiveExtraAI after the base payload; hazards carry owner index + encounter serial.
    /// </summary>
    [LegacyName("OolacileKnight")]
    public class DarkrootSentry : PuppetNPC, IHumanoidMeleeHitEffects
    {
        // ── Damage: declared Expert damage before defense, converted once by EnemyDamage.Projectile ──────────
        public const int HewExpertDamage = 260;
        public const int UnderhandExpertDamage = 280;
        public const int PierceExpertDamage = 300;
        public const int MeasureExpertDamage = 256;
        public const int AdvanceExpertDamage = 300;
        public const int ThornExpertDamage = 224;
        public const int RootExpertDamage = 280;
        public const int LashExpertDamage = 272;

        const string HewName = "Briar Hew";
        const string OathName = "Oathkeeper";

        // ── Halberd geometry, measured from the 74x74 supplied art (shared with the player Darkroot Halberd) ──
        // Point (70.5, 1.5), grip (12.5, 61.5), butt (1.5, 72.5), axe neck (49.5, 24.5). Point->butt = 99.0 texels.
        // Grip fractions are measured from the point toward the butt (the base spear-grip convention).
        // Carry 0.843 = 83.45 texels = 96.0px at draw scale 1.15. Extended 0.966 = +14px of reach (~110px).
        // Cocked 0.791 pulls the head back 6px for the thrust tells. The axe head spans the last 31.1 texels = 35.8px.
        public const float HalberdDrawScale = 1.15f;
        static readonly Vector2 HalberdButt = new Vector2(1.5f, 72.5f);
        public static readonly float ShaftTexels = (DarkrootHalberdMotion.Point - HalberdButt).Length();
        public static readonly float CarryGrip = (DarkrootHalberdMotion.Point - DarkrootHalberdMotion.Grip).Length() / ShaftTexels;
        public static readonly float ExtendedGrip = CarryGrip + 14f / HalberdDrawScale / ShaftTexels;
        static readonly float CockedGrip = CarryGrip - 6f / HalberdDrawScale / ShaftTexels;
        static readonly float HeadLength = (DarkrootHalberdMotion.Point - DarkrootHalberdMotion.Neck).Length() * HalberdDrawScale;

        // Swing-space poses: world angle = rotation - 45 degrees (facing right). Level = pi/4.
        const float CarryRotation = -0.30f;
        const float LevelRotation = MathHelper.PiOver4;
        const float CockedRotation = -1.55f;
        const float LowRotation = 2.36f;
        const float RootwakeLowered = MathHelper.PiOver4 + 0.95f;
        const float ThrustPitchLimit = 0.35f;
        const float CastPitchLimit = 1.1f;
        const float LashPitchLimit = 0.6f;

        static readonly Color Accent = new Color(132, 186, 92);

        enum Move : byte { None, Hew, Measure, Advance, Thorncast, Rootwake, Lash }
        enum CustomAction : byte { None, Measure, Advance, Thorncast, Rootwake, Lash, Pierce }

        // ── Synced state (SendExtraAI / ReceiveExtraAI, in this order) ─────────────────────────────────────
        int _serial;
        bool _phaseTwo;
        Move _move;
        CustomAction _custom;
        int _actionTotal;
        bool _abortRecovery;
        float _abortRotation;
        float _abortGrip;
        int _lockedTarget = -1;
        int _lockFacing = 1;
        float _pitch;
        Vector2 _thornAim = Vector2.UnitX;
        int _fanCount = 1;
        int _releasedMask;
        float _advanceDistance;
        readonly Vector2[] _lashControls = new Vector2[4];
        // Every machine, refreshed in AI(): facing-space angle from the back shoulder to a player one of this Sentry's
        // vines is holding, or null. The free arm directs the earthen snare toward its captive.
        float? _tetherArmAngle;
        bool _guardUp;
        int _guardLift;
        int _guardTicks;
        int _guardCooldown;
        int _guardFacing = 1;
        int _restTicks;
        int _graceTicks;

        // ── Server-only selection state (never synced; clients follow the chosen action) ─────────────────
        static int _nextSerial;
        AttackBag<Move> _bag;
        readonly Dictionary<Move, int> _cooldowns = new Dictionary<Move, int>();
        Move _lastMove;
        int _age;
        int _completedActions;
        int _neutralTicks;
        int _rootRetryTicks;
        bool _pendingPierce;
        float _advanceStartBottomY;

        // ── Per-tick, recomputed on every machine ─────────────────────────────────────────────────────
        bool _customLive;
        bool _customArmed;
        int _customExpertDamage;

        public int EncounterSerial => _serial;
        bool Server => Main.netMode != NetmodeID.MultiplayerClient;
        bool Neutral => Phase == AttackPhase.Idle || Phase == AttackPhase.CasualStroll || Phase == AttackPhase.ClosingDistance;
        int CustomElapsed => _actionTotal - PhaseTimer;
        bool ThrustPose => Phase == AttackPhase.Custom
            && (_custom == CustomAction.Measure || _custom == CustomAction.Advance || _custom == CustomAction.Pierce);

        // ── Puppet configuration ─────────────────────────────────────────────────────────────────────────
        protected override string InvaderTitle => "Darkroot Sentry";
        protected override bool AnnounceInvasion => false;
        protected override bool AnnounceInvaderDefeat => false;
        public override bool UsesPuppetDifficultyScaling => false;
        protected override bool DespawnsOnPartyWipe => false;
        protected override int CasualStrollChance => 0;
        protected override int EstusChargesMax => 0;
        protected override int HeadArmorItemType => ModContent.ItemType<DarkrootSentryHelmet>();
        protected override int BodyArmorItemType => ModContent.ItemType<DarkrootSentryBreastplate>();
        protected override int LegsArmorItemType => ModContent.ItemType<DarkrootSentryGreaves>();
        protected override Color PuppetSkinColor => Color.Black;
        protected override int MeleeWeaponItemType => ModContent.ItemType<EnemyDarkrootHalberd>();
        protected override int RangedWeaponItemType => -1;
        protected override int MeleeDamage => EnemyDamage.Projectile(HewExpertDamage);
        protected override int RangedDamage => 0;
        protected override bool HoldAttackSelection => true;
        protected override bool ShowWeaponDuringNeutral => true;
        // Carry the halberd in the fist (composite arm at the carry angle the Hew tell starts from) whenever it is out,
        // including the run-in to an attack, instead of floating off the hand during walks and jumps.
        protected override bool HoldNeutralWeaponOnCompositeArm => true;
        protected override bool ShowMeleeWeaponWhileClosingDistance => true;
        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Halberd;
        protected override MeleeCombo[] MeleeComboPoolOverride => Combos;

        // Every pose draws the halberd on the spear path so a thrust can slide the grip along the shaft; cuts and
        // carry hold the measured carry grip, so the cut and thrust draws are the same picture at the same angle.
        protected override bool DrawWeaponAsSpear => true;
        protected override bool MirrorSpearRotationByFacing => true;
        protected override string SpearDrawTexturePath => UsefulFunctions.RefactorableFilepath(typeof(DarkrootHalberd));
        protected override Vector2 SpearTextureSize => new Vector2(74f, 74f);
        protected override Vector2 SpearHeadNorm => DarkrootHalberdMotion.Point / 74f;
        protected override Vector2 SpearBaseNorm => HalberdButt / 74f;
        // The art's shaft sits at -46.0 degrees; this turns it onto the -45 degree swing-space convention.
        protected override float SpearDrawRotationOffset => -MathHelper.PiOver4 - DarkrootHalberdMotion.NativeAngle;
        protected override Vector2 MeleeHandleNorm => DarkrootHalberdMotion.Grip / 74f;
        protected override float MeleeWeaponDrawScale => HalberdDrawScale;
        protected override float ComboReachBase => 137.1f;
        protected override float MeleeBladeWidth => 12f;
        protected override float MeleeRange => 112f;
        protected override float MeleeEngageRange => 112f;
        protected override float ComboMaxStartRange => 160f;
        protected override float RangedStartComboMaxRange => 160f;

        protected override bool UseAuthoredComboSwingClock => true;
        protected override bool UseLogicalMeleeTelegraphs => true;
        protected override bool UseSwingEasing => true;
        protected override bool UseCompositeArmSwing => true;
        protected override bool MirrorMeleeSwingRotationByFacing => true;
        protected override bool UseAimAdaptiveArc => true;
        protected override bool UseCompositeArmForAdditionalPhase => Phase == AttackPhase.Custom || Phase == AttackPhase.MeleeComboTelegraph;
        protected override float ComboTelegraphMultiplier => 1f;
        protected override int MinComboTelegraphTicks => 20;
        protected override int PhaseTelegraphMinimumTicks => 20;
        protected override int MeleeComboInterStepLingerTicks => 3;
        protected override int MeleeRecoveryLingerTicks => 6;
        // Facing commits at t28 of the 40t tell.
        protected override int TelegraphFacingCommitTicks => 12;

        const int HewTellTicks = 40;

        /// <summary>Hew tell pose: the halberd rises straight back from carry (-0.30) to the cocked pose (-1.55 plus the
        /// combo's synced aim bias) over 26t, SmoothStep, then holds 14t. Replaces the base logical windup, which dips the
        /// head down through the front first and reads like a swing. Ends exactly on the strike's start: no handoff
        /// jump. Public for SwingPreview, which applies the same curve.</summary>
        public static float HewTellRotation(int elapsed, float aimBias)
        {
            float raise = MathHelper.Clamp(elapsed / 26f, 0f, 1f);
            return MathHelper.SmoothStep(CarryRotation, CockedRotation + aimBias, raise);
        }

        protected override void ModifyAdditionalPhaseWeaponRotation(ref float rotation)
        {
            if (Phase == AttackPhase.MeleeComboTelegraph)
            {
                rotation = HewTellRotation(HewTellTicks - PhaseTimer, ComboAimBias);
            }
        }
        protected override Color SlashVFXColor => Accent;
        protected override float SlashVFXOpacity => 0.4f;
        protected override bool SuppressSlashVFXForCurrentPhase => Phase == AttackPhase.Custom;

        protected override float TopSpeed => 1.9f;
        protected override float Acceleration => 0.08f;
        protected override float PuppetJumpPower => 9f;
        protected override float PuppetJumpBoost => 5f;

        protected override int ShieldItemType => ModContent.ItemType<DragonCrestShield>();
        protected override string ShieldDrawTexturePath => UsefulFunctions.RefactorableFilepath(typeof(DragonCrestShield)) + "_Shield";
        protected override bool ShieldVisible => _guardUp && Neutral;
        // The 10t lift shows the shield lowered first, so the raise is visible before it starts blocking.
        protected override bool ShieldRaisedForPose => ShieldVisible && _guardLift == 0;
        protected override float? FreeBackArmFacingAngle
        {
            get
            {
                if (ShieldVisible)
                {
                    return 0f;
                }

                // A held Briar Lash victim: the casting hand keeps pointing toward the earth-bound victim while the
                // halberd is free to punish. Found every tick in AI() on every machine.
                if (_tetherArmAngle is float tetherAngle)
                {
                    return tetherAngle;
                }

                bool lashing = Phase == AttackPhase.Custom && _custom == CustomAction.Lash && !_abortRecovery;
                if (!lashing)
                {
                    return null;
                }

                // Lash cast: raise up-and-back, hold cocked, whip overhand to the aim, hold while the vine is out,
                // then let the arm fall back to the side over 14t.
                int elapsed = CustomElapsed;
                if (elapsed < LashCockTicks)
                {
                    return MathHelper.SmoothStep(ArmHangingAngle, LashCockAngle, elapsed / (float)LashCockTicks);
                }

                if (elapsed < LashWhipStart)
                {
                    return LashCockAngle;
                }

                if (elapsed < LashRelease)
                {
                    float whip = (elapsed - LashWhipStart) / (float)(LashRelease - LashWhipStart);
                    return MathHelper.SmoothStep(LashCockAngle, _pitch, whip);
                }

                if (elapsed < LashVineEnd)
                {
                    return _pitch;
                }

                float lower = MathHelper.Clamp((elapsed - LashVineEnd) / 14f, 0f, 1f);
                return MathHelper.SmoothStep(_pitch, ArmHangingAngle, lower);
            }
        }

        protected override bool SlowDownDuringCustom => _custom != CustomAction.Advance && _custom != CustomAction.Pierce;
        // Hyper-armor only while the Advance body is travelling and its point is out: advance ticks [0, 30).
        protected override bool CustomAttackCommitted => _custom == CustomAction.Advance && !_abortRecovery
            && CustomElapsed >= AdvanceTell && CustomElapsed < AdvanceTell + AdvanceRetractStart;

        protected override float? CustomWeaponRotation
        {
            get
            {
                if (_custom == CustomAction.None)
                {
                    return null;
                }

                ResolveCustomPose(out float rotation, out _);
                return rotation;
            }
        }

        protected override float? OverrideSpearGrip(float swingClockT)
        {
            if (Phase != AttackPhase.Custom || _custom == CustomAction.None)
            {
                return CarryGrip;
            }

            ResolveCustomPose(out _, out float grip);
            return grip;
        }

        protected override void ModifyMeleeArcEndpoints(ComboMotion motion, ref float start, ref float end)
        {
            if (motion == ComboMotion.OverheadArc)
            {
                start = CockedRotation;
                end = LowRotation;
            }

            if (motion == ComboMotion.UnderhandArc)
            {
                start = LowRotation;
                end = CockedRotation;
            }
        }

        // Cuts: the swept capsule runs from 24px out from the hand to the drawn point - the axe head and upper shaft,
        // never the hand pocket. Thrusts: only the head (neck to point), so the point is what connects.
        protected override void ModifyFrontBladeCapsule(ref Vector2 origin, ref Vector2 tip)
        {
            Vector2 point = PuppetSpearTipPosition;
            Vector2 shaftDirection = (point - origin).SafeNormalize(new Vector2(NPC.direction, 0f));

            if (ThrustPose)
            {
                origin = point - shaftDirection * HeadLength;
            }
            else
            {
                origin += shaftDirection * 24f;
            }

            tip = point;
        }

        // ── Briar Hew / Oathkeeper (legacy MeleeCombo, authored Weighted clock) ────────────────────────
        // Briar Hew (overhand, both health phases)
        //   Poses:   -1.55 -> 2.36 rad (envelope 224.0 deg; live ~0.8 x envelope = ~180 deg); aim bias +-0.35
        //   Tell:    40t on screen (multiplier 1): straight raise carry -> cocked over 26t, hold 14t (HewTellRotation)
        //   Strike:  EaseIn 8, cruise 0, EaseOut 34, k6 -> peak 224 / (8/3 + 34 * (1 - e^-6) / 6) = 26.9 deg/t
        //   Live:    HitWindowEnd (8 + 34 * 1.204 / 6) / 42 = 0.353 -> ~15t (< 22: one roll covers it)
        //   Open:    ~27t harmless tail + 32t recovery (first 6t held) = ~59t after the blade disarms
        //   Move:    planted (no forward push)            Select: 0-112px, |dy| < 48, bag x2, cooldown 45t
        //   Counter: roll through the ~15t blade or step out of the head's path. Rolls needed: 1
        //   Reach:   hitbox ComboReachBase 137.1 x 0.7 = 96px; visual grip->point 96.0px at scale 1.15
        // Oathkeeper (below half HP: replaces Hew's single cut)
        //   Overhand as above, pause 16 (re-face only here, after the position gate), then underhand
        //   2.36 -> -1.55: EaseIn 8, EaseOut 32, k6 -> peak 224 / (8/3 + 32 * (1 - e^-6) / 6) = 28.1 deg/t,
        //   HitWindowEnd (8 + 32 * 1.204 / 6) / 40 = 0.361 (~15t). Endpoints shared: no re-raise snap.
        //   Underhand only if the target is alive, on the committed side and within 112px (checked before the
        //   pause re-faces). Pierce only if same side and within 144px at the underhand's end. A failed gate goes
        //   straight to the 42t recovery. Live starts 58t apart; second live starts 43t after the first ends (>= 30).
        static MeleeComboStep HalberdCut(ComboMotion motion, int tell, int easeOut, int pause, float damageMult) => new MeleeComboStep
        {
            Motion = motion,
            TelegraphTicks = tell,
            AttackTicks = 8 + easeOut,
            PostStepPause = pause,
            DamageMult = damageMult,
            ReachMult = 1f,
            SwingSpeedMult = 1f,
            Ease = SwingEaseStyle.Weighted,
            EaseInTicks = 8,
            EaseOutTicks = easeOut,
            EaseOutDecay = 6f,
            HitWindowEnd = (8f + easeOut * 1.204f / 6f) / (8 + easeOut),
        };

        static readonly MeleeCombo[] Combos =
        {
            new MeleeCombo
            {
                Name = HewName, BaseWeight = 100, Preferred = ComboRangeBand.Any, RecoveryTicks = 32, InitialFlashColor = Accent,
                Steps = new[] { HalberdCut(ComboMotion.OverheadArc, HewTellTicks, 34, 0, 1f) },
            },
            new MeleeCombo
            {
                Name = OathName, BaseWeight = 100, Preferred = ComboRangeBand.Any, RecoveryTicks = 42, InitialFlashColor = Accent,
                Steps = new[]
                {
                    HalberdCut(ComboMotion.OverheadArc, HewTellTicks, 34, 16, 1f),
                    HalberdCut(ComboMotion.UnderhandArc, 0, 32, 0, UnderhandExpertDamage / (float)HewExpertDamage),
                },
            },
        };

        string _requestedCombo;

        protected override bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction) => combo.Name == _requestedCombo;
        protected override bool CanDamageWithMeleeStep(MeleeComboStep step, float progress) => progress < step.HitWindowEnd;
        protected override void DoMeleeAttack() { }
        protected override void DoRangedAttack() { }

        protected override bool ShouldContinueMeleeCombo(string comboName, int nextStepIndex, Player target, bool previousStepHit)
        {
            if (comboName != OathName || target == null || !target.active || target.dead)
            {
                return false;
            }

            int targetSide = Math.Sign(target.Center.X - NPC.Center.X);
            bool sameSide = targetSide == NPC.direction;
            bool inRange = NPC.Distance(target.Center) <= 112f;
            Trace(sameSide && inRange ? "oath-underhand" : "oath-truncated");
            return sameSide && inRange;
        }

        protected override void OnComboStepCompleted(MeleeComboStep step)
        {
            if (!Server || ActiveMeleeComboName != OathName || step.Motion != ComboMotion.UnderhandArc || !NPC.HasValidTarget)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            int targetSide = Math.Sign(target.Center.X - NPC.Center.X);
            bool sameSide = targetSide == NPC.direction;
            bool inRange = NPC.Distance(target.Center) <= 144f;
            _pendingPierce = sameSide && inRange;
            Trace(_pendingPierce ? "oath-pierce" : "oath-truncated");
        }

        protected override void OnMeleeComboTelegraphTick(MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            if (Main.dedServ)
            {
                return;
            }

            if (elapsed == 0)
            {
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, Pitch = -0.3f }, NPC.Center);
            }

            // Green motes gather on the head through the tell (~2 per 2 ticks), so the timer is never invisible.
            if (elapsed % 2 == 0)
            {
                DarkrootWoodVFX.Motes(PuppetSpearTipPosition, 2, 10f, true);
            }
        }

        public void OnHumanoidMeleeHit(Player target)
        {
            // Confirmed contact only: a small bark/steel puff where the halberd landed. No status effects.
            DarkrootWoodVFX.Motes(target.Center, 10, 8f, false, DustID.WoodFurniture);
        }

        // ── Custom action timelines (ticks from the action's start) ──────────────────────────────────────
        // Sentry's Measure: tell 34 (draw back 22, hold 12; lock t24) | strike 24: extend 6, LIVE [6,16), retract 8
        //   | recovery 36 (retracted pose held 6). Point 96 -> 110px; planted. Open = 8 + 36 = 44t. 64-144px.
        const int MeasureTell = 34;
        const int MeasureLock = 24;
        const int MeasureTotal = 34 + 24 + 36;
        // Rootbound Advance: tell 46 (lock direction/destination/pitch at t32, lane shown 14t) | advance 24t
        //   (6 accel, 12 cruise, 6 decel; peak = distance / 18) | point extends from advance t12 over 6t, LIVE [18,30),
        //   retract [30,42) | recovery 42. Destination 48px short of the locked target X. 144-288px.
        const int AdvanceTell = 46;
        const int AdvanceLock = 32;
        const int AdvanceTravelTicks = 24;
        const int AdvanceRetractStart = 30;
        const int AdvanceTotal = 46 + 42 + 42;
        // Thorncast: tell 42 (aim locks t30, fan corridor 12t) | release t42 (and t84 below half) | recoil 12
        //   | recovery 36. Owner offense/guard also wait for every thorn to go harmless + 30t.
        const int ThornRelease = 42;
        const int ThornLock = 30;
        const int ThornSecondRelease = 84;
        // Rootwake: lower the point 20t | lock + mark at t20 | eruptions t60/84/108 (live 12t each, from +4)
        //   | last sinks by t136 | recovery 36 -> 172. Planted throughout.
        const int RootLock = 20;
        const int RootLastSink = 136;
        const int RootTotal = 172;
        // Briar Lash, cast from the free (back) hand while the halberd stays at carry:
        //   tell 48: the hand rises up-and-back over 20t, an earth root locks at t28 (corridor shown
        //   20t), the arm whips overhand to the aim t42-48 | vine extends 20t (tip LIVE) | retracts 18t | recovery 36.
        const int LashLock = 28;
        const int LashRelease = 48;
        const int LashCockTicks = 20;
        const int LashWhipStart = 42;
        // Facing-space arm angles (0 = ahead, negative = up): cocked up and behind the head; hanging at the side.
        const float LashCockAngle = -2.3f;
        const float ArmHangingAngle = MathHelper.PiOver2;
        const float LashMinDistance = 128f;
        const float LashMaxDistance = 560f;
        const int LashVineEnd = 48 + DarkrootVine.ExtendTicks + DarkrootVine.RetractTicks;
        const int LashTotal = LashVineEnd + 36;
        // Oathkeeper pierce: regrip 12 (from the underhand's raised end down to level) + hold 6 | strike 24 as
        //   Measure, with a 24px tile-stopped step over the 6t extension | recovery 42.
        const int PierceRegrip = 18;
        const int PierceTotal = 18 + 24 + 42;

        /// <summary>
        /// The single source of every Custom pose: halberd swing-space rotation and spear grip fraction for the
        /// current action and tick. Read by CustomWeaponRotation (draw + collision direction) and OverrideSpearGrip,
        /// so the drawn halberd and its capsule can never disagree. Aim = _pitch: DoCustomTick tracks the live target
        /// into it on every machine until the action's lock tick, after which the server's locked value is synced.
        /// </summary>
        void ResolveCustomPose(out float rotation, out float grip)
        {
            int elapsed = CustomElapsed;
            rotation = CarryRotation;
            grip = CarryGrip;

            if (_abortRecovery)
            {
                // Hold the pose the action stopped in for 6t, then ease to carry over 12t.
                float easeBack = MathHelper.Clamp((elapsed - 6) / 12f, 0f, 1f);
                rotation = MathHelper.SmoothStep(_abortRotation, CarryRotation, easeBack);
                grip = MathHelper.SmoothStep(_abortGrip, CarryGrip, easeBack);
                return;
            }

            switch (_custom)
            {
                case CustomAction.Measure:
                case CustomAction.Pierce:
                {
                    bool pierce = _custom == CustomAction.Pierce;
                    int tell = pierce ? PierceRegrip : MeasureTell;
                    float aim = LevelRotation + _pitch;
                    float tellStart = pierce ? CockedRotation : CarryRotation;
                    float tellGrip = pierce ? CarryGrip : CockedGrip;
                    int moveTicks = pierce ? 12 : 22;

                    if (elapsed < tell)
                    {
                        float raise = MathHelper.Clamp(elapsed / (float)moveTicks, 0f, 1f);
                        rotation = MathHelper.SmoothStep(tellStart, aim, raise);
                        grip = MathHelper.SmoothStep(CarryGrip, tellGrip, raise);
                        return;
                    }

                    int strike = elapsed - tell;
                    rotation = aim;

                    if (strike < 6)
                    {
                        grip = MathHelper.SmoothStep(tellGrip, ExtendedGrip, strike / 6f);
                    }
                    else if (strike < 16)
                    {
                        grip = ExtendedGrip;
                    }
                    else if (strike < 24)
                    {
                        grip = MathHelper.SmoothStep(ExtendedGrip, CarryGrip, (strike - 16) / 8f);
                    }
                    else
                    {
                        float settle = MathHelper.Clamp((strike - 24 - 6) / 14f, 0f, 1f);
                        rotation = MathHelper.SmoothStep(aim, CarryRotation, settle);
                    }

                    return;
                }

                case CustomAction.Advance:
                {
                    float aim = LevelRotation + _pitch;

                    if (elapsed < AdvanceTell)
                    {
                        float raise = MathHelper.Clamp(elapsed / 26f, 0f, 1f);
                        rotation = MathHelper.SmoothStep(CarryRotation, aim, raise);
                        grip = MathHelper.SmoothStep(CarryGrip, CockedGrip, raise);
                        return;
                    }

                    int advance = elapsed - AdvanceTell;
                    rotation = aim;
                    grip = CockedGrip;

                    if (advance >= 12 && advance < 18)
                    {
                        grip = MathHelper.SmoothStep(CockedGrip, ExtendedGrip, (advance - 12) / 6f);
                    }
                    else if (advance >= 18 && advance < AdvanceRetractStart)
                    {
                        grip = ExtendedGrip;
                    }
                    else if (advance >= AdvanceRetractStart && advance < 42)
                    {
                        grip = MathHelper.SmoothStep(ExtendedGrip, CarryGrip, (advance - AdvanceRetractStart) / 12f);
                    }
                    else if (advance >= 42)
                    {
                        grip = CarryGrip;
                        float settle = MathHelper.Clamp((advance - 42 - 6) / 14f, 0f, 1f);
                        rotation = MathHelper.SmoothStep(aim, CarryRotation, settle);
                    }

                    return;
                }

                case CustomAction.Thorncast:
                {
                    float aim = LevelRotation + _pitch;
                    int lastRelease = _fanCount == 2 ? ThornSecondRelease : ThornRelease;

                    if (elapsed < 12)
                    {
                        rotation = MathHelper.SmoothStep(CarryRotation, aim, elapsed / 12f);
                        return;
                    }

                    rotation = aim;

                    // Release recoil: the head kicks 0.22 rad up and settles over 12t after each release.
                    bool afterFirst = elapsed >= ThornRelease && elapsed < ThornRelease + 12;
                    bool afterSecond = _fanCount == 2 && elapsed >= ThornSecondRelease && elapsed < ThornSecondRelease + 12;
                    if (afterFirst || afterSecond)
                    {
                        int sinceRelease = afterSecond ? elapsed - ThornSecondRelease : elapsed - ThornRelease;
                        rotation -= 0.22f * (float)Math.Sin(MathHelper.Pi * sinceRelease / 12f);
                    }

                    if (elapsed >= lastRelease + 12)
                    {
                        float settle = MathHelper.Clamp((elapsed - lastRelease - 12 - 6) / 14f, 0f, 1f);
                        rotation = MathHelper.SmoothStep(aim, CarryRotation, settle);
                    }

                    return;
                }

                case CustomAction.Rootwake:
                {
                    if (elapsed < RootLock)
                    {
                        rotation = MathHelper.SmoothStep(CarryRotation, RootwakeLowered, elapsed / (float)RootLock);
                        return;
                    }

                    rotation = RootwakeLowered;

                    if (elapsed >= RootLastSink)
                    {
                        float settle = MathHelper.Clamp((elapsed - RootLastSink - 6) / 14f, 0f, 1f);
                        rotation = MathHelper.SmoothStep(RootwakeLowered, CarryRotation, settle);
                    }

                    return;
                }

                case CustomAction.Lash:
                {
                    // The free hand casts (FreeBackArmFacingAngle); the halberd stays at carry, ready to punish.
                    return;
                }
            }
        }

        /// <summary>Current aim pitch at the target (positive = below), clamped. Pre-lock only; every machine can
        /// compute it from the synced target, so the tell pose matches before the lock snapshot arrives.</summary>
        float LivePitch(float limit)
        {
            int targetIndex = _lockedTarget >= 0 ? _lockedTarget : NPC.target;
            if (targetIndex < 0 || targetIndex >= Main.maxPlayers || !Main.player[targetIndex].active)
            {
                return 0f;
            }

            Vector2 gap = Main.player[targetIndex].Center - NPC.Center;
            float pitch = (float)Math.Atan2(gap.Y, Math.Abs(gap.X));
            return MathHelper.Clamp(pitch, -limit, limit);
        }

        // ── Per-tick AI ──────────────────────────────────────────────────────────────────────────────────────
        public override void AI()
        {
            _age++;

            if (_restTicks > 0)
            {
                _restTicks--;
            }

            if (_guardCooldown > 0)
            {
                _guardCooldown--;
            }

            bool hazards = HasOwnedHazards();
            if (hazards)
            {
                _graceTicks = 30;
            }
            else if (_graceTicks > 0)
            {
                _graceTicks--;
            }

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();

            // Direct the free arm toward a player held by the ground vines. Matched by ai[0] rather than OwnedBy,
            // because the encounter serial is server-only. Shoulder = vanilla back shoulder (6 * facing, -2).
            _tetherArmAngle = null;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                bool myHold = projectile.active && projectile.ModProjectile is DarkrootVine vine
                    && (int)projectile.ai[0] == NPC.whoAmI && vine.HeldPlayer >= 0;

                if (myHold)
                {
                    Player held = Main.player[((DarkrootVine)projectile.ModProjectile).HeldPlayer];
                    Vector2 shoulder = NPC.Center + new Vector2(6f * NPC.direction, -2f);
                    Vector2 gap = held.Center - shoulder;
                    _tetherArmAngle = (float)Math.Atan2(gap.Y, gap.X * NPC.direction);
                    break;
                }
            }

            if (Server)
            {
                ServerPreTick(globalNPC, hazards);
            }

            base.AI();

            // Custom thrust damage samples AFTER base.TickWeaponAnim, so it uses this tick's visible pose.
            if (Phase == AttackPhase.Custom && _customLive && Server)
            {
                if (!_customArmed)
                {
                    float damageMult = _customExpertDamage / (float)HewExpertDamage;
                    DoComboMeleeHit(new MeleeComboStep { DamageMult = damageMult, ReachMult = 1.2f });
                    _customArmed = true;
                }

                TickBladeHit();
            }
            else if (_customArmed)
            {
                StopMeleeHit();
                _customArmed = false;
            }

            if (Server)
            {
                ServerPostTick();
            }

            // Hyper-armor is only the Advance travel window; cuts, tells and recoveries stay poise-breakable.
            globalNPC.AttackCommitted = Phase == AttackPhase.Custom && CustomAttackCommitted;
            globalNPC.AttackTelegraphing = !Neutral;

            if (_guardUp && Neutral)
            {
                NPC.direction = _guardFacing;
                NPC.spriteDirection = _guardFacing;
            }

            globalNPC.ShieldGuarding = _guardUp && _guardLift == 0 && Neutral;
            DebugAttackLabel = CurrentLabel();
        }

        /// <summary>Server, before base.AI: cooldowns, phase latch, guard, target loss, and the next pick.</summary>
        void ServerPreTick(tsorcRevampGlobalNPC globalNPC, bool hazards)
        {
            foreach (Move move in new[] { Move.Hew, Move.Measure, Move.Advance, Move.Thorncast, Move.Rootwake, Move.Lash })
            {
                if (_cooldowns.TryGetValue(move, out int remaining) && remaining > 0)
                {
                    _cooldowns[move] = remaining - 1;
                }
            }

            if (_rootRetryTicks > 0)
            {
                _rootRetryTicks--;
            }

            if (!NPC.HasValidTarget)
            {
                NPC.TargetClosest();
            }

            if (_serial == 0)
            {
                _nextSerial++;
                _serial = _nextSerial;
                Snapshot();
            }

            // Below half health latches once; it changes the NEXT action only (Oathkeeper form, double fan).
            if (!_phaseTwo && NPC.life < NPC.lifeMax * 0.5f)
            {
                _phaseTwo = true;
                _bag?.Clear();
                Trace("phase-two");
                Snapshot();
            }

            if (Neutral)
            {
                _neutralTicks++;
            }
            else
            {
                _neutralTicks = 0;
            }

            UpdateGuard(globalNPC, hazards);

            bool grounded = NPC.velocity.Y == 0f;

            // Briar Lash grab punish: while one of this Sentry's vines holds a player, it skips the bag, grace and
            // cooldowns and spends one attack on them: Hew in reach, Measure at 64-144px, Advance at 144-288px.
            // Farther away it walks in. Once claimed, normal selection resumes.
            DarkrootVine holdingVine = null;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                bool ownedHold = projectile.active && projectile.ModProjectile is DarkrootVine vine && vine.OwnedBy(NPC.whoAmI, _serial)
                    && vine.HeldPlayer >= 0 && !vine.PunishClaimed;

                if (ownedHold)
                {
                    holdingVine = (DarkrootVine)projectile.ModProjectile;
                    break;
                }
            }

            if (holdingVine != null)
            {
                Player held = Main.player[holdingVine.HeldPlayer];
                NPC.target = held.whoAmI;
                bool canStart = Neutral && grounded && globalNPC.StaggerTimer == 0
                    && !Collision.SolidCollision(NPC.position, NPC.width, NPC.height);

                if (canStart)
                {
                    float distance = NPC.Distance(held.Center);
                    float verticalGap = Math.Abs(held.Center.Y - NPC.Center.Y);
                    Move punish = Move.None;

                    if (distance <= 112f && verticalGap < 48f)
                    {
                        punish = Move.Hew;
                    }
                    else if (distance >= 64f && distance <= 144f && verticalGap < 48f)
                    {
                        punish = Move.Measure;
                    }
                    else if (distance >= 144f && distance <= 288f && ValidateAdvanceRoute(held, out _))
                    {
                        punish = Move.Advance;
                    }

                    if (punish != Move.None)
                    {
                        BeginMove(punish, held, distance);

                        // A failed Hew dispatch leaves _move at None: keep the punish available for the next tick.
                        if (_move != Move.None)
                        {
                            holdingVine.PunishClaimed = true;
                            Trace("grab-punish-" + punish);
                        }
                    }
                }

                return;
            }

            bool ready = Neutral && NPC.HasValidTarget && grounded && _restTicks == 0 && _graceTicks == 0 && !hazards
                && globalNPC.StaggerTimer == 0 && _age >= 30 && !InFinisherPunishWindow
                && !Collision.SolidCollision(NPC.position, NPC.width, NPC.height);

            if (ready)
            {
                SelectMove(Main.player[NPC.target]);
            }
        }

        /// <summary>Server, after base.AI: chain the Oathkeeper pierce, cancel on target loss, close finished actions.</summary>
        void ServerPostTick()
        {
            if (_pendingPierce && Phase == AttackPhase.MeleeComboRecovery)
            {
                _pendingPierce = false;
                _lockedTarget = NPC.target;
                _lockFacing = NPC.direction;
                StartAction(CustomAction.Pierce, PierceTotal);
            }

            if (Phase == AttackPhase.Custom && _custom != CustomAction.None && !_abortRecovery)
            {
                bool targetLost = _lockedTarget < 0 || _lockedTarget >= Main.maxPlayers
                    || !Main.player[_lockedTarget].active || Main.player[_lockedTarget].dead;

                if (targetLost)
                {
                    DisarmOwnedHazards();
                    Trace("target-lost");
                    AbortIntoRecovery(36);
                }
            }

            if (Neutral && _move != Move.None)
            {
                CompleteAction();
            }
        }

        void Snapshot()
        {
            NPC.netUpdate = true;
            RequestNetworkSnapshot();
        }

        /// <summary>Server, from DarkrootVine.TryLatch: the Lash tip grabbed a player. Cut the Lash's long hold-pose
        /// short with an 8t recovery so the grab punish (ServerPreTick) can start while the player is still held.</summary>
        public void OnVineLatched(Player held)
        {
            NPC.target = held.whoAmI;

            if (Phase == AttackPhase.Custom && _custom == CustomAction.Lash && !_abortRecovery)
            {
                AbortIntoRecovery(8);
            }

            Trace("lash-grab p" + held.whoAmI);
        }

        public void Trace(string kind)
        {
            if (!Server || !ModContent.GetInstance<tsorcRevampConfig>().DebugMode)
            {
                return;
            }

            string json = System.Text.Json.JsonSerializer.Serialize(new
            {
                kind,
                serial = _serial,
                npc = NPC.whoAmI,
                tick = Main.GameUpdateCount,
                move = _move.ToString(),
                custom = _custom.ToString(),
                phase = Phase.ToString(),
                remaining = PhaseTimer,
                guard = _guardUp,
                phaseTwo = _phaseTwo,
                life = NPC.life,
                fans = _fanCount,
                released = _releasedMask,
            });
            Mod.Logger.Info("DARKROOT_SENTRY " + json);
        }

        string CurrentLabel()
        {
            if (_guardUp && Neutral)
            {
                return _guardLift > 0 ? "Dragon Crest Guard (raising)" : "Dragon Crest Guard";
            }

            if (Phase == AttackPhase.Custom && _custom != CustomAction.None)
            {
                string stage = _abortRecovery ? "aborted" : "t" + CustomElapsed;
                return _custom switch
                {
                    CustomAction.Measure => "Sentry's Measure (" + stage + ")",
                    CustomAction.Advance => "Rootbound Advance (" + stage + ")",
                    CustomAction.Thorncast => "Thorncast x" + _fanCount + " (" + stage + ")",
                    CustomAction.Rootwake => "Rootwake (" + stage + ")",
                    CustomAction.Lash => "Briar Lash (" + stage + ")",
                    _ => "Oathkeeper Pierce (" + stage + ")",
                };
            }

            if (!Neutral)
            {
                return ActiveMeleeComboName;
            }

            return null;
        }

        // ── Selection ──────────────────────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Server, neutral and ready. Rootwake is reserved ahead of the bag once the fight is established; otherwise
        /// the bag deals Hew x2, Measure, Advance, Thorncast, Rootwake, Lash, skipping ineligible cards without
        /// dropping them. Immediate repeats are blocked until a different action completes, or 90t of neutral pass
        /// (so a target hugging the Sentry, where only Hew fits, is not ignored forever).
        /// </summary>
        void SelectMove(Player target)
        {
            float distance = NPC.Distance(target.Center);
            float verticalGap = Math.Abs(target.Center.Y - NPC.Center.Y);
            bool lineOfSight = Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1);

            if (!lineOfSight)
            {
                return;
            }

            bool rootwakeEligible = CooldownReady(Move.Rootwake) && _rootRetryTicks == 0
                && distance >= 128f && distance <= 480f && PlanRoots(target, out _, out _) >= 2;

            if (_completedActions > 0 && _age >= 180 && rootwakeEligible)
            {
                Trace("rootwake-reserved");
                BeginMove(Move.Rootwake, target, distance);
                return;
            }

            _bag ??= new AttackBag<Move>(cards =>
            {
                cards.Add(Move.Hew);
                cards.Add(Move.Hew);
                cards.Add(Move.Measure);
                cards.Add(Move.Advance);
                cards.Add(Move.Thorncast);
                cards.Add(Move.Rootwake);
                cards.Add(Move.Lash);
            });

            bool Eligible(Move move)
            {
                bool repeatBlocked = move == _lastMove && _neutralTicks < 90;
                if (repeatBlocked || !CooldownReady(move))
                {
                    return false;
                }

                switch (move)
                {
                    case Move.Hew:
                        return distance <= 112f && verticalGap < 48f;
                    case Move.Measure:
                        return distance >= 64f && distance <= 144f && verticalGap < 48f;
                    case Move.Advance:
                        return distance >= 144f && distance <= 288f && ValidateAdvanceRoute(target, out _);
                    case Move.Thorncast:
                        return distance >= 160f && distance <= 640f;
                    case Move.Rootwake:
                        return rootwakeEligible;
                    case Move.Lash:
                    {
                        if (distance < LashMinDistance || distance > LashMaxDistance)
                        {
                            return false;
                        }

                        return BuildLashPath(target, null);
                    }
                    default:
                        return false;
                }
            }

            if (!_bag.TryDraw(Eligible, out Move chosen))
            {
                if (!rootwakeEligible && _rootRetryTicks == 0 && distance >= 128f && distance <= 480f && CooldownReady(Move.Rootwake))
                {
                    // Log why the signature is gated out: no valid ground pattern near this target.
                    _rootRetryTicks = 120;
                    Trace("rootwake-rejected-terrain");
                }

                return;
            }

            BeginMove(chosen, target, distance);
        }

        bool CooldownReady(Move move)
        {
            return !_cooldowns.TryGetValue(move, out int remaining) || remaining <= 0;
        }

        /// <summary>Server: drop the guard, face and lock the target, and dispatch the chosen action.</summary>
        void BeginMove(Move move, Player target, float distance)
        {
            DropGuard();
            int facing = target.Center.X < NPC.Center.X ? -1 : 1;
            NPC.direction = facing;
            NPC.spriteDirection = facing;
            _lockFacing = facing;
            _lockedTarget = target.whoAmI;
            _move = move;
            _releasedMask = 0;
            _pitch = 0f;

            switch (move)
            {
                case Move.Hew:
                    _requestedCombo = _phaseTwo ? OathName : HewName;
                    if (!TryStartMeleeCombo(distance, rangedStartOnly: false))
                    {
                        // Failed dispatch must not consume the action; it stays eligible next neutral tick.
                        _move = Move.None;
                        Trace("hew-dispatch-failed");
                        return;
                    }

                    break;
                case Move.Measure:
                    StartAction(CustomAction.Measure, MeasureTotal);
                    break;
                case Move.Advance:
                    StartAction(CustomAction.Advance, AdvanceTotal);
                    break;
                case Move.Thorncast:
                    _fanCount = _phaseTwo ? 2 : 1;
                    StartAction(CustomAction.Thorncast, (_fanCount == 2 ? ThornSecondRelease : ThornRelease) + 12 + 36);
                    break;
                case Move.Rootwake:
                    StartAction(CustomAction.Rootwake, RootTotal);
                    break;
                case Move.Lash:
                    StartAction(CustomAction.Lash, LashTotal);
                    break;
            }

            Trace("pick-" + move);
            Snapshot();
        }

        void StartAction(CustomAction action, int totalTicks)
        {
            _custom = action;
            _actionTotal = totalTicks;
            _abortRecovery = false;
            _customLive = false;
            _releasedMask = 0;
            if (action == CustomAction.Lash)
            {
                Array.Clear(_lashControls, 0, _lashControls.Length);
            }

            StartCustomAttack(totalTicks, MeleeWeaponItemType);
            Snapshot();
        }

        /// <summary>Server: end the current Custom action's offense and play a harmless recovery of the given length,
        /// starting from the exact pose it stopped in.</summary>
        void AbortIntoRecovery(int recoveryTicks)
        {
            ResolveCustomPose(out float rotation, out float grip);
            _abortRotation = rotation;
            _abortGrip = grip;
            _abortRecovery = true;
            _customLive = false;
            _actionTotal = recoveryTicks;
            PhaseTimer = recoveryTicks + 1;
            NPC.velocity.X = 0f;
            Snapshot();
        }

        /// <summary>Server: the action returned to neutral. Cooldowns start on full exit.</summary>
        void CompleteAction()
        {
            int cooldown = _move switch
            {
                Move.Hew => 45,
                Move.Measure => 75,
                Move.Advance => 210,
                Move.Thorncast => _fanCount == 2 ? 300 : 240,
                Move.Rootwake => 480,
                Move.Lash => 210,
                _ => 0,
            };

            _cooldowns[_move] = cooldown;
            bool melee = _move == Move.Hew || _move == Move.Measure || _move == Move.Advance;
            if (melee)
            {
                // A 12t neutral planning beat after melee, on top of the authored recovery.
                _restTicks = 12;
            }

            Trace("complete-" + _move);
            _lastMove = _move;
            _completedActions++;
            _move = Move.None;
            _custom = CustomAction.None;
            _abortRecovery = false;
            _lockedTarget = -1;
            Snapshot();
        }

        // ── Custom phase tick (every machine; spawns and locks are server only) ──────────────────────────
        protected override void DoCustomTick(int ticksRemaining)
        {
            if (_custom == CustomAction.None)
            {
                return;
            }

            int elapsed = _actionTotal - ticksRemaining;
            _customLive = false;

            if (_abortRecovery)
            {
                NPC.velocity.X *= 0.7f;
                return;
            }

            Player target = null;
            if (_lockedTarget >= 0 && _lockedTarget < Main.maxPlayers && Main.player[_lockedTarget].active)
            {
                target = Main.player[_lockedTarget];
            }

            // Facing follows the target until the action's lock, then holds the locked side.
            int lockTick = _custom switch
            {
                CustomAction.Measure => MeasureLock,
                CustomAction.Advance => AdvanceLock,
                CustomAction.Thorncast => ThornLock,
                CustomAction.Rootwake => RootLock,
                CustomAction.Lash => LashLock,
                CustomAction.Pierce => 10,
                _ => 0,
            };

            // The Pierce keeps the side committed by the combo; the others turn with the target until their lock.
            bool canTurn = _custom != CustomAction.Pierce && target != null && Math.Abs(target.Center.X - NPC.Center.X) > 8f;
            if (elapsed < lockTick && canTurn)
            {
                _lockFacing = target.Center.X < NPC.Center.X ? -1 : 1;
            }

            // Pre-lock the aim tracks the target on every machine; the server's value at the lock tick is then synced.
            if (elapsed < lockTick && target != null)
            {
                float pitchLimit = ThrustPitchLimit;
                if (_custom == CustomAction.Thorncast)
                {
                    pitchLimit = CastPitchLimit;
                }
                else if (_custom == CustomAction.Lash)
                {
                    pitchLimit = LashPitchLimit;
                }

                _pitch = LivePitch(pitchLimit);
            }

            NPC.direction = _lockFacing;
            NPC.spriteDirection = _lockFacing;

            switch (_custom)
            {
                case CustomAction.Measure:
                {
                    if (elapsed == 0 && !Main.dedServ)
                    {
                        SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.6f, Pitch = 0.1f }, NPC.Center);
                    }

                    if (elapsed == MeasureLock && Server)
                    {
                        _pitch = LivePitch(ThrustPitchLimit);
                        Trace("measure-lock");
                        Snapshot();
                    }

                    if (elapsed >= 22 && elapsed < MeasureTell && !Main.dedServ && elapsed % 3 == 0)
                    {
                        // Small green/steel point glint for the held part of the tell.
                        DarkrootWoodVFX.Motes(PuppetSpearTipPosition, 1, 3f, true, DustID.GreenTorch);
                    }

                    int strike = elapsed - MeasureTell;
                    _customLive = strike >= 6 && strike < 16;
                    _customExpertDamage = MeasureExpertDamage;
                    break;
                }

                case CustomAction.Pierce:
                {
                    if (elapsed == 10 && Server)
                    {
                        _pitch = LivePitch(ThrustPitchLimit);
                        Snapshot();
                    }

                    int strike = elapsed - PierceRegrip;

                    // 24px maximum step over the 6t extension (4px/t), stopped by any wall, then braked.
                    if (strike >= 0 && strike < 6)
                    {
                        Vector2 stepAhead = NPC.position + new Vector2(_lockFacing * 6f, -2f);
                        bool wallAhead = Collision.SolidCollision(stepAhead, NPC.width, NPC.height);
                        NPC.velocity.X = wallAhead ? 0f : _lockFacing * 4f;
                    }
                    else
                    {
                        NPC.velocity.X *= 0.6f;
                    }

                    _customLive = strike >= 6 && strike < 16;
                    _customExpertDamage = PierceExpertDamage;
                    break;
                }

                case CustomAction.Advance:
                    TickAdvance(elapsed, target);
                    break;

                case CustomAction.Thorncast:
                    TickThorncast(elapsed, target);
                    break;

                case CustomAction.Rootwake:
                {
                    if (elapsed == 0 && !Main.dedServ)
                    {
                        SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                    }

                    // Tip sparks tie the marks to the weapon until the first eruption.
                    if (elapsed < 60 && elapsed % 3 == 0 && !Main.dedServ)
                    {
                        DarkrootWoodVFX.Motes(PuppetSpearTipPosition, 1, 6f, false, DustID.GreenTorch);
                    }

                    if (elapsed == RootLock && Server)
                    {
                        LockRootwake(target);
                    }

                    break;
                }

                case CustomAction.Lash:
                    TickLash(elapsed, target);
                    break;
            }
        }

        /// <summary>Rootbound Advance: lock the lane at t32, then a distance-solved 24t ground advance with the point
        /// extending from advance t12 (live [18, 30)). Any wall, lost floor or 16px height change disarms it.</summary>
        void TickAdvance(int elapsed, Player target)
        {
            if (elapsed == 0 && !Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, Pitch = -0.5f }, NPC.Center);
            }

            if (elapsed == AdvanceLock && Server)
            {
                _pitch = LivePitch(ThrustPitchLimit);
                bool routeValid = target != null && ValidateAdvanceRoute(target, out _advanceDistance);

                if (!routeValid)
                {
                    Trace("advance-route-rejected");
                    AbortIntoRecovery(42);
                    return;
                }

                _advanceStartBottomY = NPC.Bottom.Y;
                Trace("advance-lock");
                Snapshot();
            }

            int advance = elapsed - AdvanceTell;
            _customExpertDamage = AdvanceExpertDamage;
            _customLive = advance >= 18 && advance < AdvanceRetractStart;

            if (advance < 0)
            {
                return;
            }

            if (advance >= AdvanceTravelTicks)
            {
                NPC.velocity.X *= 0.6f;
                return;
            }

            // Velocity area: 6t SmoothStep ramp (area 3) + 12t cruise + 6t ramp down (area 3) = 18t at peak speed.
            float peakSpeed = Math.Abs(_advanceDistance) / 18f;
            float ramp = 1f;
            if (advance < 6)
            {
                ramp = MathHelper.SmoothStep(0f, 1f, (advance + 0.5f) / 6f);
            }
            else if (advance >= 18)
            {
                ramp = MathHelper.SmoothStep(1f, 0f, (advance - 18 + 0.5f) / 6f);
            }

            float speed = peakSpeed * ramp;
            NPC.velocity.X = _lockFacing * speed;

            if (!Server)
            {
                return;
            }

            Vector2 nextPosition = NPC.position + new Vector2(NPC.velocity.X, -2f);
            bool wall = Collision.SolidCollision(nextPosition, NPC.width, NPC.height);
            bool floorLost = !HasFloorBelow(NPC.Bottom + new Vector2(_lockFacing * (NPC.width * 0.5f + speed), 0f));
            bool heightChanged = Math.Abs(NPC.Bottom.Y - _advanceStartBottomY) > 16f;

            if (wall || floorLost || heightChanged)
            {
                string reason = "advance-height";
                if (wall)
                {
                    reason = "advance-wall";
                }
                else if (floorLost)
                {
                    reason = "advance-floor-lost";
                }

                Trace(reason);
                StopMeleeHit();
                AbortIntoRecovery(42);
            }
        }

        /// <summary>Thorncast: three pale sap buds harden at the head, the aim locks at t30 and three thorns release
        /// at t42 (-12, 0, +12 degrees). Below half health a second identical fan releases at t84.</summary>
        void TickThorncast(int elapsed, Player target)
        {
            if (elapsed == 0 && !Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
            }

            if (elapsed == ThornLock && Server)
            {
                Vector2 tip = PuppetSpearTipPosition;
                Vector2 aimPoint = target != null ? target.Center : tip + new Vector2(_lockFacing * 100f, 0f);
                _thornAim = (aimPoint - tip).SafeNormalize(new Vector2(_lockFacing, 0f));
                _pitch = LivePitch(CastPitchLimit);
                Trace("thorncast-lock");
                Snapshot();
            }

            bool firstRelease = elapsed == ThornRelease && (_releasedMask & 1) == 0;
            bool secondRelease = _fanCount == 2 && elapsed == ThornSecondRelease && (_releasedMask & 2) == 0;

            if ((firstRelease || secondRelease) && Server)
            {
                _releasedMask |= firstRelease ? 1 : 2;
                Vector2 muzzle = PuppetSpearTipPosition;
                int damage = EnemyDamage.Projectile(ThornExpertDamage);

                for (int ray = -1; ray <= 1; ray++)
                {
                    Vector2 velocity = _thornAim.RotatedBy(MathHelper.ToRadians(12f) * ray) * DarkrootThorn.Speed;
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), muzzle, velocity, ModContent.ProjectileType<DarkrootThorn>(),
                        damage, 2f, Main.myPlayer, NPC.whoAmI, _serial, 0f);
                }

                Trace("thorncast-release");
                Snapshot();
            }

            bool releaseTick = elapsed == ThornRelease || (_fanCount == 2 && elapsed == ThornSecondRelease);
            if (releaseTick && !Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item17 with { Pitch = 0.4f }, NPC.Center);
                DarkrootWoodVFX.Motes(PuppetSpearTipPosition, 8, 6f, false, DustID.GrassBlades);
            }
        }

        /// <summary>Briar Lash: the hand commands an earthen root. At t28 its ground origin and path lock and remain
        /// marked for 20t; t48 releases the moving tip for 20t, followed by 18t of harmless retraction.
        /// The source is at least 192px away and approximately the Sentry's distance from the target.</summary>
        void TickLash(int elapsed, Player target)
        {
            if (elapsed == 0 && !Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item17 with { Pitch = -0.2f }, NPC.Center);
            }

            // Before the lock, gather at the casting palm; afterward the committed earth source stirs.
            if (elapsed < LashRelease && !Main.dedServ)
            {
                int moteCount = 1;
                if (elapsed >= LashLock)
                {
                    moteCount = 2;
                }

                Vector2 gatheringPoint = PuppetBackHandPosition;
                if (elapsed >= LashLock && _lashControls[0] != Vector2.Zero)
                {
                    gatheringPoint = _lashControls[0];
                }

                DarkrootWoodVFX.Motes(gatheringPoint, moteCount, 18f, true);
            }

            if (elapsed == LashLock && Server)
            {
                _pitch = LivePitch(LashPitchLimit);
                bool pathValid = target != null && BuildLashPath(target, _lashControls);

                if (!pathValid)
                {
                    Trace("lash-path-rejected");
                    AbortIntoRecovery(36);
                    return;
                }

                Trace("lash-lock");
                Snapshot();
            }

            bool groundReady = _lashControls[0] != Vector2.Zero;
            if (groundReady && !Main.dedServ)
            {
                if (elapsed == LashLock)
                {
                    SoundEngine.PlaySound(SoundID.Grass with { Volume = 1f, Pitch = -0.5f }, _lashControls[0]);
                }

                if (elapsed == LashRelease)
                {
                    SoundEngine.PlaySound(SoundID.Item152 with { Pitch = -0.3f }, _lashControls[0]);
                    DarkrootWoodVFX.Motes(_lashControls[0], 12, 10f, false, DustID.WoodFurniture);
                }
            }

            if (elapsed == LashRelease && Server)
            {
                int index = Projectile.NewProjectile(NPC.GetSource_FromAI(), _lashControls[0], Vector2.Zero, ModContent.ProjectileType<DarkrootVine>(),
                    EnemyDamage.Projectile(LashExpertDamage), 2f, Main.myPlayer, NPC.whoAmI, _serial, 0f);

                if (index >= 0 && index < Main.maxProjectiles && Main.projectile[index].ModProjectile is DarkrootVine vine)
                {
                    vine.SetPath(_lashControls);
                }

                Trace("lash-release");
            }
        }

        // ── Terrain validation (server; each called at selection and again at the lock) ────────────────────
        /// <summary>Advance corridor: same-level target (feet within 32px), destination 48px short of the target X,
        /// and every 16px of the lane is open at body height with a floor under it and no step over 16px.</summary>
        bool ValidateAdvanceRoute(Player target, out float signedTravel)
        {
            signedTravel = 0f;
            float feetGap = Math.Abs(target.Bottom.Y - NPC.Bottom.Y);
            if (feetGap > 32f)
            {
                return false;
            }

            float gap = target.Center.X - NPC.Center.X;
            int side = Math.Sign(gap);
            float travel = Math.Abs(gap) - 48f;
            if (travel < 32f)
            {
                return false;
            }

            for (float distance = 16f; distance <= travel; distance += 16f)
            {
                Vector2 bodyPosition = NPC.position + new Vector2(side * distance, -2f);
                if (Collision.SolidCollision(bodyPosition, NPC.width, NPC.height))
                {
                    return false;
                }

                if (!HasFloorBelow(NPC.Bottom + new Vector2(side * distance, 0f)))
                {
                    return false;
                }
            }

            signedTravel = side * travel;
            return true;
        }

        /// <summary>True when a solid or platform tile lies within 18px below the given foot point.</summary>
        static bool HasFloorBelow(Vector2 foot)
        {
            for (float drop = 2f; drop <= 18f; drop += 8f)
            {
                Point tilePoint = (foot + new Vector2(0f, drop)).ToTileCoordinates();
                Tile tile = Framing.GetTileSafely(tilePoint);
                if (tile.HasTile && !tile.IsActuated && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Rootwake floor plan around the target's feet: nominal offsets -96 / 0 / +96px, each allowed +-16px of
        /// horizontal slide and +-64px of vertical search for a supported surface (solid or platform top) with an
        /// open 24x86 column above it, in line of sight of the target (no floor between player and mark).
        /// Slots closer than 72px centre-to-centre to an accepted slot are dropped (48px clear gap between 24px roots).
        /// Returns the count; <paramref name="points"/> holds each slot's floor point, <paramref name="validMask"/> bit
        /// i = slot i accepted. Slot order is nearest-to-the-Sentry first.
        /// </summary>
        int PlanRoots(Player target, out Vector2[] points, out int validMask)
        {
            points = new Vector2[3];
            validMask = 0;
            int count = 0;
            int side = Math.Sign(target.Center.X - NPC.Center.X);
            if (side == 0)
            {
                side = NPC.direction;
            }

            float[] nominalOffsets = { -96f * side, 0f, 96f * side };
            float[] slides = { 0f, -8f, 8f, -16f, 16f };
            int feetTileY = (int)(target.Bottom.Y / 16f);

            for (int slot = 0; slot < 3; slot++)
            {
                bool found = false;

                foreach (float slide in slides)
                {
                    float worldX = target.Center.X + nominalOffsets[slot] + slide;
                    int tileX = (int)(worldX / 16f);

                    for (int tileY = feetTileY - 4; tileY <= feetTileY + 4 && !found; tileY++)
                    {
                        Tile support = Framing.GetTileSafely(tileX, tileY);
                        Tile above = Framing.GetTileSafely(tileX, tileY - 1);
                        bool supported = support.HasTile && !support.IsActuated
                            && (Main.tileSolid[support.TileType] || Main.tileSolidTop[support.TileType]);
                        bool openAbove = !above.HasTile || above.IsActuated || !Main.tileSolid[above.TileType];

                        if (!supported || !openAbove)
                        {
                            continue;
                        }

                        Vector2 floor = new Vector2(worldX, tileY * 16f);
                        bool roomForRoot = !Collision.SolidCollision(floor + new Vector2(-12f, -DarkrootRoot.RootHeight), 24, DarkrootRoot.RootHeight - 2);
                        bool visibleFromTarget = Collision.CanHitLine(target.Center, 1, 1, floor + new Vector2(0f, -8f), 1, 1);
                        bool clearOfOthers = true;

                        for (int other = 0; other < slot; other++)
                        {
                            if ((validMask & (1 << other)) != 0 && Math.Abs(points[other].X - floor.X) < 72f)
                            {
                                clearOfOthers = false;
                            }
                        }

                        if (roomForRoot && visibleFromTarget && clearOfOthers)
                        {
                            points[slot] = floor;
                            validMask |= 1 << slot;
                            count++;
                            found = true;
                        }
                    }

                    if (found)
                    {
                        break;
                    }
                }
            }

            return count;
        }

        /// <summary>Server, t20: snapshot the target's floor pattern and plant the buds. Fewer than two valid marks
        /// releases the reservation (120t retry) into a harmless recovery. Each slot keeps its nominal eruption time
        /// (60 / 84 / 108) even if a neighbour is skipped, so the rhythm never compresses.</summary>
        void LockRootwake(Player target)
        {
            int count = 0;
            Vector2[] points = null;
            int validMask = 0;

            if (target != null)
            {
                count = PlanRoots(target, out points, out validMask);
            }

            if (count < 2)
            {
                _rootRetryTicks = 120;
                Trace("rootwake-placement-failed");
                AbortIntoRecovery(36);
                return;
            }

            int damage = EnemyDamage.Projectile(RootExpertDamage);
            for (int slot = 0; slot < 3; slot++)
            {
                if ((validMask & (1 << slot)) == 0)
                {
                    continue;
                }

                // Negative age = ticks until eruption, counted from this lock tick (t20): 40 / 64 / 88.
                float budAge = -(40f + 24f * slot);
                Vector2 rootCenter = points[slot] + new Vector2(0f, -DarkrootRoot.RootHeight * 0.5f);
                Projectile.NewProjectile(NPC.GetSource_FromAI(), rootCenter, Vector2.Zero, ModContent.ProjectileType<DarkrootRoot>(),
                    damage, 0f, Main.myPlayer, NPC.whoAmI, _serial, budAge);
            }

            _releasedMask = validMask;
            Trace("rootwake-lock");
            Snapshot();
        }

        /// <summary>
        /// Lock a ground root approximately as far from the player as the Sentry is, with at least 192px of
        /// horizontal clearance. Try nearby earth surfaces on the caster's side, then validate a marked 16px
        /// corridor to the committed target point; no usable ground or blocked paths reject the attack.
        /// </summary>
        bool BuildLashPath(Player target, Vector2[] controlsOut)
        {
            int side = Math.Sign(NPC.Center.X - target.Center.X);
            if (side == 0)
            {
                side = -NPC.direction;
            }

            float sourceDistance = Math.Max(DarkrootVine.MinimumSpawnDistance, NPC.Distance(target.Center));
            float groundY = Math.Max(NPC.Bottom.Y, target.Bottom.Y);
            float[] slides = { 0f, 16f, -16f, 32f, -32f };
            Vector2[] controls = new Vector2[4];
            Vector2[] samples = new Vector2[DarkrootVine.SampleCount + 1];
            float[] arcFraction = new float[DarkrootVine.SampleCount + 1];

            foreach (float slide in slides)
            {
                float distance = sourceDistance + slide;
                if (distance < DarkrootVine.MinimumSpawnDistance)
                {
                    continue;
                }

                Vector2 search = new(target.Center.X + side * distance, groundY);
                if (!DarkrootVine.TryFindGround(search, target.Center, 12, out Vector2 floor))
                {
                    continue;
                }

                Vector2 origin = floor - new Vector2(0f, 8f);
                Vector2 chord = target.Center - origin;
                float chordLength = chord.Length();
                if (chordLength < DarkrootVine.MinimumSpawnDistance || chordLength > DarkrootVine.MaximumPathLength)
                {
                    continue;
                }

                float[] bends = { Math.Min(40f, chordLength * 0.12f), 0f };
                foreach (float bend in bends)
                {
                    controls[0] = origin;
                    controls[1] = origin + chord / 3f + new Vector2(0f, -bend);
                    controls[2] = origin + chord * (2f / 3f) + new Vector2(0f, -bend);
                    controls[3] = target.Center;
                    DarkrootVine.BuildSamples(controls, samples, arcFraction, out float arcLength);
                    if (arcLength > DarkrootVine.MaximumPathLength)
                    {
                        continue;
                    }

                    bool clear = true;
                    for (int i = 0; i < samples.Length; i++)
                    {
                        if (Collision.SolidCollision(samples[i] - new Vector2(8f), 16, 16))
                        {
                            clear = false;
                            break;
                        }
                    }

                    if (!clear)
                    {
                        continue;
                    }

                    if (controlsOut != null)
                    {
                        Array.Copy(controls, controlsOut, 4);
                    }

                    return true;
                }
            }

            return false;
        }

        // ── Owned hazards ────────────────────────────────────────────────────────────────────────────────────
        bool HasOwnedHazards()
        {
            if (_serial == 0)
            {
                return false;
            }

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.ModProjectile is DarkrootHazard hazard && hazard.OwnedBy(NPC.whoAmI, _serial) && hazard.Threatening)
                {
                    return true;
                }
            }

            return false;
        }

        void DisarmOwnedHazards()
        {
            if (!Server || _serial == 0)
            {
                return;
            }

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.ModProjectile is DarkrootHazard hazard && hazard.OwnedBy(NPC.whoAmI, _serial))
                {
                    hazard.Disarm();
                }
            }
        }

        // ── Dragon Crest Guard ───────────────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Server. Raises a frontal guard while neutral, grounded and closing on a visible target at 112-640px, or when
        /// a friendly damaging projectile is predicted within 18t. 10t visible lift, then up to 120t of 100% frontal
        /// blocking while it walks. Any drop starts a 120t cooldown; hits never refresh it. Offense always wins: a
        /// pick drops the guard first.
        /// </summary>
        void UpdateGuard(tsorcRevampGlobalNPC globalNPC, bool hazards)
        {
            bool mustDrop = !Neutral || globalNPC.StaggerTimer > 0 || _restTicks > 0 || _graceTicks > 0 || hazards
                || NPC.velocity.Y != 0f || !NPC.HasValidTarget;

            if (mustDrop)
            {
                DropGuard();
                return;
            }

            if (_guardUp)
            {
                if (_guardLift > 0)
                {
                    _guardLift--;
                    if (_guardLift == 0)
                    {
                        Snapshot();
                    }
                }

                _guardTicks--;
                if (_guardTicks <= 0)
                {
                    DropGuard();
                }

                return;
            }

            if (_guardCooldown > 0)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            int incomingSide = PredictThreatSide();
            float distance = NPC.Distance(target.Center);
            bool closing = distance >= 112f && distance <= 640f && Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1);

            if (incomingSide == 0 && !closing)
            {
                return;
            }

            _guardFacing = incomingSide != 0 ? incomingSide : Math.Sign(target.Center.X - NPC.Center.X);
            if (_guardFacing == 0)
            {
                _guardFacing = NPC.direction;
            }

            _guardUp = true;
            _guardLift = 10;
            _guardTicks = 10 + 120;
            NPC.direction = _guardFacing;
            NPC.spriteDirection = _guardFacing;
            Trace("guard-raise");
            Snapshot();
        }

        /// <summary>Side (-1 / +1) of the soonest friendly damaging projectile whose closest approach within 18t
        /// passes through the Sentry's box (+6px), or of the target swinging within 96px. 0 = no threat.</summary>
        int PredictThreatSide()
        {
            float soonest = 19f;
            int side = 0;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active || !projectile.friendly || projectile.damage <= 0)
                {
                    continue;
                }

                Vector2 gap = projectile.Center - NPC.Center;
                Vector2 relative = projectile.velocity - NPC.velocity;
                float relativeSpeedSquared = relative.LengthSquared();
                float closestTime = relativeSpeedSquared > 0.001f ? -Vector2.Dot(gap, relative) / relativeSpeedSquared : 0f;

                if (closestTime < 0f || closestTime > 18f || closestTime > soonest)
                {
                    continue;
                }

                Vector2 closest = gap + relative * closestTime;
                bool overlapsX = Math.Abs(closest.X) <= (projectile.width + NPC.width) * 0.5f + 6f;
                bool overlapsY = Math.Abs(closest.Y) <= (projectile.height + NPC.height) * 0.5f + 6f;

                if (overlapsX && overlapsY)
                {
                    soonest = closestTime;
                    side = Math.Sign(gap.X);
                }
            }

            if (side == 0 && NPC.HasValidTarget)
            {
                Player target = Main.player[NPC.target];
                if (target.itemAnimation > 0 && NPC.Distance(target.Center) <= 96f)
                {
                    side = Math.Sign(target.Center.X - NPC.Center.X);
                }
            }

            return side;
        }

        void DropGuard()
        {
            if (!_guardUp)
            {
                return;
            }

            _guardUp = false;
            _guardLift = 0;
            _guardTicks = 0;
            _guardCooldown = 120;

            if (Server)
            {
                Trace("guard-drop");
                Snapshot();
            }
        }

        /// <summary>Frontal block by the source centre's horizontal side (the same test as Darkmoon's guard; not a
        /// shield-plane collision). Only once the lift has finished. No sound or dust here: tModLoader asks
        /// CanBeHitBy* every tick for every nearby projectile and swing, not only on real hits, so feedback in this
        /// check clinked constantly while the shield was up.</summary>
        bool Blocks(Vector2 source)
        {
            bool block = _guardUp && _guardLift == 0 && Neutral && Math.Sign(source.X - NPC.Center.X) == _guardFacing;
            return block;
        }

        public override bool? CanBeHitByItem(Player player, Item item)
        {
            if (Blocks(player.Center))
            {
                return false;
            }

            return base.CanBeHitByItem(player, item);
        }

        public override bool? CanBeHitByProjectile(Projectile projectile)
        {
            if (Blocks(projectile.Center))
            {
                return false;
            }

            return base.CanBeHitByProjectile(projectile);
        }

        // ── Movement ─────────────────────────────────────────────────────────────────────────────────────────
        // Neutral: deliberate approach via SmartFighter4 (Pace patrol at the spawn post when there is no target).
        // Actions own their movement: Advance and the Pierce step set velocity in DoCustomTick; everything else brakes.
        protected override void RunMovementAI(float speedMult)
        {
            bool actionOwnsMovement = Phase == AttackPhase.Custom
                && (_custom == CustomAction.Advance || _custom == CustomAction.Pierce) && !_abortRecovery;

            if (actionOwnsMovement)
            {
                return;
            }

            if (Neutral && _restTicks == 0)
            {
                float walkSpeed = TopSpeed;
                if (!_guardUp)
                {
                    walkSpeed *= speedMult;
                }

                SmartFighter4AI.Run(NPC, topSpeed: walkSpeed, acceleration: Acceleration, doorBreakingDamage: 3, attackRange: 700f);
                return;
            }

            NPC.velocity.X *= 0.65f;
        }

        // ── Interruption ─────────────────────────────────────────────────────────────────────────────────────
        // Only a completed poise break lands here: cancel unreleased payloads, disarm and fade live hazards, drop the
        // guard. The interrupted action still counts as exited (its cooldown starts).
        public override void OnStagger(NPC npc)
        {
            if (Server)
            {
                DisarmOwnedHazards();
                _pendingPierce = false;
                _graceTicks = 30;
                DropGuard();
                Trace("poise-interruption");

                if (_move != Move.None)
                {
                    CompleteAction();
                }
            }

            _custom = CustomAction.None;
            _customLive = false;
            base.OnStagger(npc);
        }

        // ── Telegraph drawing (client) ───────────────────────────────────────────────────────────────────────
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            base.PostDraw(spriteBatch, screenPos, drawColor);

            if (Phase != AttackPhase.Custom || _custom == CustomAction.None || _abortRecovery)
            {
                return;
            }

            int elapsed = CustomElapsed;
            Vector2 head = PuppetSpearTipPosition;
            Color corridor = Accent * 0.45f;

            switch (_custom)
            {
                case CustomAction.Thorncast:
                {
                    // Buds form around the real head over t12-36 (and t54-78 for the second fan), then harden.
                    bool firstBuds = elapsed >= 12 && elapsed < ThornRelease;
                    bool secondBuds = _fanCount == 2 && elapsed >= 54 && elapsed < ThornSecondRelease;
                    if (firstBuds || secondBuds)
                    {
                        int budStart = secondBuds ? 54 : 12;
                        float growth = MathHelper.Clamp((elapsed - budStart) / 24f, 0f, 1f);
                        Vector2 side = PuppetWeaponDirection.RotatedBy(MathHelper.PiOver2) * 9f;
                        DarkrootWoodVFX.Draw("DarkrootBud", head + side, new Vector2(26f), 0f, new Vector2(0.5f), growth, growth, 0.85f);
                        DarkrootWoodVFX.Draw("DarkrootBud", head, new Vector2(26f), 0f, new Vector2(0.5f), growth, growth, 0.85f);
                        DarkrootWoodVFX.Draw("DarkrootBud", head - side, new Vector2(26f), 0f, new Vector2(0.5f), growth, growth, 0.85f);
                    }

                    // Dotted fan corridor (three separate lanes) for the final 12t before each release.
                    bool firstCorridor = elapsed >= ThornLock && elapsed < ThornRelease;
                    bool secondCorridor = _fanCount == 2 && elapsed >= ThornSecondRelease - 12 && elapsed < ThornSecondRelease;
                    if (firstCorridor || secondCorridor)
                    {
                        for (int ray = -1; ray <= 1; ray++)
                        {
                            Vector2 direction = _thornAim.RotatedBy(MathHelper.ToRadians(12f) * ray);
                            DarkrootWoodVFX.DashedLine(head, head + direction * 640f, corridor, 1.5f);
                        }
                    }

                    break;
                }

                case CustomAction.Advance:
                {
                    // Dim ground streak along the locked lane for the last 14t of the tell.
                    if (elapsed >= AdvanceLock && elapsed < AdvanceTell)
                    {
                        Vector2 feet = NPC.Bottom + new Vector2(0f, -2f);
                        DarkrootWoodVFX.DashedLine(feet, feet + new Vector2(_advanceDistance, 0f), corridor, 2f);
                    }

                    if (elapsed >= AdvanceLock && elapsed < AdvanceTell && elapsed % 2 == 0)
                    {
                        DarkrootWoodVFX.Motes(head, 1, 3f, true, DustID.GreenTorch);
                    }

                    break;
                }

                case CustomAction.Lash:
                {
                    // The committed path for the last 20t: dashes that brighten and pulse (8t period) as the release
                    // nears, a sprite sprouting at its ground source, and a ring at the committed target point.
                    if (elapsed >= LashLock && elapsed < LashRelease && _lashControls[0] != Vector2.Zero)
                    {
                        Vector2[] samples = new Vector2[DarkrootVine.SampleCount + 1];
                        float[] arcFraction = new float[DarkrootVine.SampleCount + 1];
                        DarkrootVine.BuildSamples(_lashControls, samples, arcFraction, out _);

                        float ramp = (elapsed - LashLock) / (float)(LashRelease - LashLock);
                        DarkrootVine.DrawSprout(_lashControls[0] + new Vector2(0f, 8f), 0.35f + ramp * 0.65f, 1f);
                        float pulse = 0.5f + 0.5f * (float)Math.Sin(elapsed * MathHelper.TwoPi / 8f);
                        Color lane = Accent * (0.35f + 0.35f * ramp + 0.2f * pulse);

                        for (int i = 1; i < samples.Length; i += 2)
                        {
                            Utils.DrawLine(spriteBatch, samples[i - 1], samples[i], lane, lane * 0.6f, 4f);
                        }

                        Vector2 endPoint = samples[DarkrootVine.SampleCount];
                        float ringRadius = MathHelper.Lerp(28f, 12f, ramp);
                        for (int i = 0; i < 12; i++)
                        {
                            float from = MathHelper.TwoPi * i / 12f;
                            float to = from + MathHelper.TwoPi / 24f;
                            Vector2 arcStart = endPoint + from.ToRotationVector2() * ringRadius;
                            Vector2 arcEnd = endPoint + to.ToRotationVector2() * ringRadius;
                            Utils.DrawLine(spriteBatch, arcStart, arcEnd, lane, lane, 3f);
                        }
                    }

                    break;
                }
            }
        }

        // ── Spawn, loot and death (carried over from the legacy enemy) ─────────────────────────────────────
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 5;
            NPC.width = 20;
            NPC.height = 40;
            NPC.aiStyle = -1;
            NPC.damage = 0;
            NPC.defense = 70;
            NPC.lifeMax = 5400;
            NPC.scale = 1f;
            NPC.value = 20000;
            NPC.knockBackResist = 0.3f;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.lavaImmune = true;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.DarkrootSentryBanner>();

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.AllowDynamicEventNaturalDespawn = true;
            globalNPC.NavSearchRadius = 80;
            globalNPC.CanUseRopes = true;
            globalNPC.CanPassThroughWalls = false;
            globalNPC.KiteRangeMax = 0f;
            globalNPC.MaxJumpPower = 9f;
            globalNPC.MaxJumpBoost = 5f;
            globalNPC.PatrolMode = PatrolMode.Pace;
            globalNPC.PatrolAnchorSource = PatrolAnchorSource.SpawnPoint;
            globalNPC.PatrolRange = 10;
            globalNPC.PatrolUsesNavigation = true;
        }

        // Legacy fallback for when New Enemy Spawns is off; with it on, the registry (Systems/EnemySpawns.cs) decides
        // spawning, so this returns 0.
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (EnemySpawns.Enabled)
            {
                return 0f;
            }

            // Same rule as the registry: Super Hardmode, dry, plain underground or plain caverns (no other biome), 5%, at most two alive.
            Player player = spawnInfo.Player;
            bool plainBelowGround = player.ZoneNormalUnderground || player.ZoneNormalCaverns;

            if (spawnInfo.Water || NPC.CountNPCS(ModContent.NPCType<DarkrootSentry>()) >= 2)
            {
                return 0f;
            }

            if (tsorcRevampWorld.SuperHardMode && plainBelowGround && Main.rand.NextBool(20))
            {
                return 1;
            }

            return 0;
        }

        public override void OnKill()
        {
            // Loot resolves before OnKill; retain this ordinary enemy's first kill using the shared world history.
            if (Main.netMode != NetmodeID.MultiplayerClient && tsorcRevampWorld.NewSlain.TryAdd(new NPCDefinition(NPC.type), 1))
            {
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.WorldData);
                }
            }

            DisarmOwnedHazards();
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            base.HitEffect(hit);
            if (Main.dedServ || NPC.life > 0)
            {
                return;
            }

            // Spawn armor near the head, hands, feet, and chest on the clients that see the lethal hit.
            var source = NPC.GetSource_Death();
            Vector2 position = NPC.position;
            Vector2 velocity = NPC.velocity * 0.5f;
            int helmetType = Mod.Find<ModGore>("Darkroot Sentry Gore 1").Type;
            int armType = Mod.Find<ModGore>("Darkroot Sentry Gore 2").Type;
            int bootType = Mod.Find<ModGore>("Darkroot Sentry Gore 3").Type;
            int torsoType = Mod.Find<ModGore>("Darkroot Sentry Gore 4").Type;
            Gore.NewGore(source, position, velocity + new Vector2(NPC.direction * 1.4f, -4.2f), helmetType);
            Gore.NewGore(source, position + new Vector2(-3f, 12f), velocity + new Vector2(-2.6f, -2.6f), armType);
            Gore.NewGore(source, position + new Vector2(11f, 13f), velocity + new Vector2(2.6f, -2f), armType);
            Gore.NewGore(source, position + new Vector2(-1f, 27f), velocity + new Vector2(-1.6f, -0.8f), bootType);
            Gore.NewGore(source, position + new Vector2(10f, 27f), velocity + new Vector2(1.7f, -1.2f), bootType);
            Gore.NewGore(source, position + new Vector2(1f, 12f), velocity + new Vector2(0f, -1.4f), torsoType);

            int bloodType = Mod.Find<ModGore>("Blood Splat").Type;
            for (int i = 0; i < 8; i++)
            {
                Gore.NewGore(source, position, NPC.velocity, bloodType, 0.9f);
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            // Red Titanite 50% (one), Humanity 10% (one); halberd guaranteed on the world's first kill, 10% after.
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.SuperHardmodeRule, ModContent.ItemType<RedTitanite>(), 2));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Humanity>(), 10));
            var halberd = new LeadingConditionRule(new SuperHardmodeRule());
            var firstKill = ItemDropRule.ByCondition(new FirstBossKillRule(requireBoss: false), ModContent.ItemType<DarkrootHalberd>());
            firstKill.OnFailedConditions(ItemDropRule.Common(ModContent.ItemType<DarkrootHalberd>(), 10));
            halberd.OnSuccess(firstKill);
            npcLoot.Add(halberd);
        }

        // ── Multiplayer payload: base first, then the subclass fields in a fixed order ───────────────────────
        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write(_serial);
            writer.Write(_phaseTwo);
            writer.Write((byte)_move);
            writer.Write((byte)_custom);
            writer.Write(_actionTotal);
            writer.Write(_abortRecovery);
            writer.Write(_abortRotation);
            writer.Write(_abortGrip);
            writer.Write(_lockedTarget);
            writer.Write(_lockFacing);
            writer.Write(_pitch);
            writer.WriteVector2(_thornAim);
            writer.Write(_fanCount);
            writer.Write(_releasedMask);
            writer.Write(_advanceDistance);

            for (int i = 0; i < 4; i++)
            {
                writer.WriteVector2(_lashControls[i]);
            }

            writer.Write(_guardUp);
            writer.Write(_guardLift);
            writer.Write(_guardTicks);
            writer.Write(_guardCooldown);
            writer.Write(_guardFacing);
            writer.Write(_restTicks);
            writer.Write(_graceTicks);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            _serial = reader.ReadInt32();
            _phaseTwo = reader.ReadBoolean();
            _move = (Move)reader.ReadByte();
            _custom = (CustomAction)reader.ReadByte();
            _actionTotal = reader.ReadInt32();
            _abortRecovery = reader.ReadBoolean();
            _abortRotation = reader.ReadSingle();
            _abortGrip = reader.ReadSingle();
            _lockedTarget = reader.ReadInt32();
            _lockFacing = reader.ReadInt32();
            _pitch = reader.ReadSingle();
            _thornAim = reader.ReadVector2();
            _fanCount = reader.ReadInt32();
            _releasedMask = reader.ReadInt32();
            _advanceDistance = reader.ReadSingle();

            for (int i = 0; i < 4; i++)
            {
                _lashControls[i] = reader.ReadVector2();
            }

            _guardUp = reader.ReadBoolean();
            _guardLift = reader.ReadInt32();
            _guardTicks = reader.ReadInt32();
            _guardCooldown = reader.ReadInt32();
            _guardFacing = reader.ReadInt32();
            _restTicks = reader.ReadInt32();
            _graceTicks = reader.ReadInt32();
        }
    }
}
