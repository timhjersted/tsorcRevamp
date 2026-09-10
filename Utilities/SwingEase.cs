using Microsoft.Xna.Framework;
using System;

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
        /// <summary>Tick-accurate accel/cruise/decel/hold shape — see <see cref="SwingEase.ApplyTrapezoidal"/>.
        /// Unlike the other styles (which reshape a plain 0..1 progress fraction), this one needs the
        /// step's raw tick counts, so it's handled as a special case in PuppetNPC.ApplySwingEase rather
        /// than through the generic <see cref="SwingEase.Apply(float,float,float,SwingEaseStyle)"/> switch.</summary>
        Trapezoidal,
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

        /// <summary>Tick-accurate accel/cruise/decel/hold swing: a short quadratic ease-in over the
        /// first <c>AccelDegrees</c>, a fast constant-speed cruise through the middle (the emergent
        /// "top speed" — naturally faster than either ramp since it covers most of the remaining
        /// angle in whatever ticks are left), a slow, deliberate quadratic ease-out over the final
        /// <c>DecelDegrees</c> that always takes exactly <c>DecelTicks</c> ticks regardless of the
        /// swing's overall duration, then a dead-stop hold at the finished pose for <c>HoldTicks</c>
        /// ticks so the weapon visibly lands instead of cutting off mid-motion. Built for Dread
        /// Wraith's spear swings (see PuppetNPC.ApplySwingEase) to replace an exponential
        /// Lerp-toward-target that never actually finished moving, just crept asymptotically closer
        /// and looked like it stalled halfway. Constants are hardcoded rather than parameterized
        /// since only those three motions use this today — promote them to parameters if a future
        /// caller needs a different shape.
        ///
        /// <para><b>Needs <c>totalTicks</c> well above 55.</b> Accel(10) + Decel(30) + Hold(15) is a
        /// 55-tick allowance, so at or below that the cruise plateau collapses to zero ticks and the
        /// entire plateau angle is covered in a single frame — measured at ~112°/tick for a 26-tick
        /// greatsword arc, i.e. a hard visual snap rather than the smooth sweep this is for. Callers
        /// today budget 70–118 ticks, which leaves a healthy plateau; Dread Wraith's tables carry a
        /// comment about raising AttackTicks from 14 to 75 for exactly this reason. Verify a new
        /// caller with <c>Documentation/tools/SwingPreview</c> before shipping it.</para></summary>
        public static float ApplyTrapezoidal(float start, float end, int elapsedTicks, int totalTicks)
        {
            const float AccelDegrees = 15f;
            const float DecelDegrees = 15f;
            const int AccelTicks = 10;
            const int DecelTicks = 30;
            const int HoldTicks = 15;

            totalTicks = Math.Max(1, totalTicks);
            elapsedTicks = (int)MathHelper.Clamp(elapsedTicks, 0, totalTicks);

            float totalSweep = end - start;
            float sweepSign = totalSweep >= 0f ? 1f : -1f;
            float totalSweepAbs = Math.Abs(totalSweep);

            float accelAngle = MathHelper.ToRadians(AccelDegrees);
            float decelAngle = MathHelper.ToRadians(DecelDegrees);

            // Safety: if the ramps would eat more angle than this swing actually covers (a tiny
            // motion, or a future caller with a small sweep), shrink both proportionally rather than
            // producing a negative plateau.
            float rampAngleTotal = accelAngle + decelAngle;
            if (rampAngleTotal > totalSweepAbs && rampAngleTotal > 0f)
            {
                float shrink = totalSweepAbs / rampAngleTotal;
                accelAngle *= shrink;
                decelAngle *= shrink;
            }

            // Same idea for ticks: if the caller didn't budget enough AttackTicks for the full
            // accel+decel+hold allowance, shrink proportionally so the shape still fits.
            int accelTicks = Math.Min(AccelTicks, totalTicks);
            int decelHoldBudget = Math.Max(0, totalTicks - accelTicks);
            int decelHoldWant = DecelTicks + HoldTicks;
            int decelTicks = decelHoldWant > 0
                ? (int)Math.Round(Math.Min(decelHoldBudget, decelHoldWant) * (DecelTicks / (float)decelHoldWant))
                : 0;
            int holdTicks = Math.Max(0, Math.Min(decelHoldBudget, decelHoldWant) - decelTicks);
            int plateauTicks = Math.Max(0, totalTicks - accelTicks - decelTicks - holdTicks);

            float plateauAngle = Math.Max(0f, totalSweepAbs - accelAngle - decelAngle);

            float angle;
            if (elapsedTicks <= accelTicks)
            {
                float progress = accelTicks > 0 ? elapsedTicks / (float)accelTicks : 1f;
                angle = accelAngle * progress * progress; // ease-in: rest -> top speed
            }
            else if (elapsedTicks <= accelTicks + plateauTicks)
            {
                float progress = plateauTicks > 0 ? (elapsedTicks - accelTicks) / (float)plateauTicks : 1f;
                angle = accelAngle + plateauAngle * progress; // constant-speed cruise
            }
            else if (elapsedTicks <= accelTicks + plateauTicks + decelTicks)
            {
                float progress = decelTicks > 0
                    ? (elapsedTicks - accelTicks - plateauTicks) / (float)decelTicks
                    : 1f;
                angle = accelAngle + plateauAngle + decelAngle * (1f - (1f - progress) * (1f - progress)); // ease-out to a stop
            }
            else
            {
                angle = totalSweepAbs; // hold: fully settled at the end pose
            }

            return start + sweepSign * angle;
        }
    }
}
