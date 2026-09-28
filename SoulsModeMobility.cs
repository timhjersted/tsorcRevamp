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

        // Souls Mode Ground Speeds & Scaling (Replaced hard caps with smooth scaling)
        public const float SupersonicBootsBaseSpeed = 6.00f;
        public const float SupersonicBootsBoostPercent = 0.10f;
        public const float SupersonicBootsMoveSpeedBonus = 0.10f;

        public const float SupersonicWingsBaseSpeed = 6.40f;
        public const float SupersonicWingsBoostPercent = 0.15f;
        public const float SupersonicWingsMoveSpeedBonus = 0.15f;

        public const float SupersonicWings2BaseSpeed = 6.80f;
        public const float SupersonicWings2BoostPercent = 0.20f;
        public const float SupersonicWings2MoveSpeedBonus = 0.20f;

        public const float WingsOfSeathBaseSpeed = 7.20f;
        public const float WingsOfSeathBoostPercent = 0.25f;
        public const float WingsOfSeathMoveSpeedBonus = 0.25f;

        // Legacy reference values
        public const float SupersonicBootsRunSpeed = 7f;
        public const float SupersonicWingsRunSpeed = 7.25f;
        public const float SupersonicWings2RunSpeed = 7.5f;
        public const float WingsOfSeathRunSpeed = 8.25f;

        // Harness fallback flight times (frames) when no wing is slotted in Souls Mode
        public const int HarnessFallbackFlightTimeTier1 = 90; // 1.5s
        public const int HarnessFallbackFlightTimeTier2 = 180; // 3.0s

        public const int SupersonicWings2FlightTime = 600;
        public const int WingsOfSeathFlightTime = 300; // 5.0s in Souls Mode (down from 1200 / infinite)
        public const int WingsOfSeathSuppressedFlightTime = 180;

        public const float SupersonicWingsFlightSpeed = 6.25f;
        public const float SupersonicWingsFlightAcceleration = 0.12f;

        public const float SupersonicWings2FlightSpeed = 6.55f;
        public const float SupersonicWings2FlightAcceleration = 0.15f;

        public const float WingsOfSeathFlightSpeed = 7.25f;
        public const float WingsOfSeathFlightAcceleration = 0.2f;
        public const float WingsOfSeathHoverFlightSpeed = 7.75f;
        public const float WingsOfSeathHoverFlightAcceleration = 0.26f;
        public const float WingsOfSeathAscentMultiplier = 3.20f;

        // Suppressed vertical caps: the run-speed nerf (6f, Seath 7f) left jumping and wing ascent untouched, so
        // vertical mobility dwarfed horizontal. Same split as the run caps: the three named items share one cap,
        // Wings of Seath keeps a slightly higher one. Jump speed is px/tick (vanilla base 5.01, jumpBoost 6.51);
        // jump height is ticks the jump key keeps adding lift (vanilla base 15, jumpBoost 20).
        public const float SuppressedJumpSpeed = 5.8f;
        public const float SuppressedJumpSpeedSeath = 6.3f;
        public const int SuppressedJumpHeight = 18;
        public const int SuppressedJumpHeightSeath = 20;

        // Suppressed wing ascent caps, in the units of ModItem.VerticalWingSpeeds. Rising / constant /
        // falling caps equal Supersonic Wings' own values; Seath is capped down from 0.2 / 0.15 / 0.95.
        // Max climb speed = jumpSpeed * this multiplier, so it compounds with the jump-speed cap above.
        public const float SuppressedMaxAscentMultiplier = 2.3f;
        public const float SuppressedMaxAscentMultiplierSeath = 2.6f;
        public const float SuppressedAscentWhenRising = 0.15f;
        public const float SuppressedAscentWhenRisingSeath = 0.17f;
        public const float SuppressedConstantAscend = 0.135f;
        public const float SuppressedConstantAscendSeath = 0.145f;
        public const float SuppressedAscentWhenFalling = 0.85f;
        public const float SuppressedAscentWhenFallingSeath = 0.9f;
	
	// global speed caps are currently disabled
        public const float GlobalRunSpeedCap = 10.00f;
        public const float GlobalFlightSpeedCap = 10.00f;
        public const float GlobalFlightAccelerationCap = 0.32f;

        public static bool Enabled(Player player)
        {
            if (player == null || Main.gameMenu) return false;
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
