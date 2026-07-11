using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    public class SwordOfLordGwynTooltips : GlobalItem
    {
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.type != ModContent.ItemType<SwordOfLordGwyn>())
            {
                return;
            }

            tooltips.Add(new TooltipLine(Mod, "GwynHighTap", "Tap left click with the cursor at or above your feet to slash with the First Flame"));
            tooltips.Add(new TooltipLine(Mod, "GwynHighHold", $"Hold left click with the cursor at or above your feet to spend {SwordOfLordGwyn.SpearManaCost} mana on a Sunlight Spear"));
            tooltips.Add(new TooltipLine(Mod, "GwynLowTap", $"Tap left click with the cursor below your feet to spend {SwordOfLordGwyn.DashManaCost} mana on a rising flame dash"));
            tooltips.Add(new TooltipLine(Mod, "GwynLowHold", $"Hold left click with the cursor below your feet to spend {SwordOfLordGwyn.NovaManaCost} mana on Cinder Nova"));
        }
    }
}
