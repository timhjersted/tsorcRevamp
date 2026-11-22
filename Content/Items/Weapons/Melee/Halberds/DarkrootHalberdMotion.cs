using System;
using Microsoft.Xna.Framework;

namespace tsorcRevamp.Content.Items.Weapons.Melee.Halberds
{
    // Angles are relative to the committed aim, facing right. The same functions drive the offline preview.
    public static class DarkrootHalberdMotion
    {
        public const int SpinWindup = 3, Spin = 4, SpinSlam = 5, Charge = 6, Dash = 7;

        // Pierce (step 2) grip: the hands ride up to mid-shaft in the windup (MidHold px of the spear behind the
        // normal grip), the spear slides forward through them on the thrust to ThrustReach, then retracts to
        // mid-shaft again during the rise. ThrustPush is the player's forward travel during the thrust.
        public const float MidHold = -42f;
        public const float ThrustReach = 16f;
        public const float ThrustPush = 64f;

        // Hold-left charge and release dash. Charge: spear level at the cursor, drawn back ChargePullback px while
        // the arm shortens. Dash: up to DashRangeTiles to the cursor over DashTravel base ticks, DashDamage x damage.
        public const int ChargeHoldTicks = 14;     // a fresh press still held this long trades the swing for the charge
        public const float ChargePullback = -26f;
        public const float ChargeFullTicks = 20f;  // pullback and arm shortening complete
        public const int DashRangeTiles = 20;
        public const float DashTravel = 14f;
        public const float DashDamage = 3f;

        // Spin slam (released right click): the rotation carries on at spin speed, accelerating over the top into
        // the floor. SlamAngle puts the point just into flat ground for a standing player (104px point, hand ~3px
        // below MountedCenter, floor 21px below). Then a short plant and a rise back to Carry.
        public const float SlamAngle = 0.2f;
        public const float SlamRotateTicks = 16f;
        public const float SlamPlantEnd = 20f;
        public const float RootDamageMultiplier = 2f; // the slam's root eruption deals this x the halberd's damage
        const float SpinSpeed = MathHelper.TwoPi / 48f;
        public const float Carry = -0.65f, High = -2.3353982f, Low = 1.5746018f;
        public const float DrawScale = 1.25f;
        public static readonly Vector2 Grip = new Vector2(12.5f, 61.5f);
        public static readonly Vector2 Point = new Vector2(70.5f, 1.5f);
        public static readonly Vector2 Neck = new Vector2(49.5f, 24.5f);
        public static readonly Vector2 Edge = new Vector2(67.5f, 20.5f);
        public static readonly Vector2 Butt = new Vector2(1.5f, 72.5f);
        public static readonly Vector2 ShaftDirection = (Point - Grip) / (Point - Grip).Length();
        public static readonly float NativeAngle = (float)Math.Atan2(Point.Y - Grip.Y, Point.X - Grip.X);

        // Charge lasts as long as the button is held; 600 is only the auto-release cap.
        public static int BaseDuration(int step) => step switch { 0 => 44, 1 => 46, 2 => 56, SpinWindup => 24, Spin => 48, Charge => 600, _ => 30 };
        public static int MinimumDuration(int step) => step switch { 0 => 34, 1 => 36, 2 => 44, SpinWindup => 20, Spin => 40, Charge => 600, _ => 24 };
        public static float Stamina(int step) => step switch { 0 => 24, 1 => 26, _ => 30 };
        public static float DamageMultiplier(int step) => step switch { 1 => 1.1f, 2 => 1.35f, Spin => 0.85f, Dash => DashDamage, _ => 1f };
        public static float Time(int age, int duration, int step) => age * BaseDuration(step) / (float)Math.Max(1, duration);
        public static float Windup(int step) => step switch { 0 => 18, 1 => 20, 2 => 22, _ => 0 };
        static float Smooth(float value) { value = MathHelper.Clamp(value, 0, 1); return value * value * (3 - 2 * value); }
        static float Blend(float from, float to, float value) => MathHelper.Lerp(from, to, Smooth(value));

