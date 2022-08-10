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
            if (item.type == ItemID.FieryGreatsword)
            {
                item.useAnimation = 30;
                item.useTime = 30;
                item.damage = 40;
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

    }
}
