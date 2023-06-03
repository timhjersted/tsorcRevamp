using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.VanillaItems
{
    class SuspiciousLookingEye : GlobalItem
    {

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.type == ItemID.SuspiciousLookingEye)
            {
                int ttindex = tooltips.FindLastIndex(t => t.Mod == "Terraria");
                if (ttindex != -1)
                {
                    tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "EoC1", Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.SuspiciousLookingEye")));
                }
            }
        }
    }
}
