using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Weapons
{
    /// <summary>
    /// Shared "spread a row of lingering flames along the hit surface, following terrain contour" logic — the
    /// impact half of EnemyFireFlask's behavior, extracted so other sticky-flame projectiles (e.g.
    /// GreatBlackKnightFlailEmber) can reuse the same floor/ceiling/wall surface-following scan instead of
    /// re-deriving it. Behavior is unchanged from the original EnemyFireFlask.SpawnSurfaceFlames.
    /// </summary>
    public static class SurfaceFlameHelper
    {
        // How far above / below the impact each column is allowed to search for its ground surface. The downward
        // bias lets ceiling hits "drip" to the floor and lets the spread fall into pits, while a small upward
        // allowance lets it climb gentle steps.
        private const int SurfaceSearchUp = 4;
        private const int SurfaceSearchDown = 16;

        /// <summary>Spawns lingeringFlameType instances (with the given damage) spread spreadTiles either side of
        /// impactCenter, following the surface the source projectile hit (floor/ceiling contour, or a wall face).</summary>
        public static void SpawnSurfaceFlames(Projectile source, Vector2 impactCenter, Vector2 normal, int spreadTiles, int lingeringFlameType, int damage)
        {
            // Hit a (mostly) vertical wall face → keep the flames clinging to that wall.
            if (System.Math.Abs(normal.X) > 0.5f)
            {
                SpawnWallFlames(source, impactCenter, normal, spreadTiles, lingeringFlameType, damage);
                return;
            }

            // Floor OR ceiling hit → spread along the GROUND contour beneath the impact. Each column resolves its
            // surface independently (scanning down for the first walkable tile), so the flames follow uneven
            // terrain — steps, slopes, gaps — instead of assuming a single flat row. Scanning downward also means a
            // ceiling hit naturally drips to the floor below it.
            int centerX = (int)(impactCenter.X / 16f);
            int impactTileY = (int)(impactCenter.Y / 16f);

            for (int offset = -spreadTiles; offset <= spreadTiles; offset++)
            {
                int x = centerX + offset;
                if (!TryFindGroundColumn(x, impactTileY, out int groundY))
                {
                    continue; // pit / gap in the terrain — leave it unlit
                }

                // Rest the flame on top of the surface tile, facing up.
                Vector2 center = new Vector2(x * 16 + 8, groundY * 16 + 8) - Vector2.UnitY * 16f;
                SpawnFlame(source, center, -Vector2.UnitY, lingeringFlameType, damage);
            }
        }

        private static void SpawnWallFlames(Projectile source, Vector2 impactCenter, Vector2 normal, int spreadTiles, int lingeringFlameType, int damage)
        {
            if (!TryFindSurfaceTile(impactCenter, normal, out Point baseTile))
            {
                return;
            }

            for (int offset = -spreadTiles; offset <= spreadTiles; offset++)
            {
                int x = baseTile.X;
                int y = baseTile.Y + offset;
                if (!IsSolidTile(x, y))
                {
                    continue;
                }

                int openX = x + (int)normal.X;
                if (IsSolidTile(openX, y))
                {
                    continue;
                }

                Vector2 center = new Vector2(x * 16 + 8, y * 16 + 8) + normal * 16f;
                SpawnFlame(source, center, normal, lingeringFlameType, damage);
            }
        }

        private static void SpawnFlame(Projectile source, Vector2 center, Vector2 normal, int lingeringFlameType, int damage)
        {
            int flame = Projectile.NewProjectile(
                source.GetSource_FromThis(),
                center,
                Vector2.Zero,
                lingeringFlameType,
                damage,
                1f,
                Main.myPlayer,
                normal.X,
                normal.Y);

            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.SyncProjectile, number: flame);
            }
        }

        /// <summary>
        /// Finds the ground surface tile in column <paramref name="x"/> nearest the impact row — the first solid
        /// tile (scanning top-down through the search window) that has open space directly above it. Returns false
        /// if the column has no surface within range (a pit).
        /// </summary>
        private static bool TryFindGroundColumn(int x, int impactTileY, out int groundY)
        {
            int top = impactTileY - SurfaceSearchUp;
            int bottom = impactTileY + SurfaceSearchDown;
            for (int y = top; y <= bottom; y++)
            {
                // A standable surface (full block OR platform) with no full block directly above it — so the flame
                // has room to sit on top.
                if (IsStandableSurface(x, y) && !IsSolidTile(x, y - 1))
                {
                    groundY = y;
                    return true;
                }
            }

            groundY = impactTileY;
            return false;
        }

        private static bool TryFindSurfaceTile(Vector2 center, Vector2 normal, out Point tile)
        {
            Point origin = new Point((int)(center.X / 16f), (int)(center.Y / 16f));
            float bestDistSq = float.MaxValue;
            tile = origin;

            for (int x = origin.X - 3; x <= origin.X + 3; x++)
            {
                for (int y = origin.Y - 3; y <= origin.Y + 3; y++)
                {
                    if (!IsSolidTile(x, y))
                    {
                        continue;
                    }

                    int openX = x + (int)normal.X;
                    int openY = y + (int)normal.Y;
                    if (IsSolidTile(openX, openY))
                    {
                        continue;
                    }

                    Vector2 tileCenter = new Vector2(x * 16 + 8, y * 16 + 8);
                    float distSq = Vector2.DistanceSquared(center, tileCenter);
                    if (distSq < bestDistSq)
                    {
                        bestDistSq = distSq;
                        tile = new Point(x, y);
                    }
                }
            }

            return bestDistSq < float.MaxValue;
        }

        private static bool IsSolidTile(int x, int y)
        {
            Tile tile = Framing.GetTileSafely(x, y);
            return tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];
        }

        // A platform / half-tile you can stand on top of (solid only from above).
        private static bool IsPlatform(int x, int y)
        {
            Tile tile = Framing.GetTileSafely(x, y);
            return tile.HasTile && Main.tileSolidTop[tile.TileType];
        }

        // Anything the fire can rest on top of: a full block or a platform.
        private static bool IsStandableSurface(int x, int y)
        {
            return IsSolidTile(x, y) || IsPlatform(x, y);
        }
    }
}
