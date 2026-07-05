using Microsoft.Xna.Framework;

namespace tsorcRevamp.Utilities
{
    /// <summary>
    /// Optional non-linear replacement for a plain MathHelper.Lerp swing arc. Same keyframe shape
    /// as the player-weapon QuickSlashMeleeAnimation curve (BroadswordRework): slow out of the
    /// wind-up, fast through the middle, settles into the follow-through — instead of constant
    /// angular velocity for the whole swing.
    /// </summary>
    public static class SwingEase
    {
        /// <summary>Returns the eased (or, if useEasing is false, plain linear) value at progress t
        /// [0..1] between start and end. Falls back to identical output to a bare MathHelper.Lerp
        /// call when useEasing is false, so this is a drop-in, zero-behavior-change replacement
        /// until an invader opts in.</summary>
        public static float Apply(float start, float end, float t, bool useEasing)
        {
            if (!useEasing)
                return MathHelper.Lerp(start, end, t);

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
