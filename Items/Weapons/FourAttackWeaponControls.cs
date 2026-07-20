using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons
{
    internal static class FourAttackWeaponControls
    {
        // Three Terraria tiles below the bottom of the player.
        public const float LowAttackCursorDepth = 48f;

        public static bool AdvancedControlsEnabled =>
            ModContent.GetInstance<tsorcRevampControlsConfig>().AdvancedFourAttackWeaponControls;

        public static bool UsesAltFunction => !AdvancedControlsEnabled;

        public static int GetAttackMode(Player player)
        {
            if (!AdvancedControlsEnabled)
            {
                return player.altFunctionUse == 2 ? 1 : 0;
            }

            return Main.MouseWorld.Y > player.Bottom.Y + LowAttackCursorDepth ? 1 : 0;
        }
    }
}
