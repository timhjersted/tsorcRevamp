using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class ReforgedOldDoubleAxe : ModItem {
        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/OldDoubleAxe";
        public override void SetDefaults() {
            Item.damage = 16;
            Item.width = 36;
            Item.height = 36;
            Item.knockBack = 5;
            Item.maxStack = 1;
            Item.DamageType = DamageClass.Melee;
            Item.scale = 1.1f;
            Item.autoReuse = true;
            Item.useAnimation = 26;
            Item.rare = ItemRarityID.White;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 26;
            Item.value = 18000;
        }
        public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(Mod.GetItem("OldDoubleAxe"));
            recipe.AddTile(Mod.GetTile("SweatyCyclopsForge"));
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
