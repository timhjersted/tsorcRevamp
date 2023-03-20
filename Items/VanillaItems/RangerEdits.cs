using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.VanillaItems
{
    class RangerEdits : GlobalItem
    {
        public override void SetDefaults(Item item)
        {
            if (item.type == ItemID.ChlorophyteBullet)
            {
                //chlorophyte bullets are fucking stupid, dont @ me
                item.damage = 1; //from 10
            }

            //Lunar items
            if (item.type == ItemID.Phantasm)
            {
                item.damage = 35;
            }
        }
        public override void ModifyWeaponDamage(Item item, Player player, ref StatModifier damage)
        {
            if (item.type == ItemID.CrystalDart && !Main.hardMode)
            {
                damage *= 0.1f;
            }
        }
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.type == ItemID.CrystalDart && !Main.hardMode)
            {
                int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
                if (ttindex != -1)
                {
                    tooltips.Insert(ttindex, new TooltipLine(Mod, "TempNerf", "Hidden strength sealed by a demon of the underworld"));
                }
            }
        }
    }
}