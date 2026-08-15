using Microsoft.Xna.Framework;
using System;
using Terraria;

namespace tsorcRevamp
{
    /// <summary>
    /// Terrain-validated hop planning for humanoid enemies that want to jump WHILE attacking, rather than
    /// standing still to throw. Extracted from RedKnightAttackController so the Black Knight family can use
    /// the same logic instead of copying it; the red controller now forwards to these.
    /// </summary>
    /// <remarks>
    /// The point of the validation is that a throw-hop is committed at takeoff and has no air control, so a
    /// hop that ends in a wall or off a cliff cannot be corrected mid-flight. Everything here is therefore
    /// checked BEFORE the jump: the full predicted parabola is swept with the enemy's real body box (not a
    /// headroom ray, which happily clears low ceilings and ledges the body cannot), and the landing point has
    /// to be standable ground at a survivable height difference.
    /// </remarks>
    public static class KnightHopPlanner
    {
        /// <summary>Vertical takeoff speed of a standard knight throw-hop.</summary>
        public const float DefaultHopSpeedY = 5.2f;

        /// <summary>Gravity the hop arc is predicted against. Must match what the caller's AI applies.</summary>
        public const float DefaultGravity = 0.35f;

        /// <summary>
        /// How much room an ADVANCING hop must still leave between its landing point and the target, so
        /// closing in never lands the enemy on top of (or past) the player.
        /// </summary>
        public const float DefaultForwardClearance = 72f;

        /// <summary>
        /// Finds standable ground near <paramref name="around"/>, scanning up then down from it. Rejects
        /// surfaces with a solid tile directly overhead, since an enemy cannot occupy those.
        /// </summary>
        public static bool TryFindGround(Vector2 around, int searchUpTiles, int searchDownTiles, out Vector2 surface)
        {
            int tileX = Utils.Clamp((int)(around.X / 16f), 2, Main.maxTilesX - 3);
            int originY = Utils.Clamp((int)(around.Y / 16f), 5, Main.maxTilesY - 10);
            int startY = Utils.Clamp(originY - searchUpTiles, 5, Main.maxTilesY - 10);
            int endY = Utils.Clamp(originY + searchDownTiles, 5, Main.maxTilesY - 5);
            for (int tileY = startY; tileY <= endY; tileY++)
            {
                Tile tile = Framing.GetTileSafely(tileX, tileY);
                bool standable = tile.HasTile && !tile.IsActuated
                    && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]);
                Tile above = Framing.GetTileSafely(tileX, tileY - 1);
                bool blockedAbove = above.HasTile && !above.IsActuated
                    && Main.tileSolid[above.TileType] && !Main.tileSolidTop[above.TileType];
                if (standable && !blockedAbove)
                {
                    surface = new Vector2(tileX * 16f + 8f, tileY * 16f - 2f);
                    return true;
                }
            }
            surface = Vector2.Zero;
            return false;
        }

        /// <summary>
        /// Accelerates toward a target horizontal speed without ever snapping to it, so a change of pace
        /// reads as the enemy building into a run rather than teleporting up to speed.
        /// </summary>
        public static void ApproachHorizontalSpeed(NPC npc, int direction, float speed, float acceleration)
        {
            float target = direction * speed;
            if (npc.velocity.X < target)
            {
                npc.velocity.X = Math.Min(npc.velocity.X + acceleration, target);
            }
            else if (npc.velocity.X > target)
            {
                npc.velocity.X = Math.Max(npc.velocity.X - acceleration, target);
            }
        }

        /// <summary>
        /// Whether a hop in <paramref name="travelDirection"/> is actually survivable and useful from where
        /// the enemy is standing right now. Call this both when CHOOSING a hop and again on the takeoff tick:
        /// the enemy may have moved between the two.
        /// </summary>
        /// <param name="facing">Which way the enemy is facing, used to sign the forward-clearance test.</param>
        /// <param name="travelDirection">Which way the hop travels; may oppose <paramref name="facing"/> for a
        /// retreating hop, which is the whole point of throwing while backing off.</param>
        /// <param name="advancing">True only for hops meant to close distance, which get the extra clearance
        /// check against the target. Retreating and vertical hops skip it.</param>
        public static bool HasSafeHop(NPC npc, Vector2 target, int facing, int travelDirection,
            float horizontalSpeed, bool advancing,
            float hopSpeedY = DefaultHopSpeedY, float gravity = DefaultGravity,
            float minimumForwardClearance = DefaultForwardClearance)
        {
            if (gravity <= 0f)
            {
                return false;
            }

            int flightTicks = (int)Math.Ceiling(2f * hopSpeedY / gravity);
            float landingX = npc.Center.X + travelDirection * horizontalSpeed * flightTicks;
            if (advancing && (target.X - landingX) * facing < minimumForwardClearance)
            {
                return false;
            }

            // Sweep the whole body along the predicted parabola, not just a headroom ray: this is what
            // rejects low ceilings, walls and ledges that cannot actually receive the enemy.
            for (int tick = 4; tick < flightTicks; tick += 4)
            {
                float x = npc.Center.X + travelDirection * horizontalSpeed * tick;
                float y = npc.Center.Y - hopSpeedY * tick + 0.5f * gravity * tick * tick;
                Vector2 topLeft = new Vector2(x - npc.width * 0.5f, y - npc.height * 0.5f);
                if (Collision.SolidCollision(topLeft, npc.width, npc.height))
                {
                    return false;
                }
            }

            if (!TryFindGround(new Vector2(landingX, npc.Bottom.Y), 5, 8, out Vector2 surface)
                || Math.Abs(surface.Y - npc.Bottom.Y) > 64f)
            {
                return false;
            }
            Vector2 landingTopLeft = new Vector2(landingX - npc.width * 0.5f, surface.Y - npc.height - 2f);
            return !Collision.SolidCollision(landingTopLeft, npc.width, npc.height);
        }
    }
}
