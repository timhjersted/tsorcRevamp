using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions {
    class GreaterRestorationPotion : ModItem {
        public override void SetDefaults() {
            item.width = 20;
            item.height = 26;
            item.useStyle = ItemUseStyleID.EatingUsing;
            item.useAnimation = 15;
            item.useTime = 15;
            item.maxStack = 100;
            item.healLife = 300;
            item.healMana = 300;
            item.potion = true;
            item.rare = ItemRarityID.Green;
            item.consumable = true;
            item.value = 10000;
            item.UseSound = SoundID.Item3;
        }

        public override void GetHealLife(Player player, bool quickHeal, ref int healValue) {
            healValue = 300;
        }
        public override void GetHealMana(Player player, bool quickHeal, ref int healValue) {
            healValue = 300;
        }
    }
}
