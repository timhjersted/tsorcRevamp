using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class ReforgedOldSabre : ModItem
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/OldSabre";
        public override void SetDefaults()
        {
            item.damage = 10;
            item.width = 34;
            item.height = 38;
            item.knockBack = 4;
            item.maxStack = 1;
            item.melee = true;
            item.scale = 1f;
            item.useAnimation = 17;
            item.autoReuse = true;
            item.rare = ItemRarityID.White;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 21;
            item.value = 6000;
        }
        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("OldSabre"));
            recipe.AddTile(mod.GetTile("SweatyCyclopsForge"));
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
