using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace tsorcRevamp
{
    ///<summary>
    ///Reusable movement AI for HOVERING FLOATERS — demon-eye / hummingbird enemies that drift, can fully
    ///stop and hover, and move in any direction (unlike forward-flyers that must keep momentum, or ground
    ///walkers driven by FighterAI / SmartProjectileFighterAI). Works on any <see cref="Entity"/> (NPC or
    ///Projectile): it only reads/sets velocity + reads size, so the caller owns tileCollide and gravity
    ///(a floater should have NO gravity). State is caller-owned (store a <see cref="FloaterState"/> field).
    ///
    ///Navigation is a FLYING A* over open-air tiles (8-directional, ignores platforms) — the flying
    ///counterpart to SF4's ground A*. When it has line-of-sight to the target it hovers directly in a
    ///distance band (with a little drift/bob); when LOS is blocked it paths AROUND terrain to regain it,
    ///so a ranged floater's shots never spawn inside walls.
    ///</summary>
    public static class FloaterAI
    {
        const float Tile = 16f;

        public class FloaterState
        {
            public List<Point> Path;
            public int PathIndex;
            public int ReplanTimer;
            public float Bob;
            public int StuckTimer;
            public Vector2 LastCenter;
        }

        ///<summary>Steer <paramref name="self"/> toward <paramref name="target"/>, hovering in
        ///[hoverMin,hoverMax] px and pathing around terrain to keep line-of-sight. Sets self.velocity.
        ///Returns true if it currently has LOS to the target (a good moment to fire).</summary>
        public static bool Run(Entity self, Entity target, FloaterState s,
            float topSpeed = 6f, float accel = 0.10f,
            float hoverMin = 180f, float hoverMax = 360f,
            int navRadius = 30, float bobAmplitude = 10f, bool keepLOS = true, float driftAmount = 22f)
        {
            s.Bob += 0.06f;
            if (target == null || !target.active)
            {
                self.velocity *= 0.92f;
                return false;
            }

            Vector2 toTarget = target.Center - self.Center;
            float dist = toTarget.Length();
            bool los = Collision.CanHitLine(self.Center, 1, 1, target.Center, 1, 1);

            int fw = Math.Max(1, self.width / 16);
            int fh = Math.Max(1, self.height / 16);

            Vector2 desired;
            if (los || !keepLOS)
            {
                // Direct hover: sit at the band midpoint on the sightline, with a little lateral drift + bob.
                s.Path = null;
                Vector2 dir = toTarget.SafeNormalize(Vector2.UnitX);
                float band = (hoverMin + hoverMax) * 0.5f;
                desired = target.Center - dir * band;
                desired += new Vector2((float)Math.Cos(s.Bob * 0.7f) * driftAmount, (float)Math.Sin(s.Bob) * bobAmplitude);
            }
            else
            {
                // LOS blocked: fly a path around the terrain.
                if (s.ReplanTimer <= 0 || s.Path == null || s.PathIndex >= s.Path.Count)
                {
                    Point start = self.position.ToTileCoordinates();
                    Point goal = target.position.ToTileCoordinates();
                    s.Path = FindFlyingPath(start, goal, navRadius, fw, fh);
                    s.PathIndex = 0;
                    s.ReplanTimer = 30;
                }
                if (s.ReplanTimer > 0) s.ReplanTimer--;

                if (s.Path != null && s.PathIndex < s.Path.Count)
                {
                    Point wp = s.Path[s.PathIndex];
                    Vector2 wpCenter = new Vector2(wp.X * Tile + fw * Tile * 0.5f, wp.Y * Tile + fh * Tile * 0.5f);
                    if (Vector2.Distance(self.Center, wpCenter) < 24f) s.PathIndex++;
                    desired = wpCenter;
                }
                else
                {
                    desired = target.Center; // no path found — steer straight and let physics stop us
                }
            }

            // Eased seek toward the desired point, clamped to top speed.
            Vector2 seek = desired - self.Center;
            float sd = seek.Length();
            Vector2 wanted = sd > 1f ? seek / sd * MathHelper.Min(sd * 0.06f, topSpeed) : Vector2.Zero;
            self.velocity = Vector2.Lerp(self.velocity, wanted, accel);

            HandleStuck(self, s);
            s.LastCenter = self.Center;
            return los;
        }

        // Pressed against terrain (barely moved for a while though it wants to): give a perpendicular nudge
        // and force a replan so it tries a new route.
        static void HandleStuck(Entity self, FloaterState s)
        {
            if (self.velocity.Length() > 0.6f && Vector2.Distance(self.Center, s.LastCenter) > 1.5f)
            {
                s.StuckTimer = 0;
                return;
            }
            s.StuckTimer++;
            if (s.StuckTimer > 40)
            {
                s.StuckTimer = 0;
                s.ReplanTimer = 0;
                self.velocity += new Vector2(Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-4f, -1f));
            }
        }

        #region Flying A*

        static bool Solid(int x, int y)
        {
            if (x < 5 || y < 5 || x > Main.maxTilesX - 5 || y > Main.maxTilesY - 5) return true;
            Tile t = Main.tile[x, y];
            // Platforms (solid-top) are passable for a floater; only real solids block.
            return t.HasTile && !t.IsActuated && Main.tileSolid[t.TileType] && !Main.tileSolidTop[t.TileType];
        }

        static bool Fits(int x, int y, int fw, int fh)
        {
            for (int dx = 0; dx < fw; dx++)
                for (int dy = 0; dy < fh; dy++)
                    if (Solid(x + dx, y + dy)) return false;
            return true;
        }

        static Point NearestFit(Point p, int fw, int fh, int maxR)
        {
            for (int r = 0; r <= maxR; r++)
                for (int dx = -r; dx <= r; dx++)
                    for (int dy = -r; dy <= r; dy++)
                        if (Math.Abs(dx) == r || Math.Abs(dy) == r)
                            if (Fits(p.X + dx, p.Y + dy, fw, fh))
                                return new Point(p.X + dx, p.Y + dy);
            return p;
        }

        static readonly Point[] Dirs8 =
        {
            new Point(1,0), new Point(-1,0), new Point(0,1), new Point(0,-1),
            new Point(1,1), new Point(1,-1), new Point(-1,1), new Point(-1,-1),
        };

        static List<Point> FindFlyingPath(Point start, Point goal, int radius, int fw, int fh)
        {
            if (!Fits(goal.X, goal.Y, fw, fh)) goal = NearestFit(goal, fw, fh, 6);
            if (start == goal) return null;

            int minX = start.X - radius, maxX = start.X + radius;
            int minY = start.Y - radius, maxY = start.Y + radius;

            var open = new PriorityQueue<Point, float>();
            var g = new Dictionary<Point, float>();
            var came = new Dictionary<Point, Point>();
            open.Enqueue(start, 0f);
            g[start] = 0f;

            int budget = 5000;
            Point best = start;
            float bestH = Heuristic(start, goal);

            while (open.Count > 0 && budget-- > 0)
            {
                Point cur = open.Dequeue();
                if (cur == goal) return Reconstruct(came, cur, start);

                float h = Heuristic(cur, goal);
                if (h < bestH) { bestH = h; best = cur; }

                float cg = g[cur];
                for (int i = 0; i < Dirs8.Length; i++)
                {
                    Point d = Dirs8[i];
                    Point nb = new Point(cur.X + d.X, cur.Y + d.Y);
                    if (nb.X < minX || nb.X > maxX || nb.Y < minY || nb.Y > maxY) continue;
                    if (!Fits(nb.X, nb.Y, fw, fh)) continue;
                    bool diag = d.X != 0 && d.Y != 0;
                    if (diag && (Solid(cur.X + d.X, cur.Y) || Solid(cur.X, cur.Y + d.Y))) continue; // no corner cutting
                    float ng = cg + (diag ? 1.414f : 1f);
                    if (!g.TryGetValue(nb, out float old) || ng < old)
                    {
                        g[nb] = ng;
                        came[nb] = cur;
                        open.Enqueue(nb, ng + Heuristic(nb, goal));
                    }
                }
            }
            // No complete path — head toward the closest node we reached (still routes partway around).
            return best == start ? null : Reconstruct(came, best, start);
        }

        static float Heuristic(Point a, Point b)
        {
            float dx = a.X - b.X, dy = a.Y - b.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        static List<Point> Reconstruct(Dictionary<Point, Point> came, Point cur, Point start)
        {
            var path = new List<Point>();
            while (cur != start && came.ContainsKey(cur))
            {
                path.Add(cur);
                cur = came[cur];
            }
            path.Reverse();
            return path.Count > 0 ? path : null;
        }

        #endregion
    }
}
