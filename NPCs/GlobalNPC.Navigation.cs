using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Drawing;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;
using tsorcRevamp.Buffs;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Buffs.Runeterra.Magic;
using tsorcRevamp.Buffs.Runeterra.Summon;
using tsorcRevamp.Buffs.Weapons;
using tsorcRevamp.Buffs.Weapons.Summon;
using tsorcRevamp.Buffs.Weapons.Summon.WhipDebuffs;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Accessories.Defensive;
using tsorcRevamp.Items.Accessories.Damage;
using tsorcRevamp.Items.Armors.Melee;
using tsorcRevamp.Items.Debug;
using tsorcRevamp.Items.ItemCrates;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.Items.VanillaItems;
using tsorcRevamp.Items.Weapons.Magic.Runeterra;
using tsorcRevamp.Items.Weapons.Ranged;
using tsorcRevamp.Items.Weapons.Ranged.Runeterra;
using tsorcRevamp.Items.Weapons.Ranged.Specialist;
using tsorcRevamp.Items.Weapons.Summon;
using tsorcRevamp.Items.Weapons.Summon.Runeterra;
using tsorcRevamp.Items.Weapons.Summon.Whips;
using tsorcRevamp.Items.Weapons.Throwing;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.Fiends;
using tsorcRevamp.Projectiles.Ranged;
using tsorcRevamp.Projectiles.Summon;
using tsorcRevamp.Projectiles.Summon.Archer;
using tsorcRevamp.Projectiles.Summon.SamuraiBeetle;
using tsorcRevamp.Projectiles.Summon.Whips;
using tsorcRevamp.Projectiles.Summon.Whips.Dominatrix;
using tsorcRevamp.Projectiles.Summon.Whips.EnchantedWhip;
using tsorcRevamp.Projectiles.Summon.Whips.PolarisLeash;
using tsorcRevamp.Projectiles.VFX;
using tsorcRevamp.Utilities;
using tsorcRevamp;

namespace tsorcRevamp.NPCs
{
    // BFS pathfinding / waypoint navigation helpers for tsorcRevampGlobalNPC (split out of GlobalNPC.cs).
    public partial class tsorcRevampGlobalNPC
    {
        /// <summary>
        /// BFS from the NPC's current floor tile to the player's floor tile.
        /// Navigates walks, ledge-falls, jumps, and platform drops.
        /// Returns the world-space centre of the first waypoint tile on the path.
        /// No heap allocation — uses static buffers.
        /// </summary>
        internal static bool BfsFindWaypoint(NPC npc, float maxJumpPower, float maxJumpBoost,
                                              out Vector2 waypoint)
        {
            return BfsFindWaypoint(npc, maxJumpPower, maxJumpBoost, out waypoint, out _);
        }

        internal static bool BfsFindWaypoint(NPC npc, float maxJumpPower, float maxJumpBoost,
                                              out Vector2 waypoint, out NavActionType action)
        {
            waypoint = Vector2.Zero;
            action = NavActionType.None;
            Span<Vector2> routeTargets = stackalloc Vector2[MaxNavRouteSteps];
            Span<NavActionType> routeActions = stackalloc NavActionType[MaxNavRouteSteps];
            if (BfsFindRoute(npc, maxJumpPower, maxJumpBoost, routeTargets, routeActions, out int routeCount) && routeCount > 0)
            {
                waypoint = routeTargets[0];
                action = routeActions[0];
                return true;
            }

            return false;
        }

