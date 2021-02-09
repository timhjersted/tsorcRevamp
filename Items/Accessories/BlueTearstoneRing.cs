using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class BlueTearstoneRing : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("The rare gem called tearstone has the uncanny ability to sense imminent death." +
                                "\nThis blue tearstone from Catarina boosts the defense of its wearer by 50 when in danger." +
                                "\nWhen the ring is active, melee damage is reduced by 200%, making it a ring ideal for mages" +
                                "\nand other ranged classes. However, the ring provides 6 defense under normal circumstances.");
        }

        public override void SetDefaults() {
            item.accessory = true;
            item.value = 270000;
            item.rare = ItemRarityID.Pink;
        }


        public override void UpdateEquip(Player player) {
            if (player.statLife <= 80) {
                player.statDefense += 50;
                player.meleeDamage -= 2f;
                player.meleeCrit = -50;
            }
            else {
                player.statDefense += 6;
            }
        }

    }
}
