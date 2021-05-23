using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged {
    class ReforgedOldLongbow : ModItem {
        public override string Texture => "tsorcRevamp/Items/Weapons/Ranged/OldLongbow";
        public override void SetDefaults() {

            item.damage = 16;
            item.height = 66;
            item.width = 16;
            item.ranged = true;
            item.knockBack = 4f;
            item.maxStack = 1;
            item.noMelee = true;
            item.rare = ItemRarityID.White;
            item.scale = 0.9f;
            item.shoot = AmmoID.Arrow;
            item.shootSpeed = 13f;
            item.useAmmo = AmmoID.Arrow;
            item.useAnimation = 25;
            item.useTime = 25;
            item.UseSound = SoundID.Item5;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.value = 50000;

        }
        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("OldLongbow"));
            recipe.AddTile(mod.GetTile("SweatyCyclopsForge"));
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
