using System.Collections.Generic;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items;

class DyingWindShard : ModItem
{
    public override void SetStaticDefaults()
    {
        Tooltip.SetDefault("A shard of the fading Crystal of Wind.");
    }

    public override void SetDefaults()
    {
        Item.width = 10;
        Item.height = 16;
        Item.rare = ItemRarityID.Orange;
        Item.value = 1000;
        Item.maxStack = 100;
        Item.rare = ItemRarityID.LightRed;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        if (!ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
        {
            tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "chaos", "Used to craft the crystal that summons Chaos."));
        }
    }
}
