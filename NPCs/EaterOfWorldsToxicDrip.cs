using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles;

namespace tsorcRevamp.NPCs
{
    /// <summary>
    /// Eater of Worlds Toxic Drip. Every 8-12s a random exposed body piece oozes for one second,
    /// then releases one full tile of REAL water at the piece, which vanilla liquid physics drops.
    /// While the boss is near, Liquids/PoisonSwampWater treats all nearby water as toxic (green + Poisoned).
    ///
    /// The world is put back afterwards: before the first drip is released in an area, the server
    /// snapshots the liquid of every tile in the surrounding 3x3 block of 50x50-tile cells. When the
    /// fight ends (every segment gone, or every player dead) each snapshotted tile is restored and
    /// synced, so drip water vanishes and pre-existing lakes/pools are left exactly as they were.
    /// Known gap (accepted): water that falls or flows 50+ tiles out of every snapshotted cell, e.g. down
    /// a deep chasm, is not removed.
    /// Known gap: a dedicated server shut down mid-fight saves the drip water (SP restores on quit).
    /// </summary>
    public class EaterOfWorldsToxicDrip : ModSystem
    {
        // One drip per 8-12s for the whole boss (all split worms share it), each piece at most once per 10s.
        public const int MinDripIntervalTicks = 8 * 60;
        public const int MaxDripIntervalTicks = 12 * 60;
        public const int PieceCooldownTicks = 10 * 60;

        // Water is toxic for players within 150 tiles of any Eater segment.
        public const float ToxicWaterRange = 150 * 16;

        // Snapshot cells are grid-aligned so an area is only ever captured once, before any drip water reaches it.
        private const int CellSize = 50;

        private static int nextDripTimer = MinDripIntervalTicks;
        private static readonly Dictionary<Point, LiquidSnapshot> snapshots = new Dictionary<Point, LiquidSnapshot>();

        private class LiquidSnapshot
        {
            public readonly byte[] Amounts = new byte[CellSize * CellSize];
            public readonly byte[] Types = new byte[CellSize * CellSize];
        }

        private static bool IsEaterSegment(NPC npc)
        {
            bool isEaterType = npc.type == NPCID.EaterofWorldsHead || npc.type == NPCID.EaterofWorldsBody || npc.type == NPCID.EaterofWorldsTail;
            return npc.active && isEaterType;
        }

        /// <summary>True while an Eater of Worlds segment is close to this player. Read on every machine.</summary>
        public static bool IsToxicFightNear(Player player)
        {
            float rangeSquared = ToxicWaterRange * ToxicWaterRange;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];

                if (IsEaterSegment(npc) && Vector2.DistanceSquared(npc.Center, player.Center) < rangeSquared)
                {
                    return true;
                }
            }

