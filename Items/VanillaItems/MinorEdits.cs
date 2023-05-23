using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.VanillaItems
{
    class MinorEdits : GlobalItem
    {

        public override void SetDefaults(Item item)
        {
            if (item.type == ItemID.StaffofRegrowth && ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                item.createTile = -1; //block placing grass, thus allowing use
            }
            if (item.type == ItemID.DivingHelmet)
            {
                item.accessory = true;
            }
            if (item.type == ItemID.MoltenPickaxe)
            {
                item.useTime = 15;
                item.useAnimation = 15;
            }
        }
        public override void GrabRange(Item item, Player player, ref int grabRange)
        {
            if (item.type == ItemID.ManaCloakStar)
            {
                if (player.manaMagnet)
                {
                    grabRange += 100;
                }
            }
        }

        public override bool CanUseItem(Item item, Player player)
        {
            if ((item.type == ItemID.DirtRod || item.type == ItemID.BoneWand) && ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                return false;
            }
            return true;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.type == ItemID.MechanicalEye)
            {
                int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
                if (ttindex != -1)
                {
                    tooltips.RemoveAt(ttindex);
                    tooltips.Insert(ttindex, new TooltipLine(Mod, "RealBossName", "Summons The Triad\nItem is non-consumable."));
                }
            }
        }
    }
}
