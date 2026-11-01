using System;
using Microsoft.Xna.Framework;

namespace tsorcRevamp.Content.Items.Weapons.Melee.Halberds
{
    // Angles are relative to the committed aim, facing right. The same functions drive the offline preview.
    public static class DarkrootHalberdMotion
    {
        public const int SpinWindup = 3, Spin = 4, SpinRecovery = 5;
        public const float Carry = -0.65f, High = -2.3353982f, Low = 1.5746018f;
        public const float DrawScale = 1.25f;
        public static readonly Vector2 Grip = new Vector2(12.5f, 61.5f);
        public static readonly Vector2 Point = new Vector2(70.5f, 1.5f);
        public static readonly Vector2 Neck = new Vector2(49.5f, 24.5f);
        public static readonly Vector2 Edge = new Vector2(67.5f, 20.5f);
        public static readonly Vector2 ShaftDirection = (Point - Grip) / (Point - Grip).Length();
        public static readonly float NativeAngle = (float)Math.Atan2(Point.Y - Grip.Y, Point.X - Grip.X);

        public static int BaseDuration(int step) => step switch { 0 => 44, 1 => 46, 2 => 56, SpinWindup => 24, Spin => 48, _ => 18 };
        public static int MinimumDuration(int step) => step switch { 0 => 34, 1 => 36, 2 => 44, SpinWindup => 20, Spin => 40, _ => 16 };
        public static float Stamina(int step) => step switch { 0 => 24, 1 => 26, _ => 30 };
        public static float DamageMultiplier(int step) => step switch { 1 => 1.1f, 2 => 1.35f, Spin => 0.85f, _ => 1f };
        public static float Time(int age, int duration, int step) => age * BaseDuration(step) / (float)Math.Max(1, duration);
        public static float Windup(int step) => step switch { 0 => 18, 1 => 20, 2 => 22, _ => 0 };
        static float Smooth(float value) { value = MathHelper.Clamp(value, 0, 1); return value * value * (3 - 2 * value); }
        static float Blend(float from, float to, float value) => MathHelper.Lerp(from, to, Smooth(value));

        public static bool Live(int step, float time) => step == Spin && time >= 0 && time < 48
            || step <= 2 && time >= Windup(step) && time < Windup(step) + 10;

        public static float Angle(int step, float time, float entry)
        {
            if (step == SpinWindup) return Blend(entry, High, time / 24f);
            if (step == Spin) return entry + MathHelper.TwoPi * MathHelper.Clamp(time / 48f, 0, 1);
            if (step == SpinRecovery) return entry + MathHelper.WrapAngle(Carry - entry) * Smooth(time / 18f);
            float windup = Windup(step);
            if (step == 2)
            {
                if (time < windup) return Blend(entry, 0, time / windup);
                if (time < 32) return 0;
                return Blend(0, Carry, (time - 32) / 16f);
            }
            float start = step == 0 ? High : Low;
            float end = step == 0 ? Low : High;
            if (time < windup) return Blend(entry, start, time / windup);
            float sign = step == 0 ? 1 : -1;
            float strikeEnd = start + sign * MathHelper.Pi;
            if (time < windup + 10) return Blend(start, strikeEnd, (time - windup) / 10f);
            return Blend(strikeEnd, end, (time - windup - 10) / 8f);
        }

        public static float Extension(int step, float time)
        {
            if (step != 2) return 0;
            if (time < 22) return 0;
            if (time < 28) return 16 * Smooth((time - 22) / 6f);
            if (time < 32) return 16;
            return 16 * (1 - Smooth((time - 32) / 16f));
        }

        public static Vector2 GripAt(float extension) => Grip - ShaftDirection * (extension / DrawScale);

        public static float PushDistance(float from, float to)
            => 32 * (MathHelper.Clamp((to - 22) / 8f, 0, 1) - MathHelper.Clamp((from - 22) / 8f, 0, 1));

        public static float RearArm(Vector2 frontHandOffset, float angle, int facing, float extension, out float error)
        {
            // Invert vanilla's full-stretch back-hand ellipse and choose a reachable point on the actual shaft.
            Vector2 axis = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
            Vector2 shoulder = new Vector2(6 * facing, -2);
            float butt = Math.Max(0, (GripAt(extension) - new Vector2(1.5f, 72.5f)).Length() * DrawScale - 2);
            float best = float.MaxValue, chosen = 0;
            error = float.MaxValue;
            for (float distance = -butt; distance <= 28; distance += 0.5f)
            {
                Vector2 desired = frontHandOffset + axis * distance;
                Vector2 gap = desired - shoulder;
                float theta = (float)Math.Atan2(gap.Y / 12f, gap.X / 10f);
                Vector2 actual = shoulder + new Vector2((float)Math.Cos(theta) * 10, (float)Math.Sin(theta) * 12);
                float miss = Vector2.Distance(actual, desired);
                float cost = miss * 100 + Math.Abs(distance - 8) * 0.01f;
                if (cost < best) { best = cost; chosen = theta - MathHelper.PiOver2; error = miss; }
            }
            return chosen;
        }

        // Flipping in local Y gives the exact horizontal reflection once the blade points left.
        public static Vector2 Offset(Vector2 pixel, float angle, int facing, float extension)
        {
            Vector2 offset = (pixel - GripAt(extension)) * DrawScale;
            if (facing < 0) offset.Y = -offset.Y;
            float rotation = angle - (facing < 0 ? -NativeAngle : NativeAngle);
            float cos = (float)Math.Cos(rotation), sin = (float)Math.Sin(rotation);
            return new Vector2(offset.X * cos - offset.Y * sin, offset.X * sin + offset.Y * cos);
        }
    }
}
