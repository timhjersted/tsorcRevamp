using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace tsorcRevamp.NPCs
{
    // SmartFighter — Test 1 of three navigation experiments.
    //
    // Philosophy: smooth, committed, vanilla-feeling movement. No multi-step plan.
    //
    // The old BFS / route-target / vertical-search pathfinder was stripped (was the
    // source of the 50-second wall-pace deadlock and 90% airborne thrash in logs).
    // Behavior is now:
    //
    //   1. Direct chase toward the player when on the same floor or in LOS.
    //   2. If player is on a different floor and out of LOS, scan ±24 tiles for the
    //      nearest "ascent column" (gap in ceiling with reachable landing) or
    //      "descent column" (gap in floor with reachable landing below) and walk
    //      toward that column.
    //   3. The well-tested local terrain handlers (step-hop, obstacle-jump,
    //      gap-jump, platform-drop, platform-jump, rope-climb, door-break) handle
    //      the actual vertical movement once the NPC arrives at the column.
    //
    // Compared to SmartFighter3 (true multi-step routing), this gives up optimal
    // routing in exchange for predictable, vanilla-style behavior with no deadlock
    // states. It will fail to reach a player behind multiple walls/corners that
    // require non-greedy paths, but it never spends 50 seconds bouncing.
    public static class SmartFighterAI
    {
        // Per-NPC airborne jump commitment so chase steering can't reverse a planned jump mid-flight.
        // Keyed by NPC.whoAmI to avoid polluting tsorcRevampGlobalNPC.
        private static readonly Dictionary<int, (int dir, int timer)> JumpCommits = new Dictionary<int, (int, int)>();

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
            // Airborne X-commit: when a deliberate jump fires, lock X velocity toward the
            // launch direction for ~30 frames so the chase steering can't reverse the arc.
            if (JumpCommits.TryGetValue(npc.whoAmI, out var commit))
            {
                if (commit.timer > 0 && !grounded && commit.dir != 0)
                {
                    npc.velocity.X = MathHelper.Clamp(commit.dir * topSpeed * 1.1f, -topSpeed * 1.3f, topSpeed * 1.3f);
                }
                if (grounded || commit.timer <= 1) JumpCommits.Remove(npc.whoAmI);
                else JumpCommits[npc.whoAmI] = (commit.dir, commit.timer - 1);
            }
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

            bool canUseProjectile = globalNPC.AttackList.Count > 0
                && lineOfSight
                && npc.Distance(player.Center) <= attackRange
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

            // ====================================================================
            // SmartFighter is the "smooth vanilla-feeling" baseline. It does NOT
            // pathfind. It chooses a direction by answering a single question:
            //   "what floor is the player on, and which way is the nearest staircase
            //    or jumpable column toward that floor?"
            // The local terrain handlers (step-hop, gap-jump, platform-jump,
            // platform-drop, rope-climb, door-break) execute the actual movement.
            // ====================================================================

            float playerDeltaX = player.Center.X - npc.Center.X;
            float playerDeltaY = player.Center.Y - npc.Center.Y;
            bool playerWellAbove = playerDeltaY < -72f;
            bool playerWellBelow = playerDeltaY > 72f;
            int directToPlayer = playerDeltaX >= 0f ? 1 : -1;

            // Backup state still wins — if we're actively unsticking from a wall,
            // keep doing that until the timer expires.
            if (globalNPC.LastNavIntent == "smart:backup" && globalNPC.NavExploreTimer > 0)
            {
                return globalNPC.NavExploreDirection == 0 ? -directToPlayer : globalNPC.NavExploreDirection;
            }

            // Anti-flap: if we recently bonked a wall going `direct`, briefly
            // chase the opposite way to give us a chance to find a staircase.
            if (grounded && globalNPC.NavBlockedDirectionTimer > 0
                && globalNPC.NavBlockedDirection == directToPlayer && !lineOfSight)
            {
                globalNPC.LastNavIntent = "smart:direct";
                return -directToPlayer;
            }

            // In the air, we don't change direction. Whatever launched us is committed.
            if (!grounded)
            {
                return directToPlayer;
            }

            // Same floor / LOS / boredom-bored: just chase the player directly.
            // The local terrain handlers will fire any needed hops/jumps along the way.
            bool sameFloor = !playerWellAbove && !playerWellBelow;
            if (sameFloor || lineOfSight)
            {
                globalNPC.LastNavIntent = "smart:direct";
                return directToPlayer;
            }

            // Different floor + no LOS: walk toward the nearest column where we can
            // ascend (if player is above) or descend (if below) toward player's floor.
            int feetY = GetFeetTileY(npc);
            int playerFeetY = (int)((player.Bottom.Y - 1f) / 16f);
            int npcCol = (int)(npc.Center.X / 16f);
            int playerCol = (int)(player.Center.X / 16f);
            int passageCol = playerWellAbove
                ? FindNearestAscentColumn(npcCol, feetY, playerFeetY, playerCol)
                : FindNearestDescentColumn(npcCol, feetY, playerFeetY, playerCol);

            if (passageCol != int.MinValue)
            {
                globalNPC.LastNavIntent = playerWellAbove ? "smart:seek-up" : "smart:seek-down";
                int toPassage = passageCol >= npcCol ? 1 : -1;
                // If we're already at the passage column, fall to local handlers
                // (which will fire platform-jump / drop / rope-climb).
                if (Math.Abs(passageCol - npcCol) <= 1) return directToPlayer;
                return toPassage;
            }

            // No passage found in scan range — just pace toward the player's column
            // so we don't oscillate. Vanilla-style.
            globalNPC.LastNavIntent = "smart:direct";
            return directToPlayer;
        }

        // ---- Staircase / jumpable-column finder ----
        // We scan ±24 tiles around the NPC. A column counts as an "ascent" if it
        // has a clear vertical opening at least (rise+2) tiles tall AND a standable
        // platform within the NPC's jump reach (up to 8 tiles). For descents we
        // require a clear opening down toward the player's floor.

        private const int PassageScanRadius = 24;
        private const int MaxAscentReach = 8;
        private const int MaxDescentReach = 12;

        private static int FindNearestAscentColumn(int npcCol, int feetY, int targetFeetY, int playerCol)
        {
            int rise = Math.Max(1, feetY - targetFeetY);
            int requiredOpen = Math.Min(MaxAscentReach, rise + 2);
            int best = int.MinValue;
            int bestScore = int.MaxValue;
            for (int dx = -PassageScanRadius; dx <= PassageScanRadius; dx++)
            {
                int col = npcCol + dx;
                if (!IsAscentColumn(col, feetY, requiredOpen)) continue;
                int dist = Math.Abs(dx);
                // Prefer columns toward the player's X.
                int dirBias = Math.Sign(col - npcCol) == Math.Sign(playerCol - npcCol) ? -2 : 0;
                int score = dist + dirBias;
                if (score < bestScore) { bestScore = score; best = col; }
            }
            return best;
        }

        private static int FindNearestDescentColumn(int npcCol, int feetY, int targetFeetY, int playerCol)
        {
            int drop = Math.Max(1, targetFeetY - feetY);
            int requiredOpen = Math.Min(MaxDescentReach, drop + 2);
            int best = int.MinValue;
            int bestScore = int.MaxValue;
            for (int dx = -PassageScanRadius; dx <= PassageScanRadius; dx++)
            {
                int col = npcCol + dx;
                if (!IsDescentColumn(col, feetY, requiredOpen)) continue;
                int dist = Math.Abs(dx);
                int dirBias = Math.Sign(col - npcCol) == Math.Sign(playerCol - npcCol) ? -2 : 0;
                int score = dist + dirBias;
                if (score < bestScore) { bestScore = score; best = col; }
            }
            return best;
        }

        // Column has enough vertical clearance above NPC's feet for a jump up.
        private static bool IsAscentColumn(int col, int feetY, int requiredOpen)
        {
            int openTiles = 0;
            for (int y = feetY - 1; y >= feetY - requiredOpen - 4; y--)
            {
                if (IsNavigationSolid(col, y)) break;
                openTiles++;
            }
            // Must also have a standable platform at the top of the opening,
            // OR be a wide-open column (sky/outdoor).
            if (openTiles < requiredOpen) return false;
            // Look for a standable tile within reach above.
            for (int y = feetY - 2; y >= feetY - MaxAscentReach; y--)
            {
                if (IsStandableTile(col, y) && HasBodyClearanceAt(col, y - 1)) return true;
            }
            // Open ceiling counts as ascendable (player is somewhere above through the air).
            return openTiles >= requiredOpen + 1;
        }

        // Column has a drop opening at least requiredOpen tall, with a standable landing.
        private static bool IsDescentColumn(int col, int feetY, int requiredOpen)
        {
            int openTiles = 0;
            for (int y = feetY; y <= feetY + requiredOpen + 4; y++)
            {
                if (IsNavigationSolid(col, y)) break;
                openTiles++;
            }
            if (openTiles < requiredOpen) return false;
            // Standable landing within drop range
            for (int y = feetY + 2; y <= feetY + MaxDescentReach; y++)
            {
                if (IsStandableTile(col, y)) return true;
            }
            return false;
        }


        private static bool TryHandleTerrain(NPC npc, tsorcRevampGlobalNPC globalNPC, int direction, float topSpeed, int doorBreakingDamage, ref SmartNavDebugFrame debugFrame)
        {
            int frontX = GetFrontTileX(npc, direction);
            int feetY = GetFeetTileY(npc);
            debugFrame.FrontX = frontX;
            debugFrame.FeetY = feetY;
            debugFrame.TerrainScan = BuildTerrainScan(frontX, feetY, direction);

            // Vertical-search and route-target modes are removed. SmartFighter now only
            // dispatches: backup recovery, local terrain handlers, and door breaking.
            bool playerBelow = debugFrame.PlayerDeltaY > 48f;
            if (TryDropThroughPlatform(npc, globalNPC, playerBelow, debugFrame.PlayerDeltaX, ref debugFrame))
            {
                return true;
            }

            if (globalNPC.NavExploreTimer > 0 && globalNPC.LastNavIntent == "smart:backup")
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

            if (TryBreakDoor(npc, globalNPC, frontX, feetY, direction, doorBreakingDamage))
            {
                debugFrame.Action = "door";
                debugFrame.Reason = "door-blocking";
                return false;
            }

            int obstacleHeight = GetObstacleHeight(frontX, feetY);
            debugFrame.ObstacleHeight = obstacleHeight;
            if (obstacleHeight > 0)
            {
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
                debugFrame.Action = "walk";
                debugFrame.Reason = dropDepth == 0 ? "level-ground" : "small-drop";
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
            // Commit horizontal direction for the airborne phase so chase steering can't reverse mid-flight.
            JumpCommits[npc.whoAmI] = (direction, 30);
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
