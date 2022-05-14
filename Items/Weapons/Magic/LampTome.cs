using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    class LampTome : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("A lost tome known to cure blindness.");
        }
        public override void SetDefaults() {
            item.height = 10;
            item.knockBack = 4;
            item.rare = ItemRarityID.Orange; //yes, even though it's hardmode
            item.magic = true;
            item.noMelee = true;
            item.mana = 5;
            item.UseSound = SoundID.Item21;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = 10;
            item.useAnimation = 10;
            item.value = PriceByRarity.Orange_3;
            item.width = 34;
        }

        public override bool UseItem(Player player) {
            int buffIndex = 0;

            foreach (int buffType in player.buffType) {

                if (buffType == BuffID.Darkness || buffType == BuffID.Blackout || buffType == BuffID.Obstructed) {
                    player.DelBuff(buffIndex);
                }
                buffIndex++;
            }
            return true;
        }
    }
}
