using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs
{
    /// <summary>
    /// Opt-in terrain policy for Gigas and Ice Gigas. SmartFighter4 plans ordinary routes; this
    /// owns every navigation jump, its exact landing, route handoff, and bounded foot settling.
    /// Other enemies keep their existing policy unless they explicitly assign this controller.
    /// </summary>
    public sealed class GigasTerrainNavigation
    {
        int leapTicks;
        int cooldown;
        float leapVelocityX;
        float landingCenterX;
        int runupTicks;
        float runupTargetCenterX;
        Vector2 plannedLandingPosition;
        float runupLastCenterX;
        int runupStallTicks;
        int perchRecoveryCooldown;
        int settleTicks;
        int routeRevision;
        int flightFrames;
        int localRetryTicks;
        bool sawAirborne; //server-only observation; clients follow the synced settling transition
        internal bool IsTraversing => leapTicks > 0 || runupTicks > 0;

        public void Send(BinaryWriter writer)
        {
            writer.Write(leapTicks);
            writer.Write(cooldown);
            writer.Write(leapVelocityX);
            writer.Write(landingCenterX);
            writer.Write(runupTicks);
            writer.Write(runupTargetCenterX);
            writer.Write(plannedLandingPosition.X);
            writer.Write(plannedLandingPosition.Y);
            writer.Write(runupLastCenterX);
            writer.Write(runupStallTicks);
            writer.Write(perchRecoveryCooldown);
            writer.Write((byte)settleTicks);
            writer.Write(routeRevision);
            writer.Write(flightFrames);
            writer.Write((byte)localRetryTicks);
        }

        public void Receive(BinaryReader reader, NPC npc)
        {
            leapTicks = reader.ReadInt32();
            cooldown = reader.ReadInt32();
            leapVelocityX = reader.ReadSingle();
            landingCenterX = reader.ReadSingle();
            runupTicks = reader.ReadInt32();
            runupTargetCenterX = reader.ReadSingle();
            plannedLandingPosition = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            runupLastCenterX = reader.ReadSingle();
            runupStallTicks = reader.ReadInt32();
            perchRecoveryCooldown = reader.ReadInt32();
            settleTicks = reader.ReadByte();
            int revision = reader.ReadInt32();
            flightFrames = reader.ReadInt32();
            localRetryTicks = reader.ReadByte();
            if (revision != routeRevision) SmartFighter4AI.ResetStableHeavyRoute(npc);
            routeRevision = revision;
        }

        /// <summary>Returns true while the run-up or committed ballistic arc owns movement.</summary>
        public bool Update(NPC npc)
        {
            if (perchRecoveryCooldown > 0) perchRecoveryCooldown--;
            if (localRetryTicks > 0) localRetryTicks--;
            if (settleTicks > 0)
            {
                settleTicks--;
                npc.velocity.X = 0f;
                StepAndSettle(npc);
                return true;
            }
            if (cooldown > 0)
            {
                cooldown--;
            }

            if (runupTicks > 0)
            {
                runupTicks--;
                if (npc.velocity.Y != 0f)
                {
                    return true;
                }

                if (Math.Abs(npc.Center.X - runupLastCenterX) < 0.4f)
                {
                    runupStallTicks++;
                }
                else
                {
                    runupLastCenterX = npc.Center.X;
                    runupStallTicks = 0;
                }
                if (runupStallTicks >= 45 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Log(npc, "runup-blocked", $"target={runupTargetCenterX / 16f:F1}");
                    runupTicks = 0;
                    runupTargetCenterX = 0f;
                    cooldown = 120;
                    npc.netUpdate = true;
                    return false;
                }

                float dx = runupTargetCenterX - npc.Center.X;
                if (Math.Abs(dx) > 8f)
                {
                    int direction = Math.Sign(dx);
                    npc.velocity.X = direction * 1.5f;
                    npc.direction = direction;
                    npc.spriteDirection = direction;
                    StepAndSettle(npc);
                    return true;
                }

                if (Main.netMode == NetmodeID.MultiplayerClient) { npc.velocity.X = 0f; return true; }
                if (TryBuildArc(npc, npc.position, plannedLandingPosition, out float flightTime, out float launchVelocityY)
                    && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Launch(npc, flightTime, launchVelocityY, "runup-launch");
                    runupTicks = 0;
                    runupTargetCenterX = 0f;
                    runupStallTicks = 0;
                    return true;
                }

                Log(npc, "runup-abort", "launch tile no longer has a clear arc");
                runupTicks = 0;
                runupTargetCenterX = 0f;
                cooldown = 90;
                npc.netUpdate = true;
                return false;
            }

            if (leapTicks <= 0 && runupTargetCenterX != 0f)
            {
                if (Main.netMode == NetmodeID.MultiplayerClient) { runupTicks = 1; return true; }
                Log(npc, "runup-timeout", $"target={runupTargetCenterX / 16f:F1}");
                runupTargetCenterX = 0f;
                runupStallTicks = 0;
                cooldown = 120;
                npc.netUpdate = true;
                return false;
            }

            if (leapTicks <= 0)
            {
                // Ordinary falls/evasive hops also invalidate the old route on landing. This
                // decision is server-owned; the revision and settling timer reach every client.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    bool grounded = npc.velocity.Y >= 0f && npc.collideY && npc.oldVelocity.Y >= 0f;
                    if (sawAirborne && grounded)
                    {
                        sawAirborne = false;
                        FinishFlight(npc, failed: false);
                        return true;
                    }
                    if (!grounded && Math.Abs(npc.velocity.Y) > 1f) sawAirborne = true;
                }
                return false;
            }

            leapTicks--;
            flightFrames++;
            npc.velocity.X = leapVelocityX;
            int leapDirection = Math.Sign(leapVelocityX);
            npc.direction = leapDirection;
            npc.spriteDirection = leapDirection;

            if (flightFrames > 2 && npc.collideY && npc.velocity.Y >= 0f && npc.oldVelocity.Y > 0f)
            {
                // A client never ends an arc independently; it holds until the landing snapshot.
                if (Main.netMode == NetmodeID.MultiplayerClient) { npc.velocity.X = 0f; return true; }
                bool failed = Math.Abs(npc.Center.X - landingCenterX) > 24f
                    || Math.Abs(npc.position.Y - plannedLandingPosition.Y) > 16f
                    || !HasWalkingFooting(npc);
                Log(npc, failed ? "landing-missed" : "landed", $"x={npc.Center.X / 16f:F1} target={landingCenterX / 16f:F1} settle=12 route=reset");
                FinishFlight(npc, failed);
                return true;
            }
            if (leapTicks <= 0)
            {
                if (Main.netMode == NetmodeID.MultiplayerClient) { leapTicks = 1; return true; }
                Log(npc, "timeout", $"x={npc.Center.X / 16f:F1} target={landingCenterX / 16f:F1}");
                FinishFlight(npc, failed: true);
                return true;
            }
            return true;
        }

        void ResetRoute(NPC npc)
        {
            SmartFighter4AI.ResetStableHeavyRoute(npc);
            routeRevision++;
        }

        internal void CancelForCombat(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !IsTraversing) return;
            leapTicks = runupTicks = settleTicks = 0;
            runupTargetCenterX = 0f;
            sawAirborne = false;
            ResetRoute(npc);
        }

        void FinishFlight(NPC npc, bool failed)
        {
            if (failed) SmartFighter4AI.RememberStableHeavyFailure(npc, plannedLandingPosition);
            leapTicks = 0;
            runupTicks = 0;
            runupTargetCenterX = 0f;
            sawAirborne = false;
            npc.velocity.X = 0f;
            if (npc.collideY && npc.velocity.Y > 0f) npc.velocity.Y = 0f;
            settleTicks = 12;
            cooldown = 90;
            perchRecoveryCooldown = Math.Max(perchRecoveryCooldown, 90);
            ResetRoute(npc);
            npc.GetGlobalNPC<tsorcRevampGlobalNPC>().RequestNetworkSnapshot();
        }

        /// <summary>
        /// Starts a direct leap when possible. If the wall is too close for a safe launch, searches
        /// up to ten flat tiles behind the giant, walks back there, then revalidates before launch.
        /// </summary>
        public bool TryBegin(NPC npc, Player player)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || cooldown > 0 || leapTicks > 0 || runupTicks > 0
                || npc.velocity.Y != 0f || !player.active || player.dead)
            {
                return false;
            }

            float playerDx = player.Center.X - npc.Center.X;
            if (Math.Abs(playerDx) < 240f || Math.Abs(playerDx) > 1120f)
            {
                return false;
            }
            bool blocked = npc.collideX || !Collision.CanHitLine(npc.position, npc.width, npc.height, player.position, player.width, player.height);
            if (!blocked)
            {
                return false;
            }

            int direction = Math.Sign(playerDx);
            if (!TryFindLanding(npc, player, direction, out Vector2 landing))
            {
                cooldown = 45;
                Log(npc, "rejected", "blocked but no flat landing near player");
                return false;
            }

            if (TryBuildArc(npc, npc.position, landing, out float directTime, out float directVelocityY))
            {
                plannedLandingPosition = landing;
                Launch(npc, directTime, directVelocityY, "launch");
                return true;
            }

            if (TryFindRunup(npc, landing, direction, out float runupCenterX))
            {
                ResetRoute(npc);
                runupTargetCenterX = runupCenterX;
                plannedLandingPosition = landing;
                runupTicks = 150;
                npc.velocity.X = 0f;
                runupLastCenterX = npc.Center.X;
                runupStallTicks = 0;
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().RequestNetworkSnapshot();
                Log(npc, "runup", $"target={runupCenterX / 16f:F1} landing={(landing.X + npc.width * 0.5f) / 16f:F1}");
                return true;
            }

            cooldown = 45;
            Log(npc, "rejected", "blocked: direct arc and runup tiles are unsafe");
            return false;
        }

        /// <summary>
        /// Lets the two-tile support core bridge one ordinary stair-step (the lower foot may sink
        /// into its tile), but performs one validated hop when the core is genuinely stranded. The
        /// former direction-only hop retried from every bad landing and caused the rapid slope bounce.
        /// </summary>
        public bool RecoverFromNarrowPerch(NPC npc, tsorcRevampGlobalNPC globalNPC, Player player)
        {
            // Launching the beast is a decision that moves the body, and this controller's timers are per-machine, so
            // each client was free to hop on its own schedule. Server decides; the launch snapshot carries the arc.
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return false;
            }

            if (perchRecoveryCooldown > 0)
            {
                return false;
            }
            if (npc.velocity.Y != 0f)
            {
                return false;
            }
            int supportX = (int)(npc.Center.X / 16f);
            int supportY = (int)((npc.Bottom.Y + 1f) / 16f);
            if (HasWalkingFooting(npc))
            {
                return false;
            }

            int preferredDirection = player.active && !player.dead ? Math.Sign(player.Center.X - npc.Center.X) : npc.direction;
            if (preferredDirection == 0)
            {
                preferredDirection = npc.direction == 0 ? 1 : npc.direction;
            }
            if (!TryFindRecoveryLanding(npc, supportX, supportY, preferredDirection, out Vector2 landing)
                || !TryBuildRecoveryArc(npc, npc.position, landing, out float flightTime, out float launchVelocityY))
            {
                // Do not spam an unplanned jump against terrain. SF4 can keep walking/replan, and
                // this short cooldown prevents this exceptional recovery from fighting it every tick.
                perchRecoveryCooldown = 45;
                Log(npc, "perch-rejected", "no reachable two-tile landing");
                return false;
            }

            plannedLandingPosition = landing;
            Launch(npc, flightTime, launchVelocityY, "perch-launch");
            perchRecoveryCooldown = 150;
            npc.netUpdate = true;
            return true;
        }

        static bool TryFindRecoveryLanding(NPC npc, int supportX, int supportY, int preferredDirection, out Vector2 landingPosition)
        {
            landingPosition = Vector2.Zero;
            for (int width = 4; width >= 2; width -= 2)
            for (int pass = 0; pass < 2; pass++)
            {
                int direction = pass == 0 ? preferredDirection : -preferredDirection;
                for (int distance = 3; distance <= 10; distance++)
                {
                    int tileX = supportX + direction * distance;
                    float groundY = FindGroundY(new Vector2(tileX * 16f + 8f, npc.Bottom.Y - 2f * 16f), 6);
                    if (groundY <= 0f)
                    {
                        continue;
                    }
                    int groundTileY = (int)(groundY / 16f);
                    if (Math.Abs(groundTileY - supportY) > 3
                        || !UsefulFunctions.IsFootprintSupported(tileX, groundTileY, width)
                        || SmartFighter4AI.IsStableHeavyLandingFailed(npc, tileX, groundTileY - 1))
                    {
                        continue;
                    }
                    Vector2 candidate = new Vector2(tileX * 16f + 8f - npc.width * 0.5f, groundY - npc.height);
                    if (!Collision.SolidCollision(candidate, npc.width, npc.height)
                        && TryBuildRecoveryArc(npc, npc.position, candidate, out _, out _))
                    {
                        landingPosition = candidate;
                        return true;
                    }
                }
            }
            return false;
        }

        static bool TryFindLanding(NPC npc, Player player, int direction, out Vector2 landingPosition)
        {
            landingPosition = Vector2.Zero;
            int playerTileX = (int)(player.Center.X / 16f);
            float searchStartY = player.Bottom.Y - 20f * 16f;
            for (int width = 4; width >= 2; width -= 2)
            for (int offset = 0; offset <= 16; offset++)
            {
                int signedOffset = offset == 0 ? 0 : -direction * offset;
                int landingTileX = playerTileX + signedOffset;
                float groundY = FindGroundY(new Vector2(landingTileX * 16f + 8f, searchStartY), 48);
                if (groundY <= 0f || !UsefulFunctions.IsFootprintSupported(landingTileX, (int)(groundY / 16f), width))
                {
                    continue;
                }
                Vector2 candidate = new Vector2(landingTileX * 16f + 8f - npc.width * 0.5f, groundY - npc.height);
                if (SmartFighter4AI.IsStableHeavyLandingFailed(npc, landingTileX, (int)(groundY / 16f) - 1)) continue;
                float dx = candidate.X - npc.position.X;
                if (Math.Sign(dx) != direction || Math.Abs(dx) < 192f || Math.Abs(dx) > 1120f
                    || Collision.SolidCollision(candidate, npc.width, npc.height))
                {
                    continue;
                }
                landingPosition = candidate;
                return true;
            }
            return false;
        }

        static bool TryFindRunup(NPC npc, Vector2 landingPosition, int direction, out float runupCenterX)
        {
            runupCenterX = 0f;
            int originTileX = (int)(npc.Center.X / 16f);
            for (int tilesBack = 3; tilesBack <= 10; tilesBack++)
            {
                int tileX = originTileX - direction * tilesBack;
                float groundY = FindGroundY(new Vector2(tileX * 16f + 8f, npc.Bottom.Y - 4f * 16f), 12);
                if (groundY <= 0f || Math.Abs(groundY - npc.Bottom.Y) > 16f
                    || !UsefulFunctions.IsFootprintSupported(tileX, (int)(groundY / 16f), 2))
                {
                    continue;
                }
                Vector2 candidate = new Vector2(tileX * 16f + 8f - npc.width * 0.5f, groundY - npc.height);
                if (Collision.SolidCollision(candidate, npc.width, npc.height))
                {
                    continue;
                }
                if (TryBuildArc(npc, candidate, landingPosition, out _, out _))
                {
                    runupCenterX = candidate.X + npc.width * 0.5f;
                    return true;
                }
            }
            return false;
        }

        internal static bool HasWalkingFooting(NPC npc)
            => UsefulFunctions.IsFootprintSupported((int)(npc.Center.X / 16f), (int)((npc.Bottom.Y + 1f) / 16f), 2, 1);

        internal static bool IsFlatLanding(NPC npc, int centerX, int floorY, int width = 2)
        {
            if (!UsefulFunctions.IsFootprintSupported(centerX, floorY, width)) return false;
            Vector2 position = new Vector2(centerX * 16f + 8f - npc.width * 0.5f, floorY * 16f - npc.height);
            return !Collision.SolidCollision(position, npc.width, npc.height);
        }

        internal static void StepAndSettle(NPC npc)
        {
            if (npc.noGravity || npc.noTileCollide || npc.velocity.Y < 0f) return;
            Vector2 previousPosition = npc.position;
            float previousOffset = npc.gfxOffY;
            float previousStepSpeed = npc.stepSpeed;
            UsefulFunctions.AutoStepUp(npc);
            if (npc.position != previousPosition)
            {
                // Vanilla's helper checks only three headroom rows. These giants need their
                // entire body clear, and must retain the same one-step support core as A*.
                Vector2 next = npc.position + new Vector2(npc.velocity.X, 0f);
                if (!UsefulFunctions.IsFootprintSupported((int)((next.X + npc.width * 0.5f) / 16f),
                        (int)((next.Y + npc.height + 1f) / 16f), 2, 1)
                    || Collision.SolidCollision(next, npc.width, npc.height))
                {
                    npc.position = previousPosition;
                    npc.gfxOffY = previousOffset;
                    npc.stepSpeed = previousStepSpeed;
                }
            }
            if (npc.velocity.Y != 0f || !HasWalkingFooting(npc))
            {
                return;
            }

            // Sink the sprite 16px only while the two-tile core straddles a real 1-tile step-down: one column flush
            // (HasWalkingFooting guarantees it) and the other EMPTY at the feet row. A slope or half-block there is
            // not a step, so it no longer counts. Collision stays on the high support; only the sprite moves.
            int centerX = (int)(npc.Center.X / 16f);
            int floorY = (int)((npc.Bottom.Y + 1f) / 16f);
            bool leftFlush = UsefulFunctions.IsValidWalkableTile(centerX - 1, floorY);
            bool rightFlush = UsefulFunctions.IsValidWalkableTile(centerX, floorY);
            bool stepDownOnLeft = !leftFlush && !WorldGen.SolidTile(centerX - 1, floorY);
            bool stepDownOnRight = !rightFlush && !WorldGen.SolidTile(centerX, floorY);
            bool straddlingStep = stepDownOnLeft || stepDownOnRight;

            if (straddlingStep)
            {
                float easedSink = MathHelper.Lerp(Math.Max(0f, npc.gfxOffY), 16f, 0.35f);
                npc.gfxOffY = Math.Min(16f, easedSink);
            }
            else if (leftFlush && rightFlush && npc.gfxOffY > 0f)
            {
                // Both core tiles flat: ease the sink back out. Vanilla only decays gfxOffY by stepSpeed, which
                // is 0 unless a step-up just ran, so without this a sink set once stayed for the rest of the fight.
                npc.gfxOffY = MathHelper.Lerp(npc.gfxOffY, 0f, 0.25f);

                if (npc.gfxOffY < 0.5f)
                {
                    npc.gfxOffY = 0f;
                }
            }
        }

        internal bool TryPlannedJump(NPC npc, int targetX, int targetFeetY, float maxJump, float maxSpeed)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || settleTicks > 0 || leapTicks > 0) return false;
            Vector2 landing = new Vector2(targetX * 16f + 8f - npc.width * 0.5f, (targetFeetY + 1) * 16f - npc.height);
            if (!TrySolveArc(npc, npc.position, landing, 3.5f, maxJump, maxSpeed, out int ticks, out Vector2 velocity))
            {
                SmartFighter4AI.RememberStableHeavyFailure(npc, landing);
                ResetRoute(npc);
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().RequestNetworkSnapshot();
                Log(npc, "plan-rejected", $"landing=({targetX},{targetFeetY}) full-body arc blocked");
                return false;
            }
            plannedLandingPosition = landing;
            Launch(npc, ticks, velocity.Y, "plan-launch");
            return true;
        }

        internal bool TryLocalJump(NPC npc, int direction, float maxJump, float maxSpeed)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || settleTicks > 0 || leapTicks > 0 || localRetryTicks > 0) return false;
            int startX = (int)(npc.Center.X / 16f);
            // Prefer a broad flat patch beyond the first bump, then a two-tile fallback.
            for (int width = 4; width >= 2; width -= 2)
                for (int distance = 4; distance <= 18; distance++)
                {
                    int x = startX + direction * distance;
                    float groundY = FindGroundY(new Vector2(x * 16f + 8f, npc.Bottom.Y - 8f * 16f), 16);
                    if (groundY < 0f || !IsFlatLanding(npc, x, (int)(groundY / 16f), width)
                        || SmartFighter4AI.IsStableHeavyLandingFailed(npc, x, (int)(groundY / 16f) - 1)) continue;
                    Vector2 landing = new Vector2(x * 16f + 8f - npc.width * 0.5f, groundY - npc.height);
                    if (!TrySolveArc(npc, npc.position, landing, 3.5f, maxJump, maxSpeed, out int ticks, out Vector2 velocity)) continue;
                    plannedLandingPosition = landing;
                    Launch(npc, ticks, velocity.Y, "local-launch");
                    return true;
                }
            localRetryTicks = 30; // Bounded retry when terrain offers no actual landing.
            return false;
        }

        static bool TryBuildRecoveryArc(NPC npc, Vector2 start, Vector2 landing, out float time, out float vy)
        {
            bool valid = TrySolveArc(npc, start, landing, 3.5f, 10f, 6f, out int ticks, out Vector2 velocity);
            time = ticks;
            vy = velocity.Y;
            return valid;
        }

        static bool TryBuildArc(NPC npc, Vector2 start, Vector2 landing, out float time, out float vy)
        {
            bool valid = TrySolveArc(npc, start, landing, 10f, 18f, 12f, out int ticks, out Vector2 velocity);
            time = ticks;
            vy = velocity.Y;
            return valid;
        }

        // The server commits one exact arc. Integrate gravity BEFORE movement, cap fall speed,
        // and check the full 52x110 body against terrain/platforms on every tick, including contact.
        internal static bool TrySolveArc(NPC npc, Vector2 start, Vector2 landing, float minJump,
            float maxJump, float maxSpeed, out int flightTicks, out Vector2 launch)
        {
            flightTicks = 0;
            launch = Vector2.Zero;
            int floorY = (int)Math.Round((landing.Y + npc.height) / 16f);
            int centerX = (int)((landing.X + npc.width * 0.5f) / 16f);
            if (!IsFlatLanding(npc, centerX, floorY)) return false;
            float gravity = npc.gravity > 0f ? npc.gravity : 0.3f;
            float maxFall = npc.maxFallSpeed > 0f ? npc.maxFallSpeed : 10f;
            for (float jump = minJump; jump <= maxJump + 0.001f; jump += 0.5f)
            {
                float y = start.Y, vy = -jump;
                int ticks = 0;
                for (int t = 1; t <= 240; t++)
                {
                    vy = Math.Min(vy + gravity, maxFall);
                    y += vy;
                    if (vy > 0f && y >= landing.Y - 0.01f) { ticks = t; break; }
                }
                if (ticks == 0) continue;
                float vx = (landing.X - start.X) / ticks;
                if (Math.Abs(vx) > maxSpeed) continue;
                Vector2 position = start;
                vy = -jump;
                bool clear = true;
                for (int t = 1; t <= ticks; t++)
                {
                    vy = Math.Min(vy + gravity, maxFall);
                    Vector2 step = new Vector2(vx, vy);
                    Vector2 allowed = Collision.TileCollision(position, step, npc.width, npc.height);
                    if (Math.Abs(allowed.X - vx) > 0.01f
                        || (t < ticks && Math.Abs(allowed.Y - vy) > 0.01f)) { clear = false; break; }
                    position += allowed;
                    if (Collision.SolidCollision(position, npc.width, npc.height)) { clear = false; break; }
                }
                if (!clear || Vector2.DistanceSquared(position, landing) > 2f * 2f) continue;
                flightTicks = ticks;
                launch = new Vector2(vx, -jump);
                return true;
            }
            return false;
        }


        void Launch(NPC npc, float flightTime, float launchVelocityY, string action)
        {
            ResetRoute(npc);
            flightFrames = 0;
            runupTicks = 0;
            runupTargetCenterX = 0f;
            runupStallTicks = 0;
            settleTicks = 0;
            sawAirborne = false;
            leapVelocityX = (plannedLandingPosition.X - npc.position.X) / flightTime;
            landingCenterX = plannedLandingPosition.X + npc.width * 0.5f;
            leapTicks = (int)Math.Ceiling(flightTime + 24f);
            npc.velocity.Y = launchVelocityY;
            npc.velocity.X = leapVelocityX;
            npc.direction = Math.Sign(leapVelocityX);
            npc.spriteDirection = npc.direction;
            // Every launch path ends here, so this is the one place the arc has to reach clients. A queued netUpdate
            // would wait out the throttle and the beast would already be mid-flight before anyone else saw it move.
            npc.GetGlobalNPC<tsorcRevampGlobalNPC>().RequestNetworkSnapshot();
            Log(npc, action, $"from=({npc.Center.X / 16f:F1},{npc.Bottom.Y / 16f:F1}) to=({landingCenterX / 16f:F1},{(plannedLandingPosition.Y + npc.height) / 16f:F1}) t={flightTime:F0} vel=({leapVelocityX:F2},{launchVelocityY:F2})");
        }

        static float FindGroundY(Vector2 worldPos, int maxTilesDown)
        {
            int tx = (int)(worldPos.X / 16f);
            int ty = (int)(worldPos.Y / 16f);
            if (tx < 5 || tx > Main.maxTilesX - 5)
            {
                return -1f;
            }
            for (int d = 0; d <= maxTilesDown; d++)
            {
                int y = ty + d;
                if (y < 5 || y >= Main.maxTilesY - 5)
                {
                    break;
                }
                Tile tile = Main.tile[tx, y];
                if (tile.HasTile && !tile.IsActuated && Main.tileSolid[tile.TileType])
                {
                    return y * 16f;
                }
            }
            return -1f;
        }

        static void Log(NPC npc, string action, string detail)
        {
            if (!ModContent.GetInstance<tsorcRevampConfig>().DebugMode || Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            try
            {
                string directory = Main.SavePath + Path.DirectorySeparatorChar + "Logs";
                Directory.CreateDirectory(directory);
                string line = $"[{DateTime.Now:HH:mm:ss}] {npc.TypeName}#{npc.whoAmI} [gigas-leap] {action} {detail}";
                File.AppendAllText(directory + Path.DirectorySeparatorChar + "tsorcRevamp-smartfighter4.log", line + Environment.NewLine);
            }
            catch { }
        }
    }
}
