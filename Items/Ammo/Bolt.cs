using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Ammo {

    public class Bolt : ModItem {

        public override void SetDefaults() {

            item.stack = 1;
            item.consumable = true;
            item.ammo = item.type; //this is what defines a custom ammo type. now we can call mod.ItemType("Bolt") as useAmmo for weapons
            item.damage = 5;
            item.height = 28;
            item.knockBack = (float)3;
            item.maxStack = 250;
            item.ranged = true;
            item.scale = (float)1;
            item.shootSpeed = (float)3.5f;
            item.useAnimation = 100;
            item.useTime = 100;
            item.value = 10;
            item.width = 10;
            item.shoot = ModContent.ProjectileType<Projectiles.Bolt>(); //dont forget to make it shoot a projectile
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);

            recipe.AddIngredient(ItemID.Wood, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 2);

            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 10);
            recipe.AddRecipe();
        }


    }
}
