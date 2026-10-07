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

        // Harness fallback flight times (frames) when no wing is slotted.
        // Tier 1 matches Angel Wings' 100 (its recipe wing) so crafting the harness never loses flight time.
        public const int HarnessFallbackFlightTimeTier1 = 100; // ~1.7s
        public const int HarnessFallbackFlightTimeTier2 = 210; // 3.5s

        public const int SupersonicWings2FlightTime = 600;
        public const int WingsOfSeathFlightTime = 300; // 5.0s in Souls Mode (down from 1200 / infinite)
        public const int WingsOfSeathSuppressedFlightTime = 180;

        public const float SupersonicWingsFlightSpeed = 6.25f;
        public const float SupersonicWingsFlightAcceleration = 0.12f;

        public const float SupersonicWings2FlightSpeed = 6.55f;
        public const float SupersonicWings2FlightAcceleration = 0.15f;

        // Seath's minimum AIR speed (px/tick): PostUpdateRunSpeeds takes the higher of this and the Supersonic
        // formula while airborne. 8.0 = Fishron / Empress wings. Hover keeps its +0.5 lead.
        public const float WingsOfSeathFlightSpeed = 8.0f;
        public const float WingsOfSeathFlightAcceleration = 0.2f;
        public const float WingsOfSeathHoverFlightSpeed = 8.5f;
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

        // The mobility rework now applies in every mode; the config toggle was removed. The !Enabled() branches
        // in the four mobility items are the OLD Classic stats, kept unreachable on purpose so the toggle can be
        // restored by reverting this check. Classic's one difference (wing fall immunity) gates on SoulsMode directly.
        public static bool Enabled(Player player)
        {
            if (player == null || Main.gameMenu)
            {
                return false;
            }

            return true;
        }

        // Supersonic sprint speed (px/tick). Only boostPercent of the player's total moveSpeed applies:
        // boostPercent 0.25 at moveSpeed 1.5 gives baseSpeed * 1.125. Used by PostUpdateRunSpeeds and the wing tooltip.
        public static float SupersonicRunSpeed(float baseSpeed, float boostPercent, float moveSpeed)
        {
            float scaledMoveSpeed = (moveSpeed * boostPercent) + (1 - boostPercent);
            return baseSpeed * scaledMoveSpeed;
        }
    }
}
