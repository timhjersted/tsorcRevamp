using Humanizer;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.VanillaItems
{
    class TitanPotion : GlobalItem
    {
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.type == ItemID.TitanPotion)
            {
                tooltips.Insert(3, new TooltipLine(Mod, "SharpEyes", Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.TitanPotion").FormatWith(tsorcRevampPlayer.TitanMeleeSize)));
            }
        }
    }
}
