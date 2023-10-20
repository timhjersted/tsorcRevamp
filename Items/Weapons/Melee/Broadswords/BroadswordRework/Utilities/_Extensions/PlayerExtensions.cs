using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using tsorcRevamp.Items.Weapons.Melee.Broadswords.BroadswordRework.Common.Movement;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords.BroadswordRework.Utilities;

public static partial class PlayerExtensions
{
    // Essentials

    public static bool IsLocal(this Player player)
        => player.whoAmI == Main.myPlayer;

    public static Vector2 KeyDirection(this Player player)
    {
        Vector2 result;

        result.X = (player.controlRight ? 1f : 0f) - (player.controlLeft ? 1f : 0f);
        result.Y = (player.controlDown ? 1f : 0f) - (player.controlUp ? 1f : 0f);

        return result;
    }

    public static Vector2 LookDirection(this Player player)
        => (player.GetModPlayer<BroadswordReworkPlayer>().MouseWorld - player.Center).SafeNormalize(Vector2.UnitY);

    // (De)buffs

    public static void RemoveBuffsOfType(this Player player, int type)
    {
        int buffIndex = player.FindBuffIndex(type);

        if (buffIndex >= 0)
        {
            player.DelBuff(buffIndex);
        }
    }

    public static IEnumerable<(Item item, int index)> EnumerateAccessories(this Player player)
    {
        //TODO: Might need to update this in the future.
        for (int i = 3; i < 10; i++)
        {
            var item = player.armor[i];

            if (item != null && item.active)
            {
                yield return (item, i);
            }
        }
    }


}
