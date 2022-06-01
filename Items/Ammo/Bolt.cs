using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Ammo {

    public class Bolt : ModItem {

        public override void SetDefaults() {

            Item.stack = 1;
            Item.consumable = true;
            Item.ammo = Item.type; //this is what defines a custom ammo type. now we can call mod.ItemType("Bolt") as useAmmo for weapons
            Item.damage = 7;
            Item.height = 28;
            Item.knockBack = (float)3;
            Item.maxStack = 2000;
            Item.DamageType = DamageClass.Ranged;
            Item.scale = (float)1;
            Item.shootSpeed = (float)3.5f;
            Item.useAnimation = 100;
            Item.useTime = 100;
            Item.value = 10;
            Item.width = 10;
            Item.shoot = ModContent.ProjectileType<Projectiles.Bolt>(); //dont forget to make it shoot a projectile
        }

        public override void AddRecipes() {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ItemID.Wood, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 2);

            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 10);
            recipe.Register();
        }


    }
}
