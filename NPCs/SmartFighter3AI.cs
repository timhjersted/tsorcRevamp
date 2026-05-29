using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace tsorcRevamp.NPCs
{
    // SmartFighter3 — Test 3 ground enemy AI, third iteration.
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
    //     5× horizontal. Drop edges that move AWAY from the player's vertical
    //     level get a large cost penalty so the NPC stops walking off cliffs
    //     toward a player who is actually above.
    //
    // Self-contained — does not touch SmartFighterAI or PathFighter2AI globals.
    public static class SmartFighter3AI
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
        //   apex_height ≈ power^2 / (2 * 0.3) tiles  (~135px at power 9 = 8 tiles)
        //   airtime ≈ 2 * power / 0.3 frames        (~60 frames at power 9)
        //   horizontal_reach ≈ airtime * (topSpeed + boost)
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
            bool grounded = IsGrounded(npc);

            // Airborne X-velocity lock — only active during a committed jump action.
            if (!grounded && s.AirCommitTimer > 0 && s.AirCommitDirX != 0)
            {
                float t = s.AirCommitDirX * topSpeed * 1.05f;
                npc.velocity.X = MathHelper.Clamp(t, -topSpeed * 1.3f, topSpeed * 1.3f);
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

                // Halt-at-attack-range: if we can see the player AND we're already in
                // attack range, stand still and let the projectile attack do the work.
                // Stops the "fired spear, then walked off ledge" behavior.
                bool losNow = Collision.CanHit(npc.position, npc.width, npc.height,
                    player.position, player.width, player.height);
                bool inRange = npc.Distance(player.Center) <= attackRange;
                if (losNow && inRange && g.AttackList.Count > 0)
                {
                    npc.velocity.X *= 0.6f; // brake
                    npc.direction = dir; npc.spriteDirection = dir;
                    actionLabel = "halt-attack"; reasonLabel = $"d={npc.Distance(player.Center):F0}";
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
            else
            {
                actionLabel = s.AirCommitTimer > 0 ? "air-commit" : "airborne";
                reasonLabel = s.AirCommitTimer > 0 ? $"dirX={s.AirCommitDirX} t={s.AirCommitTimer}" : "no-commit";
            }

            // Attacks
            bool los = Collision.CanHit(npc.position, npc.width, npc.height,
                player.position, player.width, player.height);
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
        }

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
            // Big drift = player switched rooms / floors → replan.
            float planEndX = s.Plan[s.Plan.Count - 1].TargetX * TileF;
            float planEndY = s.Plan[s.Plan.Count - 1].TargetY * TileF;
            if (Math.Abs(player.Center.X - planEndX) > 16 * TileF) return true;
            if (Math.Abs(player.Center.Y - planEndY) > 10 * TileF) return true;
            return false;
        }

        private static void Replan(NavState s, NPC npc, Player player)
        {
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
                // ---- WALK / step-hop. Same Y or ±1 row, touching or 1-gap ----
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
                // For each column in span A, look for a rope tile column going up
                // far enough to reach another span. Rope climbing is cheaper than
                // jumping for tall climbs and uses no jump cooldown.
                for (int x = a.LeftX; x <= a.RightX; x++)
                {
                    // Find the column the rope occupies (might be the same as x).
                    if (!HasRopeColumn(x, a.Y, out int ropeTopY)) continue;
                    // Find a span we can step off the rope onto, near ropeTopY.
                    foreach (var bb in spans)
                    {
                        if (bb == a) continue;
                        int dyClimb = a.Y - bb.Y;
                        if (dyClimb < 2 || dyClimb > 14) continue;
                        if (Math.Abs(bb.Y - ropeTopY) > 2) continue;
                        if (x < bb.LeftX - 1 || x > bb.RightX + 1) continue;
                        int badP = BadEdgePenalty(x, bb.Y, badEdges);
                        // Rope climb has no jump cooldown — keep its cost low so
                        // the planner prefers ropes for tall climbs.
                        a.Edges.Add(new Edge(bb, EdgeKind.RopeClimb, 3 + dyClimb / 2 + badP, x, x));
                        break;
                    }
                }
            }
        }

        // A rope column has rope tiles for at least 3 contiguous rows above feetY.
        private static bool HasRopeColumn(int x, int feetY, out int topY)
        {
            topY = feetY;
            int run = 0;
            for (int y = feetY - 1; y >= feetY - 16; y--)
            {
                if (IsRopeTile(x, y)) { run++; topY = y; }
                else break;
            }
            return run >= 3;
        }

        private static bool IsRopeTile(int x, int y)
        {
            if (!WorldGen.InWorld(x, y)) return false;
            Tile t = Main.tile[x, y];
            return t.HasTile && !t.IsActuated && t.TileType == TileID.Rope;
        }

        private static Span FindContainingSpan(List<Span> spans, int x, int feetY)
        {
            // Strict pass: must contain x (±1) and be within 3 rows.
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
        //  A* search — goal-aware heuristic (5× vertical weight)
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
        // is weighted 5× horizontal so the planner won't volunteer to drop a floor
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
                    return ExecRopeClimb(s, npc, step, topSpeed, acceleration, grounded, out action, out reason);
            }
            action = "?"; reason = "";
            return false;
        }

        private static bool ExecRopeClimb(NavState s, NPC npc, PlanStep step, float topSpeed, float acceleration,
            bool grounded, out string action, out string reason)
        {
            float ropeCenter = step.LaunchX * TileF + 8f;
            // Phase 1: walk to the rope column
            if (Math.Abs(npc.Center.X - ropeCenter) > AlignTolerancePx && grounded)
            {
                int aDir = npc.Center.X < ropeCenter ? 1 : -1;
                npc.direction = aDir; npc.spriteDirection = aDir;
                ApplyChase(npc, aDir, topSpeed, acceleration);
                action = "align-rope"; reason = $"ropeX={step.LaunchX}";
                return true;
            }
            // Phase 2: climb. Set noGravity, pull toward rope center, move up.
            npc.noGravity = true;
            npc.velocity.X = MathHelper.Clamp((ropeCenter - npc.Center.X) * 0.1f, -1.2f, 1.2f);
            npc.velocity.Y = -2.5f;
            // Commit for ~80 frames or until we reach the target Y. ManagePlatformPass
            // is unrelated; we restore gravity on step completion.
            s.CommitFrames = 60;
            action = "rope-climb"; reason = $"toY={step.TargetY}";
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
            // Step-hop / door pass — but if a tall wall blocks, mark this walk as failed.
            if (doorDamage > 0 && TryDoor(npc, dir, doorDamage, g, out action, out reason)) return true;
            if (TryLocalTerrain(s, npc, dir, jumpCeil, boostCeil, topSpeed, out action, out reason))
            {
                if (action == "blocked")
                {
                    // Walk step can't progress — abort step so the planner reroutes.
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
                action = "jump-air";
                reason = $"commit={s.CommitFrames}";
                return false;
            }
            int absDx = Math.Abs(step.TargetX - step.LaunchX);
            // For pure vertical jumps we need tighter alignment (±5 px) since the
            // body has to fit through a narrow vertical opening, and we ALSO need
            // to bring the NPC to a near-stop before firing — otherwise residual
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
            // Fire from arc table
            int rise = feetY - step.TargetY;          // positive = rising
            (int dx, int dy) key = (absDx, rise);
            if (!JumpArcs.TryGetValue(key, out var arc))
            {
                arc = NearestArc(absDx, rise, jumpCeil, boostCeil);
            }
            float power = MathHelper.Clamp(arc.power, 4.5f, jumpCeil);
            float boost = MathHelper.Clamp(arc.boost, 0f, boostCeil);
            int launchDir = step.TargetX > step.LaunchX ? 1
                          : step.TargetX < step.LaunchX ? -1
                          : npc.direction;
            FireJump(s, npc, launchDir, power, boost, /*airCommit*/ 35, /*planCommit*/ 45);
            if (isVertical)
            {
                // Kill horizontal velocity and disable the airborne X-lock so we go
                // straight up — the "stop and jump straight up" capability that was
                // missing.
                npc.velocity.X = 0f;
                s.AirCommitDirX = 0;
            }
            action = "jump-fire";
            reason = $"arc({absDx},{rise})=>p{power:F1}/b{boost:F1} dir={launchDir}";
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
            int descend = step.TargetX > step.LaunchX ? 1
                        : step.TargetX < step.LaunchX ? -1
                        : npc.direction;
            npc.direction = descend; npc.spriteDirection = descend;
            ApplyChase(npc, descend, topSpeed, acceleration);
            // Commit the airborne phase so we follow through and don't reverse.
            s.AirCommitDirX = descend;
            s.AirCommitTimer = 25;
            s.CommitFrames = 25;
            action = "drop"; reason = $"toY={step.TargetY}";
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
            int oh = GetObstacleHeight(frontX, feetY);
            if (oh > 0)
            {
                if (oh == 1 && HasHeadroomForJump(npc, direction, 1))
                {
                    var arc = JumpArcs.TryGetValue((2, 1), out var a) ? a : (power: 5f, boost: 1.2f);
                    FireJump(s, npc, direction, arc.power * 0.75f, arc.boost, 16, 12);
                    action = "step-hop"; reason = "h=1";
                    return true;
                }
                if (oh <= 4 && HasHeadroomForJump(npc, direction, oh))
                {
                    if (!JumpArcs.TryGetValue((2, oh), out var arc))
                        arc = NearestArc(2, oh, jumpCeil, boostCeil);
                    FireJump(s, npc, direction, arc.power, arc.boost, 26, 30);
                    action = "obstacle-jump"; reason = $"h={oh}";
                    return true;
                }
                npc.velocity.X *= 0.4f;
                action = "blocked"; reason = oh > 4 ? "too-tall" : "no-headroom";
                return true;
            }
            int drop = GetDropDepth(frontX, feetY, 6);
            if (drop >= 2 && TryMeasureGap(frontX, feetY, direction, out int gap, out int landDrop, out int landX))
            {
                if (gap >= 2 && gap <= 5 && landDrop <= 2)
                {
                    if (!JumpArcs.TryGetValue((gap, -landDrop), out var arc))
                        arc = NearestArc(gap, -landDrop, jumpCeil, boostCeil);
                    FireJump(s, npc, direction, arc.power, arc.boost, 26, 30);
                    action = "gap-jump"; reason = $"gap={gap},drop={landDrop}";
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

        private static void FireJump(NavState s, NPC npc, int direction, float jumpPower, float horizontalBoost,
            int airCommitFrames, int planCommitFrames)
        {
            npc.velocity.Y = -jumpPower;
            npc.velocity.X += horizontalBoost * direction;
            npc.direction = direction;
            npc.spriteDirection = direction;
            s.AirCommitDirX = direction;
            s.AirCommitTimer = airCommitFrames;
            s.CommitFrames = planCommitFrames;
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
                // B is to the right of A — prefer launching from A's right edge inward.
                for (int x = a.RightX; x >= a.LeftX && launches.Count < 6; x--) launches.Add(x);
            }
            else if (b.RightX < a.LeftX)
            {
                for (int x = a.LeftX; x <= a.RightX && launches.Count < 6; x++) launches.Add(x);
            }
            else
            {
                // Overlap — start in the middle of overlap, fan out.
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
                    if (!JumpArcs.ContainsKey((adx, dy))) continue;
                    if (!HasTrajectoryClearance(lx, a.Y, la, b.Y, dy)) continue;
                    launchX = lx; landX = la; absDx = adx;
                    return true;
                }
            }
            launchX = 0; landX = 0; absDx = 0;
            return false;
        }

        // Trajectory-shaped clearance — only checks the actual jump arc, not a
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
                    // Body is 3 rows tall — check 3 rows centered at apex region.
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
            if (now - _lastLogTick < 12) return;
            _lastLogTick = now;
            try
            {
                string sep = Path.DirectorySeparatorChar.ToString();
                string dir = Main.SavePath + sep + "Logs";
                Directory.CreateDirectory(dir);
                string path = dir + sep + "tsorcRevamp-smartfighter3.log";
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
                    + $" action={action} reason={reason} attack={attack}";
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
            // CommitFrames > 0 means an action is in flight; replan is locked.
            public int CommitFrames;
            public bool PlatformPassActive;
            public int PlatformPassTimer;
            public float PlatformPassStartY;
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
