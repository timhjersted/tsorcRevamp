using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Ranged.Ammo;

namespace tsorcRevamp.Items.Ammo
{
    public class AbyssBullet : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
			Item.damage = 22; 
			Item.DamageType = DamageClass.Ranged;
			Item.width = 10;
			Item.height = 14;
			Item.maxStack = Item.CommonMaxStack;
			Item.consumable = true; 
			Item.knockBack = 4.1f;
			Item.value = 10;
			Item.rare = ItemRarityID.Red;
			Item.shoot = ModContent.ProjectileType<AbyssBulletProjectile>(); 
			Item.shootSpeed = 6f; 
			Item.ammo = AmmoID.Bullet; 
		}

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe(400);
            recipe.AddIngredient(ItemID.EmptyBullet, 400);
            recipe.AddIngredient(ModContent.ItemType<FlameOfTheAbyss>(), 1);  
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}