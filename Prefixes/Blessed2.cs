using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Prefixes;

public class Blessed2 : ModPrefix
{
    public const int Defense = 1;
    
    public override PrefixCategory Category => PrefixCategory.Accessory;

    public override float RollChance(Item item)
    {
        return 0f;
    }

    public override bool CanRoll(Item item)
    {
        return true;
    }

    public override void ApplyAccessoryEffects(Player player)
    {
        player.statDefense += Defense;
    }

    public override IEnumerable<TooltipLine> GetTooltipLines(Item item)
    {
        yield return new TooltipLine(Mod, "Blessed2", LangUtils.GetTextValue("CommonItemTooltip.AccessoryPrefixes.Blessed", Defense))
        {
            IsModifier = true
        };
    }
}