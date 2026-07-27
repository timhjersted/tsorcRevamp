using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Puppets
{
    /// <summary>
    /// Named pose vocabulary for puppet attacks. These names intentionally mirror the useful ideas
    /// in vanilla item-use styles without asking Player.ItemCheck to own enemy combat timing.
    /// </summary>
    public enum PuppetPosePreset
    {
        Hold,
        Swing,
        TwoHandedSwing,
        Overhead,
        AimedThrust,
        AimedShoot,
        HoldUp,
        Drink,
    }

    public enum PuppetAttackStage : byte
    {
        Inactive,
        Windup,
        Active,
        Recovery,
    }

    /// <summary>
    /// Data-only definition for one complete puppet strike. The clip owns the windup, hit window,
    /// follow-through, and recovery pose so rendering, collision, VFX, and AI cannot drift onto
    /// separate timers.
    /// </summary>
    public sealed class PuppetAttackClip
    {
        public string Name { get; }
        public PuppetPosePreset Pose { get; }
        public int WindupTicks { get; }
        public int ActiveTicks { get; }
        public int RecoveryTicks { get; }
        public float OppositeWindupRotation { get; }
        public float AttackStartRotation { get; }
        public float AttackEndRotation { get; }
        public float RecoveryEndRotation { get; }
        public float HitWindowStart { get; }
        public float HitWindowEnd { get; }
        public SwingEaseStyle SwingEase { get; }
        public float MaxAimCorrection { get; }
        public float AimTurnRate { get; }
        public int AimLockTicksBeforeActive { get; }

        public PuppetAttackClip(
            string name,
            PuppetPosePreset pose,
            int windupTicks,
            int activeTicks,
            int recoveryTicks,
            float oppositeWindupRotation,
            float attackStartRotation,
            float attackEndRotation,
            float recoveryEndRotation = -0.30f,
            float hitWindowStart = 0.12f,
            float hitWindowEnd = 0.88f,
            SwingEaseStyle swingEase = SwingEaseStyle.Smooth,
            float maxAimCorrection = 0.35f,
            float aimTurnRate = 0.035f,
            int aimLockTicksBeforeActive = 18)
        {
            Name = name;
            Pose = pose;
            WindupTicks = Math.Max(1, windupTicks);
            ActiveTicks = Math.Max(1, activeTicks);
            RecoveryTicks = Math.Max(1, recoveryTicks);
            OppositeWindupRotation = oppositeWindupRotation;
            AttackStartRotation = attackStartRotation;
            AttackEndRotation = attackEndRotation;
            RecoveryEndRotation = recoveryEndRotation;
            HitWindowStart = MathHelper.Clamp(hitWindowStart, 0f, 1f);
            HitWindowEnd = MathHelper.Clamp(Math.Max(hitWindowStart, hitWindowEnd), 0f, 1f);
            SwingEase = swingEase;
            MaxAimCorrection = Math.Abs(maxAimCorrection);
            AimTurnRate = Math.Abs(aimTurnRate);
            AimLockTicksBeforeActive = Math.Max(0, aimLockTicksBeforeActive);
        }
    }

    /// <summary>
    /// Per-NPC execution state for a <see cref="PuppetAttackClip"/>. It deliberately knows nothing
    /// about Terraria hitboxes or drawing; PuppetNPC samples this single clock for both.
    /// </summary>
    public sealed class PuppetAttackRuntime
    {
        private readonly HashSet<int> _hitPlayers = new HashSet<int>();

        public PuppetAttackClip Clip { get; private set; }
        public PuppetAttackStage Stage { get; private set; }
        public int StageTick { get; private set; }
        public float AimCorrection { get; private set; }
        public bool AimLocked { get; private set; }
        public int LockedFacing { get; private set; } = 1;
        public bool Active => Clip != null && Stage != PuppetAttackStage.Inactive;

        public int StageDuration => Stage switch
        {
            PuppetAttackStage.Windup => Clip?.WindupTicks ?? 1,
            PuppetAttackStage.Active => Clip?.ActiveTicks ?? 1,
            PuppetAttackStage.Recovery => Clip?.RecoveryTicks ?? 1,
            _ => 1,
        };

        public int TicksRemaining => Math.Max(0, StageDuration - StageTick);
        public float StageProgress => MathHelper.Clamp(StageTick / (float)Math.Max(1, StageDuration - 1), 0f, 1f);
        public bool HitWindowOpen => Stage == PuppetAttackStage.Active
            && StageProgress >= Clip.HitWindowStart && StageProgress <= Clip.HitWindowEnd;

        public void Start(PuppetAttackClip clip, int facing)
        {
            Clip = clip ?? throw new ArgumentNullException(nameof(clip));
            Stage = PuppetAttackStage.Windup;
            StageTick = 0;
            AimCorrection = 0f;
            AimLocked = false;
            LockedFacing = facing < 0 ? -1 : 1;
            _hitPlayers.Clear();
        }

        public void Cancel()
        {
            Clip = null;
            Stage = PuppetAttackStage.Inactive;
            StageTick = 0;
            AimCorrection = 0f;
            AimLocked = false;
            _hitPlayers.Clear();
        }

        public void UpdateAim(Vector2 attackerCenter, Vector2 targetCenter)
        {
            if (!Active || Stage != PuppetAttackStage.Windup || AimLocked)
                return;

            if (TicksRemaining <= Clip.AimLockTicksBeforeActive)
            {
                AimLocked = true;
                return;
            }

            float dx = Math.Abs(targetCenter.X - attackerCenter.X);
            float dy = targetCenter.Y - attackerCenter.Y;
            // Keep every endpoint inside the composite arm's authored range. A modest amount of
            // behind-the-head travel is intentional for full overhead windups; only much deeper
            // rotations fold the arm back through the torso.
            float clipMinimum = Math.Min(
                Clip.OppositeWindupRotation,
                Math.Min(Clip.AttackStartRotation, Clip.AttackEndRotation));
            float clipMaximum = Math.Max(
                Clip.OppositeWindupRotation,
                Math.Max(Clip.AttackStartRotation, Clip.AttackEndRotation));
            float minimumCorrection = Math.Max(-Clip.MaxAimCorrection, -1.85f - clipMinimum);
            float maximumCorrection = Math.Min(Clip.MaxAimCorrection, 1.40f - clipMaximum);
            if (minimumCorrection > maximumCorrection)
                minimumCorrection = maximumCorrection = 0f;
            float desired = MathHelper.Clamp(
                (float)Math.Atan2(dy, Math.Max(dx, 8f)),
                minimumCorrection,
                maximumCorrection);
            AimCorrection = Approach(AimCorrection, desired, Clip.AimTurnRate);
        }

        public float SampleRotation()
        {
            if (!Active)
                return -0.30f;

            float progress = StageProgress;
            float rotation = Stage switch
            {
                PuppetAttackStage.Windup => SampleLogicalWindup(progress),
                PuppetAttackStage.Active => SwingEase.Apply(
                    Clip.AttackStartRotation,
                    Clip.AttackEndRotation,
                    progress,
                    Clip.SwingEase),
                PuppetAttackStage.Recovery => MathHelper.SmoothStep(
                    Clip.AttackEndRotation,
                    Clip.RecoveryEndRotation,
                    progress),
                _ => Clip.RecoveryEndRotation,
            };
            float aimWeight = Stage == PuppetAttackStage.Recovery ? 1f - progress : 1f;
            return rotation + AimCorrection * aimWeight;
        }

        /// <summary>
        /// Advances the authoritative clock by one tick. Returns true when a stage boundary was
        /// crossed; <paramref name="completed"/> is true only after the final recovery tick.
        /// </summary>
        public bool Advance(out bool completed)
        {
            completed = false;
            if (!Active)
                return false;

            StageTick++;
            if (StageTick < StageDuration)
                return false;

            StageTick = 0;
            switch (Stage)
            {
                case PuppetAttackStage.Windup:
                    Stage = PuppetAttackStage.Active;
                    return true;
                case PuppetAttackStage.Active:
                    Stage = PuppetAttackStage.Recovery;
                    return true;
                default:
                    Stage = PuppetAttackStage.Inactive;
                    completed = true;
                    return true;
            }
        }

        public bool TryRecordHit(int playerIndex) => _hitPlayers.Add(playerIndex);
        public bool HasHitPlayer(int playerIndex) => _hitPlayers.Contains(playerIndex);

        public void LoadNetworkState(
            PuppetAttackClip clip,
            PuppetAttackStage stage,
            int stageTick,
            float aimCorrection,
            bool aimLocked,
            int lockedFacing)
        {
            Clip = clip;
            Stage = stage;
            StageTick = Math.Clamp(stageTick, 0, Math.Max(0, StageDuration - 1));
            AimCorrection = MathHelper.Clamp(aimCorrection, -clip.MaxAimCorrection, clip.MaxAimCorrection);
            AimLocked = aimLocked;
            LockedFacing = lockedFacing < 0 ? -1 : 1;
            _hitPlayers.Clear();
        }

        private float SampleLogicalWindup(float progress)
        {
            const float settleFraction = 0.25f;
            if (progress < settleFraction)
            {
                float settle = MathHelper.SmoothStep(0f, 1f, progress / settleFraction);
                return MathHelper.Lerp(Clip.RecoveryEndRotation, Clip.OppositeWindupRotation, settle);
            }

            float raise = MathHelper.SmoothStep(
                0f,
                1f,
                (progress - settleFraction) / (1f - settleFraction));
            return MathHelper.Lerp(Clip.OppositeWindupRotation, Clip.AttackStartRotation, raise);
        }

        private static float Approach(float current, float target, float maxChange)
        {
            float delta = MathHelper.Clamp(target - current, -maxChange, maxChange);
            return current + delta;
        }
    }
}
