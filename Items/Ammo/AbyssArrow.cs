using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Ranged.Ammo;

namespace tsorcRevamp.Items.Ammo
{
    public class AbyssArrow : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.consumable = true;
            Item.ammo = AmmoID.Arrow;
            Item.damage = 25;
            Item.height = 28;
            Item.knockBack = (float)4.2;
            Item.maxStack = Item.CommonMaxStack;
            Item.DamageType = DamageClass.Ranged;
            Item.shootSpeed = 4.5f;
            Item.value = 50;
            Item.width = 10;
            Item.rare = ItemRarityID.Red; 
            Item.shoot = ModContent.ProjectileType<AbyssArrowProjectile>();
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe(400);
            recipe.AddIngredient(ItemID.WoodenArrow, 400);
            recipe.AddIngredient(ModContent.ItemType<FlameOfTheAbyss>(), 1);  
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }


    }
}
