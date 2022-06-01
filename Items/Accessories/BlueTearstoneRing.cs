using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class BlueTearstoneRing : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("The rare gem called tearstone has the uncanny ability to sense imminent death." +
                                "\nThis blue tearstone from Catarina boosts the defense of its wearer by 50 when in danger." +
                                "\nWhen the ring is active, melee damage is reduced by 200%, making it a ring ideal for mages" +
                                "\nand other ranged classes. However, the ring provides 8 defense under normal circumstances.");
        }

        public override void SetDefaults() {
            Item.width = 24;
            Item.height = 24;
            Item.accessory = true;
            Item.value = PriceByRarity.Red_10;
            Item.rare = ItemRarityID.Red;
        }


        public override void UpdateEquip(Player player) {
            if (player.statLife <= 80) {
                player.statDefense += 50;
                player.GetDamage(DamageClass.Melee) -= 2f;
                player.GetCritChance(DamageClass.Melee) = -50;
            }
            else {
                player.statDefense += 8;
            }
        }

    }
}
