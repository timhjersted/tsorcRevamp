using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items {
    class DyingWindShard : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("A shard of the fading Crystal of Wind.\n" + "Used to craft the crystal that summons Chaos.");
        }

        public override void SetDefaults() {
            item.width = 10;
            item.height = 16;
            item.rare = ItemRarityID.Orange;
            item.value = 1000;
            item.maxStack = 100;
            item.rare = ItemRarityID.LightRed;
        }
    }
}
