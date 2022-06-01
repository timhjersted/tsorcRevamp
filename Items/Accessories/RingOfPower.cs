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
            Item.width = 28;
            Item.height = 38;
            Item.accessory = true;
            Item.value = PriceByRarity.Red_10;
            Item.rare = ItemRarityID.Red;
        }
 
        public override void UpdateEquip(Player player) {
			player.noKnockback = true;
			player.AddBuff(22, 500, false); //Darkness
			player.AddBuff(13, 500, false); //Battle Potion
			player.GetCritChance(DamageClass.Ranged) += 25;
			player.GetCritChance(DamageClass.Melee) += 25;
			player.GetCritChance(DamageClass.Magic) += 25;
        }
 
    }
}

