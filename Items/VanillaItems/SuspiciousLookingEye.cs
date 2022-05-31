using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.VanillaItems {
    class SuspiciousLookingEye : GlobalItem {

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
            if (item.type == ItemID.SuspiciousLookingEye) {
                int ttindex = tooltips.FindLastIndex(t => t.mod == "Terraria");
                if (ttindex != -1) {
                    tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "EoC1", "Item is not consumed so that you can retry the fight on defeat"));
                    tooltips.Insert(ttindex + 2, new TooltipLine(Mod, "EoC2", "The Eye of Cthulhu can only be summoned during the night."));
                    tooltips.Insert(ttindex + 3, new TooltipLine(Mod, "EoC3", "A silver watch may aid in the passing of time..."));
                }
            }
        }
    }
}
