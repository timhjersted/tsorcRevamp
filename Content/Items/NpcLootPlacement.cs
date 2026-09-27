using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items
{
    public class NpcLootPlacement : GlobalItem
    {
        private const int SearchRadiusTiles = 32;

        public override void OnSpawn(Item item, IEntitySource source)
        {
            // OnSpawn runs before both normal item broadcasts and instanced boss-bag packets.
            // Move the server's item here so every recipient gets the corrected position.
            if (Main.netMode == NetmodeID.MultiplayerClient
                || source is not EntitySource_Loot loot || loot.Entity is not NPC npc
                || !Collision.SolidCollision(item.position, item.width, item.height))
            {
                return;
            }

            Player nearestPlayer = null;
            float nearestDistanceSquared = float.MaxValue;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead || player.ghost)
                {
                    continue;
                }

                float distanceSquared = Vector2.DistanceSquared(npc.Center, player.Center);
                if (distanceSquared < nearestDistanceSquared)
                {
                    nearestPlayer = player;
                    nearestDistanceSquared = distanceSquared;
                }
            }

            // If everyone died with the NPC, still try to free its loot near the death position.
            Vector2 searchCenter = nearestPlayer?.Center ?? npc.Center;
            if (TryFindOpenPosition(item, searchCenter, out Vector2 position))
            {
                item.position = position;
                item.velocity = Vector2.Zero;
                item.wet = Collision.WetCollision(position, item.width, item.height);
            }
        }

        private static bool TryFindOpenPosition(Item item, Vector2 center, out Vector2 position)
        {
            position = item.position;
            int centerTileX = (int)(center.X / 16f);
            int centerTileY = (int)(center.Y / 16f);
            int minX = Math.Max(1, centerTileX - SearchRadiusTiles);
            int maxX = Math.Min(Main.maxTilesX - 2, centerTileX + SearchRadiusTiles);
            int minY = Math.Max(1, centerTileY - SearchRadiusTiles);
            int maxY = Math.Min(Main.maxTilesY - 2, centerTileY + SearchRadiusTiles);
            float bestDistanceSquared = float.MaxValue;
            bool found = false;

            // Compare actual distances, rather than accepting the first tile in a square ring.
            // Test the entire item rectangle: an empty center tile alone cannot fit a large bag.
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2 candidateCenter = new Vector2(x * 16 + 8, y * 16 + 8);
                    float distanceSquared = Vector2.DistanceSquared(center, candidateCenter);
                    if (distanceSquared >= bestDistanceSquared)
                    {
                        continue;
                    }

                    Vector2 candidate = candidateCenter - item.Size / 2f;
                    if (candidate.X < 16 || candidate.Y < 16
                        || candidate.X + item.width > (Main.maxTilesX - 1) * 16
                        || candidate.Y + item.height > (Main.maxTilesY - 1) * 16
                        || Collision.SolidCollision(candidate, item.width, item.height))
                    {
                        continue;
                    }

                    position = candidate;
                    bestDistanceSquared = distanceSquared;
                    found = true;
                }
            }

            return found;
        }
    }
}
