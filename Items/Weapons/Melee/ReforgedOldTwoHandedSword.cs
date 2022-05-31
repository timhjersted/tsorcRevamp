using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class ReforgedOldTwoHandedSword : ModItem {
        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/OldTwoHandedSword";
        public override void SetDefaults() {
            Item.damage = 25; //it's post EoC
            Item.width = 50;
            Item.height = 50;
            Item.knockBack = 5;
            Item.maxStack = 1;
            Item.melee = true;
            Item.scale = 1f;
            Item.useAnimation = 30;
            Item.rare = ItemRarityID.White;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 30;
            Item.value = 15000;
        }
        public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(Mod.GetItem("OldTwoHandedSword"));
            recipe.AddTile(Mod.GetTile("SweatyCyclopsForge"));
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
