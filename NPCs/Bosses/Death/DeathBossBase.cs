using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Projectiles.Enemy.Death;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses.Death
{
    /// <summary>
    /// Shared state machine for Death and Absolute Death. Phase one, the transition, phase-two
    /// move selection, containment-ring behavior, and projectile patterns live here; subclasses
    /// provide only the per-variant tuning overrides.
    /// </summary>
    abstract class DeathBossBase : ModNPC, IStaggerable
    {
        internal enum ReaperFacingMode
        {
            Player,
            IntentDirection
        }

        protected abstract Color AuraColor { get; }
        internal virtual ReaperFacingMode CurrentReaperFacingMode => ReaperFacingMode.IntentDirection;
        internal Color ReaperLightColor => AuraColor;
        internal int ReaperLaserCount => PhaseTwoReaperLaserCount;
        internal virtual bool ReaperStrong5WarningEnabled => true;
        internal virtual float ReaperStrong5WarningLength => 1500f;
        internal virtual int ReaperStrong5WarningTicks => 15;
        internal int ReaperLaserIntervalTicks => PhaseTwoReaperLaserIntervalTicks;
        internal int ReaperBurstIntervalTicks => PhaseTwoReaperBurstIntervalTicks;
        internal int ReaperBurstShotCount => PhaseTwoReaperBurstShotCount;
        internal int ReaperOrbitVolleyIntervalTicks => PhaseTwoReaperOrbitVolleyIntervalTicks;
        internal float ReaperLaserSpreadDegrees => PhaseTwoReaperLaserSpreadDegrees;
        internal bool ReaperStrong2UsesSickles => PhaseTwoStrong2UsesSickles;
        internal float ReaperStrong2SickleSpeed => PhaseTwoStrong2SickleSpeed;
        internal float ScatterInnerRadius => PhaseTwoScatterInnerRadius;
        internal float ScatterOuterRadius => PhaseTwoScatterOuterRadius;
        internal int ScatterInnerStartTicks => PhaseTwoScatterInnerStartTicks;
        internal int ScatterOuterStartTicks => PhaseTwoScatterOuterStartTicks;
        internal int ScatterFireIntervalTicks => PhaseTwoScatterFireIntervalTicks;
        internal float ScatterFastSickleSpeed => PhaseTwoScatterFastSickleSpeed;
        internal float ScatterSlowSickleSpeed => PhaseTwoScatterSlowSickleSpeed;
        internal float ScatterSwirlDegreesPerSecond => PhaseTwoScatterSwirlDegreesPerSecond;
        internal int ScatterFadeStartTicks => PhaseTwoScatterFadeStartTicks;
        internal int ScatterDurationTicks => PhaseTwoScatterDurationTicks;
        internal bool ScatterBodyBoltsEnabled => PhaseTwoScatterBodyBoltsEnabled;
        internal float ScatterFormationAngle => GetScatterFormationAngle();
        internal int ReaperSickleDamage => SickleDamage;
        internal int ReaperBoltDamage => BoltDamage;
        internal float ReaperBoltColorValue => UsefulFunctions.ColorToFloat(PhaseOneBoltColor);
        internal int ReaperLifeMax => GetReaperLifeMax();
        internal int ReaperDefenseStat => GetReaperDefense();
        internal int ReaperContactDamage => GetReaperContactDamage();
        internal int Strong6AttackStartTicks => PhaseTwoStrong6AttackStartTicks;
        internal int Strong6AttackEndTicks => PhaseTwoStrong6AttackStartTicks + PhaseTwoStrong6AttackDurationTicks;
        internal int Strong6RecoveryTicks => PhaseTwoStrong6RecoveryTicks;
        protected abstract Color AuraDustColor { get; }
        protected abstract Color DespawnTextColor { get; }
        protected abstract int DespawnDustType { get; }
        protected abstract int BoltDamage { get; }
        protected abstract int SickleDamage { get; }
        protected abstract int GiantScytheDamage { get; }
        // Restored after Weak5 recovery; subclasses opt in with their contact damage.
        protected virtual int BodyContactDamage => 0;
        // Deepened violet family: scale the old (190,70,255) down so the additive aura reads as
        // saturated violet instead of washing toward pink/white.
        internal virtual Color VfxGlowColor => new Color(140, 35, 205);
        internal Color SickleGlowColor => VfxGlowColor;
        internal virtual float SickleScaleMultiplier => 1f;
        protected virtual int GetReaperLifeMax() => 1200;
        protected virtual int GetReaperDefense() => 30;
        protected virtual int GetReaperContactDamage() => SickleDamage;

        protected virtual float AuraDustScale => 0.5f;
        protected virtual float PhaseDustHealthThreshold => 0.30f;
        protected virtual float HighHealthDustScale => 1.2f;
        protected virtual float LowHealthDustScale => 1.7f;
        protected virtual float GoreScale => 1.1f;
        protected virtual bool UsesContainmentRing => true;
        protected virtual float ContainmentRingRadius => 1000f;
        protected virtual float ContainmentRingPullSpeed => 5f;
        protected virtual Color ContainmentFogDarkColor => new Color(3, 0, 10);
        protected virtual Color ContainmentFogMidColor => new Color(26, 4, 48);
        protected virtual Color ContainmentFogCoreColor => new Color(82, 18, 130);
        protected virtual Vector2 SickleAnchorOffset => new Vector2(0f, -10f);
        protected virtual float ContainmentFogOpacity => 0.92f;
        protected virtual double ContainmentRingMidnightTime => 16240.0;
        protected virtual float PhaseOneEndHealthFraction => 0.6666f;
        protected virtual int PhaseTransitionDurationTicks => 4 * 60;
        protected virtual int PhaseTransitionRingStartTicks => 60;
        protected virtual int PhaseTransitionHealEndTicks => 3 * 60;
        protected virtual int PhaseTransitionHealInterval => 2;
        protected virtual int PhaseTransitionHealAmount => 666;
        protected virtual int ContainmentRingExpandTicks => 2 * 60;
        protected virtual float PhaseOneMoveSpeed => 3f;
        protected virtual int PhaseOneSickleInterval => 60;
        protected virtual float PhaseOneSickleMinRange => 500f;
        protected virtual float PhaseOneSickleMaxRange => 750f;
        protected virtual float PhaseOneSickleSpeed => 5f;
        protected virtual int PhaseTwoFiveDashWindupTicks => 30;
        protected virtual int PhaseTwoFiveDashCount => 5;
        protected virtual int PhaseTwoFiveDashDurationTicks => 40;
        protected virtual int PhaseTwoFiveDashDecelStartTicks => 25;
        protected virtual int PhaseTwoFiveDashGapTicks => 15;
        protected virtual int PhaseTwoFiveDashRingReturnTicks => 60;
        protected virtual float PhaseTwoFiveDashSpeed => PhaseOneSpecialDashSpeed;
        protected virtual int PhaseTwoBoltIntervalTicks => 10;
        protected virtual float PhaseTwoBoltSpeed => 20f;
        protected virtual int PhaseTwoBoltCount => 1;
        protected virtual float PhaseTwoBoltSpreadDegrees => 0f;
        protected virtual float PhaseTwoCircleRadius => 300f;
        protected virtual int PhaseTwoReaperMoveDurationTicks => 10 * 60;
        protected virtual int PhaseTwoReaperBoltStartTicks => 2 * 60;
        protected virtual int PhaseTwoBodyBoltStartTicks => PhaseTwoReaperBoltStartTicks;
        protected virtual int PhaseTwoReaperBoltIntervalTicks => 60;
        protected virtual int PhaseTwoReaperBoltStopTicks => 9 * 60;
        protected virtual float PhaseTwoReaperBoltSpeed => 10f;
        protected virtual int PhaseTwoBodyBoltCount => 2;
        protected virtual bool PhaseTwoStrong2BodyBoltsEnabled => true;
        protected virtual float PhaseTwoBodyBoltSpacingDegrees => 60f;
        protected virtual int PhaseTwoReaperCount => 6;
        protected virtual bool PhaseTwoStrong2MovesOnCircle => false;
        protected virtual int PhaseTwoReaperLaserCount => 2;
        protected virtual int PhaseTwoReaperLaserIntervalTicks => 10;
        protected virtual int PhaseTwoReaperBurstIntervalTicks => 5;
        protected virtual int PhaseTwoReaperBurstShotCount => 4;
        protected virtual int PhaseTwoReaperOrbitVolleyIntervalTicks => 60;
        protected virtual int PhaseTwoIntermissionTicks => 60;
        protected virtual int PhaseTwoScatterReaperCount => 6;
        protected virtual float PhaseTwoScatterInnerRadius => 100f;
        protected virtual float PhaseTwoScatterOuterRadius => 200f;
        protected virtual int PhaseTwoScatterInnerStartTicks => 120;
        protected virtual int PhaseTwoScatterOuterStartTicks => 150;
        protected virtual int PhaseTwoScatterFireIntervalTicks => 15;
        // Each stream fires two sickles along the same line: a fast one and a slow one.
        protected virtual float PhaseTwoScatterFastSickleSpeed => 8f;
        protected virtual float PhaseTwoScatterSlowSickleSpeed => 5f;
        /// <summary>dt: the swirl angle grows by 90 degrees per second, mirrored on both sides.</summary>
        protected virtual float PhaseTwoScatterSwirlDegreesPerSecond => 90f;
        protected virtual int PhaseTwoScatterFlamingScytheIntervalTicks => 120;
        protected virtual float PhaseTwoScatterFlamingScytheSpeed => 10f;
        // Radius of the circle the boss slides along when PhaseTwoScatterMovesOnCircle is set.
        protected virtual float PhaseTwoScatterCircleRadius => 600f;
        // Firing phase: first volley at PhaseTwoScatterInnerStartTicks (2s) through 10s, i.e. 8s of
        // fire, then the usual 1s fade-out.
        protected virtual int PhaseTwoScatterFadeStartTicks => 10 * 60;
        protected virtual int PhaseTwoScatterDurationTicks => 11 * 60;
        protected virtual int PhaseTwoWeakDurationTicks => 6 * 60;
        protected virtual int PhaseTwoWeak2WarningTicks => 2 * 60;
        protected virtual int PhaseTwoWeak2ThrowTicks => 150;
        protected virtual int PhaseTwoWeak2DurationTicks => 300;
        protected virtual int PhaseTwoWeak2ScytheCount => 3;
        protected virtual float PhaseTwoWeak2ScytheSpreadDegrees => 15f;
        protected virtual bool PhaseTwoWeak2FullCircle => false;
        protected virtual float PhaseTwoWeak2WarningLength => 1800f;
        protected virtual float PhaseTwoWeak2WarningSize => 0.18f;
        protected virtual float PhaseTwoWeak2WarningBrightness => 1f;
        protected virtual int PhaseTwoWeak3WarningTicks => 60;
        protected virtual int PhaseTwoWeak3ReleaseTicks => 90;
        protected virtual int PhaseTwoWeak3DurationTicks => 120;
        protected virtual int PhaseTwoWeak3PhantomLeadTicks => 90;
        protected virtual int PhaseTwoWeak3LineCount => 6;
        protected virtual int PhaseTwoWeak3ReverseLineCount => 0;
        protected virtual float PhaseTwoWeak3BehindDistance => 1500f;
        protected virtual float PhaseTwoWeak3LineSpacing => 600f;
        protected virtual float PhaseTwoWeak3WarningLength => 1500f;
        protected virtual float PhaseTwoWeak3WarningSize => 0.18f;
        protected virtual float PhaseTwoWeak3WarningBrightness => 1.2f;
        protected virtual float PhaseTwoWeak3ScytheSpeed => 30f;
        protected virtual int PhaseTwoWeak4WarningTicks => 90;
        protected virtual int PhaseTwoWeak4ReleaseTicks => 120;
        protected virtual int PhaseTwoWeak4VolleyCount => 1;
        protected virtual int PhaseTwoWeak4VolleyIntervalTicks => 60;
        protected virtual int PhaseTwoWeak4RecoveryTicks => 30;
        protected virtual int PhaseTwoWeak4DurationTicks =>
            PhaseTwoWeak4ReleaseTicks + (PhaseTwoWeak4VolleyCount - 1) * PhaseTwoWeak4VolleyIntervalTicks + PhaseTwoWeak4RecoveryTicks;
        // Weak5 teleports behind and below the player, 45 degrees off the rear axis.
        protected virtual int PhaseTwoWeak5TeleportDistance => 600;
        protected virtual int PhaseTwoWeak5FadeInTicks => 30;
        protected virtual int PhaseTwoWeak5ChargeRotateTicks => 15;
        protected virtual int PhaseTwoWeak5ChargeHoldTicks => 45;
        protected virtual int PhaseTwoWeak5WarningTicks => 60;
        protected virtual float PhaseTwoWeak5WarningLength => 1200f;
        protected virtual int PhaseTwoWeak5SwingCycleTicks => 16;
        protected virtual int PhaseTwoWeak5SwingCycles => 3;
        protected virtual int PhaseTwoWeak5RecoveryTicks => 90;
        protected virtual float PhaseTwoWeak5RushDistance => 2000f;
        protected virtual float PhaseTwoWeak5CircleReturnMaxSpeed => 45f;
        protected virtual float PhaseTwoWeak5CircleReturnAcceleration => 1.20f;
        protected virtual float PhaseTwoWeak5CircleReturnDeceleration => 2.00f;
        protected virtual int PhaseTwoStrong4FadeInTicks => 60;
        protected virtual float PhaseTwoStrong4SpawnRadius => 100f;
        protected virtual int PhaseTwoStrong4DashCount => 6;
        protected virtual int PhaseTwoStrong4DashTicks => 60;
        protected virtual int PhaseTwoStrong4GapTicks => 48;
        protected virtual int PhaseTwoStrong5AttackStartTicks => 60;
        protected virtual int PhaseTwoStrong5AttackDurationTicks => 4 * 60;
        protected virtual int PhaseTwoStrong5RecoveryTicks => 3 * 60;
        protected virtual int PhaseTwoStrong5DurationTicks =>
            PhaseTwoStrong5AttackStartTicks + PhaseTwoStrong5AttackDurationTicks + PhaseTwoStrong5RecoveryTicks;
        protected virtual int PhaseTwoStrong5WaveCount => 4;
        protected virtual int PhaseTwoStrong5ScythesPerWave => 6;
        protected virtual int PhaseTwoStrong5WaveIntervalTicks => 60;
        protected virtual int PhaseTwoStrong5WarningTicks => 30;
        protected virtual float PhaseTwoStrong5ScytheSpeed => 30f;
        protected virtual float PhaseTwoStrong5WarningLength => 1800f;
        protected virtual int PhaseTwoStrong5ReaperIntervalTicks => 15;
        protected virtual int PhaseTwoStrong5ReaperCount => 8;
        protected virtual float PhaseTwoStrong5CircleMoveMaxSpeed => 5f;
        /// <summary>
        /// True: snap onto the circle once, then hold position as soon as the first flaming-scythe
        /// volley is away (normal Death). Absolute Death keeps orbiting for the whole move.
        /// </summary>
        protected virtual bool PhaseTwoStrong5SnapsThenHolds => true;

        // Strong move 6 (Absolute Death only): Death roars, pins the ring, teleports straight up
        // and drops a four-Reaper firing line that hammers the arena for eight seconds.
        protected virtual int PhaseTwoStrong6TeleportOffset => 600;
        protected virtual int PhaseTwoStrong6ContactSuppressTicks => 60;
        protected virtual int PhaseTwoStrong6AttackStartTicks => 60;
        protected virtual int PhaseTwoStrong6AttackDurationTicks => 8 * 60;
        protected virtual int PhaseTwoStrong6RecoveryTicks => 2 * 60;
        protected virtual int PhaseTwoStrong6RingReturnTicks => 60;
        protected virtual int PhaseTwoStrong6DurationTicks =>
            PhaseTwoStrong6AttackStartTicks + PhaseTwoStrong6AttackDurationTicks + PhaseTwoStrong6RecoveryTicks;
        /// <summary>Spawn spacing of the firing line: slots sit at -2, -1, +1 and +2 spacings.</summary>
        protected virtual float PhaseTwoStrong6ReaperSpacing => 200f;

        /// <summary>
        /// Bit mask of the phase-two strong moves this variant may pick. Ids match the switch in
        /// StartNextPhaseTwoMove: 2 = five-dash, 3 = reaper formation, 4 = scatter sickles,
        /// 7 = sickle dashes, 9 = flaming scythes + reaper rush, 10 = death squad
        /// (Absolute Death). Move 4 is parked out of every bag for now; its code is kept intact.
        /// </summary>
        protected virtual int StrongMoveMask => (1 << 2) | (1 << 3) | (1 << 7) | (1 << 9);

        protected virtual float PhaseTwoStrong4DashMinDistance => 900f;
        protected virtual float PhaseTwoStrong4DashMaxDistance => 1600f;
        protected virtual float PhaseTwoStrong4DashOvershoot => 220f;
        protected virtual int PhaseTwoStrong4FadeOutTicks => 30;
        protected virtual int PhaseTwoStrong4PostWaitTicks => 90;
        protected virtual bool LargeScythePhantomsEnabled => true;
        protected virtual int PhaseTwoWeak4LineCount => 6;
        protected virtual float PhaseTwoWeak4Radius => 1500f;
        protected virtual float PhaseTwoWeak4WarningLength => 1800f;
        protected virtual float PhaseTwoWeak4WarningSize => 0.18f;
        protected virtual float PhaseTwoWeak4WarningBrightness => 1.2f;
        protected virtual int PhaseTwoWeakBoltMaxIntervalTicks => 60;
        protected virtual int PhaseTwoWeakBoltMinIntervalTicks => 20;
        protected virtual float PhaseTwoWeakBoltSpeed => 20f;
        protected virtual bool PhaseTwoStrong2UsesSickles => true;
        protected virtual float PhaseTwoStrong2SickleSpeed => 12f;
        protected virtual bool PhaseTwoScatterBodyBoltsEnabled => false;
        /// <summary>Scatter move: true when the body also lobs flaming scythes at the player.</summary>
        protected virtual bool PhaseTwoScatterBodyScytheEnabled => true;
        /// <summary>Scatter move: true when the boss keeps sliding onto the circle around the player.</summary>
        protected virtual bool PhaseTwoScatterMovesOnCircle => false;
        protected virtual float PhaseTwoReaperLaserSpreadDegrees => 20f;
        protected virtual float PhaseTwoCircleMoveMaxSpeed => 6f;
        protected virtual float PhaseTwoCircleMoveMinSpeed => 1.5f;
        protected virtual float PhaseTwoCircleAcceleration => 0.22f;
        protected virtual float PhaseTwoCircleDeceleration => 0.30f;
        protected virtual int PhaseOneSpecialCooldownTicks => 5 * 60;
        protected virtual int PhaseOneSpecialWindupTicks => 60;
        protected virtual int PhaseOneSpecialDashCount => 3;
        protected virtual int PhaseOneSpecialDashDurationTicks => 60;
        protected virtual int PhaseOneSpecialDashGapTicks => 15;
        protected virtual float PhaseOneSpecialDashSpeed => 20f;
        protected virtual int PhaseOneSpecialDashDecelStartTicks => 45;
        protected virtual float PhaseOneSpecialDashDeceleration => 0.9f;
        protected virtual int PhaseOneBoltCount => 5;
        protected virtual float PhaseOneBoltSpreadDegrees => 20f;
        protected virtual int SpecialAnimationFrameStart => 4;
        protected virtual int SpecialAnimationFrameCount => 4;
        protected virtual int NormalAnimationFrameCount => 4;
        protected virtual Color PhaseOneBoltColor => AuraColor;
        protected virtual Color LargeScytheWarningColor => new Color(255, 120, 35);
        protected virtual float AnimationFrameCounterStep => 0.25f;


        const int ScatterReaperModeBit = 1 << 17;

        enum PhaseOneSpecialState
        {
            Inactive,
            Windup,
            Dash,
            Pause
        }

        enum BossPhase
        {
            PhaseOne,
            Transition,
            PhaseTwo
        }

        enum PhaseTwoAttackState
        {
            Inactive,
            Windup,
            Dash,
            DashPause,
            DashRingReturn,
            ReaperMove,
            ScatterMove,
            WeakMove,
            WeakFlamingScythe,
            WeakStraightScythe,
            WeakInwardScythes,
            WeakSickleRush,
            StrongSickleDash,
            StrongReaperScytheRush,
            StrongDeathSquad,
            Intermission
        }

        int containmentRingAge;
        int containmentRingVfxTimer;
        int phaseOneSickleTimer;
        int phaseOneSpecialTimer;
        int phaseOneSpecialCooldown;
        int phaseOneSpecialDashIndex;
        int phaseTwoAttackTimer;
        int phaseTwoWeakBoltTimer;
        int phaseTwoDashIndex;
        int phaseTwoSequenceIndex;
        int phaseTwoLastStrongMove;
        int phaseTwoWeakBagMask;
        int phaseTwoLastWeakMove;
        int phaseTwoStrongBagMask;
        int phaseTransitionTimer;
        int phaseTransitionHealTimer;
        BossPhase bossPhase;
        PhaseOneSpecialState phaseOneSpecialState;
        Vector2 phaseOneSpecialDashDirection;
        PhaseTwoAttackState phaseTwoAttackState;
        Vector2 phaseTwoDashDirection;
        Vector2 phaseTwoReaperCenter;
        float phaseTwoWeak2AimAngle;
        float phaseTwoWeak3AimAngle;
        Vector4 phaseTwoWeak4WaveAngles;
        Vector4 phaseTwoStrong5WaveAngles;
        bool phaseTwoReaperClockwise;
        int phaseTwoScatterRotationSeed;
        int phaseTwoSickleNpcIndex;
        Vector2 phaseTwoSickleVelocity;
        Vector2 phaseTwoSickleSpawnPosition;
        Vector2 phaseTwoStrong4DashStart;
        Vector2 phaseTwoStrong4DashTarget;
        float phaseTwoSickleAngularVelocity;
        float phaseTwoSickleBaseRotation;
        Vector2 phaseTwoFixedRingCenter;
        int phaseTwoIntermissionDuration;
        float phaseTwoSickleSpawnAngle;
        int phaseTwoStrong4SpinDirection;
        bool containmentRingInitialized;
        bool containmentRingFollowsBoss;
        // Body shader glow: nothing in phase one; it fades in with the containment ring during the
        // transition, breathes at a baseline, and takes a 0.4s expand-then-contract pulse whenever
        // a strong move starts.
        const int BodyGlowFlashTicks = 24;
        const float BodyGlowFlashOpacity = 0.5f;
        protected virtual float BodyGlowBaselineOpacity => 0.45f;


        float strongMoveGlowOpacity;
        float bodyGlowFlashTimer = BodyGlowFlashTicks;
        bool bodyGlowStrongMoveWasActive;
        Vector2 containmentRingCenter;

        NPCDespawnHandler despawnHandler;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 8;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire3] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Venom] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Frostburn] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Frostburn2] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.ShadowFlame] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Ichor] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.CursedInferno] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][ModContent.BuffType<CrimsonBurn>()] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][ModContent.BuffType<DarkInferno>()] = true;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 10;
            NPC.aiStyle = -1;
            NPC.width = 100;
            NPC.height = 100;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.friendly = false;
            NPC.boss = true;
            NPC.noTileCollide = true;
            // Keep the boss silhouette and its arena VFX in front of tiles.
            NPC.behindTiles = false;
            NPC.noGravity = true;
            NPC.knockBackResist = 0;
            NPC.value = 150000;
            NPC.rarity = 21;

            bossPhase = BossPhase.PhaseOne;
            phaseOneSickleTimer = 0;
            phaseOneSpecialTimer = 0;
            phaseOneSpecialCooldown = PhaseOneSpecialCooldownTicks;
            phaseOneSpecialDashIndex = 0;
            phaseTransitionTimer = 0;
            phaseTransitionHealTimer = 0;
            phaseOneSpecialState = PhaseOneSpecialState.Inactive;
            phaseOneSpecialDashDirection = Vector2.UnitX;
            phaseTwoAttackState = PhaseTwoAttackState.Inactive;
            phaseTwoAttackTimer = 0;
            phaseTwoDashIndex = 0;
            phaseTwoSequenceIndex = 0;
            phaseTwoLastStrongMove = -1;
            phaseTwoWeakBagMask = 0;
            phaseTwoLastWeakMove = -1;
            phaseTwoStrongBagMask = 0;
            phaseTwoDashDirection = Vector2.UnitX;
            phaseTwoWeak2AimAngle = 0f;
            phaseTwoWeak3AimAngle = 0f;
            phaseTwoWeak4WaveAngles = Vector4.Zero;
            phaseTwoStrong5WaveAngles = Vector4.Zero;
            phaseTwoReaperCenter = Vector2.Zero;
            phaseTwoReaperClockwise = true;
            phaseTwoScatterRotationSeed = 0;
            phaseTwoSickleNpcIndex = -1;
            phaseTwoSickleVelocity = Vector2.Zero;
            phaseTwoSickleSpawnPosition = Vector2.Zero;
            phaseTwoStrong4DashStart = Vector2.Zero;
            phaseTwoStrong4DashTarget = Vector2.Zero;
            phaseTwoSickleAngularVelocity = 0f;
            phaseTwoSickleBaseRotation = 0f;
            phaseTwoFixedRingCenter = Vector2.Zero;
            phaseTwoIntermissionDuration = PhaseTwoIntermissionTicks;
            phaseTwoSickleSpawnAngle = 0f;
            phaseTwoStrong4SpinDirection = 1;
            containmentRingInitialized = false;
            strongMoveGlowOpacity = 0f;
            bodyGlowFlashTimer = BodyGlowFlashTicks;
            bodyGlowStrongMoveWasActive = false;
            containmentRingFollowsBoss = true;
            containmentRingCenter = Vector2.Zero;

            despawnHandler = new NPCDespawnHandler(
                LangUtils.GetTextValue("NPCs.Death.DespawnHandler"),
                DespawnTextColor,
                DespawnDustType);
        }

        public override void OnKill()
        {
            KillPhaseTwoSickle();
            ClearDeathReapers();
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            BlightBuildup.Apply(target);
        }

        void ClearDeathReapers()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC reaper = Main.npc[i];
                if (reaper.active && reaper.ModNPC is DeathReaper)
                {
                    reaper.active = false;
                }
            }
        }

        public override void AI()
        {
            ApplyWorldOverrides();
            LockContainmentRingWorldTime();

            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            NPC.netUpdate = false;

            Lighting.AddLight(NPC.Center, AuraColor.ToVector3() * 2f);

            int auraDust = Dust.NewDust(
                NPC.position,
                NPC.width,
                NPC.height,
                DustID.Shadowflame,
                NPC.velocity.X,
                NPC.velocity.Y,
                150,
                AuraDustColor,
                AuraDustScale);
            Main.dust[auraDust].noGravity = true;

            if (NPC.life > NPC.lifeMax * PhaseDustHealthThreshold)
            {
                int dust = Dust.NewDust(
                    NPC.position,
                    NPC.width,
                    NPC.height,
                    DustID.Shadowflame,
                    NPC.velocity.X,
                    NPC.velocity.Y,
                    180,
                    AuraDustColor,
                    HighHealthDustScale);
                Main.dust[dust].noGravity = true;
            }
            else if (NPC.life < NPC.lifeMax * PhaseDustHealthThreshold)
            {
                int dust = Dust.NewDust(
                    NPC.position,
                    NPC.width,
                    NPC.height,
                    DustID.Shadowflame,
                    NPC.velocity.X,
                    NPC.velocity.Y,
                    150,
                    AuraDustColor,
                    LowHealthDustScale);
                Main.dust[dust].noGravity = true;
            }

            UpdateContainmentRing();
            if (containmentRingInitialized && UsesContainmentRing)
            {
                containmentRingAge++;
                TickContainmentRingVisuals();
                ApplyContainmentPull();
            }

            switch (bossPhase)
            {
                case BossPhase.PhaseOne:
                    if (NPC.life <= NPC.lifeMax * PhaseOneEndHealthFraction)
                    {
                        StartPhaseTransition();
                    }
                    else
                    {
                        RunPhaseOneAI();
                    }
                    break;

                case BossPhase.Transition:
                    TickPhaseTransition();
                    break;

                case BossPhase.PhaseTwo:
                    ApplyPhaseTwoPlayerBuffs();
                    RunPhaseTwoAI();
                    break;
            }

            float glowTarget = GetPhaseTwoGlowTargetOpacity();
            strongMoveGlowOpacity = MathHelper.Lerp(strongMoveGlowOpacity, glowTarget, 0.035f);

            // Rising edge of a strong move restarts the expand-and-contract flash. It is read off
            // the synced attack state, so every client agrees without extra netcode.
            bool strongMoveActive = IsStrongMoveGlowActive;
            if (strongMoveActive && !bodyGlowStrongMoveWasActive)
            {
                bodyGlowFlashTimer = 0f;
            }

            bodyGlowStrongMoveWasActive = strongMoveActive;
            if (bodyGlowFlashTimer < BodyGlowFlashTicks)
            {
                bodyGlowFlashTimer++;
            }
        }

        void UpdateContainmentRing()
        {
            // Deliberate exception to the spawn-anchor rule: the ring follows the boss
            // during neutral phase-two play, but is anchored during Reaper formation.
            if (containmentRingInitialized && containmentRingFollowsBoss)
            {
                containmentRingCenter = NPC.Center;
            }
        }

        void ActivateContainmentRing()
        {
            if (!UsesContainmentRing || containmentRingInitialized)
            {
                return;
            }

            containmentRingInitialized = true;
            containmentRingAge = 0;
            containmentRingCenter = NPC.Center;
            LockContainmentRingWorldTime();
            NPC.netUpdate = true;
        }

        float GetContainmentRingProgress()
        {
            return MathHelper.Clamp(containmentRingAge / (float)ContainmentRingExpandTicks, 0f, 1f);
        }

        float GetContainmentRingRadius()
        {
            return MathHelper.SmoothStep(0f, ContainmentRingRadius, GetContainmentRingProgress());
        }

        bool IsStrongMoveGlowActive =>
            bossPhase == BossPhase.PhaseTwo
            && phaseTwoAttackState != PhaseTwoAttackState.Inactive
            && phaseTwoAttackState != PhaseTwoAttackState.Intermission
            && phaseTwoAttackState != PhaseTwoAttackState.WeakMove
            && phaseTwoAttackState != PhaseTwoAttackState.WeakFlamingScythe
            && phaseTwoAttackState != PhaseTwoAttackState.WeakStraightScythe
            && phaseTwoAttackState != PhaseTwoAttackState.WeakInwardScythes
            && phaseTwoAttackState != PhaseTwoAttackState.WeakSickleRush;

        float GetPhaseTwoGlowTargetOpacity()
        {
            if (bossPhase != BossPhase.PhaseTwo)
            {
                return 0f;
            }

            return IsStrongMoveGlowActive ? 1f : 0.2f;
        }

        /// <summary>
        /// 0 -> 1 -> 0 over BodyGlowFlashTicks: the glow swells out first, then contracts back.
        /// </summary>
        float GetBodyGlowFlash()
        {
            if (bodyGlowFlashTimer >= BodyGlowFlashTicks)
            {
                return 0f;
            }

            return (float)Math.Sin(MathHelper.Pi * bodyGlowFlashTimer / BodyGlowFlashTicks);
        }

        void StartPhaseTransition()
        {
            bossPhase = BossPhase.Transition;
            phaseTransitionTimer = 0;
            phaseTransitionHealTimer = 0;
            phaseOneSpecialState = PhaseOneSpecialState.Inactive;
            phaseOneSpecialTimer = 0;
            NPC.velocity = Vector2.Zero;
            NPC.dontTakeDamage = true;
            NPC.netUpdate = true;
        }

        void TickPhaseTransition()
        {
            NPC.velocity = Vector2.Zero;
            if (NPC.HasValidTarget)
            {
                FacePhaseOneTarget(Main.player[NPC.target]);
            }

            phaseTransitionTimer++;

            if (phaseTransitionTimer == PhaseTransitionRingStartTicks)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                ActivateContainmentRing();
            }

            bool healingWindow = phaseTransitionTimer >= PhaseTransitionRingStartTicks
                && phaseTransitionTimer < PhaseTransitionHealEndTicks;
            if (healingWindow && NPC.life < NPC.lifeMax)
            {
                phaseTransitionHealTimer++;
                if (phaseTransitionHealTimer >= PhaseTransitionHealInterval)
                {
                    phaseTransitionHealTimer = 0;
                    HealPhaseTransition();
                }
            }

            if (phaseTransitionTimer >= PhaseTransitionDurationTicks)
            {
                EndPhaseTransition();
            }
        }

        void HealPhaseTransition()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.life = Math.Min(NPC.life + PhaseTransitionHealAmount, NPC.lifeMax);
                NPC.netUpdate = true;
            }

            if (!Main.dedServ)
            {
                CombatText.NewText(NPC.Hitbox, CombatText.HealLife, PhaseTransitionHealAmount);
            }
        }

        void EndPhaseTransition()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.life = NPC.lifeMax;
            }

            bossPhase = BossPhase.PhaseTwo;
            phaseTransitionTimer = 0;
            phaseTransitionHealTimer = 0;
            NPC.dontTakeDamage = false;
            StartNextPhaseTwoMove();
            NPC.netUpdate = true;
        }

        void StartNextPhaseTwoMove()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            int move = phaseTwoSequenceIndex == 0
                ? 0
                : phaseTwoSequenceIndex == 1
                    ? PickRandomWeakMove()
                    : PickRandomStrongMove();

            phaseTwoSequenceIndex = (phaseTwoSequenceIndex + 1) % 3;

            switch (move)
            {
                case 0:
                    StartPhaseTwoWeakMove();
                    break;
                case 1:
                    StartPhaseTwoWeakFlamingScythe();
                    break;
                case 2:
                    StartPhaseTwoFiveDash();
                    break;
                case 3:
                    StartPhaseTwoReaperMove();
                    break;
                case 5:
                    StartPhaseTwoWeak3Move();
                    break;
                case 6:
                    StartPhaseTwoWeak4Move();
                    break;
                case 7:
                    StartPhaseTwoStrong4Move();
                    break;
                case 8:
                    StartPhaseTwoWeak5Move();
                    break;
                case 9:
                    StartPhaseTwoStrong5Move();
                    break;
                case 10:
                    StartPhaseTwoStrong6Move();
                    break;
                default:
                    StartPhaseTwoScatterMove();
                    break;
            }
        }

        int PickRandomWeakMove()
        {
            const int allWeakMoves = (1 << 1) | (1 << 5) | (1 << 6) | (1 << 8);
            int available = phaseTwoWeakBagMask;
            int lastMask = phaseTwoLastWeakMove >= 0 ? 1 << phaseTwoLastWeakMove : 0;

            if ((available & ~lastMask) == 0)
            {
                available = allWeakMoves;
            }

            available &= ~lastMask;

            int availableCount = 0;
            for (int move = 1; move <= 8; move++)
            {
                if ((available & (1 << move)) != 0)
                {
                    availableCount++;
                }
            }

            int selectedSlot = Main.rand.Next(availableCount);
            int selectedMove = 1;
            for (; selectedMove <= 8; selectedMove++)
            {
                if ((available & (1 << selectedMove)) == 0)
                {
                    continue;
                }

                if (selectedSlot == 0)
                {
                    break;
                }

                selectedSlot--;
            }

            phaseTwoWeakBagMask = available & ~(1 << selectedMove);
            phaseTwoLastWeakMove = selectedMove;
            return selectedMove;
        }

        int PickRandomStrongMove()
        {
            int allStrongMoves = StrongMoveMask;
            int available = phaseTwoStrongBagMask;
            int lastMask = phaseTwoLastStrongMove >= 0 ? 1 << phaseTwoLastStrongMove : 0;

            if ((available & ~lastMask) == 0)
            {
                available = allStrongMoves;
            }

            available &= ~lastMask;

            int availableCount = 0;
            for (int move = 2; move <= 10; move++)
            {
                if ((available & (1 << move)) != 0)
                {
                    availableCount++;
                }
            }

            int selectedSlot = Main.rand.Next(availableCount);
            int selectedMove = 2;
            for (; selectedMove <= 10; selectedMove++)
            {
                if ((available & (1 << selectedMove)) == 0)
                {
                    continue;
                }

                if (selectedSlot == 0)
                {
                    break;
                }

                selectedSlot--;
            }

            phaseTwoStrongBagMask = available & ~(1 << selectedMove);
            phaseTwoLastStrongMove = selectedMove;
            return selectedMove;
        }

        void StartPhaseTwoReaperMove()
        {
            PlayStrongMoveRoar();
            phaseTwoAttackState = PhaseTwoAttackState.ReaperMove;
            phaseTwoAttackTimer = 0;
            phaseTwoReaperCenter = NPC.Center;
            NPC.velocity = Vector2.Zero;
            containmentRingFollowsBoss = false;
            containmentRingCenter = phaseTwoReaperCenter;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                phaseTwoReaperClockwise = Main.rand.NextBool();
                SpawnPhaseTwoReaperFormation(phaseTwoReaperCenter, phaseTwoReaperClockwise);
            }

            NPC.netUpdate = true;
        }

        void PlayStrongMoveRoar()
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
        }

        void SpawnPhaseTwoReaperFormation(Vector2 center, bool clockwise)
        {
            int count = PhaseTwoReaperCount;
            for (int slot = 0; slot < count; slot++)
            {
                int packedSlot = (count << 8) | slot;
                if (clockwise)
                {
                    packedSlot |= 1 << 16;
                }

                NPC.NewNPC(
                    NPC.GetSource_FromAI(),
                    (int)center.X,
                    (int)center.Y,
                    ModContent.NPCType<DeathReaper>(),
                    ai0: NPC.whoAmI,
                    ai1: packedSlot,
                    ai2: center.X,
                    ai3: center.Y);
            }
        }

        void TickPhaseTwoReaperMove(Player target)
        {
            phaseTwoAttackTimer++;
            FacePhaseOneTarget(target);
            if (PhaseTwoStrong2MovesOnCircle)
            {
                MovePhaseTwoOnCircle(target, PhaseTwoStrong5CircleMoveMaxSpeed);
            }

            if (PhaseTwoStrong2BodyBoltsEnabled
                && phaseTwoAttackTimer >= PhaseTwoBodyBoltStartTicks
                && phaseTwoAttackTimer < PhaseTwoReaperBoltStopTicks
                && (phaseTwoAttackTimer - PhaseTwoBodyBoltStartTicks) % PhaseTwoReaperBoltIntervalTicks == 0)
            {
                SpawnPhaseTwoReaperBoltVolley(target);
            }

            int ringReturnStart = PhaseTwoReaperMoveDurationTicks - 60;
            if (phaseTwoAttackTimer < ringReturnStart)
            {
                containmentRingCenter = phaseTwoReaperCenter;
            }
            else
            {
                float progress = MathHelper.Clamp(
                    (phaseTwoAttackTimer - ringReturnStart) / 60f,
                    0f,
                    1f);
                containmentRingCenter = Vector2.Lerp(
                    phaseTwoReaperCenter,
                    NPC.Center,
                    MathHelper.SmoothStep(0f, 1f, progress));
            }

            if (phaseTwoAttackTimer >= PhaseTwoReaperMoveDurationTicks)
            {
                containmentRingFollowsBoss = true;
                containmentRingCenter = NPC.Center;
                BeginPhaseTwoIntermission();
            }
        }

        void StartPhaseTwoWeakMove()
        {
            phaseTwoAttackState = PhaseTwoAttackState.WeakMove;
            phaseTwoAttackTimer = 0;
            phaseTwoWeakBoltTimer = 0;
            phaseOneSickleTimer = 0;
            NPC.velocity = Vector2.Zero;
            containmentRingFollowsBoss = true;
            NPC.netUpdate = true;
        }

        void TickPhaseTwoWeakMove(Player target)
        {
            phaseTwoAttackTimer++;
            MovePhaseTwoChase(target);

            phaseOneSickleTimer++;
            if (phaseOneSickleTimer >= PhaseOneSickleInterval)
            {
                phaseOneSickleTimer = 0;
                SpawnPhaseOneDeathSickle(target);
            }

            phaseTwoWeakBoltTimer++;
            if (phaseTwoWeakBoltTimer >= GetPhaseTwoWeakBoltIntervalTicks())
            {
                phaseTwoWeakBoltTimer = 0;
                SpawnPhaseTwoWeakBolt(target);
            }

            if (phaseTwoAttackTimer >= PhaseTwoWeakDurationTicks)
            {
                BeginPhaseTwoIntermission();
            }
        }

        int GetPhaseTwoWeakBoltIntervalTicks()
        {
            float healthFraction = MathHelper.Clamp(NPC.life / (float)NPC.lifeMax, 0f, 1f);
            return (int)Math.Round(MathHelper.Lerp(PhaseTwoWeakBoltMinIntervalTicks, PhaseTwoWeakBoltMaxIntervalTicks, healthFraction));
        }

        void SpawnPhaseTwoWeakBolt(Player target)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Vector2 direction = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitX);
            Projectile.NewProjectile(
                NPC.GetSource_FromAI(),
                NPC.Center,
                direction * PhaseTwoWeakBoltSpeed,
                ModContent.ProjectileType<DeathBolt>(),
                BoltDamage,
                0f,
                Main.myPlayer,
                direction.X,
                direction.Y,
                UsefulFunctions.ColorToFloat(PhaseOneBoltColor));
        }

        void StartPhaseTwoWeakFlamingScythe()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int count = PhaseTwoWeak2ScytheCount;
                float step = PhaseTwoWeak2FullCircle
                    ? MathHelper.TwoPi / count
                    : MathHelper.ToRadians(PhaseTwoWeak2ScytheSpreadDegrees);
                float centerOffset = (count - 1) * 0.5f;

                for (int i = 0; i < count; i++)
                {
                    float aimOffset = PhaseTwoWeak2FullCircle
                        ? i * step
                        : (i - centerOffset) * step;
                    DeathLaserWarning.Spawn(
                        NPC.GetSource_FromAI(),
                        NPC,
                        LargeScytheWarningColor,
                        trackTicks: PhaseTwoWeak2WarningTicks,
                        totalTicks: PhaseTwoWeak2ThrowTicks,
                        beamLength: PhaseTwoWeak2WarningLength,
                        beamSize: PhaseTwoWeak2WarningSize,
                        beamBrightness: PhaseTwoWeak2WarningBrightness,
                        aimOffsetRadians: aimOffset);
                }
            }
            phaseTwoAttackState = PhaseTwoAttackState.WeakFlamingScythe;
            phaseTwoAttackTimer = 0;
            phaseTwoWeak2AimAngle = 0f;
            NPC.velocity = Vector2.Zero;
            NPC.netUpdate = true;
        }

        void TickPhaseTwoWeakFlamingScythe(Player target)
        {
            phaseTwoAttackTimer++;
            FacePhaseOneTarget(target);

            if (phaseTwoAttackTimer <= PhaseTwoWeak2WarningTicks)
            {
                phaseTwoWeak2AimAngle = (target.Center - NPC.Center).ToRotation();
            }

            if (phaseTwoAttackTimer == PhaseTwoWeak2WarningTicks
                && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SpawnPhaseTwoWeak2Phantoms();
            }

            if (phaseTwoAttackTimer == PhaseTwoWeak2ThrowTicks - DeathSpawnWarning.FlamingScytheWarningTicks
                && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int count = PhaseTwoWeak2ScytheCount;
                float step = PhaseTwoWeak2FullCircle
                    ? MathHelper.TwoPi / count
                    : MathHelper.ToRadians(PhaseTwoWeak2ScytheSpreadDegrees);
                float centerOffset = (count - 1) * 0.5f;

                for (int i = 0; i < count; i++)
                {
                    float aimOffset = PhaseTwoWeak2FullCircle
                        ? i * step
                        : (i - centerOffset) * step;
                    Vector2 direction = (phaseTwoWeak2AimAngle + aimOffset).ToRotationVector2();
                    DeathSpawnWarning.SpawnFlamingScythe(
                        NPC.GetSource_FromAI(),
                        NPC.Center,
                        direction,
                        EnemyDamage.Projectile(GiantScytheDamage),
                        UsefulFunctions.ColorToFloat(new Color(255, 120, 35)),
                        playReleaseSound: i == 0);
                }
            }

            if (phaseTwoAttackTimer >= PhaseTwoWeak2DurationTicks)
            {
                BeginPhaseTwoIntermission();
            }
        }

        void StartPhaseTwoWeak3Move()
        {
            phaseTwoAttackState = PhaseTwoAttackState.WeakStraightScythe;
            phaseTwoAttackTimer = 0;
            phaseTwoWeak3AimAngle = 0f;
            NPC.velocity = Vector2.Zero;
            containmentRingFollowsBoss = true;
            NPC.netUpdate = true;
        }

        void TickPhaseTwoWeak3Move(Player target)
        {
            phaseTwoAttackTimer++;
            NPC.velocity = Vector2.Zero;
            FacePhaseOneTarget(target);

            if (phaseTwoAttackTimer == 1)
            {
                phaseTwoWeak3AimAngle = (target.Center - NPC.Center).ToRotation();
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    SpawnPhaseTwoWeak3Warnings();
                }
            }

            int phantomSpawnTick = Math.Max(1, PhaseTwoWeak3ReleaseTicks - PhaseTwoWeak3PhantomLeadTicks);
            if (phaseTwoAttackTimer == phantomSpawnTick && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SpawnPhaseTwoWeak3Phantoms();
            }

            if (phaseTwoAttackTimer == PhaseTwoWeak3ReleaseTicks
                && Main.netMode != NetmodeID.MultiplayerClient)
            {
                PlayDeathSickleUseSound();
                SpawnPhaseTwoWeak3Scythes();
            }

            if (phaseTwoAttackTimer >= PhaseTwoWeak3DurationTicks)
            {
                BeginPhaseTwoIntermission();
            }
        }


        void SpawnPhaseTwoWeak2Phantoms()
        {
            if (!LargeScythePhantomsEnabled)
            {
                return;
            }
            int count = PhaseTwoWeak2ScytheCount;
            float step = PhaseTwoWeak2FullCircle
                ? MathHelper.TwoPi / count
                : MathHelper.ToRadians(PhaseTwoWeak2ScytheSpreadDegrees);
            float centerOffset = (count - 1) * 0.5f;

            for (int i = 0; i < count; i++)
            {
                float aimOffset = PhaseTwoWeak2FullCircle
                    ? i * step
                    : (i - centerOffset) * step;
                DeathFlamingScythePhantom.Spawn(
                    NPC.GetSource_FromAI(),
                    NPC.Center,
                    (phaseTwoWeak2AimAngle + aimOffset).ToRotationVector2(),
                    lifetimeTicks: 30);
            }
        }

        void SpawnPhaseTwoWeak3Phantoms()
        {
            if (!LargeScythePhantomsEnabled)
            {
                return;
            }
            int count = PhaseTwoWeak3LineCount;
            float centerOffset = (count - 1) * 0.5f;
            Vector2 direction = phaseTwoWeak3AimAngle.ToRotationVector2();
            Vector2 behind = NPC.Center - direction * PhaseTwoWeak3BehindDistance;
            Vector2 perpendicular = direction.RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < count; i++)
            {
                Vector2 position = behind + perpendicular * ((i - centerOffset) * PhaseTwoWeak3LineSpacing);
                DeathFlamingScythePhantom.Spawn(
                    NPC.GetSource_FromAI(),
                    position,
                    direction,
                    lifetimeTicks: 120,
                    movementMode: DeathFlamingScythe.MovementMode.Straight,
                    speed: PhaseTwoWeak3ScytheSpeed);
            }

            int reverseCount = PhaseTwoWeak3ReverseLineCount;
            float reverseCenterOffset = (reverseCount - 1) * 0.5f;
            Vector2 front = NPC.Center + direction * PhaseTwoWeak3BehindDistance;
            for (int i = 0; i < reverseCount; i++)
            {
                Vector2 position = front + perpendicular * ((i - reverseCenterOffset) * PhaseTwoWeak3LineSpacing);
                DeathFlamingScythePhantom.Spawn(
                    NPC.GetSource_FromAI(),
                    position,
                    -direction,
                    lifetimeTicks: 120,
                    movementMode: DeathFlamingScythe.MovementMode.Straight,
                    speed: PhaseTwoWeak3ScytheSpeed);
            }
        }

        void SpawnPhaseTwoWeak3Warnings()
        {
            int count = PhaseTwoWeak3LineCount;
            float centerOffset = (count - 1) * 0.5f;
            Vector2 direction = phaseTwoWeak3AimAngle.ToRotationVector2();
            Vector2 behind = NPC.Center - direction * PhaseTwoWeak3BehindDistance;
            Vector2 perpendicular = direction.RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < count; i++)
            {
                Vector2 position = behind + perpendicular * ((i - centerOffset) * PhaseTwoWeak3LineSpacing);
                DeathLaserWarning.Spawn(
                    NPC.GetSource_FromAI(),
                    NPC,
                    LargeScytheWarningColor,
                    trackTicks: 0,
                    totalTicks: PhaseTwoWeak3ReleaseTicks,
                    beamLength: PhaseTwoWeak3WarningLength,
                    beamSize: PhaseTwoWeak3WarningSize,
                    beamBrightness: PhaseTwoWeak3WarningBrightness,
                    origin: position,
                    direction: direction,
                    followHost: false);
            }

            int reverseCount = PhaseTwoWeak3ReverseLineCount;
            float reverseCenterOffset = (reverseCount - 1) * 0.5f;
            Vector2 front = NPC.Center + direction * PhaseTwoWeak3BehindDistance;
            for (int i = 0; i < reverseCount; i++)
            {
                Vector2 position = front + perpendicular * ((i - reverseCenterOffset) * PhaseTwoWeak3LineSpacing);
                DeathLaserWarning.Spawn(
                    NPC.GetSource_FromAI(),
                    NPC,
                    LargeScytheWarningColor,
                    trackTicks: 0,
                    totalTicks: PhaseTwoWeak3ReleaseTicks,
                    beamLength: PhaseTwoWeak3WarningLength,
                    beamSize: PhaseTwoWeak3WarningSize,
                    beamBrightness: PhaseTwoWeak3WarningBrightness,
                    origin: position,
                    direction: -direction,
                    followHost: false);
            }
        }

        void SpawnPhaseTwoWeak3Scythes()
        {
            int count = PhaseTwoWeak3LineCount;
            float centerOffset = (count - 1) * 0.5f;
            Vector2 direction = phaseTwoWeak3AimAngle.ToRotationVector2();
            Vector2 behind = NPC.Center - direction * PhaseTwoWeak3BehindDistance;
            Vector2 perpendicular = direction.RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < count; i++)
            {
                Vector2 position = behind + perpendicular * ((i - centerOffset) * PhaseTwoWeak3LineSpacing);
                Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    position,
                    direction * PhaseTwoWeak3ScytheSpeed,
                    ModContent.ProjectileType<DeathFlamingScythe>(),
                    EnemyDamage.Projectile(GiantScytheDamage),
                    0f,
                    Main.myPlayer,
                    position.X,
                    position.Y,
                    (float)DeathFlamingScythe.MovementMode.Straight);
            }

            int reverseCount = PhaseTwoWeak3ReverseLineCount;
            float reverseCenterOffset = (reverseCount - 1) * 0.5f;
            Vector2 front = NPC.Center + direction * PhaseTwoWeak3BehindDistance;
            for (int i = 0; i < reverseCount; i++)
            {
                Vector2 position = front + perpendicular * ((i - reverseCenterOffset) * PhaseTwoWeak3LineSpacing);
                Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    position,
                    -direction * PhaseTwoWeak3ScytheSpeed,
                    ModContent.ProjectileType<DeathFlamingScythe>(),
                    EnemyDamage.Projectile(GiantScytheDamage),
                    0f,
                    Main.myPlayer,
                    position.X,
                    position.Y,
                    (float)DeathFlamingScythe.MovementMode.Straight);
            }
        }

        void StartPhaseTwoWeak4Move()
        {
            phaseTwoAttackState = PhaseTwoAttackState.WeakInwardScythes;
            phaseTwoAttackTimer = 0;
            NPC.velocity = Vector2.Zero;
            containmentRingFollowsBoss = true;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                phaseTwoWeak4WaveAngles = new Vector4(
                    Main.rand.NextFloat(MathHelper.TwoPi),
                    Main.rand.NextFloat(MathHelper.TwoPi),
                    Main.rand.NextFloat(MathHelper.TwoPi),
                    Main.rand.NextFloat(MathHelper.TwoPi));
                SpawnPhaseTwoWeak4Warnings(0);
            }
            NPC.netUpdate = true;
        }

        void TickPhaseTwoWeak4Move(Player target)
        {
            phaseTwoAttackTimer++;
            NPC.velocity = Vector2.Zero;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int phantomLeadTicks = PhaseTwoWeak4ReleaseTicks - PhaseTwoWeak4WarningTicks;
                for (int wave = 0; wave < PhaseTwoWeak4VolleyCount; wave++)
                {
                    int releaseTick = PhaseTwoWeak4ReleaseTicks + wave * PhaseTwoWeak4VolleyIntervalTicks;
                    int warningStartTick = wave == 0 ? 0 : releaseTick - PhaseTwoWeak4WarningTicks;
                    int phantomTick = releaseTick - phantomLeadTicks;

                    if (phaseTwoAttackTimer == warningStartTick)
                    {
                        SpawnPhaseTwoWeak4Warnings(wave);
                    }

                    if (LargeScythePhantomsEnabled && phaseTwoAttackTimer == phantomTick)
                    {
                        SpawnPhaseTwoWeak4Phantoms(wave);
                    }

                    if (phaseTwoAttackTimer == releaseTick)
                    {
                        PlayDeathSickleUseSound();
                        SpawnPhaseTwoWeak4Scythes(wave);
                    }
                }
            }

            if (phaseTwoAttackTimer >= PhaseTwoWeak4DurationTicks)
            {
                BeginPhaseTwoIntermission();
            }
        }

        float GetPhaseTwoWeak4WaveAngle(int wave)
        {
            return wave switch
            {
                0 => phaseTwoWeak4WaveAngles.X,
                1 => phaseTwoWeak4WaveAngles.Y,
                2 => phaseTwoWeak4WaveAngles.Z,
                _ => phaseTwoWeak4WaveAngles.W
            };
        }

        void SpawnPhaseTwoWeak4Warnings(int wave)
        {
            float baseAngle = GetPhaseTwoWeak4WaveAngle(wave);
            int count = PhaseTwoWeak4LineCount;
            float step = MathHelper.TwoPi / count;
            for (int i = 0; i < count; i++)
            {
                Vector2 radial = (baseAngle + i * step).ToRotationVector2();
                Vector2 position = NPC.Center + radial * PhaseTwoWeak4Radius;
                DeathLaserWarning.Spawn(
                    NPC.GetSource_FromAI(),
                    NPC,
                    LargeScytheWarningColor,
                    trackTicks: 0,
                    totalTicks: PhaseTwoWeak4WarningTicks,
                    beamLength: PhaseTwoWeak4WarningLength,
                    beamSize: PhaseTwoWeak4WarningSize,
                    beamBrightness: PhaseTwoWeak4WarningBrightness,
                    origin: position,
                    direction: -radial,
                    followHost: false);
            }
        }

        void SpawnPhaseTwoWeak4Phantoms(int wave)
        {
            float baseAngle = GetPhaseTwoWeak4WaveAngle(wave);
            int count = PhaseTwoWeak4LineCount;
            float step = MathHelper.TwoPi / count;
            for (int i = 0; i < count; i++)
            {
                Vector2 radial = (baseAngle + i * step).ToRotationVector2();
                Vector2 position = NPC.Center + radial * PhaseTwoWeak4Radius;
                DeathFlamingScythePhantom.Spawn(
                    NPC.GetSource_FromAI(),
                    position,
                    -radial,
                    lifetimeTicks: 120,
                    movementMode: DeathFlamingScythe.MovementMode.Straight,
                    speed: PhaseTwoWeak3ScytheSpeed);
            }
        }

        void SpawnPhaseTwoWeak4Scythes(int wave)
        {
            float baseAngle = GetPhaseTwoWeak4WaveAngle(wave);
            int count = PhaseTwoWeak4LineCount;
            float step = MathHelper.TwoPi / count;
            for (int i = 0; i < count; i++)
            {
                Vector2 radial = (baseAngle + i * step).ToRotationVector2();
                Vector2 position = NPC.Center + radial * PhaseTwoWeak4Radius;
                Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    position,
                    -radial * PhaseTwoWeak3ScytheSpeed,
                    ModContent.ProjectileType<DeathFlamingScythe>(),
                    EnemyDamage.Projectile(GiantScytheDamage),
                    0f,
                    Main.myPlayer,
                    position.X,
                    position.Y,
                    (float)DeathFlamingScythe.MovementMode.Straight);
            }
        }

        void StartPhaseTwoWeak5Move()
        {
            phaseTwoAttackState = PhaseTwoAttackState.WeakSickleRush;
            phaseTwoAttackTimer = 0;
            phaseTwoSickleVelocity = Vector2.Zero;
            phaseTwoSickleAngularVelocity = 0f;
            phaseTwoSickleBaseRotation = 0f;
            containmentRingFollowsBoss = false;
            phaseTwoFixedRingCenter = containmentRingCenter;
            NPC.velocity = Vector2.Zero;

            Player target = Main.player[NPC.target];
            int playerFacing = target.direction == 0 ? 1 : target.direction;
            Vector2 rearLowerDirection = new Vector2(-playerFacing, 1f).SafeNormalize(new Vector2(-playerFacing, 1f));
            NPC.Center = target.Center + rearLowerDirection * PhaseTwoWeak5TeleportDistance;
            FacePhaseOneTarget(target);
            NPC.netUpdate = true;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 sickleOffset = SickleAnchorOffset;
                SpawnPhaseTwoSickle(NPC.Center + sickleOffset, 0f, DeathSickleWeapon.MovementMode.EndPivot);
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
        }

        void TickPhaseTwoWeak5Move(Player target)
        {
            phaseTwoAttackTimer++;
            int elapsed = phaseTwoAttackTimer - 1;
            int warningStart = PhaseTwoWeak5FadeInTicks + PhaseTwoWeak5ChargeRotateTicks + PhaseTwoWeak5ChargeHoldTicks;
            int rushStart = warningStart + PhaseTwoWeak5WarningTicks;
            int rushTicks = PhaseTwoWeak5SwingCycleTicks * PhaseTwoWeak5SwingCycles;
            int fadeOutStart = rushStart + rushTicks;
            int totalTicks = fadeOutStart + PhaseTwoWeak5RecoveryTicks;
            Vector2 sickleOffset = SickleAnchorOffset;
            float rushSpeed = PhaseTwoWeak5RushDistance / Math.Max(1, rushTicks);
            int facing = NPC.direction == 0 ? 1 : NPC.direction;
            // Start the wind-up opposite the sprite-facing side so the blade sweeps inward.
            float chargeRotation = -MathHelper.PiOver4 * facing;
            float spinDirection = facing;

            if (elapsed < PhaseTwoWeak5FadeInTicks)
            {
                NPC.velocity = Vector2.Zero;
                FacePhaseOneTarget(target);
                float opacity = (elapsed + 1) / (float)PhaseTwoWeak5FadeInTicks;
                SetPhaseTwoSicklePose(NPC.Center + sickleOffset, 0f, NPC.direction, opacity, 0);
                return;
            }

            if (elapsed < PhaseTwoWeak5FadeInTicks + PhaseTwoWeak5ChargeRotateTicks)
            {
                NPC.velocity = Vector2.Zero;
                FacePhaseOneTarget(target);
                int rotateElapsed = elapsed - PhaseTwoWeak5FadeInTicks;
                float progress = MathHelper.SmoothStep(0f, 1f, rotateElapsed / (float)PhaseTwoWeak5ChargeRotateTicks);
                phaseTwoSickleBaseRotation = MathHelper.Lerp(0f, chargeRotation, progress);
                SetPhaseTwoSicklePose(NPC.Center + sickleOffset, phaseTwoSickleBaseRotation, NPC.direction, 1f, GiantScytheDamage);
                return;
            }

            if (elapsed < warningStart)
            {
                NPC.velocity = Vector2.Zero;
                FacePhaseOneTarget(target);
                phaseTwoSickleBaseRotation = chargeRotation;
                SetPhaseTwoSicklePose(NPC.Center + sickleOffset, phaseTwoSickleBaseRotation, NPC.direction, 1f, GiantScytheDamage);
                return;
            }

            if (elapsed < rushStart)
            {
                NPC.velocity = Vector2.Zero;
                if (elapsed == warningStart)
                {
                    phaseTwoDashDirection = UsefulFunctions.Aim(NPC.Center, target.Center, 1f);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        DeathLaserWarning.Spawn(
                            NPC.GetSource_FromAI(),
                            NPC,
                            AuraColor,
                            trackTicks: 0,
                            totalTicks: PhaseTwoWeak5WarningTicks,
                            beamLength: PhaseTwoWeak5WarningLength,
                            beamSize: 0.18f,
                            beamBrightness: 1.15f,
                            origin: NPC.Center,
                            direction: phaseTwoDashDirection,
                            followHost: false);
                    }
                }

                phaseTwoSickleBaseRotation = chargeRotation;
                SetPhaseTwoSicklePose(NPC.Center + sickleOffset, phaseTwoSickleBaseRotation, NPC.direction, 1f, GiantScytheDamage);
                return;
            }

            if (elapsed < fadeOutStart)
            {
                int rushElapsed = elapsed - rushStart;
                if (rushElapsed == 0)
                {
                    NPC.direction = phaseTwoDashDirection.X < 0f ? -1 : 1;
                    NPC.spriteDirection = NPC.direction;
                    facing = NPC.direction;
                    chargeRotation = -MathHelper.PiOver4 * facing;
                    spinDirection = facing;
                    phaseTwoSickleBaseRotation = chargeRotation;
                }

                NPC.velocity = phaseTwoDashDirection * rushSpeed;
                if (rushElapsed % PhaseTwoWeak5SwingCycleTicks == 0)
                {
                    PlayDeathSickleUseSound();
                }

                float cycles = rushElapsed / (float)PhaseTwoWeak5SwingCycleTicks;
                float rotation = phaseTwoSickleBaseRotation + spinDirection * MathHelper.TwoPi * cycles;
                SetPhaseTwoSicklePose(NPC.Center + sickleOffset, rotation, facing, 1f, GiantScytheDamage);
                return;
            }

            float fadeProgress = MathHelper.Clamp(
                (elapsed - fadeOutStart) / (float)(PhaseTwoWeak5SwingCycleTicks / 2), 0f, 1f);
            float ringReturnProgress = MathHelper.Clamp(
                (elapsed - fadeOutStart) / (float)PhaseTwoWeak5RecoveryTicks, 0f, 1f);
            if (elapsed == fadeOutStart)
            {
                NPC.velocity = Vector2.Zero;
            }
            // Stop dealing contact damage while the boss brakes back into the player orbit.
            NPC.damage = 0;
            MovePhaseTwoWeak5Return(target);
            float finalRotation = phaseTwoSickleBaseRotation
                + spinDirection * MathHelper.TwoPi * PhaseTwoWeak5SwingCycles
                + spinDirection * MathHelper.Pi * fadeProgress;
            SetPhaseTwoSicklePose(NPC.Center + sickleOffset, finalRotation, facing, 1f - fadeProgress, 0);
            // Follow Death during its return; the ring must not stick to the player.
            containmentRingCenter = Vector2.Lerp(
                phaseTwoFixedRingCenter,
                NPC.Center,
                MathHelper.SmoothStep(0f, 1f, ringReturnProgress));

            if (elapsed >= totalTicks)
            {
                KillPhaseTwoSickle();
                containmentRingFollowsBoss = true;
                containmentRingCenter = NPC.Center;
                // Only restore contact damage once the recovery motion has finished.
                NPC.damage = BodyContactDamage;
                BeginPhaseTwoIntermission(0);
            }
        }

        void StartPhaseTwoStrong4Move()
        {
            PlayStrongMoveRoar();
            phaseTwoAttackState = PhaseTwoAttackState.StrongSickleDash;
            phaseTwoAttackTimer = 0;
            phaseTwoDashIndex = 0;
            phaseTwoSickleVelocity = Vector2.Zero;
            phaseTwoSickleAngularVelocity = 0f;
            containmentRingFollowsBoss = false;
            phaseTwoFixedRingCenter = containmentRingCenter;
            NPC.velocity = Vector2.Zero;

            phaseTwoSickleSpawnAngle = Main.rand.NextFloat(MathHelper.TwoPi);
            phaseTwoSickleSpawnPosition = NPC.Center + SickleAnchorOffset + phaseTwoSickleSpawnAngle.ToRotationVector2() * PhaseTwoStrong4SpawnRadius;
            phaseTwoSickleBaseRotation = Main.rand.NextFloat(MathHelper.TwoPi);
            phaseTwoStrong4SpinDirection = Main.rand.NextBool() ? 1 : -1;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                SpawnPhaseTwoSickle(phaseTwoSickleSpawnPosition, phaseTwoSickleBaseRotation, DeathSickleWeapon.MovementMode.MiddlePivot);
            }

            NPC.netUpdate = true;
        }

        void TickPhaseTwoStrong4Move(Player target)
        {
            phaseTwoAttackTimer++;
            int elapsed = phaseTwoAttackTimer - 1;
            int dashCycleTicks = PhaseTwoStrong4DashTicks + PhaseTwoStrong4GapTicks;
            int dashPhaseTicks = PhaseTwoStrong4DashCount * PhaseTwoStrong4DashTicks + (PhaseTwoStrong4DashCount - 1) * PhaseTwoStrong4GapTicks;
            int fadeOutStart = PhaseTwoStrong4FadeInTicks + dashPhaseTicks;
            int totalTicks = fadeOutStart + PhaseTwoStrong4FadeOutTicks + PhaseTwoStrong4PostWaitTicks;
            DeathSickleWeapon sickle = GetPhaseTwoSickle();

            if (elapsed < PhaseTwoStrong4FadeInTicks)
            {
                NPC.velocity = Vector2.Zero;
                FacePhaseOneTarget(target);
                float progress = (elapsed + 1) / (float)PhaseTwoStrong4FadeInTicks;
                float rotation = phaseTwoSickleBaseRotation + phaseTwoStrong4SpinDirection * 0.35f * progress;
                SetPhaseTwoSicklePose(phaseTwoSickleSpawnPosition, rotation, 1, progress, 0);
                return;
            }

            FacePhaseOneTarget(target);
            containmentRingCenter = phaseTwoFixedRingCenter;

            if (elapsed < fadeOutStart)
            {
                MovePhaseTwoOnCircle(target);
                int activeTick = elapsed - PhaseTwoStrong4FadeInTicks;
                int dashIndex = activeTick / dashCycleTicks;
                int dashTick = activeTick % dashCycleTicks;
                bool dashing = dashTick < PhaseTwoStrong4DashTicks;

                if (sickle != null && dashing && dashTick == 0)
                {
                    phaseTwoDashDirection = (target.Center - sickle.NPC.Center).SafeNormalize(Vector2.UnitX);
                    phaseTwoStrong4DashStart = sickle.NPC.Center;
                    float dashDistance = MathHelper.Clamp(
                        Vector2.Distance(phaseTwoStrong4DashStart, target.Center) + PhaseTwoStrong4DashOvershoot,
                        PhaseTwoStrong4DashMinDistance,
                        PhaseTwoStrong4DashMaxDistance);
                    phaseTwoStrong4DashTarget = phaseTwoStrong4DashStart + phaseTwoDashDirection * dashDistance;
                    phaseTwoSickleBaseRotation = sickle.NPC.rotation;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        phaseTwoStrong4SpinDirection = Main.rand.NextBool() ? 1 : -1;
                    }
                    phaseTwoSickleAngularVelocity = phaseTwoStrong4SpinDirection * 0.08f;
                    PlayDeathSickleUseSound();

                }

                if (sickle != null)
                {
                    if (dashing)
                    {
                        float dashProgress = (dashTick + 1) / (float)PhaseTwoStrong4DashTicks;
                        float eased = MathHelper.SmoothStep(0f, 1f, dashProgress);
                        Vector2 previousCenter = sickle.NPC.Center;
                        sickle.NPC.Center = Vector2.Lerp(phaseTwoStrong4DashStart, phaseTwoStrong4DashTarget, eased);
                        phaseTwoSickleVelocity = sickle.NPC.Center - previousCenter;
                        sickle.NPC.rotation = phaseTwoSickleBaseRotation
                            + phaseTwoStrong4SpinDirection * MathHelper.TwoPi * eased;
                    }
                    else
                    {
                        sickle.NPC.Center += phaseTwoSickleVelocity;
                        phaseTwoSickleVelocity *= 0.90f;
                        sickle.NPC.rotation += phaseTwoSickleAngularVelocity;
                        phaseTwoSickleAngularVelocity *= 0.92f;
                    }
                    sickle.SetPose(sickle.NPC.Center, sickle.NPC.rotation, 1, 1f, GiantScytheDamage);
                }
                NPC.netUpdate = true;
                return;
            }

            int fadeTick = elapsed - fadeOutStart;
            if (fadeTick < PhaseTwoStrong4FadeOutTicks)
            {
                MovePhaseTwoOnCircle(target);
                float progress = (fadeTick + 1) / (float)PhaseTwoStrong4FadeOutTicks;
                if (sickle != null)
                {
                    sickle.NPC.Center += phaseTwoSickleVelocity;
                    phaseTwoSickleVelocity *= 0.90f;
                    sickle.NPC.rotation += phaseTwoStrong4SpinDirection * 0.05f;
                    sickle.SetPose(sickle.NPC.Center, sickle.NPC.rotation, 1, 1f - progress, 0);
                }

                containmentRingCenter = Vector2.Lerp(
                    phaseTwoFixedRingCenter,
                    NPC.Center,
                    MathHelper.SmoothStep(0f, 1f, progress));
                return;
            }

            NPC.velocity = Vector2.Zero;
            containmentRingFollowsBoss = true;
            containmentRingCenter = NPC.Center;
            KillPhaseTwoSickle();

            if (elapsed >= totalTicks)
            {
                BeginPhaseTwoIntermission(0);
            }
        }

        void StartPhaseTwoStrong5Move()
        {
            PlayStrongMoveRoar();
            phaseTwoAttackState = PhaseTwoAttackState.StrongReaperScytheRush;
            phaseTwoAttackTimer = 0;
            phaseTwoStrong5WaveAngles = Vector4.Zero;
            containmentRingFollowsBoss = true;
            NPC.velocity = Vector2.Zero;
            NPC.netUpdate = true;
        }

        void TickPhaseTwoStrong5Move(Player target)
        {
            phaseTwoAttackTimer++;
            int elapsed = phaseTwoAttackTimer - 1;
            int attackEnd = PhaseTwoStrong5AttackStartTicks + PhaseTwoStrong5AttackDurationTicks;
            if (elapsed < attackEnd)
            {
                if (PhaseTwoStrong5SnapsThenHolds && elapsed >= PhaseTwoStrong5AttackStartTicks)
                {
                    // One snap onto the circle, then planted: after the opening volley the boss holds
                    // position for the rest of the move.
                    NPC.velocity = Vector2.Zero;
                    FacePhaseOneTarget(target);
                }
                else
                {
                    MovePhaseTwoOnCircle(target);
                }
            }
            else
            {
                NPC.velocity = Vector2.Zero;
                FacePhaseOneTarget(target);
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                SpawnPhaseTwoStrong5Warnings(elapsed);
                SpawnPhaseTwoStrong5Waves(elapsed);
                SpawnPhaseTwoStrong5Reapers(elapsed);
            }

            if (elapsed >= PhaseTwoStrong5DurationTicks)
            {
                ClearDeathReapers();
                containmentRingFollowsBoss = true;
                containmentRingCenter = NPC.Center;
                BeginPhaseTwoIntermission();
            }
        }

        void StartPhaseTwoStrong6Move()
        {
            PlayStrongMoveRoar();
            phaseTwoAttackState = PhaseTwoAttackState.StrongDeathSquad;
            phaseTwoAttackTimer = 0;
            containmentRingFollowsBoss = false;
            phaseTwoFixedRingCenter = containmentRingCenter;
            NPC.velocity = Vector2.Zero;

            // Teleport straight up so the firing line gets a clean platform over the arena.
            NPC.Center = new Vector2(NPC.Center.X, NPC.Center.Y - PhaseTwoStrong6TeleportOffset);
            // Landing on top of the player right after a blind teleport would be unavoidable, so
            // contact damage stays off until the squad is up and firing.
            NPC.damage = 0;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                SpawnPhaseTwoStrong6Squad();
            }

            NPC.netUpdate = true;
        }

        /// <summary>
        /// Spawns the four-Reaper firing line. Slots are numbered 1-4 from left to right at
        /// -2, -1, +1 and +2 spacings; DeathReaper drives their individual fire patterns.
        /// </summary>
        void SpawnPhaseTwoStrong6Squad()
        {
            float spacing = PhaseTwoStrong6ReaperSpacing;
            for (int index = 1; index <= 4; index++)
            {
                // Slots read 1-2-3-4 strictly left to right: -2, -1, +1, +2 spacings. #1/#4 are
                // therefore the outer pair (the cardinal sweepers) and #2/#3 sit inside them.
                float offset = index switch
                {
                    1 => -2f * spacing,
                    2 => -spacing,
                    3 => spacing,
                    _ => 2f * spacing
                };
                Vector2 spawn = new Vector2(NPC.Center.X + offset, NPC.Center.Y);

                NPC.NewNPC(
                    NPC.GetSource_FromAI(),
                    (int)spawn.X,
                    (int)spawn.Y,
                    ModContent.NPCType<DeathReaper>(),
                    ai0: NPC.whoAmI,
                    ai1: DeathReaper.Strong6ModeBit | index,
                    ai2: spawn.X,
                    ai3: spawn.Y);
            }
        }

        void TickPhaseTwoStrong6Move(Player target)
        {
            phaseTwoAttackTimer++;
            int elapsed = phaseTwoAttackTimer - 1;
            int attackEnd = PhaseTwoStrong6AttackStartTicks + PhaseTwoStrong6AttackDurationTicks;
            int ringReturnStart = attackEnd + PhaseTwoStrong6RecoveryTicks - PhaseTwoStrong6RingReturnTicks;

            // The boss plants itself for the whole move; the squad does the work.
            NPC.velocity = Vector2.Zero;
            FacePhaseOneTarget(target);

            if (elapsed < PhaseTwoStrong6ContactSuppressTicks)
            {
                NPC.damage = 0;
            }
            else if (NPC.damage != BodyContactDamage)
            {
                NPC.damage = BodyContactDamage;
            }

            if (elapsed < ringReturnStart)
            {
                containmentRingCenter = phaseTwoFixedRingCenter;
            }
            else
            {
                float progress = MathHelper.Clamp(
                    (elapsed - ringReturnStart) / (float)PhaseTwoStrong6RingReturnTicks,
                    0f,
                    1f);
                containmentRingCenter = Vector2.Lerp(
                    phaseTwoFixedRingCenter,
                    NPC.Center,
                    MathHelper.SmoothStep(0f, 1f, progress));
            }

            if (elapsed >= PhaseTwoStrong6DurationTicks)
            {
                ClearDeathReapers();
                containmentRingFollowsBoss = true;
                containmentRingCenter = NPC.Center;
                NPC.damage = BodyContactDamage;
                BeginPhaseTwoIntermission();
            }
        }

        void SpawnPhaseTwoStrong5Warnings(int elapsed)
        {
            int warningStart = PhaseTwoStrong5AttackStartTicks - PhaseTwoStrong5WarningTicks;
            if (elapsed < warningStart)
            {
                return;
            }

            int wave = (elapsed - warningStart) / PhaseTwoStrong5WaveIntervalTicks;
            if (wave < 0 || wave >= PhaseTwoStrong5WaveCount
                || (elapsed - warningStart) % PhaseTwoStrong5WaveIntervalTicks != 0)
            {
                return;
            }

            float aimAngle = (Main.player[NPC.target].Center - NPC.Center).ToRotation();
            SetPhaseTwoStrong5WaveAngle(wave, aimAngle);

            for (int i = 0; i < PhaseTwoStrong5ScythesPerWave; i++)
            {
                float angle = GetPhaseTwoStrong5ScytheAngle(wave, i, aimAngle);
                Vector2 direction = angle.ToRotationVector2();
                DeathLaserWarning.Spawn(
                    NPC.GetSource_FromAI(),
                    NPC,
                    LargeScytheWarningColor,
                    trackTicks: 0,
                    totalTicks: PhaseTwoStrong5WarningTicks,
                    beamLength: PhaseTwoStrong5WarningLength,
                    beamSize: 0.18f,
                    beamBrightness: 1.2f,
                    origin: NPC.Center,
                    direction: direction,
                    followHost: true);
            }
        }

        void SpawnPhaseTwoStrong5Waves(int elapsed)
        {
            if (elapsed < PhaseTwoStrong5AttackStartTicks)
            {
                return;
            }

            int wave = (elapsed - PhaseTwoStrong5AttackStartTicks) / PhaseTwoStrong5WaveIntervalTicks;
            if (wave < 0 || wave >= PhaseTwoStrong5WaveCount
                || (elapsed - PhaseTwoStrong5AttackStartTicks) % PhaseTwoStrong5WaveIntervalTicks != 0)
            {
                return;
            }

            float aimAngle = GetPhaseTwoStrong5WaveAngle(wave);
            for (int i = 0; i < PhaseTwoStrong5ScythesPerWave; i++)
            {
                float angle = GetPhaseTwoStrong5ScytheAngle(wave, i, aimAngle);
                Vector2 direction = angle.ToRotationVector2();
                Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    NPC.Center,
                    direction * PhaseTwoStrong5ScytheSpeed,
                    ModContent.ProjectileType<DeathFlamingScythe>(),
                    EnemyDamage.Projectile(GiantScytheDamage),
                    0f,
                    Main.myPlayer,
                    NPC.Center.X,
                    NPC.Center.Y,
                    (float)DeathFlamingScythe.MovementMode.Straight);
            }

            PlayDeathSickleUseSound();
        }

        void SpawnPhaseTwoStrong5Reapers(int elapsed)
        {
            if (elapsed < PhaseTwoStrong5AttackStartTicks)
            {
                return;
            }

            int reaperIndex = (elapsed - PhaseTwoStrong5AttackStartTicks) / PhaseTwoStrong5ReaperIntervalTicks;
            if (reaperIndex < 0 || reaperIndex >= PhaseTwoStrong5ReaperCount
                || (elapsed - PhaseTwoStrong5AttackStartTicks) % PhaseTwoStrong5ReaperIntervalTicks != 0)
            {
                return;
            }

            float spawnAngle = Main.rand.NextFloat(MathHelper.TwoPi);
            NPC.NewNPC(
                NPC.GetSource_FromAI(),
                (int)NPC.Center.X,
                (int)NPC.Center.Y,
                ModContent.NPCType<DeathReaper>(),
                ai0: NPC.whoAmI,
                ai1: DeathReaper.Strong5ChargeModeBit,
                ai2: spawnAngle,
                ai3: 0f);
        }

        float GetPhaseTwoStrong5ScytheAngle(int wave, int index, float aimAngle)
        {
            // Wave 0 and every even wave put the player on the bisector between two lines (even volley);
            // odd waves put a line straight on the player. The two alternate across the move.
            float step = MathHelper.TwoPi / PhaseTwoStrong5ScythesPerWave;
            bool bisectorWave = wave % 2 == 0;
            float offset = bisectorWave ? step * 0.5f : 0f;
            return aimAngle + offset + index * step;
        }

        float GetPhaseTwoStrong5WaveAngle(int wave)
        {
            return wave switch
            {
                0 => phaseTwoStrong5WaveAngles.X,
                1 => phaseTwoStrong5WaveAngles.Y,
                2 => phaseTwoStrong5WaveAngles.Z,
                _ => phaseTwoStrong5WaveAngles.W
            };
        }

        void SetPhaseTwoStrong5WaveAngle(int wave, float angle)
        {
            switch (wave)
            {
                case 0:
                    phaseTwoStrong5WaveAngles.X = angle;
                    break;
                case 1:
                    phaseTwoStrong5WaveAngles.Y = angle;
                    break;
                case 2:
                    phaseTwoStrong5WaveAngles.Z = angle;
                    break;
                default:
                    phaseTwoStrong5WaveAngles.W = angle;
                    break;
            }

            NPC.netUpdate = true;
        }

        DeathSickleWeapon GetPhaseTwoSickle()
        {
            if (phaseTwoSickleNpcIndex < 0 || phaseTwoSickleNpcIndex >= Main.maxNPCs)
            {
                return null;
            }

            NPC sickleNpc = Main.npc[phaseTwoSickleNpcIndex];
            return sickleNpc.active ? sickleNpc.ModNPC as DeathSickleWeapon : null;
        }

        void SpawnPhaseTwoSickle(Vector2 position, float rotation, DeathSickleWeapon.MovementMode mode)
        {
            phaseTwoSickleNpcIndex = DeathSickleWeapon.Spawn(
                NPC.GetSource_FromAI(),
                position,
                rotation,
                NPC.whoAmI,
                mode,
                0);
        }

        void SetPhaseTwoSicklePose(Vector2 center, float rotation, int direction, float opacity, int damage)
        {
            DeathSickleWeapon sickle = GetPhaseTwoSickle();
            if (sickle == null)
            {
                return;
            }

            sickle.SetPose(center, rotation, direction, opacity, damage);
            if (Main.netMode != NetmodeID.MultiplayerClient && phaseTwoAttackTimer % 5 == 0)
            {
                sickle.NPC.netUpdate = true;
            }
        }

        void KillPhaseTwoSickle()
        {
            if (phaseTwoSickleNpcIndex >= 0 && phaseTwoSickleNpcIndex < Main.maxNPCs)
            {
                Main.npc[phaseTwoSickleNpcIndex].active = false;
            }

            phaseTwoSickleNpcIndex = -1;
        }

        void PlayDeathSickleUseSound()
        {
            Item sickle = new Item();
            sickle.SetDefaults(ItemID.DeathSickle);
            if (sickle.UseSound.HasValue)
            {
                Terraria.Audio.SoundEngine.PlaySound(sickle.UseSound.Value, NPC.Center);
            }
        }

        /// <summary>
        /// Random rotation applied to the scatter formation. Derived from the synced seed rather than
        /// Main.rand so every client places the six summons identically while the spacing stays even.
        /// </summary>
        float GetScatterFormationAngle()
        {
            uint hash = (uint)phaseTwoScatterRotationSeed;
            hash ^= hash >> 16;
            hash *= 0x7FEB352Du;
            hash ^= hash >> 15;
            hash *= 0x846CA68Bu;
            hash ^= hash >> 16;
            return (hash & 0xFFFF) / 65535f * MathHelper.TwoPi;
        }

        void StartPhaseTwoScatterMove()
        {
            PlayStrongMoveRoar();
            phaseTwoAttackState = PhaseTwoAttackState.ScatterMove;
            phaseTwoAttackTimer = 0;
            NPC.velocity = Vector2.Zero;
            // Pin the ring where it stands for the whole move; it slides back onto the boss during the
            // fade-out.
            containmentRingFollowsBoss = false;
            phaseTwoFixedRingCenter = containmentRingCenter;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                SpawnPhaseTwoScatterReapers();
                phaseTwoScatterRotationSeed = Main.rand.Next();
            }

            NPC.netUpdate = true;
        }

        void SpawnPhaseTwoScatterReapers()
        {
            int count = PhaseTwoScatterReaperCount;
            for (int slot = 0; slot < count; slot++)
            {
                int packedSlot = (count << 8) | slot | ScatterReaperModeBit;
                NPC.NewNPC(
                    NPC.GetSource_FromAI(),
                    (int)NPC.Center.X,
                    (int)NPC.Center.Y,
                    ModContent.NPCType<DeathReaper>(),
                    ai0: NPC.whoAmI,
                    ai1: packedSlot,
                    ai2: NPC.Center.X,
                    ai3: NPC.Center.Y);
            }
        }

        void TickPhaseTwoScatterMove(Player target)
        {
            phaseTwoAttackTimer++;
            NPC.rotation = 0f;
            FacePhaseOneTarget(target);

            if (PhaseTwoScatterMovesOnCircle)
            {
                MovePhaseTwoOnCircle(target, radiusOverride: PhaseTwoScatterCircleRadius);
            }
            else
            {
                NPC.velocity = Vector2.Zero;
            }

            // The body lobs a flaming scythe at the player on its interval - no telegraph line.
            if (PhaseTwoScatterBodyScytheEnabled
                && phaseTwoAttackTimer >= PhaseTwoReaperBoltStartTicks
                && phaseTwoAttackTimer < PhaseTwoScatterFadeStartTicks
                && (phaseTwoAttackTimer - PhaseTwoReaperBoltStartTicks) % PhaseTwoScatterFlamingScytheIntervalTicks == 0)
            {
                // Swing sound on every client; the projectile itself is only spawned by the server.
                PlayDeathSickleUseSound();
                SpawnPhaseTwoScatterFlamingScythe(target);
            }

            if (phaseTwoAttackTimer >= PhaseTwoScatterFadeStartTicks)
            {
                // The fade-out doubles as the pinned ring sliding back onto the boss.
                float ringReturnProgress = MathHelper.Clamp(
                    (phaseTwoAttackTimer - PhaseTwoScatterFadeStartTicks)
                        / (float)Math.Max(1, PhaseTwoScatterDurationTicks - PhaseTwoScatterFadeStartTicks),
                    0f,
                    1f);
                containmentRingCenter = Vector2.Lerp(
                    phaseTwoFixedRingCenter,
                    NPC.Center,
                    MathHelper.SmoothStep(0f, 1f, ringReturnProgress));
            }

            if (phaseTwoAttackTimer >= PhaseTwoScatterDurationTicks)
            {
                containmentRingFollowsBoss = true;
                containmentRingCenter = NPC.Center;
                BeginPhaseTwoIntermission();
            }
        }

        void SpawnPhaseTwoScatterFlamingScythe(Player target)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Vector2 direction = (target.Center - NPC.Center).SafeNormalize(-Vector2.UnitY);
            Projectile.NewProjectile(
                NPC.GetSource_FromAI(),
                NPC.Center,
                direction * PhaseTwoScatterFlamingScytheSpeed,
                ModContent.ProjectileType<DeathFlamingScythe>(),
                EnemyDamage.Projectile(GiantScytheDamage),
                0f,
                Main.myPlayer,
                NPC.Center.X,
                NPC.Center.Y,
                (float)DeathFlamingScythe.MovementMode.Straight);
        }

        void MovePhaseTwoChase(Player target)
        {
            NPC.rotation = 0f;
            NPC.direction = target.Center.X < NPC.Center.X ? -1 : 1;
            NPC.spriteDirection = NPC.direction;
            Vector2 direction = (target.Center - NPC.Center).SafeNormalize(Vector2.Zero);
            NPC.velocity = direction * PhaseOneMoveSpeed;
        }

        void BeginPhaseTwoIntermission(int? duration = null)
        {
            phaseTwoAttackState = PhaseTwoAttackState.Intermission;
            phaseTwoAttackTimer = 0;
            phaseTwoIntermissionDuration = duration ?? PhaseTwoIntermissionTicks;
            NPC.velocity = Vector2.Zero;
            containmentRingFollowsBoss = true;
            NPC.netUpdate = true;
        }

        void TickPhaseTwoIntermission(Player target)
        {
            NPC.velocity = Vector2.Zero;
            FacePhaseOneTarget(target);
            phaseTwoAttackTimer++;
            if (phaseTwoAttackTimer >= phaseTwoIntermissionDuration)
            {
                StartNextPhaseTwoMove();
            }
        }

        void MovePhaseTwoOnCircle(Player target, float? maxSpeedOverride = null, float? radiusOverride = null)
        {
            NPC.rotation = 0f;
            Vector2 fromPlayer = NPC.Center - target.Center;
            Vector2 radial = fromPlayer.SafeNormalize(-Vector2.UnitY);
            Vector2 destination = target.Center + radial * (radiusOverride ?? PhaseTwoCircleRadius);
            Vector2 toDestination = destination - NPC.Center;
            float distance = toDestination.Length();

            Vector2 desiredVelocity = Vector2.Zero;
            if (distance > 1f)
            {
                float slowFactor = MathHelper.Clamp(distance / 120f, 0f, 1f);
                float maxSpeed = maxSpeedOverride ?? PhaseTwoCircleMoveMaxSpeed;
                float speed = MathHelper.Lerp(PhaseTwoCircleMoveMinSpeed, maxSpeed, slowFactor);
                desiredVelocity = toDestination / distance * speed;
            }

            NPC.velocity = SmoothPhaseTwoCircleVelocity(NPC.velocity, desiredVelocity);
        }

        void MovePhaseTwoWeak5Return(Player target)
        {
            Vector2 fromPlayer = NPC.Center - target.Center;
            Vector2 radial = fromPlayer.SafeNormalize(-Vector2.UnitY);
            Vector2 destination = target.Center + radial * PhaseTwoCircleRadius;
            Vector2 toDestination = destination - NPC.Center;
            float distance = toDestination.Length();

            Vector2 desiredVelocity = Vector2.Zero;
            if (distance > 4f)
            {
                // Brake into the orbit instead of accelerating through the destination.
                float stoppingDistance = Math.Max(0f, distance - 4f);
                float desiredSpeed = (float)Math.Sqrt(
                    2f * PhaseTwoWeak5CircleReturnDeceleration * stoppingDistance);
                desiredSpeed = Math.Min(desiredSpeed, PhaseTwoWeak5CircleReturnMaxSpeed);
                desiredVelocity = toDestination / distance * desiredSpeed;
            }

            NPC.velocity = SmoothVelocity(
                NPC.velocity,
                desiredVelocity,
                PhaseTwoWeak5CircleReturnAcceleration,
                PhaseTwoWeak5CircleReturnDeceleration);
        }

        static Vector2 SmoothVelocity(Vector2 current, Vector2 desired, float acceleration, float deceleration)
        {
            Vector2 delta = desired - current;
            float distance = delta.Length();
            if (distance < 0.001f)
            {
                return desired;
            }

            float maxChange = desired.LengthSquared() > current.LengthSquared() ? acceleration : deceleration;
            return distance <= maxChange ? desired : current + delta / distance * maxChange;
        }

        Vector2 SmoothPhaseTwoCircleVelocity(Vector2 current, Vector2 desired)
        {
            Vector2 delta = desired - current;
            float distance = delta.Length();
            if (distance < 0.001f)
            {
                return desired;
            }

            float maxChange = desired.LengthSquared() > current.LengthSquared()
                ? PhaseTwoCircleAcceleration
                : PhaseTwoCircleDeceleration;

            return distance <= maxChange
                ? desired
                : current + delta / distance * maxChange;
        }

        void SpawnPhaseTwoReaperBoltVolley(Player target)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            int count = PhaseTwoBodyBoltCount;
            float baseAngle = (target.Center - NPC.Center).ToRotation();
            float spread = MathHelper.ToRadians(PhaseTwoBodyBoltSpacingDegrees);
            float startAngle = baseAngle - (count - 1) * spread * 0.5f;

            for (int i = 0; i < count; i++)
            {
                Vector2 direction = (startAngle + spread * i).ToRotationVector2();
                Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    NPC.Center,
                    direction * PhaseTwoReaperBoltSpeed,
                    ModContent.ProjectileType<DeathBolt>(),
                    BoltDamage,
                    0f,
                    Main.myPlayer,
                    direction.X,
                    direction.Y,
                    UsefulFunctions.ColorToFloat(PhaseOneBoltColor));
            }
        }

        void ApplyPhaseTwoPlayerBuffs()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            int buffType = ModContent.BuffType<FasterThanSight>();
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player.active && !player.dead && player.Distance(NPC.Center) < 3000f)
                {
                    player.AddBuff(buffType, 2);
                }
            }
        }

        void RunPhaseTwoAI()
        {
            if (!NPC.HasValidTarget)
            {
                return;
            }

            if (phaseTwoAttackState == PhaseTwoAttackState.Inactive)
            {
                StartNextPhaseTwoMove();
            }

            if (phaseTwoAttackState == PhaseTwoAttackState.ReaperMove)
            {
                TickPhaseTwoReaperMove(Main.player[NPC.target]);
            }
            else if (phaseTwoAttackState == PhaseTwoAttackState.ScatterMove)
            {
                TickPhaseTwoScatterMove(Main.player[NPC.target]);
            }
            else if (phaseTwoAttackState == PhaseTwoAttackState.WeakMove)
            {
                TickPhaseTwoWeakMove(Main.player[NPC.target]);
            }
            else if (phaseTwoAttackState == PhaseTwoAttackState.WeakFlamingScythe)
            {
                TickPhaseTwoWeakFlamingScythe(Main.player[NPC.target]);
            }
            else if (phaseTwoAttackState == PhaseTwoAttackState.WeakStraightScythe)
            {
                TickPhaseTwoWeak3Move(Main.player[NPC.target]);
            }
            else if (phaseTwoAttackState == PhaseTwoAttackState.WeakInwardScythes)
            {
                TickPhaseTwoWeak4Move(Main.player[NPC.target]);
            }
            else if (phaseTwoAttackState == PhaseTwoAttackState.WeakSickleRush)
            {
                TickPhaseTwoWeak5Move(Main.player[NPC.target]);
            }
            else if (phaseTwoAttackState == PhaseTwoAttackState.StrongSickleDash)
            {
                TickPhaseTwoStrong4Move(Main.player[NPC.target]);
            }
            else if (phaseTwoAttackState == PhaseTwoAttackState.StrongReaperScytheRush)
            {
                TickPhaseTwoStrong5Move(Main.player[NPC.target]);
            }
            else if (phaseTwoAttackState == PhaseTwoAttackState.StrongDeathSquad)
            {
                TickPhaseTwoStrong6Move(Main.player[NPC.target]);
            }
            else if (phaseTwoAttackState == PhaseTwoAttackState.Intermission)
            {
                TickPhaseTwoIntermission(Main.player[NPC.target]);
            }
            else
            {
                TickPhaseTwoFiveDash(Main.player[NPC.target]);
            }
        }

        void StartPhaseTwoFiveDash()
        {
            PlayStrongMoveRoar();
            phaseTwoAttackState = PhaseTwoAttackState.Windup;
            phaseTwoAttackTimer = 0;
            phaseTwoDashIndex = 0;
            phaseTwoDashDirection = Vector2.UnitX;
            NPC.velocity = Vector2.Zero;
            containmentRingFollowsBoss = false;
            phaseTwoFixedRingCenter = containmentRingCenter;
            NPC.netUpdate = true;
        }

        void TickPhaseTwoFiveDash(Player target)
        {
            switch (phaseTwoAttackState)
            {
                case PhaseTwoAttackState.Windup:
                    NPC.velocity = Vector2.Zero;
                    FacePhaseOneTarget(target);
                    containmentRingCenter = phaseTwoFixedRingCenter;
                    phaseTwoAttackTimer++;
                    if (phaseTwoAttackTimer >= PhaseTwoFiveDashWindupTicks)
                    {
                        BeginPhaseTwoDash(target);
                    }
                    break;

                case PhaseTwoAttackState.Dash:
                    phaseTwoAttackTimer++;
                    TickPhaseTwoDashMovement();
                    containmentRingCenter = phaseTwoFixedRingCenter;

                    if (phaseTwoAttackTimer % PhaseTwoBoltIntervalTicks == 0)
                    {
                        SpawnPhaseTwoDashBolt(target);
                    }

                    if (phaseTwoAttackTimer >= PhaseTwoFiveDashDurationTicks)
                    {
                        if (phaseTwoDashIndex >= PhaseTwoFiveDashCount)
                        {
                            BeginPhaseTwoFiveDashRingReturn();
                        }
                        else
                        {
                            if (PhaseTwoFiveDashGapTicks <= 0)
                            {
                                BeginPhaseTwoDash(target);
                            }
                            else
                            {
                                BeginPhaseTwoDashPause();
                            }
                        }
                    }
                    break;

                case PhaseTwoAttackState.DashPause:
                    NPC.velocity = Vector2.Zero;
                    FacePhaseOneTarget(target);
                    containmentRingCenter = phaseTwoFixedRingCenter;
                    phaseTwoAttackTimer++;
                    if (phaseTwoAttackTimer >= PhaseTwoFiveDashGapTicks)
                    {
                        BeginPhaseTwoDash(target);
                    }
                    break;

                case PhaseTwoAttackState.DashRingReturn:
                    NPC.velocity = Vector2.Zero;
                    FacePhaseOneTarget(target);
                    phaseTwoAttackTimer++;
                    float ringReturnProgress = MathHelper.Clamp(
                        phaseTwoAttackTimer / (float)PhaseTwoFiveDashRingReturnTicks, 0f, 1f);
                    containmentRingCenter = Vector2.Lerp(
                        phaseTwoFixedRingCenter,
                        NPC.Center,
                        MathHelper.SmoothStep(0f, 1f, ringReturnProgress));
                    if (phaseTwoAttackTimer >= PhaseTwoFiveDashRingReturnTicks)
                    {
                        containmentRingFollowsBoss = true;
                        containmentRingCenter = NPC.Center;
                        BeginPhaseTwoIntermission();
                    }
                    break;
            }
        }

        void BeginPhaseTwoDash(Player target)
        {
            phaseTwoAttackState = PhaseTwoAttackState.Dash;
            phaseTwoAttackTimer = 0;
            phaseTwoDashIndex++;
            phaseTwoDashDirection = UsefulFunctions.Aim(NPC.Center, target.Center, 1f);
            NPC.velocity = Vector2.Zero;
            NPC.direction = phaseTwoDashDirection.X < 0f ? -1 : 1;
            NPC.spriteDirection = NPC.direction;
            NPC.netUpdate = true;
        }

        void BeginPhaseTwoDashPause()
        {
            phaseTwoAttackState = PhaseTwoAttackState.DashPause;
            phaseTwoAttackTimer = 0;
            NPC.velocity = Vector2.Zero;
            NPC.netUpdate = true;
        }

        void BeginPhaseTwoFiveDashRingReturn()
        {
            phaseTwoAttackState = PhaseTwoAttackState.DashRingReturn;
            phaseTwoAttackTimer = 0;
            NPC.velocity = Vector2.Zero;
            NPC.netUpdate = true;
        }

        void TickPhaseTwoDashMovement()
        {
            if (phaseTwoAttackTimer == 1)
            {
                NPC.velocity = phaseTwoDashDirection * PhaseTwoFiveDashSpeed;
                return;
            }

            if (phaseTwoAttackTimer > PhaseTwoFiveDashDecelStartTicks)
            {
                NPC.velocity *= PhaseOneSpecialDashDeceleration;
            }
        }

        void SpawnPhaseTwoDashBolt(Player target)
        {
            Vector2 aim = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitX);
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            float baseAngle = aim.ToRotation();
            float spreadRadians = MathHelper.ToRadians(PhaseTwoBoltSpreadDegrees);
            float centerOffset = (PhaseTwoBoltCount - 1) * 0.5f;

            for (int i = 0; i < PhaseTwoBoltCount; i++)
            {
                Vector2 shotDirection = (baseAngle + (i - centerOffset) * spreadRadians).ToRotationVector2();
                Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    NPC.Center,
                    shotDirection * PhaseTwoBoltSpeed,
                    ModContent.ProjectileType<DeathBolt>(),
                    BoltDamage,
                    0f,
                    Main.myPlayer,
                    shotDirection.X,
                    shotDirection.Y,
                    UsefulFunctions.ColorToFloat(PhaseOneBoltColor));
            }
        }

        void RunPhaseOneAI()
        {
            if (!NPC.HasValidTarget)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            phaseOneSickleTimer++;
            if (phaseOneSickleTimer >= PhaseOneSickleInterval)
            {
                phaseOneSickleTimer = 0;
                SpawnPhaseOneDeathSickle(target);
            }

            if (phaseOneSpecialState != PhaseOneSpecialState.Inactive)
            {
                TickPhaseOneSpecial(target);
                return;
            }

            Vector2 toTarget = target.Center - NPC.Center;
            if (toTarget.LengthSquared() > 0.01f)
            {
                NPC.velocity = toTarget.SafeNormalize(Vector2.Zero) * PhaseOneMoveSpeed;
                NPC.direction = toTarget.X < 0f ? -1 : 1;
                NPC.spriteDirection = NPC.direction;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && phaseOneSpecialCooldown > 0)
            {
                phaseOneSpecialCooldown--;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && phaseOneSpecialCooldown <= 0)
            {
                StartPhaseOneSpecialAttack();
            }
        }

        void SpawnPhaseOneDeathSickle(Player target)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            float radius = Main.rand.NextFloat(PhaseOneSickleMinRange, PhaseOneSickleMaxRange);
            float angle = Main.rand.NextFloat(MathHelper.TwoPi);
            Vector2 spawnPosition = target.Center + angle.ToRotationVector2() * radius;
            Vector2 direction = (target.Center - spawnPosition).SafeNormalize(Vector2.UnitX);
            Vector2 velocity = direction * PhaseOneSickleSpeed;

            Projectile.NewProjectile(
                NPC.GetSource_FromAI(),
                spawnPosition,
                velocity,
                ModContent.ProjectileType<DeathSickleProjectile>(),
                SickleDamage,
                0f,
                Main.myPlayer,
                direction.X,
                direction.Y,
                PhaseOneSickleSpeed);
        }

        void StartPhaseOneSpecialAttack()
        {
            phaseOneSpecialState = PhaseOneSpecialState.Windup;
            phaseOneSpecialTimer = 0;
            phaseOneSpecialCooldown = PhaseOneSpecialCooldownTicks;
            phaseOneSpecialDashIndex = 0;
            phaseOneSpecialDashDirection = Vector2.UnitX;
            NPC.velocity = Vector2.Zero;
            NPC.netUpdate = true;
        }

        void TickPhaseOneSpecial(Player target)
        {
            switch (phaseOneSpecialState)
            {
                case PhaseOneSpecialState.Windup:
                    NPC.velocity = Vector2.Zero;
                    FacePhaseOneTarget(target);
                    phaseOneSpecialTimer++;
                    if (phaseOneSpecialTimer >= PhaseOneSpecialWindupTicks)
                    {
                        BeginPhaseOneDash(target);
                    }
                    break;

                case PhaseOneSpecialState.Dash:
                    phaseOneSpecialTimer++;
                    TickPhaseOneDash();
                    if (phaseOneSpecialTimer >= PhaseOneSpecialDashDurationTicks)
                    {
                        if (phaseOneSpecialDashIndex >= PhaseOneSpecialDashCount)
                        {
                            EndPhaseOneSpecialAttack();
                        }
                        else
                        {
                            if (PhaseOneSpecialDashGapTicks <= 0)
                            {
                                BeginPhaseOneDash(target);
                            }
                            else
                            {
                                BeginPhaseOneDashPause();
                            }
                        }
                    }
                    break;

                case PhaseOneSpecialState.Pause:
                    NPC.velocity = Vector2.Zero;
                    FacePhaseOneTarget(target);
                    phaseOneSpecialTimer++;
                    if (phaseOneSpecialTimer < PhaseOneSpecialDashGapTicks)
                    {
                        break;
                    }

                    if (phaseOneSpecialDashIndex >= PhaseOneSpecialDashCount)
                    {
                        EndPhaseOneSpecialAttack();
                    }
                    else
                    {
                        BeginPhaseOneDash(target);
                    }
                    break;
            }
        }

        void BeginPhaseOneDash(Player target)
        {
            phaseOneSpecialState = PhaseOneSpecialState.Dash;
            phaseOneSpecialTimer = 0;
            phaseOneSpecialDashIndex++;
            phaseOneSpecialDashDirection = UsefulFunctions.Aim(NPC.Center, target.Center, 1f);
            NPC.velocity = Vector2.Zero;
            NPC.direction = phaseOneSpecialDashDirection.X < 0f ? -1 : 1;
            NPC.spriteDirection = NPC.direction;
            SpawnPhaseOneBoltVolley(target);
            NPC.netUpdate = true;
        }

        void TickPhaseOneDash()
        {
            if (phaseOneSpecialTimer == 1)
            {
                NPC.velocity = phaseOneSpecialDashDirection * PhaseOneSpecialDashSpeed;
                return;
            }

            if (phaseOneSpecialTimer > PhaseOneSpecialDashDecelStartTicks)
            {
                NPC.velocity *= PhaseOneSpecialDashDeceleration;
            }
        }

        void BeginPhaseOneDashPause()
        {
            phaseOneSpecialState = PhaseOneSpecialState.Pause;
            phaseOneSpecialTimer = 0;
            NPC.velocity = Vector2.Zero;
            NPC.netUpdate = true;
        }

        void EndPhaseOneSpecialAttack()
        {
            phaseOneSpecialState = PhaseOneSpecialState.Inactive;
            phaseOneSpecialTimer = 0;
            phaseOneSpecialDashIndex = 0;
            NPC.velocity = Vector2.Zero;
            NPC.netUpdate = true;
        }

        void SpawnPhaseOneBoltVolley(Player target)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            float baseAngle = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitX).ToRotation();
            float spreadRadians = MathHelper.ToRadians(PhaseOneBoltSpreadDegrees);
            float centerOffset = (PhaseOneBoltCount - 1) * 0.5f;

            for (int i = 0; i < PhaseOneBoltCount; i++)
            {
                Vector2 shotDirection = (baseAngle + (i - centerOffset) * spreadRadians).ToRotationVector2();
                Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    NPC.Center,
                    shotDirection * PhaseOneSpecialDashSpeed,
                    ModContent.ProjectileType<DeathBolt>(),
                    BoltDamage,
                    0f,
                    Main.myPlayer,
                    shotDirection.X,
                    shotDirection.Y,
                    UsefulFunctions.ColorToFloat(PhaseOneBoltColor));
            }
        }

        void FacePhaseOneTarget(Player target)
        {
            Vector2 toTarget = target.Center - NPC.Center;
            if (toTarget.LengthSquared() > 0.01f)
            {
                NPC.direction = toTarget.X < 0f ? -1 : 1;
                NPC.spriteDirection = NPC.direction;
            }
        }


        // Attack state and timers are custom fields, so transitions must request a sync explicitly.
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(containmentRingInitialized);
            writer.WriteVector2(containmentRingCenter);
            writer.Write((byte)bossPhase);
            writer.Write(phaseTransitionTimer);
            writer.Write(phaseTransitionHealTimer);
            writer.Write(containmentRingAge);
            writer.Write((byte)phaseOneSpecialState);
            writer.Write(phaseOneSpecialTimer);
            writer.Write(phaseOneSpecialDashIndex);
            writer.WriteVector2(phaseOneSpecialDashDirection);
            writer.Write((byte)phaseTwoAttackState);
            writer.Write(phaseTwoAttackTimer);
            writer.Write(phaseTwoDashIndex);
            writer.WriteVector2(phaseTwoDashDirection);
            writer.Write(phaseTwoSequenceIndex);
            writer.Write(phaseTwoLastStrongMove);
            writer.Write(phaseTwoWeakBagMask);
            writer.Write(phaseTwoLastWeakMove);
            writer.Write(phaseTwoStrongBagMask);
            writer.Write(phaseTwoWeak2AimAngle);
            writer.Write(phaseTwoWeak3AimAngle);
            writer.Write(phaseTwoWeak4WaveAngles.X);
            writer.Write(phaseTwoWeak4WaveAngles.Y);
            writer.Write(phaseTwoWeak4WaveAngles.Z);
            writer.Write(phaseTwoWeak4WaveAngles.W);
            writer.Write(phaseTwoStrong5WaveAngles.X);
            writer.Write(phaseTwoStrong5WaveAngles.Y);
            writer.Write(phaseTwoStrong5WaveAngles.Z);
            writer.Write(phaseTwoStrong5WaveAngles.W);
            writer.Write(phaseTwoScatterRotationSeed);
            writer.Write(phaseTwoSickleNpcIndex);
            writer.WriteVector2(phaseTwoSickleVelocity);
            writer.WriteVector2(phaseTwoSickleSpawnPosition);
            writer.WriteVector2(phaseTwoStrong4DashStart);
            writer.WriteVector2(phaseTwoStrong4DashTarget);
            writer.Write(phaseTwoSickleAngularVelocity);
            writer.Write(phaseTwoSickleBaseRotation);
            writer.WriteVector2(phaseTwoFixedRingCenter);
            writer.Write(phaseTwoIntermissionDuration);
            writer.Write(phaseTwoSickleSpawnAngle);
            writer.Write(phaseTwoStrong4SpinDirection);
            writer.WriteVector2(phaseTwoReaperCenter);
            writer.Write(phaseTwoReaperClockwise);
            writer.Write(containmentRingFollowsBoss);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            containmentRingInitialized = reader.ReadBoolean();
            containmentRingCenter = reader.ReadVector2();
            bossPhase = (BossPhase)reader.ReadByte();
            phaseTransitionTimer = reader.ReadInt32();
            phaseTransitionHealTimer = reader.ReadInt32();
            containmentRingAge = reader.ReadInt32();
            phaseOneSpecialState = (PhaseOneSpecialState)reader.ReadByte();
            phaseOneSpecialTimer = reader.ReadInt32();
            phaseOneSpecialDashIndex = reader.ReadInt32();
            phaseOneSpecialDashDirection = reader.ReadVector2();
            phaseTwoAttackState = (PhaseTwoAttackState)reader.ReadByte();
            phaseTwoAttackTimer = reader.ReadInt32();
            phaseTwoDashIndex = reader.ReadInt32();
            phaseTwoDashDirection = reader.ReadVector2();
            phaseTwoSequenceIndex = reader.ReadInt32();
            phaseTwoLastStrongMove = reader.ReadInt32();
            phaseTwoWeakBagMask = reader.ReadInt32();
            phaseTwoLastWeakMove = reader.ReadInt32();
            phaseTwoStrongBagMask = reader.ReadInt32();
            phaseTwoWeak2AimAngle = reader.ReadSingle();
            phaseTwoWeak3AimAngle = reader.ReadSingle();
            phaseTwoWeak4WaveAngles = new Vector4(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle());
            phaseTwoStrong5WaveAngles = new Vector4(
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle(),
                reader.ReadSingle());
            phaseTwoScatterRotationSeed = reader.ReadInt32();
            phaseTwoSickleNpcIndex = reader.ReadInt32();
            phaseTwoSickleVelocity = reader.ReadVector2();
            phaseTwoSickleSpawnPosition = reader.ReadVector2();
            phaseTwoStrong4DashStart = reader.ReadVector2();
            phaseTwoStrong4DashTarget = reader.ReadVector2();
            phaseTwoSickleAngularVelocity = reader.ReadSingle();
            phaseTwoSickleBaseRotation = reader.ReadSingle();
            phaseTwoFixedRingCenter = reader.ReadVector2();
            phaseTwoIntermissionDuration = reader.ReadInt32();
            phaseTwoSickleSpawnAngle = reader.ReadSingle();
            phaseTwoStrong4SpinDirection = reader.ReadInt32();
            phaseTwoReaperCenter = reader.ReadVector2();
            phaseTwoReaperClockwise = reader.ReadBoolean();
            containmentRingFollowsBoss = reader.ReadBoolean();
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (!Main.dedServ)
            {
                DrawBodyGlow();
            }

            if (UsesContainmentRing && !Main.dedServ && containmentRingAge > 0)
            {
                float opacity = MathHelper.SmoothStep(0f, 1f, GetContainmentRingProgress());
            Color coreColor = Color.Lerp(AuraColor, Color.White, 0.2f);
                DeathContainmentRingVFX.DrawBoundaryFog(
                    containmentRingCenter,
                    GetContainmentRingRadius(),
                    opacity * ContainmentFogOpacity,
                    ContainmentFogDarkColor,
                    ContainmentFogMidColor,
                    ContainmentFogCoreColor);
                DeathContainmentRingVFX.DrawBoundaryEdge(
                    containmentRingCenter,
                    GetContainmentRingRadius(),
                    40f,
                    opacity,
                    AuraColor,
                    coreColor);
            }


            if (!Main.dedServ)
            {
                DrawBody(screenPos, drawColor);
            }


            return false;
        }

        /// <summary>
        /// Draws the body ourselves instead of letting the vanilla path do it. Measured off a
        /// screenshot with a hitbox overlay, the vanilla draw on this NPC lands the sprite ~34
        /// source pixels above its own hitbox (and above the aura), so the body is drawn here from
        /// NPC.Center with the frame-centre origin, matching the aura's screen-space source.
        /// </summary>
        void DrawBody(Vector2 screenPos, Color drawColor)
        {
            if (NPC.hide)
            {
                return;
            }

            Rectangle frame = NPC.frame;
            if (frame.Width <= 0 || frame.Height <= 0)
            {
                return;
            }

            Main.EntitySpriteDraw(
                TextureAssets.Npc[NPC.type].Value,
                NPC.Center - Main.screenPosition + new Vector2(0f, NPC.gfxOffY),
                frame,
                NPC.GetAlpha(drawColor),
                NPC.rotation,
                frame.Size() / 2f,
                NPC.scale,
                NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally,
                0f);
        }

        /// <summary>
        /// Body aura. Shader-driven so it follows the body's exact frame, rotation and flip.
        /// Baseline breathes on its own (see DeathVFX.DrawShaderGlow); a running strong move holds
        /// a bigger, brighter aura, and its opening moments add a swell-then-contract flash.
        /// </summary>
        void DrawBodyGlow()
        {
            Rectangle frame = NPC.frame;
            if (frame.Width <= 0 || frame.Height <= 0)
            {
                return;
            }

            // Phase one carries no aura at all: the glow fades in with the containment ring that
            // appears during the phase transition.
            float fadeIn = UsesContainmentRing ? MathHelper.SmoothStep(0f, 1f, GetContainmentRingProgress()) : 0f;
            if (fadeIn <= 0.001f)
            {
                return;
            }

            float steady = MathHelper.Lerp(BodyGlowBaselineOpacity, 1f, strongMoveGlowOpacity);
            float flash = GetBodyGlowFlash();
            float opacity = MathHelper.Clamp(steady + flash * BodyGlowFlashOpacity, 0f, 1f) * fadeIn;

            DeathVFX.DrawShaderGlow(
                TextureAssets.Npc[NPC.type].Value,
                frame,
                NPC.Center,
                NPC.rotation,
                frame.Size() / 2f,
                NPC.scale,
                NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally,
                VfxGlowColor,
                opacity,
                haloRadius: MathHelper.Lerp(5f, 8f, strongMoveGlowOpacity) + flash * 3.5f,
                haloDirections: 10,
                bloomScale: MathHelper.Lerp(1.18f, 1.32f, strongMoveGlowOpacity) + flash * 0.25f,
                bloomOpacity: 0.22f + 0.25f * strongMoveGlowOpacity + flash * 0.2f,
                rimOpacity: 0.35f + 0.25f * strongMoveGlowOpacity + flash * 0.3f,
                coreOpacity: 0.3f + 0.15f * strongMoveGlowOpacity + flash * 0.25f,
                coreWhiteMix: 0.3f);
        }

        void TickContainmentRingVisuals()
        {
            if (!UsesContainmentRing || Main.dedServ || containmentRingAge <= 0)
            {
                return;
            }

            if (containmentRingVfxTimer > 0)
            {
                containmentRingVfxTimer--;
                return;
            }

            containmentRingVfxTimer = 10;
            float fade = GetContainmentRingProgress();
            float radius = GetContainmentRingRadius();
            int dustCount = Math.Max(12, (int)(160f * fade));

            for (int i = 0; i < dustCount; i++)
            {
                float angle = MathHelper.TwoPi * i / dustCount;
                Vector2 radial = angle.ToRotationVector2();
                Vector2 position = containmentRingCenter + radial * radius;
                Vector2 velocity = radial.RotatedBy(MathHelper.PiOver2) * 0.35f;
                int dustType = i % 4 == 0 ? DustID.PurpleTorch : DustID.ShadowbeamStaff;
                Dust dust = Dust.NewDustPerfect(
                    position,
                    dustType,
                    velocity,
                    100,
                    AuraColor,
                    Main.rand.NextFloat(1.15f, 1.45f));
                dust.noGravity = true;
            }
        }

        void ApplyContainmentPull()
        {
            if (!UsesContainmentRing
                || bossPhase != BossPhase.PhaseTwo
                || GetContainmentRingProgress() < 1f)
            {
                return;
            }

            float radius = GetContainmentRingRadius();

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead)
                {
                    continue;
                }

                Vector2 toOwner = containmentRingCenter - player.Center;
                float distance = toOwner.Length();
                if (distance <= radius || distance <= 0.001f)
                {
                    continue;
                }

                OnPlayerOutsideContainmentRing(player, distance - radius);

                Vector2 inward = toOwner / distance;
                float inwardSpeed = Vector2.Dot(player.velocity, inward);
                if (inwardSpeed >= ContainmentRingPullSpeed)
                {
                    continue;
                }

                float step = MathHelper.Clamp((ContainmentRingPullSpeed - inwardSpeed) * 0.25f, 0.25f, 0.75f);
                player.velocity += inward * step;
            }
        }

        protected virtual void OnPlayerOutsideContainmentRing(Player player, float distanceOutside)
        {
            // Blight is server-authoritative; clients receive the resulting sync from BlightPlayer.
            if (Main.netMode != NetmodeID.MultiplayerClient && Main.GameUpdateCount % 2 == 0)
            {
                BlightBuildup.Apply(player, 1);
            }
        }

        protected virtual void ApplyWorldOverrides()
        {
        }

        void LockContainmentRingWorldTime()
        {
            if (!containmentRingInitialized)
            {
                return;
            }

            Main.dayTime = false;
            Main.time = ContainmentRingMidnightTime;
        }

        public override void FindFrame(int currentFrame)
        {
            int frameHeight = 1;
            if (!Main.dedServ)
            {
                frameHeight = TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type];
            }

            bool specialAnimation = phaseOneSpecialState != PhaseOneSpecialState.Inactive
                || (bossPhase == BossPhase.PhaseTwo && (phaseTwoAttackState == PhaseTwoAttackState.Windup || phaseTwoAttackState == PhaseTwoAttackState.Dash));
            specialAnimation |= phaseTwoAttackState == PhaseTwoAttackState.ScatterMove
                && phaseTwoAttackTimer >= PhaseTwoReaperBoltStartTicks
                && phaseTwoAttackTimer < PhaseTwoScatterFadeStartTicks;
            specialAnimation |= phaseTwoAttackState == PhaseTwoAttackState.WeakFlamingScythe
                && phaseTwoAttackTimer >= PhaseTwoWeak2WarningTicks
                && phaseTwoAttackTimer < PhaseTwoWeak2ThrowTicks;
            specialAnimation |= phaseTwoAttackState == PhaseTwoAttackState.WeakStraightScythe
                && phaseTwoAttackTimer >= PhaseTwoWeak3WarningTicks
                && phaseTwoAttackTimer < PhaseTwoWeak3ReleaseTicks;
            specialAnimation |= phaseTwoAttackState == PhaseTwoAttackState.WeakInwardScythes
                && phaseTwoAttackTimer >= PhaseTwoWeak4WarningTicks
                && phaseTwoAttackTimer < PhaseTwoWeak4ReleaseTicks;
            specialAnimation |= phaseTwoAttackState == PhaseTwoAttackState.WeakSickleRush
                && phaseTwoAttackTimer >= PhaseTwoWeak5FadeInTicks + PhaseTwoWeak5ChargeRotateTicks + PhaseTwoWeak5ChargeHoldTicks
                && phaseTwoAttackTimer < PhaseTwoWeak5FadeInTicks + PhaseTwoWeak5ChargeRotateTicks + PhaseTwoWeak5ChargeHoldTicks
                    + PhaseTwoWeak5SwingCycleTicks * PhaseTwoWeak5SwingCycles;
            specialAnimation |= phaseTwoAttackState == PhaseTwoAttackState.StrongReaperScytheRush
                && phaseTwoAttackTimer >= PhaseTwoStrong5AttackStartTicks
                && phaseTwoAttackTimer < PhaseTwoStrong5AttackStartTicks + PhaseTwoStrong5AttackDurationTicks;
            int frameStart = specialAnimation ? SpecialAnimationFrameStart : 0;
            int frameCount = specialAnimation ? SpecialAnimationFrameCount : NormalAnimationFrameCount;
            NPC.frameCounter += AnimationFrameCounterStep;
            if (NPC.frameCounter >= frameCount)
            {
                NPC.frameCounter = 0f;
            }

            NPC.frame.Y = frameHeight * (frameStart + (int)NPC.frameCounter);
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life > 0)
            {
                return;
            }

            Vector2 center = new Vector2(NPC.position.X + NPC.width * 0.5f, NPC.position.Y + NPC.height * 0.5f);
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), center, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Death Gore 1").Type, GoreScale);
                Gore.NewGore(NPC.GetSource_Death(), center, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Death Gore 2").Type, GoreScale);
                Gore.NewGore(NPC.GetSource_Death(), center, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Death Gore 3").Type, GoreScale);
            }

            for (int i = 0; i < 50; i++)
            {
                int dust = Dust.NewDust(
                    NPC.position,
                    NPC.width,
                    NPC.height,
                    DustID.GemAmethyst,
                    NPC.velocity.X + Main.rand.Next(-10, 10),
                    NPC.velocity.Y + Main.rand.Next(-10, 10),
                    200,
                    default,
                    4f);
                Main.dust[dust].noGravity = true;
            }
        }

        public override bool CheckActive()
        {
            return false;
        }

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }

        public virtual void OnStagger(NPC npc)
        {
            // NOTE: Death and Absolute Death are not registered in PoiseProfiles yet, and the
            // attack state machine does not set AttackTelegraphing/AttackCommitted. This method
            // deliberately preserves current behavior until the poise rollout is designed.
            npc.velocity *= 0.5f;
            npc.netUpdate = true;
        }

        // ---------------------------------------------------------------------------------------
        // OLD AI (pre-rework Death behaviour) - kept commented out for reference only.
        // It chased the player, fired a homing volley on a timer and warped around them. The
        // current state machine never calls any of it. The matching overrides are commented out in
        // Death.cs / TrueDeath.cs; the old reset lines lived in StartPhaseTransition, the
        // transition-to-phase-two hand-off, OnStagger, SendExtraAI and ReceiveExtraAI.
        // To restore: un-comment this block plus those overrides, and re-add the reset/sync lines.
        //
        // protected abstract int VolleyProjectileType { get; }
        // protected virtual int VolleyDamage => BoltDamage;
        // protected virtual int VolleyInterval => 12;
        // protected virtual int VolleyCount => 5;
        // protected virtual float VolleySpeed => 0.5f;
        // protected virtual int NormalWarpTicks => 150;
        // protected virtual int LowHealthWarpTicks => 100;
        // protected virtual float LowHealthWarpThreshold => 0.15f;
        // protected virtual int WarpDistance => 600;
        // protected virtual float WarpSpeed => 14f;
        //
        // protected int volleyTimer;
        // protected int teleportTimer;
        // protected int volleyShotsFired;
        // protected float nextWarpAngle;
        //
        // void RunLegacyPlaceholderAI()
        // {
        //     volleyTimer++;
        //     teleportTimer++;
        //     TickVolley();
        //
        //     if (teleportTimer >= 40)
        //     {
        //         NPC.velocity.X *= 0.97f;
        //         NPC.velocity.Y *= 0.97f;
        //     }
        //
        //     if (ShouldWarp())
        //     {
        //         WarpNearTarget();
        //     }
        // }
        //
        // void TickVolley()
        // {
        //     if (volleyTimer < VolleyInterval || volleyShotsFired >= VolleyCount)
        //     {
        //         return;
        //     }
        //
        //     Vector2 origin = new Vector2(NPC.position.X + NPC.width * 0.5f, NPC.position.Y + NPC.height * 0.5f);
        //     Player target = Main.player[NPC.target];
        //     float rotation = (float)Math.Atan2(
        //         origin.Y - (target.position.Y + target.height * 0.5f),
        //         origin.X - (target.position.X + target.width * 0.5f));
        //
        //     if (Main.netMode != NetmodeID.MultiplayerClient)
        //     {
        //         Projectile.NewProjectile(
        //             NPC.GetSource_FromThis(),
        //             origin.X,
        //             origin.Y,
        //             (float)(Math.Cos(rotation) * VolleySpeed) * -1f,
        //             (float)(Math.Sin(rotation) * VolleySpeed) * -1f,
        //             VolleyProjectileType,
        //             VolleyDamage,
        //             0f,
        //             Main.myPlayer);
        //     }
        //
        //     volleyTimer = 0;
        //     volleyShotsFired++;
        // }
        //
        // bool ShouldWarp()
        // {
        //     bool lowHealth = NPC.life <= NPC.lifeMax * LowHealthWarpThreshold;
        //     return lowHealth
        //         ? teleportTimer >= LowHealthWarpTicks
        //         : teleportTimer >= NormalWarpTicks;
        // }
        //
        // void WarpNearTarget()
        // {
        //     Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
        //
        //     for (int i = 0; i < 10; i++)
        //     {
        //         int dust = Dust.NewDust(
        //             NPC.position,
        //             NPC.width,
        //             NPC.height,
        //             DustID.GemAmethyst,
        //             NPC.velocity.X + Main.rand.Next(-10, 10),
        //             NPC.velocity.Y + Main.rand.Next(-10, 10),
        //             200,
        //             default,
        //             4f);
        //         Main.dust[dust].noGravity = true;
        //     }
        //
        //     volleyShotsFired = 0;
        //     teleportTimer = 0;
        //
        //     Player target = Main.player[NPC.target];
        //     NPC.position.X = target.position.X + (float)(WarpDistance * Math.Cos(nextWarpAngle)) * -1f;
        //     NPC.position.Y = target.position.Y + (float)(WarpDistance * Math.Sin(nextWarpAngle)) * -1f;
        //
        //     Vector2 origin = new Vector2(NPC.position.X + NPC.width * 0.5f, NPC.position.Y + NPC.height * 0.5f);
        //     float rotation = (float)Math.Atan2(
        //         origin.Y - (target.position.Y + target.height * 0.5f),
        //         origin.X - (target.position.X + target.width * 0.5f));
        //
        //     NPC.velocity.X = (float)Math.Cos(rotation) * WarpSpeed * -1f;
        //     NPC.velocity.Y = (float)Math.Sin(rotation) * WarpSpeed * -1f;
        //     nextWarpAngle = (float)(Main.rand.Next(360) * (Math.PI / 180));
        //     NPC.netUpdate = true;
        // }
        //
        // // Old AI state resets (removed from StartPhaseTransition, the phase-two hand-off and OnStagger):
        // //   volleyTimer = 0; teleportTimer = 0; volleyShotsFired = 0;
        // // Old AI network sync (removed from SendExtraAI / ReceiveExtraAI, in this order):
        // //   nextWarpAngle, volleyTimer, teleportTimer, volleyShotsFired
        // ---------------------------------------------------------------------------------------
    }
}
