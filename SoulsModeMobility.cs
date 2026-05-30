using System;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp
{
    public static class SoulsModeMobility
    {
        public const int SupersonicBootsLevel = 1;
        public const int SupersonicWingsLevel = 2;
        public const int SupersonicWings2Level = 3;
        public const int WingsOfSeathLevel = 4;

        public const float SupersonicBootsRunSpeed = 6.85f;
        public const float SupersonicWingsRunSpeed = 7.00f;
        public const float SupersonicWings2RunSpeed = 7.00f;
        public const float WingsOfSeathRunSpeed = 8.00f;

        public const int SupersonicWings2FlightTime = 600;
        public const int WingsOfSeathFlightTime = 1200;

        public const float SupersonicWingsFlightSpeed = 6.00f;
        public const float SupersonicWingsFlightAcceleration = 0.10f;

        public const float SupersonicWings2FlightSpeed = 6.25f;
        public const float SupersonicWings2FlightAcceleration = 0.12f;

        public const float WingsOfSeathFlightSpeed = 7.00f;
        public const float WingsOfSeathFlightAcceleration = 0.18f;
        public const float WingsOfSeathHoverFlightSpeed = 7.50f;
        public const float WingsOfSeathHoverFlightAcceleration = 0.25f;

        public const float GlobalRunSpeedCap = 8.00f;
        public const float GlobalFlightSpeedCap = 8.00f;
        public const float GlobalFlightAccelerationCap = 0.30f;

        public static bool Enabled(Player player)
        {
            return player.GetModPlayer<tsorcRevampPlayer>().SoulsMode
                && ModContent.GetInstance<tsorcRevampConfig>().EnableSoulsModeMobilityLimit;
        }

        public static void ApplyFlightCap(Player player, ref float speed, ref float acceleration)
        {
            if (!Enabled(player))
            {
                return;
            }

            speed = Math.Min(speed, GlobalFlightSpeedCap);
            acceleration = Math.Min(acceleration, GlobalFlightAccelerationCap);
        }
    }
}
