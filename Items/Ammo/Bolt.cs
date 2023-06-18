using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Ammo
{

    public class Bolt : ModItem
    {

        public override void SetDefaults()
        {
            Item.consumable = true;
            Item.ammo = Item.type; //this is what defines a custom ammo type. now we can call mod.ItemType("Bolt") as useAmmo for weapons
            Item.damage = 7;
            Item.height = 28;
            Item.knockBack = 3f;
            Item.maxStack = 2000;
            Item.DamageType = DamageClass.Ranged;
            Item.shootSpeed = 3.5f;
            Item.useAnimation = 100;
            Item.useTime = 100;
            Item.value = 10;
            Item.width = 10;
            Item.shoot = ModContent.ProjectileType<Projectiles.Bolt>(); //dont forget to make it shoot a projectile
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe(10);

            recipe.AddIngredient(ItemID.Wood, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 2);

            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }


    }
}
