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
        }

        public override bool CanUseItem(Item item, Player player)
        {
            if (item.type == ItemID.DirtRod && ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                return false;
            }
            return true;
        }

    }
}
