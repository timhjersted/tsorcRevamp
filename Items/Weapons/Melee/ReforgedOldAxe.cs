using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class ReforgedOldAxe : ModItem {
        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/OldAxe";
        public override void SetDefaults() {
            item.damage = 11;
            item.width = 36;
            item.height = 30;
            item.knockBack = 6;
            item.maxStack = 1;
            item.melee = true;
            item.scale = 1;
            item.useAnimation = 20;
            item.rare = ItemRarityID.White;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 20;
            item.value = 9000;
        }
        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("OldAxe"));
            recipe.AddTile(mod.GetTile("SweatyCyclopsForge"));
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
