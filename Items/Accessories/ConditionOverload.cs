using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class ConditionOverload : ModItem {

        public override bool Autoload(ref string name) => !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Enemies take exponentially increasing" +
                                "\nbonus damage for every debuff affecting them." + 
                                "\n\"H Deimos CO farm 3/4 LF despoil\"");
        }

        public override void SetDefaults() {
            item.width = 24;
            item.height = 24;
            item.accessory = true;
            item.value = 270000;
            item.rare = ItemRarityID.Pink;
        }

        public override void UpdateEquip(Player player) {
            player.GetModPlayer<tsorcRevampPlayer>().ConditionOverload = true;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Fireblossom, 2);
            recipe.AddIngredient(ItemID.CursedFlame, 2);
            recipe.AddIngredient(ItemID.Stinger, 5);

        }
    }
}