        internal static bool BfsFindRoute(NPC npc, float maxJumpPower, float maxJumpBoost,
                                          Span<Vector2> routeTargets, Span<NavActionType> routeActions, out int routeCount)
        {
            routeCount = 0;
            int sx = (int)(npc.Center.X / 16f);
            int sy = BfsFindStandableFloorY(npc, sx, (int)((npc.position.Y + npc.height + 4f) / 16f), 8, 12);
            int tx = (int)(Main.player[npc.target].Center.X / 16f);
            int ty = BfsFindTargetFloorY(npc, ref tx, (int)((Main.player[npc.target].position.Y
                            + Main.player[npc.target].height + 4f) / 16f));

            if (sy < 0 || ty < 0)
            {
                return false;
            }

            if (Math.Abs(sx - tx) <= 1 && Math.Abs(sy - ty) <= 1) return false;

            // Max upward tile reach: v²/(2g·tileSize), g≈0.4 px/frame², tileSize=16
            int jumpH = Math.Max(2, (int)(maxJumpPower * maxJumpPower / 12.8f));
            // Conservative horizontal reach during a jump arc
            int jumpW = Math.Max(1, Math.Min((int)maxJumpBoost, 5));

            Array.Clear(_bfsVis, 0, _bfsVis.Length);
            int head = 0, tail = 0;

            // Enqueue start node (anchor = itself = no step taken yet)
            _bfsVis[BFS_R, BFS_R] = true;
            _bfsAnX[BFS_R, BFS_R] = (short)sx;
            _bfsAnY[BFS_R, BFS_R] = (short)sy;
            _bfsAnAction[BFS_R, BFS_R] = NavActionType.None;
            _bfsEdgeAction[BFS_R, BFS_R] = NavActionType.None;
            _bfsParentX[BFS_R, BFS_R] = (short)sx;
            _bfsParentY[BFS_R, BFS_R] = (short)sy;
            _bfsQX[0] = sx; _bfsQY[0] = sy;
            tail = 1;

            int startScore = BfsTileScore(sx, sy, tx, ty);
            int bestScore = startScore;
            int bestX = sx;
            int bestY = sy;

            while (head != tail)
            {
                int cx = _bfsQX[head], cy = _bfsQY[head];
                head = (head + 1) & BFS_QMSK;

                int aox = cx - sx + BFS_R, aoy = cy - sy + BFS_R;
                short anX = _bfsAnX[aox, aoy], anY = _bfsAnY[aox, aoy];
                NavActionType anAction = _bfsAnAction[aox, aoy];
                bool isStart = cx == sx && cy == sy;

                if (!isStart)
                {
                    int score = BfsTileScore(cx, cy, tx, ty);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestX = cx;
                        bestY = cy;
                    }
                }

                if (Math.Abs(cx - tx) <= 2 && Math.Abs(cy - ty) <= 1)
                {
                    return BfsBuildCompressedRoute(npc, sx, sy, cx, cy, routeTargets, routeActions, out routeCount);
                }

                // ── Walk left / right ─────────────────────────────────────────────
                for (int d = -1; d <= 1; d += 2)
                {
                    int nx = cx + d;

                    short ax = isStart ? (short)nx : anX;
                    short ay = isStart ? (short)cy : anY;
                    NavActionType nextAction = isStart ? NavActionType.Walk : anAction;

                    if (BfsCanStand(npc, nx, cy))
                    {
                        if (isStart)
                        {
                            BfsFindWalkCommitment(npc, sx, sy, d, tx, ty, out ax, out ay);
                        }
                        BfsEnqueue(ref tail, nx, cy, ax, ay, sx, sy, cx, cy, nextAction, NavActionType.Walk);
                    }
                    else if (BfsCanStand(npc, nx, cy - 1))
                    {
                        // One-tile rises should be treated as ordinary terrain, not a full jump.
                        BfsEnqueue(ref tail, nx, cy - 1, ax, (short)(isStart ? cy - 1 : ay), sx, sy, cx, cy, nextAction, NavActionType.Walk);
                    }
                    else if (BfsCanStand(npc, nx, cy + 1))
                    {
                        // One-tile drops/slopes are walkable. Do not convert them into jump waypoints.
                        BfsEnqueue(ref tail, nx, cy + 1, ax, (short)(isStart ? cy + 1 : ay), sx, sy, cx, cy, nextAction, NavActionType.Walk);
                    }
                    else if (BfsCanStand(npc, nx, cy - 2))
                    {
                        NavActionType stepJumpAction = isStart ? NavActionType.JumpTo : anAction;
                        BfsEnqueue(ref tail, nx, cy - 2, ax, (short)(isStart ? cy - 2 : ay), sx, sy, cx, cy, stepJumpAction, NavActionType.JumpTo);
                    }
                    else
                    {
                        // Gap — fall to next landing (max 20 tiles)
                        for (int fy = cy + 2; fy <= cy + 20; fy++)
                        {
                            if (UsefulFunctions.IsTileReallySolid(nx, fy)) break;
                            if (BfsCanStand(npc, nx, fy))
                            {
                                NavActionType dropAction = isStart ? NavActionType.Drop : anAction;
                                BfsEnqueue(ref tail, nx, fy, ax, ay, sx, sy, cx, cy, dropAction, NavActionType.Drop);
                                break;
                            }
                        }
                    }
                }

                // ── Jump up to higher floors ──────────────────────────────────────
                for (int dh = 1; dh <= jumpH; dh++)
                {
                    int ry = cy - dh;
                    // Try landing at this level BEFORE checking whether the path is blocked —
                    // IsTileReallySolid(cx, ry) would be true for the target floor tile itself,
                    // which previously caused the loop to break before ever checking for a landing.
                    for (int jdx = -jumpW; jdx <= jumpW; jdx++)
                    {
                        int jx = cx + jdx;
                        if (BfsCanStand(npc, jx, ry))
                        {
                            short ax = isStart ? (short)jx : anX;
                            short ay = isStart ? (short)ry  : anY;
                            NavActionType jumpAction = isStart ? NavActionType.JumpTo : anAction;
                            BfsEnqueue(ref tail, jx, ry, ax, ay, sx, sy, cx, cy, jumpAction, NavActionType.JumpTo);
                        }
                    }
                    // Stop ascending if the vertical path is blocked (solid ceiling above).
                    if (UsefulFunctions.IsTileReallySolid(cx, ry)) break;
                }

                // ── Drop through platform ─────────────────────────────────────────
                Tile floorTile = Framing.GetTileSafely(cx, cy);
                if (floorTile.HasTile && !floorTile.IsActuated && TileID.Sets.Platforms[floorTile.TileType])
                {
                    for (int fy = cy + 1; fy <= cy + 20; fy++)
                    {
                        if (UsefulFunctions.IsTileReallySolid(cx, fy)) break;
                        if (BfsCanStand(npc, cx, fy))
                        {
                            short ax = isStart ? (short)cx : anX;
                            short ay = isStart ? (short)fy  : anY;
                            NavActionType platformDropAction = isStart ? NavActionType.DropThroughPlatform : anAction;
                            BfsEnqueue(ref tail, cx, fy, ax, ay, sx, sy, cx, cy, platformDropAction, NavActionType.DropThroughPlatform);
                            break;
                        }
                    }
                }
            }

