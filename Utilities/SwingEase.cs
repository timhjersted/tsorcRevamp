using Microsoft.Xna.Framework;

namespace tsorcRevamp.Utilities
{
    /// <summary>Named easing shapes for a swing arc, so different attacks can feel different even at
    /// the same duration — a slow menacing wind-up vs a snappy flick vs a constant-speed sweep.</summary>
    public enum SwingEaseStyle
    {
        /// <summary>Constant angular velocity for the whole swing (a bare MathHelper.Lerp).</summary>
        Linear,
        /// <summary>Slow out of the wind-up, fast through the middle, settles into the follow-through
        /// — the player-weapon QuickSlashMeleeAnimation curve (BroadswordRework). Reads "heavy".</summary>
        Smooth,
        /// <summary>Front-loaded: most of the rotation happens in the first third, then decelerates —
        /// a fast committed strike that "lands" early. Reads "snappy / light".</summary>
        Snap,
        /// <summary>Back-loaded: eases slowly at first then whips through the end — a wind-up that
        /// holds the apex a beat longer before the strike. Reads "delayed / baiting".</summary>
        Whip,
    }

    /// <summary>
    /// Optional non-linear replacement for a plain MathHelper.Lerp swing arc. Same keyframe approach
    /// as the player-weapon QuickSlashMeleeAnimation curve (BroadswordRework): reshape the timing of
    /// a swing without changing its start/end angles.
    /// </summary>
    public static class SwingEase
    {
        /// <summary>Returns the eased (or, if useEasing is false, plain linear) value at progress t
        /// [0..1] between start and end. Falls back to identical output to a bare MathHelper.Lerp
        /// call when useEasing is false, so this is a drop-in, zero-behavior-change replacement
        /// until an invader opts in.</summary>
        public static float Apply(float start, float end, float t, bool useEasing)
            => Apply(start, end, t, useEasing ? SwingEaseStyle.Smooth : SwingEaseStyle.Linear);

        /// <summary>Returns the value at progress t [0..1] between start and end, shaped by the
        /// chosen <see cref="SwingEaseStyle"/>.</summary>
        public static float Apply(float start, float end, float t, SwingEaseStyle style)
        {
            switch (style)
            {
                case SwingEaseStyle.Linear:
                    return MathHelper.Lerp(start, end, t);

                case SwingEaseStyle.Snap:
                {
                    // Front-loaded: ~85% of the arc is covered by t=0.35, then it eases out.
                    var snap = new Gradient<float>(
                        (0.00f, start),
                        (0.15f, MathHelper.Lerp(start, end, 0.45f)),
                        (0.35f, MathHelper.Lerp(start, end, 0.85f)),
                        (0.55f, MathHelper.Lerp(start, end, 0.97f)),
                        (0.75f, end),
                        (1.00f, end)
                    );
                    return snap.GetValue(t);
                }

                case SwingEaseStyle.Whip:
                {
                    // Back-loaded: barely moves until ~t=0.5, then whips through the strike.
                    var whip = new Gradient<float>(
                        (0.00f, start),
                        (0.35f, MathHelper.Lerp(start, end, 0.08f)),
                        (0.50f, MathHelper.Lerp(start, end, 0.20f)),
                        (0.70f, MathHelper.Lerp(start, end, 0.60f)),
                        (0.85f, MathHelper.Lerp(start, end, 0.92f)),
                        (1.00f, end)
                    );
                    return whip.GetValue(t);
                }

                case SwingEaseStyle.Smooth:
                default:
                {
                    var curve = new Gradient<float>(
                        (0.00f, start),
                        (0.10f, MathHelper.Lerp(start, end, 0.10f)),
                        (0.15f, MathHelper.Lerp(start, end, 0.125f)),
                        (0.30f, MathHelper.Lerp(start, end, 0.50f)),
                        (0.50f, MathHelper.Lerp(start, end, 0.75f)),
                        (0.60f, MathHelper.Lerp(start, end, 0.90f)),
                        (0.75f, MathHelper.Lerp(start, end, 0.96f)),
                        (0.90f, end),
                        (1.00f, end)
                    );
                    return curve.GetValue(t);
                }
            }
        }
    }
}
