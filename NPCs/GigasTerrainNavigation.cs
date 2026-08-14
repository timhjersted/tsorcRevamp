using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs
{
    /// <summary>
    /// Shared heavy-giant terrain movement for Gigas and Ice Gigas. SmartFighter4 owns ordinary
    /// routing; this owns only the exceptional, pre-validated building leap and the recovery from
    /// a visually-invalid one-tile perch.
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
        }

        public void Receive(BinaryReader reader)
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
        }

        /// <summary>Returns true while the run-up or committed ballistic arc owns movement.</summary>
        public bool Update(NPC npc)
        {
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
                if (runupStallTicks >= 45)
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
                    return true;
                }

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
                Log(npc, "runup-timeout", $"target={runupTargetCenterX / 16f:F1}");
                runupTargetCenterX = 0f;
                runupStallTicks = 0;
                cooldown = 120;
                npc.netUpdate = true;
                return false;
            }

            if (leapTicks <= 0)
            {
                return false;
            }

            leapTicks--;
            npc.velocity.X = leapVelocityX;
            int leapDirection = Math.Sign(leapVelocityX);
            npc.direction = leapDirection;
            npc.spriteDirection = leapDirection;

            if ((npc.collideY && npc.velocity.Y >= 0f) || (npc.velocity.Y == 0f && leapTicks < 100))
            {
                Log(npc, "landed", $"x={npc.Center.X / 16f:F1} target={landingCenterX / 16f:F1}");
                leapTicks = 0;
                cooldown = 90;
                npc.netUpdate = true;
                return false;
            }
            if (leapTicks <= 0)
            {
                Log(npc, "timeout", $"x={npc.Center.X / 16f:F1} target={landingCenterX / 16f:F1}");
                cooldown = 120;
                npc.netUpdate = true;
                return false;
            }
            return true;
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
                runupTargetCenterX = runupCenterX;
                plannedLandingPosition = landing;
                runupTicks = 150;
                npc.velocity.X = 0f;
                runupLastCenterX = npc.Center.X;
                runupStallTicks = 0;
                npc.netUpdate = true;
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
            if (perchRecoveryCooldown > 0)
            {
                perchRecoveryCooldown--;
                return false;
            }
            if (npc.velocity.Y != 0f)
            {
                return false;
            }
            int supportX = (int)(npc.Center.X / 16f);
            int supportY = (int)((npc.Bottom.Y + 4f) / 16f);
            if (UsefulFunctions.IsFootprintSupported(supportX, supportY, 2, 1))
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
                        || !UsefulFunctions.IsFootprintSupported(tileX, groundTileY, 2, 1))
                    {
                        continue;
                    }
                    Vector2 candidate = new Vector2(tileX * 16f + 8f - npc.width * 0.5f, groundY - npc.height);
                    if (!Collision.SolidCollision(candidate, npc.width, npc.height))
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
            for (int offset = 0; offset <= 16; offset++)
            {
                int signedOffset = offset == 0 ? 0 : -direction * offset;
                int landingTileX = playerTileX + signedOffset;
                float groundY = FindGroundY(new Vector2(landingTileX * 16f + 8f, searchStartY), 48);
                if (groundY <= 0f || !UsefulFunctions.IsFootprintSupported(landingTileX, (int)(groundY / 16f), 2))
                {
                    continue;
                }
                Vector2 candidate = new Vector2(landingTileX * 16f + 8f - npc.width * 0.5f, groundY - npc.height);
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

        static bool TryBuildRecoveryArc(NPC npc, Vector2 launchPosition, Vector2 landingPosition, out float flightTime, out float launchVelocityY)
        {
            flightTime = 0f;
            launchVelocityY = 0f;
            float dx = landingPosition.X - launchPosition.X;
            float time = MathHelper.Clamp(Math.Abs(dx) / 5.5f, 28f, 50f);
            float gravity = npc.gravity > 0f ? npc.gravity : 0.3f;
            float velocityY = (landingPosition.Y - launchPosition.Y - gravity * time * (time - 1f) * 0.5f) / time;
            float velocityX = dx / time;
            if (velocityY < -10f || velocityY > -2f || Math.Abs(velocityX) > 6f
                || !HasClearance(npc, launchPosition, landingPosition, velocityX, velocityY, gravity, (int)Math.Ceiling(time)))
            {
                return false;
            }
            flightTime = time;
            launchVelocityY = velocityY;
            return true;
        }

        static bool TryBuildArc(NPC npc, Vector2 launchPosition, Vector2 landingPosition, out float flightTime, out float launchVelocityY)
        {
            flightTime = 0f;
            launchVelocityY = 0f;
            float dx = landingPosition.X - launchPosition.X;
            float time = MathHelper.Clamp(Math.Abs(dx) / 10f, 82f, 110f);
            float gravity = npc.gravity > 0f ? npc.gravity : 0.3f;
            float velocityY = (landingPosition.Y - launchPosition.Y - gravity * time * (time - 1f) * 0.5f) / time;
            float velocityX = dx / time;
            if (velocityY < -18f || velocityY > -10f || Math.Abs(velocityX) > 12f
                || !HasClearance(npc, launchPosition, landingPosition, velocityX, velocityY, gravity, (int)Math.Ceiling(time)))
            {
                return false;
            }
            flightTime = time;
            launchVelocityY = velocityY;
            return true;
        }

        static bool HasClearance(NPC npc, Vector2 launchPosition, Vector2 landingPosition, float velocityX, float velocityY, float gravity, int flightTicks)
        {
            Vector2 simulatedPosition = launchPosition;
            Vector2 simulatedVelocity = new Vector2(velocityX, velocityY);
            for (int tick = 0; tick < flightTicks; tick++)
            {
                simulatedPosition += simulatedVelocity;
                simulatedVelocity.Y += gravity;
                if (tick < flightTicks - 1 && Collision.SolidCollision(simulatedPosition, npc.width, npc.height))
                {
                    return false;
                }
            }
            return Vector2.DistanceSquared(simulatedPosition, landingPosition) < 20f * 20f
                && !Collision.SolidCollision(landingPosition, npc.width, npc.height);
        }

        void Launch(NPC npc, float flightTime, float launchVelocityY, string action)
        {
            leapVelocityX = (plannedLandingPosition.X - npc.position.X) / flightTime;
            landingCenterX = plannedLandingPosition.X + npc.width * 0.5f;
            leapTicks = (int)Math.Ceiling(flightTime + 24f);
            npc.velocity.Y = launchVelocityY;
            npc.velocity.X = leapVelocityX;
            npc.direction = Math.Sign(leapVelocityX);
            npc.spriteDirection = npc.direction;
            npc.netUpdate = true;
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
                if (y >= Main.maxTilesY - 5)
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
