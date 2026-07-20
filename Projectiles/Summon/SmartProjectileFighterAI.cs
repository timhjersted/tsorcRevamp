using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace tsorcRevamp.Projectiles.Summon
{
    public static class SmartProjectileFighterAI
    {
        private const int ScanRadiusX = 100;
        private const int ScanRadiusY = 48;
        private const int MaxDropDepth = 10;
        private const int StepTimeoutFrames = 180;
        private const float TileF = 16f;
        private const float AlignTolerancePx = 12f;
        private const int ReplanCooldown = 60;

        private static readonly Dictionary<int, NavState> States = new Dictionary<int, NavState>();

        // High-level entry point to run the pathfinding AI for a Projectile.
        // Returns true if the projectile is currently navigating a path.
        public static bool Run(Projectile projectile, Entity target, float topSpeed, float acceleration, int navSearchRadius = 24, float gravity = 0.4f, float maxJumpPower = 8.5f, float maxJumpBoost = 3.5f)
        {
            if (!IsTargetValid(target))
            {
                projectile.velocity.X *= 0.9f;
                return false;
            }

            NavState s = GetState(projectile);
            TickTimers(s);
            ManagePlatformPass(s, projectile);

            // Safety check: if tileCollide is false but we are not passing platforms, re-enable it
            if (!projectile.tileCollide && !s.PlatformPassActive)
            {
                projectile.tileCollide = true;
            }

            bool grounded = IsGrounded(projectile);

            // Airborne X-velocity lock during a committed jump action
            if (!grounded && s.AirCommitTimer > 0 && s.AirCommitDirX != 0)
            {
                if (s.CommittedLaunchVx != 0f)
                {
                    projectile.velocity.X = s.CommittedLaunchVx;
                }
                else
                {
                    float t = s.AirCommitDirX * topSpeed * 1.05f;
                    projectile.velocity.X = MathHelper.Clamp(t, -topSpeed * 1.3f, topSpeed * 1.3f);
                }
            }

            // A* replanning when needed
            if (navSearchRadius > 0)
            {
                if (!s.IsCommitted && grounded && s.ReplanCooldown == 0 && ShouldReplan(s, projectile, target))
                {
                    Replan(s, projectile, target, navSearchRadius, gravity, maxJumpPower, maxJumpBoost);
                    s.ReplanCooldown = ReplanCooldown;
                }
            }
            else if (s.Plan != null)
            {
                s.Plan = null;
                s.PlanIndex = 0;
            }

            if (s.Plan != null && s.PlanIndex < s.Plan.Count)
            {
                string action, reason;
                ExecuteStep(s, projectile, target, topSpeed, acceleration, maxJumpPower, maxJumpBoost, grounded, out action, out reason);
                if (!s.RopeGravityDisabled && projectile.tileCollide) AutoStepUp(projectile);
                return true;
            }

            return false;
        }

        // Returns true if gravity should be disabled for the projectile (e.g. while climbing a rope)
        public static bool IsGravityDisabled(Projectile projectile)
        {
            if (States.TryGetValue(projectile.whoAmI, out NavState s))
            {
                return s.RopeGravityDisabled;
            }
            return false;
        }

        private static bool IsTargetValid(Entity target)
        {
            if (target == null || !target.active) return false;
            if (target is Player player) return !player.dead;
            if (target is NPC npc) return npc.life > 0;
            return true;
        }

        private static NavState GetState(Projectile projectile)
        {
            if (!States.TryGetValue(projectile.whoAmI, out NavState s))
            {
                s = new NavState();
                States[projectile.whoAmI] = s;
            }
            if (Main.GameUpdateCount % 3600 == 0 && States.Count > 64) Prune();
            return s;
        }

        private static void Prune()
        {
            List<int> dead = new List<int>();
            foreach (var kv in States)
            {
                if (!Main.projectile[kv.Key].active) dead.Add(kv.Key);
            }
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

        private static void ManagePlatformPass(NavState s, Projectile projectile)
        {
            if (!s.PlatformPassActive) return;
            bool timerDone = s.PlatformPassTimer <= 0;
            bool clearedAndFalling = projectile.velocity.Y > 0.5f && projectile.Bottom.Y > s.PlatformPassStartY + 18f;
            bool landed = IsGrounded(projectile) && projectile.velocity.Y >= 0f;
            if (timerDone || clearedAndFalling || landed)
            {
                projectile.tileCollide = true;
                s.PlatformPassActive = false;
                s.PlatformPassTimer = 0;
            }
        }

        private static bool ShouldReplan(NavState s, Projectile projectile, Entity target)
        {
            if (s.Plan == null) return true;
            if (s.PlanIndex >= s.Plan.Count) return true;
            if (s.StepTimer == 0) return true;

            float planEndX = s.Plan[s.Plan.Count - 1].TargetX * TileF;
            float planEndY = s.Plan[s.Plan.Count - 1].TargetY * TileF;
            if (Math.Abs(target.Center.X - planEndX) > 16 * TileF) return true;
            if (Math.Abs(target.Center.Y - planEndY) > 10 * TileF) return true;
            return false;
        }

        private static float _planGravity = 0.4f;
        private static float _planJumpCeil = 8.5f;
        private static float _planMaxLaunchVx = 5f;

        private static void Replan(NavState s, Projectile projectile, Entity target, int navSearchRadius, float gravity, float maxJumpPower, float maxJumpBoost)
        {
            _planGravity = gravity > 0f ? gravity : 0.4f;
            _planJumpCeil = Math.Max(maxJumpPower, 5f);
            _planMaxLaunchVx = 1.55f + Math.Max(maxJumpBoost, 2f);

            int feetY = GetFeetTileY(projectile);
            int cx = (int)(projectile.Center.X / TileF);
            int targetFeetY = (int)((target.Bottom.Y - 1f) / TileF);
            int targetCx = (int)(target.Center.X / TileF);

            int radius = Math.Clamp(navSearchRadius, 1, ScanRadiusX);
            int yRadius = Math.Min(radius, ScanRadiusY);
            int xMin = cx - radius, xMax = cx + radius;
            int yMin = Math.Min(feetY, targetFeetY) - yRadius;
            int yMax = Math.Max(feetY, targetFeetY) + yRadius;

            List<Span> spans = BuildSpans(xMin, xMax, yMin, yMax);

            int now = (int)Main.GameUpdateCount;
            if (s.BadEdgeTargets.Count > 0)
            {
                List<(int, int)> dead = new List<(int, int)>();
                foreach (var kv in s.BadEdgeTargets) if (kv.Value <= now) dead.Add(kv.Key);
                foreach (var k in dead) s.BadEdgeTargets.Remove(k);
            }
            BuildEdges(spans, targetFeetY, s.BadEdgeTargets);

            Span start = FindContainingSpan(spans, cx, feetY);
            Span goal = FindContainingSpan(spans, targetCx, targetFeetY);
            if (start == null || goal == null)
            {
                s.Plan = null;
                s.PlanIndex = 0;
                return;
            }

            List<Span> path = AStar(start, goal, targetCx, targetFeetY);
            if (path == null)
            {
                s.Plan = null;
                s.PlanIndex = 0;
                return;
            }

            s.Plan = ConvertToSteps(path, targetCx, targetFeetY);
            s.PlanIndex = 0;
            s.StepTimer = StepTimeoutFrames;
            s.CommitFrames = 0;
        }

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

        private static void BuildEdges(List<Span> spans, int targetY, Dictionary<(int x, int y), int> badEdges)
        {
            Dictionary<int, List<Span>> byY = new Dictionary<int, List<Span>>();
            foreach (var sp in spans)
            {
                if (!byY.TryGetValue(sp.Y, out var bucket)) { bucket = new List<Span>(); byY[sp.Y] = bucket; }
                bucket.Add(sp);
            }

            foreach (var a in spans)
            {
                // WALK / step-hop
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (!byY.TryGetValue(a.Y + dy, out var bucket)) continue;
                    foreach (var b in bucket)
                    {
                        if (b == a) continue;
                        bool touching = b.LeftX <= a.RightX + 1 && b.RightX >= a.LeftX - 1;
                        if (!touching) continue;
                        int joinX = b.LeftX > a.RightX ? b.LeftX : b.RightX < a.LeftX ? b.RightX : a.LeftX;
                        int joinY = Math.Min(a.Y, b.Y);
                        if (!HasBodyClearanceAtRow(joinX, joinY)) continue;
                        a.Edges.Add(new Edge(b, EdgeKind.Walk, 1 + Math.Abs(dy)));
                    }
                }

                // JUMP edges
                foreach (var b in spans)
                {
                    if (b == a) continue;
                    int dy = a.Y - b.Y;
                    if (Math.Abs(dy) > 8) continue;
                    if (TryFindJumpEdge(a, b, dy, out int launchX, out int landX, out int absDx))
                    {
                        EdgeKind kind = dy >= 1 ? EdgeKind.JumpUp : EdgeKind.JumpGap;
                        int wrongDirPenalty = 0;
                        if (targetY < a.Y && b.Y > a.Y) wrongDirPenalty = 60;
                        int landingWidth = b.RightX - b.LeftX + 1;
                        int tightPenalty = landingWidth <= 2 ? 15 : 0;
                        int badPenalty = BadEdgePenalty(landX, b.Y, badEdges);
                        int baseCost = kind == EdgeKind.JumpUp ? 5 + dy * 2 : 5 + absDx;
                        a.Edges.Add(new Edge(b, kind, baseCost + wrongDirPenalty + tightPenalty + badPenalty, launchX, landX));
                    }
                }

                // DROP edges
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
                        int dropPenalty = targetY < a.Y - 3 ? 80 : 0;
                        int badP = BadEdgePenalty(candidate, b.Y, badEdges);
                        a.Edges.Add(new Edge(b, EdgeKind.Drop, 3 + dy + dropPenalty + badP, candidate, candidate));
                    }
                }

                // PLATFORM DROP edges
                for (int dy = 2; dy <= MaxDropDepth; dy++)
                {
                    if (!byY.TryGetValue(a.Y + dy, out var bucket)) continue;
                    foreach (var b in bucket)
                    {
                        if (b == a) continue;
                        int candidate = -1;
                        int xMin = Math.Max(a.LeftX, b.LeftX), xMax = Math.Min(a.RightX, b.RightX);
                        for (int x = xMin; x <= xMax; x++)
                        {
                            if (IsPlatformTile(x, a.Y + 1) && HasDropClearance(x, a.Y + 2, b.Y))
                            { candidate = x; break; }
                        }
                        if (candidate == -1) continue;
                        int dropPenalty = targetY < a.Y - 3 ? 80 : 0;
                        int badP = BadEdgePenalty(candidate, b.Y, badEdges);
                        a.Edges.Add(new Edge(b, EdgeKind.PlatformDrop, 4 + dy + dropPenalty + badP, candidate, candidate));
                    }
                }

                // ROPE-CLIMB edges
                for (int x = a.LeftX - 1; x <= a.RightX + 1; x++)
                {
                    if (!FindRopeSpan(x, a.Y, 5, out int ropeBottomY, out int ropeTopY)) continue;
                    if (ropeTopY >= a.Y) continue;
                    foreach (var bb in spans)
                    {
                        if (bb == a) continue;
                        int dyClimb = a.Y - bb.Y;
                        if (dyClimb < 2 || dyClimb > MaxRopeClimb) continue;
                        if (x < bb.LeftX - 1 || x > bb.RightX + 1) continue;

                        bool valid = false;
                        int extra = 0;
                        int landX = x;

                        if (bb.Y >= ropeTopY && bb.Y <= ropeBottomY && RopeSideExitClear(x, bb))
                        {
                            valid = true;
                            landX = x < bb.LeftX ? bb.LeftX : (x > bb.RightX ? bb.RightX : x);
                        }
                        else if (bb.Y < ropeTopY)
                        {
                            int jumpRise = ropeTopY - bb.Y;
                            if (jumpRise >= 1 && RopeTopJumpClear(x, ropeTopY, bb)
                                && ComputeJumpArc(0, jumpRise, _planGravity, _planJumpCeil, _planMaxLaunchVx, out _, out _))
                            {
                                valid = true;
                                extra = 2 + jumpRise;
                                landX = Math.Clamp(x, bb.LeftX, bb.RightX);
                            }
                        }

                        if (!valid) continue;
                        int badP = BadEdgePenalty(x, bb.Y, badEdges);
                        a.Edges.Add(new Edge(bb, EdgeKind.RopeClimb, 3 + dyClimb / 2 + extra + badP, x, landX));
                    }
                }
            }
        }

        private const int MaxRopeClimb = 200;
        private const float RopeClimbSpeed = 3.2f;

        private static bool IsRopeTile(int x, int y)
        {
            if (!WorldGen.InWorld(x, y)) return false;
            Tile t = Main.tile[x, y];
            if (!t.HasTile || t.IsActuated) return false;
            int ty = t.TileType;
            return ty == TileID.Rope || ty == TileID.SilkRope || ty == TileID.WebRope
                || ty == TileID.VineRope || ty == TileID.Chain;
        }

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
            if (b - t < 2) return false;
            bottomY = b; topY = t;
            return true;
        }

        private static bool RopeSideExitClear(int ropeCol, Span bb)
        {
            int sideX = ropeCol < bb.LeftX ? bb.LeftX : (ropeCol > bb.RightX ? bb.RightX : ropeCol);
            if (sideX == ropeCol) return false;
            return IsStandableTile(sideX, bb.Y + 1) && HasBodyClearanceAtRow(sideX, bb.Y);
        }

        private static bool RopeTopJumpClear(int ropeCol, int ropeTopY, Span bb)
        {
            for (int y = ropeTopY - 1; y >= bb.Y; y--)
            {
                if (IsNavigationSolid(ropeCol, y)) return false;
            }
            if (!IsStandableTile(ropeCol, bb.Y + 1)) return false;
            for (int dx = -1; dx <= 1; dx++)
            {
                if (IsNavigationSolid(ropeCol + dx, bb.Y) || IsNavigationSolid(ropeCol + dx, bb.Y - 1)) return false;
            }
            return true;
        }

        private static float RopeSnapX(Projectile proj, int ropeCol, float ropeCenter, int feetY)
        {
            float snapX = ropeCenter;
            int bodyRow = feetY - 1;
            bool solidRight = IsNavigationSolid(ropeCol + 1, bodyRow) && !IsRopeTile(ropeCol + 1, bodyRow);
            bool solidLeft = IsNavigationSolid(ropeCol - 1, bodyRow) && !IsRopeTile(ropeCol - 1, bodyRow);
            float overhang = (proj.width - TileF) / 2f + 1f;
            if (overhang > 0f)
            {
                if (solidRight && !solidLeft) snapX -= overhang;
                else if (solidLeft && !solidRight) snapX += overhang;
            }
            return snapX - proj.width / 2f;
        }

        private static Span FindContainingSpan(List<Span> spans, int x, int y)
        {
            foreach (var sp in spans)
            {
                if (sp.Y == y && x >= sp.LeftX && x <= sp.RightX) return sp;
            }
            // Vertical tolerance fallback
            foreach (var sp in spans)
            {
                if (Math.Abs(sp.Y - y) <= 1 && x >= sp.LeftX && x <= sp.RightX) return sp;
            }
            return null;
        }

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

        private static int Heuristic(Span sp, int goalX, int goalY)
        {
            int dx = sp.LeftX > goalX ? sp.LeftX - goalX
                   : goalX > sp.RightX ? goalX - sp.RightX : 0;
            int dy = Math.Abs(sp.Y - goalY);
            return dx + dy * 5;
        }

        private static List<PlanStep> ConvertToSteps(List<Span> spanPath, int targetX, int targetY)
        {
            var steps = new List<PlanStep>();
            for (int i = 0; i < spanPath.Count - 1; i++)
            {
                Span from = spanPath[i];
                Span to = spanPath[i + 1];
                Edge edge = null;
                foreach (var e in from.Edges)
                {
                    if (e.To == to) { edge = e; break; }
                }
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
                        steps.Add(new PlanStep(edge.Kind == EdgeKind.JumpUp ? StepKind.JumpUp : StepKind.JumpGap, edge.LandX, to.Y, edge.LaunchX));
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
            if (spanPath.Count > 0)
            {
                Span last = spanPath[spanPath.Count - 1];
                int finalX = targetX < last.LeftX ? last.LeftX : targetX > last.RightX ? last.RightX : targetX;
                steps.Add(new PlanStep(StepKind.Walk, finalX, last.Y, finalX));
            }
            return steps;
        }

        private static bool ExecuteStep(NavState s, Projectile proj, Entity target, float topSpeed, float acceleration, float jumpCeil, float boostCeil, bool grounded, out string action, out string reason)
        {
            PlanStep step = s.Plan[s.PlanIndex];
            int feetY = GetFeetTileY(proj);

            float tgtPx = step.TargetX * TileF + 8f;
            bool xClose = Math.Abs(proj.Center.X - tgtPx) <= AlignTolerancePx + 4f;
            bool xNear = Math.Abs(proj.Center.X - tgtPx) <= TileF * 3f;
            bool yClose = Math.Abs(feetY - step.TargetY) <= 2;
            bool completed = false;
            switch (step.Kind)
            {
                case StepKind.Walk: completed = grounded && xClose && yClose; break;
                default: completed = grounded && yClose && xNear; break;
            }

            if (completed)
            {
                if (s.RopeGravityDisabled) s.RopeGravityDisabled = false;
                s.PlanIndex++;
                s.StepTimer = StepTimeoutFrames;
                s.CommitFrames = 0;
                s.AirCommitTimer = 0;
                s.RopeEngaged = false; s.RopeDirLatched = false; s.RopeDismounting = false;
                action = "step-done";
                reason = $"#{s.PlanIndex - 1}={step.Kind}";
                return false;
            }

            if (s.StepTimer == 0)
            {
                if (s.RopeGravityDisabled) s.RopeGravityDisabled = false;
                int expiry = (int)Main.GameUpdateCount + 480;
                s.BadEdgeTargets[(step.TargetX, step.TargetY)] = expiry;
                s.Plan = null;
                s.PlanIndex = 0;
                s.CommitFrames = 0;
                s.RopeEngaged = false; s.RopeDirLatched = false; s.RopeDismounting = false;
                action = "step-timeout";
                reason = $"{step.Kind} target=({step.TargetX},{step.TargetY})";
                return false;
            }

            switch (step.Kind)
            {
                case StepKind.Walk:
                    return ExecWalk(s, proj, step, topSpeed, acceleration, jumpCeil, boostCeil, grounded, out action, out reason);
                case StepKind.JumpUp:
                case StepKind.JumpGap:
                    return ExecJump(s, proj, step, topSpeed, acceleration, jumpCeil, boostCeil, grounded, feetY, out action, out reason);
                case StepKind.Drop:
                    return ExecDrop(s, proj, step, topSpeed, acceleration, grounded, out action, out reason);
                case StepKind.PlatformDrop:
                    return ExecPlatformDrop(s, proj, step, topSpeed, acceleration, grounded, out action, out reason);
                case StepKind.RopeClimb:
                    return ExecRopeClimb(s, proj, step, topSpeed, acceleration, jumpCeil, boostCeil, grounded, out action, out reason);
            }
            action = "?"; reason = "";
            return false;
        }

        private static bool ExecRopeClimb(NavState s, Projectile proj, PlanStep step, float topSpeed, float acceleration, float jumpCeil, float boostCeil, bool grounded, out string action, out string reason)
        {
            float ropeCenter = step.LaunchX * TileF + 8f;
            int feetY = GetFeetTileY(proj);
            bool onRope = s.RopeGravityDisabled;

            bool descend = s.RopeDirLatched ? s.RopeDescend : step.TargetY > feetY;

            if (IsRopeTile(step.LaunchX, feetY) || IsRopeTile(step.LaunchX, feetY - 1))
                s.RopeEngaged = true;

            if (grounded && !onRope) { s.RopeJumpedThisStep = false; s.RopeDirLatched = false; s.RopeDismounting = false; }

            bool ropeAtFeet = IsRopeTile(step.LaunchX, feetY)
                           || IsRopeTile(step.LaunchX, feetY - 1)
                           || IsRopeTile(step.LaunchX, feetY + 1);
            bool closeEnoughToGrab = ropeAtFeet && Math.Abs(proj.Center.X - ropeCenter) <= TileF;

            // If we are grounded and aligned but the rope starts above us, jump up to grab it
            if (grounded && !onRope && !closeEnoughToGrab && Math.Abs(proj.Center.X - ropeCenter) <= 12f)
            {
                // Check if there is rope above us
                bool ropeAbove = false;
                for (int y = feetY - 1; y >= feetY - 5; y--)
                {
                    if (IsRopeTile(step.LaunchX, y))
                    {
                        ropeAbove = true;
                        break;
                    }
                }
                if (ropeAbove)
                {
                    proj.velocity.Y = -7f;
                    s.AlignStallFrames = 0;
                    action = "rope-grab-jump";
                    reason = "rope-above";
                    return true;
                }
            }

            // Phase 1: align horizontally
            if (grounded && !onRope && !closeEnoughToGrab && Math.Abs(proj.Center.X - ropeCenter) > 4f)
            {
                int aDir = proj.Center.X < ropeCenter ? 1 : -1;
                proj.direction = aDir; proj.spriteDirection = aDir;
                s.RopeStallFrames = 0; s.LastRopeFeetY = feetY; s.RopeEngaged = false;
                int aFrontX = GetFrontTileX(proj, aDir);
                int oh = GetObstacleHeight(aFrontX, feetY);
                if (oh == 2 && HasHeadroomForJump(proj, aDir, oh)
                    && ComputeJumpArc(2, oh, _planGravity, jumpCeil, topSpeed + boostCeil, out float hp, out float hvx))
                {
                    FireJump(s, proj, aDir, hp, hvx, 16, 12);
                    s.AlignStallFrames = 0;
                    action = "align-rope-hop"; reason = $"oh={oh}";
                    return true;
                }
                if (Math.Abs(proj.velocity.X) < 0.1f && oh > 2)
                {
                    s.AlignStallFrames++;
                    if (s.AlignStallFrames > 18) { s.StepTimer = 0; s.AlignStallFrames = 0; }
                }
                else s.AlignStallFrames = 0;
                ApplyChase(proj, aDir, topSpeed, acceleration);
                action = "align-rope"; reason = $"ropeX={step.LaunchX}";
                return true;
            }
            s.AlignStallFrames = 0;

            bool atRopeEnd = s.RopeEngaged && (descend
                ? !IsRopeTile(step.LaunchX, feetY + 1)
                : !IsRopeTile(step.LaunchX, feetY - 1));
            bool reachedTarget = descend ? feetY >= step.TargetY : feetY <= step.TargetY;

            bool ceilingCapped = !descend && s.RopeEngaged
                && IsNavigationSolid(step.LaunchX, feetY - 2) && !IsRopeTile(step.LaunchX, feetY - 2);

            bool noProgress = descend ? feetY <= s.LastRopeFeetY : feetY >= s.LastRopeFeetY;
            bool blocked = onRope && noProgress;
            if (blocked) s.RopeStallFrames++;
            else s.RopeStallFrames = 0;
            bool forceDismount = s.RopeStallFrames > 12;

            if (atRopeEnd || reachedTarget || ceilingCapped || forceDismount)
            {
                s.RopeGravityDisabled = false;
                s.RopeDirLatched = false;
                s.RopeDismounting = true;
                proj.velocity.Y = 0f;

                if (ceilingCapped || forceDismount)
                {
                    int exitDir = proj.direction;
                    proj.velocity.Y = -5f;
                    proj.velocity.X = 1.5f * exitDir;
                    s.CommitFrames = 20;
                    action = "rope-abort-jump"; reason = ceilingCapped ? "ceiling" : "stall";
                    return true;
                }

                // Normal dismount step off
                int dDir = step.TargetX > step.LaunchX ? 1 : (step.TargetX < step.LaunchX ? -1 : proj.direction);
                if (!descend)
                {
                    proj.velocity.Y = -5.5f; // Small hop upward to clear platform lip
                    proj.velocity.X = dDir * topSpeed * 1.2f; // Extra forward push
                    s.CommitFrames = 15;
                }
                else
                {
                    proj.velocity.X = dDir * topSpeed;
                    s.CommitFrames = 10;
                }
                proj.direction = dDir; proj.spriteDirection = dDir;
                action = "rope-dismount"; reason = atRopeEnd ? "end" : "reached";
                return true;
            }

            // We've already fired an intentional dismount (reached target / ran off the end / aborted) this
            // step — do NOT let Phase 2 re-grab and re-snap X back onto the rope. That re-snap erases the
            // step-off velocity and pins us beside the rope, and the dismount<->re-grab alternation is the
            // left/right vibration. Let the committed step-off velocity carry clear; the step completes once
            // grounded near the target. (Mirrors the SF4 RopeDismounting guard.)
            if (!onRope && (s.RopeDismounting || (s.RopeEngaged && !ropeAtFeet)))
            {
                action = "rope-detached"; reason = s.RopeDismounting ? "dismounting" : "off-rope";
                return true;
            }

            // Phase 2: ride rope
            s.RopeGravityDisabled = true;
            s.RopeDirLatched = true;
            s.RopeDescend = descend;

            // Snap center
            proj.position.X = RopeSnapX(proj, step.LaunchX, ropeCenter, feetY);
            proj.velocity.X = 0f;
            proj.velocity.Y = descend ? RopeClimbSpeed : -RopeClimbSpeed;

            bool progressed = feetY != s.LastRopeFeetY;
            if (progressed) s.StepTimer = StepTimeoutFrames;
            s.LastRopeFeetY = feetY;
            action = descend ? "rope-descend" : "rope-climb";
            reason = $"feetY={feetY}->toY={step.TargetY}";
            return true;
        }

        private static bool ExecWalk(NavState s, Projectile proj, PlanStep step, float topSpeed, float acceleration, float jumpCeil, float boostCeil, bool grounded, out string action, out string reason)
        {
            float tgtPx = step.TargetX * TileF + 8f;
            int dir = proj.Center.X < tgtPx - AlignTolerancePx * 0.5f ? 1
                    : proj.Center.X > tgtPx + AlignTolerancePx * 0.5f ? -1
                    : proj.direction;
            proj.direction = dir; proj.spriteDirection = dir;
            if (!grounded) { action = "walk-air"; reason = ""; return false; }

            if (TryLocalTerrain(s, proj, dir, jumpCeil, boostCeil, topSpeed, out action, out reason))
            {
                if (action == "blocked")
                {
                    s.StepTimer = 0;
                }
                return true;
            }
            ApplyChase(proj, dir, topSpeed, acceleration);
            action = "walk"; reason = $"->{step.TargetX}";
            return true;
        }

        private static bool ExecJump(NavState s, Projectile proj, PlanStep step, float topSpeed, float acceleration, float jumpCeil, float boostCeil, bool grounded, int feetY, out string action, out string reason)
        {
            if (!grounded)
            {
                if (Math.Abs(proj.velocity.Y) < 0.5f && Math.Abs(proj.velocity.X) < 0.5f)
                {
                    int expiry = (int)Main.GameUpdateCount + 480;
                    s.BadEdgeTargets[(step.TargetX, step.TargetY)] = expiry;
                    s.StepTimer = Math.Min(s.StepTimer, 8);
                }
                action = "jump-air";
                reason = $"commit={s.CommitFrames}";
                return false;
            }

            int absDx = Math.Abs(step.TargetX - step.LaunchX);
            bool isVertical = absDx == 0;
            float alignTol = isVertical ? 5f : AlignTolerancePx;
            float launchCenter = step.LaunchX * TileF + 8f;
            bool aligned = Math.Abs(proj.Center.X - launchCenter) <= alignTol;
            if (!aligned)
            {
                int aDir = proj.Center.X < launchCenter ? 1 : -1;
                proj.direction = aDir; proj.spriteDirection = aDir;
                ApplyChase(proj, aDir, topSpeed, acceleration);
                if (Math.Abs(proj.velocity.X) < 0.1f)
                {
                    s.AlignStallFrames++;
                    if (s.AlignStallFrames > 18) { s.StepTimer = 0; s.AlignStallFrames = 0; }
                }
                else s.AlignStallFrames = 0;
                action = "align-jump";
                reason = $"launch={step.LaunchX}";
                return true;
            }
            s.AlignStallFrames = 0;

            if (isVertical && Math.Abs(proj.velocity.X) > 0.4f)
            {
                proj.velocity.X *= 0.5f;
                action = "settle-jump";
                reason = "";
                return true;
            }

            int rise = feetY - step.TargetY;

            // Descending target below us: dropdown platforms instead of jumping
            if (rise < 0 && absDx <= 1)
            {
                proj.tileCollide = false;
                proj.velocity.X = 0f;
                proj.velocity.Y = Math.Max(proj.velocity.Y, 2.0f);
                s.PlatformPassActive = true;
                s.PlatformPassTimer = 24;
                s.PlatformPassStartY = proj.Bottom.Y;
                s.CommitFrames = 25;
                s.AirCommitDirX = 0;
                s.CommittedLaunchVx = 0f;
                action = "jump-dropdown"; reason = $"down rise={rise}";
                return true;
            }

            float maxLaunchVx = topSpeed + boostCeil;
            int launchDir = step.TargetX > step.LaunchX ? 1 : (step.TargetX < step.LaunchX ? -1 : proj.direction);
            bool feasible = ComputeJumpArc(absDx, rise, _planGravity, jumpCeil, maxLaunchVx, out float power, out float launchVx);
            if (!feasible)
            {
                s.StepTimer = 0;
                proj.velocity.X *= 0.5f;
                action = "jump-abort";
                reason = "infeasible";
                return true;
            }

            FireJump(s, proj, launchDir, power, launchVx, 35, 45);
            if (isVertical)
            {
                proj.velocity.X = 0f;
                s.AirCommitDirX = 0;
                s.CommittedLaunchVx = 0f;
            }
            action = "jump-fire";
            reason = $"rise={rise}";
            return true;
        }

        private static bool ComputeJumpArc(int dxTiles, int riseTiles, float gravity, float maxJumpPower, float maxLaunchVx, out float jumpPower, out float launchVx)
        {
            jumpPower = 0f; launchVx = 0f;
            if (gravity <= 0f) gravity = 0.4f;

            const float Tile = 16f;
            const float LandMarginTiles = 1.0f;
            const float ClearLipTiles = 1.0f;
            const float MinApexTiles = 2.5f;

            float dxPx = (dxTiles + LandMarginTiles) * Tile;
            float landingDeltaY = -riseTiles * Tile;

            float neededApexPx = Math.Max(MinApexTiles * Tile, (Math.Max(0, riseTiles) + ClearLipTiles) * Tile);
            float minPower = (float)Math.Sqrt(2f * gravity * neededApexPx);
            float startPower = Math.Max(minPower, 4f);

            float chosenVx = float.MaxValue;
            float chosenPower = startPower;
            for (float p = startPower; p <= maxJumpPower + 1e-3f; p += 0.1f)
            {
                float disc = p * p + 2f * gravity * landingDeltaY;
                if (disc < 0f) continue;
                float airtime = (p + (float)Math.Sqrt(disc)) / gravity;
                if (airtime <= 0f) continue;
                float vx = dxPx / airtime;
                if (vx <= maxLaunchVx)
                {
                    chosenPower = p;
                    chosenVx = vx;
                    break;
                }
            }

            if (chosenVx > maxLaunchVx) return false;

            jumpPower = MathHelper.Clamp(chosenPower, 4f, maxJumpPower);
            launchVx = MathHelper.Clamp(chosenVx, 0.4f, maxLaunchVx);
            return true;
        }

        private static bool ExecDrop(NavState s, Projectile proj, PlanStep step, float topSpeed, float acceleration, bool grounded, out string action, out string reason)
        {
            if (!grounded) { action = "drop-air"; reason = ""; return false; }
            if (!IsAligned(proj, step.LaunchX))
            {
                int aDir = AlignDir(proj, step.LaunchX);
                proj.direction = aDir; proj.spriteDirection = aDir;
                ApplyChase(proj, aDir, topSpeed, acceleration);
                action = "align-drop"; reason = $"launch={step.LaunchX}";
                return true;
            }

            int belowFeet = GetFeetTileY(proj) + 1;
            bool platformBelow = IsPlatformTile(step.LaunchX, belowFeet);
            bool solidBelow = !platformBelow && IsNavigationSolid(step.LaunchX, belowFeet);
            if (solidBelow)
            {
                int expiry = (int)Main.GameUpdateCount + 480;
                s.BadEdgeTargets[(step.TargetX, step.TargetY)] = expiry;
                s.StepTimer = 0;
                action = "drop-blocked"; reason = "solid-below";
                return true;
            }

            int descend = step.TargetX > step.LaunchX ? 1 : (step.TargetX < step.LaunchX ? -1 : proj.direction);
            proj.direction = descend; proj.spriteDirection = descend;
            if (platformBelow)
            {
                proj.tileCollide = false;
                proj.velocity.X = 0f;
                proj.velocity.Y = Math.Max(proj.velocity.Y, 1.6f);
                s.PlatformPassActive = true;
                s.PlatformPassTimer = 18;
                s.PlatformPassStartY = proj.Bottom.Y;
                s.AirCommitDirX = 0;
                s.CommittedLaunchVx = 0f;
                s.CommitFrames = 25;
            }
            else
            {
                ApplyChase(proj, descend, topSpeed, acceleration);
                s.AirCommitDirX = descend;
                s.AirCommitTimer = 25;
                s.CommitFrames = 25;
                s.CommittedLaunchVx = 0f;
            }
            action = "drop"; reason = $"toY={step.TargetY}";
            return true;
        }

        private static bool ExecPlatformDrop(NavState s, Projectile proj, PlanStep step, float topSpeed, float acceleration, bool grounded, out string action, out string reason)
        {
            if (!grounded || s.PlatformPassActive) { action = "pdrop-air"; reason = ""; return false; }
            if (!IsAligned(proj, step.LaunchX))
            {
                int aDir = AlignDir(proj, step.LaunchX);
                proj.direction = aDir; proj.spriteDirection = aDir;
                ApplyChase(proj, aDir, topSpeed, acceleration);
                action = "align-pdrop"; reason = $"launch={step.LaunchX}";
                return true;
            }
            proj.tileCollide = false;
            proj.velocity.Y = Math.Max(proj.velocity.Y, 1.6f);
            s.PlatformPassActive = true;
            s.PlatformPassTimer = 18;
            s.PlatformPassStartY = proj.Bottom.Y;
            s.CommitFrames = 25;
            action = "platform-drop"; reason = $"toY={step.TargetY}";
            return true;
        }

        private static bool TryLocalTerrain(NavState s, Projectile proj, int direction, float jumpCeil, float boostCeil, float topSpeed, out string action, out string reason)
        {
            int frontX = GetFrontTileX(proj, direction);
            int feetY = GetFeetTileY(proj);
            float maxLaunchVx = topSpeed + boostCeil;
            int oh = GetObstacleHeight(frontX, feetY);

            if (oh > 1)
            {
                if (oh <= 6 && HasHeadroomForJump(proj, direction, oh))
                {
                    if (ComputeJumpArc(2, oh, _planGravity, jumpCeil, maxLaunchVx, out float op, out float ovx))
                    {
                        FireJump(s, proj, direction, op, ovx, 26, 30);
                        action = "obstacle-jump"; reason = $"h={oh}";
                        return true;
                    }
                }
                proj.velocity.X *= 0.4f;
                action = "blocked"; reason = "obstacle";
                return true;
            }

            int drop = GetDropDepth(frontX, feetY, 6);
            if (drop >= 2 && TryMeasureGap(frontX, feetY, direction, out int gap, out int landDrop, out int landX))
            {
                if (gap >= 2 && gap <= 7 && landDrop <= 2)
                {
                    if (ComputeJumpArc(gap, -landDrop, _planGravity, jumpCeil, maxLaunchVx, out float gp, out float gvx))
                    {
                        FireJump(s, proj, direction, gp, gvx, 26, 30);
                        action = "gap-jump"; reason = $"gap={gap}";
                        return true;
                    }
                    proj.velocity.X *= 0.5f;
                    action = "gap-halt"; reason = "gap";
                    return true;
                }
            }

            int lookahead = Math.Abs(proj.velocity.X) > 3f ? 2 : 1;
            int edgeDrop = GetDropDepth(frontX + direction * (lookahead - 1), feetY, 6);
            if (drop >= 4 || edgeDrop >= 4)
            {
                proj.velocity.X *= 0.3f;
                if (Math.Abs(proj.velocity.X) < 1f) proj.velocity.X = 0f;
                action = "cliff-halt"; reason = "cliff";
                return true;
            }
            action = ""; reason = "";
            return false;
        }

        private static bool IsGrounded(Projectile proj)
        {
            if (proj.velocity.Y != 0f) return false;
            int left = (int)(proj.Left.X / TileF);
            int right = (int)((proj.Right.X - 1f) / TileF);
            int belowFeet = (int)((proj.Bottom.Y + 4f) / TileF);
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

        private static int GetFrontTileX(Projectile proj, int direction) =>
            direction > 0 ? (int)((proj.Right.X + 4f) / TileF) : (int)((proj.Left.X - 4f) / TileF);

        private static int GetFeetTileY(Projectile proj) => (int)((proj.Bottom.Y - 1f) / TileF);

        private static void AutoStepUp(Projectile proj)
        {
            if (proj.velocity.Y < 0f) return;
            int offset = 0;
            if (proj.velocity.X < 0f) offset = -1;
            else if (proj.velocity.X > 0f) offset = 1;
            if (offset == 0) return;

            Vector2 pos = proj.position;
            pos.X += proj.velocity.X;
            int tileX = (int)((pos.X + (proj.width / 2) + ((proj.width / 2 + 1) * offset)) / 16f);
            int tileY = (int)((pos.Y + proj.height - 1f) / 16f);
            if (!WorldGen.InWorld(tileX, tileY, 5)) return;

            Tile t = Main.tile[tileX, tileY];
            Tile tU1 = Main.tile[tileX, tileY - 1];
            Tile tU2 = Main.tile[tileX, tileY - 2];
            Tile tU3 = Main.tile[tileX, tileY - 3];
            Tile tU4 = Main.tile[tileX, tileY - 4];
            Tile tBackU3 = Main.tile[tileX - offset, tileY - 3];

            bool stepBlock = (t.HasUnactuatedTile && !t.TopSlope && !tU1.TopSlope && Main.tileSolid[t.TileType] && !Main.tileSolidTop[t.TileType])
                             || (tU1.IsHalfBlock && tU1.HasUnactuatedTile);
            bool clearU1 = !tU1.HasUnactuatedTile || !Main.tileSolid[tU1.TileType] || Main.tileSolidTop[tU1.TileType]
                           || (tU1.IsHalfBlock && (!tU4.HasUnactuatedTile || !Main.tileSolid[tU4.TileType] || Main.tileSolidTop[tU4.TileType]));
            bool clearU2 = !tU2.HasUnactuatedTile || !Main.tileSolid[tU2.TileType] || Main.tileSolidTop[tU2.TileType];
            bool clearU3 = !tU3.HasUnactuatedTile || !Main.tileSolid[tU3.TileType] || Main.tileSolidTop[tU3.TileType];
            bool clearBehind = !tBackU3.HasUnactuatedTile || !Main.tileSolid[tBackU3.TileType];

            if ((float)(tileX * 16) < pos.X + proj.width && (float)(tileX * 16 + 16) > pos.X
                && stepBlock && clearU1 && clearU2 && clearU3 && clearBehind)
            {
                float tileWorldY = tileY * 16f;
                if (t.IsHalfBlock) tileWorldY += 8f;
                if (tU1.IsHalfBlock) tileWorldY -= 8f;
                if (tileWorldY < pos.Y + proj.height)
                {
                    float tileWorldYHeight = pos.Y + proj.height - tileWorldY;
                    if (tileWorldYHeight <= 16.1f)
                    {
                        proj.position.Y = tileWorldY - proj.height;
                    }
                }
            }
        }

        private static int GetObstacleHeight(int frontX, int feetY)
        {
            if (!IsNavigationSolid(frontX, feetY)) return 0;
            int h = 1;
            while (h <= 6 && IsNavigationSolid(frontX, feetY - h)) h++;
            return h;
        }

        private static bool HasHeadroomForJump(Projectile proj, int direction, int obstacleHeight)
        {
            int frontX = GetFrontTileX(proj, direction);
            int headY = (int)((proj.Top.Y + 4f) / TileF);
            int feetY = GetFeetTileY(proj);
            int highest = Math.Max(headY - 2, feetY - obstacleHeight - 4);
            for (int y = highest; y <= headY; y++)
                if (IsNavigationSolid(frontX, y)) return false;
            return true;
        }

        private static int GetDropDepth(int x, int feetY, int maxDepth)
        {
            for (int d = 0; d <= maxDepth; d++)
                if (IsStandableTile(x, feetY + d)) return d;
            return maxDepth + 1;
        }

        private static bool TryMeasureGap(int frontX, int feetY, int direction, out int gapTiles, out int landingDrop, out int landingX)
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

        private static bool TryFindJumpEdge(Span a, Span b, int dy, out int launchX, out int landX, out int absDx)
        {
            List<int> launches = new List<int>();
            if (b.LeftX > a.RightX)
            {
                for (int x = a.RightX; x >= a.LeftX && launches.Count < 6; x--) launches.Add(x);
            }
            else if (b.RightX < a.LeftX)
            {
                for (int x = a.LeftX; x <= a.RightX && launches.Count < 6; x++) launches.Add(x);
            }
            else
            {
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
                    lands.Add(lx);
                    if (lx + 1 <= oMax) lands.Add(lx + 1);
                    if (lx - 1 >= oMin) lands.Add(lx - 1);
                }

                foreach (int la in lands)
                {
                    int adx = Math.Abs(la - lx);
                    if (!ComputeJumpArc(adx, dy, _planGravity, _planJumpCeil, _planMaxLaunchVx, out _, out _)) continue;
                    if (!HasTrajectoryClearance(lx, a.Y, la, b.Y, dy)) continue;
                    launchX = lx; landX = la; absDx = adx;
                    return true;
                }
            }
            launchX = 0; landX = 0; absDx = 0;
            return false;
        }

        private static bool HasTrajectoryClearance(int launchX, int fromY, int landX, int toY, int dy)
        {
            int higherY = Math.Min(fromY, toY);
            int apexY = higherY - Math.Max(2, Math.Abs(dy));
            int absDx = Math.Abs(landX - launchX);

            for (int y = apexY; y <= fromY - 1; y++)
                if (IsNavigationSolid(launchX, y)) return false;

            for (int y = toY - 2; y <= toY; y++)
                if (IsNavigationSolid(landX, y)) return false;

            if (absDx > 0)
            {
                int x0 = Math.Min(launchX, landX), x1 = Math.Max(launchX, landX);
                for (int x = x0 + 1; x < x1; x++)
                {
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

        private static bool IsAligned(Projectile proj, int launchX)
        {
            float c = launchX * TileF + 8f;
            return Math.Abs(proj.Center.X - c) <= AlignTolerancePx;
        }

        private static int AlignDir(Projectile proj, int launchX)
        {
            float c = launchX * TileF + 8f;
            return proj.Center.X < c ? 1 : -1;
        }

        private static void FireJump(NavState s, Projectile proj, int direction, float jumpPower, float launchVx, int airCommitFrames, int planCommitFrames)
        {
            proj.velocity.Y = -jumpPower;
            proj.velocity.X = launchVx * direction;
            proj.direction = direction;
            proj.spriteDirection = direction;
            s.AirCommitDirX = direction;
            s.AirCommitTimer = airCommitFrames;
            s.CommitFrames = planCommitFrames;
            s.CommittedLaunchVx = launchVx * direction;
            proj.netUpdate = true;
        }

        private static void ApplyChase(Projectile proj, int direction, float topSpeed, float acceleration)
        {
            float t = topSpeed * direction;
            if (proj.velocity.X < t)
            {
                proj.velocity.X += acceleration;
                if (proj.velocity.X > t) proj.velocity.X = t;
            }
            else if (proj.velocity.X > t)
            {
                proj.velocity.X -= acceleration;
                if (proj.velocity.X < t) proj.velocity.X = t;
            }
        }

        private class NavState
        {
            public List<PlanStep> Plan;
            public int PlanIndex;
            public int StepTimer;
            public int ReplanCooldown;
            public int StuckGiveUpFrames;
            public float StuckCheckX = float.MaxValue;
            public int AlignStallFrames;
            public int AirCommitTimer;
            public int AirCommitDirX;
            public float CommittedLaunchVx;
            public int CommitFrames;
            public bool PlatformPassActive;
            public int PlatformPassTimer;
            public float PlatformPassStartY;
            public int RopeStallFrames;
            public int LastRopeFeetY;
            public bool RopeJumpedThisStep;
            public bool RopeEngaged;
            public bool RopeDirLatched;
            public bool RopeDescend;
            public bool RopeDismounting;
            public bool RopeGravityDisabled;
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
