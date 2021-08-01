using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class ReforgedOldTwoHandedSword : ModItem {
        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/OldTwoHandedSword";
        public override void SetDefaults() {
            item.damage = 22; //it's post EoC
            item.width = 50;
            item.height = 50;
            item.knockBack = 5;
            item.maxStack = 1;
            item.melee = true;
            item.scale = 1f;
            item.useAnimation = 30;
            item.rare = ItemRarityID.White;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 30;
            item.value = 15000;
        }
        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("OldTwoHandedSword"));
            recipe.AddTile(mod.GetTile("SweatyCyclopsForge"));
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
