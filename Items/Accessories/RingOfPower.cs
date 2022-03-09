using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
 
namespace tsorcRevamp.Items.Accessories {
    public class RingOfPower : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("A great ring of power gifted to men." +
								"\nCasts darkness and battle potion effects on wearer." + 
								"\n+25% critical chance. ");
        }
 
        public override void SetDefaults() {
            item.width = 28;
            item.height = 38;
            item.accessory = true;
            item.value = PriceByRarity.Red_10;
            item.rare = ItemRarityID.Red;
        }
 
        public override void UpdateEquip(Player player) {
			player.noKnockback = true;
			player.AddBuff(22, 500, false); //Darkness
			player.AddBuff(13, 500, false); //Battle Potion
			player.rangedCrit += 25;
			player.meleeCrit += 25;
			player.magicCrit += 25;
        }
 
    }
}

