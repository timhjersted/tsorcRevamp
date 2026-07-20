using Microsoft.Xna.Framework;
using Terraria;

namespace tsorcRevamp.Items.Weapons.Summon
{
    internal static class BellSwingAnimation
    {
        // These bell textures place their small grip at the upper-right, opposite the lower-left
        // grip expected by Terraria's Swing use style. Rotate around the visible grip instead.
        const float GripInset = 2f;

        public static void Apply(Item item, Player player, Rectangle heldItemFrame)
        {
            player.itemRotation += MathHelper.Pi;

            Vector2 gripFromVanillaOrigin = new Vector2(
                player.direction * (heldItemFrame.Width - GripInset),
                player.gravDir * (GripInset - heldItemFrame.Height));

            player.itemLocation -= gripFromVanillaOrigin.RotatedBy(player.itemRotation)
                * player.GetAdjustedItemScale(item);
        }
    }
}