            // If the exact player floor was not representable in the simplified graph
            // (common around stairs, half-blocks, platforms, furniture, or cramped houses),
            // still return the first step toward the best reachable node. This keeps bored
            // tiered enemies from doing nothing just because the final tile failed BFS.
            if ((bestX != sx || bestY != sy) && bestScore <= startScore - 4)
            {
                return BfsBuildCompressedRoute(npc, sx, sy, bestX, bestY, routeTargets, routeActions, out routeCount);
            }

            return false;
        }

        private static bool BfsBuildCompressedRoute(NPC npc, int sx, int sy, int endX, int endY,
                                                    Span<Vector2> routeTargets, Span<NavActionType> routeActions, out int routeCount)
        {
            routeCount = 0;
            int pathCount = 0;
            int cx = endX;
            int cy = endY;

            while ((cx != sx || cy != sy) && pathCount < BFS_PATH_CAP)
            {
                int ox = cx - sx + BFS_R;
                int oy = cy - sy + BFS_R;
                if ((uint)ox >= BFS_DIM || (uint)oy >= BFS_DIM)
                {
                    break;
                }

                _bfsPathX[pathCount] = (short)cx;
                _bfsPathY[pathCount] = (short)cy;
                _bfsPathAction[pathCount] = _bfsEdgeAction[ox, oy] == NavActionType.None ? NavActionType.Walk : _bfsEdgeAction[ox, oy];

                int px = _bfsParentX[ox, oy];
                int py = _bfsParentY[ox, oy];
                if (px == cx && py == cy)
                {
                    break;
                }

                cx = px;
                cy = py;
                pathCount++;
            }

            if (pathCount == 0)
            {
                return false;
            }

            int maxSteps = Math.Min(routeTargets.Length, MaxNavRouteSteps);
            NavActionType currentAction = NavActionType.None;
            int lastAddedX = sx;
            int lastAddedY = sy;

            for (int i = pathCount - 1; i >= 0 && routeCount < maxSteps; i--)
            {
                int x = _bfsPathX[i];
                int y = _bfsPathY[i];
                NavActionType rawAction = _bfsPathAction[i] == NavActionType.None ? NavActionType.Walk : _bfsPathAction[i];
                NavActionType action = BfsNormalizeRouteAction(lastAddedX, lastAddedY, x, y, rawAction);
                bool isFinal = i == 0;
                bool actionChanged = routeCount == 0 || action != currentAction;
                bool longWalkCommit = action == NavActionType.Walk && Math.Abs(x - lastAddedX) >= 6;
                bool verticalChange = Math.Abs(y - lastAddedY) >= 2;
                bool tinyOpeningWalk = routeCount == 0
                    && action == NavActionType.Walk
                    && !isFinal
                    && Math.Abs(x - sx) <= 1
                    && Math.Abs(y - sy) <= 1;

                if (!tinyOpeningWalk && (actionChanged || longWalkCommit || verticalChange || isFinal))
                {
                    routeTargets[routeCount] = BfsFloorToNpcCenter(npc, x, y);
                    routeActions[routeCount] = action;
                    routeCount++;
                    currentAction = action;
                    lastAddedX = x;
                    lastAddedY = y;
                }
            }

            return routeCount > 0;
        }

