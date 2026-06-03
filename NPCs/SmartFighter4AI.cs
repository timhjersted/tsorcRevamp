using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

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
    // Self-contained â€” does not touch SmartFighterAI or PathFighter2AI globals.
    public static class SmartFighter4AI
    {
        // ---- Window / search tuning ----
        private const int ScanRadiusX = 50;
        private const int ScanRadiusY = 30;
        private const int MaxDropDepth = 10;
        private const int StepTimeoutFrames = 180;
        private const float TileF = 16f;
        private const float AlignTolerancePx = 12f;

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

        public static void Run(NPC npc, float topSpeed = 1.55f, float acceleration = 0.10f,
            int doorBreakingDamage = 4, float attackRange = 700f)
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

            NavState s = GetState(npc);
            tsorcRevampGlobalNPC g = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();
            float jumpCeil = Math.Max(g.MaxJumpPower, 5f);
            float boostCeil = Math.Max(g.MaxJumpBoost, 2f);

            TickTimers(s);
            ManagePlatformPass(s, npc);
            // Safety: never leave noTileCollide stuck on outside an active platform pass OR a rope
            // climb (noGravity), or the NPC can phase through solid walls (the "passed through a
            // wall" bug). Rope climb legitimately uses noTileCollide and is excluded here.
            if (npc.noTileCollide && !s.PlatformPassActive && !npc.noGravity) npc.noTileCollide = false;
            bool grounded = IsGrounded(npc);

            // Airborne X-velocity lock — only active during a committed jump action.
            // SF4: re-assert the exact launch velocity computed by ComputeJumpArc so the
            // physics arc lands where intended. (SF3 clamped to topSpeed*1.3 here, which erased
            // the horizontal launch speed every frame and made the NPC fall short of gaps.)
            // Drops and other commits with no computed arc fall back to the old gentle clamp.
            if (!grounded && s.AirCommitTimer > 0 && s.AirCommitDirX != 0)
            {
                if (s.CommittedLaunchVx != 0f)
                {
                    npc.velocity.X = s.CommittedLaunchVx;
                }
                else
                {
                    float t = s.AirCommitDirX * topSpeed * 1.05f;
                    npc.velocity.X = MathHelper.Clamp(t, -topSpeed * 1.3f, topSpeed * 1.3f);
                }
            }

            // Replan gating: committed actions LOCK the plan. Only OnHit unlocks early.
            if (!s.IsCommitted && grounded && s.ReplanCooldown == 0 && ShouldReplan(s, npc, player))
            {
                Replan(s, npc, player);
                s.ReplanCooldown = ReplanCooldown;
            }

            string actionLabel = "idle", reasonLabel = "";
            bool actionHandled = false;

            if (s.Plan != null && s.PlanIndex < s.Plan.Count)
            {
                actionHandled = ExecuteStep(s, npc, player, topSpeed, acceleration,
                    jumpCeil, boostCeil, grounded, doorBreakingDamage, g,
                    out actionLabel, out reasonLabel);
            }
            else if (grounded)
            {
                int dir = player.Center.X >= npc.Center.X ? 1 : -1;
                npc.direction = dir; npc.spriteDirection = dir;

                // Fallback rope-grab: no plan, player is above, and a rope is within reach.
                // Synthesizes a one-step RopeClimb plan so the normal executor rides it up.
                // Safety net for cases the span graph misses (the "just stands at the base" bug).
                bool losNow = Collision.CanHit(npc.position, npc.width, npc.height,
                    player.position, player.width, player.height);
                bool inRange = npc.Distance(player.Center) <= attackRange;

                if (TryGrabNearbyRope(s, npc, player))
                {
                    npc.velocity.X *= 0.7f; // settle this frame; executor takes over next frame
                    actionLabel = "rope-grab"; reasonLabel = "fallback";
                    actionHandled = true;
                }
                else
                {
                    // Halt budget counts while NEAR the player — LOS-independent so a flickering
                    // line of sight can't reset it (that flicker-reset was why it never exited the
                    // stand-and-fire). After HaltMaxFrames it forces a RepositionTimer move window.
                    bool nearPlayer = inRange && g.AttackList.Count > 0;
                    if (nearPlayer && s.RepositionTimer == 0)
                    {
                        s.HaltFrames++;
                        if (s.HaltFrames >= HaltMaxFrames)
                        {
                            s.HaltFrames = 0;
                            s.RepositionTimer = RepositionFrames;
                        }
                    }
                    else if (!nearPlayer)
                    {
                        s.HaltFrames = 0; // only reset when genuinely out of range
                    }

                    bool doHalt = nearPlayer && losNow && s.RepositionTimer == 0;
                    if (doHalt)
                    {
                        // Brake to a FULL stop so the idle (feet-together) frame shows instead of
                        // the walk frame — a tiny residual velocity keeps vanilla in the walk anim.
                        if (Math.Abs(npc.velocity.X) < 0.3f) npc.velocity.X = 0f;
                        else npc.velocity.X *= 0.6f;
                        npc.direction = dir; npc.spriteDirection = dir;
                        actionLabel = "halt-attack"; reasonLabel = $"d={npc.Distance(player.Center):F0} h={s.HaltFrames}";
                        actionHandled = true;
                    }
                    else
                    {
                        actionHandled = TryLocalTerrain(s, npc, dir, jumpCeil, boostCeil, topSpeed,
                            out actionLabel, out reasonLabel);
                        if (!actionHandled)
                        {
                        // Don't blindly walk off cliffs in fallback chase. If there's a
                        // big drop directly ahead, brake instead.
                        if (IsCliffAhead(npc, dir))
                        {
                            npc.velocity.X *= 0.6f;
                            actionLabel = "halt-cliff"; reasonLabel = "no-plan,drop-ahead";
                        }
                        else
                        {
                            ApplyChase(npc, dir, topSpeed, acceleration);
                            actionLabel = "chase"; reasonLabel = "no-plan";
                        }
                    }
                    }
                }
            }
            else
            {
                actionLabel = s.AirCommitTimer > 0 ? "air-commit" : "airborne";
                reasonLabel = s.AirCommitTimer > 0 ? $"dirX={s.AirCommitDirX} t={s.AirCommitTimer}" : "no-commit";
            }

            // Attacks
            bool los = Collision.CanHit(npc.position, npc.width, npc.height,
                player.position, player.width, player.height);
            // Attacks are allowed on the rope: SF4 forces CanStopToFire=false below, so firing
            // never halts the climb/descent. (Attacking mid-rope is fine per design.)
            bool canAttack = g.AttackList.Count > 0 && los && npc.Distance(player.Center) <= attackRange;
            if (g.AttackList.Count > 0)
            {
                bool oldStop = g.CanStopToFire;
                g.CanStopToFire = false;
                tsorcRevampAIs.SimpleProjectile(npc, canAttack);
                g.CanStopToFire = oldStop;
            }

            LogFrame(npc, player, s, grounded, los, canAttack, actionLabel, reasonLabel);
        }

        public static void OnHit(NPC npc)
        {
            if (npc.noGravity) npc.noGravity = false;
            if (States.TryGetValue(npc.whoAmI, out NavState s))
            {
                // Damage unlocks commitment and forces a fresh replan.
                s.Plan = null;
                s.PlanIndex = 0;
                s.CommitFrames = 0;
                s.ReplanCooldown = 0;
            }
        }

        // ================================================================================
        //  State management
        // ================================================================================

        private static NavState GetState(NPC npc)
        {
            if (!States.TryGetValue(npc.whoAmI, out NavState s))
            {
                s = new NavState();
                States[npc.whoAmI] = s;
            }
            if (Main.GameUpdateCount % 3600 == 0 && States.Count > 64) Prune();
            return s;
        }

        private static void Prune()
        {
            List<int> dead = new List<int>();
            foreach (var kv in States) if (!Main.npc[kv.Key].active) dead.Add(kv.Key);
            foreach (int id in dead) States.Remove(id);
        }

        private static void TickTimers(NavState s)
        {
            if (s.AirCommitTimer > 0) s.AirCommitTimer--;
            if (s.CommitFrames > 0) s.CommitFrames--;
            if (s.StepTimer > 0) s.StepTimer--;
            if (s.ReplanCooldown > 0) s.ReplanCooldown--;
            if (s.PlatformPassTimer > 0) s.PlatformPassTimer--;
            if (s.RepositionTimer > 0) s.RepositionTimer--;
        }

        // Stand-and-fire tuning: brief stand (~1 attack), then a longer reposition window so the
        // NPC spends most of its time pursuing/maneuvering rather than rooted firing.
        private const int HaltMaxFrames = 180;    // ~3s max standing-and-firing, then forced to move
        private const int RepositionFrames = 180; // ~3s moving before it may halt again

        private static void ManagePlatformPass(NavState s, NPC npc)
        {
            if (!s.PlatformPassActive) return;
            bool timerDone = s.PlatformPassTimer <= 0;
            bool clearedAndFalling = npc.velocity.Y > 0.5f && npc.Bottom.Y > s.PlatformPassStartY + 18f;
            bool landed = npc.collideY && npc.velocity.Y >= 0f;
            if (timerDone || clearedAndFalling || landed)
            {
                npc.noTileCollide = false;
                s.PlatformPassActive = false;
                s.PlatformPassTimer = 0;
            }
        }

        // ================================================================================
        //  Replan
        // ================================================================================

        private static bool ShouldReplan(NavState s, NPC npc, Player player)
        {
            if (s.Plan == null) return true;
            if (s.PlanIndex >= s.Plan.Count) return true;
            if (s.StepTimer == 0) return true;
            // Big drift = player switched rooms / floors â†’ replan.
            float planEndX = s.Plan[s.Plan.Count - 1].TargetX * TileF;
            float planEndY = s.Plan[s.Plan.Count - 1].TargetY * TileF;
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

        private static void Replan(NavState s, NPC npc, Player player)
        {
            // Capture this NPC's jump physics so BuildEdges/TryFindJumpEdge only propose
            // edges this enemy can actually clear (gravity-aware, gap-width-aware).
            tsorcRevampGlobalNPC pg = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();
            _planGravity = npc.gravity > 0f ? npc.gravity : 0.3f;
            _planJumpCeil = Math.Max(pg.MaxJumpPower, 5f);
            _planMaxLaunchVx = 1.55f + Math.Max(pg.MaxJumpBoost, 2f); // topSpeed + boost ceiling

            int npcFeetY = GetFeetTileY(npc);
            int npcCx = (int)(npc.Center.X / TileF);
            int playerFeetY = (int)((player.Bottom.Y - 1f) / TileF);
            int playerCx = (int)(player.Center.X / TileF);

            int xMin = npcCx - ScanRadiusX, xMax = npcCx + ScanRadiusX;
            int yMin = Math.Min(npcFeetY, playerFeetY) - ScanRadiusY;
            int yMax = Math.Max(npcFeetY, playerFeetY) + ScanRadiusY;

            List<Span> spans = BuildSpans(xMin, xMax, yMin, yMax);
            // Prune expired bad-edge entries before building.
            int now = (int)Main.GameUpdateCount;
            if (s.BadEdgeTargets.Count > 0)
            {
                List<(int, int)> dead = new List<(int, int)>();
                foreach (var kv in s.BadEdgeTargets) if (kv.Value <= now) dead.Add(kv.Key);
                foreach (var k in dead) s.BadEdgeTargets.Remove(k);
            }
            BuildEdges(spans, playerFeetY, s.BadEdgeTargets);

            Span start = FindContainingSpan(spans, npcCx, npcFeetY);
            Span goal = FindContainingSpan(spans, playerCx, playerFeetY);
            if (start == null || goal == null)
            {
                s.Plan = null; s.PlanIndex = 0;
                s.LastPlanResult = start == null ? "no-start-span" : "no-goal-span";
                return;
            }

            List<Span> path = AStar(start, goal, playerCx, playerFeetY);
            if (path == null)
            {
                s.Plan = null; s.PlanIndex = 0;
                s.LastPlanResult = $"no-path spans={spans.Count}";
                return;
            }

            s.Plan = ConvertToSteps(path, playerCx, playerFeetY);
            s.PlanIndex = 0;
            s.StepTimer = StepTimeoutFrames;
            s.CommitFrames = 0;
            s.LastPlanResult = $"plan steps={s.Plan.Count} spans={spans.Count} pathLen={path.Count}";
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
                    if (150 > worst) worst = 150;
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
                        int joinX = b.LeftX > a.RightX ? b.LeftX : b.RightX < a.LeftX ? b.RightX : a.LeftX;
                        int joinY = Math.Min(a.Y, b.Y);
                        if (!HasBodyClearanceAtRow(joinX, joinY)) continue;
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
                    if (Math.Abs(dy) > 8) continue; // outside any arc reach
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
                            landX = x < bb.LeftX ? bb.LeftX : (x > bb.RightX ? bb.RightX : x);
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
        private const int MaxRopeClimb = 200; // generous so very tall ropes (100+ tiles) are detected & climbable in one go
        private const float RopeClimbSpeed = 3.2f; // px/frame vertical ride speed

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
            int sideX = ropeCol < bb.LeftX ? bb.LeftX : (ropeCol > bb.RightX ? bb.RightX : ropeCol);
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
        private static bool TryGrabNearbyRope(NavState s, NPC npc, Player player)
        {
            if (s.Plan != null) return false; // only as a no-plan fallback
            int npcCx = (int)(npc.Center.X / TileF);
            int npcFeetY = GetFeetTileY(npc);
            int playerFeetY = (int)((player.Bottom.Y - 1f) / TileF);
            int vdelta = playerFeetY - npcFeetY; // < 0 player above, > 0 player below
            if (Math.Abs(vdelta) < 3) { s.LastPlanResult = "rope-skip same-level"; return false; }
            bool goUp = vdelta < 0;

            int bestX = int.MinValue, bestTargetY = 0, bestDist = int.MaxValue;
            for (int dx = -8; dx <= 8; dx++)
            {
                int x = npcCx + dx;
                if (!FindRopeSpan(x, npcFeetY, 5, out int rb, out int rt)) continue;
                // Rope must extend in the direction of the player.
                if (goUp && rt >= npcFeetY - 1) continue;   // no rope above us
                if (!goUp && rb <= npcFeetY + 1) continue;  // no rope below us
                int dist = Math.Abs(dx);
                if (dist < bestDist)
                {
                    bestDist = dist; bestX = x;
                    bestTargetY = goUp ? Math.Max(rt, playerFeetY)   // up: toward player, capped at top
                                       : Math.Min(rb, playerFeetY);  // down: toward player, capped at bottom
                }
            }
            if (bestX == int.MinValue) { s.LastPlanResult = $"rope-none up={goUp} d={vdelta}"; return false; }

            int targetX = bestX + (player.Center.X >= bestX * TileF + 8f ? 1 : -1);
            s.Plan = new List<PlanStep> { new PlanStep(StepKind.RopeClimb, targetX, bestTargetY, bestX) };
            s.PlanIndex = 0;
            s.StepTimer = StepTimeoutFrames;
            s.CommitFrames = 0;
            s.RopeEngaged = false;
            s.LastPlanResult = $"rope-fallback {(goUp ? "up" : "down")} toY={bestTargetY} x={bestX}";
            return true;
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
                int score = dy * 10 + (x < sp.LeftX ? sp.LeftX - x : x > sp.RightX ? x - sp.RightX : 0);
                if (score < bestScore) { bestScore = score; best = sp; }
            }
            if (best != null) return best;

            // Permissive fallback: pick the nearest span by Manhattan distance.
            // This catches the airborne-player case so we still get a plan toward
            // where the player most likely is/will be.
            int bestDist = int.MaxValue;
            foreach (var sp in spans)
            {
                int dx = x < sp.LeftX ? sp.LeftX - x : x > sp.RightX ? x - sp.RightX : 0;
                int dy = Math.Abs(sp.Y - feetY);
                // Cap so we don't reach into far rooms.
                if (dx > 16 || dy > 12) continue;
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
                int finalX = playerX < last.LeftX ? last.LeftX : playerX > last.RightX ? last.RightX : playerX;
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

        private static bool ExecuteStep(NavState s, NPC npc, Player player, float topSpeed, float acceleration,
            float jumpCeil, float boostCeil, bool grounded, int doorDamage, tsorcRevampGlobalNPC g,
            out string action, out string reason)
        {
            PlanStep step = s.Plan[s.PlanIndex];
            int feetY = GetFeetTileY(npc);

            // ---- Step completion ----
            float tgtPx = step.TargetX * TileF + 8f;
            bool xClose = Math.Abs(npc.Center.X - tgtPx) <= AlignTolerancePx + 4f;
            bool xNear  = Math.Abs(npc.Center.X - tgtPx) <= TileF * 3f;
            bool yClose = Math.Abs(feetY - step.TargetY) <= 2;
            bool completed = false;
            switch (step.Kind)
            {
                case StepKind.Walk: completed = grounded && xClose && yClose; break;
                default:           completed = grounded && yClose && xNear; break;
            }
            if (completed)
            {
                // Restore any rope-climb suspended gravity.
                if (npc.noGravity) npc.noGravity = false;
                s.PlanIndex++;
                s.StepTimer = StepTimeoutFrames;
                s.CommitFrames = 0;
                s.AirCommitTimer = 0;
                action = "step-done";
                reason = $"#{s.PlanIndex - 1}={step.Kind}";
                return false;
            }

            // ---- Step failure timeout ----
            if (s.StepTimer == 0)
            {
                if (npc.noGravity) npc.noGravity = false;
                // Bad-edge memory: remember this failed step's target span so the
                // next replan doesn't immediately pick the same route.
                int expiry = (int)Main.GameUpdateCount + 480; // 8 seconds
                s.BadEdgeTargets[(step.TargetX, step.TargetY)] = expiry;
                s.Plan = null;
                s.PlanIndex = 0;
                s.CommitFrames = 0;
                action = "step-timeout";
                reason = $"{step.Kind} target=({step.TargetX},{step.TargetY})";
                return false;
            }

            // ---- Dispatch ----
            switch (step.Kind)
            {
                case StepKind.Walk:
                    return ExecWalk(s, npc, step, topSpeed, acceleration, jumpCeil, boostCeil, doorDamage, g, grounded, out action, out reason);
                case StepKind.JumpUp:
                case StepKind.JumpGap:
                    return ExecJump(s, npc, step, topSpeed, acceleration, jumpCeil, boostCeil, grounded, feetY, out action, out reason);
                case StepKind.Drop:
                    return ExecDrop(s, npc, step, topSpeed, acceleration, grounded, out action, out reason);
                case StepKind.PlatformDrop:
                    return ExecPlatformDrop(s, npc, step, topSpeed, acceleration, grounded, out action, out reason);
                case StepKind.RopeClimb:
                    return ExecRopeClimb(s, npc, step, topSpeed, acceleration, jumpCeil, boostCeil, grounded, out action, out reason);
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
        private static bool ExecRopeClimb(NavState s, NPC npc, PlanStep step, float topSpeed, float acceleration,
            float jumpCeil, float boostCeil, bool grounded, out string action, out string reason)
        {
            float ropeCenter = step.LaunchX * TileF + 8f;
            int feetY = GetFeetTileY(npc);
            bool onRope = npc.noGravity; // we set this true once riding begins
            bool descend = step.TargetY > feetY; // target below the NPC -> ride down

            // Mark engagement once feet are actually within the rope tiles. Lets the NPC float
            // up to (or drop onto) a rope whose end hangs away from its starting position without
            // the end-of-rope check firing prematurely.
            if (IsRopeTile(step.LaunchX, feetY) || IsRopeTile(step.LaunchX, feetY - 1))
                s.RopeEngaged = true;

            // Reset per-step flags whenever we're grounded and off the rope (fresh entry).
            if (grounded && !onRope) { s.RopeJumpedThisStep = false; }

            // Phase 1: align horizontally with the rope column while grounded.
            if (grounded && !onRope && Math.Abs(npc.Center.X - ropeCenter) > 4f)
            {
                int aDir = npc.Center.X < ropeCenter ? 1 : -1;
                npc.direction = aDir; npc.spriteDirection = aDir;
                ApplyChase(npc, aDir, topSpeed, acceleration);
                s.RopeStallFrames = 0; s.LastRopeFeetY = feetY; s.RopeEngaged = false;
                action = "align-rope"; reason = $"ropeX={step.LaunchX} cx={npc.Center.X / TileF:F1} {(descend ? "down" : "up")}";
                return true;
            }

            // End-of-rope: only meaningful once engaged. Up = no rope above; down = no rope below.
            bool atRopeEnd = s.RopeEngaged && (descend
                ? !IsRopeTile(step.LaunchX, feetY + 1)
                : !IsRopeTile(step.LaunchX, feetY - 1));
            bool reachedTarget = descend ? feetY >= step.TargetY : feetY <= step.TargetY;

            // Climbing up into a SOLID (non-rope) ceiling at head height. The ride uses noTileCollide so the
            // body would otherwise phase up into the blocks above the rope. Treat this as a stop point so the
            // dismount logic resolves it instead of riding into the solid.
            bool ceilingCapped = !descend && s.RopeEngaged
                && IsNavigationSolid(step.LaunchX, feetY - 2) && !IsRopeTile(step.LaunchX, feetY - 2);

            // Stall detection: blocked in the travel direction with no progress.
            bool noProgress = descend ? feetY <= s.LastRopeFeetY : feetY >= s.LastRopeFeetY;
            bool blocked = onRope && npc.collideY && noProgress;
            if (blocked) s.RopeStallFrames++;
            else s.RopeStallFrames = 0;
            bool forceDismount = s.RopeStallFrames > 12;

            // Phase 3: dismount — reached target, ran off the rope end, hit a ceiling, or stalled.
            if (onRope && (reachedTarget || atRopeEnd || forceDismount || ceilingCapped))
            {
                npc.noGravity = false;
                npc.noTileCollide = false; // re-enable collision before any horizontal dismount motion
                s.RopeStallFrames = 0;
                int rise = feetY - step.TargetY; // > 0 means the target is still above us

                // DEAD END (up): a solid ceiling is above us and we have NOT reached the (higher) target — this
                // rope physically can't deliver us there (the planner aimed at a span sitting above solid blocks).
                // Abandon the route and replan instead of looping dismount<->re-grab at the rope top, which is the
                // on-rope vibration, with the head stuck phasing in the ceiling. Bad-edge it so we don't immediately
                // retry the same dead-end rope.
                if (!descend && !reachedTarget && IsNavigationSolid(step.LaunchX, feetY - 2))
                {
                    s.BadEdgeTargets[(step.TargetX, step.TargetY)] = (int)Main.GameUpdateCount + 480;
                    s.Plan = null; s.PlanIndex = 0; s.CommitFrames = 0;
                    s.RopeEngaged = false;
                    action = "rope-deadend"; reason = $"toY={step.TargetY} ropeTop~{feetY} solidAbove";
                    return false;
                }

                // Second forced dismount = real ceiling/floor block, not a jump-through platform.
                if (forceDismount && s.RopeJumpedThisStep)
                {
                    int expiry = (int)Main.GameUpdateCount + 480;
                    s.BadEdgeTargets[(step.TargetX, step.TargetY)] = expiry;
                    s.Plan = null; s.PlanIndex = 0; s.CommitFrames = 0;
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
                    s.AirCommitDirX = 0; s.AirCommitTimer = 0; s.CommittedLaunchVx = 0f;
                    s.CommitFrames = 24;
                    if (forceDismount) s.RopeJumpedThisStep = true;
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
                s.AirCommitDirX = offDir;
                s.AirCommitTimer = 14;
                s.CommittedLaunchVx = 0f;
                s.CommitFrames = 14;
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
            if (!onRope && s.RopeEngaged && !atRopeNow)
            {
                action = "rope-detached"; reason = "off-rope";
                return false;
            }

            // Phase 2: steady vertical ride. noTileCollide lets the body ignore blocks beside the
            // rope (and the solid underside of a destination ledge the rope passes), mimicking how
            // a player climbs a rope between walls. Safe from sideways wall-phasing because
            // velocity.X is zeroed and X is hard-snapped to the rope center every frame.
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.position.X = ropeCenter - npc.width / 2f;
            npc.velocity.X = 0f;
            npc.velocity.Y = descend ? RopeClimbSpeed : -RopeClimbSpeed;
            s.CommitFrames = Math.Max(s.CommitFrames, 8);
            // Refresh the step timeout ONLY while making progress toward the target. If the ride
            // stalls (blocked), let StepTimer run down so the step times out and replans.
            bool progressed = descend ? feetY > s.LastRopeFeetY : feetY < s.LastRopeFeetY;
            if (progressed) s.StepTimer = StepTimeoutFrames;
            s.LastRopeFeetY = feetY;
            action = descend ? "rope-descend" : "rope-climb";
            reason = $"feetY={feetY}->toY={step.TargetY} eng={s.RopeEngaged} stall={s.RopeStallFrames}";
            return true;
        }

        private static bool ExecWalk(NavState s, NPC npc, PlanStep step, float topSpeed, float acceleration,
            float jumpCeil, float boostCeil, int doorDamage, tsorcRevampGlobalNPC g, bool grounded,
            out string action, out string reason)
        {
            float tgtPx = step.TargetX * TileF + 8f;
            int dir = npc.Center.X < tgtPx - AlignTolerancePx * 0.5f ? 1
                    : npc.Center.X > tgtPx + AlignTolerancePx * 0.5f ? -1
                    : npc.direction;
            npc.direction = dir; npc.spriteDirection = dir;
            if (!grounded) { action = "walk-air"; reason = ""; return false; }
            // Step-hop / door pass â€” but if a tall wall blocks, mark this walk as failed.
            if (doorDamage > 0 && TryDoor(npc, dir, doorDamage, g, out action, out reason)) return true;
            if (TryLocalTerrain(s, npc, dir, jumpCeil, boostCeil, topSpeed, out action, out reason))
            {
                if (action == "blocked")
                {
                    // Walk step can't progress â€” abort step so the planner reroutes.
                    s.StepTimer = 0;
                }
                return true;
            }
            ApplyChase(npc, dir, topSpeed, acceleration);
            action = "walk"; reason = $"->{step.TargetX}";
            return true;
        }

        private static bool ExecJump(NavState s, NPC npc, PlanStep step, float topSpeed, float acceleration,
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
                    s.BadEdgeTargets[(step.TargetX, step.TargetY)] = expiry;
                    s.StepTimer = Math.Min(s.StepTimer, 8);
                }
                action = "jump-air";
                reason = $"commit={s.CommitFrames} cx={npc.collideX}";
                return false;
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
                ApplyChase(npc, aDir, topSpeed, acceleration);
                action = "align-jump";
                reason = $"launch={step.LaunchX} cx={npc.Center.X / TileF:F1}";
                return true;
            }
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

            // Descending target directly below us: NEVER jump up to go down. Drop straight
            // through instead (noTileCollide passes wood platforms; ManagePlatformPass ends it
            // on landing). Fixes the "bounce up to reach a target below" bug from the log.
            if (rise < 0 && absDx <= 1)
            {
                npc.noTileCollide = true;
                npc.velocity.X = 0f;
                npc.velocity.Y = Math.Max(npc.velocity.Y, 2.0f);
                s.PlatformPassActive = true;
                s.PlatformPassTimer = 24;
                s.PlatformPassStartY = npc.Bottom.Y;
                s.CommitFrames = 25;
                s.AirCommitDirX = 0;
                s.CommittedLaunchVx = 0f;
                action = "jump-dropdown"; reason = $"down rise={rise}";
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
                s.StepTimer = 0;
                npc.velocity.X *= 0.5f;
                action = "jump-abort";
                reason = $"infeasible dx={absDx} rise={rise} g={npc.gravity:F2} maxVx={maxLaunchVx:F1}";
                return true;
            }
            FireJump(s, npc, launchDir, power, launchVx, /*airCommit*/ 35, /*planCommit*/ 45);
            if (isVertical)
            {
                // Kill horizontal velocity and disable the airborne X-lock so we go
                // straight up — the "stop and jump straight up" capability.
                npc.velocity.X = 0f;
                s.AirCommitDirX = 0;
                s.CommittedLaunchVx = 0f;
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
        //    maxJumpPower : ceiling on launch vertical speed (g.MaxJumpPower)
        //    maxLaunchVx  : ceiling on launch horizontal speed (topSpeed + g.MaxJumpBoost)
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

        private static bool ExecDrop(NavState s, NPC npc, PlanStep step, float topSpeed, float acceleration,
            bool grounded, out string action, out string reason)
        {
            if (!grounded) { action = "drop-air"; reason = ""; return false; }
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
                s.BadEdgeTargets[(step.TargetX, step.TargetY)] = expiry;
                s.StepTimer = 0;
                action = "drop-blocked"; reason = "solid-below";
                return true;
            }
            int descend = step.TargetX > step.LaunchX ? 1
                        : step.TargetX < step.LaunchX ? -1
                        : npc.direction;
            npc.direction = descend; npc.spriteDirection = descend;
            if (platformBelow)
            {
                // Pass straight down through the wood platform. CRITICAL: zero horizontal velocity
                // while noTileCollide is active — any sideways motion would phase THROUGH walls
                // (the "passed through a solid wall" bug). ManagePlatformPass ends it on landing.
                npc.noTileCollide = true;
                npc.velocity.X = 0f;
                npc.velocity.Y = Math.Max(npc.velocity.Y, 1.6f);
                s.PlatformPassActive = true;
                s.PlatformPassTimer = 18;
                s.PlatformPassStartY = npc.Bottom.Y;
                s.AirCommitDirX = 0;
                s.CommittedLaunchVx = 0f;
                s.CommitFrames = 25;
            }
            else
            {
                // Free-fall through an open gap — drift toward the target column (no walls here).
                ApplyChase(npc, descend, topSpeed, acceleration);
                s.AirCommitDirX = descend;
                s.AirCommitTimer = 25;
                s.CommitFrames = 25;
                s.CommittedLaunchVx = 0f;
            }
            action = "drop"; reason = $"toY={step.TargetY} plat={platformBelow}";
            return true;
        }

        private static bool ExecPlatformDrop(NavState s, NPC npc, PlanStep step, float topSpeed, float acceleration,
            bool grounded, out string action, out string reason)
        {
            if (!grounded || s.PlatformPassActive)
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
            s.PlatformPassActive = true;
            s.PlatformPassTimer = 18;
            s.PlatformPassStartY = npc.Bottom.Y;
            s.CommitFrames = 25;
            action = "platform-drop"; reason = $"toY={step.TargetY}";
            return true;
        }

        // ================================================================================
        //  Local terrain handlers (step-hop, small gap-jump) for inside Walk steps
        //  and the no-plan fallback chase.
        // ================================================================================

        private static bool TryLocalTerrain(NavState s, NPC npc, int direction, float jumpCeil,
            float boostCeil, float topSpeed, out string action, out string reason)
        {
            int frontX = GetFrontTileX(npc, direction);
            int feetY = GetFeetTileY(npc);
            float maxLaunchVx = topSpeed + boostCeil;
            int oh = GetObstacleHeight(frontX, feetY);
            if (oh > 0)
            {
                if (oh == 1 && HasHeadroomForJump(npc, direction, 1))
                {
                    // Small step-hop: physics arc over a 1-tile obstacle, ~2 tiles forward.
                    if (ComputeJumpArc(2, 1, npc.gravity, jumpCeil, maxLaunchVx, out float hp, out float hvx))
                    {
                        FireJump(s, npc, direction, hp, hvx, 16, 12);
                        action = "step-hop"; reason = $"h=1 p{hp:F1}/vx{hvx:F2}";
                        return true;
                    }
                }
                // Cap raised 4 -> 6 to match this NPC's jump power (MaxJumpPower 9 ≈ 8 tiles of height).
                // It was giving up ("too-tall") on walls it can actually clear; ComputeJumpArc still gates
                // feasibility, so an unmakeable jump falls through to the blocked brake below.
                if (oh <= 6 && HasHeadroomForJump(npc, direction, oh))
                {
                    if (ComputeJumpArc(2, oh, npc.gravity, jumpCeil, maxLaunchVx, out float op, out float ovx))
                    {
                        FireJump(s, npc, direction, op, ovx, 26, 30);
                        action = "obstacle-jump"; reason = $"h={oh} p{op:F1}/vx{ovx:F2}";
                        return true;
                    }
                }
                npc.velocity.X *= 0.4f;
                action = "blocked"; reason = oh > 6 ? "too-tall" : "no-headroom";
                return true;
            }
            int drop = GetDropDepth(frontX, feetY, 6);
            if (drop >= 2 && TryMeasureGap(frontX, feetY, direction, out int gap, out int landDrop, out int landX))
            {
                // Physics gates the gap now (not a hardcoded <=5 cap): jump only if makeable.
                if (gap >= 2 && gap <= 7 && landDrop <= 2)
                {
                    if (ComputeJumpArc(gap, -landDrop, npc.gravity, jumpCeil, maxLaunchVx, out float gp, out float gvx))
                    {
                        FireJump(s, npc, direction, gp, gvx, 26, 30);
                        action = "gap-jump"; reason = $"gap={gap},drop={landDrop} p{gp:F1}/vx{gvx:F2}";
                        return true;
                    }
                    // Not makeable — halt at the edge rather than committing to a fall.
                    npc.velocity.X *= 0.5f;
                    action = "gap-halt"; reason = $"infeasible gap={gap},drop={landDrop} g={npc.gravity:F2}";
                    return true;
                }
            }
            action = ""; reason = "";
            return false;
        }

        private static bool TryDoor(NPC npc, int direction, int doorDamage, tsorcRevampGlobalNPC g,
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
                g.DoorBreakProgress += doorDamage;
                WorldGen.KillTile(dx, dy, true, true);
                if (g.DoorBreakProgress >= 10)
                {
                    g.DoorBreakProgress = 0;
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

        private static void FireJump(NavState s, NPC npc, int direction, float jumpPower, float launchVx,
            int airCommitFrames, int planCommitFrames)
        {
            npc.velocity.Y = -jumpPower;
            // SET (not +=) the exact computed horizontal speed. The airborne handler re-asserts
            // CommittedLaunchVx every frame so this magnitude survives the whole arc.
            npc.velocity.X = launchVx * direction;
            npc.direction = direction;
            npc.spriteDirection = direction;
            s.AirCommitDirX = direction;
            s.AirCommitTimer = airCommitFrames;
            s.CommitFrames = planCommitFrames;
            s.CommittedLaunchVx = launchVx * direction;
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
            return IsNavigationSolid(x, y) || IsPlatformTile(x, y);
        }

        private static int GetFrontTileX(NPC npc, int direction) =>
            direction > 0 ? (int)((npc.Right.X + 4f) / TileF) : (int)((npc.Left.X - 4f) / TileF);

        private static int GetFeetTileY(NPC npc) => (int)((npc.Bottom.Y - 1f) / TileF);

        private static int GetObstacleHeight(int frontX, int feetY)
        {
            for (int h = 5; h >= 1; h--)
                if (IsNavigationSolid(frontX, feetY - h)) return h + 1;
            if (IsNavigationSolid(frontX, feetY)) return 1;
            return 0;
        }

        private static bool HasHeadroomForJump(NPC npc, int direction, int obstacleHeight)
        {
            int frontX = GetFrontTileX(npc, direction);
            int headY = (int)((npc.Top.Y + 4f) / TileF);
            int feetY = GetFeetTileY(npc);
            int highest = Math.Max(headY - 2, feetY - obstacleHeight - 4);
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
            out int gapTiles, out int landingDrop, out int landingX)
        {
            gapTiles = 0; landingDrop = 0; landingX = frontX;
            for (int o = 1; o <= 7; o++)
            {
                int cx = frontX + direction * o;
                int d = GetDropDepth(cx, feetY, 5);
                if (d <= 2 && HasBodyClearanceAtRow(cx, feetY + d))
                {
                    gapTiles = o; landingDrop = d; landingX = cx;
                    return true;
                }
            }
            return false;
        }

        private static bool HasBodyClearanceAtRow(int x, int feetY)
        {
            for (int y = feetY - 2; y <= feetY; y++)
                if (IsNavigationSolid(x, y)) return false;
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
                    lands.Add(lx); // same column = pure vertical
                    if (lx + 1 <= oMax) lands.Add(lx + 1);
                    if (lx - 1 >= oMin) lands.Add(lx - 1);
                }

                foreach (int la in lands)
                {
                    int adx = Math.Abs(la - lx);
                    // SF4: propose the edge only if this NPC can physically make the arc
                    // (gravity + jump/boost limits). Replaces the static-table membership test,
                    // so flat wide gaps (no table entry) are now correctly considered.
                    if (!ComputeJumpArc(adx, dy, _planGravity, _planJumpCeil, _planMaxLaunchVx, out _, out _)) continue;
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

        private static bool IsSpanOnPlatformOnly(Span s)
        {
            for (int x = s.LeftX; x <= s.RightX; x++)
                if (!IsPlatformTile(x, s.Y + 1)) return false;
            return true;
        }

        private static bool IsPlatformTile(int x, int y)
        {
            if (!WorldGen.InWorld(x, y)) return false;
            Tile t = Main.tile[x, y];
            return t.HasTile && !t.IsActuated && TileID.Sets.Platforms[t.TileType];
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

        private static int _lastLogTick;
        private static void LogFrame(NPC npc, Player player, NavState s, bool grounded, bool los,
            bool attack, string action, string reason)
        {
            int now = (int)Main.GameUpdateCount;
            // Sample finer while on/working a rope so fast vibration & phasing are actually captured between lines.
            bool ropeContext = npc.noGravity
                || (s.Plan != null && s.PlanIndex < s.Plan.Count && s.Plan[s.PlanIndex].Kind == StepKind.RopeClimb);
            int interval = ropeContext ? 3 : 12;
            if (now - _lastLogTick < interval) return;
            _lastLogTick = now;
            try
            {
                string sep = Path.DirectorySeparatorChar.ToString();
                string dir = Main.SavePath + sep + "Logs";
                Directory.CreateDirectory(dir);
                string path = dir + sep + "tsorcRevamp-smartfighter4.log";
                string planStr = "none";
                if (s.Plan != null)
                {
                    planStr = $"{s.PlanIndex}/{s.Plan.Count}";
                    if (s.PlanIndex < s.Plan.Count)
                    {
                        var st = s.Plan[s.PlanIndex];
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
                if (s.Plan != null && s.PlanIndex < s.Plan.Count && s.Plan[s.PlanIndex].Kind == StepKind.RopeClimb)
                {
                    var rs = s.Plan[s.PlanIndex];
                    int lx = rs.LaunchX;
                    int feetY = GetFeetTileY(npc);
                    bool descend = rs.TargetY > feetY;
                    bool atEnd = s.RopeEngaged && (descend ? !IsRopeTile(lx, feetY + 1) : !IsRopeTile(lx, feetY - 1));
                    bool reached = descend ? feetY >= rs.TargetY : feetY <= rs.TargetY;
                    ropeStr = $" [rope {(descend ? "DN" : "UP")} toY={rs.TargetY} feetY={feetY} lastFeetY={s.LastRopeFeetY}"
                        + $" ropeU1={IsRopeTile(lx, feetY - 1)} ropeHead={IsRopeTile(lx, feetY - 2)} ropeD1={IsRopeTile(lx, feetY + 1)}"
                        + $" solidU2={IsNavigationSolid(lx, feetY - 2)} solidU3={IsNavigationSolid(lx, feetY - 3)}"
                        + $" eng={s.RopeEngaged} stall={s.RopeStallFrames} atEnd={atEnd} reached={reached}]";
                }
                string line = $"[{DateTime.Now:HH:mm:ss}] {npc.TypeName}#{npc.whoAmI}"
                    + $" pos=({npc.Center.X / TileF:F1},{npc.Center.Y / TileF:F1})"
                    + $" player=({player.Center.X / TileF:F1},{player.Center.Y / TileF:F1})"
                    + $" vel=({npc.velocity.X:F2},{npc.velocity.Y:F2})"
                    + $" g={grounded} cx={npc.collideX} cy={npc.collideY}"
                    + $" pass=({npc.noTileCollide},{npc.noGravity}) los={los}"
                    + $" plan={planStr}"
                    + $" stepT={s.StepTimer} commit={s.CommitFrames} air={s.AirCommitDirX}/{s.AirCommitTimer}"
                    + $" rcd={s.ReplanCooldown} pdrop={s.PlatformPassActive}/{s.PlatformPassTimer}"
                    + $" plan=\"{s.LastPlanResult}\""
                    + $" action={action} reason={reason} attack={attack}"
                    + ropeStr;
                File.AppendAllText(path, line + Environment.NewLine);
            }
            catch { }
        }

        // ================================================================================
        //  Types
        // ================================================================================

        private class NavState
        {
            public List<PlanStep> Plan;
            public int PlanIndex;
            public int StepTimer;
            public int ReplanCooldown;
            public int AirCommitTimer;
            public int AirCommitDirX;
            // Signed horizontal launch velocity computed by ComputeJumpArc, re-asserted each
            // airborne frame so the physics arc isn't eroded by chase steering. 0 = use fallback.
            public float CommittedLaunchVx;
            // CommitFrames > 0 means an action is in flight; replan is locked.
            public int CommitFrames;
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
            public string LastPlanResult = "";
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

