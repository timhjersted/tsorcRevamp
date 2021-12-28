using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.VanillaItems {
    class MinorEdits : GlobalItem {

        public override void SetDefaults(Item item) {
            if (item.type == ItemID.StaffofRegrowth && ModContent.GetInstance<tsorcRevampConfig>().AdventureMode && !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) {
                item.createTile = -1; //block placing grass, thus allowing use
            }
            if (item.type == ItemID.DivingHelmet) {
                item.accessory = true;
            }
            if (item.type == ItemID.NightsEdge)
            {
                item.autoReuse = true;
                item.useAnimation = 21;
                item.useTime = 21;
            }
            if (item.type == ItemID.FieryGreatsword)
            {
                item.useAnimation = 30;
                item.useTime = 30;
                item.damage = 40;
            }
            if (item.type == ItemID.ChlorophyteBullet) {
                //chlorophyte bullets are fucking stupid, dont @ me
                item.damage = 6; //from 10
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
