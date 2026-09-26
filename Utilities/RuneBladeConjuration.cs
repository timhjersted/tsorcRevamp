using Microsoft.Xna.Framework;

namespace tsorcRevamp.Utilities
{
    // Shared by the actual staff, summoned blade, and offline SwingPreview; angles are
    // logical world directions for a right-facing caster (not melee's 45-degree sprite space).
    public static class RuneBladeConjuration
    {
        public const int TellTicks = 40, RepeatTellTicks = 32, SweepTicks = 40, FadeTicks = 16;
        public const int StepTicks = SweepTicks * 2 + FadeTicks + RepeatTellTicks;
        public const float Carry = -0.3f, High = -1.66f, Low = 2.18f;
        public const float Reach = 128f, OffsetY = 30f, OffsetX = 24f;
        public const float ApproachSpeed = 5f, CastSpeed = 3.2f, StartDistance = 96f;
        public static readonly WeightedSwing Curve = new WeightedSwing(8, 32, 6f);
        public static float Start(bool underhand) => underhand ? Low : High;
        public static float End(bool underhand) => underhand ? High : Low;
        public static float Sweep(bool underhand, float tick) => Curve.Apply(Start(underhand), End(underhand), tick);
        public static bool Underhand(bool firstUnderhand, int step) => firstUnderhand ^ (step % 2 != 0);
        public static int ActiveTicks(int count) => count * StepTicks - RepeatTellTicks;
        public static Vector2 Offset(bool underhand, int facing) => new Vector2(OffsetX * facing, underhand ? OffsetY : -OffsetY);
        public static float WorldAngle(float logical, int facing) => facing == 1 ? logical : MathHelper.Pi - logical;
        public static float Tell(bool underhand, float progress) => MathHelper.Lerp(Carry, Start(underhand), MathHelper.SmoothStep(0f, 1f, progress));
        public static float StaffPose(bool firstUnderhand, int elapsed, int count)
        {
            int step = System.Math.Min(count - 1, elapsed / StepTicks);
            int tick = elapsed - step * StepTicks;
            bool underhand = Underhand(firstUnderhand, step);
            if (tick <= SweepTicks) return Sweep(underhand, tick);
            if (tick < SweepTicks * 2 + FadeTicks)
                return MathHelper.Lerp(End(underhand), Carry,
                    MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp((tick - SweepTicks - 12f) / 44f, 0f, 1f)));
            return Tell(!underhand, (tick - SweepTicks * 2 - FadeTicks) / (float)RepeatTellTicks);
        }
        // Include the short acceleration and the frame straddling the 30%-speed cutoff
        // (8+32*ln(1/.3)/6 = 14.42t). Fifteen live frames give a broadsword-size sweep.
        public static bool Live(float tick) => tick >= 1f && tick <= System.Math.Ceiling(Curve.LiveTicks);
    }
}
