using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
 
namespace tsorcRevamp.Items.Accessories {
    public class TheRingOfArtorias : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("The great Ring of Artorias." +
								"\nAll weapons do 2x damage.");
        }
 
        public override void SetDefaults() {
            item.width = 28;
            item.height = 38;
            item.accessory = true;
            item.value = 1000000;
            item.rare = ItemRarityID.Orange;
        }
 
        public override void UpdateEquip(Player player) {
			player.noKnockback = true;
			player.meleeDamage *= 2;
			player.magicDamage *= 2;
			player.rangedDamage *= 2;
        }
 
    }
}

