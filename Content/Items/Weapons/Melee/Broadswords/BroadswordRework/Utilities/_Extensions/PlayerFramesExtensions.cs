using Microsoft.Xna.Framework;

namespace tsorcRevamp.Content.Items.Weapons.Melee.Broadswords.BroadswordRework.Utilities._Extensions;

public static class PlayerFramesExtensions
{
    private const int PlayerSheetWidth = 40;
    private const int PlayerSheetHeight = 56;

    public static Rectangle ToRectangle(this PlayerFrames frame)
    {
        return new Rectangle(0, (int)frame * PlayerSheetHeight, PlayerSheetWidth, PlayerSheetHeight);
    }
}
