using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class DragonHorn : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Horn inhabited by the spirit of a dragon." +
                                "\n200% melee damage if falling.");
        }

        public override void SetDefaults() {
            Item.width = 24;
            Item.height = 24;
            Item.accessory = true;
            Item.value = PriceByRarity.Red_10;
            Item.rare = ItemRarityID.Red; //water fiend (shm) drop
        }

        public override void UpdateEquip(Player player) {
            player.GetModPlayer<tsorcRevampPlayer>().DragoonHorn = true;
        }

    }
}
