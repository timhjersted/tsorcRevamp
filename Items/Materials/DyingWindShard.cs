using System.Collections.Generic;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.Materials
{
    class DyingWindShard : ModItem
    {
        public override void SetStaticDefaults()
        {
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
                tooltips.Add(new TooltipLine(ModContent.GetInstance<tsorcRevamp>(), "Chaos", LaUtils.GetTextValue("Items.DyingWindShard.Chaos")));
            }
        }
    }
}
