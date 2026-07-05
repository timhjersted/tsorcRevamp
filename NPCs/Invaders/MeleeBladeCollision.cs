using Microsoft.Xna.Framework;

namespace tsorcRevamp.NPCs.Invaders
{
    /// <summary>
    /// Geometry for invader melee hit detection: a "blade" is a line segment from the hand to the
    /// weapon tip, thickened by <c>thickness</c>.  Used so an invader's swing only lands while the
    /// weapon sprite is actually overlapping the target, instead of a static box sitting in place
    /// for the whole swing window regardless of where the sprite is pointing.
    /// </summary>
    public static class MeleeBladeCollision
    {
        /// <summary>Closest point on segment a→b to point p.</summary>
        public static Vector2 ClosestPointOnSegment(Vector2 a, Vector2 b, Vector2 p)
        {
            Vector2 ab = b - a;
            float lenSq = ab.LengthSquared();
            if (lenSq < 0.0001f)
                return a;
            float t = MathHelper.Clamp(Vector2.Dot(p - a, ab) / lenSq, 0f, 1f);
            return a + ab * t;
        }

        /// <summary>
        /// True if the thickened segment (capsule) from segA to segB overlaps rect.
        /// One-iteration closest-point approximation (segment-closest-to-rect-center, then
        /// clamped back into the rect) — not exact in every extreme configuration, but more
        /// than accurate enough for a blade-width capsule against a player-sized hurtbox.
        /// </summary>
        public static bool SegmentIntersectsRect(Vector2 segA, Vector2 segB, float thickness, Rectangle rect)
        {
            Vector2 rectCenter = new Vector2(rect.X + rect.Width * 0.5f, rect.Y + rect.Height * 0.5f);
            Vector2 halfSize = new Vector2(rect.Width * 0.5f, rect.Height * 0.5f);

            Vector2 onSegment = ClosestPointOnSegment(segA, segB, rectCenter);

            Vector2 diff = onSegment - rectCenter;
            Vector2 clamped = Vector2.Clamp(diff, -halfSize, halfSize);
            Vector2 onRect = rectCenter + clamped;

            float halfThickness = thickness * 0.5f;
            return Vector2.DistanceSquared(onSegment, onRect) <= halfThickness * halfThickness;
        }
    }
}
