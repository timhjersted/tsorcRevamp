using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
 
namespace tsorcRevamp.Items.Accessories {
    public class TheRingOfArtorias : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("The great Ring of Artorias." +
								"\nOnce held strength too great for this world to bear." +
                                "\n[c/888888:It now grants immunity to Powerful Curse Buildup and Freezing.]");
        }
 
        public override void SetDefaults() {
            item.width = 28;
            item.height = 38;
            item.accessory = true;
            item.value = PriceByRarity.Red_10;
            item.rare = ItemRarityID.Red;
        }
 
        public override void UpdateEquip(Player player) {
            player.buffImmune[ModContent.BuffType<Buffs.PowerfulCurseBuildup>()] = true;
            //player.statDefense += 10;
            player.buffImmune[BuffID.Frozen] = true;
            //player.allDamage *= 2;
        }
 
    }
}