        private static NavActionType BfsNormalizeRouteAction(int fromX, int fromY, int toX, int toY, NavActionType rawAction)
        {
            int dx = Math.Abs(toX - fromX);
            int dy = toY - fromY; // positive means the target floor is lower.

            if (rawAction == NavActionType.DropThroughPlatform)
            {
                return NavActionType.DropThroughPlatform;
            }

            if (dy > 1)
            {
                return NavActionType.Drop;
            }

            if (dy == 1)
            {
                return NavActionType.Walk;
            }

            if (dy == 0)
            {
                return rawAction == NavActionType.JumpTo && dx > 4 ? NavActionType.JumpTo : NavActionType.Walk;
            }

            if (dy == -1)
            {
                return NavActionType.Walk;
            }

            return NavActionType.JumpTo;
        }

        private static int BfsTileScore(int x, int y, int targetX, int targetY)
        {
            return Math.Abs(x - targetX) * 2 + Math.Abs(y - targetY) * 3;
        }

        private static Vector2 BfsFloorToNpcCenter(NPC npc, int floorX, int floorY)
        {
            return new Vector2(floorX * 16f + 8f, floorY * 16f - npc.height / 2f);
        }

        private static void BfsFindWalkCommitment(NPC npc, int startX, int startY, int direction, int targetX, int targetY, out short anchorX, out short anchorY)
        {
            anchorX = (short)(startX + direction);
            anchorY = (short)startY;

            int bestX = anchorX;
            int bestY = anchorY;
            int bestScore = BfsTileScore(bestX, bestY, targetX, targetY);
            int previousY = startY;

            for (int step = 1; step <= 18; step++)
            {
                int x = startX + step * direction;
                int y = previousY;

                if (!BfsCanStand(npc, x, y))
                {
                    int adjustedY = -1;
                    for (int dy = -2; dy <= 3; dy++)
                    {
                        if (BfsCanStand(npc, x, previousY + dy))
                        {
                            adjustedY = previousY + dy;
                            break;
                        }
                    }

                    if (adjustedY < 0)
                    {
                        break;
                    }

                    y = adjustedY;
                }

                if (UsefulFunctions.IsTileReallySolid(x, y - 1) || UsefulFunctions.IsTileReallySolid(x, y - 2))
                {
                    break;
                }

                int score = BfsTileScore(x, y, targetX, targetY);
                if (score <= bestScore)
                {
                    bestScore = score;
                    bestX = x;
                    bestY = y;
                }

                previousY = y;

                if (step >= 6 && (Math.Abs(x - targetX) <= 2 || score > bestScore + 10))
                {
                    break;
                }
            }

            anchorX = (short)bestX;
            anchorY = (short)bestY;
        }

