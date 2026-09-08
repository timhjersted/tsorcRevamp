using Microsoft.Xna.Framework;
using System;
using Terraria;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.NPCs.AI
{
    /// <summary>
    /// Movement-intent "jouster" helper for any NPC that rides a mount: it commits to a charge, runs
    /// PAST the target rather than stopping at it, decelerates, turns around, and charges back.
    /// Pure AI — like <see cref="EnemyFlightController"/> it has no opinion on how the NPC is drawn;
    /// the caller reads <see cref="IsCharging"/> to enable a trample hitbox / fire a weapon.
    ///
    /// In intent-only mode it commits a fixed waypoint beyond the target and lets SmartFighter4AI route
    /// there. This preserves the authored overshoot while retaining validated jumps and abyss safety.
    /// Self-driven mode remains available for controlled arenas that do not need terrain navigation.
    ///
    /// Calling pattern (from NPC.AI()):
    ///   _mount ??= new EnemyMountController(MyMountConfig);
    ///   _mount.Tick(NPC, Main.player[NPC.target]);
    ///   if (_mount.IsCharging) { /* trample + attack window */ }
    /// </summary>
    public class EnemyMountController
    {
        public EnemyMountConfig Config;

        public MountMode Mode { get; private set; } = MountMode.Idle;
        public int ModeTimer { get; private set; }
        public int CooldownRemaining { get; private set; }

        /// <summary>Multiplier on top speed, set by the owner each tick. Used to keep the mount moving but
        /// slowed while the rider is committed to an attack, rather than stopping dead to cast.</summary>
        public float SpeedScale = 1f;

        /// <summary>When false this controller becomes INTENT-ONLY: it still runs its state machine, timers
        /// and transitions, but never writes velocity — a real navigator (SmartFighter4) drives movement,
        /// reads <see cref="ModeSpeedMultiplier"/> for speed, and consumes <see cref="NavigationWaypoint"/>
        /// during the committed charge.
        /// <para/>
        /// This is the preferred mode for anything fighting on real terrain. Self-driven movement here is
        /// deliberately pathfinding-free (a charge commits to a straight line), which means pits, ledges
        /// and walls are dead ends — it hops blindly and gets stuck. Letting the navigator drive keeps A*,
        /// doors, ledges and rope handling active at ALL times, and the mount becomes purely an extension
        /// of movement: a speed and aggression profile rather than a separate movement system.</summary>
        public bool DriveMovement = true;

        /// <summary>Speed multiplier the external navigator should use for the current state. This is how
        /// the mount expresses itself when it isn't writing velocity: a charge is simply the navigator
        /// pursuing much faster.</summary>
        public float ModeSpeedMultiplier
        {
            get
            {
                switch (Mode)
                {
                    case MountMode.Charge:
                        return Config.ChargeTopSpeed / Math.Max(0.01f, Config.ApproachTopSpeed);

                    case MountMode.ChargeWindup:
                        return 0f;   // planted: the wind-up is a full stop

                    case MountMode.Overshoot:
                        return 0.65f;

                    case MountMode.TurnAround:
                        return 0.15f;

                    default:
                        return 1f;
                }
            }
        }

        /// <summary>True only during the committed run-past. The owner spawns its trample hitbox and
        /// opens its attack window on this — it is the one state the rider cannot steer out of.</summary>
        public bool IsCharging => Mode == MountMode.Charge;

        /// <summary>True through both the wind-up and the charge that follows it, when this pass was rolled
        /// as the special variant. Owners hang bespoke VFX / hazards off this (e.g. a burning wake).</summary>
        public bool IsSpecialCharge { get; private set; }

        /// <summary>One-tick pulse on the frame the special wind-up begins, so the owner can fire a
        /// single burst effect without having to track mode transitions itself.</summary>
        public bool SpecialWindupStarted { get; private set; }

        /// <summary>True while planted during the special charge's wind-up.</summary>
        public bool IsWindingUp => Mode == MountMode.ChargeWindup;

        /// <summary>Fixed world-space center destination for a navigator-driven charge. It is committed once,
        /// beyond the target, so the mount completes the pass even if the target dodges or doubles back. If
        /// navigation reports blocked terrain, the same safe endpoint remains active through coast/turnaround
        /// so ordinary pursuit cannot immediately walk the mount back into the rejected abyss.</summary>
        public Vector2? NavigationWaypoint => !DriveMovement
            && (Mode == MountMode.Charge || (_navigatorBlocked
                && (Mode == MountMode.Overshoot || Mode == MountMode.TurnAround)))
            ? _navigationWaypoint
            : null;

        /// <summary>Ticks spent actively trying to move while barely moving. This controller has NO
        /// pathfinding by design (a charge commits to a straight line), so terrain it can't hop is a dead
        /// end — the owner watches this to trigger an unstick, e.g. a teleport.</summary>
        public int StuckTicks { get; private set; }

        /// <summary>True once StuckTicks passes the configured threshold. The owner is expected to act and
        /// then call <see cref="ClearStuck"/>; this does not self-clear.</summary>
        public bool IsStuck => Config.StuckTeleportTicks > 0 && StuckTicks >= Config.StuckTeleportTicks;

        public void ClearStuck()
        {
            StuckTicks = 0;
        }

        /// <summary>Horizontal facing locked in when the current charge began. The charge deliberately
        /// ignores where the target moves to after this point, which is what makes it dodgeable.</summary>
        private int _chargeDirection;

        /// <summary>Sign of (target - npc) on the X axis at charge start. When the live sign differs we
        /// have physically crossed the target, which starts the overshoot measurement.</summary>
        private int _approachSign;

        /// <summary>Set once the charge crosses the target, so overshoot distance is measured from the
        /// crossing point rather than from wherever the charge started.</summary>
        private float _crossingX;
        private bool _hasCrossed;
        private Vector2 _navigationWaypoint;
        private bool _navigatorFinished;
        private bool _navigatorBlocked;

        public EnemyMountController(EnemyMountConfig config)
        {
            Config = config;
        }

        /// <summary>Feeds the external navigator's last result back into this intent controller. Reaching the
        /// waypoint completes a normal pass; finding an abyss or unmakeable route also ends it safely.</summary>
        public void ReportNavigationStatus(SmartFighter4WaypointStatus status)
        {
            if (!DriveMovement && Mode == MountMode.Charge
                && (status == SmartFighter4WaypointStatus.Reached || status == SmartFighter4WaypointStatus.Blocked))
            {
                _navigatorFinished = true;
                _navigatorBlocked = status == SmartFighter4WaypointStatus.Blocked;
            }
        }

        /// <summary>Force a charge from outside the FSM (e.g. an enrage). Ignored mid-charge or on cooldown.</summary>
        public bool RequestCharge(NPC npc, Player target)
        {
            if (Mode != MountMode.Idle || CooldownRemaining > 0)
            {
                return false;
            }

            BeginChargeSequence(npc, target);
            return true;
        }

        public void Tick(NPC npc, Player target)
        {
            // One-tick pulse: consumed by the owner during this same Tick call, cleared for the next.
            SpecialWindupStarted = false;

            if (ModeTimer > 0)
            {
                ModeTimer--;
            }

            if (CooldownRemaining > 0 && Mode == MountMode.Idle)
            {
                CooldownRemaining--;
            }

            if (target == null || !target.active || target.dead)
            {
                Brake(npc, Config.BrakingPower);
                return;
            }

            switch (Mode)
            {
                case MountMode.Idle:
                    TickIdle(npc, target);
                    break;

                case MountMode.Approach:
                    TickApproach(npc, target);
                    break;

                case MountMode.ChargeWindup:
                    TickChargeWindup(npc, target);
                    break;

                case MountMode.Charge:
                    TickCharge(npc, target);
                    break;

                case MountMode.Overshoot:
                    TickOvershoot(npc);
                    break;

                case MountMode.TurnAround:
                    TickTurnAround(npc, target);
                    break;
            }

            // Terrain failsafe, self-driven mode ONLY: a mount pinned against a step can't route around it
            // (there is no pathfinding here), so hop when horizontally blocked. Skipped entirely when a
            // navigator is driving — SF4 handles ledges and jumps properly, and a second opinion on
            // velocity.Y is exactly what produced the mindless hopping.
            bool blockedByTerrain = DriveMovement && npc.collideX && npc.velocity.Y == 0f;

            if (blockedByTerrain)
            {
                npc.velocity.Y = -Config.HopPower;
            }

            // Stuck accounting: only counts while we're actually trying to travel. Decays at 2/tick when
            // moving freely so a brief scrape against a wall never accumulates into a false positive,
            // and resets outright in the planted states where standing still is correct.
            bool tryingToTravel = Mode == MountMode.Approach || Mode == MountMode.Charge;

            if (!tryingToTravel)
            {
                StuckTicks = 0;
            }
            else if (Math.Abs(npc.velocity.X) < 0.6f)
            {
                StuckTicks++;
            }
            else
            {
                StuckTicks = Math.Max(0, StuckTicks - 2);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Mode handlers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Standing regroup between passes. Picks the next pass once the dwell and the charge
        /// cooldown have both elapsed: charge immediately if already at a usable range, else reposition.</summary>
        private void TickIdle(NPC npc, Player target)
        {
            Brake(npc, Config.BrakingPower);
            FaceTarget(npc, target);

            if (ModeTimer > 0 || CooldownRemaining > 0)
            {
                return;
            }

            float horizontalDistance = Math.Abs(target.Center.X - npc.Center.X);

            if (horizontalDistance >= Config.ChargeMinRange && horizontalDistance <= Config.ChargeTriggerRange)
            {
                BeginChargeSequence(npc, target);
                return;
            }

            EnterMode(MountMode.Approach, Config.ApproachMaxTicks);
        }

        /// <summary>Reposition to a usable charge distance. Unlike Charge this DOES track the target, so a
        /// player who keeps running is followed until the range band opens up.</summary>
        private void TickApproach(NPC npc, Player target)
        {
            float offsetX = target.Center.X - npc.Center.X;
            float horizontalDistance = Math.Abs(offsetX);

            // Too close to build up a run: back away from the target instead of toward it.
            int moveDirection = 0;

            if (horizontalDistance < Config.ChargeMinRange)
            {
                moveDirection = -Math.Sign(offsetX);
            }
            else
            {
                moveDirection = Math.Sign(offsetX);
            }

            if (moveDirection == 0)
            {
                moveDirection = npc.direction;
            }

            Accelerate(npc, moveDirection, Config.ApproachTopSpeed, Config.ApproachAcceleration);
            npc.direction = moveDirection;
            npc.spriteDirection = moveDirection;

            bool inChargeBand = horizontalDistance >= Config.ChargeMinRange
                && horizontalDistance <= Config.ChargeTriggerRange;

            if (inChargeBand || ModeTimer <= 0)
            {
                BeginChargeSequence(npc, target);
            }
        }

        /// <summary>The committed run-past. Direction is frozen at entry, so the rider ploughs through the
        /// space the target USED to occupy — dodging sideways beats it, backpedalling does not.</summary>
        private void TickCharge(NPC npc, Player target)
        {
            Accelerate(npc, _chargeDirection, Config.ChargeTopSpeed, Config.ChargeAcceleration);
            npc.direction = _chargeDirection;
            npc.spriteDirection = _chargeDirection;

            // Detect the moment we cross the target, then measure the overshoot from that point.
            int liveSign = Math.Sign(target.Center.X - npc.Center.X);

            if (!_hasCrossed && liveSign != 0 && liveSign != _approachSign)
            {
                _hasCrossed = true;
                _crossingX = npc.Center.X;
            }

            bool overshotFarEnough = DriveMovement && _hasCrossed
                && Math.Abs(npc.Center.X - _crossingX) >= Config.OvershootDistance;

            if (overshotFarEnough || _navigatorFinished || ModeTimer <= 0)
            {
                EnterMode(MountMode.Overshoot, Config.OvershootTicks);
            }
        }

        /// <summary>Planted wind-up before a special charge. Comes to a full stop and holds, which is the
        /// entire tell — a mount that stops dead is about to do something worse than run at you.</summary>
        private void TickChargeWindup(NPC npc, Player target)
        {
            Brake(npc, Config.TurnBraking);
            FaceTarget(npc, target);

            if (ModeTimer <= 0)
            {
                EnterCharge(npc, target);
            }
        }

        /// <summary>Roll whether this pass is the special variant, then enter either the planted wind-up or
        /// a normal charge. Every path into a charge goes through here so the roll can't be bypassed.</summary>
        private void BeginChargeSequence(NPC npc, Player target)
        {
            bool rolledSpecial = Config.SpecialChargeChance > 0
                && Config.ChargeWindupTicks > 0
                && Main.rand.Next(100) < Config.SpecialChargeChance;

            if (rolledSpecial)
            {
                IsSpecialCharge = true;
                SpecialWindupStarted = true;
                EnterMode(MountMode.ChargeWindup, Config.ChargeWindupTicks);
                return;
            }

            IsSpecialCharge = false;
            EnterCharge(npc, target);
        }

        /// <summary>Coast down after the pass. Keeps the charge facing so the deceleration reads as a
        /// gallop running out of room rather than an instant stop.</summary>
        private void TickOvershoot(NPC npc)
        {
            // The special pass is over the moment the run ends — the burning wake stops being laid here.
            IsSpecialCharge = false;

            Brake(npc, Config.OvershootBraking);

            if (ModeTimer <= 0)
            {
                EnterMode(MountMode.TurnAround, Config.TurnAroundTicks);
            }
        }

        /// <summary>Plant and wheel around to face the target again. This is the rider's vulnerable window —
        /// it is deliberately the slowest state, so a player who dodged the pass gets a punish opportunity.</summary>
        private void TickTurnAround(NPC npc, Player target)
        {
            Brake(npc, Config.TurnBraking);
            FaceTarget(npc, target);

            if (ModeTimer <= 0)
            {
                CooldownRemaining = Config.ChargeCooldownTicks;
                EnterMode(MountMode.Idle, Config.IdleDwellTicks);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────

        private void EnterCharge(NPC npc, Player target)
        {
            float offsetX = target.Center.X - npc.Center.X;

            _approachSign = Math.Sign(offsetX);

            if (_approachSign == 0)
            {
                _approachSign = npc.direction;
            }

            _chargeDirection = _approachSign;
            _hasCrossed = false;
            _crossingX = npc.Center.X;
            _navigationWaypoint = target.Center + new Vector2(_chargeDirection * Config.OvershootDistance, 0f);
            _navigatorFinished = false;
            _navigatorBlocked = false;

            EnterMode(MountMode.Charge, Config.ChargeMaxTicks);
        }

        private void EnterMode(MountMode mode, int durationTicks)
        {
            Mode = mode;
            ModeTimer = durationTicks;
        }

        /// <summary>Ramp horizontal velocity toward direction * topSpeed. Vertical motion is left entirely
        /// to normal NPC gravity/collision — this controller only ever writes velocity.X (plus the hop).</summary>
        private void Accelerate(NPC npc, int direction, float topSpeed, float acceleration)
        {
            if (!DriveMovement)
            {
                return;
            }

            float scaledTopSpeed = topSpeed * SpeedScale;
            float desiredVelocity = direction * scaledTopSpeed;

            // Already over the (possibly newly lowered) cap — e.g. the rider just started a cast mid-charge.
            // Bleed down to it instead of accelerating further.
            if (Math.Abs(npc.velocity.X) > scaledTopSpeed)
            {
                npc.velocity.X *= 0.92f;
                return;
            }

            npc.velocity.X += direction * acceleration;

            if (direction > 0 && npc.velocity.X > desiredVelocity)
            {
                npc.velocity.X = desiredVelocity;
            }

            if (direction < 0 && npc.velocity.X < desiredVelocity)
            {
                npc.velocity.X = desiredVelocity;
            }
        }

        /// <summary>Bleed off horizontal speed. Snaps to zero under 0.1 px/f so the mount settles instead of
        /// creeping — a residual sub-pixel drift makes the standing frame jitter.</summary>
        private void Brake(NPC npc, float brakingPower)
        {
            if (!DriveMovement)
            {
                return;
            }

            npc.velocity.X *= 1f - brakingPower;

            if (Math.Abs(npc.velocity.X) < 0.1f)
            {
                npc.velocity.X = 0f;
            }
        }

        private void FaceTarget(NPC npc, Player target)
        {
            int facing = Math.Sign(target.Center.X - npc.Center.X);

            if (facing == 0)
            {
                return;
            }

            npc.direction = facing;
            npc.spriteDirection = facing;
        }
    }

    public enum MountMode
    {
        Idle,
        Approach,
        ChargeWindup,
        Charge,
        Overshoot,
        TurnAround
    }

    public struct EnemyMountConfig
    {
        /// <summary>Cruise speed while repositioning between passes.</summary>
        public float ApproachTopSpeed;
        public float ApproachAcceleration;

        /// <summary>Speed of the committed run-past. Should be clearly faster than ApproachTopSpeed or the
        /// charge doesn't read as a charge.</summary>
        public float ChargeTopSpeed;
        public float ChargeAcceleration;

        /// <summary>Idle/turn deceleration per tick, as a fraction of current speed (0-1).</summary>
        public float BrakingPower;
        /// <summary>Deceleration during the post-pass coast. Lower than BrakingPower so it glides out.</summary>
        public float OvershootBraking;
        /// <summary>Deceleration while wheeling around. Highest of the three — it plants to turn.</summary>
        public float TurnBraking;

        /// <summary>Horizontal band (px) in which a charge may start. Below min it backs off first; above
        /// max it closes distance first.</summary>
        public float ChargeMinRange;
        public float ChargeTriggerRange;

        /// <summary>How far (px) past the target the charge runs before decelerating.</summary>
        public float OvershootDistance;

        /// <summary>Upward velocity used to hop a blocked step. There is no pathfinding here by design.</summary>
        public float HopPower;

        /// <summary>Phase durations (ticks). ChargeMaxTicks also caps a charge that never crosses the
        /// target (e.g. the player outran it), preventing an endless run.</summary>
        /// <summary>Ticks of blocked travel before <see cref="EnemyMountController.IsStuck"/> trips.
        /// 0 disables stuck detection entirely.</summary>
        public int StuckTeleportTicks;

        /// <summary>Legacy contact threshold retained for source compatibility. Navigator-driven charges now
        /// finish at their committed safe waypoint instead of pulling up when they touch the player.</summary>
        public float ChargeContactRange;

        /// <summary>Chance (0-100) that a given pass becomes the special variant: plant, wind up, then
        /// charge. 0 disables it entirely and no wind-up ever occurs.</summary>
        public int SpecialChargeChance;
        /// <summary>How long the mount stands planted before a special charge. This is the whole telegraph,
        /// so it wants to be long enough to be unmistakable.</summary>
        public int ChargeWindupTicks;

        public int ApproachMaxTicks;
        public int ChargeMaxTicks;
        public int OvershootTicks;
        public int TurnAroundTicks;
        public int IdleDwellTicks;
        public int ChargeCooldownTicks;

        /// <summary>Reasonable defaults for a medium cavalry enemy.</summary>
        public static EnemyMountConfig Default => new EnemyMountConfig
        {
            ApproachTopSpeed    = 4.0f,
            ApproachAcceleration = 0.14f,
            ChargeTopSpeed      = 9.5f,
            ChargeAcceleration  = 0.42f,
            BrakingPower        = 0.14f,
            OvershootBraking    = 0.055f,
            TurnBraking         = 0.22f,
            ChargeMinRange      = 160f,   // 10 tiles — needs room to build speed
            ChargeTriggerRange  = 900f,   // ~56 tiles
            OvershootDistance   = 220f,   // runs ~14 tiles past before pulling up
            HopPower            = 8.5f,
            StuckTeleportTicks  = 90,
            ChargeContactRange  = 90f,
            SpecialChargeChance = 0,      // opt-in per enemy; 0 means the wind-up never fires
            ChargeWindupTicks   = 90,
            ApproachMaxTicks    = 180,
            ChargeMaxTicks      = 150,
            OvershootTicks      = 35,
            TurnAroundTicks     = 40,
            IdleDwellTicks      = 30,
            ChargeCooldownTicks = 60,
        };
    }
}
