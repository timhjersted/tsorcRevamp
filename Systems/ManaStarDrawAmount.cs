using Terraria;
using Terraria.GameContent.UI.ResourceSets;

namespace tsorcRevamp.Systems;

class ManaStarDrawAmount
{
    internal static void ApplyManaStarAmount()
    {
        On_PlayerStatsSnapshot.ctor += CustomManaStarAmount;
    }

    private static void CustomManaStarAmount(On_PlayerStatsSnapshot.orig_ctor orig, ref PlayerStatsSnapshot self, Player player)
    {
        orig(ref self, player);
        if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
        {
            //self.AmountOfManaStars = player.statManaMax2 / 100;
        }
    }
}