        private static int BfsFindStandableFloorY(NPC npc, int x, int approximateFloorY, int scanUp, int scanDown)
        {
            int bestY = -1;
            int bestDistance = int.MaxValue;
            for (int dy = -scanUp; dy <= scanDown; dy++)
            {
                int floorY = approximateFloorY + dy;
                if (!BfsCanStand(npc, x, floorY))
                {
                    continue;
                }

                int distance = Math.Abs(dy);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestY = floorY;
                }
            }
            return bestY;
        }

        private static int BfsFindTargetFloorY(NPC npc, ref int targetX, int approximateFloorY)
        {
            int bestX = targetX;
            int bestY = -1;
            int bestScore = int.MaxValue;

            for (int dx = -2; dx <= 2; dx++)
            {
                int x = targetX + dx;
                int y = BfsFindStandableFloorY(npc, x, approximateFloorY, 8, 16);
                if (y < 0)
                {
                    continue;
                }

                int score = Math.Abs(dx) * 4 + Math.Abs(y - approximateFloorY);
                if (score < bestScore)
                {
                    bestScore = score;
                    bestX = x;
                    bestY = y;
                }
            }

            if (bestY >= 0)
            {
                targetX = bestX;
                return bestY;
            }

            return BfsFindStandableFloorY(npc, targetX, approximateFloorY, 8, 20);
        }

        /// <summary>True if the NPC (2 tiles tall) can stand at (x, floorY).</summary>
        internal static bool BfsCanStand(NPC npc, int x, int floorY)
        {
            int widthTiles = Math.Max(1, (int)Math.Ceiling(npc.width / 16f));
            int leftX = x - (widthTiles - 1) / 2;
            int rightX = leftX + widthTiles - 1;

            bool hasFloor = false;
            for (int tx = leftX; tx <= rightX; tx++)
            {
                if (UsefulFunctions.IsTileReallySolid(tx, floorY))
                {
                    hasFloor = true;
                    break;
                }

                Tile t = Framing.GetTileSafely(tx, floorY);
                if (t.HasTile && !t.IsActuated && TileID.Sets.Platforms[t.TileType])
                {
                    hasFloor = true;
                    break;
                }
            }

            if (!hasFloor)
            {
                return false;
            }

            int bodyTiles = Math.Max(2, (int)Math.Ceiling(npc.height / 16f));
            for (int tx = leftX; tx <= rightX; tx++)
            {
                for (int y = floorY - bodyTiles; y < floorY; y++)
                {
                    if (UsefulFunctions.IsTileReallySolid(tx, y))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>Add (x,y) to the BFS queue if in-bounds and not yet visited.</summary>
        internal static void BfsEnqueue(ref int tail, int x, int y,
                                         short ax, short ay, int sx, int sy, int parentX, int parentY,
                                         NavActionType action, NavActionType edgeAction)
        {
            int ox = x - sx + BFS_R, oy = y - sy + BFS_R;
            if ((uint)ox >= BFS_DIM || (uint)oy >= BFS_DIM) return;
            if (_bfsVis[ox, oy]) return;
            _bfsVis[ox, oy] = true;
            _bfsAnX[ox, oy] = ax; _bfsAnY[ox, oy] = ay;
            _bfsAnAction[ox, oy] = action;
            _bfsEdgeAction[ox, oy] = edgeAction;
            _bfsParentX[ox, oy] = (short)parentX;
            _bfsParentY[ox, oy] = (short)parentY;
            _bfsQX[tail] = x; _bfsQY[tail] = y;
            tail = (tail + 1) & BFS_QMSK;
        }
    }
}