        public static bool Live(int step, float time) => step == Spin && time >= 0 && time < 48
            || step == SpinSlam && time >= 0 && time < SlamRotateTicks
            || step == Dash && time >= 0 && time < DashTravel + 2f
            || step <= 2 && time >= Windup(step) && time < Windup(step) + 10;

        public static float Angle(int step, float time, float entry)
        {
            if (step == SpinWindup) return Blend(entry, High, time / 24f);
            if (step == Spin) return entry + MathHelper.TwoPi * MathHelper.Clamp(time / 48f, 0, 1);
            if (step == SpinSlam)
            {
                return SlamAngleAt(time, entry);
            }

            // Charge: level the spear along the (cursor-tracking) aim. Dash: stay level through the travel, then
            // rise back to Carry.
            if (step == Charge)
            {
                return Blend(entry, 0f, time / 8f);
            }

            if (step == Dash)
            {
                if (time < DashTravel)
                {
                    return 0f;
                }

                return Blend(0f, Carry, (time - DashTravel) / 16f);
            }
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

        static float SlamAngleAt(float time, float entry)
        {
            // Forward arc still to cover, taken the short way round but always forward (the spin's direction).
            float arc = SlamAngle - entry;
            while (arc < 0.5f)
            {
                arc += MathHelper.TwoPi;
            }

            while (arc > MathHelper.TwoPi + 0.5f)
            {
                arc -= MathHelper.TwoPi;
            }

            if (time < SlamRotateTicks)
            {
                // Constant acceleration from spin speed so the arc lands exactly at SlamRotateTicks: no snap at the
                // hand-off from the last rotation, and the blade gains speed into the floor.
                float acceleration = 2f * (arc - SpinSpeed * SlamRotateTicks) / (SlamRotateTicks * SlamRotateTicks);
                return entry + SpinSpeed * time + 0.5f * acceleration * time * time;
            }

            float landed = entry + arc;
            if (time < SlamPlantEnd)
            {
                return landed;
            }

            float riseTarget = landed + MathHelper.WrapAngle(Carry - landed);
            return Blend(landed, riseTarget, (time - SlamPlantEnd) / 10f);
        }

        /// <summary>Spear slide through the hands in world px: positive = spear further forward of the fist.</summary>
        public static float Extension(int step, float time)
        {
            if (step == 2)
            {
                // Windup: hands ride up to mid-shaft. Thrust: the spear slides forward through them.
                // Rise: it retracts back to the mid-shaft hold.
                if (time < 22)
                {
                    return Blend(0f, MidHold, time / 14f);
                }

                if (time < 28)
                {
                    return Blend(MidHold, ThrustReach, (time - 22) / 6f);
                }

                if (time < 32)
                {
                    return ThrustReach;
                }

                return Blend(ThrustReach, MidHold, (time - 32) / 14f);
            }

            if (step == Charge)
            {
                return Blend(0f, ChargePullback, time / ChargeFullTicks);
            }

            if (step == Dash)
            {
                if (time < DashTravel)
                {
                    return Blend(ChargePullback, ThrustReach, time / 4f);
                }

                return Blend(ThrustReach, 0f, (time - DashTravel) / 16f);
            }

            return 0;
        }

        public static Vector2 GripAt(float extension) => Grip - ShaftDirection * (extension / DrawScale);

        public static float PushDistance(float from, float to)
            => ThrustPush * (MathHelper.Clamp((to - 22) / 8f, 0, 1) - MathHelper.Clamp((from - 22) / 8f, 0, 1));

        public static float RearArm(Vector2 frontHandOffset, float angle, int facing, float extension, out float error)
        {
            // Invert vanilla's full-stretch back-hand ellipse and choose a reachable point on the actual shaft.
            Vector2 axis = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
            Vector2 shoulder = new Vector2(6 * facing, -2);
            float butt = Math.Max(0, (GripAt(extension) - Butt).Length() * DrawScale - 2);
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
