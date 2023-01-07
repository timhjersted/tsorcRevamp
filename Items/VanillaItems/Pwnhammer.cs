using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.VanillaItems
{
    class Pwnhammer : GlobalItem
    {

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.type == ItemID.Pwnhammer)
            {
                int ttindex = tooltips.FindLastIndex(t => t.Mod == "Terraria");
                if (ttindex != -1)
                {
                    tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "EoC3", "This can be crafted into a"));
                    tooltips.Insert(ttindex + 2, new TooltipLine(Mod, "EoC3", "powerful hammer with enough Dark Souls"));
                }
            }
        }
    }
}
