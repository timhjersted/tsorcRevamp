using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class ReforgedOldLongsword : ModItem {

        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/OldLongsword";
        public override void SetDefaults() {
            Item.damage = 12;
            Item.width = 44;
            Item.height = 44;
            Item.knockBack = 4;
            Item.maxStack = 1;
            Item.DamageType = DamageClass.Melee;
            Item.scale = .9f;
            Item.useAnimation = 19;
            Item.rare = ItemRarityID.White;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 21;
            Item.value = 7000;
        }
        public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(Mod.GetItem("OldLongsword"));
            recipe.AddTile(Mod.GetTile("SweatyCyclopsForge"));
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
