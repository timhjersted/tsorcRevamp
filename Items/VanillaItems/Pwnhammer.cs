using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.VanillaItems
{
    class Pwnhammer : GlobalItem
    {

        public override void SetDefaults(Item item)
        {
            if (item.type == ItemID.Pwnhammer)
            {
                item.pick = 100;
            }
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.type == ItemID.Pwnhammer)
            {
                int ttindex = tooltips.FindLastIndex(t => t.mod == "Terraria");
                if (ttindex != -1)
                {
                    tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "EoC1", "The Pwnhammer has a secret form, possessing the mining power of the Molten Pickaxe."));
                    tooltips.Insert(ttindex + 2, new TooltipLine(Mod, "EoC2", "Use the Pwnhammer to open the Cobalt Gate to the East of Obsidian's Volcano"));
                    tooltips.Insert(ttindex + 3, new TooltipLine(Mod, "EoC3", "The Pwnhammer can also be crafted into a"));
                    tooltips.Insert(ttindex + 4, new TooltipLine(Mod, "EoC3", "more powerful hammer with enough Dark Souls"));
                }
            }
        }
    }
}
