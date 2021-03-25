using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class ForgottenTigerFangs : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Long and extremely sharp fighting claws.");
        }

        public override void SetDefaults() {
            item.autoReuse = true;
            item.useTurn = true;
            item.rare = ItemRarityID.Pink;
            item.damage = 150;
            item.height = 12;
            item.knockBack = 3;
            item.melee = true;
            item.useAnimation = 8;
            item.useTime = 8;
            item.UseSound = SoundID.Item1;
            item.value = 19500000;
            item.width = 18;
        }
        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("ForgottenKaiserKnuckles"), 1);
            recipe.AddIngredient(mod.GetItem("GuardianSoul"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 80000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
