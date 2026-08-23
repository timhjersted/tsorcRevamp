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

            NavState nav = GetState(projectile);
            TickTimers(nav);
            ManagePlatformPass(nav, projectile);

            // Safety check: if tileCollide is false but we are not passing platforms, re-enable it
            if (!projectile.tileCollide && !nav.PlatformPassActive)
            {
                projectile.tileCollide = true;
            }

            bool grounded = IsGrounded(projectile);

            // Airborne X-velocity lock during a committed jump action
            if (!grounded && nav.AirCommitTimer > 0 && nav.AirCommitDirX != 0)
            {
                if (nav.CommittedLaunchVx != 0f)
                {
                    projectile.velocity.X = nav.CommittedLaunchVx;
                }
                else
                {
                    float targetVx = nav.AirCommitDirX * topSpeed * 1.05f;
                    projectile.velocity.X = MathHelper.Clamp(targetVx, -topSpeed * 1.3f, topSpeed * 1.3f);
                }
            }

            // A* replanning when needed
            if (navSearchRadius > 0)
            {
                if (!nav.IsCommitted && grounded && nav.ReplanCooldown == 0 && ShouldReplan(nav, projectile, target))
                {
                    Replan(nav, projectile, target, navSearchRadius, gravity, maxJumpPower, maxJumpBoost);
                    nav.ReplanCooldown = ReplanCooldown;
                }
            }
            else if (nav.Plan != null)
            {
                nav.Plan = null;
                nav.PlanIndex = 0;
            }

            if (nav.Plan != null && nav.PlanIndex < nav.Plan.Count)
            {
                string action, reason;
                ExecuteStep(nav, projectile, target, topSpeed, acceleration, maxJumpPower, maxJumpBoost, grounded, out action, out reason);
                if (!nav.RopeGravityDisabled && projectile.tileCollide)
                {
                    AutoStepUp(projectile);
                }
                return true;
            }

            return false;
        }

        // Returns true if gravity should be disabled for the projectile (e.g. while climbing a rope)
        public static bool IsGravityDisabled(Projectile projectile)
        {
            if (States.TryGetValue(projectile.whoAmI, out NavState nav))
            {
                return nav.RopeGravityDisabled;
            }
            return false;
        }

        private static bool IsTargetValid(Entity target)
        {
            if (target == null || !target.active)
            {
                return false;
            }
            if (target is Player player)
            {
                return !player.dead;
            }
            if (target is NPC npc)
            {
                return npc.life > 0;
            }
            return true;
        }

        private static NavState GetState(Projectile projectile)
        {
            if (!States.TryGetValue(projectile.whoAmI, out NavState nav))
            {
                nav = new NavState();
                States[projectile.whoAmI] = nav;
            }
            if (Main.GameUpdateCount % 3600 == 0 && States.Count > 64)
            {
                Prune();
            }
            return nav;
        }

        private static void Prune()
        {
            List<int> dead = new List<int>();
            foreach (var kv in States)
            {
                if (!Main.projectile[kv.Key].active)
                {
                    dead.Add(kv.Key);
                }
            }
            foreach (int projectileId in dead)
            {
                States.Remove(projectileId);
            }
        }

        private static void TickTimers(NavState nav)
        {
            if (nav.AirCommitTimer > 0)
            {
                nav.AirCommitTimer--;
            }
            if (nav.CommitFrames > 0)
            {
                nav.CommitFrames--;
            }
            if (nav.StepTimer > 0)
            {
                nav.StepTimer--;
            }
            if (nav.ReplanCooldown > 0)
            {
                nav.ReplanCooldown--;
            }
            if (nav.PlatformPassTimer > 0)
            {
                nav.PlatformPassTimer--;
            }
        }

        private static void ManagePlatformPass(NavState nav, Projectile projectile)
        {
            if (!nav.PlatformPassActive)
            {
                return;
            }
            bool timerDone = nav.PlatformPassTimer <= 0;
            bool clearedAndFalling = projectile.velocity.Y > 0.5f && projectile.Bottom.Y > nav.PlatformPassStartY + 18f;
            bool landed = IsGrounded(projectile) && projectile.velocity.Y >= 0f;
            if (timerDone || clearedAndFalling || landed)
            {
                projectile.tileCollide = true;
                nav.PlatformPassActive = false;
                nav.PlatformPassTimer = 0;
            }
        }

        private static bool ShouldReplan(NavState nav, Projectile projectile, Entity target)
        {
            if (nav.Plan == null)
            {
                return true;
            }
            if (nav.PlanIndex >= nav.Plan.Count)
            {
                return true;
            }
            if (nav.StepTimer == 0)
            {
                return true;
            }

            float planEndX = nav.Plan[nav.Plan.Count - 1].TargetX * TileF;
            float planEndY = nav.Plan[nav.Plan.Count - 1].TargetY * TileF;
            if (Math.Abs(target.Center.X - planEndX) > 16 * TileF)
            {
                return true;
            }
            if (Math.Abs(target.Center.Y - planEndY) > 10 * TileF)
            {
                return true;
            }
            return false;
        }

        private static float _planGravity = 0.4f;
        private static float _planJumpCeil = 8.5f;
        private static float _planMaxLaunchVx = 5f;

        private static void Replan(NavState nav, Projectile projectile, Entity target, int navSearchRadius, float gravity, float maxJumpPower, float maxJumpBoost)
        {
            _planGravity = gravity > 0f ? gravity : 0.4f;
            _planJumpCeil = Math.Max(maxJumpPower, 5f);
            _planMaxLaunchVx = 1.55f + Math.Max(maxJumpBoost, 2f);

            int feetY = GetFeetTileY(projectile);
            int centerX = (int)(projectile.Center.X / TileF);
            int targetFeetY = (int)((target.Bottom.Y - 1f) / TileF);
            int targetCx = (int)(target.Center.X / TileF);

            int radius = Math.Clamp(navSearchRadius, 1, ScanRadiusX);
            int yRadius = Math.Min(radius, ScanRadiusY);
            int xMin = centerX - radius, xMax = centerX + radius;
            int yMin = Math.Min(feetY, targetFeetY) - yRadius;
            int yMax = Math.Max(feetY, targetFeetY) + yRadius;

            List<Span> spans = BuildSpans(xMin, xMax, yMin, yMax);

            int now = (int)Main.GameUpdateCount;
            if (nav.BadEdgeTargets.Count > 0)
            {
                List<(int, int)> dead = new List<(int, int)>();
                foreach (var kv in nav.BadEdgeTargets)
                {
                    if (kv.Value <= now)
                    {
                        dead.Add(kv.Key);
                    }
                }
                foreach (var key in dead)
                {
                    nav.BadEdgeTargets.Remove(key);
                }
            }
            BuildEdges(spans, targetFeetY, nav.BadEdgeTargets);

            Span start = FindContainingSpan(spans, centerX, feetY);
            Span goal = FindContainingSpan(spans, targetCx, targetFeetY);
            if (start == null || goal == null)
            {
                nav.Plan = null;
                nav.PlanIndex = 0;
                return;
            }

            List<Span> path = AStar(start, goal, targetCx, targetFeetY);
            if (path == null)
            {
                nav.Plan = null;
                nav.PlanIndex = 0;
                return;
            }

            nav.Plan = ConvertToSteps(path, targetCx, targetFeetY);
            nav.PlanIndex = 0;
            nav.StepTimer = StepTimeoutFrames;
            nav.CommitFrames = 0;
        }

        private static List<Span> BuildSpans(int xMin, int xMax, int yMin, int yMax)
        {
            List<Span> spans = new List<Span>();
            for (int y = yMin; y <= yMax; y++)
            {
                int spanStart = -1;
                for (int x = xMin; x <= xMax; x++)
                {
                    bool standable = IsStandableTile(x, y + 1) && HasBodyClearanceAtRow(x, y);
                    if (standable)
                    {
                        if (spanStart == -1)
                        {
                            spanStart = x;
                        }
                    }
                    else if (spanStart != -1)
                    {
                        spans.Add(new Span(spanStart, x - 1, y));
                        spanStart = -1;
                    }
                }
                if (spanStart != -1)
                {
                    spans.Add(new Span(spanStart, xMax, y));
                }
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
                    if (150 > worst)
                    {
                        worst = 150;
                    }
                }
            }
            return worst;
        }

        private static void BuildEdges(List<Span> spans, int targetY, Dictionary<(int x, int y), int> badEdges)
        {
            Dictionary<int, List<Span>> byY = new Dictionary<int, List<Span>>();
            foreach (var span in spans)
            {
                if (!byY.TryGetValue(span.Y, out var bucket))
                {
                    bucket = new List<Span>();
                    byY[span.Y] = bucket;
                }
                bucket.Add(span);
            }

            foreach (var a in spans)
            {
                // WALK / step-hop
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (!byY.TryGetValue(a.Y + dy, out var bucket))
                    {
                        continue;
                    }
                    foreach (var b in bucket)
                    {
                        if (b == a)
                        {
                            continue;
                        }
                        bool touching = b.LeftX <= a.RightX + 1 && b.RightX >= a.LeftX - 1;
                        if (!touching)
                        {
                            continue;
                        }
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
                        if (!HasBodyClearanceAtRow(joinX, joinY))
                        {
                            continue;
                        }
                        a.Edges.Add(new Edge(b, EdgeKind.Walk, 1 + Math.Abs(dy)));
                    }
                }

                // JUMP edges
                foreach (var b in spans)
                {
                    if (b == a)
                    {
                        continue;
                    }
                    int dy = a.Y - b.Y;
                    if (Math.Abs(dy) > 8)
                    {
                        continue;
                    }
                    if (TryFindJumpEdge(a, b, dy, out int launchX, out int landX, out int absDx))
                    {
                        EdgeKind kind = dy >= 1 ? EdgeKind.JumpUp : EdgeKind.JumpGap;
                        int wrongDirPenalty = 0;
                        if (targetY < a.Y && b.Y > a.Y)
                        {
                            wrongDirPenalty = 60;
                        }
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
                    if (!byY.TryGetValue(a.Y + dy, out var bucket))
                    {
                        continue;
                    }
                    foreach (var b in bucket)
                    {
                        if (b == a)
                        {
                            continue;
                        }
                        int candidate = -1;
                        for (int x = Math.Max(a.LeftX - 1, b.LeftX); x <= Math.Min(a.RightX + 1, b.RightX); x++)
                        {
                            if (HasDropClearance(x, a.Y, b.Y))
                            {
                                candidate = x;
                                break;
                            }
                        }
                        if (candidate == -1)
                        {
                            continue;
                        }
                        int dropPenalty = targetY < a.Y - 3 ? 80 : 0;
                        int badP = BadEdgePenalty(candidate, b.Y, badEdges);
                        a.Edges.Add(new Edge(b, EdgeKind.Drop, 3 + dy + dropPenalty + badP, candidate, candidate));
                    }
                }

                // PLATFORM DROP edges
                for (int dy = 2; dy <= MaxDropDepth; dy++)
                {
                    if (!byY.TryGetValue(a.Y + dy, out var bucket))
                    {
                        continue;
                    }
                    foreach (var b in bucket)
                    {
                        if (b == a)
                        {
                            continue;
                        }
                        int candidate = -1;
                        int xMin = Math.Max(a.LeftX, b.LeftX), xMax = Math.Min(a.RightX, b.RightX);
                        for (int x = xMin; x <= xMax; x++)
                        {
                            if (IsPlatformTile(x, a.Y + 1) && HasDropClearance(x, a.Y + 2, b.Y))
                            { candidate = x; break; }
                        }
                        if (candidate == -1)
                        {
                            continue;
                        }
                        int dropPenalty = targetY < a.Y - 3 ? 80 : 0;
                        int badP = BadEdgePenalty(candidate, b.Y, badEdges);
                        a.Edges.Add(new Edge(b, EdgeKind.PlatformDrop, 4 + dy + dropPenalty + badP, candidate, candidate));
                    }
                }

                // ROPE-CLIMB edges
                for (int x = a.LeftX - 1; x <= a.RightX + 1; x++)
                {
                    if (!FindRopeSpan(x, a.Y, 5, out int ropeBottomY, out int ropeTopY))
                    {
                        continue;
                    }
                    if (ropeTopY >= a.Y)
                    {
                        continue;
                    }
                    foreach (var landingSpan in spans)
                    {
                        if (landingSpan == a)
                        {
                            continue;
                        }
                        int dyClimb = a.Y - landingSpan.Y;
                        if (dyClimb < 2 || dyClimb > MaxRopeClimb)
                        {
                            continue;
                        }
                        if (x < landingSpan.LeftX - 1 || x > landingSpan.RightX + 1)
                        {
                            continue;
                        }

                        bool valid = false;
                        int extra = 0;
                        int landX = x;

                        if (landingSpan.Y >= ropeTopY && landingSpan.Y <= ropeBottomY && RopeSideExitClear(x, landingSpan))
                        {
                            valid = true;
                            landX = Math.Clamp(x, landingSpan.LeftX, landingSpan.RightX);
                        }
                        else if (landingSpan.Y < ropeTopY)
                        {
                            int jumpRise = ropeTopY - landingSpan.Y;
                            if (jumpRise >= 1 && RopeTopJumpClear(x, ropeTopY, landingSpan)
                                && ComputeJumpArc(0, jumpRise, _planGravity, _planJumpCeil, _planMaxLaunchVx, out _, out _))
                            {
                                valid = true;
                                extra = 2 + jumpRise;
                                landX = Math.Clamp(x, landingSpan.LeftX, landingSpan.RightX);
                            }
                        }

                        if (!valid)
                        {
                            continue;
                        }
                        int badP = BadEdgePenalty(x, landingSpan.Y, badEdges);
                        a.Edges.Add(new Edge(landingSpan, EdgeKind.RopeClimb, 3 + dyClimb / 2 + extra + badP, x, landX));
                    }
                }
            }
        }

        private const int MaxRopeClimb = 200;
        private const float RopeClimbSpeed = 3.2f;

        private static bool IsRopeTile(int x, int y)
        {
            if (!WorldGen.InWorld(x, y))
            {
                return false;
            }
            Tile tile = Main.tile[x, y];
            if (!tile.HasTile || tile.IsActuated)
            {
                return false;
            }
            int tileType = tile.TileType;
            return tileType == TileID.Rope || tileType == TileID.SilkRope || tileType == TileID.WebRope
                || tileType == TileID.VineRope || tileType == TileID.Chain;
        }

        private static bool FindRopeSpan(int x, int feetY, int reach, out int bottomY, out int topY)
        {
            bottomY = topY = 0;
            int seed = int.MinValue;
            for (int y = feetY - reach; y <= feetY + reach; y++)
            {
                if (IsRopeTile(x, y))
                {
                    seed = y;
                    break;
                }
            }
            if (seed == int.MinValue)
            {
                return false;
            }
            int bottomRow = seed, topRow = seed;

            for (int y = seed + 1; y <= seed + MaxRopeClimb; y++)
            {
                if (IsRopeTile(x, y))
                {
                    bottomRow = y;
                }
                else
                {
                    break;
                }
            }

            for (int y = seed - 1; y >= seed - MaxRopeClimb; y--)
            {
                if (IsRopeTile(x, y))
                {
                    topRow = y;
                }
                else
                {
                    break;
                }
            }
            if (bottomRow - topRow < 2)
            {
                return false;
            }
            bottomY = bottomRow;
            topY = topRow;
            return true;
        }

        private static bool RopeSideExitClear(int ropeCol, Span landingSpan)
        {
            int sideX = Math.Clamp(ropeCol, landingSpan.LeftX, landingSpan.RightX);
            if (sideX == ropeCol)
            {
                return false;
            }
            return IsStandableTile(sideX, landingSpan.Y + 1) && HasBodyClearanceAtRow(sideX, landingSpan.Y);
        }

        private static bool RopeTopJumpClear(int ropeCol, int ropeTopY, Span landingSpan)
        {
            for (int y = ropeTopY - 1; y >= landingSpan.Y; y--)
            {
                if (IsNavigationSolid(ropeCol, y))
                {
                    return false;
                }
            }
            if (!IsStandableTile(ropeCol, landingSpan.Y + 1))
            {
                return false;
            }
            for (int dx = -1; dx <= 1; dx++)
            {
                if (IsNavigationSolid(ropeCol + dx, landingSpan.Y) || IsNavigationSolid(ropeCol + dx, landingSpan.Y - 1))
                {
                    return false;
                }
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
                if (solidRight && !solidLeft)
                {
                    snapX -= overhang;
                }
                else if (solidLeft && !solidRight)
                {
                    snapX += overhang;
                }
            }
            return snapX - proj.width / 2f;
        }

        private static Span FindContainingSpan(List<Span> spans, int x, int y)
        {
            foreach (var span in spans)
            {
                if (span.Y == y && x >= span.LeftX && x <= span.RightX)
                {
                    return span;
                }
            }
            // Vertical tolerance fallback
            foreach (var span in spans)
            {
                if (Math.Abs(span.Y - y) <= 1 && x >= span.LeftX && x <= span.RightX)
                {
                    return span;
                }
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
                    Span node = cur;
                    while (node != null)
                    {
                        path.Add(node);
                        cameFrom.TryGetValue(node, out node);
                    }
                    path.Reverse();
                    return path;
                }
                int curG = gScore[cur];
                foreach (var edge in cur.Edges)
                {
                    int tentative = curG + edge.Cost;
                    if (!gScore.TryGetValue(edge.To, out int existing) || tentative < existing)
                    {
                        gScore[edge.To] = tentative;
                        cameFrom[edge.To] = cur;
                        int fScore = tentative + Heuristic(edge.To, goalX, goalY);
                        open.Push(edge.To, fScore);
                    }
                }
            }
            return null;
        }

        private static int Heuristic(Span span, int goalX, int goalY)
        {
            // Horizontal gap from the goal column to this span, 0 when the goal sits inside it.
            int dx = 0;

            if (span.LeftX > goalX)
            {
                dx = span.LeftX - goalX;
            }
            else if (goalX > span.RightX)
            {
                dx = goalX - span.RightX;
            }

            int dy = Math.Abs(span.Y - goalY);
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
                foreach (var candidate in from.Edges)
                {
                    if (candidate.To == to)
                    {
                        edge = candidate;
                        break;
                    }
                }
                if (edge == null)
                {
                    continue;
                }

                int entry;
                switch (edge.Kind)
                {
                    case EdgeKind.Walk:
                        // Enter the next span at its near edge when it sits clear to one side, otherwise
                        // (the spans overlap) aim for its middle.
                        if (to.LeftX > from.RightX)
                        {
                            entry = to.LeftX;
                        }
                        else if (to.RightX < from.LeftX)
                        {
                            entry = to.RightX;
                        }
                        else
                        {
                            entry = (to.LeftX + to.RightX) / 2;
                        }

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
                int finalX = Math.Clamp(targetX, last.LeftX, last.RightX);
                steps.Add(new PlanStep(StepKind.Walk, finalX, last.Y, finalX));
            }
            return steps;
        }

        private static bool ExecuteStep(NavState nav, Projectile proj, Entity target, float topSpeed, float acceleration, float jumpCeil, float boostCeil, bool grounded, out string action, out string reason)
        {
            PlanStep step = nav.Plan[nav.PlanIndex];
            int feetY = GetFeetTileY(proj);

            float tgtPx = step.TargetX * TileF + 8f;
            bool xClose = Math.Abs(proj.Center.X - tgtPx) <= AlignTolerancePx + 4f;
            bool xNear = Math.Abs(proj.Center.X - tgtPx) <= TileF * 3f;
            bool yClose = Math.Abs(feetY - step.TargetY) <= 2;
            bool completed = false;
            switch (step.Kind)
            {
                case StepKind.Walk:
                    completed = grounded && xClose && yClose;
                    break;

                default:
                    completed = grounded && yClose && xNear;
                    break;
            }

            if (completed)
            {
                if (nav.RopeGravityDisabled)
                {
                    nav.RopeGravityDisabled = false;
                }
                nav.PlanIndex++;
                nav.StepTimer = StepTimeoutFrames;
                nav.CommitFrames = 0;
                nav.AirCommitTimer = 0;
                nav.RopeEngaged = false;
                nav.RopeDirLatched = false;
                nav.RopeDismounting = false;
                action = "step-done";
                reason = $"#{nav.PlanIndex - 1}={step.Kind}";
                return false;
            }

            if (nav.StepTimer == 0)
            {
                if (nav.RopeGravityDisabled)
                {
                    nav.RopeGravityDisabled = false;
                }
                int expiry = (int)Main.GameUpdateCount + 480;
                nav.BadEdgeTargets[(step.TargetX, step.TargetY)] = expiry;
                nav.Plan = null;
                nav.PlanIndex = 0;
                nav.CommitFrames = 0;
                nav.RopeEngaged = false;
                nav.RopeDirLatched = false;
                nav.RopeDismounting = false;
                action = "step-timeout";
                reason = $"{step.Kind} target=({step.TargetX},{step.TargetY})";
                return false;
            }

            switch (step.Kind)
            {
                case StepKind.Walk:
                    return ExecWalk(nav, proj, step, topSpeed, acceleration, jumpCeil, boostCeil, grounded, out action, out reason);
                case StepKind.JumpUp:
                case StepKind.JumpGap:
                    return ExecJump(nav, proj, step, topSpeed, acceleration, jumpCeil, boostCeil, grounded, feetY, out action, out reason);
                case StepKind.Drop:
                    return ExecDrop(nav, proj, step, topSpeed, acceleration, grounded, out action, out reason);
                case StepKind.PlatformDrop:
                    return ExecPlatformDrop(nav, proj, step, topSpeed, acceleration, grounded, out action, out reason);
                case StepKind.RopeClimb:
                    return ExecRopeClimb(nav, proj, step, topSpeed, acceleration, jumpCeil, boostCeil, grounded, out action, out reason);
            }
            action = "?";
            reason = "";
            return false;
        }

        private static bool ExecRopeClimb(NavState nav, Projectile proj, PlanStep step, float topSpeed, float acceleration, float jumpCeil, float boostCeil, bool grounded, out string action, out string reason)
        {
            float ropeCenter = step.LaunchX * TileF + 8f;
            int feetY = GetFeetTileY(proj);
            bool onRope = nav.RopeGravityDisabled;

            bool descend = nav.RopeDirLatched ? nav.RopeDescend : step.TargetY > feetY;

            if (IsRopeTile(step.LaunchX, feetY) || IsRopeTile(step.LaunchX, feetY - 1))
                nav.RopeEngaged = true;

            if (grounded && !onRope)
            {
                nav.RopeJumpedThisStep = false;
                nav.RopeDirLatched = false;
                nav.RopeDismounting = false;
            }

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
                    nav.AlignStallFrames = 0;
                    action = "rope-grab-jump";
                    reason = "rope-above";
                    return true;
                }
            }

            // Phase 1: align horizontally
            if (grounded && !onRope && !closeEnoughToGrab && Math.Abs(proj.Center.X - ropeCenter) > 4f)
            {
                int aDir = proj.Center.X < ropeCenter ? 1 : -1;
                proj.direction = aDir;
                proj.spriteDirection = aDir;
                nav.RopeStallFrames = 0;
                nav.LastRopeFeetY = feetY;
                nav.RopeEngaged = false;
                int aFrontX = GetFrontTileX(proj, aDir);
                int obstacleHeight = GetObstacleHeight(aFrontX, feetY);
                if (obstacleHeight == 2 && HasHeadroomForJump(proj, aDir, obstacleHeight)
                    && ComputeJumpArc(2, obstacleHeight, _planGravity, jumpCeil, topSpeed + boostCeil, out float hopPower, out float hopVx))
                {
                    FireJump(nav, proj, aDir, hopPower, hopVx, 16, 12);
                    nav.AlignStallFrames = 0;
                    action = "align-rope-hop";
                    reason = $"oh={obstacleHeight}";
                    return true;
                }
                if (Math.Abs(proj.velocity.X) < 0.1f && obstacleHeight > 2)
                {
                    nav.AlignStallFrames++;
                    if (nav.AlignStallFrames > 18)
                    {
                        nav.StepTimer = 0;
                        nav.AlignStallFrames = 0;
                    }
                }
                else
                {
                    nav.AlignStallFrames = 0;
                }
                ApplyChase(proj, aDir, topSpeed, acceleration);
                action = "align-rope";
                reason = $"ropeX={step.LaunchX}";
                return true;
            }
            nav.AlignStallFrames = 0;

            bool atRopeEnd = nav.RopeEngaged && (descend
                ? !IsRopeTile(step.LaunchX, feetY + 1)
                : !IsRopeTile(step.LaunchX, feetY - 1));
            bool reachedTarget = descend ? feetY >= step.TargetY : feetY <= step.TargetY;

            bool ceilingCapped = !descend && nav.RopeEngaged
                && IsNavigationSolid(step.LaunchX, feetY - 2) && !IsRopeTile(step.LaunchX, feetY - 2);

            bool noProgress = descend ? feetY <= nav.LastRopeFeetY : feetY >= nav.LastRopeFeetY;
            bool blocked = onRope && noProgress;
            if (blocked)
            {
                nav.RopeStallFrames++;
            }
            else
            {
                nav.RopeStallFrames = 0;
            }
            bool forceDismount = nav.RopeStallFrames > 12;

            if (atRopeEnd || reachedTarget || ceilingCapped || forceDismount)
            {
                nav.RopeGravityDisabled = false;
                nav.RopeDirLatched = false;
                nav.RopeDismounting = true;
                proj.velocity.Y = 0f;

                if (ceilingCapped || forceDismount)
                {
                    int exitDir = proj.direction;
                    proj.velocity.Y = -5f;
                    proj.velocity.X = 1.5f * exitDir;
                    nav.CommitFrames = 20;
                    action = "rope-abort-jump";
                    reason = ceilingCapped ? "ceiling" : "stall";
                    return true;
                }

                // Normal dismount step off
                // Step off toward the landing span; keep current facing when the target IS the launch column.
                int dDir = proj.direction;

                if (step.TargetX > step.LaunchX)
                {
                    dDir = 1;
                }
                else if (step.TargetX < step.LaunchX)
                {
                    dDir = -1;
                }

                if (!descend)
                {
                    proj.velocity.Y = -5.5f; // Small hop upward to clear platform lip
                    proj.velocity.X = dDir * topSpeed * 1.2f; // Extra forward push
                    nav.CommitFrames = 15;
                }
                else
                {
                    proj.velocity.X = dDir * topSpeed;
                    nav.CommitFrames = 10;
                }
                proj.direction = dDir;
                proj.spriteDirection = dDir;
                action = "rope-dismount";
                reason = atRopeEnd ? "end" : "reached";
                return true;
            }

            // We've already fired an intentional dismount (reached target / ran off the end / aborted) this
            // step — do NOT let Phase 2 re-grab and re-snap X back onto the rope. That re-snap erases the
            // step-off velocity and pins us beside the rope, and the dismount<->re-grab alternation is the
            // left/right vibration. Let the committed step-off velocity carry clear; the step completes once
            // grounded near the target. (Mirrors the SF4 RopeDismounting guard.)
            if (!onRope && (nav.RopeDismounting || (nav.RopeEngaged && !ropeAtFeet)))
            {
                action = "rope-detached";
                reason = nav.RopeDismounting ? "dismounting" : "off-rope";
                return true;
            }

            // Phase 2: ride rope
            nav.RopeGravityDisabled = true;
            nav.RopeDirLatched = true;
            nav.RopeDescend = descend;

            // Snap center
            proj.position.X = RopeSnapX(proj, step.LaunchX, ropeCenter, feetY);
            proj.velocity.X = 0f;
            proj.velocity.Y = descend ? RopeClimbSpeed : -RopeClimbSpeed;

            bool progressed = feetY != nav.LastRopeFeetY;
            if (progressed)
            {
                nav.StepTimer = StepTimeoutFrames;
            }
            nav.LastRopeFeetY = feetY;
            action = descend ? "rope-descend" : "rope-climb";
            reason = $"feetY={feetY}->toY={step.TargetY}";
            return true;
        }

        private static bool ExecWalk(NavState nav, Projectile proj, PlanStep step, float topSpeed, float acceleration, float jumpCeil, float boostCeil, bool grounded, out string action, out string reason)
        {
            float tgtPx = step.TargetX * TileF + 8f;
            // Face the way we need to travel. Inside the alignment tolerance keep the current facing, so
            // the sprite does not flip back and forth while settling onto the launch column.
            int dir = proj.direction;

            if (proj.Center.X < tgtPx - AlignTolerancePx * 0.5f)
            {
                dir = 1;
            }
            else if (proj.Center.X > tgtPx + AlignTolerancePx * 0.5f)
            {
                dir = -1;
            }

            proj.direction = dir;
            proj.spriteDirection = dir;
            if (!grounded)
            {
                action = "walk-air";
                reason = "";
                return false;
            }

            if (TryLocalTerrain(nav, proj, dir, jumpCeil, boostCeil, topSpeed, out action, out reason))
            {
                if (action == "blocked")
                {
                    nav.StepTimer = 0;
                }
                return true;
            }
            ApplyChase(proj, dir, topSpeed, acceleration);
            action = "walk";
            reason = $"->{step.TargetX}";
            return true;
        }

        private static bool ExecJump(NavState nav, Projectile proj, PlanStep step, float topSpeed, float acceleration, float jumpCeil, float boostCeil, bool grounded, int feetY, out string action, out string reason)
        {
            if (!grounded)
            {
                if (Math.Abs(proj.velocity.Y) < 0.5f && Math.Abs(proj.velocity.X) < 0.5f)
                {
                    int expiry = (int)Main.GameUpdateCount + 480;
                    nav.BadEdgeTargets[(step.TargetX, step.TargetY)] = expiry;
                    nav.StepTimer = Math.Min(nav.StepTimer, 8);
                }
                action = "jump-air";
                reason = $"commit={nav.CommitFrames}";
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
                proj.direction = aDir;
                proj.spriteDirection = aDir;
                ApplyChase(proj, aDir, topSpeed, acceleration);
                if (Math.Abs(proj.velocity.X) < 0.1f)
                {
                    nav.AlignStallFrames++;
                    if (nav.AlignStallFrames > 18)
                    {
                        nav.StepTimer = 0;
                        nav.AlignStallFrames = 0;
                    }
                }
                else
                {
                    nav.AlignStallFrames = 0;
                }
                action = "align-jump";
                reason = $"launch={step.LaunchX}";
                return true;
            }
            nav.AlignStallFrames = 0;

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
                nav.PlatformPassActive = true;
                nav.PlatformPassTimer = 24;
                nav.PlatformPassStartY = proj.Bottom.Y;
                nav.CommitFrames = 25;
                nav.AirCommitDirX = 0;
                nav.CommittedLaunchVx = 0f;
                action = "jump-dropdown";
                reason = $"down rise={rise}";
                return true;
            }

            float maxLaunchVx = topSpeed + boostCeil;
            int launchDir = proj.direction;

            if (step.TargetX > step.LaunchX)
            {
                launchDir = 1;
            }
            else if (step.TargetX < step.LaunchX)
            {
                launchDir = -1;
            }

            bool feasible = ComputeJumpArc(absDx, rise, _planGravity, jumpCeil, maxLaunchVx, out float power, out float launchVx);
            if (!feasible)
            {
                nav.StepTimer = 0;
                proj.velocity.X *= 0.5f;
                action = "jump-abort";
                reason = "infeasible";
                return true;
            }

            FireJump(nav, proj, launchDir, power, launchVx, 35, 45);
            if (isVertical)
            {
                proj.velocity.X = 0f;
                nav.AirCommitDirX = 0;
                nav.CommittedLaunchVx = 0f;
            }
            action = "jump-fire";
            reason = $"rise={rise}";
            return true;
        }

        private static bool ComputeJumpArc(int dxTiles, int riseTiles, float gravity, float maxJumpPower, float maxLaunchVx, out float jumpPower, out float launchVx)
        {
            jumpPower = 0f;
            launchVx = 0f;
            if (gravity <= 0f)
            {
                gravity = 0.4f;
            }

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
            for (float power = startPower; power <= maxJumpPower + 1e-3f; power += 0.1f)
            {
                float disc = power * power + 2f * gravity * landingDeltaY;
                if (disc < 0f)
                {
                    continue;
                }
                float airtime = (power + (float)Math.Sqrt(disc)) / gravity;
                if (airtime <= 0f)
                {
                    continue;
                }
                float vx = dxPx / airtime;
                if (vx <= maxLaunchVx)
                {
                    chosenPower = power;
                    chosenVx = vx;
                    break;
                }
            }

            if (chosenVx > maxLaunchVx)
            {
                return false;
            }

            jumpPower = MathHelper.Clamp(chosenPower, 4f, maxJumpPower);
            launchVx = MathHelper.Clamp(chosenVx, 0.4f, maxLaunchVx);
            return true;
        }

        private static bool ExecDrop(NavState nav, Projectile proj, PlanStep step, float topSpeed, float acceleration, bool grounded, out string action, out string reason)
        {
            if (!grounded)
            {
                action = "drop-air";
                reason = "";
                return false;
            }
            if (!IsAligned(proj, step.LaunchX))
            {
                int aDir = AlignDir(proj, step.LaunchX);
                proj.direction = aDir;
                proj.spriteDirection = aDir;
                ApplyChase(proj, aDir, topSpeed, acceleration);
                action = "align-drop";
                reason = $"launch={step.LaunchX}";
                return true;
            }

            int belowFeet = GetFeetTileY(proj) + 1;
            bool platformBelow = IsPlatformTile(step.LaunchX, belowFeet);
            bool solidBelow = !platformBelow && IsNavigationSolid(step.LaunchX, belowFeet);
            if (solidBelow)
            {
                int expiry = (int)Main.GameUpdateCount + 480;
                nav.BadEdgeTargets[(step.TargetX, step.TargetY)] = expiry;
                nav.StepTimer = 0;
                action = "drop-blocked";
                reason = "solid-below";
                return true;
            }

            int descend = proj.direction;

            if (step.TargetX > step.LaunchX)
            {
                descend = 1;
            }
            else if (step.TargetX < step.LaunchX)
            {
                descend = -1;
            }

            proj.direction = descend;
            proj.spriteDirection = descend;
            if (platformBelow)
            {
                proj.tileCollide = false;
                proj.velocity.X = 0f;
                proj.velocity.Y = Math.Max(proj.velocity.Y, 1.6f);
                nav.PlatformPassActive = true;
                nav.PlatformPassTimer = 18;
                nav.PlatformPassStartY = proj.Bottom.Y;
                nav.AirCommitDirX = 0;
                nav.CommittedLaunchVx = 0f;
                nav.CommitFrames = 25;
            }
            else
            {
                ApplyChase(proj, descend, topSpeed, acceleration);
                nav.AirCommitDirX = descend;
                nav.AirCommitTimer = 25;
                nav.CommitFrames = 25;
                nav.CommittedLaunchVx = 0f;
            }
            action = "drop";
            reason = $"toY={step.TargetY}";
            return true;
        }

        private static bool ExecPlatformDrop(NavState nav, Projectile proj, PlanStep step, float topSpeed, float acceleration, bool grounded, out string action, out string reason)
        {
            if (!grounded || nav.PlatformPassActive)
            {
                action = "pdrop-air";
                reason = "";
                return false;
            }
            if (!IsAligned(proj, step.LaunchX))
            {
                int aDir = AlignDir(proj, step.LaunchX);
                proj.direction = aDir;
                proj.spriteDirection = aDir;
                ApplyChase(proj, aDir, topSpeed, acceleration);
                action = "align-pdrop";
                reason = $"launch={step.LaunchX}";
                return true;
            }
            proj.tileCollide = false;
            proj.velocity.Y = Math.Max(proj.velocity.Y, 1.6f);
            nav.PlatformPassActive = true;
            nav.PlatformPassTimer = 18;
            nav.PlatformPassStartY = proj.Bottom.Y;
            nav.CommitFrames = 25;
            action = "platform-drop";
            reason = $"toY={step.TargetY}";
            return true;
        }

        private static bool TryLocalTerrain(NavState nav, Projectile proj, int direction, float jumpCeil, float boostCeil, float topSpeed, out string action, out string reason)
        {
            int frontX = GetFrontTileX(proj, direction);
            int feetY = GetFeetTileY(proj);
            float maxLaunchVx = topSpeed + boostCeil;
            int obstacleHeight = GetObstacleHeight(frontX, feetY);

            if (obstacleHeight > 1)
            {
                if (obstacleHeight <= 6 && HasHeadroomForJump(proj, direction, obstacleHeight))
                {
                    if (ComputeJumpArc(2, obstacleHeight, _planGravity, jumpCeil, maxLaunchVx, out float obstaclePower, out float obstacleVx))
                    {
                        FireJump(nav, proj, direction, obstaclePower, obstacleVx, 26, 30);
                        action = "obstacle-jump";
                        reason = $"h={obstacleHeight}";
                        return true;
                    }
                }
                proj.velocity.X *= 0.4f;
                action = "blocked";
                reason = "obstacle";
                return true;
            }

            int drop = GetDropDepth(frontX, feetY, 6);
            if (drop >= 2 && TryMeasureGap(frontX, feetY, direction, out int gap, out int landDrop, out int landX))
            {
                if (gap >= 2 && gap <= 7 && landDrop <= 2)
                {
                    if (ComputeJumpArc(gap, -landDrop, _planGravity, jumpCeil, maxLaunchVx, out float gapPower, out float gapVx))
                    {
                        FireJump(nav, proj, direction, gapPower, gapVx, 26, 30);
                        action = "gap-jump";
                        reason = $"gap={gap}";
                        return true;
                    }
                    proj.velocity.X *= 0.5f;
                    action = "gap-halt";
                    reason = "gap";
                    return true;
                }
            }

            int lookahead = Math.Abs(proj.velocity.X) > 3f ? 2 : 1;
            int edgeDrop = GetDropDepth(frontX + direction * (lookahead - 1), feetY, 6);
            if (drop >= 4 || edgeDrop >= 4)
            {
                proj.velocity.X *= 0.3f;
                if (Math.Abs(proj.velocity.X) < 1f)
                {
                    proj.velocity.X = 0f;
                }
                action = "cliff-halt";
                reason = "cliff";
                return true;
            }
            action = "";
            reason = "";
            return false;
        }

        private static bool IsGrounded(Projectile proj)
        {
            if (proj.velocity.Y != 0f)
            {
                return false;
            }
            int left = (int)(proj.Left.X / TileF);
            int right = (int)((proj.Right.X - 1f) / TileF);
            int belowFeet = (int)((proj.Bottom.Y + 4f) / TileF);
            for (int x = left; x <= right; x++)
                if (IsStandableTile(x, belowFeet))
                {
                    return true;
                }
            return false;
        }

        private static bool IsStandableTile(int x, int y)
        {
            if (!WorldGen.InWorld(x, y))
            {
                return false;
            }
            Tile tile = Main.tile[x, y];
            if (!tile.HasTile || tile.IsActuated)
            {
                return false;
            }
            return IsNavigationSolid(x, y) || IsPlatformTile(x, y);
        }

        private static int GetFrontTileX(Projectile proj, int direction) =>
            direction > 0 ? (int)((proj.Right.X + 4f) / TileF) : (int)((proj.Left.X - 4f) / TileF);

        private static int GetFeetTileY(Projectile proj) => (int)((proj.Bottom.Y - 1f) / TileF);

        private static void AutoStepUp(Projectile proj)
        {
            if (proj.velocity.Y < 0f)
            {
                return;
            }
            int offset = 0;
            if (proj.velocity.X < 0f)
            {
                offset = -1;
            }
            else if (proj.velocity.X > 0f)
            {
                offset = 1;
            }
            if (offset == 0)
            {
                return;
            }

            Vector2 pos = proj.position;
            pos.X += proj.velocity.X;
            int tileX = (int)((pos.X + (proj.width / 2) + ((proj.width / 2 + 1) * offset)) / 16f);
            int tileY = (int)((pos.Y + proj.height - 1f) / 16f);
            if (!WorldGen.InWorld(tileX, tileY, 5))
            {
                return;
            }

            Tile tile = Main.tile[tileX, tileY];
            Tile tU1 = Main.tile[tileX, tileY - 1];
            Tile tU2 = Main.tile[tileX, tileY - 2];
            Tile tU3 = Main.tile[tileX, tileY - 3];
            Tile tU4 = Main.tile[tileX, tileY - 4];
            Tile tBackU3 = Main.tile[tileX - offset, tileY - 3];

            bool stepBlock = (tile.HasUnactuatedTile && !tile.TopSlope && !tU1.TopSlope && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType])
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
                if (tile.IsHalfBlock)
                {
                    tileWorldY += 8f;
                }
                if (tU1.IsHalfBlock)
                {
                    tileWorldY -= 8f;
                }
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
            if (!IsNavigationSolid(frontX, feetY))
            {
                return 0;
            }
            int height = 1;
            while (height <= 6 && IsNavigationSolid(frontX, feetY - height))
            {
                height++;
            }
            return height;
        }

        private static bool HasHeadroomForJump(Projectile proj, int direction, int obstacleHeight)
        {
            int frontX = GetFrontTileX(proj, direction);
            int headY = (int)((proj.Top.Y + 4f) / TileF);
            int feetY = GetFeetTileY(proj);
            int highest = Math.Max(headY - 2, feetY - obstacleHeight - 4);
            for (int y = highest; y <= headY; y++)
                if (IsNavigationSolid(frontX, y))
                {
                    return false;
                }
            return true;
        }

        private static int GetDropDepth(int x, int feetY, int maxDepth)
        {
            for (int depth = 0; depth <= maxDepth; depth++)
                if (IsStandableTile(x, feetY + depth))
                {
                    return depth;
                }
            return maxDepth + 1;
        }

        private static bool TryMeasureGap(int frontX, int feetY, int direction, out int gapTiles, out int landingDrop, out int landingX)
        {
            gapTiles = 0;
            landingDrop = 0;
            landingX = frontX;
            for (int offset = 1; offset <= 7; offset++)
            {
                int columnX = frontX + direction * offset;
                int depth = GetDropDepth(columnX, feetY, 5);
                if (depth <= 2 && HasBodyClearanceAtRow(columnX, feetY + depth))
                {
                    gapTiles = offset;
                    landingDrop = depth;
                    landingX = columnX;
                    return true;
                }
            }
            return false;
        }

        private static bool HasBodyClearanceAtRow(int x, int feetY)
        {
            for (int y = feetY - 2; y <= feetY; y++)
                if (IsNavigationSolid(x, y))
                {
                    return false;
                }
            return true;
        }

        private static bool TryFindJumpEdge(Span a, Span b, int dy, out int launchX, out int landX, out int absDx)
        {
            List<int> launches = new List<int>();
            if (b.LeftX > a.RightX)
            {
                for (int x = a.RightX; x >= a.LeftX && launches.Count < 6; x--)
                {
                    launches.Add(x);
                }
            }
            else if (b.RightX < a.LeftX)
            {
                for (int x = a.LeftX; x <= a.RightX && launches.Count < 6; x++)
                {
                    launches.Add(x);
                }
            }
            else
            {
                int oMin = Math.Max(a.LeftX, b.LeftX);
                int oMax = Math.Min(a.RightX, b.RightX);
                int mid = (oMin + oMax) / 2;
                launches.Add(mid);
                for (int offset = 1; offset <= 4 && launches.Count < 6; offset++)
                {
                    if (mid + offset <= oMax)
                    {
                        launches.Add(mid + offset);
                    }
                    if (mid - offset >= oMin)
                    {
                        launches.Add(mid - offset);
                    }
                }
            }

            foreach (int launchCol in launches)
            {
                List<int> lands = new List<int>();
                if (b.LeftX > a.RightX)
                {
                    for (int x = b.LeftX; x <= b.RightX && lands.Count < 6; x++)
                    {
                        lands.Add(x);
                    }
                }
                else if (b.RightX < a.LeftX)
                {
                    for (int x = b.RightX; x >= b.LeftX && lands.Count < 6; x--)
                    {
                        lands.Add(x);
                    }
                }
                else
                {
                    int oMin = Math.Max(a.LeftX, b.LeftX);
                    int oMax = Math.Min(a.RightX, b.RightX);
                    lands.Add(launchCol);
                    if (launchCol + 1 <= oMax)
                    {
                        lands.Add(launchCol + 1);
                    }
                    if (launchCol - 1 >= oMin)
                    {
                        lands.Add(launchCol - 1);
                    }
                }

                foreach (int landCol in lands)
                {
                    int adx = Math.Abs(landCol - launchCol);
                    if (!ComputeJumpArc(adx, dy, _planGravity, _planJumpCeil, _planMaxLaunchVx, out _, out _))
                    {
                        continue;
                    }
                    if (!HasTrajectoryClearance(launchCol, a.Y, landCol, b.Y, dy))
                    {
                        continue;
                    }
                    launchX = launchCol;
                    landX = landCol;
                    absDx = adx;
                    return true;
                }
            }
            launchX = 0;
            landX = 0;
            absDx = 0;
            return false;
        }

        private static bool HasTrajectoryClearance(int launchX, int fromY, int landX, int toY, int dy)
        {
            int higherY = Math.Min(fromY, toY);
            int apexY = higherY - Math.Max(2, Math.Abs(dy));
            int absDx = Math.Abs(landX - launchX);

            for (int y = apexY; y <= fromY - 1; y++)
                if (IsNavigationSolid(launchX, y))
                {
                    return false;
                }

            for (int y = toY - 2; y <= toY; y++)
                if (IsNavigationSolid(landX, y))
                {
                    return false;
                }

            if (absDx > 0)
            {
                int x0 = Math.Min(launchX, landX), x1 = Math.Max(launchX, landX);
                for (int x = x0 + 1; x < x1; x++)
                {
                    for (int y = apexY; y <= apexY + 2; y++)
                        if (IsNavigationSolid(x, y))
                        {
                            return false;
                        }
                }
            }
            return true;
        }

        private static bool HasDropClearance(int x, int fromY, int toY)
        {
            for (int y = fromY; y <= toY; y++)
                if (IsNavigationSolid(x, y))
                {
                    return false;
                }
            return true;
        }

        private static bool IsPlatformTile(int x, int y)
        {
            if (!WorldGen.InWorld(x, y))
            {
                return false;
            }
            Tile tile = Main.tile[x, y];
            return tile.HasTile && !tile.IsActuated && TileID.Sets.Platforms[tile.TileType];
        }

        private static bool IsNavigationSolid(int x, int y)
        {
            if (!WorldGen.InWorld(x, y))
            {
                return false;
            }
            Tile tile = Main.tile[x, y];
            if (!tile.HasTile || tile.IsActuated || TileID.Sets.Platforms[tile.TileType])
            {
                return false;
            }
            if (!Main.tileSolid[tile.TileType])
            {
                return false;
            }
            return !Main.tileFrameImportant[tile.TileType] || tile.TileType == TileID.ClosedDoor;
        }

        private static bool IsAligned(Projectile proj, int launchX)
        {
            float launchCenterX = launchX * TileF + 8f;
            return Math.Abs(proj.Center.X - launchCenterX) <= AlignTolerancePx;
        }

        private static int AlignDir(Projectile proj, int launchX)
        {
            float launchCenterX = launchX * TileF + 8f;
            return proj.Center.X < launchCenterX ? 1 : -1;
        }

        private static void FireJump(NavState nav, Projectile proj, int direction, float jumpPower, float launchVx, int airCommitFrames, int planCommitFrames)
        {
            proj.velocity.Y = -jumpPower;
            proj.velocity.X = launchVx * direction;
            proj.direction = direction;
            proj.spriteDirection = direction;
            nav.AirCommitDirX = direction;
            nav.AirCommitTimer = airCommitFrames;
            nav.CommitFrames = planCommitFrames;
            nav.CommittedLaunchVx = launchVx * direction;
            proj.netUpdate = true;
        }

        private static void ApplyChase(Projectile proj, int direction, float topSpeed, float acceleration)
        {
            float targetVx = topSpeed * direction;
            if (proj.velocity.X < targetVx)
            {
                proj.velocity.X += acceleration;
                if (proj.velocity.X > targetVx)
                {
                    proj.velocity.X = targetVx;
                }
            }
            else if (proj.velocity.X > targetVx)
            {
                proj.velocity.X -= acceleration;
                if (proj.velocity.X < targetVx)
                {
                    proj.velocity.X = targetVx;
                }
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
            public PlanStep(StepKind kind, int targetX, int targetY, int launchX) { Kind = kind; TargetX = targetX; TargetY = targetY; LaunchX = launchX; }
        }

        private enum EdgeKind { Walk, JumpUp, JumpGap, Drop, PlatformDrop, RopeClimb }
        private class Edge
        {
            public Span To;
            public EdgeKind Kind;
            public int Cost;
            public int LaunchX, LandX;
            public Edge(Span to, EdgeKind kind, int cost, int launchX = 0, int landX = 0)
            { To = to; Kind = kind; Cost = cost; LaunchX = launchX; LandX = landX; }
        }

        private class Span
        {
            public int LeftX, RightX, Y;
            public List<Edge> Edges = new List<Edge>();
            public Span(int left, int right, int y) { LeftX = left; RightX = right; Y = y; }
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
                    int parent = (i - 1) / 2;
                    if (heap[parent].prio <= heap[i].prio)
                    {
                        break;
                    }
                    (heap[parent], heap[i]) = (heap[i], heap[parent]);
                    i = parent;
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
                    int leftChild = i * 2 + 1, rightChild = leftChild + 1, smallest = i;
                    if (leftChild < heap.Count && heap[leftChild].prio < heap[smallest].prio)
                    {
                        smallest = leftChild;
                    }
                    if (rightChild < heap.Count && heap[rightChild].prio < heap[smallest].prio)
                    {
                        smallest = rightChild;
                    }
                    if (smallest == i)
                    {
                        break;
                    }
                    (heap[smallest], heap[i]) = (heap[i], heap[smallest]);
                    i = smallest;
                }
                return top;
            }
        }
    }
}
