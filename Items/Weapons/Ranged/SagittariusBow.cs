using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged {
    class SagittariusBow : ModItem {

        public override void SetDefaults() {
            item.autoReuse = true;
            item.damage = 500;
            item.height = 28;
            item.knockBack = 12;
            item.noMelee = true;
            item.ranged = true;
            item.rare = ItemRarityID.LightPurple;
            item.shootSpeed = 16;
            item.useAnimation = 60;
            item.useTime = 60;
            item.UseSound = SoundID.Item5;
            item.shoot = ProjectileID.WoodenArrowFriendly;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.value = 19000000;
            item.width = 14;
            item.useAmmo = AmmoID.Arrow;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("ArtemisBow"), 1);
            recipe.AddIngredient(mod.GetItem("CursedSoul"), 70);
            recipe.AddIngredient(mod.GetItem("BlueTitanite"), 25);
            recipe.AddIngredient(mod.GetItem("FlameOfTheAbyss"), 40);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 250000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
