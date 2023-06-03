using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
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
                tooltips.Insert(3, new TooltipLine(Mod, "Alcoholic", Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.AleSake")));
            }
        }
    }
}
