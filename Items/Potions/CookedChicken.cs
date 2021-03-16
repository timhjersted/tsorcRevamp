using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions {
    class CookedChicken : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Heals 125 HP and can be used at any time. Amazing!");
        }

        public override void SetDefaults() {
            item.stack = 1;
            item.consumable = true;
            item.healLife = 125;
            item.useAnimation = 17;
            item.UseSound= SoundID.Item2;
            item.useStyle = ItemUseStyleID.EatingUsing;
            item.useTime = 17;
            item.height = 16;
            item.maxStack = 100;
            item.scale = 1;
            item.value = 2;
            item.width = 20;
        }

        public override void GetHealLife(Player player, bool quickHeal, ref int healValue) {
            healValue = 125;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("DeadChicken"), 1);
            recipe.AddTile(TileID.CookingPots);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
