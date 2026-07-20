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

        public const float SupersonicBootsRunSpeed = 7f;
        public const float SupersonicWingsRunSpeed = 7.25f;
        public const float SupersonicWings2RunSpeed = 7.5f;
        public const float WingsOfSeathRunSpeed = 8.25f;

        public const int SupersonicWings2FlightTime = 600;
        public const int WingsOfSeathFlightTime = 1200;

        public const float SupersonicWingsFlightSpeed = 6.25f;
        public const float SupersonicWingsFlightAcceleration = 0.12f;

        public const float SupersonicWings2FlightSpeed = 6.55f;
        public const float SupersonicWings2FlightAcceleration = 0.15f;

        public const float WingsOfSeathFlightSpeed = 7.25f;
        public const float WingsOfSeathFlightAcceleration = 0.2f;
        public const float WingsOfSeathHoverFlightSpeed = 7.75f;
        public const float WingsOfSeathHoverFlightAcceleration = 0.26f;

        public const float GlobalRunSpeedCap = 10.00f;
        public const float GlobalFlightSpeedCap = 10.00f;
        public const float GlobalFlightAccelerationCap = 0.32f;

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
