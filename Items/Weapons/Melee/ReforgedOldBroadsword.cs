using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class ReforgedOldBroadsword : ModItem
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/OldBroadsword";
        public override void SetDefaults()
        {
            Item.damage = 14;
            Item.width = 44;
            Item.height = 44;
            Item.knockBack = 4;
            Item.maxStack = 1;
            Item.melee = true;
            Item.scale = .8f;
            Item.useAnimation = 17;
            Item.rare = ItemRarityID.White;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 21;
            Item.value = 13000;
        }
        public override void AddRecipes()
        {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(Mod.GetItem("OldBroadsword"));
            recipe.AddTile(Mod.GetTile("SweatyCyclopsForge"));
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}

