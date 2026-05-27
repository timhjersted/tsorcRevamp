using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace tsorcRevamp.NPCs
{
    public static class PathFighter2AI
    {
        private const int TileSize = 16;
        private const int MaxRouteNodes = 14;
        private const int RepathInterval = 45;
        private const int StaleRouteDistance = 12;
        private const int SearchHalfWidth = 70;
        private const int SearchHalfHeight = 42;
        private const int BadEdgeMemoryTicks = 900;
        private static readonly Dictionary<int, Brain> Brains = new Dictionary<int, Brain>();

        public static void Run(NPC npc, float topSpeed = 1.55f, float acceleration = 0.06f, float attackRange = 700f)
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

            Brain brain = GetBrain(npc);
            tsorcRevampGlobalNPC globalNPC = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();
            if (globalNPC.NavJumpCooldown > 0)
            {
                globalNPC.NavJumpCooldown--;
            }
            if (brain.PassCooldown > 0)
            {
                brain.PassCooldown--;
            }
            brain.TickBadEdges();

            bool grounded = IsGrounded(npc);
            bool lineOfSight = Collision.CanHit(npc.position, npc.width, npc.height, player.position, player.width, player.height);
            Point npcNode = FindNearestStandNode((int)(npc.Center.X / TileSize), GetFeetTileY(npc), 4, 8);
            Point targetNode = FindPlayerTargetNode(player, npcNode);

            brain.RepathTimer--;
            bool routeInvalid = brain.Route.Count == 0
                || brain.RepathTimer <= 0
                || DistanceTiles(brain.TargetNode, targetNode) > StaleRouteDistance
                || brain.RouteIndex >= brain.Route.Count
                || npcNode == Point.Zero;

            if (routeInvalid)
            {
                BuildRoute(brain, npc, npcNode, targetNode);
            }

            PathDebug debug = new PathDebug
            {
                Grounded = grounded,
                LineOfSight = lineOfSight,
                TargetNode = targetNode,
                NpcNode = npcNode,
                RouteCount = brain.Route.Count,
                RouteIndex = brain.RouteIndex,
                Action = "none",
                Reason = "none"
            };

            bool movementAction = ExecuteRoute(npc, globalNPC, brain, player, npcNode, grounded, topSpeed, acceleration, ref debug);

            bool canUseProjectile = globalNPC.AttackList.Count > 0
                && lineOfSight
                && npc.Distance(player.Center) <= attackRange
                && !movementAction;

            bool oldCanStopToFire = globalNPC.CanStopToFire;
            globalNPC.CanStopToFire = false;
            if (globalNPC.AttackList.Count > 0)
            {
                tsorcRevampAIs.SimpleProjectile(npc, canUseProjectile);
            }
            globalNPC.CanStopToFire = oldCanStopToFire;

            debug.AttackAllowed = canUseProjectile;
            Log(npc, player, brain, debug);
        }

        public static void Reset(NPC npc)
        {
            Brains.Remove(npc.whoAmI);
        }

        private static Brain GetBrain(NPC npc)
        {
            if (!Brains.TryGetValue(npc.whoAmI, out Brain brain) || brain.NpcType != npc.type)
            {
                brain = new Brain
                {
                    NpcType = npc.type,
                    RepathTimer = 0,
                    LastMoveDirection = npc.direction == 0 ? 1 : npc.direction
                };
                Brains[npc.whoAmI] = brain;
            }

            return brain;
        }

        private static bool ExecuteRoute(NPC npc, tsorcRevampGlobalNPC globalNPC, Brain brain, Player player, Point npcNode, bool grounded, float topSpeed, float acceleration, ref PathDebug debug)
        {
            if (brain.PassTimer > 0)
            {
                brain.PassTimer--;
                npc.noTileCollide = true;
                npc.noGravity = true;
                npc.velocity.X = brain.PassDirection * MathHelper.Clamp(topSpeed * 1.15f, 1.1f, 2.2f);
                npc.velocity.Y = 0f;
                debug.Action = "sprite-pass";
                debug.Reason = $"timer={brain.PassTimer}";
                debug.OverrideHorizontal = true;
                return true;
            }

            npc.noGravity = false;
            npc.noTileCollide = false;

            bool pseudoGrounded = grounded || (npc.collideY && Math.Abs(npc.velocity.Y) < 0.2f);
            if (brain.Action != MoveActionKind.None)
            {
                if (ExecuteCommittedAction(npc, globalNPC, brain, npcNode, pseudoGrounded, topSpeed, acceleration, ref debug))
                {
                    return true;
                }
            }

            if (TryRecoverBlockedMovement(npc, globalNPC, brain, pseudoGrounded, topSpeed, ref debug))
            {
                return true;
            }

            if (brain.Route.Count == 0 || brain.RouteIndex >= brain.Route.Count)
            {
                int direct = StableDirection(npc, brain, player.Center.X - npc.Center.X, Math.Abs(player.Center.Y - npc.Center.Y) > 72f);
                ApplyHorizontal(npc, direct, topSpeed, acceleration);
                debug.Action = "direct";
                debug.Reason = "no-route";
                return false;
            }

            AdvanceRouteIndex(npc, brain, npcNode);
            if (brain.RouteIndex >= brain.Route.Count)
            {
                int direct = StableDirection(npc, brain, player.Center.X - npc.Center.X, false);
                ApplyHorizontal(npc, direct, topSpeed, acceleration);
                debug.Action = "direct";
                debug.Reason = "route-complete";
                return false;
            }

            Point next = brain.Route[brain.RouteIndex];
            Point after = brain.RouteIndex + 1 < brain.Route.Count ? brain.Route[brain.RouteIndex + 1] : next;
            debug.NextNode = next;
            debug.Edge = ClassifyEdge(npcNode, next);

            int direction = DirectionToNode(npc, brain, next, after);
            brain.LastMoveDirection = direction;
            npc.direction = direction;
            npc.spriteDirection = direction;

            StartCommittedAction(npc, globalNPC, brain, npcNode, next, debug.Edge, direction, pseudoGrounded, topSpeed, ref debug);
            if (brain.Action != MoveActionKind.None)
            {
                return ExecuteCommittedAction(npc, globalNPC, brain, npcNode, pseudoGrounded, topSpeed, acceleration, ref debug);
            }

            ApplyHorizontal(npc, direction, topSpeed, acceleration);
            debug.Action = "route-walk";
            debug.Reason = $"next=({next.X},{next.Y})";
            return true;
        }

        private static void StartCommittedAction(NPC npc, tsorcRevampGlobalNPC globalNPC, Brain brain, Point from, Point target, string edge, int direction, bool grounded, float topSpeed, ref PathDebug debug)
        {
            brain.Action = edge == "jump" ? MoveActionKind.Jump : edge == "drop" ? MoveActionKind.Drop : MoveActionKind.Walk;
            brain.ActionTarget = target;
            brain.ActionStart = from;
            brain.ActionDirection = direction == 0 ? 1 : direction;
            int dxTiles = Math.Abs(target.X - from.X);
            brain.ActionTimer = brain.Action == MoveActionKind.Jump ? 64 : brain.Action == MoveActionKind.Drop ? 46 : Math.Max(24, Math.Min(80, 18 + dxTiles * 9));
            brain.ActionBlockedFrames = 0;
            brain.ActionStartX = npc.Center.X;
            brain.ActionStartY = npc.Center.Y;

            if (brain.Action == MoveActionKind.Jump && grounded)
            {
                int dx = Math.Abs(target.X - from.X);
                int rise = Math.Max(0, from.Y - target.Y);
                float jumpPower = MathHelper.Clamp(5.25f + rise * 0.5f + dx * 0.17f, 5.5f, globalNPC.MaxJumpPower);
                float boost = MathHelper.Clamp(0.95f + dx * 0.24f, 1.05f, globalNPC.MaxJumpBoost);
                npc.velocity.Y = -jumpPower;
                npc.velocity.X = brain.ActionDirection * Math.Max(Math.Abs(npc.velocity.X), boost);
                globalNPC.NavJumpCooldown = 16 + Math.Min(dx, 10);
                npc.netUpdate = true;
                debug.JumpPower = jumpPower;
                debug.Boost = boost;
            }
            else if (brain.Action == MoveActionKind.Drop && IsStandingOnPlatform(npc))
            {
                npc.noTileCollide = true;
                npc.velocity.Y = Math.Max(npc.velocity.Y, 1.6f);
            }
        }

        private static bool ExecuteCommittedAction(NPC npc, tsorcRevampGlobalNPC globalNPC, Brain brain, Point npcNode, bool grounded, float topSpeed, float acceleration, ref PathDebug debug)
        {
            Point target = brain.ActionTarget;
            debug.NextNode = target;
            debug.Edge = ClassifyEdge(brain.ActionStart, target);
            brain.ActionTimer--;

            float targetX = target.X * TileSize + 8f;
            float dxPixels = targetX - npc.Center.X;
            bool closeX = Math.Abs(dxPixels) <= (brain.Action == MoveActionKind.Jump ? 26f : 16f);
            bool closeY = Math.Abs(npcNode.Y - target.Y) <= (brain.Action == MoveActionKind.Drop ? 2 : 1);
            if (closeX && closeY)
            {
                // Critical: a Drop action sets npc.noTileCollide = true. If we don't reset it,
                // the NPC keeps falling through every floor for the rest of its life.
                if (brain.Action == MoveActionKind.Drop) npc.noTileCollide = false;
                brain.RouteIndex++;
                brain.ClearAction();
                brain.BlockedFrames = 0;
                debug.Action = "action-complete";
                debug.Reason = $"node=({target.X},{target.Y})";
                return true;
            }

            int direction = Math.Abs(dxPixels) > 10f ? (dxPixels > 0f ? 1 : -1) : brain.ActionDirection;
            brain.LastMoveDirection = direction;
            npc.direction = direction;
            npc.spriteDirection = direction;

            bool blocked = npc.collideX || (grounded && Math.Abs(npc.velocity.X) < 0.08f && Math.Abs(dxPixels) > 18f);
            if (blocked)
            {
                brain.ActionBlockedFrames++;
            }
            else if (brain.ActionBlockedFrames > 0)
            {
                brain.ActionBlockedFrames--;
            }

            if (brain.ActionTimer <= 0 || brain.ActionBlockedFrames >= 16)
            {
                string edgeKey = Brain.EdgeKey(brain.ActionStart, brain.ActionTarget);
                int blockedFrames = brain.ActionBlockedFrames;
                brain.RememberBadEdge(edgeKey, BadEdgeMemoryTicks);
                // Reset pass-through if Drop was the failing action.
                if (brain.Action == MoveActionKind.Drop) npc.noTileCollide = false;
                brain.Route.Clear();
                brain.RouteIndex = 0;
                brain.RepathTimer = 0;
                brain.ClearAction();
                debug.Action = "action-failed";
                debug.Reason = $"bad-edge={edgeKey} blocked={blockedFrames}";
                return true;
            }

            switch (brain.Action)
            {
                case MoveActionKind.Jump:
                    // Use full acceleration in the air so we actually reach launch X velocity
                    // before gravity ends the jump; the old 0.82 multiplier left vx near 0.06
                    // for the whole arc (visible in logs as stuck-in-air repeats).
                    ApplyHorizontal(npc, direction, topSpeed, acceleration * (grounded ? 1f : 1.6f));
                    debug.Action = grounded ? "commit-jump-ground" : "commit-jump-air";
                    debug.Reason = $"target=({target.X},{target.Y}) timer={brain.ActionTimer} blocked={brain.ActionBlockedFrames}";
                    return true;

                case MoveActionKind.Drop:
                    if (IsStandingOnPlatform(npc) || IsOverlappingPlatform(npc))
                    {
                        npc.noTileCollide = true;
                        npc.velocity.Y = Math.Max(npc.velocity.Y, 1.6f);
                    }
                    ApplyHorizontal(npc, direction, topSpeed * 0.82f, acceleration);
                    debug.Action = "commit-drop";
                    debug.Reason = $"target=({target.X},{target.Y}) timer={brain.ActionTimer}";
                    return true;

                default:
                    ApplyHorizontal(npc, direction, topSpeed, acceleration);
                    debug.Action = "commit-walk";
                    debug.Reason = $"target=({target.X},{target.Y}) timer={brain.ActionTimer} blocked={brain.ActionBlockedFrames}";
                    return true;
            }
        }

        private static bool TryRecoverBlockedMovement(NPC npc, tsorcRevampGlobalNPC globalNPC, Brain brain, bool grounded, float topSpeed, ref PathDebug debug)
        {
            bool blocked = npc.collideX || (grounded && Math.Abs(npc.velocity.X) < 0.09f && brain.Route.Count > 0);
            if (!blocked)
            {
                if (brain.BlockedFrames > 0)
                {
                    brain.BlockedFrames--;
                }
                return false;
            }

            brain.BlockedFrames++;
            if (brain.BlockedFrames < 8)
            {
                return false;
            }

            int direction = brain.LastMoveDirection == 0 ? npc.direction : brain.LastMoveDirection;
            if (direction == 0)
            {
                direction = 1;
            }

            if (grounded)
            {
                int blockedFrames = brain.BlockedFrames;
                npc.velocity.Y = -MathHelper.Clamp(globalNPC.MaxJumpPower * 0.72f, 5.6f, globalNPC.MaxJumpPower);
                npc.velocity.X = direction * MathHelper.Clamp(topSpeed * 1.55f, 1.6f, globalNPC.MaxJumpBoost);
                globalNPC.NavJumpCooldown = 18;
                brain.BlockedFrames = 0;
                brain.RepathTimer = 0;
                debug.Action = "blocked-jump";
                debug.Reason = $"frames={blockedFrames} dir={direction}";
                debug.JumpPower = -npc.velocity.Y;
                debug.Boost = Math.Abs(npc.velocity.X);
                return true;
            }

            if (brain.BlockedFrames > 24)
            {
                brain.Route.Clear();
                brain.RouteIndex = 0;
                brain.RepathTimer = 0;
                brain.LastMoveDirection = -direction;
                npc.velocity.X = -direction * MathHelper.Clamp(topSpeed * 0.8f, 0.8f, 1.5f);
                brain.BlockedFrames = 0;
                debug.Action = "blocked-repath";
                debug.Reason = "air-or-no-headroom";
                return true;
            }

            return false;
        }

        private static void AdvanceRouteIndex(NPC npc, Brain brain, Point npcNode)
        {
            while (brain.RouteIndex < brain.Route.Count)
            {
                Point next = brain.Route[brain.RouteIndex];
                float dxPixels = Math.Abs(npc.Center.X - (next.X * TileSize + 8f));
                bool closeX = dxPixels < 18f;
                bool closeY = Math.Abs(npcNode.Y - next.Y) <= 1;
                if (!closeX || !closeY)
                {
                    break;
                }

                brain.RouteIndex++;
            }
        }

        private static int DirectionToNode(NPC npc, Brain brain, Point next, Point after)
        {
            float nextCenterX = next.X * TileSize + 8f;
            float delta = nextCenterX - npc.Center.X;
            if (Math.Abs(delta) > 14f)
            {
                return delta >= 0f ? 1 : -1;
            }

            if (after != next && after.X != next.X)
            {
                return after.X > next.X ? 1 : -1;
            }

            return brain.LastMoveDirection == 0 ? 1 : brain.LastMoveDirection;
        }

        private static bool ShouldJump(Point from, Point to, string edge)
        {
            if (edge == "walk" || edge == "drop")
            {
                return false;
            }

            return to.Y < from.Y - 1 || Math.Abs(to.X - from.X) >= 2;
        }

        private static string ClassifyEdge(Point from, Point to)
        {
            int dx = Math.Abs(to.X - from.X);
            int dy = to.Y - from.Y;
            if (dy > 1)
            {
                return "drop";
            }
            if (dy < -1)
            {
                return "jump";
            }
            if (dx >= 2 && HasGapBetween(from, to))
            {
                return "jump";
            }

            return "walk";
        }

        private static void BuildRoute(Brain brain, NPC npc, Point start, Point target)
        {
            brain.Route.Clear();
            brain.RouteIndex = 0;
            brain.TargetNode = target;
            brain.RepathTimer = RepathInterval;
            brain.LastPlanResult = "no-start";

            if (start == Point.Zero || target == Point.Zero)
            {
                return;
            }

            Dictionary<Point, Point> cameFrom = new Dictionary<Point, Point>();
            Dictionary<Point, int> costSoFar = new Dictionary<Point, int>();
            List<Point> open = new List<Point> { start };
            cameFrom[start] = start;
            costSoFar[start] = 0;

            Point best = start;
            int bestScore = ScoreNode(start, target, 0);
            int visited = 0;
            while (open.Count > 0 && visited < 700)
            {
                int bestOpenIndex = 0;
                int bestOpenPriority = int.MinValue;
                for (int i = 0; i < open.Count; i++)
                {
                    Point candidate = open[i];
                    int priority = ScoreNode(candidate, target, costSoFar[candidate]);
                    if (priority > bestOpenPriority)
                    {
                        bestOpenPriority = priority;
                        bestOpenIndex = i;
                    }
                }

                Point current = open[bestOpenIndex];
                open.RemoveAt(bestOpenIndex);
                visited++;

                int currentScore = ScoreNode(current, target, costSoFar[current]);
                if (currentScore > bestScore)
                {
                    bestScore = currentScore;
                    best = current;
                }
                if (DistanceTiles(current, target) <= 2)
                {
                    best = current;
                    break;
                }

                foreach (Point next in GetNeighbors(current, start, brain))
                {
                    int newCost = costSoFar[current] + MovementCost(current, next);
                    if (costSoFar.TryGetValue(next, out int oldCost) && newCost >= oldCost)
                    {
                        continue;
                    }

                    costSoFar[next] = newCost;
                    cameFrom[next] = current;
                    if (!open.Contains(next))
                    {
                        open.Add(next);
                    }
                }
            }

            if (best == start)
            {
                brain.LastPlanResult = $"none visited={visited} score={bestScore}";
                return;
            }
            // Reject paths whose best reachable node scores below zero — those are usually
            // off-cliff dead ends the NPC commits to and then can't recover from.
            if (bestScore < 0 && DistanceTiles(best, target) > 4)
            {
                brain.LastPlanResult = $"rejected-negscore visited={visited} score={bestScore}";
                return;
            }

            List<Point> reversed = new List<Point>();
            Point node = best;
            reversed.Add(node);
            while (cameFrom[node] != start && cameFrom[node] != node)
            {
                node = cameFrom[node];
                reversed.Add(node);
            }

            reversed.Reverse();
            for (int i = 0; i < reversed.Count && brain.Route.Count < MaxRouteNodes; i++)
            {
                Point routeNode = reversed[i];
                if (brain.Route.Count == 0 && DistanceTiles(routeNode, start) <= 1 && i + 1 < reversed.Count)
                {
                    continue;
                }
                brain.Route.Add(routeNode);
            }

            brain.LastPlanResult = $"route nodes={brain.Route.Count} visited={visited} best=({best.X},{best.Y}) target=({target.X},{target.Y}) score={bestScore}";
        }

        private static IEnumerable<Point> GetNeighbors(Point current, Point start, Brain brain)
        {
            for (int dir = -1; dir <= 1; dir += 2)
            {
                int x = current.X + dir;
                int y = FindGroundY(x, current.Y, 2, 3);
                Point next = new Point(x, y);
                if (IsValidNode(x, y, start) && Math.Abs(y - current.Y) <= 2 && HasWalkClearance(current, next) && !brain.IsBadEdge(current, next))
                {
                    yield return next;
                }
            }

            for (int dir = -1; dir <= 1; dir += 2)
            {
                for (int dx = 2; dx <= 8; dx++)
                {
                    int x = current.X + dir * dx;
                    for (int dy = -9; dy <= 3; dy++)
                    {
                        int y = current.Y + dy;
                        Point next = new Point(x, y);
                        if (IsValidNode(x, y, start) && IsJumpCandidate(current, next) && !brain.IsBadEdge(current, next))
                        {
                            yield return next;
                            break;
                        }
                    }
                }
            }

            for (int dx = -3; dx <= 3; dx++)
            {
                int x = current.X + dx;
                for (int y = current.Y + 2; y <= current.Y + 15; y++)
                {
                    if (IsValidNode(x, y, start) && HasDropClearance(current, new Point(x, y)))
                    {
                        Point next = new Point(x, y);
                        if (!brain.IsBadEdge(current, next))
                        {
                            yield return next;
                        }
                        break;
                    }
                }
            }

            for (int dx = -4; dx <= 4; dx++)
            {
                int x = current.X + dx;
                for (int dy = -2; dy >= -12; dy--)
                {
                    int y = current.Y + dy;
                    Point next = new Point(x, y);
                    if (IsValidNode(x, y, start) && IsJumpCandidate(current, next) && !brain.IsBadEdge(current, next))
                    {
                        yield return next;
                        break;
                    }
                }
            }
        }

        private static bool IsValidNode(int x, int y, Point start)
        {
            return y != int.MinValue
                && Math.Abs(x - start.X) <= SearchHalfWidth
                && Math.Abs(y - start.Y) <= SearchHalfHeight
                && IsStandable(x, y)
                && HasBodyClearanceAt(x, y);
        }

        private static int ScoreNode(Point node, Point target, int cost)
        {
            int dx = Math.Abs(node.X - target.X);
            int dy = Math.Abs(node.Y - target.Y);
            int score = 160 - dx * 5 - dy * 8 - cost;
            if (dx <= 2)
            {
                score += 20;
            }
            if (dy <= 2)
            {
                score += 20;
            }
            return score;
        }

        private static int MovementCost(Point from, Point to)
        {
            int dx = Math.Abs(to.X - from.X);
            int dy = Math.Abs(to.Y - from.Y);
            int cost = dx + dy * 2;
            if (to.Y < from.Y)
            {
                cost += 6;
            }
            if (to.Y > from.Y + 2)
            {
                cost += 2;
            }
            return cost;
        }

        private static Point FindPlayerTargetNode(Player player, Point fallback)
        {
            int playerX = (int)(player.Center.X / TileSize);
            int playerFeetY = (int)((player.Bottom.Y - 1f) / TileSize);
            Point best = Point.Zero;
            int bestScore = int.MinValue;
            for (int radius = 0; radius <= 10; radius++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int x = playerX + dx;
                    int y = FindGroundY(x, playerFeetY, 8, 14);
                    if (y == int.MinValue || !HasBodyClearanceAt(x, y))
                    {
                        continue;
                    }

                    int score = 200 - Math.Abs(dx) * 8 - Math.Abs(y - playerFeetY) * 4 - DistanceTiles(new Point(x, y), fallback);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = new Point(x, y);
                    }
                }
                if (best != Point.Zero)
                {
                    return best;
                }
            }

            return fallback;
        }

        private static Point FindNearestStandNode(int centerX, int feetY, int maxX, int maxY)
        {
            Point best = Point.Zero;
            int bestScore = int.MaxValue;
            for (int dx = -maxX; dx <= maxX; dx++)
            {
                int x = centerX + dx;
                int y = FindGroundY(x, feetY, maxY, maxY);
                if (y == int.MinValue || !HasBodyClearanceAt(x, y))
                {
                    continue;
                }

                int score = Math.Abs(dx) * 3 + Math.Abs(y - feetY);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = new Point(x, y);
                }
            }

            return best;
        }

        private static int FindGroundY(int x, int feetY, int maxUp, int maxDown)
        {
            // Critical: prefer ground AT or BELOW the NPC's feet first, then look up.
            // The naive top-down scan would return a roof/floor 5 tiles ABOVE the NPC,
            // causing A* to start from the wrong floor and never find a path to the player.
            if (IsStandable(x, feetY)) return feetY;
            for (int d = 1; d <= maxDown; d++)
            {
                if (IsStandable(x, feetY + d)) return feetY + d;
            }
            for (int u = 1; u <= maxUp; u++)
            {
                if (IsStandable(x, feetY - u)) return feetY - u;
            }
            return int.MinValue;
        }

        private static bool TryStartSpritePass(NPC npc, Brain brain, out string reason)
        {
            reason = "";
            return false;
        }

        private static void ApplyHorizontal(NPC npc, int direction, float topSpeed, float acceleration)
        {
            float targetSpeed = topSpeed * direction;
            if (npc.velocity.X < targetSpeed)
            {
                npc.velocity.X = Math.Min(npc.velocity.X + acceleration, targetSpeed);
            }
            else if (npc.velocity.X > targetSpeed)
            {
                npc.velocity.X = Math.Max(npc.velocity.X - acceleration, targetSpeed);
            }
        }

        private static int StableDirection(NPC npc, Brain brain, float deltaX, bool verticalProblem)
        {
            if (Math.Abs(deltaX) > 18f)
            {
                brain.LastMoveDirection = deltaX >= 0f ? 1 : -1;
                return brain.LastMoveDirection;
            }
            if (verticalProblem && brain.LastMoveDirection != 0)
            {
                return brain.LastMoveDirection;
            }
            return npc.direction == 0 ? 1 : npc.direction;
        }

        private static int DistanceTiles(Point a, Point b)
        {
            return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
        }

        private static bool HasArcClearance(Point from, Point to)
        {
            int minX = Math.Min(from.X, to.X);
            int maxX = Math.Max(from.X, to.X);
            int minY = Math.Min(from.Y, to.Y) - 5;
            int maxY = Math.Max(from.Y, to.Y) - 1;
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    if (IsNavigationSolid(x, y))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static bool IsJumpCandidate(Point from, Point to)
        {
            int dx = Math.Abs(to.X - from.X);
            int dy = to.Y - from.Y;
            if (dy < -1)
            {
                return HasArcClearance(from, to);
            }

            if (dx >= 2 && HasGapBetween(from, to))
            {
                return HasArcClearance(from, to);
            }

            return false;
        }

        private static bool HasWalkClearance(Point from, Point to)
        {
            int direction = Math.Sign(to.X - from.X);
            if (direction == 0)
            {
                return true;
            }

            int x = from.X;
            int y = from.Y;
            while (x != to.X)
            {
                x += direction;
                // Hard limit to ±1 row per step — anything bigger is a jump or drop, not a walk.
                int groundY = FindGroundY(x, y, 1, 1);
                if (groundY == int.MinValue || Math.Abs(groundY - y) > 1 || !HasBodyClearanceAt(x, groundY))
                {
                    return false;
                }
                // Verify the body-height column at the higher of the two rows is also clear.
                // Without this check, BFS happily threads "walk" edges through walls because
                // FindGroundY would just locate the floor below the wall.
                int checkY = Math.Min(y, groundY);
                for (int yc = checkY - 2; yc <= checkY; yc++)
                {
                    if (IsNavigationSolid(x, yc))
                    {
                        return false;
                    }
                }
                y = groundY;
            }

            return Math.Abs(y - to.Y) <= 1;
        }

        private static bool HasGapBetween(Point from, Point to)
        {
            int direction = Math.Sign(to.X - from.X);
            if (direction == 0)
            {
                return false;
            }

            int x = from.X + direction;
            while (x != to.X)
            {
                int groundY = FindGroundY(x, from.Y, 2, 5);
                if (groundY == int.MinValue || groundY > from.Y + 2)
                {
                    return true;
                }
                x += direction;
            }

            return false;
        }

        private static bool HasDropClearance(Point from, Point to)
        {
            int minX = Math.Min(from.X, to.X);
            int maxX = Math.Max(from.X, to.X);
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = from.Y - 3; y <= to.Y - 1; y++)
                {
                    if (IsNavigationSolid(x, y))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static bool IsGrounded(NPC npc)
        {
            if (npc.velocity.Y != 0f)
            {
                return false;
            }

            int left = (int)(npc.Left.X / TileSize);
            int right = (int)((npc.Right.X - 1f) / TileSize);
            int belowFeet = (int)((npc.Bottom.Y + 4f) / TileSize);
            for (int x = left; x <= right; x++)
            {
                if (IsStandable(x, belowFeet))
                {
                    return true;
                }
            }
            return false;
        }

        private static int GetFeetTileY(NPC npc)
        {
            return (int)((npc.Bottom.Y - 1f) / TileSize);
        }

        private static bool IsStandingOnPlatform(NPC npc)
        {
            int left = (int)(npc.Left.X / TileSize);
            int right = (int)((npc.Right.X - 1f) / TileSize);
            int belowFeet = (int)((npc.Bottom.Y + 4f) / TileSize);
            bool platform = false;
            for (int x = left; x <= right; x++)
            {
                if (IsPlatform(x, belowFeet))
                {
                    platform = true;
                }
                else if (IsNavigationSolid(x, belowFeet))
                {
                    return false;
                }
            }
            return platform;
        }

        private static bool IsStandable(int x, int y)
        {
            return IsNavigationSolid(x, y) || IsPlatform(x, y);
        }

        private static bool HasBodyClearanceAt(int x, int feetY)
        {
            for (int y = feetY - 3; y <= feetY - 1; y++)
            {
                if (IsNavigationSolid(x, y))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool IsPlatform(int x, int y)
        {
            if (!WorldGen.InWorld(x, y))
            {
                return false;
            }

            Tile tile = Main.tile[x, y];
            return tile.HasTile && !tile.IsActuated && TileID.Sets.Platforms[tile.TileType];
        }

        private static bool IsOverlappingPlatform(NPC npc)
        {
            int left = (int)(npc.Left.X / TileSize);
            int right = (int)((npc.Right.X - 1f) / TileSize);
            int top = (int)(npc.Top.Y / TileSize);
            int bottom = (int)((npc.Bottom.Y - 1f) / TileSize);
            for (int x = left; x <= right; x++)
            {
                for (int y = top; y <= bottom; y++)
                {
                    if (IsPlatform(x, y))
                    {
                        return true;
                    }
                }
            }

            return false;
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

        private static bool IsPassableSprite(int x, int y)
        {
            if (!WorldGen.InWorld(x, y))
            {
                return false;
            }

            Tile tile = Main.tile[x, y];
            ushort type = tile.TileType;
            return tile.HasTile
                && !tile.IsActuated
                && Main.tileFrameImportant[type]
                && Main.tileSolid[type]
                && type != TileID.Trees
                && type != TileID.ClosedDoor
                && type != TileID.OpenDoor
                && !TileID.Sets.Platforms[type]
                && !IsNavigationSolid(x, y);
        }

        private static void Log(NPC npc, Player player, Brain brain, PathDebug debug)
        {
            bool interesting = !debug.LineOfSight
                || debug.Action != "route-walk"
                || brain.Route.Count > 0
                || brain.PassTimer > 0
                || brain.Action != MoveActionKind.None;
            if (!interesting)
            {
                return;
            }

            int now = (int)Main.GameUpdateCount;
            if (now - brain.LastLogTick < 30)
            {
                return;
            }
            brain.LastLogTick = now;

            try
            {
                string separator = Path.DirectorySeparatorChar.ToString();
                string logDir = Main.SavePath + separator + "Logs";
                Directory.CreateDirectory(logDir);
                string logPath = logDir + separator + "tsorcRevamp-pathfighter2.log";
                string line = $"[{DateTime.Now:HH:mm:ss}] {npc.TypeName}#{npc.whoAmI} pos=({npc.Center.X / 16f:F1},{npc.Center.Y / 16f:F1}) player=({player.Center.X / 16f:F1},{player.Center.Y / 16f:F1}) vel=({npc.velocity.X:F2},{npc.velocity.Y:F2}) dir={npc.direction} grounded={debug.Grounded} collide=({npc.collideX},{npc.collideY}) los={debug.LineOfSight} npcNode=({debug.NpcNode.X},{debug.NpcNode.Y}) target=({debug.TargetNode.X},{debug.TargetNode.Y}) next=({debug.NextNode.X},{debug.NextNode.Y}) route={debug.RouteIndex}/{debug.RouteCount} edge={debug.Edge} committed={brain.Action}:{brain.ActionTimer} badEdges={brain.BadEdgeCount} plan={brain.LastPlanResult} action={debug.Action} reason={debug.Reason} jump={debug.JumpPower:F2} boost={debug.Boost:F2} pass={brain.PassDirection}/{brain.PassTimer} attack={debug.AttackAllowed}";
                File.AppendAllText(logPath, line + Environment.NewLine);
            }
            catch
            {
                // Navigation logging should never affect gameplay.
            }
        }

        private class Brain
        {
            public int NpcType;
            public readonly List<Point> Route = new List<Point>();
            public readonly Dictionary<string, int> BadEdges = new Dictionary<string, int>();
            public int RouteIndex;
            public Point TargetNode;
            public int RepathTimer;
            public int LastMoveDirection = 1;
            public int PassTimer;
            public int PassDirection;
            public int PassCooldown;
            public int BlockedFrames;
            public MoveActionKind Action;
            public Point ActionStart;
            public Point ActionTarget;
            public int ActionDirection;
            public int ActionTimer;
            public int ActionBlockedFrames;
            public float ActionStartX;
            public float ActionStartY;
            public string LastPlanResult = "none";
            public int LastLogTick;

            public int BadEdgeCount => BadEdges.Count;

            public void ClearAction()
            {
                Action = MoveActionKind.None;
                ActionStart = Point.Zero;
                ActionTarget = Point.Zero;
                ActionDirection = 0;
                ActionTimer = 0;
                ActionBlockedFrames = 0;
                ActionStartX = 0f;
                ActionStartY = 0f;
            }

            public bool IsBadEdge(Point from, Point to)
            {
                return BadEdges.ContainsKey(EdgeKey(from, to));
            }

            public void RememberBadEdge(string edgeKey, int ticks)
            {
                BadEdges[edgeKey] = ticks;
            }

            public void TickBadEdges()
            {
                if (BadEdges.Count == 0)
                {
                    return;
                }

                List<string> expired = new List<string>();
                List<string> keys = new List<string>(BadEdges.Keys);
                foreach (string key in keys)
                {
                    int ticks = BadEdges[key] - 1;
                    if (ticks <= 0)
                    {
                        expired.Add(key);
                    }
                    else
                    {
                        BadEdges[key] = ticks;
                    }
                }

                foreach (string edge in expired)
                {
                    BadEdges.Remove(edge);
                }
            }

            public static string EdgeKey(Point from, Point to)
            {
                return $"{from.X},{from.Y}>{to.X},{to.Y}";
            }
        }

        private enum MoveActionKind
        {
            None,
            Walk,
            Jump,
            Drop
        }

        private struct PathDebug
        {
            public bool Grounded;
            public bool LineOfSight;
            public bool AttackAllowed;
            public bool OverrideHorizontal;
            public Point NpcNode;
            public Point TargetNode;
            public Point NextNode;
            public int RouteCount;
            public int RouteIndex;
            public string Edge;
            public string Action;
            public string Reason;
            public float JumpPower;
            public float Boost;
        }
    }
}
