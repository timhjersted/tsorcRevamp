using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
 
namespace tsorcRevamp.Items.Accessories {
    public class RingOfPower : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("A great ring of power gifted to men." +
								"\nCasts darkness and battle potion effects on wearer." + 
								"\nNo knockback, +50 percent critical chance. ");
        }
 
        public override void SetDefaults() {
            item.width = 28;
            item.height = 38;
            item.accessory = true;
            item.value = 270000;
            item.rare = ItemRarityID.Orange;
        }
 
        public override void UpdateEquip(Player player) {
			player.noKnockback = true;
			player.AddBuff(22, 500, false); //Darkness
			player.AddBuff(13, 500, false); //Battle Potion
			player.rangedCrit += 50;
			player.meleeCrit += 50;
			player.magicCrit += 50;
        }
 
    }
}

