using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.VanillaItems
{
    class Alcohol : GlobalItem
    {

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.type == ItemID.Ale
                || item.type == ItemID.Sake
                )
            {
                tooltips.Insert(3, new TooltipLine(Mod, "", "Minor improvements to whip stats"));
            }
        }
    }
}
