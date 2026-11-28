using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.ID;

namespace tsorcRevamp.NPCs.AI
{
    /// <summary>
    /// Movement-only flight helper for any NPC that wants to take off, hover, strafe, and dive.
    /// Pure AI — has no opinion on how the NPC is drawn.
    ///   • Player-puppet NPCs (PuppetNPC) read
    ///     <see cref="WingsActiveThisTick"/> to drive vanilla wing flap animation.
    ///   • Custom-sprite NPCs (TibianValkyrie, etc.) read <see cref="WingAnimPhase"/>
    ///     to advance their own wing-frame strip via EnemyWingsDrawLayer.
    ///
    /// Calling pattern (from NPC.AI()):
    ///   _flight ??= new EnemyFlightController(MyFlightConfig);
    ///   _flight.Tick(NPC, Main.player[NPC.target]);
    ///   if (_flight.IsAirborne) { /* skip FighterAI, fly */ } else { /* normal ground AI */ }
    ///
    /// External requests:
    ///   _flight.RequestTakeoff(reason);   — switches Grounded → TakeOff if cooldown ready
    ///   _flight.RequestDive(targetPos);   — switches Hover/Strafe → DiveAttack
    ///   _flight.RequestLand();            — switches any aerial mode → Land
    /// </summary>
    public class EnemyFlightController
    {
        public EnemyFlightConfig Config;
        public FlightMode Mode { get; private set; } = FlightMode.Grounded;
        /// <summary>Counts EnterMode calls. Every machine counts its own transitions, so a client can tell a
        /// server mode change it has not reached from one it already predicted (see ReadNetworkState).</summary>
        public int ModeSequence { get; private set; }
        public int ModeTimer { get; private set; }
        public int FlightTicksRemaining { get; private set; }
        public int CooldownRemaining { get; private set; }
        /// <summary>Ticks of low/zero movement while airborne.  Triggers ceiling-clearance
        /// scan and eventually a forced land if we can't unstick.</summary>
        public int StuckTicks { get; private set; }
        /// <summary>+1 / -1 / 0 horizontal scan direction when seeking ceiling clearance.</summary>
        private int _clearanceSeekDir;
        /// <summary>Locked overhead gap found while routing past a bridge or ceiling; cleared once feet pass above it.</summary>
        private bool _hasClearanceGap;
        private float _clearanceGapX;
        private float _clearanceCeilingY;

        /// <summary>Current Y offset from the ideal hover altitude.  Accumulates downward each tick;
        /// springs negative (above target) when the threshold is hit, producing the natural bob.</summary>
        private float _driftY;
        /// <summary>Ticks remaining in the current upward wing-flap beat.</summary>
        private int _flapBurstTicks;

        /// <summary>Current dive/hover/strafe target in world coordinates.</summary>
        public Vector2 TargetWaypoint { get; private set; }

        /// <summary>Opt-in: while set, Hover holds this world point instead of circling the target (no drift bob), so a
        /// locked aim line or beam keeps its origin. Set by the owner's server-side attack logic; synced.</summary>
        public Vector2? HoverAnchor;

        /// <summary>Land: -1/+1 while gliding sideways to find dry ground (never descends into a pit or liquid), 0 otherwise.</summary>
        private int _landSeekDirection;

        /// <summary>True when the wings should be visibly flapping this tick.
        /// False during glide / strafe / dive-fold.  Drives flap-and-glide cadence.</summary>
        public bool WingsActiveThisTick { get; private set; }

        /// <summary>0..1 phase of the wing flap cycle.  Only advances when WingsActiveThisTick.
        /// Custom-sprite renderers sample this to pick a wing strip frame.</summary>
        public float WingAnimPhase { get; private set; }

        /// <summary>True for any non-Grounded mode.</summary>
        public bool IsAirborne => Mode != FlightMode.Grounded;

        /// <summary>True for DiveAttack mode — the NPC AI may want to spawn a hitbox.</summary>
        public bool IsDiving => Mode == FlightMode.DiveAttack;

