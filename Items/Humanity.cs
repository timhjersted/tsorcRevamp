using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items {
    class Humanity : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Permanently increases maximum life by 20" + 
                               "\nUsed to craft the summoning stone of Blight.");
        }

        public override void SetDefaults() {
            item.width = 16;
            item.height = 16;
            item.rare = ItemRarityID.Green;
            item.value = 75000;
            item.useAnimation = 15;
            item.useTime = 15;
            item.useStyle = ItemUseStyleID.HoldingUp;
            item.UseSound = SoundID.Item4;
            item.maxStack = 500;
            item.consumable = true;
        }

        public override bool CanUseItem(Player player) {
            return (player.statLifeMax < 500);
        }

        public override bool UseItem(Player player) {

            player.statLifeMax += 20;
            player.statLife += 20; //BOTC can still heal from this, as you can in DS
            if (Main.myPlayer == player.whoAmI) {
                player.HealEffect(20, true);
            }
            if (player.statLifeMax > 500) {
                player.statLife = player.statLifeMax2;
                player.statLifeMax = 500;
            }
            return true;
        }
    }
}
