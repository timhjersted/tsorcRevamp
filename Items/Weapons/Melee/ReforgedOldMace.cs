using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class ReforgedOldMace : ModItem {
        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/OldMace";
        public override void SetDefaults() {
            Item.damage = 15; //buffed
            Item.width = 36;
            Item.height = 36;
            Item.knockBack = 6.5f;
            Item.maxStack = 1;
            Item.DamageType = DamageClass.Melee;
            Item.scale = .9f;
            Item.useAnimation = 22;
            Item.rare = ItemRarityID.White;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 21;
            Item.value = 8000;
        }
        public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(Mod.GetItem("OldMace"));
            recipe.AddTile(Mod.GetTile("SweatyCyclopsForge"));
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