        public EnemyFlightController(EnemyFlightConfig config)
        {
            Config = config;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Public requests
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Request takeoff.  Ignored if already airborne or cooldown not ready.</summary>
        public bool RequestTakeoff()
        {
            if (Mode != FlightMode.Grounded || CooldownRemaining > 0) return false;
            EnterMode(FlightMode.TakeOff, Config.TakeOffTicks);
            return true;
        }

        /// <summary>Switch to dive toward an explicit point (e.g. player.Center).</summary>
        public bool RequestDive(Vector2 worldTarget)
        {
            if (Mode == FlightMode.Grounded || Mode == FlightMode.Land) return false;
            TargetWaypoint = worldTarget;
            EnterMode(FlightMode.DiveAttack, Config.DiveAttackTicks);
            return true;
        }

        /// <summary>Switch to a horizontal strafing pass at the current altitude.</summary>
        public bool RequestStrafe(Vector2 worldTarget)
        {
            if (Mode == FlightMode.Grounded || Mode == FlightMode.Land) return false;
            TargetWaypoint = worldTarget;
            EnterMode(FlightMode.Strafe, Config.StrafeTicks);
            return true;
        }

        /// <summary>Drop out of flight this tick, no landing glide — the owner takes over velocity with
        /// gravity (e.g. a leap slam launched from the air). Starts the normal takeoff cooldown.</summary>
        public void EndFlightNow()
        {
            if (Mode == FlightMode.Grounded) return;
            CooldownRemaining = Config.CooldownTicks;
            EnterMode(FlightMode.Grounded, 0);
        }

        /// <summary>Begin landing sequence.</summary>
        public void RequestLand()
        {
            if (Mode == FlightMode.Grounded || Mode == FlightMode.Land) return;
            EnterMode(FlightMode.Land, Config.LandTicks);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Per-tick update
        // ─────────────────────────────────────────────────────────────────────

        public void Tick(NPC npc, Player target)
        {
            if (CooldownRemaining > 0 && Mode == FlightMode.Grounded) CooldownRemaining--;
            if (ModeTimer > 0) ModeTimer--;

            // ── Stuck detection (airborne only) ──────────────────────────────────
            // Accumulate ticks when barely moving while airborne — either pinned against
            // a ceiling, jammed in a cliff edge, or pushed flat against a wall.
            // 60 ticks -> start ceiling-clearance scan; 180 ticks -> force land.
            if (IsAirborne && Mode != FlightMode.Land && Mode != FlightMode.TakeOff)
            {
                if (npc.velocity.LengthSquared() < 0.6f * 0.6f)
                    StuckTicks++;
                else
                    StuckTicks = Math.Max(0, StuckTicks - 2);

                if (StuckTicks > 180)
                {
                    EnterMode(FlightMode.Land, Config.LandTicks);
                    StuckTicks = 0;
                }
            }
            else
            {
                StuckTicks = 0;
            }

            switch (Mode)
            {
                case FlightMode.Grounded:
                    npc.noGravity = false;
                    WingsActiveThisTick = false;
                    break;

                case FlightMode.TakeOff:
                    TickTakeOff(npc, target);
                    break;

                case FlightMode.Hover:
                    TickHover(npc, target);
                    break;

                case FlightMode.Strafe:
                    TickStrafe(npc, target);
                    break;

                case FlightMode.DiveAttack:
                    TickDive(npc, target);
                    break;

                case FlightMode.Land:
                    TickLand(npc, target);
                    break;
            }

            // Decrement total airborne budget; force landing if it runs out.
            // InfiniteFlight keeps the NPC airborne until its owner explicitly requests landing.
            if (IsAirborne && Mode != FlightMode.Land && !Config.InfiniteFlight)
            {
                FlightTicksRemaining--;
                if (FlightTicksRemaining <= 0)
                {
                    EnterMode(FlightMode.Land, Config.LandTicks);
                }
            }

            // Advance wing flap phase only while flapping (glide freezes mid-cycle).
            if (WingsActiveThisTick)
            {
                WingAnimPhase += Config.WingFlapSpeed;
                if (WingAnimPhase >= 1f) WingAnimPhase -= 1f;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Mode handlers
        // ─────────────────────────────────────────────────────────────────────

        private void TickTakeOff(NPC npc, Player target)
        {
            npc.noGravity = true;
            // If head is blocked by a ceiling, suppress climb and slide sideways toward open air.
            if (HeadIsBlocked(npc))
            {
                npc.velocity.Y = Math.Max(npc.velocity.Y, 0.5f); // gentle drop away from ceiling
                int seek = SeekCeilingClearanceDir(npc);
                if (seek != 0)
                    npc.velocity.X = MathHelper.Lerp(npc.velocity.X, seek * Config.HoverTopSpeed, 0.18f);
                WingsActiveThisTick = true; // wings keep working even while pinned
            }
            else
            {
                // Ceiling-aware climb: slow down over the last stretch (0.12 px/t per px of room left) so it
                // settles at the allowed height instead of bonking the ceiling. Open sky = full TakeOffSpeed.
                float highestAllowedY = ClampBelowCeiling(npc, npc.Center.X, float.NegativeInfinity);
                float climbSpeed = Config.TakeOffSpeed;
                bool underCeiling = !float.IsNegativeInfinity(highestAllowedY);

                if (underCeiling)
                {
                    float roomLeft = npc.Center.Y - highestAllowedY;
                    climbSpeed = MathHelper.Clamp(roomLeft * 0.12f, 0f, Config.TakeOffSpeed);

                    if (roomLeft <= 4f)
                    {
                        EnterMode(FlightMode.Hover, Config.HoverDwellTicks);
                        return;
                    }
                }

                npc.velocity.Y = MathHelper.Lerp(npc.velocity.Y, -climbSpeed, 0.20f);
                npc.velocity.X *= 0.85f;
                WingsActiveThisTick = true;
            }

            if (ModeTimer <= 0 || ReachedHoverAltitude(npc))
                EnterMode(FlightMode.Hover, Config.HoverDwellTicks);
        }

        private void TickHover(NPC npc, Player target)
        {
            npc.noGravity = true;
            // Hover above the player at HoverAltitude, with a side bias so we don't sit
            // directly overhead (gives a more menacing "circle the prey" feel).
            float sideOffset = npc.Center.X < target.Center.X ? -Config.HoverSideOffset : Config.HoverSideOffset;
            Vector2 desired = new Vector2(target.Center.X + sideOffset, target.Center.Y - Config.HoverAltitude);

            // When ceiling awareness is on and the player is not above the ceiling, clamp before
            // checking wantsToClimb so a ceiling bump drops back to room height instead of routing
            // sideways looking for a hole in the roof.
            bool targetAboveCeiling = target.Center.Y < npc.position.Y - 16f;
            if (Config.CeilingClearanceFraction > 0f && !targetAboveCeiling)
            {
                desired.Y = ClampBelowCeiling(npc, desired.X, desired.Y);
            }

            // ── Ceiling-clearance and wall-climb routing ──────────────────────
            // Under a bridge or roof while trying to rise, find the nearest body-wide opening
            // (up to 64 tiles away), steer under it, then climb straight through until feet clear.
            bool wantsToClimb = desired.Y < npc.Center.Y - 16f;
            bool headBlocked = HeadIsBlocked(npc);

            if (_hasClearanceGap)
            {
                float horizontalToGap = Math.Abs(_clearanceGapX - npc.Center.X);
                bool clearedCeiling = npc.Bottom.Y <= _clearanceCeilingY;
                bool blockedBeforeGap = npc.collideX && horizontalToGap > 12f;

                if (clearedCeiling || !wantsToClimb || blockedBeforeGap)
                {
                    _hasClearanceGap = false;
                    _clearanceSeekDir = 0;
                }
            }

            bool centeringThroughGap = false;

            if (HoverAnchor.HasValue)
            {
                // Station-keeping for a locked attack: no drift, no circling; the spring below eases onto the point.
                desired = HoverAnchor.Value;
                _hasClearanceGap = false;
                _clearanceSeekDir = 0;
                _driftY = 0f;
                _flapBurstTicks = 0;
            }
            else if (_hasClearanceGap)
            {
                float gapDeltaX = _clearanceGapX - npc.Center.X;
                _driftY = 0f;
                _flapBurstTicks = 0;

                if (Math.Abs(gapDeltaX) > 6f)
                {
                    float slideY = headBlocked ? npc.Center.Y + 4f : npc.Center.Y;
                    desired = new Vector2(_clearanceGapX, slideY);
                }
                else
                {
                    // Aligned under the opening: climb straight up and gently center to clear narrow 2-tile shafts.
                    desired = new Vector2(_clearanceGapX, _clearanceCeilingY - npc.height - 16f);
                    centeringThroughGap = true;
                }
            }
            else if ((headBlocked && wantsToClimb) || StuckTicks > 60)
            {
                if (headBlocked && TryFindCeilingOpening(npc, target, out float gapCenterX, out float ceilingTopY))
                {
                    _hasClearanceGap = true;
                    _clearanceGapX = gapCenterX;
                    _clearanceCeilingY = ceilingTopY;
                    _clearanceSeekDir = gapCenterX < npc.Center.X ? -1 : 1;
                    desired = new Vector2(gapCenterX, npc.Center.Y + 4f);
                }
                else
                {
                    if (_clearanceSeekDir != 0 && npc.collideX)
                    {
                        _clearanceSeekDir = -_clearanceSeekDir;
                    }

                    int seek = _clearanceSeekDir != 0 ? _clearanceSeekDir : SeekCeilingClearanceDir(npc);
                    if (seek == 0)
                    {
                        seek = npc.direction;
                    }

                    _clearanceSeekDir = seek;
                    desired = new Vector2(npc.Center.X + seek * 96f, npc.Center.Y + 4f);
                }

                // Suppress drift during ceiling routing so we don't fight the clearance logic.
                _driftY = 0f;
                _flapBurstTicks = 0;
            }
            else if (npc.collideX && !headBlocked)
            {
                // Lateral wall in the way while sky above is open: climb over the wall instead of stalling.
                _clearanceSeekDir = 0;
                _driftY = 0f;
                _flapBurstTicks = 0;
                desired = new Vector2(desired.X, Math.Min(desired.Y, npc.Center.Y - 96f));
            }
            else
            {
                _clearanceSeekDir = 0;
                // ── Natural gravity-like hover drift ──────────────────────────
                // The invader slowly sinks below its target altitude (as if gravity tugs it
                // down), then fires a short wing-flap burst to spring back up.  The cycle
                // produces an organic bobbing cadence with visible wing beats.
                if (Config.HoverDriftSpeed > 0f)
                {
                    _driftY += Config.HoverDriftSpeed;
                    if (_driftY >= Config.HoverDriftMax)
                    {
                        // Spring above target and begin the flap beat.
                        _driftY = -Config.HoverFlapRise;
                        _flapBurstTicks = Config.HoverFlapBurstTicks;
                    }
                    if (_flapBurstTicks > 0) _flapBurstTicks--;
                    // Offset the desired Y so the velocity spring chases the drifted target.
                    desired.Y += _driftY;
                }

                // After the drift, so the flap-burst spring-up can't lift it into the ceiling either.
                desired.Y = ClampBelowCeiling(npc, desired.X, desired.Y);
            }
            TargetWaypoint = desired;

            Vector2 toTarget = desired - npc.Center;
            Vector2 desiredVel = toTarget;
            float dist = desiredVel.Length();
            if (dist > 0.01f)
            {
                desiredVel.Normalize();
                desiredVel *= MathHelper.Min(dist * 0.06f, Config.HoverTopSpeed);
            }
            npc.velocity = Vector2.Lerp(npc.velocity, desiredVel, 0.10f);

            if (centeringThroughGap)
            {
                float centerCorrectionX = MathHelper.Clamp((_clearanceGapX - npc.Center.X) * 0.35f, -2f, 2f);
                npc.velocity.X = MathHelper.Lerp(npc.velocity.X, centerCorrectionX, 0.45f);
            }

            // Flap during the upward beat, when climbing hard, turning sharply, or ceiling-pinned.
            // During the downward drift the wings stay spread open (glide) — callers read
            // IsGlidingThisTick to pick the spread-open wing frame instead of animating.
            bool climbing = npc.velocity.Y < -0.4f;
            bool turning  = Math.Abs(desiredVel.X - npc.velocity.X) > 0.6f;
            bool stuck    = HeadIsBlocked(npc) || StuckTicks > 30;
            WingsActiveThisTick = _flapBurstTicks > 0 || climbing || turning || stuck;

            // Face the player while hovering
            npc.direction = target.Center.X < npc.Center.X ? -1 : 1;
            npc.spriteDirection = npc.direction;

            if (ModeTimer <= 0)
            {
                // Default back to hover refresh; outer AI is expected to trigger dive/strafe.
                EnterMode(FlightMode.Hover, Config.HoverDwellTicks);
            }
        }

        private void TickStrafe(NPC npc, Player target)
        {
            npc.noGravity = true;
            // Maintain altitude, accelerate horizontally toward TargetWaypoint.X past the player.
            float dx = TargetWaypoint.X - npc.Center.X;
            float desiredVx = Math.Sign(dx) * Config.HoverTopSpeed * 1.3f;
            npc.velocity.X = MathHelper.Lerp(npc.velocity.X, desiredVx, 0.18f);

            // Gentle vertical correction so we don't drift off altitude, plus a sine-wave arc
            // (rises through the middle of the sweep, settles back at the ends) so a strafing
            // pass — like the breath sweep — reads as a natural swoop instead of a flat line.
            float targetY = target.Center.Y - Config.HoverAltitude;
            if (Config.StrafeArcHeight > 0f && Config.StrafeTicks > 0)
            {
                float t = 1f - (float)ModeTimer / Config.StrafeTicks;
                targetY -= (float)Math.Sin(t * Math.PI) * Config.StrafeArcHeight;
            }
            targetY = ClampBelowCeiling(npc, npc.Center.X, targetY);
            float yErr = targetY - npc.Center.Y;
            npc.velocity.Y = MathHelper.Lerp(npc.velocity.Y, MathHelper.Clamp(yErr * 0.06f, -2f, 2f), 0.15f);

            npc.direction = desiredVx < 0 ? -1 : 1;
            npc.spriteDirection = npc.direction;
            WingsActiveThisTick = false; // glide

            if (ModeTimer <= 0 || Math.Abs(dx) < 16f)
                EnterMode(FlightMode.Hover, Config.HoverDwellTicks);
        }

        private void TickDive(NPC npc, Player target)
        {
            npc.noGravity = true;
            Vector2 toTarget = TargetWaypoint - npc.Center;
            float distanceToWaypoint = toTarget.Length();
            if (toTarget.LengthSquared() > 1f) toTarget.Normalize();
            npc.velocity += toTarget * Config.DiveAcceleration;
            float maxSpeed = Config.DiveTopSpeed;
            if (npc.velocity.Length() > maxSpeed)
            {
                Vector2 n = npc.velocity;
                n.Normalize();
                npc.velocity = n * maxSpeed;
            }

            // First half: wings folded for the dive read.  Last quarter: pull up — flap to brake.
            float t = Config.DiveAttackTicks > 0
                ? 1f - (float)ModeTimer / Config.DiveAttackTicks
                : 1f;
            WingsActiveThisTick = t > 0.75f;

            npc.direction = npc.velocity.X < 0 ? -1 : 1;
            npc.spriteDirection = npc.direction;

            // End in any direction once the waypoint is reached or passed. The old Y-only check
            // immediately cancelled horizontal/upward dives whenever the NPC began below the player.
            bool committedForSeveralTicks = t > 0.12f;
            bool passedWaypoint = committedForSeveralTicks
                && Vector2.Dot(TargetWaypoint - npc.Center, npc.velocity) <= 0f;
            if (ModeTimer <= 0 || distanceToWaypoint <= 24f || passedWaypoint)
            {
                EnterMode(FlightMode.Hover, Config.HoverDwellTicks);
            }
        }

        private void TickLand(NPC npc, Player target)
        {
            // Scan across the full hitbox width (not just center X) so a rock catching the left or right foot
            // registers as the landing surface.
            FindFootprintGround(npc, out float groundY, out bool groundHasLiquid, out bool groundHasLava);
            bool groundFound = groundY < npc.Center.Y + NoGroundDistance;
            bool allowSubmergedLanding = Config.Amphibious && !npc.lavaWet && !groundHasLava;
            bool safeLanding = groundFound && (!groundHasLiquid || allowSubmergedLanding);

            // If Terraria tile collision has already set the NPC down on a safe surface (solid ledge or platform,
            // never a ceiling bounce), finish landing immediately instead of flapping forever in Land mode.
            bool feetTouchingSolid = Collision.SolidCollision(new Vector2(npc.position.X, npc.Bottom.Y), npc.width, 4);
            bool standableUnderFeet = feetTouchingSolid || (groundFound && (groundY - npc.Bottom.Y) <= 4f);
            bool stoppedOnSafeFloor = (!npc.wet || allowSubmergedLanding) && safeLanding && standableUnderFeet
                && (npc.collideY || (Math.Abs(npc.velocity.Y) < 0.1f && ModeTimer < Config.LandTicks - 5));

            if (stoppedOnSafeFloor)
            {
                npc.noGravity = false;
                WingsActiveThisTick = false;
                CooldownRemaining = Config.CooldownTicks;
                EnterMode(FlightMode.Grounded, 0);
                return;
            }

            // Never descend into a pit or unsafe liquid: hold altitude and glide toward the nearest safe column.
            // If no safe column exists in range or the land timer expires (e.g. walled off), resume Hover instead
            // of freezing in place in Land mode. Replenish FlightTicksRemaining so Tick() does not immediately
            // force Land again on the same tick.
            if (!safeLanding)
            {
                if (_landSeekDirection == 0)
                {
                    _landSeekDirection = FindDryGroundDirection(npc, target);
                }

                if (_landSeekDirection == 0 || ModeTimer <= 0)
                {
                    if (!Config.InfiniteFlight && FlightTicksRemaining <= 0)
                    {
                        FlightTicksRemaining = Config.HoverDwellTicks;
                    }

                    EnterMode(FlightMode.Hover, Config.HoverDwellTicks);
                    return;
                }

                Vector2 aheadPosition = npc.position + new Vector2(_landSeekDirection * 8f, 0f);
                bool wallAhead = _landSeekDirection != 0 && Collision.SolidCollision(aheadPosition, npc.width, npc.height);

                if (wallAhead)
                {
                    _landSeekDirection = -_landSeekDirection;
                }

                float seekVelocityX = _landSeekDirection * Config.HoverTopSpeed;
                npc.noGravity = true;
                npc.velocity.X = MathHelper.Lerp(npc.velocity.X, seekVelocityX, 0.15f);
                npc.velocity.Y = MathHelper.Lerp(npc.velocity.Y, 0f, 0.2f);
                WingsActiveThisTick = true;
                return;
            }

            _landSeekDirection = 0;
            float feetY = npc.position.Y + npc.height;
            float distFeet = groundY - feetY;

            // Phase 1 — still well above ground: powered descent with flapping.
            if (distFeet > 24f)
            {
                npc.noGravity = true;
                npc.velocity.Y = MathHelper.Lerp(npc.velocity.Y, Config.LandSpeed, 0.20f);
                npc.velocity.X *= 0.90f;
                WingsActiveThisTick = true;
                return;
            }

            // Phase 2 — close to ground: drop noGravity so vanilla physics finish the landing.
            // Wings tuck just before touchdown.
            npc.noGravity = false;
            WingsActiveThisTick = false;

            // Phase 3 — feet have touched ground (velocity zeroed by collision) OR safety timeout.
            // Either path: cooldown + return to Grounded so ground AI resumes.
            if (npc.velocity.Y == 0f || ModeTimer <= 0)
            {
                CooldownRemaining = Config.CooldownTicks;
                EnterMode(FlightMode.Grounded, 0);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────

        private void EnterMode(FlightMode mode, int duration)
        {
            Mode = mode;
            ModeTimer = duration;
            ModeSequence++;
            _landSeekDirection = 0;
            if (mode == FlightMode.TakeOff)
            {
                FlightTicksRemaining = Config.MaxFlightTicks;
            }
            // Reset drift and ceiling-escape state when leaving hover so it doesn't bleed into other modes.
            if (mode != FlightMode.Hover)
            {
                _driftY = 0f;
                _flapBurstTicks = 0;
                _clearanceSeekDir = 0;
                _hasClearanceGap = false;
            }
        }

        private bool ReachedHoverAltitude(NPC npc)
        {
            // Reached cruising altitude if vertical velocity has flattened off.
            return npc.velocity.Y > -0.5f;
        }

        /// <summary>True if the NPC's head is pressed against a solid tile (4px above body top; excludes platforms
        /// since flying upward passes through tileSolidTop).</summary>
        private bool HeadIsBlocked(NPC npc)
        {
            int leftTile  = (int)(npc.position.X / 16f);
            int rightTile = (int)((npc.position.X + npc.width - 1f) / 16f);
            int topTile   = (int)((npc.position.Y - 4f) / 16f);
            for (int x = leftTile; x <= rightTile; x++)
            {
                if (x < 0 || x >= Main.maxTilesX || topTile < 0 || topTile >= Main.maxTilesY)
                {
                    continue;
                }

                Tile tile = Framing.GetTileSafely(x, topTile);
                if (tile.HasUnactuatedTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType])
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Opt-in ceiling awareness (Config.CeilingClearanceFraction > 0). Returns desiredCenterY pushed down
        /// so the NPC's head stays CeilingClearanceFraction of the floor-to-ceiling gap (at least CeilingMinPadding px)
        /// below the ceiling over column x. No ceiling within CeilingScanTiles, or the feature off = unchanged.
        /// A room too low for that headroom puts the NPC halfway between floor and ceiling instead.</summary>
        private float ClampBelowCeiling(NPC npc, float x, float desiredCenterY)
        {
            if (Config.CeilingClearanceFraction <= 0f)
            {
                return desiredCenterY;
            }

            const int CeilingScanTiles = 30;
            int leftTile = (int)((x - npc.width * 0.5f) / 16f);
            int rightTile = (int)((x + npc.width * 0.5f - 1f) / 16f);
            int startRow = (int)(npc.position.Y / 16f);

            // Lowest ceiling over any column the body covers. Bottom edge of that tile, in px.
            float ceilingY = float.NegativeInfinity;
            for (int column = leftTile; column <= rightTile; column++)
            {
                if (column < 0 || column >= Main.maxTilesX)
                {
                    continue;
                }

                for (int row = startRow; row > startRow - CeilingScanTiles && row >= 0; row--)
                {
                    Tile tile = Framing.GetTileSafely(column, row);
                    if (tile.HasUnactuatedTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType])
                    {
                        float tileBottom = (row + 1) * 16f;
                        ceilingY = Math.Max(ceilingY, tileBottom);
                        break;
                    }
                }
            }

            if (float.IsNegativeInfinity(ceilingY))
            {
                return desiredCenterY;
            }

            // Floor under the column's centre; with none in range, measure the gap to the NPC's feet instead.
            int centerColumn = (int)(x / 16f);
            float floorY = npc.Bottom.Y;
            for (int row = startRow; row < startRow + CeilingScanTiles && row < Main.maxTilesY; row++)
            {
                if (centerColumn < 0 || centerColumn >= Main.maxTilesX)
                {
                    break;
                }

                Tile tile = Framing.GetTileSafely(centerColumn, row);
                bool standable = tile.HasUnactuatedTile && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]);
                if (standable && row * 16f > ceilingY)
                {
                    floorY = row * 16f;
                    break;
                }
            }

            float gap = floorY - ceilingY;
            float headroom = Math.Max(gap * Config.CeilingClearanceFraction, Config.CeilingMinPadding);
            float highestCenterY = ceilingY + headroom + npc.height * 0.5f;
            float lowestCenterY = floorY - npc.height * 0.5f - 4f;

            if (highestCenterY > lowestCenterY)
            {
                return Math.Max(desiredCenterY, (ceilingY + floorY) * 0.5f);
            }

            return Math.Max(desiredCenterY, highestCenterY);
        }

        /// <summary>Scan outward up to 64 tiles left and right for the nearest body-wide opening through the ceiling
        /// row above the NPC (stopping a side early if a wall blocks body height). Prefers the side toward target on ties.</summary>
        private bool TryFindCeilingOpening(NPC npc, Player target, out float gapCenterX, out float ceilingTopY)
        {
            const int MaxOpeningSearchTiles = 64;
            const int RequiredClearRows = 4;

            gapCenterX = npc.Center.X;
            ceilingTopY = npc.position.Y;

            int ceilingRow = (int)((npc.position.Y - 4f) / 16f);
            int bodyTopRow = (int)(npc.position.Y / 16f);
            int bodyBottomRow = (int)((npc.Bottom.Y - 1f) / 16f);
            int bodyWidthTiles = Math.Max(1, (int)Math.Ceiling(npc.width / 16f));
            int centerTileX = (int)(npc.Center.X / 16f);

            int firstSide = 1;
            if (target != null && target.Center.X < npc.Center.X)
            {
                firstSide = -1;
            }

            bool firstSideBlocked = false;
            bool secondSideBlocked = false;

            for (int offset = 1; offset <= MaxOpeningSearchTiles; offset++)
            {
                for (int sideIndex = 0; sideIndex < 2; sideIndex++)
                {
                    int side = sideIndex == 0 ? firstSide : -firstSide;
                    if ((sideIndex == 0 && firstSideBlocked) || (sideIndex == 1 && secondSideBlocked))
                    {
                        continue;
                    }

                    int probeColumn = centerTileX + side * offset;
                    if (probeColumn < 1 || probeColumn >= Main.maxTilesX - bodyWidthTiles)
                    {
                        if (sideIndex == 0)
                        {
                            firstSideBlocked = true;
                        }
                        else
                        {
                            secondSideBlocked = true;
                        }
                        continue;
                    }

                    // Stop scanning past a solid wall at the NPC's current flight altitude.
                    bool wallAtBodyHeight = false;
                    for (int row = bodyTopRow; row <= bodyBottomRow; row++)
                    {
                        if (row < 0 || row >= Main.maxTilesY)
                        {
                            continue;
                        }

                        Tile wallTile = Framing.GetTileSafely(probeColumn, row);
                        if (wallTile.HasUnactuatedTile && Main.tileSolid[wallTile.TileType] && !Main.tileSolidTop[wallTile.TileType])
                        {
                            wallAtBodyHeight = true;
                            break;
                        }
                    }

                    if (wallAtBodyHeight)
                    {
                        if (sideIndex == 0)
                        {
                            firstSideBlocked = true;
                        }
                        else
                        {
                            secondSideBlocked = true;
                        }
                        continue;
                    }

                    // Check if a body-wide vertical shaft starting at this column is open through the ceiling.
                    int startColumn = side < 0 ? probeColumn - bodyWidthTiles + 1 : probeColumn;
                    bool openingClear = true;

                    for (int column = startColumn; column < startColumn + bodyWidthTiles && openingClear; column++)
                    {
                        if (column < 0 || column >= Main.maxTilesX)
                        {
                            openingClear = false;
                            break;
                        }

                        for (int row = ceilingRow; row > ceilingRow - RequiredClearRows && row >= 0; row--)
                        {
                            Tile tile = Framing.GetTileSafely(column, row);
                            if (tile.HasUnactuatedTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType])
                            {
                                openingClear = false;
                                break;
                            }
                        }
                    }

                    if (openingClear)
                    {
                        gapCenterX = (startColumn + bodyWidthTiles * 0.5f) * 16f;
                        ceilingTopY = ceilingRow * 16f;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>Scan ±96px from the NPC for the side with more overhead clearance.
        /// Returns +1 (right), -1 (left), or 0 if neither side is clearly better.</summary>
        private int SeekCeilingClearanceDir(NPC npc)
        {
            int yCheck = (int)((npc.position.Y - 16f) / 16f);
            int npcX   = (int)(npc.Center.X / 16f);

            int leftClear  = ScanColumnsForOpenCeiling(npcX - 6, yCheck);
            int rightClear = ScanColumnsForOpenCeiling(npcX + 6, yCheck);

            if (leftClear == rightClear) return 0;
            return rightClear > leftClear ? 1 : -1;
        }

        /// <summary>Count of how many tile-rows above (yStart, x) are non-solid (more = more clearance).</summary>
        private int ScanColumnsForOpenCeiling(int x, int yStart)
        {
            if (x < 0 || x >= Main.maxTilesX) return 0;
            int clear = 0;
            for (int y = yStart; y > yStart - 8 && y >= 0; y--)
            {
                Tile t = Framing.GetTileSafely(x, y);
                if (t.HasUnactuatedTile && Main.tileSolid[t.TileType]) break;
                clear++;
            }
            return clear;
        }

        /// <summary>Scan downward from the NPC for the first tile that can stop a fall.
        /// Recognises solid tiles AND platforms (jump-through tiles like Wood Platform).
        /// Without the platform check, an invader landing on a wood platform would never
        /// register "landed" — FindGroundY would skip past the platform looking for a deeper
        /// fully-solid tile, and TickLand would keep flapping forever.</summary>
        private float FindGroundY(float worldX, float worldY)
        {
            int x = (int)(worldX / 16f);
            int yStart = (int)(worldY / 16f);
            for (int y = yStart; y < yStart + 64; y++)
            {
                if (x < 0 || x >= Main.maxTilesX || y < 0 || y >= Main.maxTilesY) break;
                Tile t = Framing.GetTileSafely(x, y);
                if (!t.HasUnactuatedTile) continue;
                // Solid tile: standard ground.  TileSolidTop: platforms (Wood Platform etc.).
                if (Main.tileSolid[t.TileType] || Main.tileSolidTop[t.TileType])
                    return y * 16f;
            }
            return worldY + NoGroundDistance; // no ground within 64 tiles: a pit
        }

        /// <summary>FindGroundY's "nothing below" offset (px); anything at or past it counts as no ground.</summary>
        private const float NoGroundDistance = 1024f;

        /// <summary>Columns searched each side for dry ground when Land starts over a pit or liquid (tiles).</summary>
        private const int DryGroundSearchTiles = 40;

        /// <summary>True when the tile resting on the ground at groundY holds liquid, with isLava set for lava.</summary>
        private static bool IsLiquidAbove(float worldX, float groundY, out bool isLava)
        {
            isLava = false;
            int x = (int)(worldX / 16f);
            int y = (int)(groundY / 16f) - 1;

            if (x < 0 || x >= Main.maxTilesX || y < 0 || y >= Main.maxTilesY)
            {
                return false;
            }

            Tile tile = Framing.GetTileSafely(x, y);
            if (tile.LiquidAmount <= 0)
            {
                return false;
            }

            isLava = tile.LiquidType == LiquidID.Lava;
            return true;
        }

        /// <summary>Scans every tile column under the NPC's hitbox width for the highest standable surface
        /// (solid tile or platform), so landing on a rock edge detects the surface supporting the outer foot.</summary>
        private void FindFootprintGround(NPC npc, out float highestGroundY, out bool groundHasLiquid, out bool groundHasLava)
        {
            int leftTile = (int)(npc.position.X / 16f);
            int rightTile = (int)((npc.position.X + npc.width - 1f) / 16f);
            float scanStartY = Math.Max(npc.Center.Y, npc.Bottom.Y - 4f);
            highestGroundY = npc.Center.Y + NoGroundDistance;
            groundHasLiquid = false;
            groundHasLava = false;

            for (int column = leftTile; column <= rightTile; column++)
            {
                float columnWorldX = column * 16f + 8f;
                float columnGroundY = FindGroundY(columnWorldX, scanStartY);

                if (columnGroundY < highestGroundY)
                {
                    highestGroundY = columnGroundY;
                    groundHasLiquid = IsLiquidAbove(columnWorldX, columnGroundY, out bool columnHasLava);
                    groundHasLava = columnHasLava;
                }
            }
        }

        /// <summary>Nearest side (-1/+1) with safe landing ground under the NPC's current height, checking outward
        /// tile by tile and preferring the side toward target on ties; 0 if neither side has any within DryGroundSearchTiles.</summary>
        private int FindDryGroundDirection(NPC npc, Player target)
        {
            int firstSide = -1;
            if (target != null && target.Center.X > npc.Center.X)
            {
                firstSide = 1;
            }

            for (int offsetTiles = 1; offsetTiles <= DryGroundSearchTiles; offsetTiles++)
            {
                for (int sideIndex = 0; sideIndex < 2; sideIndex++)
                {
                    int side = sideIndex == 0 ? firstSide : -firstSide;
                    float columnX = npc.Center.X + side * offsetTiles * 16f;
                    float columnGroundY = FindGroundY(columnX, npc.Center.Y);
                    // Ground must be below the feet: a hit at body height is a wall, not somewhere to land.
                    bool columnHasGround = columnGroundY < npc.Center.Y + NoGroundDistance && columnGroundY > npc.Bottom.Y;

                    if (columnHasGround)
                    {
                        bool columnHasLiquid = IsLiquidAbove(columnX, columnGroundY, out bool columnHasLava);
                        bool liquidSafe = !columnHasLiquid || (Config.Amphibious && !columnHasLava);

                        if (liquidSafe)
                        {
                            return side;
                        }
                    }
                }
            }

            return 0;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Multiplayer
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Server side of the owner's SendExtraAI. Takeoff, dive and strafe requests come from
        /// server-only attack decisions, so a client's controller only learns about them from this.</summary>
        public void WriteNetworkState(BinaryWriter writer)
        {
            writer.Write(ModeSequence);
            writer.Write((byte)Mode);
            writer.Write((short)Math.Clamp(ModeTimer, 0, short.MaxValue));
            writer.Write((short)Math.Clamp(FlightTicksRemaining, short.MinValue, short.MaxValue));
            writer.Write((short)Math.Clamp(CooldownRemaining, 0, short.MaxValue));
            writer.WriteVector2(TargetWaypoint);
            writer.Write(HoverAnchor.HasValue);

            if (HoverAnchor.HasValue)
            {
                writer.WriteVector2(HoverAnchor.Value);
            }
        }

        /// <summary>Client side. Adopts the server's mode unless this client already predicted past it (a
        /// landing or strafe end reached a tick early). The skip is bounded to 2 transitions so a client that
        /// over-predicted cannot ignore the server forever, and a grounded client always follows a takeoff.</summary>
        public void ReadNetworkState(BinaryReader reader)
        {
            int sequence = reader.ReadInt32();
            FlightMode mode = (FlightMode)reader.ReadByte();
            int modeTimer = reader.ReadInt16();
            int flightTicksRemaining = reader.ReadInt16();
            int cooldownRemaining = reader.ReadInt16();
            Vector2 waypoint = reader.ReadVector2();
            bool hasAnchor = reader.ReadBoolean();
            Vector2? anchor = null;

            if (hasAnchor)
            {
                anchor = reader.ReadVector2();
            }

            // The anchor is server intent like the waypoint: adopted even when the mode itself was predicted ahead.
            HoverAnchor = anchor;

            int predictedAhead = ModeSequence - sequence;
            bool olderThanPrediction = predictedAhead > 0 && predictedAhead <= 2;
            bool groundedWhileServerFlies = Mode == FlightMode.Grounded && mode != FlightMode.Grounded;
            if (olderThanPrediction && !groundedWhileServerFlies)
            {
                return;
            }

            if (mode != Mode)
            {
                EnterMode(mode, modeTimer);
            }
            FlightTicksRemaining = flightTicksRemaining;
            CooldownRemaining = cooldownRemaining;
            TargetWaypoint = waypoint;
            ModeSequence = sequence;
        }
    }

    public enum FlightMode
    {
        Grounded,
        TakeOff,
        Hover,
        Strafe,
        DiveAttack,
        Land
    }

    public struct EnemyFlightConfig
    {
        /// <summary>Pixels above the player to maintain during Hover.</summary>
        public float HoverAltitude;
        /// <summary>Horizontal offset from directly-above-player during Hover (creates side circling).</summary>
        public float HoverSideOffset;
        /// <summary>Cruise speed while hovering / strafing.</summary>
        public float HoverTopSpeed;
        /// <summary>Upward velocity during TakeOff.</summary>
        public float TakeOffSpeed;
        /// <summary>Per-tick velocity accumulation during DiveAttack.</summary>
        public float DiveAcceleration;
        /// <summary>Cap on dive velocity magnitude.</summary>
        public float DiveTopSpeed;
        /// <summary>Downward velocity during Land.</summary>
        public float LandSpeed;
        /// <summary>Max total airborne time before forced landing.</summary>
        public int   MaxFlightTicks;
        /// <summary>When true, FlightTicksRemaining never forces a landing — the owner controls landing explicitly
        /// (used by flyers that must cross large gaps or climb out of deep pits to reach the player).</summary>
        public bool  InfiniteFlight;
        /// <summary>When true, non-lava liquid (water/honey/shimmer) counts as valid ground during Land so the
        /// flyer can fight and recover underwater without abandoning a submerged target to seek dry land.</summary>
        public bool  Amphibious;
        /// <summary>Cooldown after landing before next takeoff allowed.</summary>
        public int   CooldownTicks;

        /// <summary>Phase durations.</summary>
        public int   TakeOffTicks;
        public int   HoverDwellTicks;
        public int   StrafeTicks;
        public int   DiveAttackTicks;
        public int   LandTicks;

        /// <summary>Wing flap cycle speed (phase units per tick while flapping).</summary>
        public float WingFlapSpeed;

        // ── Hover drift (natural gravity bob) ────────────────────────────────
        /// <summary>Pixels per tick the invader sinks during idle hover.  0 disables drift.</summary>
        public float HoverDriftSpeed;
        /// <summary>How far below target altitude (px) before the invader flaps back up.</summary>
        public float HoverDriftMax;
        /// <summary>How far above target altitude (px) the flap burst springs the invader.
        /// Combined with HoverDriftMax this sets the full bob range.</summary>
        public float HoverFlapRise;
        /// <summary>Duration of active wing flapping per upward beat (ticks).
        /// Between beats the wings stay in the spread-open glide position.</summary>
        public int HoverFlapBurstTicks;

        /// <summary>Peak vertical rise (px) of the sine-wave arc during a Strafe pass.
        /// 0 disables the arc (flat strafe line).</summary>
        public float StrafeArcHeight;

        // ── Ceiling awareness (opt-in) ───────────────────────────────────────
        /// <summary>Under a ceiling, keep the NPC's head at least this fraction of the floor-to-ceiling gap below
        /// the ceiling (0.33 = the top third of the room stays empty). TakeOff eases to a stop there and
        /// Hover/Strafe targets are pushed down to it. 0 = off: only the old head-bump clearance routing.</summary>
        public float CeilingClearanceFraction;
        /// <summary>Minimum headroom in px when CeilingClearanceFraction is on, for very low rooms.</summary>
        public float CeilingMinPadding;

        /// <summary>Reasonable defaults for medium-size invaders.</summary>
        public static EnemyFlightConfig Default => new EnemyFlightConfig
        {
            HoverAltitude      = 200f,
            HoverSideOffset    = 80f,
            HoverTopSpeed      = 4.5f,
            TakeOffSpeed       = 8f,
            DiveAcceleration   = 0.55f,
            DiveTopSpeed       = 11f,
            LandSpeed          = 6f,
            MaxFlightTicks     = 720,   // 12 s
            CooldownTicks      = 240,   // 4 s
            TakeOffTicks       = 35,
            HoverDwellTicks    = 90,
            StrafeTicks        = 60,
            DiveAttackTicks    = 50,
            LandTicks          = 90,
            WingFlapSpeed      = 0.08f, // ~12 ticks per flap
            // Hover drift: sinks ~25 px over ~1.5 s, then flaps 18 px above target.
            // Full bob range ≈ 43 px; one beat every ~2.5 s.
            HoverDriftSpeed    = 0.28f,
            HoverDriftMax      = 25f,
            HoverFlapRise      = 18f,
            HoverFlapBurstTicks = 18,
            StrafeArcHeight    = 0f, // off by default; opt in per-invader
            CeilingClearanceFraction = 0f, // off by default; opt in per-invader
            CeilingMinPadding  = 24f,
        };
    }
}
