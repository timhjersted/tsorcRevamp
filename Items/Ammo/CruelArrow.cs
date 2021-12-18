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
            item.consumable = true;
            item.ammo = AmmoID.Arrow;
            item.damage = 7;
            item.height = 28;
            item.knockBack = (float)3.5;
            item.maxStack = 2000;
            item.ranged = true;
            item.scale = (float)1;
            item.shootSpeed = (float)6.5;
            item.value = 50;
            item.width = 10;
            item.shoot = ModContent.ProjectileType<Projectiles.CruelArrow>();
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.WoodenArrow, 30);
            recipe.AddIngredient(ItemID.IronOre, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 15); //480 DS per 1000, I think that's fair. 
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 30);
            recipe.AddRecipe();
        }


    }
}
