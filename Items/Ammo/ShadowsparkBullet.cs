using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Ranged.Ammo;

namespace tsorcRevamp.Items.Ammo
{
    public class ShadowsparkBullet : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
			Item.damage = 8; 
			Item.DamageType = DamageClass.Ranged;
			Item.width = 10;
			Item.height = 26;
			Item.maxStack = Item.CommonMaxStack;
			Item.consumable = true; 
			Item.knockBack = 1.1f;
			Item.value = 10;
			Item.rare = ItemRarityID.Pink;
			Item.shoot = ModContent.ProjectileType<ShadowsparkBulletProjectile>(); 
			Item.shootSpeed = 5.5f; 
			Item.ammo = AmmoID.Bullet; 
		}

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe(200);
            recipe.AddIngredient(ItemID.CrystalBullet, 200);
            recipe.AddIngredient(ItemID.SoulofNight, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 150);  
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}