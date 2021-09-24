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
            item.width = 24;
            item.height = 24;
            item.accessory = true;
            item.value = PriceByRarity.Red_10;
            item.rare = ItemRarityID.Red; //water fiend (shm) drop
        }

        public override void UpdateEquip(Player player) {
            //works in normal gravity and gravity potion
            if (((player.gravDir == 1f) && (player.velocity.Y > 0)) || ((player.gravDir == -1f) && (player.velocity.Y < 0))) {
                player.meleeDamage *= 2;
            }
        }

    }
}
