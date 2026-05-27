using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace tsorcRevamp.NPCs
{
    public static class SmartFighterAI
    {
        public static void Run(NPC npc, float topSpeed = 1.55f, float acceleration = 0.05f, int doorBreakingDamage = 4, float attackRange = 700f, bool allowPlantAndFire = false)
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

            tsorcRevampGlobalNPC globalNPC = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();

            if (globalNPC.NavJumpCooldown > 0)
            {
                globalNPC.NavJumpCooldown--;
            }
            if (globalNPC.NavBlockedDirectionTimer > 0)
            {
                globalNPC.NavBlockedDirectionTimer--;
                if (globalNPC.NavBlockedDirectionTimer == 0)
                {
                    globalNPC.NavBlockedDirection = 0;
                }
            }
            if (globalNPC.SmartFurniturePassCooldown > 0)
            {
                globalNPC.SmartFurniturePassCooldown--;
            }

            UpdateTemporaryTilePassThrough(npc, globalNPC);
            bool activeFurniturePass = globalNPC.SmartFurniturePassTimer > 0;
            if (activeFurniturePass)
            {
                globalNPC.SmartFurniturePassTimer--;
                npc.noTileCollide = true;
                npc.noGravity = true;
                npc.velocity.X = MathHelper.Clamp(globalNPC.SmartFurniturePassDirection, -1, 1) * MathHelper.Clamp(topSpeed * 1.15f, 1.1f, 2.35f);
                npc.velocity.Y = 0f;
                if (globalNPC.SmartFurniturePassTimer <= 0)
                {
                    npc.noTileCollide = false;
                    npc.noGravity = false;
                    globalNPC.SmartFurniturePassTimer = 0;
                    globalNPC.SmartFurniturePassDirection = 0;
                }
            }
            if (globalNPC.LastNavIntent == "smart:rope-climb")
            {
                globalNPC.NavExploreTimer--;
                if (globalNPC.NavExploreTimer <= 0)
                {
                    globalNPC.LastNavIntent = "smart:direct";
                    globalNPC.NavExploreTimer = 0;
                }
            }
            if (globalNPC.LastNavIntent != "smart:rope-climb" && !activeFurniturePass)
            {
                npc.noGravity = false;
            }

            bool grounded = IsGrounded(npc);
            if (grounded)
            {
                globalNPC.UsedDoubleJump = false;
            }
            else if (npc.collideX && npc.collideY && Math.Abs(npc.velocity.X) < 0.2f && Math.Abs(npc.velocity.Y) < 0.2f)
            {
                int escapeDirection = player.Center.X >= npc.Center.X ? 1 : -1;
                if (TryStartFurniturePass(npc, globalNPC, escapeDirection, out _))
                {
                    npc.velocity.X = 1.2f * escapeDirection;
                    npc.velocity.Y = 0f;
                }
            }

            bool lineOfSight = Collision.CanHit(npc.position, npc.width, npc.height, player.position, player.width, player.height);
            UpdateSmartBoredom(globalNPC, lineOfSight);

            int chaseDirection = GetSmartDirection(npc, globalNPC, player, grounded, lineOfSight);
            npc.direction = chaseDirection;
            npc.spriteDirection = chaseDirection;

            SmartNavDebugFrame debugFrame = new SmartNavDebugFrame
            {
                Grounded = grounded,
                LineOfSight = lineOfSight,
                Direction = chaseDirection,
                CollideX = npc.collideX,
                CollideY = npc.collideY,
                PlayerDeltaX = player.Center.X - npc.Center.X,
                PlayerDeltaY = player.Center.Y - npc.Center.Y,
                Mode = GetSmartMode(globalNPC)
            };

            bool movementAction = false;
            if (activeFurniturePass)
            {
                movementAction = true;
                debugFrame.OverrideHorizontal = true;
                debugFrame.Action = "furniture-pass-active";
                debugFrame.Reason = $"timer={Math.Max(globalNPC.SmartFurniturePassTimer, 0)}";
            }
            else if (grounded && globalNPC.NavJumpCooldown == 0)
            {
                movementAction = TryHandleTerrain(npc, globalNPC, chaseDirection, topSpeed, doorBreakingDamage, ref debugFrame);
            }
            else
            {
                debugFrame.Action = grounded ? "jump-cooldown" : "airborne";
                debugFrame.Reason = grounded ? "cooldown" : "not-grounded";
            }

            if (!debugFrame.OverrideHorizontal)
            {
                ApplyHorizontalChase(npc, chaseDirection, topSpeed, acceleration);
            }

            bool activeRouteMovement = globalNPC.LastNavIntent == "smart:route-target" && globalNPC.WaypointTimer > 0;
            bool canUseProjectile = globalNPC.AttackList.Count > 0
                && lineOfSight
                && npc.Distance(player.Center) <= attackRange
                && !activeRouteMovement
                && (!movementAction || allowPlantAndFire);
            debugFrame.AttackAllowed = canUseProjectile;
            debugFrame.Mode = GetSmartMode(globalNPC);

            bool oldCanStopToFire = globalNPC.CanStopToFire;
            if (!allowPlantAndFire)
            {
                globalNPC.CanStopToFire = false;
            }

            if (globalNPC.AttackList.Count > 0)
            {
                tsorcRevampAIs.SimpleProjectile(npc, canUseProjectile);
            }

            globalNPC.CanStopToFire = oldCanStopToFire;
            LogSmartFighterDebug(npc, globalNPC, player, debugFrame);
        }

        public static void OnHit(NPC npc)
        {
            tsorcRevampGlobalNPC globalNPC = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.BoredTimer = 0;
            globalNPC.StuckTimer = 0;
        }

        private static void UpdateSmartBoredom(tsorcRevampGlobalNPC globalNPC, bool lineOfSight)
        {
            if (lineOfSight)
            {
                globalNPC.BoredTimer = 0;
                return;
            }

            int threshold = (int)(globalNPC.BoredomThreshold * MathHelper.Clamp(globalNPC.Patience, 0.5f, 3f));
            if (globalNPC.BoredTimer < threshold)
            {
                globalNPC.BoredTimer++;
            }
        }

        private static string GetSmartMode(tsorcRevampGlobalNPC globalNPC)
        {
            if (globalNPC.SmartFurniturePassTimer > 0)
            {
                return "smart:furniture-pass";
            }

            return !string.IsNullOrEmpty(globalNPC.LastNavIntent) && globalNPC.LastNavIntent.StartsWith("smart:")
                ? globalNPC.LastNavIntent
                : "smart:direct";
        }

        private static void ApplyHorizontalChase(NPC npc, int direction, float topSpeed, float acceleration)
        {
            float targetSpeed = topSpeed * direction;
            if (npc.velocity.X < targetSpeed)
            {
                npc.velocity.X += acceleration;
                if (npc.velocity.X > targetSpeed)
                {
                    npc.velocity.X = targetSpeed;
                }
            }
            else if (npc.velocity.X > targetSpeed)
            {
                npc.velocity.X -= acceleration;
                if (npc.velocity.X < targetSpeed)
                {
                    npc.velocity.X = targetSpeed;
                }
            }
        }

        private static int GetSmartDirection(NPC npc, tsorcRevampGlobalNPC globalNPC, Player player, bool grounded, bool lineOfSight)
        {
            if (globalNPC.SmartFurniturePassTimer > 0 && globalNPC.SmartFurniturePassDirection != 0)
            {
                return globalNPC.SmartFurniturePassDirection;
            }

            float playerDeltaX = player.Center.X - npc.Center.X;
            bool playerWellAbove = player.Center.Y < npc.Center.Y - 72f;
            bool playerWellBelow = player.Center.Y > npc.Center.Y + 72f;
            int direct = GetStableDirectDirection(npc, globalNPC, playerDeltaX, playerWellAbove || playerWellBelow);
            if (grounded && globalNPC.NavBlockedDirectionTimer > 0 && globalNPC.NavBlockedDirection == direct && !lineOfSight)
            {
                return -direct;
            }

            if (!grounded && globalNPC.LastNavIntent != "smart:route-target")
            {
                return direct;
            }

            if (globalNPC.WaypointTimer > 0 && globalNPC.LastNavIntent == "smart:route-target")
            {
                globalNPC.WaypointTimer--;
                float targetDeltaX = globalNPC.WaypointTarget.X - npc.Center.X;
                float targetDeltaY = globalNPC.WaypointTarget.Y - npc.Center.Y;
                bool verticalRouteEdge = globalNPC.LastWaypointResult.Contains("edge=jump")
                    || globalNPC.LastWaypointResult.Contains("edge=rope")
                    || globalNPC.LastWaypointResult.Contains("edge=drop");
                if (Math.Abs(targetDeltaX) > 20f)
                {
                    return targetDeltaX >= 0f ? 1 : -1;
                }
                if (Math.Abs(targetDeltaY) > 24f && verticalRouteEdge)
                {
                    return globalNPC.NavExploreDirection == 0 ? direct : globalNPC.NavExploreDirection;
                }
                if ((playerWellAbove || playerWellBelow) && verticalRouteEdge)
                {
                    return globalNPC.NavExploreDirection == 0 ? direct : globalNPC.NavExploreDirection;
                }

                bool routeTargetWasBelow = globalNPC.WaypointTarget.Y > npc.Center.Y + 20f;
                globalNPC.WaypointTimer = 0;
                globalNPC.LastNavIntent = routeTargetWasBelow ? "smart:direct" : "smart:vertical-search";
            }

            bool sameColumnDifferentFloor = Math.Abs(player.Center.X - npc.Center.X) < 180f && (playerWellAbove || playerWellBelow);
            bool shouldPlanRoute = (!lineOfSight || sameColumnDifferentFloor) && (playerWellAbove || playerWellBelow || globalNPC.BoredTimer > 90);
            if (shouldPlanRoute)
            {
                if ((globalNPC.WaypointTimer <= 0 || globalNPC.LastNavIntent != "smart:route-target")
                    && TryFindPathRouteTarget(npc, globalNPC, player, out Vector2 pathTarget, out int pathDirection))
                {
                    globalNPC.WaypointTarget = pathTarget;
                    globalNPC.WaypointTimer = 240;
                    globalNPC.NavExploreDirection = pathDirection;
                    globalNPC.NavExploreTimer = 240;
                    globalNPC.LastNavIntent = "smart:route-target";
                    return pathDirection;
                }

                if (globalNPC.NavExploreTimer <= 0 || globalNPC.LastNavIntent != "smart:vertical-search")
                {
                    globalNPC.NavExploreDirection = ChooseVerticalSearchDirection(npc, direct);
                    globalNPC.NavExploreTimer = 150;
                    globalNPC.LastNavIntent = "smart:vertical-search";
                }

                return globalNPC.NavExploreDirection == 0 ? direct : globalNPC.NavExploreDirection;
            }

            if (globalNPC.LastNavIntent == "smart:vertical-search")
            {
                globalNPC.NavExploreTimer = 0;
                globalNPC.NavExploreDirection = 0;
            }
            globalNPC.LastNavIntent = "smart:direct";
            return direct;
        }

        private static int GetStableDirectDirection(NPC npc, tsorcRevampGlobalNPC globalNPC, float playerDeltaX, bool verticalProblem)
        {
            if (Math.Abs(playerDeltaX) > 24f)
            {
                return playerDeltaX >= 0f ? 1 : -1;
            }

            if (verticalProblem)
            {
                if (globalNPC.NavExploreDirection != 0)
                {
                    return globalNPC.NavExploreDirection;
                }
                if (npc.direction != 0)
                {
                    return npc.direction;
                }
            }

            return playerDeltaX >= 0f ? 1 : -1;
        }

        private static int ChooseVerticalSearchDirection(NPC npc, int direct)
        {
            int feetY = GetFeetTileY(npc);
            int leftX = GetFrontTileX(npc, -1);
            int rightX = GetFrontTileX(npc, 1);
            int leftScore = ScoreVerticalSearchDirection(leftX, feetY, -1);
            int rightScore = ScoreVerticalSearchDirection(rightX, feetY, 1);

            if (leftScore == rightScore)
            {
                return direct;
            }

            return leftScore > rightScore ? -1 : 1;
        }

        private static bool TryFindPathRouteTarget(NPC npc, tsorcRevampGlobalNPC globalNPC, Player player, out Vector2 target, out int direction)
        {
            target = Vector2.Zero;
            direction = 0;

            if (!TryFindPathStart(npc, out int startX, out int startY))
            {
                globalNPC.LastWaypointResult = "smart:path no-start";
                return false;
            }

            int playerX = (int)(player.Center.X / 16f);
            int playerFeetY = (int)((player.Bottom.Y - 1f) / 16f);
            Point start = new Point(startX, startY);
            Queue<Point> open = new Queue<Point>();
            Dictionary<Point, Point> cameFrom = new Dictionary<Point, Point>();
            Dictionary<Point, int> cost = new Dictionary<Point, int>();
            open.Enqueue(start);
            cameFrom[start] = start;
            cost[start] = 0;

            Point best = start;
            int bestScore = ScorePathNode(start, playerX, playerFeetY, 0);
            int visited = 0;
            while (open.Count > 0 && visited < 420)
            {
                Point current = open.Dequeue();
                visited++;
                int currentCost = cost[current];
                int score = ScorePathNode(current, playerX, playerFeetY, currentCost);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = current;
                }

                foreach (Point next in GetPathNeighbors(current, startX))
                {
                    if (cameFrom.ContainsKey(next))
                    {
                        continue;
                    }

                    cameFrom[next] = current;
                    cost[next] = currentCost + MovementCost(current, next);
                    open.Enqueue(next);
                }
            }

            if (best == start || bestScore < -18)
            {
                globalNPC.LastWaypointResult = $"smart:path none visited={visited} best={bestScore}";
                return false;
            }

            List<Point> route = new List<Point>();
            Point routeNode = best;
            route.Add(routeNode);
            while (cameFrom[routeNode] != start && cameFrom[routeNode] != routeNode)
            {
                routeNode = cameFrom[routeNode];
                route.Add(routeNode);
            }
            route.Reverse();

            Point first = route.Count > 0 ? route[0] : best;
            foreach (Point node in route)
            {
                if (Math.Abs(node.X - start.X) > 1 || Math.Abs(node.Y - start.Y) > 1)
                {
                    first = node;
                    break;
                }
            }

            target = new Vector2(first.X * 16f + 8f, first.Y * 16f - npc.height / 2f);
            direction = first.X == start.X ? (best.X >= start.X ? 1 : -1) : (first.X > start.X ? 1 : -1);
            string edgeAction = ClassifyPathEdge(start, first);
            globalNPC.LastWaypointResult = $"smart:path edge={edgeAction} visited={visited} best=({best.X},{best.Y}) first=({first.X},{first.Y}) score={bestScore}";
            return true;
        }

        private static bool TryFindPathStart(NPC npc, out int startX, out int startY)
        {
            int centerX = (int)(npc.Center.X / 16f);
            int feetY = GetFeetTileY(npc);
            for (int radius = 0; radius <= 3; radius++)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    int x = centerX + radius * side;
                    int y = FindNearbyGroundY(x, feetY, 4, 7);
                    if (y != int.MinValue && HasBodyClearanceAt(x, y))
                    {
                        startX = x;
                        startY = y;
                        return true;
                    }

                    if (radius == 0)
                    {
                        break;
                    }
                }
            }

            startX = centerX;
            startY = int.MinValue;
            return false;
        }

        private static string ClassifyPathEdge(Point from, Point to)
        {
            int dx = Math.Abs(to.X - from.X);
            int dy = to.Y - from.Y;
            if (HasRopeNear(from.X, from.Y, out int ropeX) && to.X == ropeX && dy < -2)
            {
                return "rope";
            }
            if (dy > 1)
            {
                return "drop";
            }
            if (dy < -1 || dx >= 2)
            {
                return "jump";
            }

            return "walk";
        }

        private static int ScorePathNode(Point node, int playerX, int playerFeetY, int cost)
        {
            int dx = Math.Abs(node.X - playerX);
            int dy = Math.Abs(node.Y - playerFeetY);
            int score = 120 - dx * 4 - dy * 7 - cost;
            if (dx <= 2)
            {
                score += 16;
            }
            if (dy <= 2)
            {
                score += 16;
            }
            if (HasBodyClearanceAt(node.X, node.Y))
            {
                score += 4;
            }

            return score;
        }

        private static IEnumerable<Point> GetPathNeighbors(Point current, int startX)
        {
            for (int dir = -1; dir <= 1; dir += 2)
            {
                int x = current.X + dir;
                int y = FindNearbyGroundY(x, current.Y, 2, 3);
                if (IsPathNodeReachable(x, y, startX) && Math.Abs(y - current.Y) <= 2)
                {
                    yield return new Point(x, y);
                }
            }

            for (int dx = -4; dx <= 4; dx++)
            {
                int x = current.X + dx;
                for (int dy = -2; dy >= -12; dy--)
                {
                    int y = current.Y + dy;
                    if (IsPathNodeReachable(x, y, startX) && HasJumpArcClearance(current, new Point(x, y)))
                    {
                        yield return new Point(x, y);
                        break;
                    }
                }
            }

            for (int dir = -1; dir <= 1; dir += 2)
            {
                for (int dx = 2; dx <= 7; dx++)
                {
                    int x = current.X + dir * dx;
                    for (int dy = -8; dy <= 4; dy++)
                    {
                        int y = current.Y + dy;
                        if (IsPathNodeReachable(x, y, startX) && HasJumpArcClearance(current, new Point(x, y)))
                        {
                            yield return new Point(x, y);
                            break;
                        }
                    }
                }
            }

            for (int dx = -2; dx <= 2; dx++)
            {
                int x = current.X + dx;
                for (int y = current.Y + 2; y <= current.Y + 12; y++)
                {
                    if (IsPathNodeReachable(x, y, startX) && HasDropClearance(current.X, current.Y, x, y))
                    {
                        yield return new Point(x, y);
                        break;
                    }
                }
            }

            if (HasRopeNear(current.X, current.Y, out int ropeX))
            {
                for (int y = current.Y - 2; y >= current.Y - 18; y--)
                {
                    if (IsPathNodeReachable(ropeX, y, startX))
                    {
                        yield return new Point(ropeX, y);
                        break;
                    }
                }
            }
        }

        private static bool IsPathNodeReachable(int x, int y, int startX)
        {
            return Math.Abs(x - startX) <= 62
                && y != int.MinValue
                && HasBodyClearanceAt(x, y)
                && IsStandableTile(x, y);
        }

        private static int MovementCost(Point from, Point to)
        {
            int dx = Math.Abs(to.X - from.X);
            int dy = Math.Abs(to.Y - from.Y);
            return dx + dy * 2 + (to.Y < from.Y ? 5 : 0);
        }

        private static bool HasJumpArcClearance(Point from, Point to)
        {
            int minY = Math.Min(from.Y, to.Y) - 4;
            int maxY = Math.Max(from.Y, to.Y) - 1;
            int minX = Math.Min(from.X, to.X);
            int maxX = Math.Max(from.X, to.X);
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

        private static bool HasDropClearance(int fromX, int fromY, int toX, int toY)
        {
            int minX = Math.Min(fromX, toX);
            int maxX = Math.Max(fromX, toX);
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = fromY - 3; y <= toY - 1; y++)
                {
                    if (IsNavigationSolid(x, y))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool HasRopeNear(int x, int feetY, out int ropeX)
        {
            for (int xOffset = -2; xOffset <= 2; xOffset++)
            {
                int checkX = x + xOffset;
                for (int y = feetY + 3; y >= feetY - 16; y--)
                {
                    if (IsRopeTile(checkX, y))
                    {
                        ropeX = checkX;
                        return true;
                    }
                }
            }

            ropeX = 0;
            return false;
        }

        private static bool TryFindSmartRouteTarget(NPC npc, tsorcRevampGlobalNPC globalNPC, Player player, bool playerWellAbove, bool playerWellBelow, out Vector2 target, out int direction)
        {
            int feetY = GetFeetTileY(npc);
            int centerX = (int)(npc.Center.X / 16f);
            int playerTileX = (int)(player.Center.X / 16f);
            int currentGroundY = FindNearbyGroundY(centerX, feetY, 2, 3);
            if (currentGroundY == int.MinValue)
            {
                currentGroundY = feetY;
            }

            int bestScore = int.MinValue;
            int bestX = centerX;
            int bestGroundY = currentGroundY;
            string bestKind = "none";
            int direct = player.Center.X >= npc.Center.X ? 1 : -1;

            if (playerWellAbove && TryFindRouteRopeTarget(npc, feetY, playerTileX, out int ropeX, out int ropeScore))
            {
                int routePenalty = ScoreRouteFeasibility(centerX, feetY, ropeX, out string ropeBlock);
                ropeScore -= routePenalty;
                if (ropeScore > bestScore)
                {
                    bestScore = ropeScore;
                    bestX = ropeX;
                    bestGroundY = currentGroundY;
                    bestKind = $"rope penalty={routePenalty} block={ropeBlock}";
                }
            }

            for (int dir = -1; dir <= 1; dir += 2)
            {
                for (int distance = 2; distance <= 42; distance++)
                {
                    int x = centerX + dir * distance;
                    int groundY = FindNearbyGroundY(x, feetY, 12, 14);
                    if (groundY == int.MinValue || !HasBodyClearanceAt(x, groundY))
                    {
                        continue;
                    }

                    int score = 0;
                    int surfaceGain = currentGroundY - groundY;
                    int surfaceDrop = groundY - currentGroundY;
                    int playerXDistanceTiles = Math.Abs(playerTileX - x);
                    int overheadOpening = CountOpenTilesUpward(x, groundY - 1, 12);
                    int feasibilityPenalty = ScoreRouteFeasibility(centerX, feetY, x, out string blockedReason);
                    bool hasPlatformAbove = HasPlatformAbove(x, groundY, 12);
                    bool hasDropAccess = playerWellBelow && (surfaceDrop >= 2 || HasPlatformAtColumn(x, groundY + 1, 5));

                    score -= distance / 2;
                    score -= playerXDistanceTiles / 3;
                    score -= feasibilityPenalty;
                    if (dir == direct)
                    {
                        score += 4;
                    }
                    if (globalNPC.NavBlockedDirectionTimer > 0 && globalNPC.NavBlockedDirection == dir)
                    {
                        score -= 24;
                    }
                    if (ContainsClosedDoor(x, groundY))
                    {
                        score += 16;
                    }

                    if (playerWellAbove)
                    {
                        score += surfaceGain * 10;
                        score += overheadOpening;
                        if (hasPlatformAbove)
                        {
                            score += 18;
                        }
                        if (surfaceGain <= 0 && overheadOpening < 5 && !hasPlatformAbove)
                        {
                            score -= 14;
                        }
                    }
                    else if (playerWellBelow)
                    {
                        score += surfaceDrop * 8;
                        if (hasDropAccess)
                        {
                            score += 22;
                        }
                        if (surfaceGain > 2)
                        {
                            score -= surfaceGain * 4;
                        }
                    }
                    else
                    {
                        score += Math.Max(0, 10 - playerXDistanceTiles);
                    }

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestX = x;
                        bestGroundY = groundY;
                        bestKind = $"surface gain={surfaceGain} drop={surfaceDrop} open={overheadOpening} penalty={feasibilityPenalty} block={blockedReason}";
                    }
                }
            }

            if (bestScore < 8)
            {
                target = Vector2.Zero;
                direction = 0;
                globalNPC.LastWaypointResult = $"smart:route none best={bestScore}";
                return false;
            }

            target = new Vector2(bestX * 16f + 8f, bestGroundY * 16f - npc.height / 2f);
            direction = target.X >= npc.Center.X ? 1 : -1;
            globalNPC.LastWaypointResult = $"smart:route score={bestScore} kind={bestKind}";
            return true;
        }

        private static bool TryFindRouteRopeTarget(NPC npc, int feetY, int playerTileX, out int ropeX, out int score)
        {
            int centerX = (int)(npc.Center.X / 16f);
            int bestScore = int.MinValue;
            int bestX = 0;
            for (int xOffset = -34; xOffset <= 34; xOffset++)
            {
                int x = centerX + xOffset;
                for (int y = feetY + 4; y >= feetY - 20; y--)
                {
                    if (!IsRopeTile(x, y))
                    {
                        continue;
                    }

                    int distance = Math.Abs(xOffset);
                    int playerDistance = Math.Abs(playerTileX - x);
                    int candidateScore = 50 - distance - playerDistance / 2;
                    if (HasBodyClearanceAt(x, feetY))
                    {
                        candidateScore += 8;
                    }
                    if (candidateScore > bestScore)
                    {
                        bestScore = candidateScore;
                        bestX = x;
                    }
                }
            }

            ropeX = bestX;
            score = bestScore;
            return bestScore > int.MinValue;
        }

        private static bool TryFindVerticalRouteTarget(NPC npc, tsorcRevampGlobalNPC globalNPC, Player player, out Vector2 target, out int direction)
        {
            int feetY = GetFeetTileY(npc);
            int centerX = (int)(npc.Center.X / 16f);
            int currentGroundY = FindNearbyGroundY(centerX, feetY, 2, 3);
            if (currentGroundY == int.MinValue)
            {
                currentGroundY = feetY;
            }

            int bestScore = int.MinValue;
            int bestX = centerX;
            int bestGroundY = feetY;
            int bestFeasibilityPenalty = 0;
            int bestSurfaceGain = 0;
            int bestOpening = 0;
            int direct = player.Center.X >= npc.Center.X ? 1 : -1;

            for (int dir = -1; dir <= 1; dir += 2)
            {
                for (int distance = 3; distance <= 34; distance++)
                {
                    int x = centerX + dir * distance;
                    int groundY = FindNearbyGroundY(x, feetY, 8, 7);
                    if (groundY == int.MinValue || !HasBodyClearanceAt(x, groundY))
                    {
                        continue;
                    }

                    int score = 0;
                    int surfaceGain = currentGroundY - groundY;
                    int playerXDistanceTiles = (int)(Math.Abs(player.Center.X / 16f - x));
                    int overheadOpening = CountOpenTilesUpward(x, groundY - 1, 12);
                    int feasibilityPenalty = ScoreRouteFeasibility(centerX, feetY, x, out string blockedReason);

                    score += surfaceGain * 10;
                    score += overheadOpening;
                    score -= distance / 2;
                    score -= playerXDistanceTiles / 3;
                    score -= feasibilityPenalty;

                    if (dir == direct)
                    {
                        score += 4;
                    }
                    if (globalNPC.NavBlockedDirectionTimer > 0 && globalNPC.NavBlockedDirection == dir)
                    {
                        score -= 24;
                    }
                    if (ContainsClosedDoor(x, groundY))
                    {
                        score += 16;
                    }
                    if (HasPlatformAbove(x, groundY, 12))
                    {
                        score += 14;
                    }
                    if (surfaceGain <= 0 && overheadOpening < 5 && !HasPlatformAbove(x, groundY, 8))
                    {
                        score -= 12;
                    }

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestX = x;
                        bestGroundY = groundY;
                        bestFeasibilityPenalty = feasibilityPenalty;
                        bestSurfaceGain = surfaceGain;
                        bestOpening = overheadOpening;
                        globalNPC.LastWaypointResult = $"smart:route candidate score={score} gain={surfaceGain} open={overheadOpening} penalty={feasibilityPenalty} block={blockedReason}";
                    }
                }
            }

            if (bestScore < 8)
            {
                target = Vector2.Zero;
                direction = 0;
                globalNPC.LastWaypointResult = $"smart:route none best={bestScore}";
                return false;
            }

            target = new Vector2(bestX * 16f + 8f, bestGroundY * 16f - npc.height / 2f);
            direction = target.X >= npc.Center.X ? 1 : -1;
            globalNPC.LastWaypointResult = $"smart:route score={bestScore} gain={bestSurfaceGain} open={bestOpening} penalty={bestFeasibilityPenalty}";
            return true;
        }

        private static int ScoreVerticalSearchDirection(int frontX, int feetY, int direction)
        {
            int score = 0;
            for (int offset = 0; offset <= 18; offset++)
            {
                int x = frontX + direction * offset;
                int obstacle = GetObstacleHeight(x, feetY);
                int drop = GetDropDepth(x, feetY, 7);

                if (ContainsClosedDoor(x, feetY))
                {
                    score += 20;
                }
                if (obstacle == 1)
                {
                    score += 8;
                }
                else if (obstacle >= 2 && obstacle <= 4)
                {
                    score += 4;
                }
                else if (obstacle >= 5)
                {
                    score -= 10;
                }

                if (drop <= 2)
                {
                    score += 2;
                }
                else if (drop >= 6)
                {
                    score -= 4;
                }

                if (HasBodyClearanceAt(x, feetY - 1) || HasBodyClearanceAt(x, feetY - 2))
                {
                    score += 1;
                }
            }

            return score;
        }

        private static bool TryHandleTerrain(NPC npc, tsorcRevampGlobalNPC globalNPC, int direction, float topSpeed, int doorBreakingDamage, ref SmartNavDebugFrame debugFrame)
        {
            int frontX = GetFrontTileX(npc, direction);
            int feetY = GetFeetTileY(npc);
            debugFrame.FrontX = frontX;
            debugFrame.FeetY = feetY;
            debugFrame.TerrainScan = BuildTerrainScan(frontX, feetY, direction);

            bool verticalSearch = globalNPC.LastNavIntent == "smart:vertical-search"
                && !debugFrame.LineOfSight
                && debugFrame.PlayerDeltaY < -72f
                && globalNPC.NavExploreTimer > 0;
            bool routeTarget = globalNPC.LastNavIntent == "smart:route-target" && globalNPC.WaypointTimer > 0;
            bool playerBelow = debugFrame.PlayerDeltaY > 48f;
            if (TryDropThroughPlatform(npc, globalNPC, playerBelow, debugFrame.PlayerDeltaX, ref debugFrame))
            {
                return true;
            }

            if (globalNPC.NavExploreTimer > 0 && !verticalSearch && globalNPC.LastNavIntent == "smart:backup")
            {
                globalNPC.NavExploreTimer--;
                int backupDirection = globalNPC.NavExploreDirection == 0 ? -direction : globalNPC.NavExploreDirection;
                npc.direction = backupDirection;
                npc.spriteDirection = backupDirection;
                npc.velocity.X = backupDirection * MathHelper.Clamp(topSpeed * 0.9f, 0.8f, 2.2f);
                debugFrame.OverrideHorizontal = true;
                debugFrame.Action = "back-up";
                debugFrame.Reason = $"timer={globalNPC.NavExploreTimer}";

                if (globalNPC.NavExploreTimer == 0 && HasHeadroomForJump(npc, -backupDirection, 3))
                {
                    float jumpPower = MathHelper.Clamp(globalNPC.MaxJumpPower * 0.9f, 6.8f, globalNPC.MaxJumpPower);
                    Jump(npc, globalNPC, -backupDirection, jumpPower, 1.4f, 24);
                    debugFrame.Action = "backup-jump";
                    debugFrame.Reason = "cleared-overhang";
                    debugFrame.JumpPower = jumpPower;
                    debugFrame.Boost = 1.4f;
                }

                return true;
            }
            if (verticalSearch)
            {
                globalNPC.NavExploreTimer--;
                debugFrame.Action = "vertical-search";
                debugFrame.Reason = $"timer={globalNPC.NavExploreTimer}";
            }
            else if (routeTarget)
            {
                debugFrame.Action = "route-target";
                debugFrame.Reason = $"target=({globalNPC.WaypointTarget.X / 16f:F1},{globalNPC.WaypointTarget.Y / 16f:F1}) timer={globalNPC.WaypointTimer}";
            }

            if (TryBreakDoor(npc, globalNPC, frontX, feetY, direction, doorBreakingDamage))
            {
                debugFrame.Action = "door";
                debugFrame.Reason = "door-blocking";
                return false;
            }
            if (routeTarget && TryExecuteRouteEdge(npc, globalNPC, direction, feetY, ref debugFrame))
            {
                return true;
            }

            int obstacleHeight = GetObstacleHeight(frontX, feetY);
            debugFrame.ObstacleHeight = obstacleHeight;
            if (obstacleHeight > 0)
            {
                if (routeTarget && obstacleHeight >= 5 && globalNPC.StuckTimer < 10)
                {
                    debugFrame.Action = "route-blocked";
                    debugFrame.Reason = $"target-blocked height={obstacleHeight}";
                    globalNPC.StuckTimer += 2;
                    if (globalNPC.StuckTimer >= 6)
                    {
                        globalNPC.NavBlockedDirection = direction;
                        globalNPC.NavBlockedDirectionTimer = 180;
                        globalNPC.WaypointTimer = 0;
                        globalNPC.NavExploreTimer = 0;
                        globalNPC.LastNavIntent = "smart:vertical-search";
                        globalNPC.LastWaypointResult = $"smart:route abandoned blocked-dir={direction} height={obstacleHeight}";
                    }
                    npc.velocity.X *= 0.5f;
                    return true;
                }

                if (obstacleHeight == 1 && HasHeadroomForJump(npc, direction, obstacleHeight))
                {
                    float stepHop = MathHelper.Clamp(globalNPC.MaxJumpPower * 0.48f, 3.8f, 4.6f);
                    float stepBoost = MathHelper.Clamp(topSpeed * 0.7f, 0.85f, 1.4f);
                    Jump(npc, globalNPC, direction, stepHop, stepBoost, 10);
                    debugFrame.Action = "step-hop";
                    debugFrame.Reason = npc.collideX ? "height=1-collide" : "height=1";
                    debugFrame.JumpPower = stepHop;
                    debugFrame.Boost = stepBoost;
                    return false;
                }

                if (obstacleHeight <= 4 && HasHeadroomForJump(npc, direction, obstacleHeight))
                {
                    float jumpPower = JumpPowerForObstacle(globalNPC, obstacleHeight);
                    float boost = MathHelper.Clamp(topSpeed * 0.6f, 0.75f, 2.5f);
                    Jump(npc, globalNPC, direction, jumpPower, boost, 14);
                    debugFrame.Action = "obstacle-jump";
                    debugFrame.Reason = $"height={obstacleHeight}";
                    debugFrame.JumpPower = jumpPower;
                    debugFrame.Boost = boost;
                    return true;
                }

                globalNPC.StuckTimer++;
                if (globalNPC.StuckTimer >= 45 && HasHeadroomForJump(npc, -direction, 2))
                {
                    float jumpPower = MathHelper.Clamp(globalNPC.MaxJumpPower, 7f, 9f);
                    Jump(npc, globalNPC, -direction, jumpPower, 1.75f, 22);
                    globalNPC.StuckTimer = 0;
                    debugFrame.OverrideHorizontal = true;
                    debugFrame.Action = "escape-jump";
                    debugFrame.Reason = "blocked-too-long";
                    debugFrame.JumpPower = jumpPower;
                    debugFrame.Boost = 1.75f;
                    return true;
                }

                if (globalNPC.StuckTimer >= 24)
                {
                    globalNPC.NavExploreDirection = -direction;
                    globalNPC.NavExploreTimer = obstacleHeight > 4 ? 28 : 18;
                    globalNPC.LastNavIntent = "smart:backup";
                    globalNPC.NavBlockedDirection = direction;
                    globalNPC.NavBlockedDirectionTimer = 120;
                    npc.velocity.X = -direction * MathHelper.Clamp(topSpeed * 0.8f, 0.7f, 2f);
                    debugFrame.OverrideHorizontal = true;
                    debugFrame.Action = "start-back-up";
                    debugFrame.Reason = obstacleHeight > 4 ? "too-tall" : "no-headroom";
                    return true;
                }

                debugFrame.Action = "blocked";
                debugFrame.Reason = obstacleHeight > 4 ? "too-tall" : "no-headroom";
                return true;
            }

            globalNPC.StuckTimer = 0;

            if (debugFrame.PlayerDeltaY < -64f && TryJumpToPlatformAhead(npc, globalNPC, direction, feetY, ref debugFrame))
            {
                return true;
            }
            if (debugFrame.PlayerDeltaY < -64f && TryClimbRope(npc, globalNPC, direction, feetY, ref debugFrame))
            {
                return true;
            }

            int dropDepth = GetDropDepth(frontX, feetY, 6);
            debugFrame.DropDepth = dropDepth;
            if (dropDepth <= 1)
            {
                if (!routeTarget && !verticalSearch)
                {
                    debugFrame.Action = "walk";
                    debugFrame.Reason = dropDepth == 0 ? "level-ground" : "small-drop";
                }
                return false;
            }

            if (playerBelow && TryFindDropLanding(frontX, feetY, direction, out int dropLandingDepth))
            {
                debugFrame.DropDepth = dropLandingDepth;
                debugFrame.Action = "descend-drop";
                debugFrame.Reason = $"player-below drop={dropLandingDepth}";
                return false;
            }

            if (TryMeasureGap(frontX, feetY, direction, out int gapTiles, out int landingDrop, out int landingX))
            {
                debugFrame.GapTiles = gapTiles;
                debugFrame.LandingDrop = landingDrop;
                debugFrame.LandingX = landingX;

                if (gapTiles <= 5 && landingDrop <= 2)
                {
                    if (gapTiles <= 1)
                    {
                        debugFrame.Action = "walk";
                        debugFrame.Reason = $"tiny-gap gap={gapTiles},drop={landingDrop}";
                        return false;
                    }

                    float jumpPower = JumpPowerForGap(globalNPC, gapTiles, landingDrop);
                    float boost = BoostForGap(globalNPC, gapTiles);
                    Jump(npc, globalNPC, direction, jumpPower, boost, 18 + gapTiles * 2);
                    debugFrame.Action = "gap-jump";
                    debugFrame.Reason = $"gap={gapTiles},drop={landingDrop}";
                    debugFrame.JumpPower = jumpPower;
                    debugFrame.Boost = boost;
                    return true;
                }

                debugFrame.Action = "halt-gap";
                debugFrame.Reason = $"too-far gap={gapTiles},drop={landingDrop}";
                npc.velocity.X *= 0.65f;
                return true;
            }

            debugFrame.GapTiles = -1;
            debugFrame.Action = "halt-gap";
            debugFrame.Reason = "no-landing";
            npc.velocity.X *= 0.65f;
            return true;
        }

        private static bool TryBreakDoor(NPC npc, tsorcRevampGlobalNPC globalNPC, int frontX, int feetY, int direction, int doorBreakingDamage)
        {
            if (doorBreakingDamage <= 0)
            {
                return false;
            }

            if (!TryFindClosedDoor(frontX, feetY, out int doorX, out int doorY))
            {
                return false;
            }

            int openY = GetDoorOpenY(doorX, doorY);
            if (Main.netMode != NetmodeID.MultiplayerClient && WorldGen.OpenDoor(doorX, openY, direction))
            {
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.ToggleDoorState, -1, -1, null, 0, doorX, openY, direction);
                }

                npc.velocity.X += 0.65f * direction;
                return true;
            }

            npc.velocity.X = 0.2f * -direction;
            if (Main.GameUpdateCount % 30 != 0)
            {
                return true;
            }

            globalNPC.DoorBreakProgress += doorBreakingDamage;
            WorldGen.KillTile(doorX, doorY, true, true);
            if (globalNPC.DoorBreakProgress >= 10 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                globalNPC.DoorBreakProgress = 0;
                if (!WorldGen.OpenDoor(doorX, openY, direction))
                {
                    npc.velocity.X = 0.2f * -direction;
                }
                else if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.ToggleDoorState, -1, -1, null, 0, doorX, openY, direction);
                }
            }

            return true;
        }

        private static bool TryFindClosedDoor(int frontX, int feetY, out int doorX, out int doorY)
        {
            for (int xOffset = -2; xOffset <= 2; xOffset++)
            {
                int x = frontX + xOffset;
                for (int y = feetY - 7; y <= feetY + 2; y++)
                {
                    if (WorldGen.InWorld(x, y) && Main.tile[x, y].HasTile && Main.tile[x, y].TileType == TileID.ClosedDoor)
                    {
                        doorX = x;
                        doorY = y;
                        return true;
                    }
                }
            }

            doorX = 0;
            doorY = 0;
            return false;
        }

        private static int GetDoorOpenY(int doorX, int doorY)
        {
            int openY = doorY;
            while (WorldGen.InWorld(doorX, openY + 1)
                && Main.tile[doorX, openY + 1].HasTile
                && Main.tile[doorX, openY + 1].TileType == TileID.ClosedDoor)
            {
                openY++;
            }

            return openY;
        }

        private static bool ContainsClosedDoor(int x, int feetY)
        {
            return TryFindClosedDoor(x, feetY, out _, out _);
        }

        private static void Jump(NPC npc, tsorcRevampGlobalNPC globalNPC, int direction, float jumpPower, float horizontalBoost, int cooldown)
        {
            npc.velocity.Y = -jumpPower;
            npc.velocity.X += horizontalBoost * direction;
            npc.direction = direction;
            npc.spriteDirection = direction;
            globalNPC.NavJumpCooldown = cooldown;
            npc.netUpdate = true;
        }

        private static float JumpPowerForObstacle(tsorcRevampGlobalNPC globalNPC, int obstacleHeight)
        {
            float target = obstacleHeight switch
            {
                1 => 5f,
                2 => 6f,
                3 => 7f,
                _ => 8f
            };

            return MathHelper.Clamp(target, 4.5f, globalNPC.MaxJumpPower);
        }

        private static float JumpPowerForGap(tsorcRevampGlobalNPC globalNPC, int gapTiles, int landingDrop)
        {
            float target = 5.2f + gapTiles * 0.72f - landingDrop * 0.2f;
            return MathHelper.Clamp(target, 5.5f, globalNPC.MaxJumpPower);
        }

        private static float BoostForGap(tsorcRevampGlobalNPC globalNPC, int gapTiles)
        {
            float target = 2.1f + gapTiles * 0.72f;
            return MathHelper.Clamp(target, 2.2f, globalNPC.MaxJumpBoost);
        }

        private static bool TryDropThroughPlatform(NPC npc, tsorcRevampGlobalNPC globalNPC, bool playerBelow, float playerDeltaX, ref SmartNavDebugFrame debugFrame)
        {
            if (!playerBelow || !IsStandingOnPlatform(npc) || Math.Abs(playerDeltaX) > 560f)
            {
                return false;
            }

            globalNPC.LastNavIntent = "smart:platform-drop";
            globalNPC.NavExploreTimer = 12;
            npc.noTileCollide = true;
            npc.velocity.Y = Math.Max(npc.velocity.Y, 1.2f);
            debugFrame.OverrideHorizontal = false;
            debugFrame.Action = "platform-drop";
            debugFrame.Reason = "player-below";
            return true;
        }

        private static bool TryJumpToPlatformAhead(NPC npc, tsorcRevampGlobalNPC globalNPC, int direction, int feetY, ref SmartNavDebugFrame debugFrame)
        {
            for (int xOffset = 0; xOffset <= 6; xOffset++)
            {
                int x = GetFrontTileX(npc, direction) + direction * xOffset;
                for (int y = feetY - 3; y >= feetY - 11; y--)
                {
                    if (!IsPlatformTile(x, y) || !HasBodyClearanceAt(x, y))
                    {
                        continue;
                    }

                    int heightTiles = feetY - y;
                    float jumpPower = MathHelper.Clamp(4.7f + heightTiles * 0.42f, 5.4f, globalNPC.MaxJumpPower);
                    float boost = MathHelper.Clamp(0.9f + xOffset * 0.28f, 0.9f, globalNPC.MaxJumpBoost);
                    Jump(npc, globalNPC, direction, jumpPower, boost, 16);
                    debugFrame.Action = "platform-jump";
                    debugFrame.Reason = $"platform=({x},{y})";
                    debugFrame.JumpPower = jumpPower;
                    debugFrame.Boost = boost;
                    return true;
                }
            }

            return false;
        }

        private static bool TryPassFurnitureObstacle(NPC npc, tsorcRevampGlobalNPC globalNPC, int frontX, int feetY, int direction, ref SmartNavDebugFrame debugFrame)
        {
            return false;
        }

        private static bool TryStartFurniturePass(NPC npc, tsorcRevampGlobalNPC globalNPC, int direction, out string reason)
        {
            reason = "";
            return false;
        }

        private static void StartFurniturePass(NPC npc, tsorcRevampGlobalNPC globalNPC, int direction)
        {
            int passDirection = direction == 0 ? (npc.direction == 0 ? 1 : npc.direction) : direction;
            globalNPC.SmartFurniturePassDirection = passDirection;
            globalNPC.SmartFurniturePassTimer = 14;
            globalNPC.SmartFurniturePassCooldown = 42;
            npc.noTileCollide = true;
            npc.noGravity = true;
            npc.velocity.X = MathHelper.Clamp(npc.velocity.X + 1.25f * passDirection, -2.35f, 2.35f);
            npc.velocity.Y = 0f;
        }

        private static bool TryExecuteRouteEdge(NPC npc, tsorcRevampGlobalNPC globalNPC, int direction, int feetY, ref SmartNavDebugFrame debugFrame)
        {
            if (globalNPC.LastWaypointResult.Contains("edge=rope") && TryClimbRope(npc, globalNPC, direction, feetY, ref debugFrame))
            {
                debugFrame.Reason += ",route-edge";
                return true;
            }

            if (globalNPC.LastWaypointResult.Contains("edge=jump"))
            {
                int targetX = (int)(globalNPC.WaypointTarget.X / 16f);
                int targetFeetY = (int)((globalNPC.WaypointTarget.Y + npc.height / 2f) / 16f);
                int dx = Math.Abs(targetX - (int)(npc.Center.X / 16f));
                int rise = Math.Max(0, feetY - targetFeetY);
                float jumpPower = MathHelper.Clamp(5.5f + rise * 0.45f + dx * 0.18f, 5.6f, globalNPC.MaxJumpPower);
                float boost = MathHelper.Clamp(0.95f + dx * 0.24f, 1.1f, globalNPC.MaxJumpBoost);
                Jump(npc, globalNPC, direction, jumpPower, boost, 14 + Math.Min(dx, 8));
                debugFrame.Action = "route-jump";
                debugFrame.Reason = $"target=({targetX},{targetFeetY}) dx={dx} rise={rise}";
                debugFrame.JumpPower = jumpPower;
                debugFrame.Boost = boost;
                return true;
            }

            if (globalNPC.LastWaypointResult.Contains("edge=drop"))
            {
                debugFrame.Action = "route-drop";
                debugFrame.Reason = $"target=({globalNPC.WaypointTarget.X / 16f:F1},{globalNPC.WaypointTarget.Y / 16f:F1})";
                return false;
            }

            return false;
        }

        private static void UpdateTemporaryTilePassThrough(NPC npc, tsorcRevampGlobalNPC globalNPC)
        {
            bool temporaryPass = globalNPC.LastNavIntent == "smart:platform-drop";
            if (!temporaryPass)
            {
                if (globalNPC.SmartFurniturePassTimer <= 0)
                {
                    npc.noTileCollide = false;
                }
                return;
            }

            globalNPC.NavExploreTimer--;
            bool shouldStop = globalNPC.NavExploreTimer <= 0;
            if (globalNPC.LastNavIntent == "smart:platform-drop" && !IsOverlappingPlatform(npc) && !IsStandingOnPlatform(npc))
            {
                shouldStop = true;
            }

            npc.noTileCollide = !shouldStop;
            if (shouldStop)
            {
                globalNPC.LastNavIntent = "smart:direct";
                globalNPC.NavExploreTimer = 0;
            }
        }

        private static bool TryClimbRope(NPC npc, tsorcRevampGlobalNPC globalNPC, int direction, int feetY, ref SmartNavDebugFrame debugFrame)
        {
            if (!FindRopeXNear(npc, direction, feetY, out int ropeX))
            {
                return false;
            }

            float ropeCenter = ropeX * 16f + 8f;
            globalNPC.LastNavIntent = "smart:rope-climb";
            globalNPC.NavExploreTimer = 70;
            npc.noGravity = true;
            npc.velocity.X = MathHelper.Clamp((ropeCenter - npc.Center.X) * 0.08f, -1.15f, 1.15f);
            npc.velocity.Y = Math.Min(npc.velocity.Y, -2.4f);
            debugFrame.OverrideHorizontal = true;
            debugFrame.Action = "rope-climb";
            debugFrame.Reason = $"ropeX={ropeX}";
            return true;
        }

        private static bool FindRopeXNear(NPC npc, int direction, int feetY, out int ropeX)
        {
            int centerX = (int)(npc.Center.X / 16f);
            for (int xOffset = 0; xOffset <= 4; xOffset++)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    int x = centerX + direction * xOffset + side;
                    for (int y = feetY + 2; y >= feetY - 12; y--)
                    {
                        if (IsRopeTile(x, y))
                        {
                            ropeX = x;
                            return true;
                        }
                    }
                }
            }

            ropeX = 0;
            return false;
        }

        private static bool IsRopeTile(int x, int y)
        {
            if (!WorldGen.InWorld(x, y))
            {
                return false;
            }

            Tile tile = Main.tile[x, y];
            return tile.HasTile && !tile.IsActuated && tile.TileType == TileID.Rope;
        }

        private static bool TryFindDropLanding(int frontX, int feetY, int direction, out int landingDepth)
        {
            landingDepth = GetDropDepth(frontX, feetY, 12);
            if (landingDepth >= 2 && landingDepth <= 10 && HasBodyClearanceAt(frontX, feetY + landingDepth))
            {
                return true;
            }

            for (int offset = 1; offset <= 3; offset++)
            {
                int x = frontX + direction * offset;
                int drop = GetDropDepth(x, feetY, 12);
                if (drop >= 2 && drop <= 10 && HasBodyClearanceAt(x, feetY + drop))
                {
                    landingDepth = drop;
                    return true;
                }
            }

            return false;
        }

        private static int FindNearbyGroundY(int x, int feetY, int maxUp, int maxDown)
        {
            for (int y = feetY - maxUp; y <= feetY + maxDown; y++)
            {
                if (IsStandableTile(x, y))
                {
                    return y;
                }
            }

            return int.MinValue;
        }

        private static int ScoreRouteFeasibility(int startX, int feetY, int targetX, out string blockedReason)
        {
            int direction = targetX >= startX ? 1 : -1;
            int penalty = 0;
            blockedReason = "clear";

            for (int x = startX + direction; x != targetX + direction; x += direction)
            {
                int obstacle = GetObstacleHeight(x, feetY);
                if (ContainsClosedDoor(x, feetY))
                {
                    penalty += 1;
                    continue;
                }

                if (obstacle >= 5)
                {
                    penalty += 80;
                    blockedReason = $"wall@{x}";
                    break;
                }
                if (obstacle >= 2)
                {
                    penalty += obstacle * 2;
                }

                int drop = GetDropDepth(x, feetY, 6);
                if (drop > 2)
                {
                    if (!TryMeasureGap(x, feetY, direction, out int gapTiles, out int landingDrop, out _)
                        || gapTiles > 5
                        || landingDrop > 2)
                    {
                        penalty += 60;
                        blockedReason = $"gap@{x}";
                        break;
                    }

                    penalty += Math.Max(0, gapTiles - 2) * 3;
                    x += direction * Math.Max(0, gapTiles - 1);
                }
            }

            return penalty;
        }

        private static int CountOpenTilesUpward(int x, int startY, int maxTiles)
        {
            int count = 0;
            for (int y = startY; y > startY - maxTiles; y--)
            {
                if (!WorldGen.InWorld(x, y) || IsNavigationSolid(x, y))
                {
                    break;
                }

                count++;
            }

            return count;
        }

        private static bool HasPlatformAbove(int x, int feetY, int maxHeight)
        {
            for (int y = feetY - 2; y >= feetY - maxHeight; y--)
            {
                if (!WorldGen.InWorld(x, y))
                {
                    continue;
                }

                Tile tile = Main.tile[x, y];
                if (IsPlatformTile(x, y))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasPlatformAtColumn(int x, int startY, int maxDepth)
        {
            for (int y = startY; y <= startY + maxDepth; y++)
            {
                if (IsPlatformTile(x, y))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsGrounded(NPC npc)
        {
            if (npc.velocity.Y != 0f)
            {
                return false;
            }

            int left = (int)(npc.Left.X / 16f);
            int right = (int)((npc.Right.X - 1f) / 16f);
            int belowFeet = (int)((npc.Bottom.Y + 4f) / 16f);
            for (int x = left; x <= right; x++)
            {
                if (IsStandableTile(x, belowFeet))
                {
                    return true;
                }
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

        private static int GetFrontTileX(NPC npc, int direction)
        {
            return direction > 0
                ? (int)((npc.Right.X + 4f) / 16f)
                : (int)((npc.Left.X - 4f) / 16f);
        }

        private static int GetFeetTileY(NPC npc)
        {
            return (int)((npc.Bottom.Y - 1f) / 16f);
        }

        private static int GetObstacleHeight(int frontX, int feetY)
        {
            for (int height = 5; height >= 1; height--)
            {
                if (IsNavigationSolid(frontX, feetY - height))
                {
                    return height + 1;
                }
            }

            if (IsNavigationSolid(frontX, feetY))
            {
                return 1;
            }

            return 0;
        }

        private static bool HasHeadroomForJump(NPC npc, int direction, int obstacleHeight)
        {
            int frontX = GetFrontTileX(npc, direction);
            int headY = (int)((npc.Top.Y + 4f) / 16f);
            int feetY = GetFeetTileY(npc);
            int highestCheck = Math.Max(headY - 2, feetY - obstacleHeight - 4);
            for (int y = highestCheck; y <= headY; y++)
            {
                if (IsNavigationSolid(frontX, y))
                {
                    return false;
                }
            }

            return true;
        }

        private static int GetDropDepth(int x, int feetY, int maxDepth)
        {
            for (int depth = 0; depth <= maxDepth; depth++)
            {
                if (IsStandableTile(x, feetY + depth))
                {
                    return depth;
                }
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
                int checkX = frontX + direction * offset;
                int drop = GetDropDepth(checkX, feetY, 5);
                if (drop <= 2 && HasBodyClearanceAt(checkX, feetY + drop))
                {
                    gapTiles = offset;
                    landingDrop = drop;
                    landingX = checkX;
                    return true;
                }
            }

            return false;
        }

        private static string BuildTerrainScan(int frontX, int feetY, int direction)
        {
            string scan = "";
            for (int offset = 0; offset <= 8; offset++)
            {
                int x = frontX + direction * offset;
                int obstacle = GetObstacleHeight(x, feetY);
                int drop = GetDropDepth(x, feetY, 7);
                string token;
                if (ContainsClosedDoor(x, feetY))
                {
                    token = "D";
                }
                else if (obstacle > 0)
                {
                    token = "#" + obstacle;
                }
                else if (drop == 0)
                {
                    token = "_";
                }
                else if (drop <= 7)
                {
                    token = "v" + drop;
                }
                else
                {
                    token = "pit";
                }

                scan += offset == 0 ? token : "," + token;
            }

            return scan;
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

        private static bool IsStandingOnPlatform(NPC npc)
        {
            int left = (int)(npc.Left.X / 16f);
            int right = (int)((npc.Right.X - 1f) / 16f);
            int belowFeet = (int)((npc.Bottom.Y + 4f) / 16f);
            bool platform = false;
            for (int x = left; x <= right; x++)
            {
                if (IsPlatformTile(x, belowFeet))
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

        private static bool IsOverlappingPlatform(NPC npc)
        {
            int left = (int)(npc.Left.X / 16f);
            int right = (int)((npc.Right.X - 1f) / 16f);
            int top = (int)(npc.Top.Y / 16f);
            int bottom = (int)((npc.Bottom.Y - 1f) / 16f);
            for (int x = left; x <= right; x++)
            {
                for (int y = top; y <= bottom; y++)
                {
                    if (IsPlatformTile(x, y))
                    {
                        return true;
                    }
                }
            }

            return false;
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

        private static bool IsFurnitureObstacleTile(int x, int y)
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
                && type != TileID.Trees
                && type != TileID.ClosedDoor
                && type != TileID.OpenDoor
                && !TileID.Sets.Platforms[type]
                && !IsNavigationSolid(x, y);
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

            // Furniture such as anvils is solid to players but should not be treated as a terrain wall for nav.
            return !Main.tileFrameImportant[tile.TileType] || tile.TileType == TileID.ClosedDoor;
        }

        private static void LogSmartFighterDebug(NPC npc, tsorcRevampGlobalNPC globalNPC, Player player, SmartNavDebugFrame frame)
        {
            bool interesting = frame.Action != "walk"
                || !frame.LineOfSight
                || frame.ObstacleHeight > 0
                || frame.DropDepth > 1
                || frame.GapTiles != 0
                || globalNPC.BoredTimer > 0
                || globalNPC.StuckTimer > 0;
            if (!interesting)
            {
                return;
            }

            int now = (int)Main.GameUpdateCount;
            if (now - globalNPC.LastNavDebugLogTick < 30)
            {
                return;
            }
            globalNPC.LastNavDebugLogTick = now;

            try
            {
                string separator = Path.DirectorySeparatorChar.ToString();
                string logDir = Main.SavePath + separator + "Logs";
                Directory.CreateDirectory(logDir);
                string logPath = logDir + separator + "tsorcRevamp-smartfighter.log";
                string line = $"[{DateTime.Now:HH:mm:ss}] {npc.TypeName}#{npc.whoAmI} pos=({npc.Center.X / 16f:F1},{npc.Center.Y / 16f:F1}) player=({player.Center.X / 16f:F1},{player.Center.Y / 16f:F1}) delta=({frame.PlayerDeltaX:F0},{frame.PlayerDeltaY:F0}) vel=({npc.velocity.X:F2},{npc.velocity.Y:F2}) dir={frame.Direction} grounded={frame.Grounded} collide=({frame.CollideX},{frame.CollideY}) tilepass=({npc.noTileCollide},{npc.noGravity}) fpass={globalNPC.SmartFurniturePassDirection}/{globalNPC.SmartFurniturePassTimer} los={frame.LineOfSight} mode={frame.Mode} bored={globalNPC.BoredTimer} stuck={globalNPC.StuckTimer} blocked={globalNPC.NavBlockedDirection}/{globalNPC.NavBlockedDirectionTimer} explore={globalNPC.NavExploreDirection}/{globalNPC.NavExploreTimer} route=({globalNPC.WaypointTarget.X / 16f:F1},{globalNPC.WaypointTarget.Y / 16f:F1})/{globalNPC.WaypointTimer} result={globalNPC.LastWaypointResult} front=({frame.FrontX},{frame.FeetY}) obstacle={frame.ObstacleHeight} drop={frame.DropDepth} gap={frame.GapTiles} landing=({frame.LandingX},{frame.LandingDrop}) scan={frame.TerrainScan} action={frame.Action} reason={frame.Reason} jump={frame.JumpPower:F2} boost={frame.Boost:F2} cd={globalNPC.NavJumpCooldown} attack={frame.AttackAllowed}";
                File.AppendAllText(logPath, line + Environment.NewLine);
            }
            catch
            {
                // Test logging must never affect NPC behavior.
            }
        }

        private struct SmartNavDebugFrame
        {
            public bool Grounded;
            public bool LineOfSight;
            public bool AttackAllowed;
            public bool CollideX;
            public bool CollideY;
            public bool OverrideHorizontal;
            public int Direction;
            public int FrontX;
            public int FeetY;
            public int ObstacleHeight;
            public int DropDepth;
            public int GapTiles;
            public int LandingX;
            public int LandingDrop;
            public string Action;
            public string Reason;
            public string TerrainScan;
            public string Mode;
            public float PlayerDeltaX;
            public float PlayerDeltaY;
            public float JumpPower;
            public float Boost;
        }
    }
}
