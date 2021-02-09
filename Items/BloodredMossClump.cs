using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items {
    class BloodredMossClump : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Heals 20 HP, with no potion sickness." +
                                "\nRemoves bleeding and poisoned." +
                                "\nA supply of these may be essential for exploring some areas." +
                                "\nIf you find yourself losing life quickly, check your buffs to see if you've been poisoned." +
                                "\nThis plant will save your life."); 
        }
        public override void SetDefaults() {
            item.width = 16;
            item.height = 25;
            item.healLife = 20;
            item.consumable = true;
            item.maxStack = 360;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useAnimation = 35;
            item.useTime = 35;
            item.UseSound = SoundID.Item21;
            item.value = 2000;
            item.rare = ItemRarityID.Orange;
        }

        public override bool UseItem(Player player) {
            int buffIndex = 0;

            foreach (int buffType in player.buffType) {
                buffIndex++;
                if (buffType == BuffID.Bleeding) {
                    player.buffTime[buffIndex] = 0;
                }

                if (buffType == BuffID.Poisoned) {
                    player.buffTime[buffIndex] = 0;
                }
            }
            return true;
        }
    }
}
