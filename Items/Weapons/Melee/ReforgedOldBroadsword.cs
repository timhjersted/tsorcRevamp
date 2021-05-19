using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class ReforgedOldBroadsword : ModItem
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/OldBroadsword";
        public override void SetDefaults()
        {
            item.damage = 14;
            item.width = 44;
            item.height = 44;
            item.knockBack = 4;
            item.maxStack = 1;
            item.melee = true;
            item.scale = .8f;
            item.useAnimation = 17;
            item.rare = ItemRarityID.White;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 21;
            item.value = 13000;
        }
        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("OldBroadsword"));
            recipe.AddTile(mod.GetTile("SweatyCyclopsForge"));
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}

