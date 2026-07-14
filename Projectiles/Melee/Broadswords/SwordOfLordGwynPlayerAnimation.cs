using Microsoft.Xna.Framework;
using Terraria;
using tsorcRevamp.Items.Weapons.Melee.Broadswords.BroadswordRework.Utilities;

namespace tsorcRevamp.Projectiles.Melee.Broadswords
{
    internal static class SwordOfLordGwynPlayerAnimation
    {
        private static readonly PlayerFrames[] UseFrames =
        {
            PlayerFrames.Use1,
            PlayerFrames.Use2,
            PlayerFrames.Use3,
            PlayerFrames.Use4
        };

        private static readonly Vector2[] HandOffsets =
        {
            new Vector2(-8f, -9f),
            new Vector2(4f, -8f),
            new Vector2(4f, 2f),
            new Vector2(4f, 7f)
        };

        public static Vector2 SetUseFrame(Player player, int frame)
        {
            frame = System.Math.Clamp(frame, 0, UseFrames.Length - 1);
            player.bodyFrame = UseFrames[frame].ToRectangle();
            player.itemLocation = GetHandPosition(player, frame);
            return player.itemLocation;
        }

        public static Vector2 GetHandPosition(Player player, int frame)
        {
            frame = System.Math.Clamp(frame, 0, HandOffsets.Length - 1);
            Vector2 offset = HandOffsets[frame];
            return player.Center + new Vector2(offset.X * player.direction, offset.Y);
        }
    }
}
