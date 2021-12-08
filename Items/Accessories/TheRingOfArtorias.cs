using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
 
namespace tsorcRevamp.Items.Accessories {
    public class TheRingOfArtorias : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("The great Ring of Artorias." +
								"\nOnce held strength too great for this world to bear." +
                                "\nPerhaps someday it will find a new purpose?" +
                                "\n[c/888888:(Note: This item has been temporarily disabled pending a rework.)]");
        }
 
        public override void SetDefaults() {
            item.width = 28;
            item.height = 38;
            //item.accessory = true;
            item.value = PriceByRarity.Red_10;
            item.rare = ItemRarityID.Red;
        }
 
        public override void UpdateEquip(Player player) {
			//player.noKnockback = true;
            //player.allDamage *= 2;
        }
 
    }
}

