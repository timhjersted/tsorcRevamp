using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Weapons
{
    public class EnemyFireFlask : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Weapons/FireFlask";

        private const float Gravity = 0.18f;
        private const int FireSpreadTiles = 5;

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 22;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 240;
            Projectile.penetrate = 1;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.knockBack = 3f;
            Projectile.light = 0.35f;
        }

        public override void AI()
        {
            Projectile.velocity.Y += Gravity;
            if (Projectile.velocity.Y > 16f)
            {
                Projectile.velocity.Y = 16f;
            }

            Projectile.rotation += Projectile.velocity.X * 0.08f;

            if (Main.rand.NextBool(3))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 120, default(Color), 0.8f);
                Main.dust[dust].velocity *= 0.25f;
                Main.dust[dust].noGravity = true;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Vector2 normal = Vector2.UnitY * -1f;
            if (Projectile.velocity.X != oldVelocity.X && System.Math.Abs(oldVelocity.X) > System.Math.Abs(oldVelocity.Y))
            {
                normal = oldVelocity.X > 0f ? -Vector2.UnitX : Vector2.UnitX;
            }
            else if (Projectile.velocity.Y != oldVelocity.Y)
            {
                normal = oldVelocity.Y > 0f ? -Vector2.UnitY : Vector2.UnitY;
            }

            Projectile.ai[0] = normal.X;
            Projectile.ai[1] = normal.Y;
            Projectile.Kill();
            return false;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 4 * 60);
            Projectile.ai[0] = 0f;
            Projectile.ai[1] = -1f;
            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Shatter with { Volume = 0.75f, PitchVariance = 0.2f }, Projectile.Center);

            for (int i = 0; i < 24; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(3f, 3f);
                Dust.NewDustPerfect(Projectile.Center, DustID.Smoke, velocity, 80, default(Color), Main.rand.NextFloat(0.8f, 1.2f));
            }

            for (int i = 0; i < 35; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(4f, 4f);
                Dust.NewDustPerfect(Projectile.Center, DustID.Torch, velocity, 120, default(Color), Main.rand.NextFloat(1.2f, 2.2f)).noGravity = true;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Vector2 normal = new Vector2(Projectile.ai[0], Projectile.ai[1]);
            if (normal == Vector2.Zero)
            {
                normal = -Vector2.UnitY;
            }

            normal.Normalize();
            SpawnSurfaceFlames(Projectile.Center, normal);
        }

        // How far above / below the impact each column is allowed to search for its ground
        // surface.  The downward bias lets ceiling hits "drip" to the floor and lets the spread
        // fall into pits, while a small upward allowance lets it climb gentle steps.
        private const int SurfaceSearchUp = 4;
        private const int SurfaceSearchDown = 16;

        private void SpawnSurfaceFlames(Vector2 impactCenter, Vector2 normal)
        {
            // Hit a (mostly) vertical wall face → keep the flames clinging to that wall.
            if (System.Math.Abs(normal.X) > 0.5f)
            {
                SpawnWallFlames(impactCenter, normal);
                return;
            }

            // Floor OR ceiling hit → spread along the GROUND contour beneath the impact.
            // We resolve each column's surface independently by scanning down for the first
            // walkable tile (solid with air above), so the flames follow uneven terrain —
            // steps, slopes, and gaps — instead of assuming a single flat row.  Scanning
            // downward also means a ceiling hit naturally drips to the floor below it.
            int centerX = (int)(impactCenter.X / 16f);
            int impactTileY = (int)(impactCenter.Y / 16f);

            for (int offset = -FireSpreadTiles; offset <= FireSpreadTiles; offset++)
            {
                int x = centerX + offset;
                if (!TryFindGroundColumn(x, impactTileY, out int groundY))
                {
                    continue; // pit / gap in the terrain — leave it unlit
                }

                // Rest the flame on top of the surface tile, facing up.
                Vector2 center = new Vector2(x * 16 + 8, groundY * 16 + 8) - Vector2.UnitY * 16f;
                SpawnFlame(center, -Vector2.UnitY);
            }
        }

        private void SpawnWallFlames(Vector2 impactCenter, Vector2 normal)
        {
            if (!TryFindSurfaceTile(impactCenter, normal, out Point baseTile))
            {
                return;
            }

            for (int offset = -FireSpreadTiles; offset <= FireSpreadTiles; offset++)
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
                SpawnFlame(center, normal);
            }
        }

        private void SpawnFlame(Vector2 center, Vector2 normal)
        {
            int flame = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                center,
                Vector2.Zero,
                ModContent.ProjectileType<EnemyFireFlaskLingeringFlame>(),
                Projectile.damage,
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
        /// Finds the ground surface tile in column <paramref name="x"/> nearest the impact row —
        /// the first solid tile (scanning top-down through the search window) that has open space
        /// directly above it.  Returns false if the column has no surface within range (a pit).
        /// </summary>
        private static bool TryFindGroundColumn(int x, int impactTileY, out int groundY)
        {
            int top = impactTileY - SurfaceSearchUp;
            int bottom = impactTileY + SurfaceSearchDown;
            for (int y = top; y <= bottom; y++)
            {
                // A standable surface (full block OR platform) with no full block directly above
                // it — so the flame has room to sit on top.
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

    public class EnemyFireFlaskLingeringFlame : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Weapons/FireFlask";

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 6 * 60;
            Projectile.penetrate = -1;
            Projectile.alpha = 255;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.light = 0.65f;
        }

        public override void AI()
        {
            Vector2 normal = new Vector2(Projectile.ai[0], Projectile.ai[1]);
            if (normal == Vector2.Zero)
            {
                normal = -Vector2.UnitY;
            }

            normal.Normalize();
            Projectile.velocity = Vector2.Zero;
            Projectile.rotation = normal.ToRotation() + MathHelper.PiOver2;

            if (Main.rand.NextBool(2))
            {
                Vector2 dustPos = Projectile.Center - normal * 4f + Main.rand.NextVector2Circular(9f, 9f);
                Vector2 dustVel = normal * Main.rand.NextFloat(0.4f, 1.4f) + Main.rand.NextVector2Circular(0.4f, 0.4f);
                Dust dust = Dust.NewDustPerfect(dustPos, DustID.Torch, dustVel, 100, default(Color), Main.rand.NextFloat(1.2f, 2.4f));
                dust.noGravity = true;
            }

            if (Main.rand.NextBool(6))
            {
                Vector2 smokeVel = -normal * Main.rand.NextFloat(0.2f, 0.8f);
                Dust.NewDustPerfect(Projectile.Center, DustID.Smoke, smokeVel, 160, default(Color), Main.rand.NextFloat(0.7f, 1.2f));
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 4 * 60);
        }
    }
}
