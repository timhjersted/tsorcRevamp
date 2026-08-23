using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs
{
    // SmartFighter3 â€” Test 4 ground enemy AI — SF3 pathfinding + physics-based jumps.
    //
    // Three pillars added in this version (vs the prior Nav iteration):
    //
    //  1. Calibrated jump-arc table. Edges are only proposed when a (dx, dy)
    //     pair has a physically tested entry. No more guess-and-pray jump power.
    //
    //  2. Genuine action commitment. Once a step starts executing (especially
    //     a jump or drop), the plan is LOCKED for the duration. The replan
    //     trigger cannot fire mid-flight just because the player moved.
    //
    //  3. Goal-aware A*. Heuristic weights vertical distance to the player
    //     5Ã— horizontal. Drop edges that move AWAY from the player's vertical
    //     level get a large cost penalty so the NPC stops walking off cliffs
    //     toward a player who is actually above.
    //
    // Self-contained — does not touch the legacy SmartFighterAI (SF1) globals.
    public static class SmartFighter4AI
    {
        // ---- Window / search tuning ----
        // Upper bound on the per-NPC NavSearchRadius span window. Raised 50->80 so a same-room player
        // a bit over 50 tiles away is still reachable by A* (the old 50 cap caused "no-goal-span" ->
        // no plan -> dumb-chase wedged on a ledge). Smaller per-NPC NavSearchRadius still = dumber+cheaper.
        private const int ScanRadiusX = 80;
        private const int ScanRadiusY = 48;
        // Max tiles a single free-fall / platform-drop edge may span. Raised 10->24: a player one floor/cliff
        // below (a house ledge is often 12-20 tiles) was unreachable when there was no intermediate landing,
        // so the NPC just stood at the edge and refused to drop. NPCs take no fall damage and HasDropClearance
        // still requires a clear shaft; deeper drops cost more and are penalized when the player is ABOVE, so
        // this only enables descending toward a lower target, not suicidal pit-diving.
        private const int MaxDropDepth = 24;
        private const int MaxJumpRiseTiles = 30;
        private const int StepTimeoutFrames = 180;
        private const float TileF = 16f;
        private const float AlignTolerancePx = 12f;
        // Minimum acceleration used when settling onto a jump launch column (see ExecJump). Decouples
        // alignment precision from the enemy's chase acceleration so low-accel pursuers still line up jumps.
        private const float AlignAccelFloor = 0.1f;

        // Replan cooldown is the minimum frames between *successful* replans.
        // The committed action lock overrides this entirely while an action runs.
        private const int ReplanCooldown = 60;

        // ---- Calibrated jump arcs ----
        // Indexed by (|dx|, dy) tiles. dy > 0 means rise (jumping up),
        // dy < 0 means drop (jumping across with a fall).
        // Values: (jumpPower, horizontalBoost). Calibrated assuming
        // MaxJumpPower = 9, MaxJumpBoost = 5, gravity ~0.3, topSpeed ~1.55.
        // Physical reach approximation:
        //   apex_height â‰ˆ power^2 / (2 * 0.3) tiles  (~135px at power 9 = 8 tiles)
        //   airtime â‰ˆ 2 * power / 0.3 frames        (~60 frames at power 9)
        //   horizontal_reach â‰ˆ airtime * (topSpeed + boost)
        // The table below was tuned against in-game test runs and is the only
        // place where jump power is decided. Edges with no entry are not proposed.
        private static readonly Dictionary<(int dx, int dy), (float power, float boost)> JumpArcs =
            new Dictionary<(int, int), (float, float)>
        {
            // ---- Vertical-emphasis (rise > horizontal). With MaxJumpPower=9 and
            //      gravity 0.3, NPCs can reach ~8 tiles up. Earlier table capped at 5
            //      which left them stuck in any pit deeper than a step.
            {( 0, 8), (9.0f, 0.0f)},
            {( 0, 7), (9.0f, 0.0f)},
            {( 0, 6), (9.0f, 0.0f)},
            {( 0, 5), (8.5f, 0.0f)},
            {( 0, 4), (8.0f, 0.0f)},
            {( 0, 3), (7.0f, 0.0f)},
            {( 0, 2), (6.0f, 0.0f)},
            {( 0, 1), (5.0f, 0.0f)},
            {( 1, 8), (9.0f, 0.5f)},
            {( 1, 7), (9.0f, 0.7f)},
            {( 1, 6), (9.0f, 1.0f)},
            {( 1, 5), (9.0f, 1.0f)},
            {( 1, 4), (8.0f, 1.0f)},
            {( 1, 3), (7.0f, 1.0f)},
            {( 1, 2), (6.0f, 1.0f)},
            {( 2, 7), (9.0f, 1.5f)},
            {( 2, 6), (9.0f, 1.8f)},
            {( 2, 5), (9.0f, 2.0f)},
            {( 2, 4), (8.5f, 1.8f)},
            {( 2, 3), (7.5f, 2.0f)},
            {( 2, 2), (6.5f, 2.0f)},
            {( 3, 6), (9.0f, 2.5f)},
            {( 3, 5), (8.5f, 2.7f)},
            {( 3, 4), (8.0f, 2.8f)},
            {( 3, 3), (7.5f, 2.8f)},
            {( 3, 2), (6.5f, 2.8f)},
            {( 3, 1), (6.0f, 2.8f)},
            {( 4, 5), (8.5f, 3.3f)},
            {( 4, 4), (8.0f, 3.4f)},
            {( 4, 3), (7.5f, 3.5f)},
            {( 4, 2), (6.5f, 3.5f)},
            {( 4, 1), (6.0f, 3.5f)},
            {( 4, 0), (5.5f, 3.5f)},
            {( 5, 4), (8.0f, 4.0f)},
            {( 5, 3), (7.5f, 4.0f)},
            {( 5, 2), (6.5f, 4.0f)},
            {( 5, 1), (6.0f, 4.0f)},
            {( 5, 0), (5.5f, 4.0f)},
            {( 6, 3), (7.5f, 4.5f)},
            {( 6, 2), (6.5f, 4.5f)},
            {( 6, 1), (6.0f, 4.5f)},
            {( 6, 0), (5.5f, 4.5f)},
            {( 7, 1), (6.0f, 5.0f)},
            {( 7, 0), (5.5f, 5.0f)},

            // ---- Drop-assisted horizontal (jumping across with a fall) ----
            {( 5, -1), (5.5f, 4.0f)},
            {( 6, -1), (5.5f, 4.5f)},
            {( 6, -2), (5.0f, 4.5f)},
            {( 7, -1), (5.0f, 5.0f)},
            {( 7, -2), (4.5f, 5.0f)},
            {( 8, -1), (5.0f, 5.0f)},
            {( 8, -2), (4.5f, 5.0f)},
            {( 8, -3), (4.0f, 5.0f)},
            {( 9, -2), (4.5f, 5.0f)},
            {( 9, -3), (4.0f, 5.0f)},
            {( 9, -4), (3.5f, 5.0f)},
            {(10, -3), (4.0f, 5.0f)},
            {(10, -4), (3.5f, 5.0f)},
            {(10, -5), (3.0f, 5.0f)},
        };

        private static readonly Dictionary<int, NavState> States = new Dictionary<int, NavState>();

        public static bool HasActiveMovementPlan(NPC npc)
        {
            if (!States.TryGetValue(npc.whoAmI, out NavState nav))
            {
                return false;
            }

            return nav.IsCommitted
                || nav.AirCommitTimer > 0
                || nav.PlatformPassActive
                || nav.RopeEngaged
                || nav.RopeDismounting
                || (nav.Plan != null && nav.PlanIndex < nav.Plan.Count);
        }

        /// <summary>
        /// Releases physics that SF4 temporarily owns while climbing a rope. Shared AI states such as
        /// Patrol, Flee, and teleport can take control before SF4 gets another movement frame, so they
        /// must explicitly hand the body back to normal gravity instead of leaving an orphaned rope ride.
        /// </summary>
        internal static void ReleaseRopeTraversal(NPC npc, bool abandonRopeStep = true)
        {
            if (!States.TryGetValue(npc.whoAmI, out NavState nav))
            {
                return;
            }

            bool activeRopeStep = nav.Plan != null && nav.PlanIndex < nav.Plan.Count
                && nav.Plan[nav.PlanIndex].Kind == StepKind.RopeClimb;
            bool ownsRopePhysics = npc.noGravity || nav.RopeEngaged || nav.RopeDismounting || activeRopeStep;
            if (!ownsRopePhysics)
            {
                return;
            }

            bool physicsChanged = npc.noGravity || (npc.noTileCollide && !nav.PlatformPassActive);
            npc.noGravity = false;
            if (!nav.PlatformPassActive)
            {
                npc.noTileCollide = false;
            }

            nav.RopeEngaged = false;
            nav.RopeDirLatched = false;
            nav.RopeDescend = false;
            nav.RopeDismounting = false;
            nav.RopeJumpedThisStep = false;
            nav.RopeStallFrames = 0;
            nav.LastRopeFeetY = 0;

            if (abandonRopeStep)
            {
                if (activeRopeStep)
                {
                    nav.Plan = null;
                    nav.PlanIndex = 0;
                }
                nav.CommitFrames = 0;
                nav.AirCommitTimer = 0;
                nav.AirCommitDirX = 0;
                nav.CommittedLaunchVx = 0f;
                nav.ReplanCooldown = 0;
            }
            if (physicsChanged)
            {
                npc.netUpdate = true;
            }
        }

        /// <summary>Returns the latest movement decision in a compact form for puppet attack telemetry.</summary>
        internal static bool TryGetTelemetryState(NPC npc, out string action, out string reason, out string plan)
        {
            if (!States.TryGetValue(npc.whoAmI, out NavState nav))
            {
                action = reason = plan = "";
                return false;
            }

            action = nav.LastAction ?? "";
            reason = nav.LastReason ?? "";
            plan = nav.LastTelemetryPlan ?? "none";
            return true;
        }

        /// <summary>Keep combat-owned SF4 frames in the same diagnostic stream as ordinary movement.</summary>
        internal static void LogCombatSeizure(NPC npc, bool lineOfSight)
        {
            if (!npc.HasValidTarget)
            {
                return;
            }

            Player player = Main.player[npc.target];
            NavState state = GetState(npc);
            tsorcRevampGlobalNPC globalNPC = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();
            string action = globalNPC.CombatMeleeActive ? "combat-melee"
                : globalNPC.InGuardPressureRecovery ? "guard-recovery"
                : globalNPC.TeleportCountdown > 0 || globalNPC.TeleportAppearanceTimer > 0 ? "teleport"
                : globalNPC.PounceTimer > 0 ? "pounce"
                : globalNPC.DodgeTimer > 0 ? "dodge"
                : globalNPC.InSustainedEvasion ? "evasion"
                : "combat-seize";
            string reason = globalNPC.CombatMeleeActive
                ? $"key={globalNPC.ActiveCombatMeleeKey} tick={globalNPC.ActiveCombatMeleeTimer}"
                : "combat owns movement";

            LogFrame(npc, player, state, IsGrounded(npc), lineOfSight, globalNPC.InAttack, action, reason);
        }

        // movementOnly (Phase 2 Step 4): act as BasicAI's pluggable MOVEMENT substrate instead of a standalone
        // driver. When true, BasicAI already advanced the shared FSM and fired combat this frame, so SF4 must NOT
        // re-run UpdateState (side effects → double give-up clock) or fire its own attacks (double shot) — it
        // reads globalNPC.PursuitState and only moves. holdForAttack = combat wants to stop-and-fire → hold this frame.
        // Defaults false → standalone behavior (Puppets, TibianValkyrieSmart4 testbed) still owns its own
        // combat, but shares the grounded overspeed brake below.
        public static void Run(NPC npc, float topSpeed = 1.55f, float acceleration = 0.10f,
            int doorBreakingDamage = 4, float attackRange = 700f, bool movementOnly = false, bool holdForAttack = false,
            float brakingPower = 0.2f)
        {
            Player player = Main.player[npc.target];
            if (!player.active || player.dead)
            {
                npc.TargetClosest(true);
                player = Main.player[npc.target];
            }
            if (!player.active || player.dead)
            {
                npc.velocity.X *= 0.94f;
                return;
            }

            NavState nav = GetState(npc);
            tsorcRevampGlobalNPC globalNPC = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();
            _minSurfaceWidth = globalNPC.MinSurfaceWidth; // beast flat-ground gate for IsStandableTile this frame (0 = off)
            _maxSurfaceStep = globalNPC.MaxSurfaceStep;   // tolerated footprint unevenness (Deerclops-clip failsafe)
            // Headroom: beasts (MinSurfaceWidth > 0) require their full sprite height of clearance; others keep the
            // old fixed 3 tiles. Auto-derived from the sprite so a tall beast won't path into a too-short space.
            _clearanceHeight = globalNPC.MinSurfaceWidth > 0 ? Math.Max(3, (int)Math.Ceiling(npc.height / 16f)) : 3;
            _canUseRopes = globalNPC.CanUseRopes; // per-enemy rope opt-in (default off)
            _canPassWalls = globalNPC.CanPassThroughWalls; // ghosts: refuse wall-wedging near-vertical jump launches (pit escape)
            float jumpCeil = Math.Max(globalNPC.MaxJumpPower, 5f);
            float boostCeil = Math.Max(globalNPC.MaxJumpBoost, 2f);

            TickTimers(nav);
            ManagePlatformPass(nav, npc);
            bool activeRopeStep = nav.Plan != null && nav.PlanIndex < nav.Plan.Count
                && nav.Plan[nav.PlanIndex].Kind == StepKind.RopeClimb;
            if (npc.noGravity && !activeRopeStep)
            {
                // Defensive recovery for a shared-AI early return that abandoned a rope between SF4 frames.
                ReleaseRopeTraversal(npc);
            }
            // Safety: never leave noTileCollide stuck on outside an active platform pass OR a rope
            // climb (noGravity), or the NPC can phase through solid walls (the "passed through a
            // wall" bug). Rope climb legitimately uses noTileCollide and is excluded here.
            if (npc.noTileCollide && !nav.PlatformPassActive && !npc.noGravity) npc.noTileCollide = false;
            bool grounded = IsGrounded(npc);

            // ── Reactive evasion (armed by EvasiveOnHit; inert without an EvasiveProfile) ──────
            // Mirror FighterAI's evasion bookkeeping so SF4 enemies get the same reactive moves:
            // tick the rate-limit cooldown + the sustained-window driver, hold during a RunningDash
            // telegraph, yield to a RetreatAndShoot back-off, and apply the dash burst's speed
            // multiplier.  Fully inert for enemies that never arm an evasion.
            if (globalNPC.FighterEvasionCooldown > 0)
                globalNPC.FighterEvasionCooldown--;
            tsorcRevampAIs.UpdateEvasion(npc, globalNPC);
            if (globalNPC.InSustainedEvasion)
            {
                if (globalNPC.CurrentEvasion == EvasiveBehavior.RunningDash && globalNPC.EvasiveTelegraphing)
                {
                    npc.velocity.X *= 0.6f; // hold position while the dash telegraph flashes
                    return;
                }
                if (globalNPC.CurrentEvasion == EvasiveBehavior.RetreatAndShoot)
                    return; // UpdateEvasion already drives the grounded back-off — don't fight it
                if (globalNPC.CurrentEvasion == EvasiveBehavior.RunningDash)
                    topSpeed *= globalNPC.EvasiveDashSpeedMult; // burst: keep pursuing, but fast
            }
            else if (!grounded && globalNPC.FighterEvasionCooldown > 30)
            {
                // A one-shot evasion hop (LeapForward / RetreatDash / RetreatJump / TeleportAway)
                // just launched — let the ballistic arc carry it instead of re-steering toward the
                // player mid-air.
                return;
            }

            // Landing from an SF4 jump can leave much more horizontal speed than normal walking
            // allows. Bleed that overspeed immediately while grounded so path jumps do not feel
            // like ice-skating after touchdown.
            ApplyGroundedOverspeedBrake(npc, topSpeed, brakingPower);

            // Airborne X-velocity lock — only active during a committed jump action.
            // SF4: re-assert the exact launch velocity computed by ComputeJumpArc so the
            // physics arc lands where intended. (SF3 clamped to topSpeed*1.3 here, which erased
            // the horizontal launch speed every frame and made the NPC fall short of gaps.)
            // Drops and other commits with no computed arc fall back to the old gentle clamp.
            if (!grounded && nav.AirCommitTimer > 0 && nav.AirCommitDirX != 0)
            {
                if (nav.CommittedLaunchVx != 0f)
                {
                    npc.velocity.X = nav.CommittedLaunchVx;
                }
                else
                {
                    float t = nav.AirCommitDirX * topSpeed * 1.05f;
                    npc.velocity.X = MathHelper.Clamp(t, -topSpeed * 1.3f, topSpeed * 1.3f);
                }
            }

            // Shaft escape (see TryShaftEscape): launched straight up out of a pit. Rise vertically (vx=0) until the
            // target side clears the overhang lip, THEN drift onto the ledge. A ballistic up-and-over arc can't clear
            // a lip that sits IN its path; this late-drift is the only way out. Cleared when we land.
            if (grounded)
            {
                nav.ShaftEscapeDir = 0;
            }
            else if (nav.ShaftEscapeDir != 0)
            {
                int escFeetY = GetFeetTileY(npc);
                // Column just past the body's LEADING edge in the drift direction (full-width aware, so a wide body
                // only drifts once its actual edge — not just the center+1 column — has cleared the lip).
                int escSide = nav.ShaftEscapeDir > 0
                    ? (int)((npc.position.X + npc.width) / TileF)
                    : (int)(npc.position.X / TileF) - 1;
                bool sideClear = !IsNavigationSolid(escSide, escFeetY)
                              && !IsNavigationSolid(escSide, escFeetY - 1)
                              && !IsNavigationSolid(escSide, escFeetY - 2);
                // Drift a touch faster than the (possibly low health-scaled) top speed so it reliably carries onto
                // the ledge before gravity pulls it back past the lip.
                npc.velocity.X = sideClear ? nav.ShaftEscapeDir * Math.Max(topSpeed, 2.5f) : 0f;
            }

            // ---- Macro-state FSM (Pursue / Search / Patrol) ----
            // Strict raycast LOS: a solid tile anywhere between the NPC and player blocks firing/navigation
            // decisions. Platforms remain transparent, matching projectile telegraph gating.
            bool los = HasStrictLineOfSight(npc.Center, player.Center);
            // Progress = closing on the player, OR actively executing a path (so a multi-tile reroute
            // that temporarily moves away isn't read as "stuck" and made to give up mid-route).
            float distNow = npc.Distance(player.Center);
            bool madeProgress = distNow < nav.LastPursuitDist - 1f
                || (nav.Plan != null && nav.PlanIndex < nav.Plan.Count);
            nav.LastPursuitDist = distNow;
            // movementOnly: BasicAI already advanced the shared FSM this frame; calling UpdateState again would
            // double-step its side effects (give-up clock, re-aggro, last-known-pos). Read the state it set.
            PursuitState pstate = movementOnly
                ? globalNPC.PursuitState
                : NavBehavior.UpdateState(npc, globalNPC, player, los, madeProgress, attackRange);

            string actionLabel = "idle", reasonLabel = "";
            bool actionHandled = false;

            // Hold-to-fire (movementOnly): combat (RunFighterCombat in BasicAI) wants to stop-and-fire, so hold
            // position this frame and skip pathing — the shot comes from BasicAI, not SF4. Mirrors halt-attack.
            // Suppressed while a plan step is in progress: the NPC should finish navigating to its destination
            // before standing still to shoot. Without this guard an archer on a raised tile zeroes its own X
            // velocity before it can step down, bouncing on the ledge edge indefinitely.
            bool hasPlanStep = nav.Plan != null && nav.PlanIndex < nav.Plan.Count;
            int holdDir = player.Center.X >= npc.Center.X ? 1 : -1;
            bool needsStepUp = grounded && !hasPlanStep && HasOneTileStepAhead(npc, holdDir);
            bool navigationRequiresHold = TryGetFiringHoldReason(npc, player, holdDir, los, out string holdReason);
            if (movementOnly && holdForAttack && pstate != PursuitState.Patrol && !hasPlanStep && !needsStepUp
                && navigationRequiresHold)
            {
                if (Math.Abs(npc.velocity.X) < 0.3f) npc.velocity.X = 0f;
                else npc.velocity.X *= 0.6f;
                npc.direction = holdDir; npc.spriteDirection = holdDir;
                actionHandled = true;
                actionLabel = "hold-fire"; reasonLabel = holdReason;
            }
            else if (pstate == PursuitState.Patrol)
            {
                // Gave up the chase: patrol instead of pathing/standing-forever. Drop any stale plan.
                ReleaseRopeTraversal(npc);
                nav.Plan = null; nav.PlanIndex = 0; nav.CommitFrames = 0; nav.StuckGiveUpFrames = 0;
                NavBehavior.RunPatrol(npc, globalNPC, topSpeed, acceleration);
                actionHandled = true;
                actionLabel = "patrol"; reasonLabel = globalNPC.PatrolMode.ToString();
            }
            else
            {
                // Kiting / beast-contact override: a ranged kiter holds / backs off to keep its preferred distance
                // from a same-level player; a large beast bobs back out of melee after a touch (anti sprite-center
                // glitch). Either takes priority over the pursuit plan + reactive chase below. For a beast this only
                // fires during the post-contact back-off window — otherwise A* still navigates it in to touch.
                bool kiting = grounded && !nav.IsCommitted &&
                    (globalNPC.RequiresFlatGround
                        ? TryBeastContactBackoff(nav, npc, player, globalNPC, acceleration, out actionLabel, out reasonLabel)
                        : TryRangedKite(nav, npc, player, globalNPC, topSpeed, acceleration, los, out actionLabel, out reasonLabel));
                if (kiting)
                {
                    actionHandled = true;
                    if (nav.Plan != null) { nav.Plan = null; nav.PlanIndex = 0; } // drop the pursue-into-melee plan
                }

                // PURSUE / SEARCH. Replan only when pathfinding is enabled (NavSearchRadius > 0) and not
                // mid-commit. Radius 0 = no global pathfinding: clear any plan and chase on sight.
                if (!kiting && globalNPC.NavSearchRadius > 0)
                {
                    // Commit to a jump in progress: IsCommitted only covers the airborne arc, NOT the grounded
                    // "walk to the launch column + settle" setup. Without this guard, every replan window (the
                    // player twitches -> A* returns a slightly different jump-staircase) makes the NPC ABANDON
                    // its current approach and start a new one, so it never commits long enough to finish a
                    // climb — the multi-second shuffle + 3-4 aborted jumps at a player perched just above. So
                    // while grounded and actively setting up a jump step, suppress the replan. The escape hatch
                    // is ExecJump's own StepTimer: a jump that can't be completed times out (<=3s), bad-edges
                    // its target, and forces a clean reroute — deliberate commit, then reroute, never churn.
                    bool approachingJump = nav.Plan != null && nav.PlanIndex < nav.Plan.Count
                        && (nav.Plan[nav.PlanIndex].Kind == StepKind.JumpUp || nav.Plan[nav.PlanIndex].Kind == StepKind.JumpGap);
                    if (!nav.IsCommitted && !approachingJump && grounded && nav.ReplanCooldown == 0 && ShouldReplan(nav, npc, player))
                    {
                        Replan(nav, npc, player, topSpeed);
                        nav.ReplanCooldown = ReplanCooldown;
                    }
                }
                else if (!kiting && nav.Plan != null) { nav.Plan = null; nav.PlanIndex = 0; }

                if (!kiting && nav.Plan != null && nav.PlanIndex < nav.Plan.Count)
                {
                    if (globalNPC.RequiresFlatGround) globalNPC.BeastUnreachableFrames = 0; // reachable: A* is navigating us in to touch
                    globalNPC.UnreachableFrames = 0; // generalized version of the line above — has a real plan, so reachable
                    actionHandled = ExecuteStep(nav, npc, player, topSpeed, acceleration,
                        jumpCeil, boostCeil, grounded, doorBreakingDamage, globalNPC,
                        out actionLabel, out reasonLabel);
                }
                else if (!kiting && grounded && globalNPC.RequiresFlatGround)
                {
                    // Large beast with no path to the player: NEVER stand still. Oscillate in/out within the large
                    // preferred band instead of the standoff / halt-attack / hold-aligned / chase branches below
                    // (which would freeze or pace robotically under the player). BeastUnreachableFrames ticks here,
                    // feeding the BasicAI stale-wander give-up.
                    BeastOscillate(nav, npc, player, globalNPC, topSpeed, acceleration, out actionLabel, out reasonLabel);
                    actionHandled = true;
                }
                else if (!kiting && grounded)
                {
                    int dir = player.Center.X >= npc.Center.X ? 1 : -1;
                    npc.direction = dir; npc.spriteDirection = dir;

                    // Fallback rope-grab: no plan, player is above, and a rope is within reach.
                    // Synthesizes a one-step RopeClimb plan so the normal executor rides it up.
                    // Safety net for cases the span graph misses (the "just stands at the base" bug).
                    bool inRange = npc.Distance(player.Center) <= attackRange;

                    if (TryGrabNearbyRope(nav, npc, player))
                    {
                        npc.velocity.X *= 0.7f; // settle this frame; executor takes over next frame
                        actionLabel = "rope-grab"; reasonLabel = "fallback";
                        actionHandled = true;
                    }
                    else if (TryShaftEscape(nav, npc, player, jumpCeil, topSpeed, acceleration, out actionLabel, out reasonLabel))
                    {
                        // Boxed under an overhang with the player above → rise straight up the shaft, then drift out.
                        actionHandled = true;
                    }
                    else if (TryRangedStandoff(npc, player, globalNPC, topSpeed, acceleration, los, inRange,
                        out actionLabel, out reasonLabel))
                    {
                        // Can shoot but can't reach (player on a different level) — hold an angle & fire.
                        actionHandled = true;
                    }
                    else
                    {
                        // A navigation hazard may justify a short standing-fire window. Even then, the
                        // halt budget eventually forces a reposition attempt instead of camping forever.
                        bool nearPlayer = inRange && globalNPC.AttackList.Count > 0;
                        string firingHazardReason = "";
                        bool firingHazard = nearPlayer && player.Center.Y <= npc.Center.Y + 24f
                            && TryGetFiringHoldReason(npc, player, dir, los, out firingHazardReason);
                        if (firingHazard && nav.RepositionTimer == 0)
                        {
                            nav.HaltFrames++;
                            if (nav.HaltFrames >= HaltMaxFrames)
                            {
                                nav.HaltFrames = 0;
                                nav.RepositionTimer = RepositionFrames;
                            }
                        }
                        else if (!firingHazard)
                        {
                            nav.HaltFrames = 0;
                        }

                        // Never stand-and-fire at a player below us; descend to pursue instead.
                        bool doHalt = firingHazard && nav.RepositionTimer == 0;
                        if (doHalt)
                        {
                            // Brake to a FULL stop so the idle (feet-together) frame shows instead of
                            // the walk frame — a tiny residual velocity keeps vanilla in the walk anim.
                            if (Math.Abs(npc.velocity.X) < 0.3f) npc.velocity.X = 0f;
                            else npc.velocity.X *= 0.6f;
                            npc.direction = dir; npc.spriteDirection = dir;
                            actionLabel = "halt-attack";
                            reasonLabel = $"{firingHazardReason} d={npc.Distance(player.Center):F0} h={nav.HaltFrames}";
                            actionHandled = true;
                        }
                        else
                        {
                            // The player is BELOW and a real landing exists within MaxDropDepth → let the
                            // drop off the ledge be the way down. Without this, TryLocalTerrain's cliff-halt
                            // brakes the NPC at the edge forever (the "frozen on a ledge above me, won't drop
                            // to chase" exploit). Bounded by GetDropDepth so it won't dive a bottomless pit.
                            bool playerBelow = player.Center.Y > npc.Center.Y + 24f;
                            int frontXc = GetFrontTileX(npc, dir);
                            int feetYc = GetFeetTileY(npc);
                            bool dropLands = GetDropDepth(frontXc, feetYc, MaxDropDepth) <= MaxDropDepth;
                            bool allowCliffDrop = playerBelow && dropLands;
                            actionHandled = TryLocalTerrain(nav, npc, dir, jumpCeil, boostCeil, topSpeed,
                                allowCliffDrop, out actionLabel, out reasonLabel);
                            if (!actionHandled)
                            {
                                // Already lined up under/over the player (and on a different level we can't
                                // reach with no plan): chasing toward their X just oscillates across it —
                                // the "rapid left-right directly below you" jitter. Hold still instead; the
                                // give-up clock then trips -> Patrol, which sweeps for a firing angle.
                                bool xAlignedDiffLevel = Math.Abs(npc.Center.X - player.Center.X) < TileF * 1.5f
                                                         && Math.Abs(player.Center.Y - npc.Center.Y) > 24f;
                                if (allowCliffDrop)
                                {
                                    // Cliff ahead + player below + reachable landing → walk off to descend.
                                    ApplyChase(npc, dir, topSpeed, acceleration);
                                    actionLabel = "chase-drop"; reasonLabel = "player-below-ledge";
                                }
                                else if (xAlignedDiffLevel)
                                {
                                    if (Math.Abs(npc.velocity.X) < 0.3f) npc.velocity.X = 0f;
                                    else npc.velocity.X *= 0.6f;
                                    actionLabel = "hold-aligned"; reasonLabel = "under/over-player,no-path";
                                }
                                else
                                {
                                    // NOTE: this used to re-check IsCliffAhead(dir) here and halt on any
                                    // drop>=4 ahead. That was a SECOND, gap-unaware cliff gate duplicating
                                    // TryLocalTerrain's own drop>=4 cliff-halt (same threshold) — harmless
                                    // when the two always agreed, but TryLocalTerrain now deliberately
                                    // returns false (walk it, no jump needed) for a makeable width-1 gap
                                    // EVEN THOUGH the local drop right at the missing tile is deep. Reaching
                                    // this point already means TryLocalTerrain looked at the terrain ahead
                                    // (walls, gaps, ledges) and found nothing that needs handling — trust
                                    // that verdict instead of re-deriving a cruder one, same as ExecWalk
                                    // does after its own TryLocalTerrain call (SmartFighter4AI.cs:2149).
                                    ApplyChase(npc, dir, topSpeed, acceleration);
                                    actionLabel = "chase"; reasonLabel = "no-plan";
                                }
                            }
                        }
                    }
                }
                else
                {
                    actionLabel = nav.AirCommitTimer > 0 ? "air-commit" : "airborne";
                    reasonLabel = nav.AirCommitTimer > 0 ? $"dirX={nav.AirCommitDirX} t={nav.AirCommitTimer}" : "no-commit";
                }

                // Anti-stuck give-up — keyed on the NPC's OWN immobility, not player distance. The old
                // distance-based test reset constantly from player micro-movement + LOS flicker, so a
                // wedged NPC never disengaged (the "frozen on the fireplace mantle" bug). Here: while
                // pursuing with no plan, uncommitted, and unable to actually engage (no LOS or out of
                // attack range), if our own X hasn't moved for ~2s we can't make progress -> disengage to
                // patrol. The canEngage guard keeps legitimate standing-fire from disengaging.
                //
                // NOTE: deliberately NOT gated on `grounded` (was, until this comment) — a target we have
                // no plan for (e.g. straight up on an unreachable ledge) still provokes reflexive, uncommitted
                // hop attempts that briefly leave the ground every cycle. Gating on grounded reset this timer
                // every one of those hops, so it never actually reached its threshold — the "20+ seconds
                // frozen/oscillating before finally giving up" bug. X-position drift alone is still the
                // progress signal, so a real climb (which moves X) still resets it correctly.
                // LOS + generic attack range alone does not mean a melee-only enemy can actually fight an
                // unreachable target. Preserve legitimate ranged standing-fire and any attack already in progress,
                // but let a neutral melee-only fighter with no path reach the normal give-up flow.
                bool canEngage = los && npc.Distance(player.Center) <= attackRange
                    && (globalNPC.AttackList.Count > 0 || globalNPC.InAttack);
                bool noPathToTarget = pstate == PursuitState.Pursue && !nav.IsCommitted && nav.Plan == null && !canEngage;
                // Generalizes BeastUnreachableFrames to every SF4 enemy — how long this pursuit has had
                // no path at all. Read by the shared FSM (tsorcRevampAIs.cs) to decide whether a hit landed
                // by an unreachable attacker should trigger Flee instead of a normal re-aggro.
                globalNPC.UnreachableFrames = noPathToTarget ? Math.Min(globalNPC.UnreachableFrames + 1, 100000) : 0;
                if (noPathToTarget && globalNPC.CanPassThroughWalls && globalNPC.UnreachableFrames >= 60)
                {
                    int awayDir = Math.Sign(npc.Center.X - player.Center.X);

                    // Fall back down the chain when the player is exactly X-aligned (awayDir 0): keep the
                    // current facing, else right. A 0 here would leave the patrol sweep with no direction.
                    if (awayDir != 0)
                    {
                        globalNPC.PatrolDirection = awayDir;
                    }
                    else if (npc.direction != 0)
                    {
                        globalNPC.PatrolDirection = npc.direction;
                    }
                    else
                    {
                        globalNPC.PatrolDirection = 1;
                    }

                    globalNPC.GhostUnreachableWanderTimer = Math.Max(globalNPC.GhostUnreachableWanderTimer, 300);
                }
                if (noPathToTarget)
                {
                    if (Math.Abs(npc.Center.X - nav.StuckCheckX) > 2f)
                    {
                        nav.StuckCheckX = npc.Center.X;
                        nav.StuckGiveUpFrames = 0;
                    }
                    else if (++nav.StuckGiveUpFrames > 90) // ~1.5s of immobility before giving up to patrol
                    {
                        bool nearbyRecovery = globalNPC.CanTeleport
                            && globalNPC.TeleportStyle == TeleportStyle.RecoveryOnly
                            && globalNPC.TeleportCooldownTimer == 0
                            && (globalNPC.RecoveryTeleportMaxRange <= 0f
                                || npc.Distance(player.Center) <= globalNPC.RecoveryTeleportMaxRange);
                        if (nearbyRecovery)
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient
                                && !tsorcRevampAIs.TryTeleportReacquire(npc, globalNPC))
                            {
                                NavBehavior.ForceDisengage(npc, globalNPC);
                            }
                        }
                        else NavBehavior.ForceDisengage(npc, globalNPC);
                        nav.StuckGiveUpFrames = 0;
                    }
                }
                else { nav.StuckCheckX = npc.Center.X; nav.StuckGiveUpFrames = 0; }

                // Plan-execution immobility guard (the "stuck till I hit it" freeze). The per-step
                // StepTimer escape is defeated for Walk steps because every replan (player twitch → A*
                // rebuild) resets StepTimer to full, so a wedged walk-into-a-lip never times out. This
                // guard is keyed on the NPC's OWN X immobility and is NOT reset by replan — only by real
                // movement — so it survives the StepTimer churn. When a movement step makes no horizontal
                // progress for ~0.66s, bad-edge the step target and drop the plan: the next replan routes
                // AROUND it. If every route is genuinely walled, the bad-edges pile up until A* finds no
                // path → the no-plan StuckGiveUpFrames above then disengages to patrol.
                bool execMove = nav.Plan != null && nav.PlanIndex < nav.Plan.Count && grounded
                    && (actionLabel == "walk" || actionLabel == "align-jump" || actionLabel == "align-drop"
                        || actionLabel == "align-pdrop" || actionLabel == "obstacle-jump"
                        // Fix B: a "blocked" no-headroom/too-tall wall braking in place counts as a wedged step, so
                        // StepNoMoveFrames bad-edges it and A* reroutes around the obstacle instead of standing forever.
                        || actionLabel == "blocked");
                if (execMove)
                {
                    if (Math.Abs(npc.Center.X - nav.MoveCheckX) > 2f) { nav.MoveCheckX = npc.Center.X; nav.StepNoMoveFrames = 0; }
                    else if (++nav.StepNoMoveFrames > 40)
                    {
                        var bs = nav.Plan[nav.PlanIndex];
                        nav.BadEdgeTargets[(bs.TargetX, bs.TargetY)] = (int)Main.GameUpdateCount + 480;
                        nav.Plan = null; nav.PlanIndex = 0; nav.CommitFrames = 0; nav.ReplanCooldown = 0;
                        nav.StepNoMoveFrames = 0;
                    }
                }
                else { nav.MoveCheckX = npc.Center.X; nav.StepNoMoveFrames = 0; }

                // Fix C v3: ground-navigation hard-stuck catch-all keyed on PERIODIC horizontal progress. A wedged NPC LURCHES a variable
                // distance toward the lip each failed jump then falls back, so any per-frame/instantaneous check is
                // reset by the lurch. Instead, sample net X displacement every ~2s window: <~3 tiles net = no real
                // progress that window. While pursuing + unable to actually engage (no clear shot / out of range) +
                // trying to move (a plan, or braking "blocked"), TWO consecutive dead windows (~4s) = truly wedged
                // (e.g. an overhanging/watery pit the jump can't clear) → blink out (CanTeleport) else disengage. The
                // !canEngage gate keeps a legitimately-firing enemy from being yanked off its spot. An engaged
                // rope ride is excluded: it intentionally has no X progress and already owns vertical stall/end checks.
                bool activeRopeRide = nav.Plan != null && nav.PlanIndex < nav.Plan.Count
                    && nav.Plan[nav.PlanIndex].Kind == StepKind.RopeClimb
                    && (nav.RopeEngaged || npc.noGravity);
                if (pstate == PursuitState.Pursue && !canEngage && !activeRopeRide
                    && (nav.Plan != null || actionLabel == "blocked"))
                {
                    if (nav.HardStuckCheckX == float.MaxValue) nav.HardStuckCheckX = npc.Center.X; // open a fresh window
                    if (++nav.HardStuckFrames >= 120) // ~2s window elapsed
                    {
                        if (Math.Abs(npc.Center.X - nav.HardStuckCheckX) > 48f) nav.HardStuckStrikes = 0; // made real ground
                        else nav.HardStuckStrikes++;                                                    // confined this window
                        nav.HardStuckCheckX = npc.Center.X; // next window measures from here
                        nav.HardStuckFrames = 0;
                        if (nav.HardStuckStrikes >= 2) // ~4s with no net progress
                        {
                            nav.LastPlanResult = $"hard-stuck x={nav.HardStuckCheckX / TileF:F1}";
                            nav.Plan = null; nav.PlanIndex = 0; nav.CommitFrames = 0;
                            bool recoveryInRange = globalNPC.TeleportStyle != TeleportStyle.RecoveryOnly
                                || (globalNPC.TeleportCooldownTimer == 0
                                    && (globalNPC.RecoveryTeleportMaxRange <= 0f
                                        || npc.Distance(player.Center) <= globalNPC.RecoveryTeleportMaxRange));
                            if (globalNPC.CanTeleport && recoveryInRange)
                            {
                                // Use the same robust reacquire search as the navigation FSM: a wider
                                // radius, a legal sightline near the target, and normal charge/cooldown
                                // accounting. The old blind 20-tile queue often found no destination,
                                // reset this detector, and repeated the exact blocked/drop loop forever.
                                // Recovery-only teleports are server-authoritative; clients wait for
                                // the resulting NPC sync instead of independently disengaging.
                                if (globalNPC.TeleportStyle != TeleportStyle.RecoveryOnly
                                    || Main.netMode != NetmodeID.MultiplayerClient)
                                {
                                    if (!tsorcRevampAIs.TryTeleportReacquire(npc, globalNPC))
                                        NavBehavior.ForceDisengage(npc, globalNPC);
                                }
                            }
                            else NavBehavior.ForceDisengage(npc, globalNPC);
                            nav.HardStuckStrikes = 0;
                            nav.HardStuckCheckX = float.MaxValue;
                        }
                    }
                }
                else { nav.HardStuckCheckX = float.MaxValue; nav.HardStuckFrames = 0; nav.HardStuckStrikes = 0; }
            }

            // Phase 2: a large beast bulls straight through a thin (<= BeastPhaseMaxWidth) body/head obstruction
            // (single block, skinny pillar, low overhang) instead of being stopped or forced to jump by it. While
            // phasing it runs with noTileCollide + the feet pinned to the entry floor; real walls (wider than the
            // cap) are NOT phased — A* still routes around/over them. Takes the place of the auto-step this frame.
            bool beastPhasing = globalNPC.RequiresFlatGround && TryBeastPhase(nav, npc, globalNPC);

            // Vanilla-style smooth auto-step: glide over <=1-tile bumps/half-blocks via gfxOffY (no jump),
            // exactly how the player and vanilla walkers move. Runs after the action sets velocity, only
            // while walking on the ground — skipped during a rope ride (noGravity) or a platform-drop
            // (noTileCollide), where stepping up would cancel the intended descent. Replaces 1-tile hops.
            if (!beastPhasing && !npc.noGravity && !npc.noTileCollide)
            {
                UsefulFunctions.AutoStepUp(npc);
            }

            // Phase 3: a beast sinks its sprite to follow the ground under it (drawn on top), so its overhang clips
            // into a slope/hill instead of floating. Cosmetic gfxOffY offset; physics still rests on the core. Plus a
            // light nudge so a near-stationary beast doesn't park its body floating off a cliff edge.
            if (globalNPC.RequiresFlatGround && !beastPhasing)
            {
                ApplyBeastGroundSink(npc, globalNPC);
                ApplyBeastAntiFloat(npc, globalNPC);
            }

            // ArcherAI sets bow/aim frames before SF4 movement runs. If the movement step then faces toward
            // a launch column/rope/path target, the bow frame can flash facing away for one tick. While the
            // archer animation is active, let movement keep its velocity but make the sprite face the player.
            if (movementOnly && globalNPC.ArcherAimDirection != 0f)
            {
                int aimFace = player.Center.X >= npc.Center.X ? 1 : -1;
                npc.direction = aimFace;
                npc.spriteDirection = aimFace;
            }

            // Attacks (reuse the LOS computed above for the FSM).
            // Attacks are allowed on the rope: SF4 forces CanStopToFire=false below, so firing
            // never halts the climb/descent. (Attacking mid-rope is fine per design.)
            bool canAttack = globalNPC.AttackList.Count > 0 && los && npc.Distance(player.Center) <= attackRange;
            // movementOnly: BasicAI's RunFighterCombat already fired this enemy's attacks — SF4 must NOT also fire
            // (double shot). Standalone (Invader/testbed) still fires its own AttackList here.
            if (!movementOnly && globalNPC.AttackList.Count > 0)
            {
                bool oldStop = globalNPC.CanStopToFire;
                globalNPC.CanStopToFire = false;
                tsorcRevampAIs.SimpleProjectile(npc, canAttack);
                globalNPC.CanStopToFire = oldStop;
            }

            nav.LastAction = actionLabel;
            nav.LastReason = reasonLabel;
            nav.LastTelemetryPlan = DescribePlan(nav);
            LogFrame(npc, player, nav, grounded, los, canAttack, actionLabel, reasonLabel);
        }

        public static void OnHit(NPC npc)
        {
            ReleaseRopeTraversal(npc);
            if (States.TryGetValue(npc.whoAmI, out NavState nav))
            {
                // Damage unlocks commitment and forces a fresh replan.
                nav.Plan = null;
                nav.PlanIndex = 0;
                nav.CommitFrames = 0;
                nav.ReplanCooldown = 0;
            }
        }

        // ================================================================================
        //  State management
        // ================================================================================

        private static NavState GetState(NPC npc)
        {
            if (!States.TryGetValue(npc.whoAmI, out NavState nav))
            {
                nav = new NavState();
                States[npc.whoAmI] = nav;
            }
            if (Main.GameUpdateCount % 3600 == 0 && States.Count > 64) Prune();
            return nav;
        }

        private static void Prune()
        {
            List<int> dead = new List<int>();
            foreach (var kv in States) if (!Main.npc[kv.Key].active) dead.Add(kv.Key);
            foreach (int id in dead) States.Remove(id);
        }

        private static void TickTimers(NavState nav)
        {
            if (nav.AirCommitTimer > 0) nav.AirCommitTimer--;
            if (nav.CommitFrames > 0) nav.CommitFrames--;
            if (nav.StepTimer > 0) nav.StepTimer--;
            if (nav.ReplanCooldown > 0) nav.ReplanCooldown--;
            if (nav.PlatformPassTimer > 0) nav.PlatformPassTimer--;
            if (nav.RepositionTimer > 0) nav.RepositionTimer--;
        }

        // Stand-and-fire tuning: brief stand (~1 attack), then a longer reposition window so the
        // NPC spends most of its time pursuing/maneuvering rather than rooted firing.
        private const int HaltMaxFrames = 180;    // ~3s max standing-and-firing, then forced to move
        private const int RepositionFrames = 180; // ~3s moving before it may halt again

        private static void ManagePlatformPass(NavState nav, NPC npc)
        {
            if (!nav.PlatformPassActive) return;
            bool timerDone = nav.PlatformPassTimer <= 0;
            bool clearedAndFalling = npc.velocity.Y > 0.5f && npc.Bottom.Y > nav.PlatformPassStartY + 18f;
            bool landed = npc.collideY && npc.velocity.Y >= 0f;
            if (timerDone || clearedAndFalling || landed)
            {
                npc.noTileCollide = false;
                nav.PlatformPassActive = false;
                nav.PlatformPassTimer = 0;
            }
        }

        // ================================================================================
        //  Replan
        // ================================================================================

        private static bool ShouldReplan(NavState nav, NPC npc, Player player)
        {
            if (nav.Plan == null) return true;
            if (nav.PlanIndex >= nav.Plan.Count) return true;
            // NOTE: do NOT replan here on StepTimer == 0. The replan check runs BEFORE ExecuteStep each
            // frame, so triggering a rebuild on timeout would reset StepTimer to full and skip ExecuteStep's
            // timeout handler — which is what records the failed step's target in BadEdgeTargets. The result
            // was A* re-picking the identical un-bad-edged edge forever (the "jump-into-the-ceiling on repeat"
            // loop). Let the timeout fall through to ExecuteStep: it bad-edges the target and nulls the plan,
            // and the (Plan == null) check above then drives a clean replan that routes AROUND the bad edge.
            // Big drift = player switched rooms / floors â†’ replan.
            float planEndX = nav.Plan[nav.Plan.Count - 1].TargetX * TileF;
            float planEndY = nav.Plan[nav.Plan.Count - 1].TargetY * TileF;
            if (Math.Abs(player.Center.X - planEndX) > 16 * TileF) return true;
            if (Math.Abs(player.Center.Y - planEndY) > 10 * TileF) return true;
            return false;
        }

        // Per-NPC physics constants captured for the duration of a planning pass.
        // The AI runs single-threaded on the main update loop, so static stash is safe and
        // avoids threading three params through BuildEdges -> TryFindJumpEdge.
        private static float _planGravity = 0.3f;
        private static float _planJumpCeil = 9f;
        private static float _planMaxLaunchVx = 6.5f;
        private static int MaxPlanRiseTiles()
        {
            float gravity = _planGravity > 0f ? _planGravity : 0.3f;
            float apexTiles = (_planJumpCeil * _planJumpCeil) / (2f * gravity * TileF);
            return Math.Min(Math.Max((int)Math.Floor(apexTiles - 1f), 1), MaxJumpRiseTiles);
        }
        // Beast flat-ground constraint (set per-frame in Run from globalNPC.MinSurfaceWidth). When > 0, IsStandableTile
        // requires a flat run >= this many tiles wide, CENTERED under the enemy, so A* only builds spans / lands
        // jumps on surfaces a large enemy can actually stand FULLY on (no 1-tile-ledge hopping, no edge-perching).
        // Single-threaded main loop → static stash is safe.
        private static int _minSurfaceWidth = 0;
        // Tolerated footprint unevenness for the beast gate (set per-frame from globalNPC.MaxSurfaceStep). 1 = a foot may dip
        // one tile into a step (the Deerclops-clip failsafe); 0 = strictly flat. Only used when _minSurfaceWidth > 0.
        private static int _maxSurfaceStep = 1;
        // Required vertical headroom in tiles for the body clearance check (HasBodyClearanceAtRow). Default 3 (the
        // old fixed value); for beasts it's auto-derived from sprite height in Run, so a tall beast won't path
        // into a space too short for it.
        private static int _clearanceHeight = 3;
        // May this enemy climb ropes this frame (set in Run from globalNPC.CanUseRopes)? Default off — gates both the
        // RopeClimb edge planner and the no-plan TryGrabNearbyRope fallback.
        private static bool _canUseRopes = false;
        // Does this enemy pass through walls this frame (set in Run from globalNPC.CanPassThroughWalls)? Ghosts walk into
        // walls and teleport through (see GlobalNPC.TryGhostWallTeleport). Default off. When on, the jump planner
        // refuses near-vertical jump launches that would wedge the body against a wall on its trailing side (see
        // LaunchTrailClear in TryFindJumpEdge) — so a ghost pinned in a pit backs off to a clear launch column and
        // arcs up-and-over instead of re-committing to an unmakeable wall-hug jump forever (the pit-wedge loop).
        private static bool _canPassWalls = false;

        private static void Replan(NavState nav, NPC npc, Player player, float topSpeed)
        {
            // Capture this NPC's jump physics so BuildEdges/TryFindJumpEdge only propose
            // edges this enemy can actually clear (gravity-aware, gap-width-aware).
            tsorcRevampGlobalNPC pg = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();
            _planGravity = npc.gravity > 0f ? npc.gravity : 0.3f;
            _planJumpCeil = Math.Max(pg.MaxJumpPower, 5f);
            _planMaxLaunchVx = Math.Max(topSpeed, 0f) + Math.Max(pg.MaxJumpBoost, 2f); // actual topSpeed + boost ceiling

            int npcFeetY = GetFeetTileY(npc);
            int npcCx = (int)(npc.Center.X / TileF);
            int playerFeetY = (int)((player.Bottom.Y - 1f) / TileF);
            int playerCx = (int)(player.Center.X / TileF);

            // Per-NPC search window = the NavSearchRadius lever (tiles). Caller only invokes Replan when
            // NavSearchRadius > 0; clamp to the hard ScanRadius caps so a huge accidental value can't
            // blow up the span build. Smaller radius = dumber AND cheaper.
            int radius = Math.Clamp(pg.NavSearchRadius, 1, ScanRadiusX);
            int yRadius = Math.Min(radius, ScanRadiusY);
            int xMin = npcCx - radius, xMax = npcCx + radius;
            int yMin = Math.Min(npcFeetY, playerFeetY) - yRadius;
            int yMax = Math.Max(npcFeetY, playerFeetY) + yRadius;

            List<Span> spans = BuildSpans(xMin, xMax, yMin, yMax);
            // Prune expired bad-edge entries before building.
            int now = (int)Main.GameUpdateCount;
            if (nav.BadEdgeTargets.Count > 0)
            {
                List<(int, int)> dead = new List<(int, int)>();
                foreach (var kv in nav.BadEdgeTargets) if (kv.Value <= now) dead.Add(kv.Key);
                foreach (var k in dead) nav.BadEdgeTargets.Remove(k);
            }
            BuildEdges(spans, playerFeetY, nav.BadEdgeTargets);

            nav.LastReplanTick = now;
            Span start = FindContainingSpan(spans, npcCx, npcFeetY);
            Span goal = FindContainingSpan(spans, playerCx, playerFeetY);
            if (start == null || goal == null)
            {
                nav.Plan = null; nav.PlanIndex = 0;
                nav.NoAStarPath = true;
                // Which end failed, and the window it searched: a missing START span means the NPC's OWN
                // footing was rejected (the beast flat-ground gate / headroom rule), a missing GOAL span
                // means the player wasn't standing on anything (airborne, roped, flying).
                nav.LastPlanResult = nav.LastReplanResult = start == null
                    ? $"no-start-span spans={spans.Count} me=({npcCx},{npcFeetY}) msw={_minSurfaceWidth} ch={_clearanceHeight}"
                    : $"no-goal-span spans={spans.Count} goal=({playerCx},{playerFeetY})";
                return;
            }

            List<Span> path = AStar(start, goal, playerCx, playerFeetY);
            if (path == null)
            {
                nav.Plan = null; nav.PlanIndex = 0;
                nav.NoAStarPath = true;
                nav.LastPlanResult = nav.LastReplanResult =
                    $"no-path spans={spans.Count} bade={nav.BadEdgeTargets.Count} r={radius}";
                return;
            }

            nav.NoAStarPath = false;
            nav.Plan = ConvertToSteps(path, playerCx, playerFeetY);
            nav.PlanIndex = 0;
            nav.StepTimer = StepTimeoutFrames;
            nav.CommitFrames = 0;
            nav.JumpFiredThisStep = false;
            nav.LastPlanResult = nav.LastReplanResult = $"plan steps={nav.Plan.Count} spans={spans.Count} pathLen={path.Count}";
        }

        // ================================================================================
        //  Span graph
        // ================================================================================

        private static List<Span> BuildSpans(int xMin, int xMax, int yMin, int yMax)
        {
            List<Span> spans = new List<Span>();
            for (int y = yMin; y <= yMax; y++)
            {
                int spanStart = -1;
                for (int x = xMin; x <= xMax; x++)
                {
                    bool ok = IsStandableTile(x, y + 1) && HasBodyClearanceAtRow(x, y);
                    if (ok)
                    {
                        if (spanStart == -1) spanStart = x;
                    }
                    else if (spanStart != -1)
                    {
                        spans.Add(new Span(spanStart, x - 1, y));
                        spanStart = -1;
                    }
                }
                if (spanStart != -1) spans.Add(new Span(spanStart, xMax, y));
            }
            return spans;
        }

        private static int BadEdgePenalty(int landX, int spanY, Dictionary<(int x, int y), int> badEdges)
        {
            // If the destination of this edge corresponds to a recently-failed step
            // target (within 2 tiles), bump the cost so A* prefers alternatives.
            int worst = 0;
            foreach (var kv in badEdges)
            {
                if (Math.Abs(kv.Key.x - landX) <= 2 && Math.Abs(kv.Key.y - spanY) <= 2)
                {
                    if (9999 > worst) worst = 9999;
                }
            }
            return worst;
        }

        private static void BuildEdges(List<Span> spans, int playerY, Dictionary<(int x, int y), int> badEdges)
        {
            Dictionary<int, List<Span>> byY = new Dictionary<int, List<Span>>();
            foreach (var sp in spans)
            {
                if (!byY.TryGetValue(sp.Y, out var bucket)) { bucket = new List<Span>(); byY[sp.Y] = bucket; }
                bucket.Add(sp);
            }

            foreach (var a in spans)
            {
                // ---- WALK / step-hop. Same Y or Â±1 row, touching or 1-gap ----
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (!byY.TryGetValue(a.Y + dy, out var bucket)) continue;
                    foreach (var b in bucket)
                    {
                        if (b == a) continue;
                        bool touching = b.LeftX <= a.RightX + 1 && b.RightX >= a.LeftX - 1;
                        if (!touching) continue;
                        // Walk transitions need body clearance at the JOIN column too.
                        // Column where the two spans meet: b's near edge when b sits clear to one side of
                        // a, otherwise (the spans overlap in X) a's left edge.
                        int joinX;

                        if (b.LeftX > a.RightX)
                        {
                            joinX = b.LeftX;
                        }
                        else if (b.RightX < a.LeftX)
                        {
                            joinX = b.RightX;
                        }
                        else
                        {
                            joinX = a.LeftX;
                        }

                        int joinY = Math.Min(a.Y, b.Y);
                        if (!HasBodyClearanceAtRow(joinX, joinY)) continue;
                        // Stepping UP one row (dy == -1, b is higher) is only a real WALK if there's a
                        // SOLID 1-tile step to climb onto at the join (b's floor sits at row a.Y). When the
                        // higher span rests on a platform or is offset across a lip, AutoStepUp can't take it
                        // and the body just paces in place (the 30s wedge in the log). Skip the Walk edge so
                        // A* uses the JumpUp edge the jump builder already adds for this pair — it actually
                        // gains elevation. (Cheap Walk cost 2 was always out-bidding the JumpUp cost ~7.)
                        if (dy == -1 && !IsNavigationSolid(joinX, a.Y)) continue;
                        a.Edges.Add(new Edge(b, EdgeKind.Walk, 1 + Math.Abs(dy)));
                    }
                }

                // ---- Calibrated JUMP edges ----
                // For each candidate target span, try multiple launch/land column pairs
                // and pick the first one with a valid trajectory. Edge proposed only if
                // (|dx|, dy) is in the arc table AND the trajectory is physically clear.
                foreach (var b in spans)
                {
                    if (b == a) continue;
                    int dy = a.Y - b.Y; // positive = b is above
                    if (dy > MaxPlanRiseTiles() || dy < -8) continue; // outside this NPC's jump reach
                    if (TryFindJumpEdge(a, b, dy, out int launchX, out int landX, out int absDx))
                    {
                        EdgeKind kind = dy >= 1 ? EdgeKind.JumpUp : EdgeKind.JumpGap;
                        int wrongDirPenalty = 0;
                        if (playerY < a.Y && b.Y > a.Y) wrongDirPenalty = 60;
                        // Tight-squeeze penalty: a 1-2 tile landing span is risky/often
                        // unreachable. Prefer wider landings (3+ tiles).
                        int landingWidth = b.RightX - b.LeftX + 1;
                        int tightPenalty = landingWidth <= 2 ? 15 : 0;
                        int badPenalty = BadEdgePenalty(landX, b.Y, badEdges);
                        int baseCost = kind == EdgeKind.JumpUp ? 5 + dy * 2 : 5 + absDx;
                        a.Edges.Add(new Edge(b, kind, baseCost + wrongDirPenalty + tightPenalty + badPenalty, launchX, landX));
                    }
                }

                // ---- DROP edges (free-fall through holes) ----
                for (int dy = 2; dy <= MaxDropDepth; dy++)
                {
                    if (!byY.TryGetValue(a.Y + dy, out var bucket)) continue;
                    foreach (var b in bucket)
                    {
                        if (b == a) continue;
                        int candidate = -1;
                        for (int x = Math.Max(a.LeftX - 1, b.LeftX); x <= Math.Min(a.RightX + 1, b.RightX); x++)
                        {
                            if (HasDropClearance(x, a.Y, b.Y)) { candidate = x; break; }
                        }
                        if (candidate == -1) continue;
                        int dropPenalty = playerY < a.Y - 3 ? 80 : 0;
                        int badP = BadEdgePenalty(candidate, b.Y, badEdges);
                        a.Edges.Add(new Edge(b, EdgeKind.Drop, 3 + dy + dropPenalty + badP, candidate, candidate));
                    }
                }

                // ---- PLATFORM DROP (per-column: any column in span A that's a platform tile
                //      can be a drop point, not just spans that are platform-only). ----
                for (int dy = 2; dy <= MaxDropDepth; dy++)
                {
                    if (!byY.TryGetValue(a.Y + dy, out var bucket)) continue;
                    foreach (var b in bucket)
                    {
                        if (b == a) continue;
                        // Find a column inside both spans where the launch tile is a platform.
                        int candidate = -1;
                        int xMin = Math.Max(a.LeftX, b.LeftX), xMax = Math.Min(a.RightX, b.RightX);
                        for (int x = xMin; x <= xMax; x++)
                        {
                            if (IsPlatformTile(x, a.Y + 1) && HasDropClearance(x, a.Y + 2, b.Y))
                            { candidate = x; break; }
                        }
                        if (candidate == -1) continue;
                        int dropPenalty = playerY < a.Y - 3 ? 80 : 0;
                        int badP = BadEdgePenalty(candidate, b.Y, badEdges);
                        a.Edges.Add(new Edge(b, EdgeKind.PlatformDrop, 4 + dy + dropPenalty + badP, candidate, candidate));
                    }
                }

                // ---- ROPE-CLIMB edges ----
                // Model what a PLAYER can actually do with a rope, so the NPC only commits to ropes it can truly
                // use to get off toward the player. A rope is only a valid edge to span bb if bb is reachable by
                // one of the two real dismount options:
                //   (1) SIDE EXIT — the rope runs up PAST a ledge/platform; ride to that level and step off
                //       left/right onto it. Works at ANY height the rope passes a standable ledge.
                //   (2) TOP JUMP-UP — at the rope's top there's a jump-through platform above with room to land
                //       on top; ride to the top and jump straight up onto it. Requires a CLEAR (no solid)
                //       vertical path — you can never jump up through a solid block, so that's never valid.
                // Multiple valid exits along one rope => A* naturally picks the one on the cheapest path to the
                // player's span (i.e. the floor the player is nearest).
                if (!_canUseRopes) continue; // ropes are a per-enemy opt-in (CanUseRopes); skip building rope edges
                for (int x = a.LeftX - 1; x <= a.RightX + 1; x++)
                {
                    if (!FindRopeSpan(x, a.Y, 5, out int ropeBottomY, out int ropeTopY)) continue;
                    if (ropeTopY >= a.Y) continue; // rope must extend above this span to climb
                    foreach (var bb in spans)
                    {
                        if (bb == a) continue;
                        int dyClimb = a.Y - bb.Y;
                        if (dyClimb < 2 || dyClimb > MaxRopeClimb) continue;
                        if (x < bb.LeftX - 1 || x > bb.RightX + 1) continue; // rope must sit under/adjacent the landing span

                        bool valid = false;
                        int extra = 0;
                        int landX = x;

                        if (bb.Y >= ropeTopY && bb.Y <= ropeBottomY && RopeSideExitClear(x, bb))
                        {
                            // Case 1: ride up to bb's level, step off sideways onto the ledge.
                            valid = true;
                            landX = Math.Clamp(x, bb.LeftX, bb.RightX);
                        }
                        else if (bb.Y < ropeTopY)
                        {
                            // Case 2: ride to the rope top, jump straight up onto the platform above it.
                            int jumpRise = ropeTopY - bb.Y;
                            if (jumpRise >= 1 && RopeTopJumpClear(x, ropeTopY, bb)
                                && ComputeJumpArc(0, jumpRise, _planGravity, _planJumpCeil, _planMaxLaunchVx, out _, out _))
                            {
                                valid = true;
                                extra = 2 + jumpRise; // top jumps cost a bit more than a plain side step
                                landX = Math.Clamp(x, bb.LeftX, bb.RightX);
                            }
                        }

                        if (!valid) continue;
                        int badP = BadEdgePenalty(x, bb.Y, badEdges);
                        // No break: register an edge to EVERY valid exit on this rope (each floor / the top jump).
                        // A* then picks the exit on the cheapest path to the player — i.e. the floor nearest them.
                        a.Edges.Add(new Edge(bb, EdgeKind.RopeClimb, 3 + dyClimb / 2 + extra + badP, x, landX));
                    }
                }
            }
        }

        // Max climbable rope length (tiles). Generous so very tall ropes are fully traversable.
        private const int MaxRopeClimb = 300; // very tall ropes are detected and climbable in one traversal
        private const float RopeClimbSpeed = 3.2f; // px/frame vertical ride speed
        private const int RopePursuitGap = 2; // minimum body spacing while pursuing a player on the same rope

        // A rope column has rope tiles for at least 3 contiguous rows above feetY.
        private static bool HasRopeColumn(int x, int feetY, out int topY)
        {
            topY = feetY;
            int run = 0;
            for (int y = feetY - 1; y >= feetY - MaxRopeClimb; y--)
            {
                if (IsRopeTile(x, y)) { run++; topY = y; }
                else break;
            }
            return run >= 3;
        }

        // Matches every climbable rope/chain variant — not just plain Rope. The old check
        // only matched TileID.Rope, so silk/web/vine ropes and chains were invisible to nav.
        private static bool IsRopeTile(int x, int y)
        {
            if (!WorldGen.InWorld(x, y)) return false;
            Tile t = Main.tile[x, y];
            if (!t.HasTile || t.IsActuated) return false;
            int ty = t.TileType;
            return ty == TileID.Rope || ty == TileID.SilkRope || ty == TileID.WebRope
                || ty == TileID.VineRope || ty == TileID.Chain;
        }

        // Finds a rope column at x whose vertical extent comes within `reach` tiles of feetY
        // (so the NPC can reach it by walking under and floating up, or stepping down onto it).
        // Returns the rope's bottom (largest Y) and top (smallest Y) tile. Robust to ropes whose
        // bottom hangs a few tiles above the NPC's feet — the old HasRopeColumn required a rope
        // tile at exactly feetY-1, which missed every hanging rope.
        private static bool FindRopeSpan(int x, int feetY, int reach, out int bottomY, out int topY)
        {
            bottomY = topY = 0;
            int seed = int.MinValue;
            for (int y = feetY - reach; y <= feetY + reach; y++)
            {
                if (IsRopeTile(x, y)) { seed = y; break; }
            }
            if (seed == int.MinValue) return false;
            int b = seed, t = seed;
            for (int y = seed + 1; y <= seed + MaxRopeClimb; y++) { if (IsRopeTile(x, y)) b = y; else break; }
            for (int y = seed - 1; y >= seed - MaxRopeClimb; y--) { if (IsRopeTile(x, y)) t = y; else break; }
            if (b - t < 2) return false; // need at least 3 contiguous rope tiles
            bottomY = b; topY = t;
            return true;
        }

        // Case 1 validation — can step sideways off a rope (column ropeCol) onto span bb at bb's level. The tile
        // beside the rope toward bb must be standable (floor below + body clearance) so the NPC walks straight off
        // onto the ledge. Returns false if bb is directly above the rope (that's a jump-up, not a side step).
        private static bool RopeSideExitClear(int ropeCol, Span bb)
        {
            int sideX = Math.Clamp(ropeCol, bb.LeftX, bb.RightX);
            if (sideX == ropeCol) return false; // bb sits over the rope column — not a sideways exit
            return IsStandableTile(sideX, bb.Y + 1) && HasBodyClearanceAtRow(sideX, bb.Y);
        }

        // Case 2 validation — from the rope top, can jump straight up onto span bb above it. The vertical path
        // must be free of SOLID tiles (jump-through platforms are fine — you can pass up through them), and there
        // must be room to land/stand on bb: a 3-wide x 2-tall clear footprint over a standable floor at the rope
        // column. A solid block anywhere in the path makes this impossible (you can't jump through solid).
        private static bool RopeTopJumpClear(int ropeCol, int ropeTopY, Span bb)
        {
            for (int y = ropeTopY - 1; y >= bb.Y; y--)
            {
                if (IsNavigationSolid(ropeCol, y)) return false; // solid in the way — can't jump through it
            }
            if (!IsStandableTile(ropeCol, bb.Y + 1)) return false; // must actually land on the platform above the rope
            for (int dx = -1; dx <= 1; dx++)
            {
                if (IsNavigationSolid(ropeCol + dx, bb.Y) || IsNavigationSolid(ropeCol + dx, bb.Y - 1)) return false;
            }
            return true;
        }

        // Computes the X position to snap the body to while on a rope. Normally the rope center,
        // but if a solid block is beside the rope column, offset toward the open side so the wider
        // body fits within the rope tile rather than jamming into the block.
        private static float RopeSnapX(NPC npc, int ropeCol, float ropeCenter, int feetY)
        {
            float snapX = ropeCenter;
            int bodyRow = feetY - 1; // mid-body sample
            bool solidRight = IsNavigationSolid(ropeCol + 1, bodyRow) && !IsRopeTile(ropeCol + 1, bodyRow);
            bool solidLeft = IsNavigationSolid(ropeCol - 1, bodyRow) && !IsRopeTile(ropeCol - 1, bodyRow);
            float overhang = (npc.width - TileF) / 2f + 1f; // how far the body overhangs the rope tile
            if (overhang > 0f)
            {
                if (solidRight && !solidLeft) snapX -= overhang;
                else if (solidLeft && !solidRight) snapX += overhang;
            }
            return snapX - npc.width / 2f;
        }

        // Fallback used when there is no plan and the player is on a different level: find the
        // nearest grabbable rope column within ±8 tiles and synthesize a one-step RopeClimb plan.
        // Handles BOTH directions — climb up to a player above, descend to a player below.
        private static bool TryGrabNearbyRope(NavState nav, NPC npc, Player player)
        {
            if (!_canUseRopes) return false;  // ropes are a per-enemy opt-in (CanUseRopes)
            if (nav.Plan != null) return false; // only as a no-plan fallback
            int now = (int)Main.GameUpdateCount;
            int npcCx = (int)(npc.Center.X / TileF);
            int npcFeetY = GetFeetTileY(npc);
            int playerFeetY = (int)((player.Bottom.Y - 1f) / TileF);
            int vdelta = playerFeetY - npcFeetY; // < 0 player above, > 0 player below
            if (Math.Abs(vdelta) < 3) { nav.RopeFallbackFails = 0; nav.LastPlanResult = "rope-skip same-level"; return false; }
            bool goUp = vdelta < 0;

            int bestX = int.MinValue, bestTargetY = 0, bestDist = int.MaxValue;
            for (int dx = -8; dx <= 8; dx++)
            {
                int x = npcCx + dx;
                if (!FindRopeSpan(x, npcFeetY, 5, out int rb, out int rt)) continue;
                // Rope must extend in the direction of the player.
                if (goUp && rt >= npcFeetY - 1) continue;   // no rope above us
                if (!goUp && rb <= npcFeetY + 1) continue;  // no rope below us
                int tY = goUp ? Math.Max(rt, playerFeetY)   // up: toward player, capped at top
                              : Math.Min(rb, playerFeetY);  // down: toward player, capped at bottom
                // Skip ropes recently marked dead — they couldn't actually deliver us to the player
                // (e.g. rope ends below a solid roof). Prevents re-picking the same on/off-loop rope.
                if (IsRopeFallbackBadEdged(nav, x, tY, now)) continue;
                int dist = Math.Abs(dx);
                if (dist < bestDist) { bestDist = dist; bestX = x; bestTargetY = tY; }
            }
            if (bestX == int.MinValue) { nav.LastPlanResult = $"rope-none up={goUp} d={vdelta}"; return false; }

            // Retry memory: re-synthesizing the SAME rope within ~3s means the previous climb didn't
            // get us to the player (we're back at the base asking again — the on/off loop). After two
            // such retries, bad-edge this rope so we stop looping and fall through to stand-and-fire.
            if (bestX == nav.RopeFallbackX && now - nav.RopeFallbackTick < 180)
            {
                nav.RopeFallbackFails++;
                if (nav.RopeFallbackFails >= 2)
                {
                    nav.BadEdgeTargets[(bestX, bestTargetY)] = now + 600; // ~10s before we'll try it again
                    nav.RopeFallbackFails = 0;
                    nav.LastPlanResult = $"rope-deadend-baded x={bestX} toY={bestTargetY}";
                    return false;
                }
            }
            else nav.RopeFallbackFails = 0;
            nav.RopeFallbackX = bestX;
            nav.RopeFallbackTick = now;

            // This is a pursuit waypoint, not necessarily a legal dismount. ExecRopeClimb validates that
            // the adjacent tile is a real landing before allowing reachedTarget to release the rope.
            int targetX = bestX + (player.Center.X >= bestX * TileF + 8f ? 1 : -1);
            nav.Plan = new List<PlanStep> { new PlanStep(StepKind.RopeClimb, targetX, bestTargetY, bestX) };
            nav.PlanIndex = 0;
            nav.StepTimer = StepTimeoutFrames;
            nav.CommitFrames = 0;
            nav.RopeEngaged = false; nav.RopeDirLatched = false; nav.RopeDismounting = false;
            nav.LastPlanResult = $"rope-fallback {(goUp ? "up" : "down")} toY={bestTargetY} x={bestX}";
            return true;
        }

        // True if a rope column/target was recently bad-edged (within ±1 tile X, ±2 tile Y, not expired).
        private static bool IsRopeFallbackBadEdged(NavState nav, int x, int y, int now)
        {
            foreach (var kv in nav.BadEdgeTargets)
                if (kv.Value > now && Math.Abs(kv.Key.x - x) <= 1 && Math.Abs(kv.Key.y - y) <= 2) return true;
            return false;
        }

        // Ranged kiting: maintain a preferred DISTANCE BAND from a SAME-LEVEL, visible player (KiteRangeMin/Max),
        // instead of pathing into melee. Three zones: too close → back off (face the player, backpedal; hold if a
        // wall/cliff is behind it); in the band → slow forward/back drift + fire; farther than Max → return false
        // so normal pursuit closes the gap. Looseness: a drifting target distance + an occasional "let them close" window are re-rolled
        // every ~1.5-4s so spacing is non-robotic and melee can sometimes rush in. Returns true if it took control
        // this frame (which suppresses the pursuit plan). Firing itself is the attack block's job; this only positions.
        private static bool TryRangedKite(NavState nav, NPC npc, Player player, tsorcRevampGlobalNPC globalNPC,
            float topSpeed, float acceleration, bool los, out string action, out string reason)
        {
            action = ""; reason = "";
            // KiteRangeMax is the explicit opt-in. Bespoke timeline enemies such as Red Knight have a real ranged
            // pool even though AttackList is intentionally empty.
            if (globalNPC.KiteRangeMax <= 0f) return false;

            // Distant projectile pressure temporarily disables the preferred kite band. Returning false hands
            // movement back to ordinary pursuit/pathfinding, whose effective target distance is zero. Existing
            // distance-aware attacks can claim the body naturally when their melee or ranged band becomes valid.
            if (globalNPC.IsClosingOnRangedThreat(npc, player))
            {
                nav.KiteTargetDist = 0f;
                nav.KiteRerollTimer = 0;
                nav.KiteLetClose = false;
                return false;
            }

            if (!los) return false;
            // Horizontal kiting only — a player on a different level is the standoff's / pursuit's job.
            if (Math.Abs(player.Center.Y - npc.Center.Y) > 48f) return false;

            float distTiles = npc.Distance(player.Center) / TileF;
            if (distTiles > globalNPC.KiteRangeMax) return false; // too far → let pursuit close in (don't take control)

            int faceP = player.Center.X >= npc.Center.X ? 1 : -1;
            npc.direction = faceP; npc.spriteDirection = faceP; // always face the player to shoot

            // Re-roll the drifting target distance + the looseness window occasionally → non-robotic spacing.
            if (nav.KiteRerollTimer <= 0)
            {
                nav.KiteTargetDist = MathHelper.Lerp(globalNPC.KiteRangeMin, globalNPC.KiteRangeMax, Main.rand.NextFloat());
                nav.KiteLetClose = Main.rand.NextFloat() < globalNPC.KiteLooseness;
                nav.KiteHoldDrift = Main.rand.NextBool() ? 1 : -1; // +1 = forward toward player, -1 = backward from player
                nav.KiteRerollTimer = Main.rand.Next(90, 240); // ~1.5-4s
            }
            else nav.KiteRerollTimer--;

            const float deadband = 1.5f; // hysteresis so it doesn't jitter at the target ring
            if (distTiles < nav.KiteTargetDist - deadband && !nav.KiteLetClose)
            {
                // Too close → back off, but don't reverse into a wall/cliff (hold + fire if cornered).
                int away = -faceP;
                int fX = GetFrontTileX(npc, away);
                if (IsCliffAhead(npc, away) || GetObstacleHeight(fX, GetFeetTileY(npc)) > 1)
                {
                    if (Math.Abs(npc.velocity.X) < 0.3f) npc.velocity.X = 0f; else npc.velocity.X *= 0.6f;
                    action = "kite-hold"; reason = $"d={distTiles:F0} cornered";
                }
                else
                {
                    ApplyChase(npc, away, topSpeed, acceleration); // ApplyChase toward `away` = backpedal
                    npc.spriteDirection = faceP;                   // keep facing the player while moving back
                    action = "kite-back"; reason = $"d={distTiles:F0}<t{nav.KiteTargetDist:F0}";
                }
                return true;
            }

            // In the band (or a let-close window) → creep forward/back slowly and fire.
            const float holdDriftSpeedMult = 0.35f;
            if (nav.KiteHoldDrift == 0) nav.KiteHoldDrift = -1;
            int drift = nav.KiteHoldDrift;
            int driftDir = drift * faceP;
            int driftFrontX = GetFrontTileX(npc, driftDir);
            if (IsCliffAhead(npc, driftDir) || GetObstacleHeight(driftFrontX, GetFeetTileY(npc)) > 1)
            {
                int otherDir = -driftDir;
                int otherFrontX = GetFrontTileX(npc, otherDir);
                if (!IsCliffAhead(npc, otherDir) && GetObstacleHeight(otherFrontX, GetFeetTileY(npc)) <= 1)
                {
                    nav.KiteHoldDrift *= -1;
                    driftDir = otherDir;
                    drift *= -1;
                }
            }

            driftFrontX = GetFrontTileX(npc, driftDir);
            if (IsCliffAhead(npc, driftDir) || GetObstacleHeight(driftFrontX, GetFeetTileY(npc)) > 1)
            {
                if (Math.Abs(npc.velocity.X) < 0.3f) npc.velocity.X = 0f; else npc.velocity.X *= 0.6f;
                action = "kite-hold"; reason = nav.KiteLetClose ? $"d={distTiles:F0} let-close blocked" : $"d={distTiles:F0} band blocked";
            }
            else
            {
                ApplyChase(npc, driftDir, topSpeed * holdDriftSpeedMult, acceleration);
                npc.spriteDirection = faceP;
                action = drift > 0 ? "kite-drift-forward" : "kite-drift-back";
                reason = nav.KiteLetClose ? $"d={distTiles:F0} let-close" : $"d={distTiles:F0} band";
            }
            return true;
        }

        // ── Large-beast positioner (Phase 1) ────────────────────────────────────────────────────────────────────
        // Two pieces, both gated on globalNPC.RequiresFlatGround by the caller:
        //   TryBeastContactBackoff — runs as the "kiting" pre-empt; bobs the beast back out of melee for a window
        //                            after each touch so it doesn't grind centered on the player (sprite glitch /
        //                            "trapped on top of you"). Returns false when there's no active back-off so A*
        //                            keeps navigating the beast in.
        //   BeastOscillate         — the no-plan fallback; the beast can't path to the player, so instead of the
        //                            standoff/halt/chase branches it ALWAYS moves, oscillating in/out around its
        //                            large preferred band (KiteRangeMin/Max). Increments BeastUnreachableFrames,
        //                            which the BasicAI overlay uses to wander off if also un-hit for ~10s.
        // Retreat/back-off speed scales with how recently it was hit (faster away while being beaten on). Firing is
        // unchanged — it stays BasicAI's job; this only positions.
        private const int BeastHitWindow = 180; // ~3s ramp for the hit-recency retreat-speed scaling

        private static float BeastRetreatSpeed(tsorcRevampGlobalNPC globalNPC)
        {
            float hitRecency = MathHelper.Clamp(1f - globalNPC.FramesSinceHit / (float)BeastHitWindow, 0f, 1f);
            return MathHelper.Lerp(globalNPC.BeastRetreatSpeedCalm, globalNPC.BeastRetreatSpeedHit, hitRecency);
        }

        // Won't step a beast off a cliff or grind it into a wall: prefer `desired`, else the opposite if it's open,
        // else press toward the player (so a boxed beast still inches forward rather than freezing).
        private static bool IsBeastStepBlocked(NPC npc, int dir)
            => IsCliffAhead(npc, dir) || GetObstacleHeight(GetFrontTileX(npc, dir), GetFeetTileY(npc)) > 1;
        private static int BeastStepDir(NPC npc, int desired, int faceP)
        {
            if (desired == 0) desired = faceP;
            if (!IsBeastStepBlocked(npc, desired)) return desired;
            if (!IsBeastStepBlocked(npc, -desired)) return -desired;
            return faceP;
        }

        private static bool TryBeastContactBackoff(NavState nav, NPC npc, Player player, tsorcRevampGlobalNPC globalNPC,
            float acceleration, out string action, out string reason)
        {
            action = ""; reason = "";
            int faceP = player.Center.X >= npc.Center.X ? 1 : -1;
            bool touching = npc.Hitbox.Intersects(player.Hitbox)
                || npc.Distance(player.Center) <= (npc.width + player.width) * 0.5f + 8f;
            if (touching) nav.BeastContactBackoff = globalNPC.BeastContactBackoffTicks;
            if (nav.BeastContactBackoff <= 0) return false;

            nav.BeastContactBackoff--;
            globalNPC.BeastUnreachableFrames = 0; // we just reached them — not stale
            int away = BeastStepDir(npc, -faceP, faceP);
            ApplyChase(npc, away, BeastRetreatSpeed(globalNPC), acceleration);
            npc.direction = faceP; npc.spriteDirection = faceP; // keep facing the player while bobbing out
            action = "beast-backoff"; reason = $"t={nav.BeastContactBackoff}";
            return true;
        }

        private static void BeastOscillate(NavState nav, NPC npc, Player player, tsorcRevampGlobalNPC globalNPC,
            float topSpeed, float acceleration, out string action, out string reason)
        {
            globalNPC.BeastUnreachableFrames++;
            int faceP = player.Center.X >= npc.Center.X ? 1 : -1;
            npc.direction = faceP; npc.spriteDirection = faceP;
            float distTiles = npc.Distance(player.Center) / TileF;
            bool closeOnRangedThreat = globalNPC.IsClosingOnRangedThreat(npc, player);

            // Re-roll the band target occasionally so it can't reverse course every frame (hysteresis).
            if (closeOnRangedThreat)
            {
                nav.KiteTargetDist = 0f;
                nav.KiteRerollTimer = 0;
            }
            else if (nav.KiteRerollTimer <= 0)
            {
                float lo = globalNPC.KiteRangeMin > 0 ? globalNPC.KiteRangeMin : 6f;
                float hi = globalNPC.KiteRangeMax > 0 ? globalNPC.KiteRangeMax : 20f;
                nav.KiteTargetDist = MathHelper.Lerp(lo, hi, Main.rand.NextFloat());
                nav.KiteHoldDrift = Main.rand.NextBool() ? 1 : -1;
                nav.KiteRerollTimer = Main.rand.Next(90, 240); // ~1.5-4s committed
            }
            else nav.KiteRerollTimer--;

            const float deadband = 2f;
            bool approachBlocked = IsBeastStepBlocked(npc, faceP);
            int desired; float speed;
            if (closeOnRangedThreat)
            {
                desired = faceP; speed = topSpeed;                                // pressure posture: approach-only
            }
            else if (!approachBlocked && distTiles > nav.KiteTargetDist + deadband)
            {
                desired = faceP; speed = topSpeed;                              // too far → close as far as terrain allows
            }
            else if (distTiles < nav.KiteTargetDist - deadband)
            {
                desired = -faceP; speed = BeastRetreatSpeed(globalNPC);                 // too close → back off (hit-scaled)
            }
            else
            {
                desired = nav.KiteHoldDrift * faceP;                              // in band (or approach blocked) → slow drift in/out
                speed = Math.Max(topSpeed * 0.45f, globalNPC.BeastRetreatSpeedCalm);
            }

            int moveDir = BeastStepDir(npc, desired, faceP);
            ApplyChase(npc, moveDir, speed, acceleration);
            npc.spriteDirection = faceP;
            action = closeOnRangedThreat ? "beast-ranged-pursuit" : "beast-oscillate";
            reason = $"d={distTiles:F0} t={nav.KiteTargetDist:F0}";
        }

        // ── Phase 2: bull through thin (<= BeastPhaseMaxWidth) body/head obstructions ─────────────────────────────
        // A giant shouldn't be stopped or forced to jump by a single block / skinny pillar / low overhang. While
        // phasing it runs noTileCollide and pins its feet to the entry floor (so gravity doesn't drop it through the
        // obstruction's base); it still needs valid floor on the far side. Real walls (wider than the cap) are NOT
        // phased — A* routes around/over them. Returns true on any frame it is phasing (the caller skips auto-step).
        private static bool TryBeastPhase(NavState nav, NPC npc, tsorcRevampGlobalNPC globalNPC)
        {
            if (nav.BeastPhasing)
            {
                // Direction latched when the phase began. It should never be 0 here; fall back to current
                // travel so a stale 0 can't stall the body inside the wall.
                int dir = nav.BeastPhaseDir;

                if (dir == 0)
                {
                    dir = npc.velocity.X >= 0f ? 1 : -1;
                }

                npc.position.Y = nav.BeastPhaseFloorY - npc.height; // hold the entry floor level
                npc.velocity.Y = 0f;
                // Commit to the latched direction so a mid-phase plan/velocity flip can't strand it inside the wall.
                if (Math.Abs(npc.velocity.X) < 1f || Math.Sign(npc.velocity.X) != dir) npc.velocity.X = dir * 1.5f;
                npc.noTileCollide = true;

                // Exit once the body is fully clear (out the far side) and nothing is blocking the next column —
                // or the safety cap trips (so a bad detection can never strand it phasing).
                bool clear = !BeastBodyInsideSolid(npc) && BeastClearColumnDistance(npc, dir, 1) <= 0;
                if (clear || ++nav.BeastPhaseFrames > 90)
                {
                    nav.BeastPhasing = false;
                    nav.BeastPhaseFrames = 0;
                    npc.noTileCollide = false;
                    return false;
                }
                return true;
            }

            // Consider STARTING a phase: grounded and walking into something.
            if (npc.velocity.Y != 0f) return false;
            // Which way we're walking. 0.1f deadzone: under that, friction jitter flips the sign every
            // frame. 0 means "not really moving", which is not a phase attempt.
            int moveDirection = 0;

            if (npc.velocity.X > 0.1f)
            {
                moveDirection = 1;
            }
            else if (npc.velocity.X < -0.1f)
            {
                moveDirection = -1;
            }

            if (moveDirection == 0)
            {
                return false;
            }

            int dist = BeastClearColumnDistance(npc, moveDirection, globalNPC.BeastPhaseMaxWidth + 1);
            if (dist <= 0) return false;                 // body isn't actually blocked ahead
            if (dist > globalNPC.BeastPhaseMaxWidth) return false; // a real wall → let A* handle it

            // It still needs ground on the far side (it phases the obstruction, not into a pit).
            int feetY = GetFeetTileY(npc);
            int startX = moveDirection > 0 ? (int)((npc.Right.X + 1f) / TileF) : (int)((npc.Left.X - 1f) / TileF);
            int farX = startX + moveDirection * dist;
            if (!IsNavigationSolid(farX, feetY + 1) && !IsNavigationSolid(farX, feetY + 2)) return false;

            nav.BeastPhasing = true;
            nav.BeastPhaseFrames = 0;
            nav.BeastPhaseDir = moveDirection;
            nav.BeastPhaseFloorY = npc.Bottom.Y;
            npc.noTileCollide = true;
            npc.velocity.Y = 0f;
            return true;
        }

        // Distance (in tiles) to the first column ahead in `dir` whose body rows are CLEAR of solids. 0 = the column
        // just ahead is already clear (not blocked); N = the obstruction is N tiles wide; -1 = solid the whole way
        // to `cap` (a real wall). Body rows only — the feet/floor are handled separately.
        private static int BeastClearColumnDistance(NPC npc, int dir, int cap)
        {
            int startX = dir > 0 ? (int)((npc.Right.X + 1f) / TileF) : (int)((npc.Left.X - 1f) / TileF);
            int topY = (int)(npc.Top.Y / TileF);
            int feetY = GetFeetTileY(npc);
            for (int i = 0; i < cap; i++)
            {
                int x = startX + dir * i;
                bool blocked = false;
                for (int y = topY; y < feetY; y++)
                    if (IsNavigationSolid(x, y)) { blocked = true; break; }
                if (!blocked) return i;
            }
            return -1;
        }

        // True if any solid tile currently overlaps the beast's body box (rows above the feet) — i.e. it is still
        // inside the obstruction it is phasing through.
        private static bool BeastBodyInsideSolid(NPC npc)
        {
            int leftX = (int)(npc.Left.X / TileF);
            int rightX = (int)(npc.Right.X / TileF);
            int topY = (int)(npc.Top.Y / TileF);
            int feetY = GetFeetTileY(npc);
            for (int x = leftX; x <= rightX; x++)
                for (int y = topY; y < feetY; y++)
                    if (IsNavigationSolid(x, y)) return true;
            return false;
        }

        // ── Phase 3: ground-clip sink draw ───────────────────────────────────────────────────────────────────────
        // Lower the sprite (gfxOffY, drawn on top) so it follows the ground under its FULL width, capped at
        // BeastSinkMaxTiles. On a slope/hill the overhang then clips DOWN into the ground instead of floating in air;
        // the physics body still rests on the support core (MinSurfaceWidth), so combat/footing are unchanged. Only
        // raises gfxOffY (never lowers it) so the vanilla auto-step glide isn't fought on flat ground.
        private static void ApplyBeastGroundSink(NPC npc, tsorcRevampGlobalNPC globalNPC)
        {
            if (npc.velocity.Y != 0f) return; // only while grounded
            int restRow = GetFeetTileY(npc) + 1; // the floor row the support core actually stands on
            int leftX = (int)(npc.Left.X / TileF);
            int rightX = (int)(npc.Right.X / TileF);
            int sinkCap = Math.Max(0, globalNPC.BeastSinkMaxTiles);
            if (sinkCap == 0) return;

            // Deepest ground under the footprint, measured in tiles BELOW the resting floor (capped). On flat
            // ground every column is solid at restRow → drop 0 → no sink. Where the ground steps down on a slope,
            // the sprite sinks toward the lowest foot so the higher body clips into the terrain instead of floating.
            // Open-air cliff drops (columns where restRow and restRow-1 are both air) do NOT pull the sink down.
            int deepest = 0;
            for (int x = leftX; x <= rightX; x++)
            {
                // Skip open-air cliff overhangs (no terrain at or above feet level)
                if (!IsNavigationSolid(x, restRow) && !IsNavigationSolid(x, restRow - 1)) continue;

                for (int d = 0; d <= sinkCap; d++)
                {
                    if (IsNavigationSolid(x, restRow + d)) { if (d > deepest) deepest = d; break; }
                }
            }
            if (deepest <= 0) return;

            float sinkPixels = deepest * TileF;
            if (sinkPixels > npc.gfxOffY) npc.gfxOffY = sinkPixels; // sink down to follow the ground; never reduce a step-up glide
        }

        // Light anti-float: if a (near-stationary) beast's body overhangs AIR on one side while the other side has
        // ground close below, nudge it toward the grounded side so its body ends up over terrain (where the sink can
        // clip it in) instead of floating off a cliff edge. Only acts when nearly stopped, so it never fights active
        // pursuit/oscillation — and never steps off the opposite edge. The deeper "refuse to perch over air" nav fix
        // is intentionally NOT done here (it risks breaking drop-pursuit); this is the cheap, safe mitigation.
        private static void ApplyBeastAntiFloat(NPC npc, tsorcRevampGlobalNPC globalNPC)
        {
            if (npc.velocity.Y != 0f || Math.Abs(npc.velocity.X) > 0.6f) return;
            int sinkCap = Math.Max(1, globalNPC.BeastSinkMaxTiles);
            bool leftAir = BeastSideOverhangsAir(npc, -1, sinkCap);
            bool rightAir = BeastSideOverhangsAir(npc, 1, sinkCap);
            if (leftAir == rightAir) return; // both float (narrow perch) or both grounded → no clearly better side
            int toGround = leftAir ? 1 : -1;
            if (!IsBeastStepBlocked(npc, toGround))
                ApplyChase(npc, toGround, Math.Max(0.6f, globalNPC.BeastRetreatSpeedCalm), 0.1f);
        }

        // True if the outer two columns on `side` of the sprite have no ground within ~sinkCap tiles below the feet
        // — i.e. that edge of the body hangs over air (a cliff) rather than clipping into a slope.
        private static bool BeastSideOverhangsAir(NPC npc, int side, int sinkCap)
        {
            int feetY = GetFeetTileY(npc);
            int edgeX = side > 0 ? (int)(npc.Right.X / TileF) : (int)(npc.Left.X / TileF);
            for (int i = 0; i < 2; i++)
            {
                int x = edgeX - side * i; // outermost column, then one inward
                for (int d = 1; d <= sinkCap + 1; d++)
                    if (IsNavigationSolid(x, feetY + d)) return false; // ground close below this column → not floating
            }
            return true;
        }

        // Different-level ranged pursuit: seek a true firing ray when LOS is blocked; once it opens,
        // continue toward the player unless navigation says advancing is unsafe or would lose that ray.
        private static bool HasStrictLineOfSight(Vector2 source, Vector2 target)
            => Collision.CanHitLine(source, 1, 1, target, 1, 1);

        // Standing fire is a navigation safety response, not the ordinary ranged posture. Clear, flat
        // terrain always remains pursuit terrain, even while an attack is telegraphing.
        private static bool TryGetFiringHoldReason(NPC npc, Player player, int direction, bool hasLos,
            out string reason)
        {
            reason = "";
            if (!hasLos || direction == 0 || !IsGrounded(npc))
            {
                return false;
            }

            int frontX = GetFrontTileX(npc, direction);
            int feetY = GetFeetTileY(npc);
            int drop = GetDropDepth(frontX, feetY, 6);
            if (drop >= 4)
            {
                reason = $"drop={drop}";
                return true;
            }

            int obstacleHeight = GetObstacleHeight(frontX, feetY);
            if (obstacleHeight > 1
                && (obstacleHeight > 6 || !HasHeadroomForJump(npc, direction, obstacleHeight)))
            {
                reason = $"blocked h={obstacleHeight}";
                return true;
            }

            Vector2 forwardCenter = npc.Center + new Vector2(direction * TileF, 0f);
            if (!HasStrictLineOfSight(forwardCenter, player.Center))
            {
                reason = "forward-loses-los";
                return true;
            }

            return false;
        }

        private const int StandoffMinTiles = 10; // walk out this far for a firing angle (clears most structures)
        private static bool TryRangedStandoff(NPC npc, Player player, tsorcRevampGlobalNPC globalNPC,
            float topSpeed, float acceleration, bool los, bool inRange, out string action, out string reason)
        {
            action = ""; reason = "";
            // Only stand off when the player is ABOVE and out of melee reach. A player BELOW should be
            // pursued by descending (drop / rope), not shot at from a perch — so don't standoff downward.
            // NOTE: LOS is NOT required to enter — we may need to step aside to FIND a sightline; we bail
            // below only if we neither have LOS nor can reach one by stepping.
            bool playerAbove = npc.Center.Y - player.Center.Y > 48f; // > 3 tiles up
            if (!(globalNPC.AttackList.Count > 0 && inRange && playerAbove)) return false;

            float dxToPlayer = npc.Center.X - player.Center.X; // > 0 => NPC is to the player's right
            float offsetTiles = Math.Abs(dxToPlayer) / TileF;
            int faceP = player.Center.X >= npc.Center.X ? 1 : -1;

            if (los && offsetTiles >= StandoffMinTiles
                && TryGetFiringHoldReason(npc, player, faceP, los, out string distantHoldReason))
            {
                // A firing angle is open, but navigation says advancing from it is unsafe.
                npc.direction = faceP; npc.spriteDirection = faceP;
                if (Math.Abs(npc.velocity.X) < 0.3f) npc.velocity.X = 0f; else npc.velocity.X *= 0.6f;
                action = "standoff-hold";
                reason = $"{distantHoldReason} off={offsetTiles:F0} d={npc.Distance(player.Center):F0}";
                return true;
            }

            // Step aside to open an angle. Score each side by how reachable a better-LOS spot is (see
            // StandoffSideScore): a side where line of sight opens (especially nearer) wins; otherwise the
            // side with more clear room. This steers it AWAY from a structure blocking the shot.
            int rScore = StandoffSideScore(npc, player, 1);
            int lScore = StandoffSideScore(npc, player, -1);
            // If we can neither see the player NOR reach a sightline by stepping (no side scores a LOS
            // spot, score >= 100), this isn't something the standoff can solve — let hold/patrol take over.
            if (!los && rScore < 100 && lScore < 100) return false;
            int sdir = los
                ? faceP
                : (rScore != lScore ? (rScore > lScore ? 1 : -1) : (dxToPlayer >= 0 ? 1 : -1));

            // Hold an open firing ray only when navigation says moving would be unsafe or would lose it.
            bool blockedAhead = TryGetFiringHoldReason(npc, player, sdir, los, out string holdReason);
            if (blockedAhead)
            {
                npc.direction = faceP; npc.spriteDirection = faceP;
                if (Math.Abs(npc.velocity.X) < 0.3f) npc.velocity.X = 0f; else npc.velocity.X *= 0.6f;
                action = "standoff-hold";
                reason = $"{holdReason} off={offsetTiles:F0} r={rScore} l={lScore}";
                return true;
            }
            npc.direction = sdir; npc.spriteDirection = sdir;
            ApplyChase(npc, sdir, topSpeed, acceleration);
            action = los ? "standoff-pursue" : "standoff-seek-los";
            reason = $"side={sdir} r={rScore} l={lScore} off={offsetTiles:F0}";
            return true;
        }

        // Scores a standoff offset side by how good a firing spot it leads to. Walks the columns out to
        // StandoffMinTiles, stopping at a wall (obstacle > 1 tile) or a cliff. If line of sight to the
        // player opens at a column, returns a high score that's larger the NEARER that LOS spot is
        // (100 + tiles-to-spare). Otherwise returns how many clear tiles it could walk (more room = better).
        private static int StandoffSideScore(NPC npc, Player player, int dir)
        {
            int feetY = GetFeetTileY(npc);
            int startX = (int)(npc.Center.X / TileF);
            int reachable = 0;
            for (int d = 1; d <= StandoffMinTiles; d++)
            {
                int x = startX + dir * d;
                if (GetObstacleHeight(x, feetY) > 1) break;   // wall this way -> can't walk farther
                if (GetDropDepth(x, feetY, 4) >= 4) break;     // cliff this way -> can't walk farther
                reachable = d;
                Vector2 spot = new Vector2(x * TileF + 8f, npc.Center.Y);
                if (HasStrictLineOfSight(spot, player.Center))
                    return 100 + (StandoffMinTiles - d);       // LOS opens at d tiles; nearer = higher score
            }
            return reachable;
        }

        private static Span FindContainingSpan(List<Span> spans, int x, int feetY)
        {
            // Strict pass: must contain x (Â±1) and be within 3 rows.
            Span best = null; int bestScore = int.MaxValue;
            foreach (var sp in spans)
            {
                if (x < sp.LeftX - 1 || x > sp.RightX + 1) continue;
                int dy = Math.Abs(sp.Y - feetY);
                if (dy > 3) continue;
                // Horizontal gap from x to this span, 0 when x already sits inside it. Vertical distance
                // is weighted 10x so a span on our own row always beats one a row off.
                int horizontalGap = 0;

                if (x < sp.LeftX)
                {
                    horizontalGap = sp.LeftX - x;
                }
                else if (x > sp.RightX)
                {
                    horizontalGap = x - sp.RightX;
                }

                int score = dy * 10 + horizontalGap;

                if (score < bestScore)
                {
                    bestScore = score;
                    best = sp;
                }
            }
            if (best != null) return best;

            // Permissive fallback: pick the nearest span by Manhattan distance.
            // This catches the airborne-player case so we still get a plan toward
            // where the player most likely is/will be.
            int bestDist = int.MaxValue;
            foreach (var sp in spans)
            {
                // Horizontal gap from x to this span, 0 when x already sits inside it.
                int dx = 0;

                if (x < sp.LeftX)
                {
                    dx = sp.LeftX - x;
                }
                else if (x > sp.RightX)
                {
                    dx = x - sp.RightX;
                }

                int dy = Math.Abs(sp.Y - feetY);
                // Cap so we don't reach into far rooms. The vertical cap scales with this NPC's jump power,
                // so high-jump experiments can still select a valid ledge span as their goal.
                if (dx > 16 || dy > Math.Max(12, MaxPlanRiseTiles())) continue;
                int d = dx + dy * 2;
                if (d < bestDist) { bestDist = d; best = sp; }
            }
            return best;
        }

        // ================================================================================
        //  A* search â€” goal-aware heuristic (5Ã— vertical weight)
        // ================================================================================

        private static List<Span> AStar(Span start, Span goal, int goalX, int goalY)
        {
            var open = new PriorityQueue<Span>();
            var cameFrom = new Dictionary<Span, Span>();
            var gScore = new Dictionary<Span, int>();
            gScore[start] = 0;
            open.Push(start, Heuristic(start, goalX, goalY));

            int iter = 0;
            while (open.Count > 0 && iter++ < 2500)
            {
                Span cur = open.Pop();
                if (cur == goal)
                {
                    var path = new List<Span>();
                    Span c = cur;
                    while (c != null) { path.Add(c); cameFrom.TryGetValue(c, out c); }
                    path.Reverse();
                    return path;
                }
                int curG = gScore[cur];
                foreach (var e in cur.Edges)
                {
                    int tentative = curG + e.Cost;
                    if (!gScore.TryGetValue(e.To, out int existing) || tentative < existing)
                    {
                        gScore[e.To] = tentative;
                        cameFrom[e.To] = cur;
                        int f = tentative + Heuristic(e.To, goalX, goalY);
                        open.Push(e.To, f);
                    }
                }
            }
            return null;
        }

        // Heuristic prefers being at the player's vertical level. Vertical mismatch
        // is weighted 5Ã— horizontal so the planner won't volunteer to drop a floor
        // unnecessarily when chasing.
        private static int Heuristic(Span sp, int goalX, int goalY)
        {
            int dx = sp.LeftX > goalX ? sp.LeftX - goalX
                   : goalX > sp.RightX ? goalX - sp.RightX : 0;
            int dy = Math.Abs(sp.Y - goalY);
            return dx + dy * 5;
        }

        // ================================================================================
        //  Plan conversion
        // ================================================================================

        private static List<PlanStep> ConvertToSteps(List<Span> spanPath, int playerX, int playerY)
        {
            var steps = new List<PlanStep>();
            for (int i = 0; i < spanPath.Count - 1; i++)
            {
                Span from = spanPath[i];
                Span to = spanPath[i + 1];
                Edge edge = null;
                foreach (var e in from.Edges)
                    if (e.To == to) { edge = e; break; }
                if (edge == null) continue;

                int entry;
                switch (edge.Kind)
                {
                    case EdgeKind.Walk:
                        entry = to.LeftX > from.RightX ? to.LeftX
                              : to.RightX < from.LeftX ? to.RightX
                              : (to.LeftX + to.RightX) / 2;
                        steps.Add(new PlanStep(StepKind.Walk, entry, to.Y, entry));
                        break;
                    case EdgeKind.JumpUp:
                    case EdgeKind.JumpGap:
                        steps.Add(new PlanStep(edge.Kind == EdgeKind.JumpUp ? StepKind.JumpUp : StepKind.JumpGap,
                            edge.LandX, to.Y, edge.LaunchX));
                        break;
                    case EdgeKind.Drop:
                        steps.Add(new PlanStep(StepKind.Drop, edge.LandX, to.Y, edge.LaunchX));
                        break;
                    case EdgeKind.PlatformDrop:
                        steps.Add(new PlanStep(StepKind.PlatformDrop, edge.LandX, to.Y, edge.LaunchX));
                        break;
                    case EdgeKind.RopeClimb:
                        steps.Add(new PlanStep(StepKind.RopeClimb, edge.LandX, to.Y, edge.LaunchX));
                        break;
                }
            }
            // Final approach to player's column.
            if (spanPath.Count > 0)
            {
                Span last = spanPath[spanPath.Count - 1];
                int finalX = Math.Clamp(playerX, last.LeftX, last.RightX);
                steps.Add(new PlanStep(StepKind.Walk, finalX, last.Y, finalX));
            }
            return steps;
        }

        // ================================================================================
        //  Step executor with committed actions
        // ================================================================================

        private static bool IsAligned(NPC npc, int launchX)
        {
            float c = launchX * TileF + 8f;
            return Math.Abs(npc.Center.X - c) <= AlignTolerancePx;
        }

        private static int AlignDir(NPC npc, int launchX)
        {
            float c = launchX * TileF + 8f;
            return npc.Center.X < c ? 1 : -1;
        }

        private static bool ExecuteStep(NavState nav, NPC npc, Player player, float topSpeed, float acceleration,
            float jumpCeil, float boostCeil, bool grounded, int doorDamage, tsorcRevampGlobalNPC globalNPC,
            out string action, out string reason)
        {
            PlanStep step = nav.Plan[nav.PlanIndex];
            int feetY = GetFeetTileY(npc);

            // ---- Step completion ----
            float tgtPx = step.TargetX * TileF + 8f;
            bool xClose = Math.Abs(npc.Center.X - tgtPx) <= AlignTolerancePx + 4f;
            bool xNear  = Math.Abs(npc.Center.X - tgtPx) <= TileF * 3f;
            // yErr > 0 means the feet are still BELOW the node (we haven't climbed to it yet).
            // The old symmetric |yErr| <= 2 was half this 3-tall sprite's height, so a Walk step
            // would "complete" while the body was still 2 tiles under the node — advancing the plan
            // up a span-staircase without ever gaining elevation, then wedging one tile short on the
            // next step. A walk-along node must be level-or-above (never left hanging below it).
            int yErr = feetY - step.TargetY;
            bool yClose = step.Kind == StepKind.Walk
                ? (yErr <= 1 && yErr >= -2)
                : Math.Abs(yErr) <= 2;
            bool completed = false;
            switch (step.Kind)
            {
                case StepKind.Walk: completed = grounded && xClose && yClose; break;
                default:           completed = grounded && yClose && xNear; break;
            }
            if (completed)
            {
                // Restore any rope-climb suspended gravity.
                ReleaseRopeTraversal(npc, abandonRopeStep: false);
                nav.PlanIndex++;
                nav.StepTimer = StepTimeoutFrames;
                nav.CommitFrames = 0;
                nav.AirCommitTimer = 0;
                nav.JumpFiredThisStep = false;
                nav.RopeEngaged = false; nav.RopeDirLatched = false; nav.RopeDismounting = false;
                action = "step-done";
                reason = $"#{nav.PlanIndex - 1}={step.Kind}";
                return false;
            }

            // ---- Step failure timeout ----
            if (nav.StepTimer == 0)
            {
                ReleaseRopeTraversal(npc, abandonRopeStep: false);
                // Bad-edge memory: remember this failed step's target span so the
                // next replan doesn't immediately pick the same route.
                int expiry = (int)Main.GameUpdateCount + 480; // 8 seconds
                nav.BadEdgeTargets[(step.TargetX, step.TargetY)] = expiry;
                nav.Plan = null;
                nav.PlanIndex = 0;
                nav.CommitFrames = 0;
                nav.JumpFiredThisStep = false;
                nav.RopeEngaged = false; nav.RopeDirLatched = false; nav.RopeDismounting = false;
                action = "step-timeout";
                reason = $"{step.Kind} target=({step.TargetX},{step.TargetY})";
                return false;
            }

            // ---- Dispatch ----
            switch (step.Kind)
            {
                case StepKind.Walk:
                    return ExecWalk(nav, npc, step, topSpeed, acceleration, jumpCeil, boostCeil, doorDamage, globalNPC, grounded, out action, out reason);
                case StepKind.JumpUp:
                case StepKind.JumpGap:
                    return ExecJump(nav, npc, step, topSpeed, acceleration, jumpCeil, boostCeil, grounded, feetY, out action, out reason);
                case StepKind.Drop:
                    return ExecDrop(nav, npc, step, topSpeed, acceleration, grounded, out action, out reason);
                case StepKind.PlatformDrop:
                    return ExecPlatformDrop(nav, npc, step, topSpeed, acceleration, grounded, out action, out reason);
                case StepKind.RopeClimb:
                    return ExecRopeClimb(nav, npc, step, topSpeed, acceleration, jumpCeil, boostCeil, grounded, out action, out reason);
            }
            action = "?"; reason = "";
            return false;
        }

        // Clean rope climb (SF4):
        //   Phase 1 — walk to the rope column while grounded.
        //   Phase 2 — ride straight up: HARD-snap X to the rope center (no SF1-style wobble),
        //             zero horizontal velocity, steady climb speed, refresh StepTimer while
        //             making upward progress so tall ropes don't hit the step timeout.
        //   Phase 3 — at the step-off height, restore gravity and hop toward the landing span;
        //             the standard "grounded + near target" completion check then advances the plan.
        private static bool ExecRopeClimb(NavState nav, NPC npc, PlanStep step, float topSpeed, float acceleration,
            float jumpCeil, float boostCeil, bool grounded, out string action, out string reason)
        {
            float ropeCenter = step.LaunchX * TileF + 8f;
            int feetY = GetFeetTileY(npc);
            bool onRope = npc.noGravity; // we set this true once riding begins
            // Travel direction is LATCHED for the step. Recomputing `step.TargetY > feetY` every
            // frame flips it to false the instant the feet reach the target row, which made the
            // dismount take the "up" branch and pop the NPC back up a tile — then descend re-armed
            // and it rode back down: that frame-by-frame flip was the on-rope vibration at the
            // target. Latch once we commit to the ride (Phase 2) and hold it for the whole step.
            bool descend = nav.RopeDirLatched ? nav.RopeDescend : step.TargetY > feetY; // target below -> ride down

            // Mark engagement once feet are actually within the rope tiles. Lets the NPC float
            // up to (or drop onto) a rope whose end hangs away from its starting position without
            // the end-of-rope check firing prematurely.
            if (IsRopeTile(step.LaunchX, feetY) || IsRopeTile(step.LaunchX, feetY - 1))
                nav.RopeEngaged = true;

            // Reset per-step flags whenever we're grounded and off the rope (fresh entry).
            if (grounded && !onRope) { nav.RopeJumpedThisStep = false; nav.RopeDirLatched = false; nav.RopeDismounting = false; }

            // If a rope tile is at our feet AND we're within a tile of the column, we're close enough to
            // grab — skip alignment and let Phase 2 engage (it glide-snaps X onto the rope). Crucial: do
            // NOT keep "aligning" in that case, because the rope sits at a drop edge (its own shaft) and a
            // cliff-aware walk would brake at that drop and never grab the rope right under us (the "rode
            // up, hopped off, stuck on the ledge" bug). The proximity check keeps us from skipping Phase 1
            // when the rope merely exists at our Y but we're still tiles away horizontally.
            bool ropeAtFeet = IsRopeTile(step.LaunchX, feetY)
                           || IsRopeTile(step.LaunchX, feetY - 1)
                           || IsRopeTile(step.LaunchX, feetY + 1);
            bool closeEnoughToGrab = ropeAtFeet && Math.Abs(npc.Center.X - ropeCenter) <= TileF;

            // Phase 1: align horizontally with the rope column while grounded (unless already close enough
            // to grab). Plain chase toward the column — NO cliff-halt/gap handling here, since the rope
            // lives at a drop. 1-tile steps are smoothed by AutoStepUp during the walk; only a 2-tile step
            // toward the rope needs a hop; abort only on a genuinely unclimbable wall.
            if (grounded && !onRope && !closeEnoughToGrab && Math.Abs(npc.Center.X - ropeCenter) > 4f)
            {
                int aDir = npc.Center.X < ropeCenter ? 1 : -1;
                npc.direction = aDir; npc.spriteDirection = aDir;
                nav.RopeStallFrames = 0; nav.LastRopeFeetY = feetY; nav.RopeEngaged = false;
                int aFrontX = GetFrontTileX(npc, aDir);
                int oh = GetObstacleHeight(aFrontX, feetY);
                if (oh == 2 && npc.collideX && HasHeadroomForJump(npc, aDir, oh)
                    && ComputeJumpArc(2, oh, npc.gravity, jumpCeil, topSpeed + boostCeil, out float hp, out float hvx))
                {
                    FireJump(nav, npc, aDir, hp, hvx, 16, 12);
                    nav.AlignStallFrames = 0;
                    action = "align-rope-hop"; reason = $"oh={oh}";
                    return true;
                }
                if (npc.collideX && oh > 2)
                {
                    nav.AlignStallFrames++;
                    if (nav.AlignStallFrames > 18) { nav.StepTimer = 0; nav.AlignStallFrames = 0; }
                }
                else nav.AlignStallFrames = 0;
                // Same alignment-precision floor as the jump launch column (see ExecJump): a low chase accel
                // shouldn't make the rope-column approach oscillate.
                ApplyChase(npc, aDir, topSpeed, Math.Max(acceleration, AlignAccelFloor));
                action = "align-rope"; reason = $"ropeX={step.LaunchX} cx={npc.Center.X / TileF:F1} {(descend ? "down" : "up")}";
                return true;
            }
            nav.AlignStallFrames = 0;

            // End-of-rope: only meaningful once engaged. Up = no rope above; down = no rope below.
            bool atRopeEnd = nav.RopeEngaged && (descend
                ? !IsRopeTile(step.LaunchX, feetY + 1)
                : !IsRopeTile(step.LaunchX, feetY - 1));
            bool reachedTarget = descend ? feetY >= step.TargetY : feetY <= step.TargetY;
            // Planner rope targets are real landing spans, but the no-plan rope fallback targets the
            // player's height while they are hanging. That height becomes stale as soon as the player
            // jumps sideways and must never be treated as permission to step off into open air.
            bool targetExitSafe = reachedTarget && RopeTargetHasSafeSideExit(step);

            // Climbing up into a SOLID (non-rope) ceiling at head height. The ride uses noTileCollide so the
            // body would otherwise phase up into the blocks above the rope. Treat this as a stop point so the
            // dismount logic resolves it instead of riding into the solid.
            bool ceilingCapped = !descend && nav.RopeEngaged
                && IsNavigationSolid(step.LaunchX, feetY - 2) && !IsRopeTile(step.LaunchX, feetY - 2);

            // Stall detection: blocked in the travel direction with no progress.
            bool noProgress = descend ? feetY <= nav.LastRopeFeetY : feetY >= nav.LastRopeFeetY;
            bool blocked = onRope && npc.collideY && noProgress;
            if (blocked) nav.RopeStallFrames++;
            else nav.RopeStallFrames = 0;
            bool forceDismount = nav.RopeStallFrames > 12;

            // PURSUE on the rope: if the player is hanging on THIS rope, don't ride to a stale fixed
            // target and dismount into the void; keep climbing toward the player's live position.
            // This fixes the rope on/off free-fall loop when the player is mid-rope (no landing there).
            // Stateless: as soon as the player leaves the rope, normal dismount logic resumes.
            if (onRope && nav.RopeEngaged && !atRopeEnd && !forceDismount && !ceilingCapped)
            {
                Player ropePlayer = Main.player[npc.target];
                int pFeetY = (int)((ropePlayer.Bottom.Y - 1f) / TileF);
                bool playerOnThisRope = Math.Abs(ropePlayer.Center.X - ropeCenter) < TileF * 1.5f
                                        && IsRopeTile(step.LaunchX, pFeetY);
                if (playerOnThisRope)
                {
                    npc.noGravity = true; npc.noTileCollide = true;
                    npc.position.X = ropeCenter - npc.width / 2f; npc.velocity.X = 0f;
                    // Stop only at collision-safe spacing; attacks continue naturally during the climb.
                    int gap = feetY - pFeetY; // > 0 we're below the player, < 0 we're above
                    int holdFeetY = gap >= 0 ? pFeetY + RopePursuitGap : pFeetY - RopePursuitGap;
                    if (feetY > holdFeetY && IsRopeTile(step.LaunchX, feetY - 1))
                    {
                        npc.velocity.Y = -RopeClimbSpeed;
                        nav.RopeDescend = false; // if the player leaves now, keep going toward the upper exit
                    }
                    else if (feetY < holdFeetY && IsRopeTile(step.LaunchX, feetY + 1))
                    {
                        npc.velocity.Y = RopeClimbSpeed;
                        nav.RopeDescend = true; // if the player leaves now, keep going toward the lower exit
                    }
                    else npc.velocity.Y = 0f;
                    nav.StepTimer = StepTimeoutFrames; // live pursuit progress should not bad-edge the rope
                    nav.CommitFrames = Math.Max(nav.CommitFrames, 8);
                    nav.RopeStallFrames = 0; nav.LastRopeFeetY = feetY; nav.RopeDismounting = false;
                    action = npc.velocity.Y < 0f ? "rope-pursue-up"
                        : npc.velocity.Y > 0f ? "rope-pursue-down"
                        : "rope-caught-up";
                    reason = $"pFeetY={pFeetY} feetY={feetY} target={holdFeetY}";
                    return true;
                }
            }

            // Phase 3: dismount — reached target, ran off the rope end, hit a ceiling, or stalled.
            if (onRope && (targetExitSafe || atRopeEnd || forceDismount || ceilingCapped))
            {
                npc.noGravity = false;
                npc.noTileCollide = false; // re-enable collision before any horizontal dismount motion
                nav.RopeStallFrames = 0;
                int rise = feetY - step.TargetY; // > 0 means the target is still above us

                // DEAD END (up): a solid ceiling is above us and we have NOT reached the (higher) target — this
                // rope physically can't deliver us there (the planner aimed at a span sitting above solid blocks).
                // Abandon the route and replan instead of looping dismount<->re-grab at the rope top, which is the
                // on-rope vibration, with the head stuck phasing in the ceiling. Bad-edge it so we don't immediately
                // retry the same dead-end rope.
                if (!descend && !reachedTarget && IsNavigationSolid(step.LaunchX, feetY - 2))
                {
                    nav.BadEdgeTargets[(step.TargetX, step.TargetY)] = (int)Main.GameUpdateCount + 480;
                    nav.Plan = null; nav.PlanIndex = 0; nav.CommitFrames = 0;
                    nav.RopeEngaged = false;
                    action = "rope-deadend"; reason = $"toY={step.TargetY} ropeTop~{feetY} solidAbove";
                    return false;
                }

                // Second forced dismount = real ceiling/floor block, not a jump-through platform.
                if (forceDismount && nav.RopeJumpedThisStep)
                {
                    int expiry = (int)Main.GameUpdateCount + 480;
                    nav.BadEdgeTargets[(step.TargetX, step.TargetY)] = expiry;
                    nav.Plan = null; nav.PlanIndex = 0; nav.CommitFrames = 0;
                    action = "rope-abort"; reason = $"blocked toY={step.TargetY}";
                    return false;
                }

                // Upward only: jump straight up onto a platform above the rope top — but ONLY if
                // the space directly above is clear/jump-through. If a SOLID block sits above the
                // rope top, jumping up would phase through it (the "phased through solid blocks
                // above rope" bug); fall through to a sideways hop toward the target instead.
                bool solidAbove = IsNavigationSolid(step.LaunchX, feetY - 2);
                if (!descend && (atRopeEnd || forceDismount) && rise >= 2 && !solidAbove)
                {
                    ComputeJumpArc(0, rise, npc.gravity, jumpCeil, topSpeed + boostCeil,
                                   out float vp, out _);
                    npc.position.X = RopeSnapX(npc, step.LaunchX, ropeCenter, feetY);
                    npc.velocity.X = 0f;
                    npc.velocity.Y = -Math.Max(vp, 6f);
                    nav.AirCommitDirX = 0; nav.AirCommitTimer = 0; nav.CommittedLaunchVx = 0f;
                    nav.CommitFrames = 24;
                    nav.RopeDismounting = true; // don't let Phase 2 re-grab/re-snap while leaving
                    if (forceDismount) nav.RopeJumpedThisStep = true;
                    action = "rope-jumpup"; reason = $"rise={rise} p={Math.Max(vp, 6f):F1}";
                    return true;
                }

                // Step off toward the landing span.
                int offDir = step.TargetX > step.LaunchX ? 1
                           : step.TargetX < step.LaunchX ? -1
                           : npc.direction;
                npc.velocity.Y = descend ? 0.5f : -3.2f; // down: let gravity settle; up: small pop
                npc.velocity.X = offDir * Math.Max(1.4f, topSpeed);
                npc.direction = offDir; npc.spriteDirection = offDir;
                nav.AirCommitDirX = offDir;
                nav.AirCommitTimer = 14;
                nav.CommittedLaunchVx = 0f;
                nav.CommitFrames = 14;
                nav.RopeDismounting = true; // don't let Phase 2 re-grab/re-snap while stepping off
                action = descend ? "rope-exit-down" : "rope-exit"; reason = $"toY={step.TargetY} dir={offDir}";
                return true;
            }

            // Anti-vibration guard: once we've engaged the rope and then dismounted, do NOT
            // re-grab it just because we're still near it — that climb/dismount flip-flop is the
            // on-rope "vibration". Only (re-)enter the ride if still on it, or if we've never
            // engaged yet and a rope tile is at/adjacent to the feet (initial grab / float toward).
            bool atRopeNow = IsRopeTile(step.LaunchX, feetY)
                          || IsRopeTile(step.LaunchX, feetY - 1)
                          || IsRopeTile(step.LaunchX, feetY + 1);
            // RopeDismounting: we've already fired an intentional dismount (reached target / ran off
            // the end / jumped up) this step. Do NOT re-enter Phase 2 even though we're still next to
            // the rope — Phase 2 hard-snaps X back to the rope center, which was erasing the step-off
            // and pinning the NPC on the rope (the dismount that "never took"). Let the committed
            // dismount velocity carry it clear; the step completes once grounded near the target.
            if (!onRope && (nav.RopeDismounting || (nav.RopeEngaged && !atRopeNow)))
            {
                action = "rope-detached"; reason = nav.RopeDismounting ? "dismounting" : "off-rope";
                return false;
            }

            // Don't start the ride until we're actually at the rope column. If a Phase-1 step-hop left us
            // airborne mid-approach (grounded check skipped Phase 1), reaching here while still >1 tile
            // from the rope would hijack the hop into a long noGravity glide across to the rope. Let the
            // hop finish — we align and engage once we land near the column. (X-aligned float-ups, where
            // we're under the rope and rising, are within a tile of center so they still engage.)
            if (!onRope && Math.Abs(npc.Center.X - ropeCenter) > TileF)
            {
                action = "rope-approach"; reason = $"cx={npc.Center.X / TileF:F1} ropeX={step.LaunchX}";
                return true;
            }

            // A hanging rope can end a few tiles above the floor. Jump up and physically intersect it
            // before taking ownership of gravity/collision; never float upward with noGravity from below.
            if (!onRope && !atRopeNow)
            {
                if (!descend && grounded
                    && FindRopeSpan(step.LaunchX, feetY, 5, out int ropeBottomY, out _)
                    && ropeBottomY < feetY)
                {
                    int riseToGrab = feetY - ropeBottomY;
                    ComputeJumpArc(0, riseToGrab, npc.gravity, jumpCeil, topSpeed + boostCeil,
                        out float grabPower, out _);
                    int facing = npc.direction != 0 ? npc.direction : 1;
                    FireJump(nav, npc, facing, Math.Max(grabPower, 5f), 0f, 18, 24);
                    nav.AirCommitDirX = 0;
                    nav.CommittedLaunchVx = 0f;
                    action = "rope-grab-hop";
                    reason = $"bottom={ropeBottomY} rise={riseToGrab} p={Math.Max(grabPower, 5f):F1}";
                    return true;
                }

                action = "rope-grab-air";
                reason = $"feetY={feetY} awaiting-contact";
                return true;
            }

            // Commit the travel direction for the rest of this step now that we're actually riding.
            if (!nav.RopeDirLatched) { nav.RopeDescend = descend; nav.RopeDirLatched = true; }

            // Phase 2: steady vertical ride. noTileCollide lets the body ignore blocks beside the
            // rope (and the solid underside of a destination ledge the rope passes), mimicking how
            // a player climbs a rope between walls. Safe from sideways wall-phasing because
            // velocity.X is zeroed and X is converged to the rope center.
            npc.noGravity = true;
            npc.noTileCollide = true;
            // Converge X to the rope center with a per-frame cap instead of an instant set. Phase 1
            // normally walks the NPC into alignment while grounded, but when the ride is entered
            // AIRBORNE (e.g. drifting off one rope straight onto a nearby one), an instant snap looks
            // like a teleport that skips the short walk between the ropes. Glide on instead; snap
            // exactly once within the cap so the ride stays centered (no sideways phasing).
            float ropeLeft = ropeCenter - npc.width / 2f;
            float dxToRope = ropeLeft - npc.position.X;
            const float RopeSnapStep = 4f; // px/frame; ~matches climb speed for a natural grab-on glide
            npc.position.X = Math.Abs(dxToRope) <= RopeSnapStep ? ropeLeft : npc.position.X + Math.Sign(dxToRope) * RopeSnapStep;
            npc.velocity.X = 0f;
            npc.velocity.Y = descend ? RopeClimbSpeed : -RopeClimbSpeed;
            nav.CommitFrames = Math.Max(nav.CommitFrames, 8);
            // Refresh the step timeout ONLY while making progress toward the target. If the ride
            // stalls (blocked), let StepTimer run down so the step times out and replans.
            bool progressed = descend ? feetY > nav.LastRopeFeetY : feetY < nav.LastRopeFeetY;
            if (progressed) nav.StepTimer = StepTimeoutFrames;
            nav.LastRopeFeetY = feetY;
            action = descend ? "rope-descend" : "rope-climb";
            reason = $"feetY={feetY}->toY={step.TargetY} eng={nav.RopeEngaged} stall={nav.RopeStallFrames}"
                + (reachedTarget && !targetExitSafe ? " unsafeTarget=continue" : "");
            return true;
        }

        // A normal A* rope step targets a standable span immediately beside the rope. The synthesized
        // player-on-rope fallback deliberately does not require one, so validate again at execution time.
        private static bool RopeTargetHasSafeSideExit(PlanStep step)
        {
            if (Math.Abs(step.TargetX - step.LaunchX) != 1) return false;
            return IsStandableTile(step.TargetX, step.TargetY + 1)
                && HasBodyClearanceAtRow(step.TargetX, step.TargetY);
        }

        private static bool ExecWalk(NavState nav, NPC npc, PlanStep step, float topSpeed, float acceleration,
            float jumpCeil, float boostCeil, int doorDamage, tsorcRevampGlobalNPC globalNPC, bool grounded,
            out string action, out string reason)
        {
            float tgtPx = step.TargetX * TileF + 8f;
            int dir = npc.Center.X < tgtPx - AlignTolerancePx * 0.5f ? 1
                    : npc.Center.X > tgtPx + AlignTolerancePx * 0.5f ? -1
                    : npc.direction;
            npc.direction = dir; npc.spriteDirection = dir;
            if (!grounded) { action = "walk-air"; reason = ""; return false; }
            // Step-hop / door pass â€” but if a tall wall blocks, mark this walk as failed.
            if (doorDamage > 0 && TryDoor(npc, dir, doorDamage, globalNPC, out action, out reason)) return true;
            // Walk steps never legitimately descend a cliff (those are Drop/JumpGap steps) → keep cliff-halt.
            if (TryLocalTerrain(nav, npc, dir, jumpCeil, boostCeil, topSpeed, false, out action, out reason))
            {
                if (action == "blocked")
                {
                    // Walk step can't progress â€” abort step so the planner reroutes.
                    nav.StepTimer = 0;
                }
                return true;
            }
            ApplyChase(npc, dir, topSpeed, acceleration);
            action = "walk"; reason = $"->{step.TargetX}";
            return true;
        }

        private static bool ExecJump(NavState nav, NPC npc, PlanStep step, float topSpeed, float acceleration,
            float jumpCeil, float boostCeil, bool grounded, int feetY, out string action, out string reason)
        {
            if (!grounded)
            {
                // Only abort if genuinely WEDGED against a wall mid-air (near-zero velocity in both
                // axes), not merely brushing one during a healthy jump — the latter is normal in
                // tight interiors and aborting on it broke upward traversal through stacked platforms.
                if (npc.collideX && Math.Abs(npc.velocity.Y) < 0.5f && Math.Abs(npc.velocity.X) < 0.5f)
                {
                    int expiry = (int)Main.GameUpdateCount + 480;
                    nav.BadEdgeTargets[(step.TargetX, step.TargetY)] = expiry;
                    nav.StepTimer = Math.Min(nav.StepTimer, 8);
                }
                action = "jump-air";
                reason = $"commit={nav.CommitFrames} cx={npc.collideX}";
                return false;
            }
            // Grounded again after this step already fired its jump arc, yet the step didn't complete
            // (ExecuteStep checks completion BEFORE dispatching here) → the jump physically undershot/missed
            // its target span. Don't re-align and re-fire (that's the ~3s / 3-jump shuffle); bad-edge this
            // target and reroute now, after the single committed attempt (~1s). A makeable jump completes on
            // the first aligned try, so this only trips on bad proposals.
            if (nav.JumpFiredThisStep)
            {
                nav.BadEdgeTargets[(step.TargetX, step.TargetY)] = (int)Main.GameUpdateCount + 480;
                nav.StepTimer = 0; // ExecuteStep nulls the plan next frame → clean replan around the bad edge
                npc.velocity.X *= 0.5f;
                action = "jump-missed"; reason = $"undershot target=({step.TargetX},{step.TargetY})";
                return true;
            }
            int absDx = Math.Abs(step.TargetX - step.LaunchX);
            // For pure vertical jumps we need tighter alignment (Â±5 px) since the
            // body has to fit through a narrow vertical opening, and we ALSO need
            // to bring the NPC to a near-stop before firing â€” otherwise residual
            // velocity carries it past the column even with AirCommit disabled.
            bool isVertical = absDx == 0;
            float alignTol = isVertical ? 5f : AlignTolerancePx;
            float launchCenter = step.LaunchX * TileF + 8f;
            bool aligned = Math.Abs(npc.Center.X - launchCenter) <= alignTol;
            if (!aligned)
            {
                int aDir = npc.Center.X < launchCenter ? 1 : -1;
                npc.direction = aDir; npc.spriteDirection = aDir;
                // Settling onto a launch column needs decisive acceleration regardless of how the enemy
                // CHASES. Acceleration is doing double duty (chase responsiveness vs alignment precision):
                // a deliberately-low chase accel (e.g. 0.05 archers, 0.03 casters) made the creep take ~14
                // frames to reach even the 0.7 creep cap, so every settle-and-reset overshot the column and
                // oscillated forever (306 align-jump vs 5 jump-fire in the log). Floor the accel used for
                // alignment ONLY — the enemy's chase feel elsewhere is untouched; here it just positions
                // crisply. Heavy beasts keep their ponderous pursuit but can still line up a jump.
                float alignAccel = Math.Max(acceleration, AlignAccelFloor);
                // Final-approach creep: within ~2 tiles of the launch column, bleed speed fast and cap the
                // approach low so the NPC settles ONTO the column instead of skating across it and
                // oscillating (the jittery pre-jump left-right shuffle). Outside that, chase at full speed.
                // ApplyChase keeps driving toward the column at the creep speed, so it still converges into
                // the alignment window (no deadlock) just deliberately instead of overshooting.
                if (Math.Abs(npc.Center.X - launchCenter) < TileF * 2f)
                {
                    if (Math.Abs(npc.velocity.X) > 0.7f) npc.velocity.X *= 0.6f;
                    ApplyChase(npc, aDir, Math.Min(topSpeed, 0.7f), alignAccel);
                }
                else
                {
                    ApplyChase(npc, aDir, topSpeed, alignAccel);
                }
                // Can't reach the launch column — it's unreachable (A* picked a launch behind a wall or
                // up a step the approach-creep can't take). Don't press toward it for the whole StepTimer
                // (~2-3s, the "stares the wrong way standing still" wedge); bail after ~0.3s so it replans
                // a reachable route. POSITION-keyed, not collideX-keyed: when the body rests against the
                // wall, vanilla zeroes velocity.X and the next frame's tiny re-accelerate doesn't re-touch
                // it, so collideX flickers false every other frame and the old gate never reached the abort
                // (the permanent align-jump freeze in the log). Track raw X progress toward the column
                // instead — no movement for ~18 frames = wedged.
                if (Math.Abs(npc.Center.X - nav.AlignLastX) > 1.5f) { nav.AlignLastX = npc.Center.X; nav.AlignStallFrames = 0; }
                else if (++nav.AlignStallFrames > 18) { nav.StepTimer = 0; nav.AlignStallFrames = 0; }
                action = "align-jump";
                reason = $"launch={step.LaunchX} cx={npc.Center.X / TileF:F1} stall={nav.AlignStallFrames}";
                return true;
            }
            nav.AlignStallFrames = 0;
            // For vertical jumps, additionally require near-zero residual velocity.
            // Without this, the airborne arc drifts off-column.
            if (isVertical && Math.Abs(npc.velocity.X) > 0.4f)
            {
                npc.velocity.X *= 0.5f;
                action = "settle-jump";
                reason = $"vx={npc.velocity.X:F2}";
                return true;
            }
            // Physics-based arc (SF4): solve the exact jump from npc.gravity + jump limits.
            int rise = feetY - step.TargetY;          // positive = rising; negative = target below

            // Descending target directly below us: NEVER jump up to go down. Drop straight down instead.
            // CRITICAL: noTileCollide passes SOLID tiles too, not just platforms — so gate it on what's
            // actually below (mirrors ExecDrop). A solid floor below = invalid drop → bad-edge + replan
            // (this was the "Invader moved THROUGH solid tiles to change levels" bug); a platform =
            // phase through it; open air = just fall (no noTileCollide). ManagePlatformPass ends the pass.
            if (rise < 0 && absDx <= 1)
            {
                int belowFeet = GetFeetTileY(npc) + 1;
                bool platformBelow = IsPlatformTile(step.LaunchX, belowFeet);
                bool solidBelow = !platformBelow && IsNavigationSolid(step.LaunchX, belowFeet);
                if (solidBelow)
                {
                    int expiry = (int)Main.GameUpdateCount + 480;
                    nav.BadEdgeTargets[(step.TargetX, step.TargetY)] = expiry;
                    nav.StepTimer = 0;
                    action = "jump-dropdown-blocked"; reason = "solid-below";
                    return true;
                }
                // Phase a platform OR clear the lips of a too-narrow open shaft (the 18px body is wider
                // than a 1-tile hole and gets caught otherwise). solidBelow is excluded above, so this
                // only ever phases lips, never a solid floor; ManagePlatformPass ends the pass.
                npc.noTileCollide = true;
                npc.velocity.X = 0f;
                npc.velocity.Y = Math.Max(npc.velocity.Y, 2.0f);
                nav.PlatformPassActive = true;
                nav.PlatformPassTimer = 24;
                nav.PlatformPassStartY = npc.Bottom.Y;
                nav.CommitFrames = 25;
                nav.AirCommitDirX = 0;
                nav.CommittedLaunchVx = 0f;
                action = "jump-dropdown"; reason = $"down rise={rise} plat={platformBelow}";
                return true;
            }

            float maxLaunchVx = topSpeed + boostCeil;
            int launchDir = step.TargetX > step.LaunchX ? 1
                          : step.TargetX < step.LaunchX ? -1
                          : npc.direction;
            bool feasible = ComputeJumpArc(absDx, rise, npc.gravity, jumpCeil, maxLaunchVx,
                                           out float power, out float launchVx);
            if (!feasible)
            {
                // Not makeable for this NPC's gravity / jump ceiling — abort the step so the
                // planner reroutes instead of leaping into the pit. (This is the guarantee:
                // SF4 only commits to a jump it can physically complete.)
                nav.StepTimer = 0;
                npc.velocity.X *= 0.5f;
                action = "jump-abort";
                reason = $"infeasible dx={absDx} rise={rise} g={npc.gravity:F2} maxVx={maxLaunchVx:F1}";
                return true;
            }
            int airCommitFrames = EstimateJumpAirFrames(power, rise, npc.gravity);
            FireJump(nav, npc, launchDir, power, launchVx, airCommitFrames, airCommitFrames + 10);
            nav.JumpFiredThisStep = true;
            if (isVertical)
            {
                // Kill horizontal velocity and disable the airborne X-lock so we go
                // straight up — the "stop and jump straight up" capability.
                npc.velocity.X = 0f;
                nav.AirCommitDirX = 0;
                nav.CommittedLaunchVx = 0f;
            }
            action = "jump-fire";
            reason = $"phys dx={absDx} rise={rise} g={npc.gravity:F2} => p{power:F1}/vx{launchVx:F2} dir={launchDir}";
            return true;
        }

        // ================================================================================
        //  Physics-based jump solver (SF4) — replaces the static JumpArcs table.
        //
        //  Computes an exact projectile arc so a jump fires only when it's genuinely
        //  makeable, and is sized to land ~1 tile onto the destination (no overshoot).
        //  Reads the NPC's own gravity (npc.gravity) — the SAME value the engine integrates
        //  every frame — so prediction == reality, and light vs heavy enemies get correct,
        //  distinct arcs automatically.
        //
        //    dxTiles      : horizontal launch->landing distance (tiles, >= 0)
        //    riseTiles    : vertical gain (tiles). >0 = landing higher; <0 = drop.
        //    gravity      : npc.gravity (px/frame^2)
        //    maxJumpPower : ceiling on launch vertical speed (globalNPC.MaxJumpPower)
        //    maxLaunchVx  : ceiling on launch horizontal speed (topSpeed + globalNPC.MaxJumpBoost)
        //  Returns true with out jumpPower/launchVx if a feasible arc exists; false if the
        //  gap is too wide or the rise too high for this NPC's limits.
        // ================================================================================
        private static bool ComputeJumpArc(int dxTiles, int riseTiles, float gravity,
            float maxJumpPower, float maxLaunchVx, out float jumpPower, out float launchVx)
        {
            jumpPower = 0f; launchVx = 0f;
            if (gravity <= 0f) gravity = 0.3f;

            const float Tile = 16f;
            const float LandMarginTiles = 1.0f;   // aim ~1 tile onto the platform
            const float ClearLipTiles = 1.0f;     // apex must clear the destination lip
            const float MinApexTiles = 2.5f;      // floor so jumps read as jumps, not flat skips

            float dxPx = (dxTiles + LandMarginTiles) * Tile;
            // Downward-positive Y: a rise (landing higher) is a negative delta.
            float landingDeltaY = -riseTiles * Tile;

            // Minimum power to physically reach the required apex height.
            float neededApexPx = Math.Max(MinApexTiles * Tile,
                                          (Math.Max(0, riseTiles) + ClearLipTiles) * Tile);
            float minPower = (float)Math.Sqrt(2f * gravity * neededApexPx);
            float startPower = Math.Max(minPower, 4f);

            // Raise power until the required horizontal speed fits under the cap.
            // More airtime -> lower vx needed, so vx decreases monotonically with power;
            // the first power that fits is the lowest (least floaty) feasible arc.
            float chosenVx = float.MaxValue;
            float chosenPower = startPower;
            for (float p = startPower; p <= maxJumpPower + 1e-3f; p += 0.1f)
            {
                float disc = p * p + 2f * gravity * landingDeltaY;
                if (disc < 0f) continue;            // can't reach that height at this power
                float airtime = (p + (float)Math.Sqrt(disc)) / gravity; // descending-branch landing
                if (airtime <= 0f) continue;
                float vx = dxPx / airtime;
                if (vx <= maxLaunchVx)
                {
                    chosenPower = p;
                    chosenVx = vx;
                    break;
                }
            }

            if (chosenVx > maxLaunchVx)
                return false;   // unreachable for this NPC

            jumpPower = MathHelper.Clamp(chosenPower, 4f, maxJumpPower);
            launchVx = MathHelper.Clamp(chosenVx, 0.4f, maxLaunchVx);
            return true;
        }

        private static int EstimateJumpAirFrames(float jumpPower, int riseTiles, float gravity)
        {
            if (gravity <= 0f) gravity = 0.3f;
            float landingDeltaY = -riseTiles * TileF;
            float disc = jumpPower * jumpPower + 2f * gravity * landingDeltaY;
            if (disc < 0f) return 35;
            float frames = (jumpPower + (float)Math.Sqrt(disc)) / gravity;
            return Math.Min(Math.Max((int)Math.Ceiling(frames) + 8, 35), 150);
        }

        private static (float power, float boost) NearestArc(int absDx, int dy, float jumpCeil, float boostCeil)
        {
            // Pick the closest table entry by Manhattan distance in (dx, dy) space.
            int bestKeyDist = int.MaxValue;
            (float p, float b) best = (jumpCeil, boostCeil * 0.5f);
            foreach (var kv in JumpArcs)
            {
                int d = Math.Abs(kv.Key.dx - absDx) + Math.Abs(kv.Key.dy - dy);
                if (d < bestKeyDist) { bestKeyDist = d; best = kv.Value; }
            }
            return best;
        }

        private static bool ExecDrop(NavState nav, NPC npc, PlanStep step, float topSpeed, float acceleration,
            bool grounded, out string action, out string reason)
        {
            if (!grounded) { action = "drop-air"; reason = ""; return false; }
            // Chase onto the launch column. Do NOT brake before we're aligned — an earlier attempt to
            // "settle" here created a 12-16px dead band (brake zone wider than the align tolerance) where
            // the body halted ~0.8 tile short and never closed the gap, deadlocking align-drop for 30s+.
            // Overshoot past the column is instead handled at commit: the straight-drop branch below zeroes
            // horizontal velocity so we fall cleanly even if we arrive with residual walk speed.
            if (!IsAligned(npc, step.LaunchX))
            {
                int aDir = AlignDir(npc, step.LaunchX);
                npc.direction = aDir; npc.spriteDirection = aDir;
                ApplyChase(npc, aDir, topSpeed, acceleration);
                action = "align-drop"; reason = $"launch={step.LaunchX}";
                return true;
            }
            // Inspect the tile directly below the launch column.
            int belowFeet = GetFeetTileY(npc) + 1;
            bool platformBelow = IsPlatformTile(step.LaunchX, belowFeet);
            bool solidBelow = !platformBelow && IsNavigationSolid(step.LaunchX, belowFeet);
            if (solidBelow)
            {
                // Drop edge is invalid here (solid floor, nothing to fall through) — abort so the
                // planner reroutes instead of shuffling in place (the oscillating "drop" from the log).
                int expiry = (int)Main.GameUpdateCount + 480;
                nav.BadEdgeTargets[(step.TargetX, step.TargetY)] = expiry;
                nav.StepTimer = 0;
                action = "drop-blocked"; reason = "solid-below";
                return true;
            }
            int descend = step.TargetX > step.LaunchX ? 1
                        : step.TargetX < step.LaunchX ? -1
                        : npc.direction;
            npc.direction = descend; npc.spriteDirection = descend;
            if (platformBelow || step.TargetX == step.LaunchX)
            {
                // Straight drop down the same column — through a wood platform OR an open shaft. CRITICAL
                // for the open-shaft case: this NPC's hitbox (18px) is WIDER than a 1-tile hole, so it
                // rests on the 1px lips of the gap and plain gravity can NEVER pull it through (the
                // "stuck on a platform, won't drop, vel=0, plat=False" freeze). So briefly noTileCollide
                // to clear the lips, exactly like a platform pass. SAFE: solidBelow was already excluded
                // above and the shaft was validated clear by HasDropClearance, so we only phase the lips,
                // not a solid floor; ManagePlatformPass ends the pass after ~1 tile of descent or on
                // landing, and the Run-loop safety clears noTileCollide the moment the pass ends.
                // CRITICAL: zero horizontal velocity while noTileCollide is active — any sideways motion
                // would phase THROUGH walls (the "passed through a solid wall" bug).
                npc.noTileCollide = true;
                npc.velocity.X = 0f;
                npc.velocity.Y = Math.Max(npc.velocity.Y, 1.6f);
                nav.PlatformPassActive = true;
                nav.PlatformPassTimer = 18;
                nav.PlatformPassStartY = npc.Bottom.Y;
                nav.AirCommitDirX = 0;
                nav.CommittedLaunchVx = 0f;
                nav.CommitFrames = 25;
            }
            else
            {
                // Free-fall through an open gap — drift toward the target column (no walls here).
                ApplyChase(npc, descend, topSpeed, acceleration);
                nav.AirCommitDirX = descend;
                nav.AirCommitTimer = 25;
                nav.CommitFrames = 25;
                nav.CommittedLaunchVx = 0f;
            }
            action = "drop"; reason = $"toY={step.TargetY} plat={platformBelow}";
            return true;
        }

        private static bool ExecPlatformDrop(NavState nav, NPC npc, PlanStep step, float topSpeed, float acceleration,
            bool grounded, out string action, out string reason)
        {
            if (!grounded || nav.PlatformPassActive)
            {
                action = "pdrop-air"; reason = "";
                return false;
            }
            if (!IsAligned(npc, step.LaunchX))
            {
                int aDir = AlignDir(npc, step.LaunchX);
                npc.direction = aDir; npc.spriteDirection = aDir;
                ApplyChase(npc, aDir, topSpeed, acceleration);
                action = "align-pdrop"; reason = $"launch={step.LaunchX}";
                return true;
            }
            npc.noTileCollide = true;
            npc.velocity.Y = Math.Max(npc.velocity.Y, 1.6f);
            nav.PlatformPassActive = true;
            nav.PlatformPassTimer = 18;
            nav.PlatformPassStartY = npc.Bottom.Y;
            nav.CommitFrames = 25;
            action = "platform-drop"; reason = $"toY={step.TargetY}";
            return true;
        }

        // ================================================================================
        //  Local terrain handlers (step-hop, small gap-jump) for inside Walk steps
        //  and the no-plan fallback chase.
        // ================================================================================

        // Pit/shaft escape: the player is well ABOVE and a normal sideways jump is blocked by an overhang lip —
        // HasTrajectoryClearance (A* edge) and HasHeadroomForJump (local obstacle-jump) both test the column TOWARD
        // the player, which the lip blocks, so A* makes no edge and the local jump refuses. A ballistic up-and-over
        // arc can't clear a lip that sits IN its path; the only way out is to rise straight up the shaft, then drift
        // sideways onto the ledge once above the lip. Fires a near-full VERTICAL jump and arms ShaftEscapeDir; the
        // Run airborne handler applies the late horizontal drift. Only triggers when genuinely boxed in a shaft
        // (a wall/overhang at the sides) with clear vertical room — open ground / reachable ledges fall through.
        // Clear vertical tiles straight up from the feet row in column `col` (capped at 8).
        private static int ColumnUpClear(int col, int feetY)
        {
            int n = 0;
            for (int u = 1; u <= 8; u++)
            {
                if (IsNavigationSolid(col, feetY - u)) break;
                n = u;
            }
            return n;
        }

        // Clear vertical room straight up across the NPC's FULL body width (min over every column the body occupies).
        // A wide body (e.g. the 38px Basilisk = ~2.4 tiles) can have a clear center column but an EDGE under an
        // overhang lip — jumping then instantly clips the lip and never rises. The min-over-width is the real ceiling.
        private static int BodyUpClear(NPC npc, int feetY)
        {
            int leftCol = (int)(npc.position.X / TileF);
            int rightCol = (int)((npc.position.X + npc.width - 1f) / TileF);
            int minClear = 8;
            for (int c = leftCol; c <= rightCol; c++)
                minClear = Math.Min(minClear, ColumnUpClear(c, feetY));
            return minClear;
        }

        private static bool TryShaftEscape(NavState nav, NPC npc, Player player, float jumpCeil, float topSpeed, float acceleration, out string action, out string reason)
        {
            action = ""; reason = "";
            if (player.Center.Y >= npc.Center.Y - 2f * TileF) return false; // player not meaningfully above
            // No A* path to the player means they're behind an impassable wall. Jumping straight up
            // will only loop the NPC against the wall forever — skip and let the stuck clock disengage.
            if (nav.NoAStarPath) return false;
            int feetY = GetFeetTileY(npc);
            int col = (int)(npc.Center.X / TileF);

            // Boxed? a wall/overhang on at least one side at body height (we're in a shaft / under a lip). If both
            // sides are open AND there's clear room straight up, it's not a shaft — let the normal jump handle it.
            bool leftWalled = IsNavigationSolid(col - 1, feetY) || IsNavigationSolid(col - 1, feetY - 1) || IsNavigationSolid(col - 1, feetY - 2);
            bool rightWalled = IsNavigationSolid(col + 1, feetY) || IsNavigationSolid(col + 1, feetY - 1) || IsNavigationSolid(col + 1, feetY - 2);
            int curUp = BodyUpClear(npc, feetY); // full body width — a wide body's EDGE under the lip blocks the rise
            if (!leftWalled && !rightWalled && curUp >= 4) return false;

            int driftDir = player.Center.X >= npc.Center.X ? 1 : -1;

            if (curUp >= 4)
            {
                // On a launch column → rise straight up; drift toward the player's side once above the lip.
                FireJump(nav, npc, driftDir, jumpCeil, 0f, 35, 45);
                npc.velocity.X = 0f;   // pure vertical rise; the sideways drift comes later (ShaftEscapeDir, in Run)
                nav.AirCommitDirX = 0;   // disable the normal airborne X-lock so we don't drift early into the lip
                nav.CommittedLaunchVx = 0f;
                nav.ShaftEscapeDir = driftDir;
                action = "shaft-escape"; reason = $"up={curUp} drift={driftDir}";
                return true;
            }

            // Under an overhang lip (e.g. standing on the floor-bump in the wedge) → no room to rise here. STEP
            // SIDEWAYS to a column that has clearance (out from under the lip), then jump on a later frame. Prefer
            // the more-open side; ties go AWAY from the player, since we usually pin into the corner chasing them.
            int upAway = ColumnUpClear(col - driftDir, feetY);   // away from the player
            int upToward = ColumnUpClear(col + driftDir, feetY); // toward the player
            int stepDir = (upAway >= upToward) ? -driftDir : driftDir;
            npc.direction = stepDir; npc.spriteDirection = stepDir;
            ApplyChase(npc, stepDir, topSpeed, Math.Max(acceleration, AlignAccelFloor));
            action = "shaft-align"; reason = $"backup {stepDir} (under lip, curUp={curUp})";
            return true;
        }

        private static bool TryLocalTerrain(NavState nav, NPC npc, int direction, float jumpCeil,
            float boostCeil, float topSpeed, bool allowCliffDrop, out string action, out string reason)
        {
            int frontX = GetFrontTileX(npc, direction);
            int feetY = GetFeetTileY(npc);
            float maxLaunchVx = topSpeed + boostCeil;
            int oh = GetObstacleHeight(frontX, feetY);
            // oh == 1 (a single-tile step) is now handled by the smooth AutoStepUp glide during the walk,
            // so we only JUMP for obstacles 2+ tiles tall. This is why the enemy no longer mini-hops over
            // every little bump.
            if (oh > 1)
            {
                // Cap raised 4 -> 6 to match this NPC's jump power (MaxJumpPower 9 ≈ 8 tiles of height).
                // It was giving up ("too-tall") on walls it can actually clear; ComputeJumpArc still gates
                // feasibility, so an unmakeable jump falls through to the blocked brake below.
                // Skip when NoAStarPath: a wall the span graph can't route through won't be cleared
                // by a jump — just brake so the stuck clock can disengage to patrol.
                if (!nav.NoAStarPath && oh <= 6 && HasHeadroomForJump(npc, direction, oh))
                {
                    if (ComputeJumpArc(2, oh, npc.gravity, jumpCeil, maxLaunchVx, out float op, out float ovx))
                    {
                        FireJump(nav, npc, direction, op, ovx, 26, 30);
                        action = "obstacle-jump"; reason = $"h={oh} p{op:F1}/vx{ovx:F2}";
                        return true;
                    }
                }
                npc.velocity.X *= 0.4f;
                action = "blocked";

                // Diagnostic label only — tells the nav log which of the three gates above rejected the jump.
                if (oh > 6)
                {
                    reason = "too-tall";
                }
                else if (nav.NoAStarPath)
                {
                    reason = "no-astar-path";
                }
                else
                {
                    reason = "no-headroom";
                }

                return true;
            }
            int drop = GetDropDepth(frontX, feetY, 6);
            // allowCliffDrop = the no-plan chase set this because the player is BELOW with a real landing ahead. In
            // that case DROP IN rather than jumping ACROSS — otherwise the NPC vaults over a pit the player is sitting
            // in (e.g. a wide enemy skips the 3-wide pit). Only suppresses the gap-jump for the below-player case;
            // horizontal pursuit (allowCliffDrop false) still jumps gaps normally.
            string gapDiag = null;
            if (drop >= 2 && !allowCliffDrop)
            {
                bool gapFound = TryMeasureGap(frontX, feetY, direction, out int gap, out int landDrop, out int landX, out gapDiag);
                if (gapFound && gap == 1 && landDrop <= 1)
                {
                    // A single missing tile with a flush (or near-flush) landing needs no jump at all — normal
                    // walking momentum carries the body across it, same as a player strolling over a 1-block
                    // notch. Requiring gap>=2 here used to fall through every width-1 gap straight to the
                    // ledge self-catch below, freezing the NPC dead at the edge of the easiest gap there is.
                    // Just don't brake; let the caller's ApplyChase keep walking it forward.
                    action = ""; reason = ""; return false;
                }
                // Physics gates the gap now (not a hardcoded <=5 cap): jump only if makeable.
                if (gapFound && gap >= 1 && gap <= 7 && landDrop <= 2)
                {
                    if (ComputeJumpArc(gap, -landDrop, npc.gravity, jumpCeil, maxLaunchVx, out float gp, out float gvx))
                    {
                        FireJump(nav, npc, direction, gp, gvx, 26, 30);
                        action = "gap-jump"; reason = $"gap={gap},drop={landDrop} p{gp:F1}/vx{gvx:F2}";
                        return true;
                    }
                    // Not makeable — halt at the edge rather than committing to a fall.
                    npc.velocity.X *= 0.5f;
                    action = "gap-halt"; reason = $"infeasible gap={gap},drop={landDrop} g={npc.gravity:F2}";
                    return true;
                }
            }

            // Ledge self-catch: a steep drop is directly ahead with no makeable gap across it (the
            // gap block above didn't fire). A Walk step never legitimately descends a cliff — those
            // are Drop/JumpGap steps — so a steep drop here means residual momentum (typically from a
            // jump landing) is about to carry us off. Brake hard to catch ourselves at the edge.
            // Look one tile farther ahead when moving fast so a high landing speed can be bled off
            // before the body clears the edge. Shared by Walk steps and the no-plan chase.
            int lookahead = Math.Abs(npc.velocity.X) > 3f ? 2 : 1;
            int edgeDrop = GetDropDepth(frontX + direction * (lookahead - 1), feetY, 6);
            // allowCliffDrop: the no-plan chase sets this when the player is BELOW and a real landing
            // exists within reach — the drop IS the way down, so let the caller walk off the ledge
            // instead of braking here forever (the "two enemies frozen on a ledge above me" exploit).
            if ((drop >= 4 || edgeDrop >= 4) && !allowCliffDrop)
            {
                npc.velocity.X *= 0.3f;
                if (Math.Abs(npc.velocity.X) < 1f) npc.velocity.X = 0f;
                action = "cliff-halt";
                reason = gapDiag != null ? $"drop={drop} edge={edgeDrop} la={lookahead} gapscan=[{gapDiag}]"
                                          : $"drop={drop} edge={edgeDrop} la={lookahead}";
                return true;
            }
            action = ""; reason = "";
            return false;
        }

        private static bool HasOneTileStepAhead(NPC npc, int direction)
        {
            int frontX = GetFrontTileX(npc, direction);
            int feetY = GetFeetTileY(npc);
            return GetObstacleHeight(frontX, feetY) == 1;
        }

        private static bool TryDoor(NPC npc, int direction, int doorDamage, tsorcRevampGlobalNPC globalNPC,
            out string action, out string reason)
        {
            int frontX = GetFrontTileX(npc, direction);
            int feetY = GetFeetTileY(npc);
            if (!TryFindClosedDoor(frontX, feetY, out int dx, out int dy))
            {
                action = ""; reason = ""; return false;
            }
            int openY = GetDoorOpenY(dx, dy);
            if (Main.netMode != NetmodeID.MultiplayerClient && WorldGen.OpenDoor(dx, openY, direction))
            {
                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.ToggleDoorState, -1, -1, null, 0, dx, openY, direction);
                npc.velocity.X += 0.65f * direction;
                action = "door-open"; reason = $"({dx},{dy})";
                return true;
            }
            if (Main.GameUpdateCount % 30 == 0)
            {
                globalNPC.DoorBreakProgress += doorDamage;
                WorldGen.KillTile(dx, dy, true, true);
                if (globalNPC.DoorBreakProgress >= 10)
                {
                    globalNPC.DoorBreakProgress = 0;
                    WorldGen.OpenDoor(dx, openY, direction);
                }
            }
            npc.velocity.X = 0.2f * -direction;
            action = "door-break"; reason = $"({dx},{dy})";
            return true;
        }

        // ================================================================================
        //  Movement primitives
        // ================================================================================

        private static void FireJump(NavState nav, NPC npc, int direction, float jumpPower, float launchVx,
            int airCommitFrames, int planCommitFrames)
        {
            npc.velocity.Y = -jumpPower;
            // SET (not +=) the exact computed horizontal speed. The airborne handler re-asserts
            // CommittedLaunchVx every frame so this magnitude survives the whole arc.
            npc.velocity.X = launchVx * direction;
            npc.direction = direction;
            npc.spriteDirection = direction;
            nav.AirCommitDirX = direction;
            nav.AirCommitTimer = airCommitFrames;
            nav.CommitFrames = planCommitFrames;
            nav.CommittedLaunchVx = launchVx * direction;
            npc.netUpdate = true;
        }

        private static void ApplyChase(NPC npc, int direction, float topSpeed, float acceleration)
        {
            float t = topSpeed * direction;
            if (npc.velocity.X < t)
            {
                npc.velocity.X += acceleration;
                if (npc.velocity.X > t) npc.velocity.X = t;
            }
            else if (npc.velocity.X > t)
            {
                npc.velocity.X -= acceleration;
                if (npc.velocity.X < t) npc.velocity.X = t;
            }
        }

        private static void ApplyGroundedOverspeedBrake(NPC npc, float topSpeed, float brakingPower)
        {
            if (npc.velocity.Y != 0f) return;

            float speed = Math.Abs(npc.velocity.X);
            if (speed <= topSpeed) return;

            float sign = Math.Sign(npc.velocity.X);
            float excess = speed - topSpeed;

            // Old fighterAI multiplied grounded overspeed by 0.8. Keep that tight feel, but
            // also guarantee at least brakingPower worth of excess is removed each frame.
            float reducedByMultiplier = speed * 0.8f;
            float reducedByBrake = speed - Math.Max(brakingPower, 0.05f);
            float targetSpeed = Math.Max(topSpeed, Math.Min(reducedByMultiplier, reducedByBrake));

            // If only a tiny bit of excess remains, snap to walk speed instead of wobbling.
            if (targetSpeed - topSpeed < Math.Max(0.05f, excess * 0.15f))
            {
                targetSpeed = topSpeed;
            }

            npc.velocity.X = sign * targetSpeed;
        }

        // ================================================================================
        //  Terrain primitives
        // ================================================================================

        private static bool IsGrounded(NPC npc)
        {
            if (npc.velocity.Y != 0f) return false;
            // Physical fallback: the engine reports a vertical collision and we're not moving, so
            // we're resting on SOMETHING — including furniture/tables/slopes that the tile-type
            // check below ignores (IsNavigationSolid excludes tileFrameImportant). Without this the
            // NPC freezes "airborne" on a table forever (the stuck-on-table bug from the log).
            if (npc.collideY) return true;
            int left = (int)(npc.Left.X / TileF);
            int right = (int)((npc.Right.X - 1f) / TileF);
            int belowFeet = (int)((npc.Bottom.Y + 4f) / TileF);
            for (int x = left; x <= right; x++)
                if (IsStandableTile(x, belowFeet)) return true;
            return false;
        }

        private static bool IsStandableTile(int x, int y)
        {
            if (!WorldGen.InWorld(x, y)) return false;
            Tile t = Main.tile[x, y];
            if (!t.HasTile || t.IsActuated) return false;
            if (!(IsNavigationSolid(x, y) || IsPlatformTile(x, y))) return false;
            // Beast flat-ground gate: a large enemy (MinSurfaceWidth > 0) may only treat this as standable if its
            // full footprint sits CENTERED on a flat run that wide — so it stands fully on, never edge-perched, and
            // refuses floors narrower than its footprint. _maxSurfaceStep tiles of unevenness are tolerated (the
            // Deerclops-clip failsafe: a single foot may dip one tile rather than the whole surface being refused).
            // (IsGrounded still detects a beast resting on a too-narrow perch via its npc.collideY physical fallback.)
            if (_minSurfaceWidth > 0 && !UsefulFunctions.IsFootprintSupported(x, y, _minSurfaceWidth, _maxSurfaceStep)) return false;
            return true;
        }

        private static int GetFrontTileX(NPC npc, int direction) =>
            direction > 0 ? (int)((npc.Right.X + 4f) / TileF) : (int)((npc.Left.X - 4f) / TileF);

        private static int GetFeetTileY(NPC npc) => (int)((npc.Bottom.Y - 1f) / TileF);

        // AutoStepUp moved to UsefulFunctions (tsorcRevampUtils.cs) so NavBehavior can share it without
        // calling back up into SF4. Beast-gated variant still lives in tsorcRevampAIs.AutoStepUp.

        // Height of the obstacle directly ahead, measured as the CONTIGUOUS solid column starting at the
        // feet row and going up. A solid tile separated from the floor by open space (e.g. the ceiling of
        // a hallway / castle gate) is NOT a walking obstacle and must be ignored — the old top-down scan
        // returned the ceiling height, which made the NPC "obstacle-jump" through flat tunnels and stall
        // at gate mouths with a bogus "no-headroom" block.
        private static int GetObstacleHeight(int frontX, int feetY)
        {
            if (!IsNavigationSolid(frontX, feetY)) return 0; // open at foot level -> nothing to climb
            int h = 1;
            while (h <= 6 && IsNavigationSolid(frontX, feetY - h)) h++;
            return h; // contiguous solid height in front, from the floor up
        }

        private static bool HasHeadroomForJump(NPC npc, int direction, int obstacleHeight)
        {
            int frontX = GetFrontTileX(npc, direction);
            int headY = (int)((npc.Top.Y + 4f) / TileF);
            int feetY = GetFeetTileY(npc);
            // Require ~1 tile of clearance above the head (was 2). The extra tile was over-conservative: standing on a
            // 1-tile bump raises headY by one, which pushed a fixed overhang into the old 2-tile window and made the
            // NPC refuse to hop a small ledge out of a pit (the "no-headroom only on the bumped/left side" stuck case),
            // while the same ledge from flat ground (one tile lower) cleared fine.
            int highest = Math.Max(headY - 1, feetY - obstacleHeight - 3);
            for (int y = highest; y <= headY; y++)
                if (IsNavigationSolid(frontX, y)) return false;
            return true;
        }

        private static bool IsCliffAhead(NPC npc, int direction)
        {
            int frontX = GetFrontTileX(npc, direction);
            int feetY = GetFeetTileY(npc);
            int drop = GetDropDepth(frontX, feetY, 5);
            return drop >= 4;
        }

        private static int GetDropDepth(int x, int feetY, int maxDepth)
        {
            for (int d = 0; d <= maxDepth; d++)
                if (IsStandableTile(x, feetY + d)) return d;
            return maxDepth + 1;
        }

        private static bool TryMeasureGap(int frontX, int feetY, int direction,
            out int gapTiles, out int landingDrop, out int landingX, out string diag)
        {
            gapTiles = 0; landingDrop = 0; landingX = frontX;
            // Diagnostic trail so a cliff-halt caused by this scan finding NOTHING within 7 tiles can be
            // told apart (in the log) from one where a landing existed but got rejected downstream — every
            // offset's (drop, clearance) is recorded regardless of pass/fail. Cheap: at most 7 iterations.
            var sb = new System.Text.StringBuilder();
            for (int o = 1; o <= 7; o++)
            {
                int cx = frontX + direction * o;
                int d = GetDropDepth(cx, feetY, 5);
                // HasBodyClearanceAtRow expects the row ABOVE the landing floor (see its other call sites
                // at BuildSpans/867 and 1114: "IsStandableTile(x, y+1) && HasBodyClearanceAtRow(x, y)").
                // GetDropDepth/IsStandableTile's own convention makes feetY+d the FLOOR tile's row itself,
                // so passing that straight through made the clearance scan include the solid landing tile
                // as its own top row — genuine solid ground (anything that isn't a jump-through platform)
                // then always "failed clearance" no matter how much headroom actually existed. This is why
                // gap-jump/gap-halt never fired for any gap width: -1 aligns it to the body row above the
                // floor, matching every other clearance check in this file.
                bool clear = d <= 2 && HasBodyClearanceAtRow(cx, feetY + d - 1);
                sb.Append(o).Append(':').Append(d).Append(clear ? "c" : "x").Append(' ');
                if (clear && gapTiles == 0) { gapTiles = o; landingDrop = d; landingX = cx; }
            }
            diag = sb.ToString().TrimEnd();
            return gapTiles > 0;
        }

        private static bool HasBodyClearanceAtRow(int x, int feetY)
        {
            // _clearanceHeight rows of headroom (3 by default; a beast's full sprite height) measured up from feet.
            int rows = _clearanceHeight > 0 ? _clearanceHeight : 3;
            for (int y = feetY - (rows - 1); y <= feetY; y++)
                if (IsNavigationSolid(x, y)) return false;
            return true;
        }

        // Ghost-only launch viability for a near-vertical RISING jump (see TryFindJumpEdge): is the body free to
        // rise off launch column lx without a wall pinning it from behind? The body is wider than one tile, so a
        // solid tile in the TRAILING column (the side opposite the landing) at body height jams it against that wall
        // mid-rise — the launch never gets off the ground. landX/lx pick the trailing side; a pure-vertical jump
        // (landX == lx) has no horizontal escape from EITHER side, so both neighbors must be clear. Checks the body's
        // full height (3 rows up from the feet row) where the wedge actually happens; higher tiles are the lip the
        // arc clears, not a launch obstruction.
        private static bool LaunchTrailClear(int lx, int landX, int feetY)
        {
            int landDir = Math.Sign(landX - lx);
            const int rows = 3; // body footprint height in tiles
            for (int y = feetY; y > feetY - rows; y--)
            {
                if (landDir == 0)
                {
                    if (IsNavigationSolid(lx - 1, y) || IsNavigationSolid(lx + 1, y)) return false;
                }
                else if (IsNavigationSolid(lx - landDir, y)) return false; // trailing side = opposite the landing
            }
            return true;
        }

        // Try several launch/land column pairs to find one with a clear trajectory.
        // Critical fix vs. the old single-column attempt: a pit's overhead ceiling
        // blocks any jump launched directly under it, but a launch one tile away
        // (where the ceiling doesn't extend) may succeed.
        private static bool TryFindJumpEdge(Span a, Span b, int dy, out int launchX, out int landX, out int absDx)
        {
            // Build candidate launch columns within span A.
            // Bias toward the edge closest to span B but try the whole span if needed.
            List<int> launches = new List<int>();
            if (b.LeftX > a.RightX)
            {
                // B is to the right of A â€” prefer launching from A's right edge inward.
                for (int x = a.RightX; x >= a.LeftX && launches.Count < 6; x--) launches.Add(x);
            }
            else if (b.RightX < a.LeftX)
            {
                for (int x = a.LeftX; x <= a.RightX && launches.Count < 6; x++) launches.Add(x);
            }
            else
            {
                // Overlap â€” start in the middle of overlap, fan out.
                int oMin = Math.Max(a.LeftX, b.LeftX);
                int oMax = Math.Min(a.RightX, b.RightX);
                int mid = (oMin + oMax) / 2;
                launches.Add(mid);
                for (int o = 1; o <= 4 && launches.Count < 6; o++)
                {
                    if (mid + o <= oMax) launches.Add(mid + o);
                    if (mid - o >= oMin) launches.Add(mid - o);
                }
            }

            foreach (int lx in launches)
            {
                // Generate candidate landing columns within span B.
                List<int> lands = new List<int>();
                if (b.LeftX > a.RightX)
                {
                    for (int x = b.LeftX; x <= b.RightX && lands.Count < 6; x++) lands.Add(x);
                }
                else if (b.RightX < a.LeftX)
                {
                    for (int x = b.RightX; x >= b.LeftX && lands.Count < 6; x--) lands.Add(x);
                }
                else
                {
                    int oMin = Math.Max(a.LeftX, b.LeftX);
                    int oMax = Math.Min(a.RightX, b.RightX);
                    // Climbing onto a higher overlapping span (dy > 0). A PURE-VERTICAL jump (straight up,
                    // zero horizontal momentum) is the cleanest, most reliable climb and the maneuver the
                    // executor does best — but it only works when the destination floor directly overhead is
                    // a JUMP-THROUGH surface (wood platform or solid-top furniture like a table): the body
                    // phases straight up through it and lands on top. When that's the case, prefer vertical —
                    // this restores the "stop, jump straight up, then walk" behavior (the diagonal-only
                    // preference made every climb a dx=1 up-and-over that drifted sideways and undershot tall
                    // arcs). When a SOLID block sits overhead instead, a vertical jump just drives the head
                    // into its underside and wedges at the launch column, so there we keep the up-and-OVER
                    // offset arc that clears the lip and lands on top from the side. (Floor of span b at the
                    // launch column is row b.Y + 1 — spans store the FEET row, floor one below.)
                    if (dy > 0)
                    {
                        bool cleanVertical = IsPlatformTile(lx, b.Y + 1);
                        if (cleanVertical) lands.Add(lx);            // straight-up through a platform first
                        // Solid lip: PREFER a flatter up-and-over (adx=2) — launching 2 tiles back, the arc rises over
                        // the pit and clears the lip's vertical FACE, instead of a steep adx=1 arc that drifts into the
                        // lip base and wedges (cx=True). Fall back to adx=1, then a last-resort vertical.
                        if (lx + 2 <= oMax) lands.Add(lx + 2);
                        if (lx - 2 >= oMin) lands.Add(lx - 2);
                        if (lx + 1 <= oMax) lands.Add(lx + 1);
                        if (lx - 1 >= oMin) lands.Add(lx - 1);
                        if (!cleanVertical) lands.Add(lx);           // vertical only as last resort over solid
                    }
                    else
                    {
                        lands.Add(lx); // same column = pure vertical
                        if (lx + 1 <= oMax) lands.Add(lx + 1);
                        if (lx - 1 >= oMin) lands.Add(lx - 1);
                    }
                }

                foreach (int la in lands)
                {
                    int adx = Math.Abs(la - lx);
                    // SF4: propose the edge only if this NPC can physically make the arc
                    // (gravity + jump/boost limits). Replaces the static-table membership test,
                    // so flat wide gaps (no table entry) are now correctly considered.
                    if (!ComputeJumpArc(adx, dy, _planGravity, _planJumpCeil, _planMaxLaunchVx, out _, out _)) continue;
                    // Pit-wedge guard: a near-vertical RISING jump launched right against a wall on its TRAILING side
                    // (the side opposite the landing) pins the wider-than-a-tile body to that wall every frame — it
                    // brushes collideX, rises ~half a tile, falls back, and never completes, so A* re-picks the same
                    // edge forever (the "wedged in a pit, jumps straight into the lip" loop). Refuse the wedging launch
                    // so A* picks one backed off the wall (clear trailing side) that arcs UP-AND-OVER the lip instead —
                    // i.e. walk back 1-2 tiles, then jump out. Now applies to ALL enemies (was ghost-only): non-ghosts
                    // wedge in pits the same way (e.g. the Dworc that hopped straight into the lip on the bumped side).
                    if (dy >= 1 && adx <= 1 && !LaunchTrailClear(lx, la, a.Y)) continue;
                    if (!HasTrajectoryClearance(lx, a.Y, la, b.Y, dy)) continue;
                    launchX = lx; landX = la; absDx = adx;
                    return true;
                }
            }
            launchX = 0; landX = 0; absDx = 0;
            return false;
        }

        // Trajectory-shaped clearance â€” only checks the actual jump arc, not a
        // bounding box. Crucially does NOT require the landing column above the
        // destination floor to be clear (that's where the NPC lands).
        private static bool HasTrajectoryClearance(int launchX, int fromY, int landX, int toY, int dy)
        {
            // Body needs 3 rows above the standing row. The trajectory peaks at apex
            // somewhere between fromY and toY. We approximate by checking:
            //  - launch column: clear above standing row up to apex
            //  - land column:   clear above standing row at land Y (just body fits)
            //  - intermediate:  clear at the higher (apex-ish) Y so body passes through
            int higherY = Math.Min(fromY, toY); // higher in world = smaller Y
            int apexY = higherY - Math.Max(2, Math.Abs(dy));
            int absDx = Math.Abs(landX - launchX);

            // 1) Launch column: from launch row body up to apex
            for (int y = apexY; y <= fromY - 1; y++)
                if (IsNavigationSolid(launchX, y)) return false;

            // 2) Land column: clear body space at land (3 rows above floor)
            for (int y = toY - 2; y <= toY; y++)
                if (IsNavigationSolid(landX, y)) return false;

            // 3) Intermediate columns: NPC body passes through at apex level
            if (absDx > 0)
            {
                int x0 = Math.Min(launchX, landX), x1 = Math.Max(launchX, landX);
                for (int x = x0 + 1; x < x1; x++)
                {
                    // Body is 3 rows tall â€” check 3 rows centered at apex region.
                    for (int y = apexY; y <= apexY + 2; y++)
                        if (IsNavigationSolid(x, y)) return false;
                }
            }
            return true;
        }

        private static bool HasDropClearance(int x, int fromY, int toY)
        {
            for (int y = fromY; y <= toY; y++)
                if (IsNavigationSolid(x, y)) return false;
            return true;
        }

        private static bool IsSpanOnPlatformOnly(Span span)
        {
            for (int x = span.LeftX; x <= span.RightX; x++)
            {
                if (!IsPlatformTile(x, span.Y + 1))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsPlatformTile(int x, int y)
        {
            if (!WorldGen.InWorld(x, y)) return false;
            Tile t = Main.tile[x, y];
            if (!t.HasTile || t.IsActuated) return false;
            // A "platform" for navigation = anything solid only on its top: wood platforms AND solid-top
            // furniture (tables, fireplaces, work benches, etc.). Gameplay-wise these can all be stood on,
            // jumped up through, and dropped down through — but they aren't in TileID.Sets.Platforms and are
            // tileFrameImportant, so IsNavigationSolid rejects them. Without this the planner saw a table as
            // neither solid nor platform: NPCs couldn't path onto one and stood frozen on top (the stuck-on-
            // table report), and couldn't drop through one to reach a lower target.
            return TileID.Sets.Platforms[t.TileType] || Main.tileSolidTop[t.TileType];
        }

        private static bool IsNavigationSolid(int x, int y)
        {
            if (!WorldGen.InWorld(x, y)) return false;
            Tile t = Main.tile[x, y];
            if (!t.HasTile || t.IsActuated || TileID.Sets.Platforms[t.TileType]) return false;
            if (!Main.tileSolid[t.TileType]) return false;
            return !Main.tileFrameImportant[t.TileType] || t.TileType == TileID.ClosedDoor;
        }

        private static bool TryFindClosedDoor(int frontX, int feetY, out int doorX, out int doorY)
        {
            for (int xOff = -2; xOff <= 2; xOff++)
            {
                int x = frontX + xOff;
                for (int y = feetY - 7; y <= feetY + 2; y++)
                {
                    if (WorldGen.InWorld(x, y) && Main.tile[x, y].HasTile && Main.tile[x, y].TileType == TileID.ClosedDoor)
                    {
                        doorX = x; doorY = y; return true;
                    }
                }
            }
            doorX = 0; doorY = 0; return false;
        }

        private static int GetDoorOpenY(int doorX, int doorY)
        {
            int y = doorY;
            while (WorldGen.InWorld(doorX, y + 1) && Main.tile[doorX, y + 1].HasTile
                && Main.tile[doorX, y + 1].TileType == TileID.ClosedDoor) y++;
            return y;
        }

        // ================================================================================
        //  Logging
        // ================================================================================

        /// <summary>
        /// Shared FSM/navigation diagnostic block, appended to every nav log line by BOTH loggers so the
        /// two files describe the same thing. Answers, in order:
        ///  fsm=  entryState:entryDisengage -> exitState:exitDisengage @branch, and `age` = frames since
        ///        UpdateState last ran (age>0 on a movement frame means the FSM did NOT tick — the state
        ///        being acted on is stale). If the LIVE pursuit=/disengage= fields disagree with the exit
        ///        values, something overwrote the FSM's decision after it ran.
        ///  mut=  the last ForceDisengage/EnterPatrol, as entryPoint&lt;-callerMember:line, plus its age.
        ///        This is the direct answer to "who keeps zeroing DisengageTimer".
        ///  gate= the SF4 STATIC per-frame stash (minSurfaceWidth / maxSurfaceStep / clearanceHeight).
        ///        These are shared across all NPCs and refilled at the top of Run; if a Dworc ever logs a
        ///        beast's values (msw&gt;0, ch&gt;3) they leaked from another NPC and every ordinary tile is
        ///        being rejected as unstandable.
        ///  unreach=/beast= the give-up counters that decide disengage-vs-persist.
        /// </summary>
        internal static string DiagnosticBlock(NPC npc, tsorcRevampGlobalNPC globalNPC)
        {
            // Self-contained failure: a diagnostic must never be able to take down the line it annotates.
            // The callers' outer try/catch is silent, so without this a throw in here would look exactly
            // like "the logger never ran" — which is an hour of debugging the debugger.
            try
            {
                int now = (int)Main.GameUpdateCount;
                string fsmAge = globalNPC.FsmTick < 0 ? "never" : (now - globalNPC.FsmTick).ToString();
                string mutAge = globalNPC.FsmMutationTick < 0 ? "never" : (now - globalNPC.FsmMutationTick).ToString();
                bool clobbered = globalNPC.FsmTick >= 0
                    && (globalNPC.PursuitState != globalNPC.FsmExitState || globalNPC.DisengageTimer != globalNPC.FsmExitDisengage);
                return $" fsm={globalNPC.FsmEntryState}:{globalNPC.FsmEntryDisengage}->{globalNPC.FsmExitState}:{globalNPC.FsmExitDisengage}"
                    + $"@{globalNPC.FsmBranch} age={fsmAge}{(clobbered ? " CLOBBERED" : "")}"
                    + $" mut={globalNPC.FsmMutationBy} mutAge={mutAge}"
                    + $" gate=msw{_minSurfaceWidth}/mss{_maxSurfaceStep}/ch{_clearanceHeight}"
                    + $" unreach={globalNPC.UnreachableFrames} beast={globalNPC.BeastStale}/{globalNPC.BeastUnreachableFrames}"
                    + $" tpApp={globalNPC.TeleportAppearanceTimer} stag={globalNPC.StaggerTimer}";
            }
            catch (Exception e)
            {
                return $" fsm=<diag-failed:{e.GetType().Name}>";
            }
        }

        /// <summary>
        /// Emitted once per session on the first line written to a nav log, so an EMPTY log is
        /// unambiguous: header present + no lines = the loggers ran but every enemy was filtered out;
        /// no header at all = the logging path was never reached (no SF4 enemy engaged, or the AI
        /// never got that far). Without this, "no file" has half a dozen indistinguishable causes.
        /// </summary>
        private static bool _navLogHeaderWritten;
        internal static void WriteNavLogHeader(string path)
        {
            if (_navLogHeaderWritten) return;
            _navLogHeaderWritten = true;
            try
            {
                File.AppendAllText(path,
                    $"[{DateTime.Now:HH:mm:ss}] ==== nav log opened (tick {Main.GameUpdateCount}, "
                    + $"DebugMode={ModContent.GetInstance<tsorcRevampConfig>().DebugMode}) ===="
                    + Environment.NewLine);
            }
            catch { }
        }

        private static void LogFrame(NPC npc, Player player, NavState nav, bool grounded, bool los,
            bool attack, string action, string reason)
        {
            tsorcRevampGlobalNPC globalNPC = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();
            int now = (int)Main.GameUpdateCount;
            // Sample finer while on/working a rope so fast vibration & phasing are actually captured between lines.
            bool ropeContext = npc.noGravity
                || (nav.Plan != null && nav.PlanIndex < nav.Plan.Count && nav.Plan[nav.PlanIndex].Kind == StepKind.RopeClimb);
            int interval = ropeContext ? 3 : 12;
            // Per-NPC, not global: several SF4 enemies alive must each get their own sample cadence.
            if (now - nav.LastLogTick < interval) return;
            nav.LastLogTick = now;
            try
            {
                string sep = Path.DirectorySeparatorChar.ToString();
                string dir = Main.SavePath + sep + "Logs";
                Directory.CreateDirectory(dir);
                string path = dir + sep + "tsorcRevamp-smartfighter4.log";
                WriteNavLogHeader(path);
                string planStr = "none";
                if (nav.Plan != null)
                {
                    planStr = $"{nav.PlanIndex}/{nav.Plan.Count}";
                    if (nav.PlanIndex < nav.Plan.Count)
                    {
                        var st = nav.Plan[nav.PlanIndex];
                        planStr += $" {st.Kind}->{st.TargetX},{st.TargetY}@launch{st.LaunchX}";
                    }
                }

                // Rope diagnostics: the exact tile state around the NPC at the rope column, so we can see WHY it
                // vibrates / phases. Key tells:
                //  - solidU2/solidU3 = a solid (non-rope) block at head height while still climbing (noGravity) =>
                //    it's phasing the body up into blocks above the rope.
                //  - ropeU1/ropeHead/ropeD1 = where the rope actually ends relative to the feet.
                //  - eng/stall/lastFeetY + the feetY trend across lines = whether the ride is progressing or
                //    flip-flopping (dismount->regrab). atEnd/reached show which dismount branch is about to fire.
                string ropeStr = "";
                if (nav.Plan != null && nav.PlanIndex < nav.Plan.Count && nav.Plan[nav.PlanIndex].Kind == StepKind.RopeClimb)
                {
                    var rs = nav.Plan[nav.PlanIndex];
                    int lx = rs.LaunchX;
                    int feetY = GetFeetTileY(npc);
                    bool descend = nav.RopeDirLatched ? nav.RopeDescend : rs.TargetY > feetY;
                    bool atEnd = nav.RopeEngaged && (descend ? !IsRopeTile(lx, feetY + 1) : !IsRopeTile(lx, feetY - 1));
                    bool reached = descend ? feetY >= rs.TargetY : feetY <= rs.TargetY;
                    ropeStr = $" [rope {(descend ? "DN" : "UP")} toY={rs.TargetY} feetY={feetY} lastFeetY={nav.LastRopeFeetY}"
                        + $" ropeU1={IsRopeTile(lx, feetY - 1)} ropeHead={IsRopeTile(lx, feetY - 2)} ropeD1={IsRopeTile(lx, feetY + 1)}"
                        + $" solidU2={IsNavigationSolid(lx, feetY - 2)} solidU3={IsNavigationSolid(lx, feetY - 3)}"
                        + $" eng={nav.RopeEngaged} latch={nav.RopeDirLatched} dismount={nav.RopeDismounting}"
                        + $" stall={nav.RopeStallFrames} atEnd={atEnd} reached={reached}]";
                }
                int facePlayer = player.Center.X >= npc.Center.X ? 1 : -1;
                bool wrongAimFace = npc.ai[2] != 0f && npc.spriteDirection != facePlayer;
                string combatStr = "";
                if (globalNPC.CombatTempo != null || globalNPC.KiteRangeMax > 0f || globalNPC.BlockedRecently > 0)
                {
                    string meleeState = globalNPC.CombatMeleeActive
                        ? $"{globalNPC.ActiveCombatMeleeKey}@{globalNPC.ActiveCombatMeleeTimer}/t{globalNPC.ActiveCombatMeleeTelegraphTicks}"
                        : "off";
                    combatStr = $" ai1={npc.ai[1]:F0} melee={meleeState}"
                        + $" pending={globalNPC.PendingCombatMoveKey}@{globalNPC.PendingCombatMoveGapTimer} combo={globalNPC.CombatComboStep}/{globalNPC.CombatComboMaximum}/rec{globalNPC.CombatComboRecoveryTimer}"
                        + $" guard={globalNPC.BlockedRecently}/{tsorcRevampGlobalNPC.GuardPressureMaxBlocks}/unblock{globalNPC.ActiveAttackBypassesShield}"
                        + $" kite={globalNPC.KiteRangeMin:F1}-{globalNPC.KiteRangeMax:F1}/threat{globalNPC.KiteRangedThreatTimer}"
                        + $"/advance{globalNPC.IsAdvanceAndShootActive(npc, player)}"
                        + $"/movingFire{globalNPC.CanUseMovingFireDuringAdvance(npc, player)}";
                }
                string line = $"[{DateTime.Now:HH:mm:ss}] {npc.TypeName}#{npc.whoAmI}"
                    + $" pos=({npc.Center.X / TileF:F1},{npc.Center.Y / TileF:F1})"
                    + $" player=({player.Center.X / TileF:F1},{player.Center.Y / TileF:F1})"
                    + $" vel=({npc.velocity.X:F2},{npc.velocity.Y:F2})"
                    + $" dir={npc.direction} spr={npc.spriteDirection} faceP={facePlayer}"
                    + $" ai2={npc.ai[2]:F1} frameY={npc.frame.Y} wrongAimFace={wrongAimFace}"
                    + $" g={grounded} cx={npc.collideX} cy={npc.collideY}"
                    + $" pass=({npc.noTileCollide},{npc.noGravity}) los={los}"
                    + $" plan={planStr}"
                    + $" stepT={nav.StepTimer} commit={nav.CommitFrames} air={nav.AirCommitDirX}/{nav.AirCommitTimer}"
                    + $" rcd={nav.ReplanCooldown} pdrop={nav.PlatformPassActive}/{nav.PlatformPassTimer}"
                    + $" plan=\"{nav.LastPlanResult}\""
                    // The A* verdict on its own, with its age in frames — LastPlanResult is clobbered by
                    // the rope fallback within the same frame and never showed why pathing actually failed.
                    + $" astar=\"{nav.LastReplanResult}\"@{(nav.LastReplanTick < 0 ? "never" : (now - nav.LastReplanTick).ToString())}"
                    + $" noPath={nav.NoAStarPath} giveup={nav.StuckGiveUpFrames} hard={nav.HardStuckStrikes}/{nav.HardStuckFrames}"
                    + $" pursuit={globalNPC.PursuitState}"
                    + $" disengage={globalNPC.DisengageTimer}/{globalNPC.NavGiveUpTicks}"
                    + DiagnosticBlock(npc, globalNPC)
                    + $" patrol={globalNPC.PatrolMode}/idle{globalNPC.PatrolIdleTimer}/leg{globalNPC.PatrolLegRemaining}/dir{globalNPC.PatrolDirection}/elapsed{globalNPC.PatrolElapsed}"
                    // Combat-layer state folded in from tsorcRevamp-nav.log so the smart log is a single superset
                    // for SF4 enemies: teleport (style/charges/cooldown/countdown) + stop-to-fire intent.
                    + $" tp={(globalNPC.CanTeleport ? $"{globalNPC.TeleportStyle}/ch{(globalNPC.TeleportChargesRemaining == int.MaxValue ? "inf" : globalNPC.TeleportChargesRemaining.ToString())}/cd{globalNPC.TeleportCooldownTimer}/cnt{globalNPC.TeleportCountdown}" : "off")}"
                    + $" stopFire={globalNPC.CanStopToFire}"
                    + $" action={action} reason={reason} attack={attack}"
                    + combatStr
                    + ropeStr;
                File.AppendAllText(path, line + Environment.NewLine);
            }
            catch (Exception e)
            {
                // Never silent again: a bare `catch {}` here means a throw anywhere in the line build
                // produces an EMPTY log, indistinguishable from "the logger never ran".
                try
                {
                    File.AppendAllText(
                        Main.SavePath + Path.DirectorySeparatorChar + "Logs"
                            + Path.DirectorySeparatorChar + "tsorcRevamp-smartfighter4.log",
                        $"[{DateTime.Now:HH:mm:ss}] {npc.TypeName}#{npc.whoAmI} LOG-FAILED "
                            + $"{e.GetType().Name}: {e.Message}" + Environment.NewLine);
                }
                catch { }
            }
        }

        private static string DescribePlan(NavState nav)
        {
            if (nav.Plan == null)
                return "none";
            string value = $"{nav.PlanIndex}/{nav.Plan.Count}";
            if (nav.PlanIndex < nav.Plan.Count)
            {
                PlanStep step = nav.Plan[nav.PlanIndex];
                value += $" {step.Kind}->{step.TargetX},{step.TargetY}@launch{step.LaunchX}";
            }
            return value;
        }

        // ================================================================================
        //  Types
        // ================================================================================

        private class NavState
        {
            public string LastAction = "";
            public string LastReason = "";
            public string LastTelemetryPlan = "none";
            public List<PlanStep> Plan;
            public int PlanIndex;
            public int StepTimer;
            public int ReplanCooldown;
            // Patrol/Pursue FSM support: last distance to the player (for progress detection) and a
            // LOS-independent "can't reach" counter that trips ForceDisengage so a walled-off NPC
            // patrols instead of pressing forever (the "stands at the wall" bug).
            public float LastPursuitDist = float.MaxValue;
            public int StuckGiveUpFrames;
            public float StuckCheckX = float.MaxValue; // NPC X at the last anti-stuck checkpoint
            // Hard-stuck catch-all (Fix C): raw X-immobility while trying to move, independent of canEngage/LOS, so a
            // wedged-but-"engaged" NPC (no-headroom pit with the player adjacent) still escapes via blink/disengage.
            public int HardStuckFrames;
            public float HardStuckCheckX = float.MaxValue;
            public int HardStuckStrikes;   // consecutive ~2s windows with no real net progress (Fix C v3)
            // Ranged kiting (TryRangedKite): drifting target distance, hold-drift direction,
            // re-roll timer, and a per-window "let melee close".
            public float KiteTargetDist;
            public int KiteRerollTimer;
            public bool KiteLetClose;
            public int KiteHoldDrift;
            // Large-beast positioner (TryBeastReposition): post-touch retreat countdown so it bobs out of melee
            // instead of grinding centered on the player (the sprite-glitch / "trapped on top of you" case).
            public int BeastContactBackoff;
            // Phase 2: a beast is mid-phase through a thin (<= BeastPhaseMaxWidth) obstruction (noTileCollide + floor-snap).
            public bool BeastPhasing;
            public float BeastPhaseFloorY;
            public int BeastPhaseDir;    // latched travel direction through the obstruction (so it can't flip mid-phase)
            public int BeastPhaseFrames; // safety cap so a phase can't get stuck on forever
            // Rope-fallback retry memory: detects the on/off loop where TryGrabNearbyRope re-grabs the
            // same rope that can't actually reach the player. After 2 rapid retries the rope is bad-edged.
            public int RopeFallbackX = int.MinValue;
            public int RopeFallbackTick;
            public int RopeFallbackFails;
            // Frames spent pressing a wall while trying to reach a jump launch column it can't get to
            // (A* picked a launch behind a wall). Trips an early step abort so the NPC doesn't face
            // away from the player, pinned at the wall, for the whole StepTimer. Position-keyed via
            // AlignLastX (collideX flickers off against a wall and never reached the abort).
            public int AlignStallFrames;
            public float AlignLastX = float.MaxValue;
            // Plan-execution immobility (survives replans): a wedged Walk/align step whose StepTimer keeps
            // getting reset by replans (the player twitching → A* rebuilds) never times out — the "stuck
            // till hit" freeze. These track raw X immobility WHILE a movement step executes; they reset
            // ONLY on real X movement, so a replan re-issuing the same wedged step keeps accumulating until
            // it bad-edges the step target and forces a reroute. MoveCheckX = X at the last progress mark.
            public float MoveCheckX = float.MaxValue;
            public int StepNoMoveFrames;
            public int AirCommitTimer;
            public int AirCommitDirX;
            // Shaft/pit escape (TryShaftEscape): we launched STRAIGHT UP out of a pit and must rise vertically until
            // above the overhang lip, THEN drift this direction onto the ledge. 0 = not escaping. Cleared on landing.
            public int ShaftEscapeDir;
            // Signed horizontal launch velocity computed by ComputeJumpArc, re-asserted each
            // airborne frame so the physics arc isn't eroded by chase steering. 0 = use fallback.
            public float CommittedLaunchVx;
            // CommitFrames > 0 means an action is in flight; replan is locked.
            public int CommitFrames;
            // True once this jump step has fired its arc. If we land grounded again with this still set
            // (and the step hasn't completed), the jump physically missed its target → reroute after the
            // ONE attempt instead of re-aligning and re-firing for the full StepTimer (the ~3s shuffle).
            public bool JumpFiredThisStep;
            public bool PlatformPassActive;
            public int PlatformPassTimer;
            public float PlatformPassStartY;
            // Stand-and-fire budget: HaltFrames counts time spent standing to attack; once it
            // reaches HaltMaxFrames the NPC enters a RepositionTimer window where it must move
            // instead of standing, so it does ~1-2 attacks then continues rather than getting stuck.
            public int HaltFrames;
            public int RepositionTimer;
            // Rope-climb stall detection: if the climb is blocked by a solid tile above
            // (collideY, no upward progress), force a dismount/jump-up instead of climbing forever.
            public int RopeStallFrames;
            public int LastRopeFeetY;
            // True once we've attempted a forced jump-up off a stalled climb; a second stall means
            // the blocker is a real ceiling (not a jump-through platform) — abort and reroute.
            public bool RopeJumpedThisStep;
            // True once the NPC's feet have actually entered the rope tiles. Lets the NPC float up
            // to a rope whose bottom hangs above its head WITHOUT prematurely triggering atRopeTop.
            public bool RopeEngaged;
            // Travel direction latched for the current rope step (true = ride down). Prevents the
            // per-frame descend recompute from flipping at the target row (the vibration source).
            public bool RopeDirLatched;
            public bool RopeDescend;
            // Set once an intentional dismount has fired this step; blocks Phase 2 from re-grabbing
            // and re-snapping X to the rope center before the step-off velocity can carry it clear.
            public bool RopeDismounting;
            public string LastPlanResult = "";
            // LastPlanResult is overwritten later in the SAME frame by the rope fallback, so Replan's own
            // verdict (no-start-span / no-goal-span / no-path spans=N / plan steps=N) never survived to the
            // log line — every frame read "rope-none". Keep the A* verdict on its own field, with the tick it
            // was produced, so the log can show WHY pathing failed instead of what the rope fallback said.
            public string LastReplanResult = "";
            public int LastReplanTick = -1;
            // Per-NPC log throttle. The previous single static tick meant that with several SF4 enemies
            // alive they COMPETED for one 12-frame sample slot, so each enemy was sampled erratically and
            // the log silently interleaved them (this is why timers appeared to jump by 12 per line).
            public int LastLogTick = -1000;
            // True when the last A* attempt found no path to the player (no-goal-span / no-path).
            // Cleared when A* successfully builds a plan. Guards TryShaftEscape / TryLocalTerrain
            // from firing jumps that loop forever against an impassable wall with no route through.
            public bool NoAStarPath;
            // Recently-failed step targets, mapped to expiry frame. Used by the
            // planner to prefer alternative routes after a step times out.
            public Dictionary<(int x, int y), int> BadEdgeTargets = new Dictionary<(int, int), int>();
            public bool IsCommitted => CommitFrames > 0;
        }

        private enum StepKind { Walk, JumpUp, JumpGap, Drop, PlatformDrop, RopeClimb }
        private struct PlanStep
        {
            public StepKind Kind;
            public int TargetX, TargetY, LaunchX;
            public PlanStep(StepKind k, int tx, int ty, int lx) { Kind = k; TargetX = tx; TargetY = ty; LaunchX = lx; }
        }

        private enum EdgeKind { Walk, JumpUp, JumpGap, Drop, PlatformDrop, RopeClimb }
        private class Edge
        {
            public Span To;
            public EdgeKind Kind;
            public int Cost;
            public int LaunchX, LandX;
            public Edge(Span to, EdgeKind k, int cost, int launchX = 0, int landX = 0)
            { To = to; Kind = k; Cost = cost; LaunchX = launchX; LandX = landX; }
        }

        private class Span
        {
            public int LeftX, RightX, Y;
            public List<Edge> Edges = new List<Edge>();
            public Span(int l, int r, int y) { LeftX = l; RightX = r; Y = y; }
        }

        private class PriorityQueue<T>
        {
            private readonly List<(T item, int prio)> heap = new List<(T, int)>();
            public int Count => heap.Count;
            public void Push(T item, int prio)
            {
                heap.Add((item, prio));
                int i = heap.Count - 1;
                while (i > 0)
                {
                    int p = (i - 1) / 2;
                    if (heap[p].prio <= heap[i].prio) break;
                    (heap[p], heap[i]) = (heap[i], heap[p]);
                    i = p;
                }
            }
            public T Pop()
            {
                var top = heap[0].item;
                heap[0] = heap[heap.Count - 1];
                heap.RemoveAt(heap.Count - 1);
                int i = 0;
                while (true)
                {
                    int l = i * 2 + 1, r = l + 1, m = i;
                    if (l < heap.Count && heap[l].prio < heap[m].prio) m = l;
                    if (r < heap.Count && heap[r].prio < heap[m].prio) m = r;
                    if (m == i) break;
                    (heap[m], heap[i]) = (heap[i], heap[m]);
                    i = m;
                }
                return top;
            }
        }
    }
}
