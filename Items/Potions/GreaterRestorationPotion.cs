using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions {
    class GreaterRestorationPotion : ModItem {
        public override void SetDefaults() {
            Item.width = 20;
            Item.height = 26;
            Item.useStyle = ItemUseStyleID.EatFood;
            Item.useAnimation = 15;
            Item.useTime = 15;
            Item.maxStack = 100;
            Item.healLife = 300;
            Item.healMana = 300;
            Item.potion = true;
            Item.rare = ItemRarityID.Green;
            Item.consumable = true;
            Item.value = 10000;
            Item.UseSound = SoundID.Item3;
        }

        public override void GetHealLife(Player player, bool quickHeal, ref int healValue) {
            healValue = 300;
        }
        public override void GetHealMana(Player player, bool quickHeal, ref int healValue) {
            healValue = 300;
        }
    }
}
