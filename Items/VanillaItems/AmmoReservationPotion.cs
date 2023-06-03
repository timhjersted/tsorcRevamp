using Humanizer;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.VanillaItems
{
    class AmmoReservationPotion : GlobalItem
    {

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.type == ItemID.AmmoReservationPotion)
            {
                tooltips.Insert(4, new TooltipLine(Mod, "SharpEyes", Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.AmmoReservationPotion").FormatWith(tsorcRevampPlayer.AmmoReservationRangedCritDamage)));
            }
        }
    }
}
