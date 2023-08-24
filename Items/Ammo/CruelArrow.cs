using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Ranged.Ammo;

namespace tsorcRevamp.Items.Ammo
{
    public class CruelArrow : ModItem
    {
        public static int Pierce = 1;
        public static float DmgMult = 8;
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.consumable = true;
            Item.ammo = AmmoID.Arrow;
            Item.damage = 7;
            Item.height = 28;
            Item.knockBack = (float)3.5;
            Item.maxStack = 9999;
            Item.DamageType = DamageClass.Ranged;
            Item.shootSpeed = 6.5f;
            Item.value = 50;
            Item.width = 10;
            Item.shoot = ModContent.ProjectileType<CruelArrowProjectile>();
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe(30);
            recipe.AddIngredient(ItemID.WoodenArrow, 30);
            recipe.AddIngredient(ItemID.IronOre, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 15); //480 DS per 1000, I think that's fair. 
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }


    }
}