            return false;
        }

        public override void PostUpdateNPCs()
        {
            // Server/singleplayer only: drips are projectiles (synced), and world restores are sent tile by tile.
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            bool anyEaterAlive = false;
            List<NPC> dripCandidates = new List<NPC>();

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];

                if (!IsEaterSegment(npc))
                {
                    continue;
                }

                anyEaterAlive = true;

                bool isBodyReady = npc.type == NPCID.EaterofWorldsBody && npc.GetGlobalNPC<EaterOfWorldsDripPiece>().CooldownTicks <= 0;
                if (isBodyReady && IsInOpenAir(npc))
                {
                    dripCandidates.Add(npc);
                }
            }

            bool anyPlayerAlive = false;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i].active && !Main.player[i].dead)
                {
                    anyPlayerAlive = true;
                    break;
                }
            }

            // Fight over: boss dead or despawned, or a wipe. Put the world back.
            if (!anyEaterAlive || !anyPlayerAlive)
            {
                RestoreSnapshots();
                nextDripTimer = MinDripIntervalTicks;
                return;
            }

            if (nextDripTimer > 0)
            {
                nextDripTimer--;
                return;
            }

            // Timer is up but every piece is buried or cooling down: hold at 0 and try again next tick.
            if (dripCandidates.Count == 0)
            {
                return;
            }

            NPC dripper = Main.rand.Next(dripCandidates);
            dripper.GetGlobalNPC<EaterOfWorldsDripPiece>().CooldownTicks = PieceCooldownTicks;
            nextDripTimer = Main.rand.Next(MinDripIntervalTicks, MaxDripIntervalTicks + 1);

            Projectile.NewProjectile(dripper.GetSource_FromAI(), dripper.Center, Vector2.Zero,
                ModContent.ProjectileType<EaterToxicOoze>(), 0, 0f, Main.myPlayer, dripper.whoAmI);
        }

        // A piece burrowing through dirt can't drip; it must be in air (not solid, not already underwater).
        private static bool IsInOpenAir(NPC npc)
        {
            Point tileCoords = npc.Center.ToTileCoordinates();

            if (!WorldGen.InWorld(tileCoords.X, tileCoords.Y, 10))
            {
                return false;
            }

            Tile tile = Main.tile[tileCoords.X, tileCoords.Y];
            bool isSolid = tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];
            return !isSolid && tile.LiquidAmount == 0;
        }

        /// <summary>
        /// Server/SP: adds one full tile of water at the release point, snapshotting the surrounding 3x3
        /// cells first. Skips lava/honey/shimmer so a drip never creates obsidian or other merge tiles.
        /// </summary>
        public static void PlaceDripWater(int tileX, int tileY)
        {
            if (!WorldGen.InWorld(tileX, tileY, 10))
            {
                return;
            }

            Tile tile = Main.tile[tileX, tileY];
            if (tile.LiquidAmount > 0 && tile.LiquidType != LiquidID.Water)
            {
                return;
            }

            int cellX = tileX / CellSize;
            int cellY = tileY / CellSize;

            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                for (int offsetY = -1; offsetY <= 1; offsetY++)
                {
                    TakeSnapshotIfMissing(new Point(cellX + offsetX, cellY + offsetY));
                }
            }

            WorldGen.PlaceLiquid(tileX, tileY, (byte)LiquidID.Water, 255);
        }

        private static void TakeSnapshotIfMissing(Point cell)
        {
            if (snapshots.ContainsKey(cell))
            {
                return;
            }

            LiquidSnapshot snapshot = new LiquidSnapshot();
            int originX = cell.X * CellSize;
            int originY = cell.Y * CellSize;

            for (int localX = 0; localX < CellSize; localX++)
            {
                for (int localY = 0; localY < CellSize; localY++)
                {
                    int tileX = originX + localX;
                    int tileY = originY + localY;

                    if (!WorldGen.InWorld(tileX, tileY))
                    {
                        continue;
                    }

                    Tile tile = Main.tile[tileX, tileY];
                    int index = localX * CellSize + localY;
                    snapshot.Amounts[index] = tile.LiquidAmount;
                    snapshot.Types[index] = (byte)tile.LiquidType;
                }
            }

            snapshots.Add(cell, snapshot);
        }

        // Writes back every tile whose liquid differs from its snapshot, then reframes and syncs only those tiles.
        private static void RestoreSnapshots()
        {
            if (snapshots.Count == 0)
            {
                return;
            }

            foreach (KeyValuePair<Point, LiquidSnapshot> entry in snapshots)
            {
                int originX = entry.Key.X * CellSize;
                int originY = entry.Key.Y * CellSize;
                LiquidSnapshot snapshot = entry.Value;

                for (int localX = 0; localX < CellSize; localX++)
                {
                    for (int localY = 0; localY < CellSize; localY++)
                    {
                        int tileX = originX + localX;
                        int tileY = originY + localY;

                        if (!WorldGen.InWorld(tileX, tileY))
                        {
                            continue;
                        }

                        Tile tile = Main.tile[tileX, tileY];
                        int index = localX * CellSize + localY;
                        byte savedAmount = snapshot.Amounts[index];
                        byte savedType = snapshot.Types[index];

                        bool unchanged = tile.LiquidAmount == savedAmount && (savedAmount == 0 || tile.LiquidType == savedType);
                        if (unchanged)
                        {
                            continue;
                        }

                        tile.LiquidAmount = savedAmount;
                        tile.LiquidType = savedType;
                        WorldGen.SquareTileFrame(tileX, tileY);

                        if (Main.netMode == NetmodeID.Server)
                        {
                            NetMessage.sendWater(tileX, tileY);
                        }
                    }
                }
            }

            snapshots.Clear();
        }

        // Singleplayer quit mid-fight: restore before the world is written to disk.
        public override void PreSaveAndQuit()
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                RestoreSnapshots();
            }
        }

        public override void OnWorldUnload()
        {
            snapshots.Clear();
            nextDripTimer = MinDripIntervalTicks;
        }
    }

    /// <summary>Per-body-piece drip cooldown. Server/SP only; clients never read it.</summary>
    public class EaterOfWorldsDripPiece : GlobalNPC
    {
        public int CooldownTicks;

        public override bool InstancePerEntity => true;

        public override bool AppliesToEntity(NPC entity, bool lateInstantiation)
        {
            return entity.type == NPCID.EaterofWorldsBody;
        }

        public override void PostAI(NPC npc)
        {
            if (CooldownTicks > 0)
            {
                CooldownTicks--;
            }
        }
    }

    /// <summary>
    /// The drip telegraph. ai[0] = body piece whoAmI. Rides the piece for OozeTicks while toxic goo
    /// gathers, then the server places one tile of real water at the piece and vanilla liquid physics
    /// drops it. Invisible and harmless itself: only its dust shows, and the water is the hazard.
    /// </summary>
    public class EaterToxicOoze : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        // 60-tick ooze telegraph before the water is released.
        private const int OozeTicks = 60;

        private const int AttachedTimerSlot = 0;

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = OozeTicks + 10;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
        }

        public override void AI()
        {
            float attachedTicks = Projectile.localAI[AttachedTimerSlot];
            int pieceIndex = (int)Projectile.ai[0];
            NPC piece = Main.npc[pieceIndex];
            bool pieceAlive = piece.active && piece.type == NPCID.EaterofWorldsBody;

            if (attachedTicks == 0 && !Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item85 with { Volume = 0.8f, Pitch = -0.4f }, Projectile.Center);
            }

            // Ooze telegraph: goo gathers on the piece, heavier as the release nears.
            if (attachedTicks < OozeTicks && pieceAlive)
            {
                Projectile.Center = piece.Center;
                Projectile.velocity = Vector2.Zero;
                Projectile.localAI[AttachedTimerSlot] = attachedTicks + 1f;

                if (!Main.dedServ)
                {
                    float progress = attachedTicks / OozeTicks;
                    int dustCount = 1 + (int)(progress * 3f);

                    for (int i = 0; i < dustCount; i++)
                    {
                        Vector2 spawnPosition = piece.Center + Main.rand.NextVector2Circular(piece.width * 0.5f, piece.height * 0.5f);
                        Dust dust = Dust.NewDustPerfect(spawnPosition, DustID.Poisoned, new Vector2(0f, Main.rand.NextFloat(0.2f, 1f)), 60, default, MathHelper.Lerp(0.9f, 1.6f, progress));
                        dust.noGravity = Main.rand.NextBool();
                    }
                }

                return;
            }

            // Release (telegraph done, or the piece died early: water comes out where it last was).
            // If the piece has burrowed into solid ground by now, PlaceLiquid refuses and nothing spills.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Point releaseTile = Projectile.Center.ToTileCoordinates();
                EaterOfWorldsToxicDrip.PlaceDripWater(releaseTile.X, releaseTile.Y);
            }

            Projectile.Kill();
        }

        // Gush on release: dust bursts downward with the water.
        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.Splash with { Volume = 0.7f, Pitch = -0.2f }, Projectile.Center);

            for (int i = 0; i < 30; i++)
            {
                Vector2 gushVelocity = new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(0.5f, 4f));
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Poisoned, gushVelocity, 40, default, Main.rand.NextFloat(1f, 1.6f));
                dust.noGravity = false;
            }
        }
    }
}
