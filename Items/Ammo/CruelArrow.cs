using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Ammo {
    public class CruelArrow : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Cruel Arrow");
            Tooltip.SetDefault("An arrow fashioned to kill lost and forgotten souls...\n" +
                                "Does 8x greater damage to some enemies\n" +
                                "Pierces once");

        }

        public override void SetDefaults() {
            Item.consumable = true;
            Item.ammo = AmmoID.Arrow;
            Item.damage = 7;
            Item.height = 28;
            Item.knockBack = (float)3.5;
            Item.maxStack = 2000;
            Item.ranged = true;
            Item.scale = (float)1;
            Item.shootSpeed = (float)6.5;
            Item.value = 50;
            Item.width = 10;
            Item.shoot = ModContent.ProjectileType<Projectiles.CruelArrow>();
        }

        public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(ItemID.WoodenArrow, 30);
            recipe.AddIngredient(ItemID.IronOre, 1);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 15); //480 DS per 1000, I think that's fair. 
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 30);
            recipe.AddRecipe();
        }


    }
}
