using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items {
    class BloodredMossClump : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Heals 20 HP, with only 5 seconds of potion sickness." +
                                "\nRemoves bleeding and poisoned." +
                                "\nA supply of these may be essential for exploring some areas." +
                                //"\nIf you find yourself losing life quickly, check your buffs to see if you've been poisoned." + //Not really necessary to point this out
                                "\nThis plant will save your life." +
                                "\nCan still be used to remove poison and bleeding while under the" +
                                "\neffect of potion sickness. However, it will not heal any HP.");
        }
        public override void SetDefaults() {
            item.width = 16;
            item.height = 25;
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
                
                if ((buffType == BuffID.Bleeding) || (buffType == BuffID.Poisoned)) {
                    player.buffTime[buffIndex] = 0;
                }
                buffIndex++;
            }

            if (!player.HasBuff(BuffID.PotionSickness))
            {
                player.statLife += 20;
                if (player.statLife > player.statLifeMax2)
                {
                    player.statLife = player.statLifeMax2;
                }
                player.HealEffect(20, true);
                player.AddBuff(BuffID.PotionSickness, 300);
            }
            return true;
        }
    }
}
