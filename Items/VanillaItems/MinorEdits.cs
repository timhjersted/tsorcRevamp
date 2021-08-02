using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.VanillaItems {
    class MinorEdits : GlobalItem {

        static readonly bool LegacyMode = ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;
        static readonly bool AdventureMode = ModContent.GetInstance<tsorcRevampConfig>().AdventureMode;
        public override void SetDefaults(Item item) {
            if (item.type == ItemID.StaffofRegrowth && AdventureMode && !LegacyMode) {
                item.createTile = -1; //block placing grass, thus allowing use
            }
            if (item.type == ItemID.DivingHelmet) {
                item.accessory = true;
            }
        }

        public override bool CanUseItem(Item item, Player player)
        {
            if (item.type == ItemID.DirtRod && AdventureMode)
            {
                return false;
            }
            return true;
        }

    }
}
