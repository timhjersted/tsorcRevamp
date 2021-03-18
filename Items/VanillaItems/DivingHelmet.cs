using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.VanillaItems {
    class DivingHelmet : GlobalItem {
        public override void SetDefaults(Item item) {
            if (item.type == ItemID.DivingHelmet) {
                item.accessory = true;
            }
        }
    }
}
