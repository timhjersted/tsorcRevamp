using Terraria.ModLoader;
using tsorcRevamp.Prefixes;

namespace tsorcRevamp.Systems.VanillaPrefixEdits;

public class AccessoryPrefixEditsPlayer : ModPlayer
{
    public int WardingAccessories;
    public int LuckyAccessories;
    public int BlessedAccessories;
    public override void ResetEffects()
    {
        WardingAccessories = 0;
        LuckyAccessories = 0;
        BlessedAccessories = 0;
    }

    public override void PostUpdateEquips()
    {
        Player.moveSpeed -= WardingAccessories * WardingAccessories * AccessoryPrefixItem.WardingBadMovespeedScale / 100f;
    }

    public override void ModifyLuck(ref float luck)
    {
        luck += LuckyAccessories * AccessoryPrefixItem.LuckyPercentLuck / 100f;
        luck += BlessedAccessories * Blessed2.Luck / 100f;
    }
}