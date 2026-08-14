using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace tsorcRevamp.NPCs
{
    /// <summary>
    /// Shared Pursue / Search / Patrol state machine + patrol locomotion, used by BOTH the legacy
    /// FighterAI (BasicAI) and SmartFighter4AI. The FSM (state transitions + timers + anchor/last-known)
    /// is fully shared; each movement driver supplies its own Pursue/Search locomotion and calls
    /// <see cref="RunPatrol"/> for the Patrol state. See
    /// Documentation/PatrolPursue_and_NavTier_Removal.md.
    ///
    /// NOT WIRED IN YET (Phase-1 step 2): this file compiles standalone; SF4/FighterAI integration is
    /// steps 3-4.
    /// </summary>
    public static class NavBehavior
    {
        // -- FSM tunables --
        private const int SearchTimeoutTicks = 240;   // ~4s investigating last-known before giving up to Patrol
        private const float ReachLastKnownPx = 32f;   // "arrived" radius for the Search target / spawn anchor

        // -- Patrol tunables (from design) --
        private const float PatrolSpeedMult = 0.6f;   // patrol ambles, doesn't sprint
        private const int IdleStandTicks = 60;        // 1s readable pause; movement is guaranteed afterward
        private const int IdleWalkTilesMin = 3;
        private const int IdleWalkTilesMax = 6;
        private const int WanderCommitFramesMin = 240; // ~4s committed to a wander direction (anti-jitter)
        private const int WanderCommitFramesMax = 480; // ~8s
        private const int CliffLookDownTiles = 4;      // a drop deeper than this = turn around (don't walk off)

        // -- Flee tunables --
        private const float FleeMaxTiles = 40f;         // safety distance before settling into Patrol
        private const float FleeRampFrames = 180f;      // ~3s to gently ramp up to full flee speed
        private const float FleeSpeedMultStart = 1.0f;  // starts at normal topSpeed...
        private const float FleeSpeedMultMax = 1.6f;    // ...gently opens up to 1.6x while running

        private const int TileF = 16;
        // Roughly how many frames it takes to cross one tile at patrol speed (topSpeed*PatrolSpeedMult ~
        // 0.9 px/frame -> ~18 frames/tile). Used to convert short idle-walk tile lengths into a frame timer.
        private const int FramesPerTile = 18;

        // =====================================================================================
        //  FSM
        // =====================================================================================

        /// <summary>
        /// Advance the macro state once per tick. Call from the movement driver BEFORE running the
        /// per-state locomotion. Returns the resulting state.
        /// </summary>
        /// <param name="hasLos">Clear (geometric) line of sight to the player this frame.</param>
        /// <param name="madeProgress">True if the NPC closed distance toward the player/target this frame
        /// (the give-up clock only advances when NOT progressing).</param>
        /// <param name="aggroRange">Distance within which a re-sighted player re-triggers Pursue.</param>
        public static PursuitState UpdateState(
            NPC npc, tsorcRevampGlobalNPC g, Player player, bool hasLos, bool madeProgress, float aggroRange)
        {
            // Diagnostics: snapshot what the FSM was handed, so the nav log can tell the FSM's own
            // decision apart from a later overwrite by the movement layer. See the Fsm* fields on
            // tsorcRevampGlobalNPC. Pure bookkeeping — nothing below reads these.
            g.FsmTick = (int)Main.GameUpdateCount;
            g.FsmEntryState = g.PursuitState;
            g.FsmEntryDisengage = g.DisengageTimer;
            g.FsmBranch = "-";

            // Record the spawn anchor once, the first time we ever tick.
            if (!g.PatrolAnchorSet && g.PatrolAnchorSource == PatrolAnchorSource.SpawnPoint)
            {
                g.PatrolAnchor = npc.Center;
                g.PatrolAnchorSet = true;
            }

            bool inAggro = npc.Distance(player.Center) <= aggroRange;

            // A leash is distinct from the A* search radius: it measures whether a fleeing player
            // is steadily opening the gap. Run this before the LOS fast-path so visible players can
            // still escape an enemy that simply cannot keep up with them.
            bool leashEnabled = g.PursuitState == PursuitState.Pursue
                && g.PursuitLeashRange > 0f && g.PursuitFallBehindTicks > 0;
            bool fallingBehind = leashEnabled
                && npc.Distance(player.Center) > g.PursuitLeashRange
                && !madeProgress;
            g.PursuitFallBehindTimer = fallingBehind ? g.PursuitFallBehindTimer + 1 : 0;
            // `leashEnabled` is REQUIRED here, not just an optimization. The default lever values are
            // PursuitLeashRange 0 / PursuitFallBehindTicks 0, so an enemy that never opted into a leash
            // (i.e. every enemy except the one this was built for) evaluates `0 >= 0` as TRUE and force-
            // disengages EVERY FRAME — returning before the LOS re-acquire and before the state switch,
            // so it can never reach Pursue and its give-up clock is pinned at 0.
            if (leashEnabled && g.PursuitFallBehindTimer >= g.PursuitFallBehindTicks)
            {
                g.PursuitFallBehindTimer = 0;
                if (g.RemembersLastKnownPos)
                {
                    g.PursuitState = PursuitState.Search;
                    g.DisengageTimer = 0;
                }
                else EnterPatrol(npc, g);
                npc.netUpdate = true;
                return FsmExit(g, "leash");
            }

            // LOS (re)acquires the player: remember where, reset the clock, and pursue — but a sighting
            // from outside aggro range while patrolling shouldn't yank us back (avoids long-range pop-aggro).
            // Excluded while Fleeing: the attacker that triggered the flee is BY DEFINITION still visible
            // (that's usually why it looks like a jitter target rather than a threat worth re-engaging), so
            // without this exclusion every fleeing NPC would snap straight back to Pursue the instant hasLos
            // is true and undo the flee before it moves anywhere.
            // GhostUnreachableWanderTimer is the same kind of intentional disengage: let wall-phasing ghosts
            // finish their wander beat instead of snapping back to a known-bad player column.
            if (hasLos && g.PursuitState != PursuitState.Flee && g.GhostUnreachableWanderTimer <= 0)
            {
                g.LastKnownPlayerPos = player.Center;
                if (g.PursuitState == PursuitState.Pursue || inAggro)
                {
                    g.DisengageTimer = 0;
                    g.PursuitState = PursuitState.Pursue;
                    return FsmExit(g, "los-reaquire");
                }
            }

            switch (g.PursuitState)
            {
                case PursuitState.Pursue:
                    g.FsmBranch = madeProgress ? "pursue-progress" : "pursue-noprogress";
                    // Give-up clock advances only while we're NOT making headway (lost LOS AND stuck/no
                    // progress). Actively chasing the player around a corner keeps it at zero.
                    if (madeProgress) g.DisengageTimer = 0;
                    else g.DisengageTimer++;

                    // whoAmI % 61 gives 0–60 ticks (0–1 s) of deterministic per-instance spread so a pack of
                    // same-type enemies doesn't all disengage simultaneously. whoAmI is server-assigned and
                    // consistent across all clients, so no sync overhead.
                    if (g.DisengageTimer >= g.NavGiveUpTicks + (npc.whoAmI % 61))
                    {
                        if (g.RemembersLastKnownPos)
                        {
                            g.PursuitState = PursuitState.Search;
                            g.DisengageTimer = 0;
                        }
                        else EnterPatrol(npc, g);
                    }
                    break;

                case PursuitState.Search:
                    g.DisengageTimer++;
                    bool reachedLastKnown = npc.Distance(g.LastKnownPlayerPos) < ReachLastKnownPx;
                    g.FsmBranch = reachedLastKnown ? "search-arrived" : "search-tick";
                    if (reachedLastKnown || g.DisengageTimer >= SearchTimeoutTicks + (npc.whoAmI % 31))
                        EnterPatrol(npc, g);
                    break;

                case PursuitState.Patrol:
                    // Stay patrolling; re-aggro is handled by the hasLos branch above.
                    g.FsmBranch = "patrol-hold";
                    break;

                case PursuitState.Flee:
                    // Fully driven by RunFlee (movement + the Flee->Patrol handoff at max distance/an
                    // obstacle); nothing to advance here.
                    g.FsmBranch = "flee-hold";
                    break;
            }

            return FsmExit(g, g.FsmBranch);
        }

        /// <summary>
        /// Diagnostics only: stamp the state UpdateState is actually returning, tagged with the branch
        /// that produced it. Compared against the live PursuitState/DisengageTimer at log time to expose
        /// downstream overwrites.
        /// </summary>
        /// <summary>
        /// Diagnostics only: record who last forced a state change from OUTSIDE the FSM's own switch.
        /// The caller attribution is what distinguishes "SF4's stuck detector gave up" from "the beast
        /// stale-wander overlay forced Patrol" from "an enemy's own AI did it" — all three land on the
        /// same two fields and were previously indistinguishable in the log.
        /// </summary>
        private static void StampMutation(tsorcRevampGlobalNPC g, string entry, string by, int line)
        {
            g.FsmMutationTick = (int)Main.GameUpdateCount;
            g.FsmMutationBy = $"{entry}<-{by}:{line}";
        }

        private static PursuitState FsmExit(tsorcRevampGlobalNPC g, string branch)
        {
            g.FsmBranch = branch;
            g.FsmExitState = g.PursuitState;
            g.FsmExitDisengage = g.DisengageTimer;
            return g.PursuitState;
        }

        /// <summary>
        /// Force an immediate disengage to Patrol (or Search). Called by the movement driver's anti-stuck
        /// detector — blocked against a wall it can't get past, regardless of LOS, so a visible-but-
        /// unreachable player never traps it pressing the wall.
        /// </summary>
        public static void ForceDisengage(NPC npc, tsorcRevampGlobalNPC g,
            [CallerMemberName] string by = "?", [CallerLineNumber] int line = 0)
        {
            StampMutation(g, "ForceDisengage", by, line);
            g.PursuitFallBehindTimer = 0;
            if (g.RemembersLastKnownPos && g.PursuitState == PursuitState.Pursue)
            {
                g.PursuitState = PursuitState.Search;
                g.DisengageTimer = 0;
            }
            else EnterPatrol(npc, g);
        }

        public static void EnterPatrol(NPC npc, tsorcRevampGlobalNPC g,
            [CallerMemberName] string by = "?", [CallerLineNumber] int line = 0)
        {
            StampMutation(g, "EnterPatrol", by, line);
            g.PursuitState = PursuitState.Patrol;
            g.DisengageTimer = 0;
            g.PursuitFallBehindTimer = 0;

            // GiveUpLocation anchors here; SpawnPoint keeps its recorded anchor (fall back to here if unset).
            if (g.PatrolAnchorSource == PatrolAnchorSource.GiveUpLocation || !g.PatrolAnchorSet)
            {
                g.PatrolAnchor = npc.Center;
                g.PatrolAnchorSet = true;
            }

            g.PatrolDirection = npc.direction != 0 ? npc.direction : 1;
            g.PatrolLegRemaining = 0;
            // Idle gets one short, readable "gave up" beat before resuming movement. Pace/Wander move
            // immediately, and ReturnToSpawn should start walking back to its anchor immediately.
            g.PatrolIdleTimer = g.PatrolMode == PatrolMode.Idle ? IdleStandTicks : 0;
            g.PatrolElapsed = 0;
        }

        // =====================================================================================
        //  Flee locomotion (run from the (unreachable) attacker, then hand off to Patrol)
        // =====================================================================================

        /// <summary>
        /// Run one tick of Flee: walk away from wherever the flee started (g.FleeDirection, locked in when
        /// Flee was entered — see tsorcRevampAIs.cs's justHit handling), gently ramping speed up over
        /// FleeRampFrames. Settles into a normal Patrol/Wander once FleeMaxTiles is covered or the NPC runs
        /// into a wall/cliff it can't cross (reuses the same cliff/wall awareness as StepAlong so it never
        /// panics off a ledge). Cliff/wall found before the safety distance is the common case, not a bug.
        /// </summary>
        public static void RunFlee(NPC npc, tsorcRevampGlobalNPC g, float topSpeed, float acceleration)
        {
            g.FleeElapsedFrames++;
            float rampT = Math.Min(g.FleeElapsedFrames / FleeRampFrames, 1f);
            float fleeSpeed = topSpeed * (FleeSpeedMultStart + (FleeSpeedMultMax - FleeSpeedMultStart) * rampT);
            float traveledTiles = Math.Abs(npc.Center.X - g.FleeOriginX) / TileF;

            bool reachedSafety = traveledTiles >= FleeMaxTiles;
            bool blocked = reachedSafety || !StepAlong(npc, g.FleeDirection, fleeSpeed, acceleration, speedMult: 1f);
            if (blocked)
            {
                // Safe distance covered, or nowhere further to run — settle into a real wander from here
                // rather than leaving one dead frame before Patrol picks up next tick.
                EnterPatrol(npc, g);
                RunPatrol(npc, g, topSpeed, acceleration);
            }
        }

        // =====================================================================================
        //  Patrol locomotion (shared, cliff/obstacle aware — never jumps)
        // =====================================================================================

        /// <summary>Run one tick of patrol movement for the NPC's configured PatrolMode.</summary>
        public static void RunPatrol(NPC npc, tsorcRevampGlobalNPC g, float topSpeed, float acceleration)
        {
            g.PatrolElapsed++; // time spent patrolling this stint (Relaxed teleport waits on this)
            switch (g.PatrolMode)
            {
                case PatrolMode.Idle: RunIdle(npc, g, topSpeed, acceleration); break;
                case PatrolMode.Pace: RunPace(npc, g, topSpeed, acceleration); break;
                case PatrolMode.Wander: RunWander(npc, g, topSpeed, acceleration); break;
                case PatrolMode.ReturnToSpawn: RunReturnToSpawn(npc, g, topSpeed, acceleration); break;
            }
        }

        // Idle: pause for one second, then ALWAYS take a short walk. The previous 50/50 roll could select
        // another 2-4 second pause indefinitely, which made a healthy patrol look frozen. When a chosen
        // direction is blocked, try the other side immediately before settling into the next short pause.
        private static void RunIdle(NPC npc, tsorcRevampGlobalNPC g, float topSpeed, float acceleration)
        {
            if (g.PatrolLegRemaining > 0)
            {
                g.PatrolLegRemaining--; // frame timer for the short walk
                if (!StepAlong(npc, g.PatrolDirection, topSpeed, acceleration))
                {
                    g.PatrolLegRemaining = 0;
                    if (!TryStartIdleLeg(npc, g, topSpeed, acceleration, -g.PatrolDirection))
                        g.PatrolIdleTimer = IdleStandTicks;
                }
                else if (g.PatrolLegRemaining == 0)
                {
                    g.PatrolIdleTimer = IdleStandTicks;
                }
            }
            else
            {
                Brake(npc);
                if (g.PatrolIdleTimer > 0)
                {
                    g.PatrolIdleTimer--;
                }
                else if (!TryStartIdleLeg(npc, g, topSpeed, acceleration, Main.rand.NextBool() ? 1 : -1))
                {
                    // Neither side is safe (for example, a roof with deep drops on both sides). Retry after a
                    // bounded pause instead of rerolling an arbitrarily long stationary chain every frame.
                    g.PatrolIdleTimer = IdleStandTicks;
                }
            }
        }

        private static bool TryStartIdleLeg(NPC npc, tsorcRevampGlobalNPC g, float topSpeed, float acceleration,
            int preferredDirection)
        {
            int firstDirection = preferredDirection != 0 ? Math.Sign(preferredDirection) : 1;
            int secondDirection = -firstDirection;
            int walkFrames = Main.rand.Next(IdleWalkTilesMin, IdleWalkTilesMax + 1) * FramesPerTile;

            if (StepAlong(npc, firstDirection, topSpeed, acceleration))
            {
                g.PatrolDirection = firstDirection;
                g.PatrolLegRemaining = Math.Max(0, walkFrames - 1); // StepAlong already moved this first frame.
                return true;
            }
            if (StepAlong(npc, secondDirection, topSpeed, acceleration))
            {
                g.PatrolDirection = secondDirection;
                g.PatrolLegRemaining = Math.Max(0, walkFrames - 1);
                return true;
            }
            return false;
        }

        // Pace: sweep BOTH sides of the anchor — walk out to PatrolRange tiles, turn at the reach limit OR
        // at a gap/wall, then sweep the other side. Distance is measured from the anchor (not frame-
        // counted, which was the "2-3 tile jitter" bug), so it actually covers ground; and turning at a
        // gap makes it commit to a full sweep the OTHER way instead of re-poking the same gap.
        private static void RunPace(NPC npc, tsorcRevampGlobalNPC g, float topSpeed, float acceleration)
        {
            if (g.PatrolDirection == 0) g.PatrolDirection = 1;
            float fromAnchorTiles = (npc.Center.X - g.PatrolAnchor.X) / TileF;
            bool atReachLimit = (g.PatrolDirection > 0 && fromAnchorTiles >= g.PatrolRange)
                             || (g.PatrolDirection < 0 && fromAnchorTiles <= -g.PatrolRange);
            if (atReachLimit || !StepAlong(npc, g.PatrolDirection, topSpeed, acceleration))
                g.PatrolDirection = -g.PatrolDirection;
        }

        // Wander: roam within the leash, committing to a direction for several seconds (anti-jitter),
        // turning at gaps/walls, reining back toward the anchor when it drifts past the leash.
        private static void RunWander(NPC npc, tsorcRevampGlobalNPC g, float topSpeed, float acceleration)
        {
            if (g.PatrolDirection == 0) g.PatrolDirection = Main.rand.NextBool() ? 1 : -1;
            bool beyond = BeyondLeash(npc, g);
            if (beyond) g.PatrolDirection = AnchorDirection(npc, g);
            if (!StepAlong(npc, g.PatrolDirection, topSpeed, acceleration))
            {
                g.PatrolDirection = -g.PatrolDirection; // turned at a gap/wall
            }
            else if (!beyond)
            {
                if (g.PatrolIdleTimer > 0) g.PatrolIdleTimer--;
                else
                {
                    if (Main.rand.NextBool(2)) g.PatrolDirection = -g.PatrolDirection;
                    g.PatrolIdleTimer = Main.rand.Next(WanderCommitFramesMin, WanderCommitFramesMax + 1);
                }
            }
        }

        // ReturnToSpawn: walk to the anchor, then idle there.
        private static void RunReturnToSpawn(NPC npc, tsorcRevampGlobalNPC g, float topSpeed, float acceleration)
        {
            if (Math.Abs(npc.Center.X - g.PatrolAnchor.X) <= ReachLastKnownPx)
            {
                RunIdle(npc, g, topSpeed, acceleration); // arrived -> idle routine
                return;
            }
            StepAlong(npc, AnchorDirection(npc, g), topSpeed, acceleration);
        }

        // =====================================================================================
        //  Locomotion + terrain helpers (self-contained; patrol never jumps)
        // =====================================================================================

        /// <summary>Walk one tick in `dir` toward `topSpeed * speedMult`. Returns false if blocked by a wall
        /// or a cliff (the caller turns). Position-based legs are managed by the callers; this no longer
        /// frame-counts a leg (that bug burned a "20-40 tile" leg in 20-40 frames = ~1-2 real tiles -> the
        /// jitter). `speedMult` defaults to the patrol amble (0.6x) — Flee passes 1f since its ramped speed
        /// is already the exact target, not a fraction of topSpeed to further scale down.</summary>
        private static bool StepAlong(NPC npc, int dir, float topSpeed, float acceleration, float speedMult = PatrolSpeedMult)
        {
            if (dir == 0) dir = 1;
            if (WallAhead(npc, dir) || !FloorAhead(npc, dir))
            {
                Brake(npc);
                return false;
            }
            npc.direction = dir;
            npc.spriteDirection = dir;
            float target = dir * topSpeed * speedMult;
            if (npc.velocity.X < target) npc.velocity.X = Math.Min(npc.velocity.X + acceleration, target);
            else if (npc.velocity.X > target) npc.velocity.X = Math.Max(npc.velocity.X - acceleration, target);
            return true;
        }

        private static void Brake(NPC npc)
        {
            npc.velocity.X *= 0.8f;
            if (Math.Abs(npc.velocity.X) < 0.1f) npc.velocity.X = 0f;
        }

        // Direction (+1/-1) from the NPC toward the patrol anchor.
        private static int AnchorDirection(NPC npc, tsorcRevampGlobalNPC g)
            => g.PatrolAnchor.X >= npc.Center.X ? 1 : -1;

        private static bool BeyondLeash(NPC npc, tsorcRevampGlobalNPC g)
            => Math.Abs(npc.Center.X - g.PatrolAnchor.X) > g.PatrolRange * TileF;

        // Solid (non-platform) tile blocking the body at head/torso height just ahead.
        private static bool WallAhead(NPC npc, int dir)
        {
            int frontX = (int)(npc.Center.X / TileF) + dir;
            int feetY = (int)((npc.Bottom.Y - 1f) / TileF);
            return SolidBlock(frontX, feetY - 1) || SolidBlock(frontX, feetY - 2);
        }

        // Is there standable ground continuing ahead, or a cliff we'd walk off?
        private static bool FloorAhead(NPC npc, int dir)
        {
            int frontX = (int)(npc.Center.X / TileF) + dir;
            int feetY = (int)((npc.Bottom.Y - 1f) / TileF);
            for (int d = 0; d <= CliffLookDownTiles; d++)
                if (Standable(frontX, feetY + d)) return true;
            return false; // nothing within CliffLookDownTiles -> treat as a cliff, turn around
        }

        private static bool SolidBlock(int x, int y)
        {
            if (!WorldGen.InWorld(x, y)) return false;
            Tile t = Main.tile[x, y];
            return t.HasTile && Main.tileSolid[t.TileType] && !Main.tileSolidTop[t.TileType];
        }

        // Standable = solid block OR jump-through platform top (so platform walkways aren't read as cliffs).
        private static bool Standable(int x, int y)
        {
            if (!WorldGen.InWorld(x, y)) return false;
            Tile t = Main.tile[x, y];
            return t.HasTile && (Main.tileSolid[t.TileType] || Main.tileSolidTop[t.TileType]);
        }
    }
}
