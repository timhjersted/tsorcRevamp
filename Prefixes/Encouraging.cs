using System.Collections.Generic;
using Humanizer;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Prefixes;

public class Encouraging : ModPrefix
{
    public const float SummonTagDmg = 1f;
    public const float TagStrength = 3f;
    
    public override PrefixCategory Category => PrefixCategory.Accessory;

    public override float RollChance(Item item)
    {
        return 1f;
    }

    public override bool CanRoll(Item item)
    {
        return true;
    }

    public override void ApplyAccessoryEffects(Player player)
    {
        var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
        modPlayer.EncouragingSummonTagDmg += SummonTagDmg;
        modPlayer.SummonTagStrength += TagStrength / 100f;
    }

    public override IEnumerable<TooltipLine> GetTooltipLines(Item item)
    {
        yield return new TooltipLine(Mod, "Encouraging", LangUtils.GetTextValue("CommonItemTooltip.AccessoryPrefixes.Encouraging", SummonTagDmg, TagStrength))
        {
            IsModifier = true
        };
    }
}