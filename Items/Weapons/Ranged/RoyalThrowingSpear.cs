using Terraria.ID;
using Terraria.ModLoader;
using Terraria;

namespace tsorcRevamp.Items.Weapons.Ranged
{
    class RoyalThrowingSpear : ModItem
    {

        public override void SetDefaults()
        {
            Item.consumable = true;
            Item.damage = 21;
            Item.height = 66;
            Item.knockBack = 6;
            Item.maxStack = 2000;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.DamageType = DamageClass.Ranged;
            Item.scale = 0.9f;
            Item.shootSpeed = 9;
            Item.useAnimation = 15;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 15;
            Item.value = 100;
            Item.width = 10;
            Item.shoot = ModContent.ProjectileType<Projectiles.RoyalThrowingSpear>();
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe(100);
            recipe.AddIngredient(ItemID.Wood, 20);
            recipe.AddIngredient(ItemID.StoneBlock, 20);
            recipe.AddIngredient(ItemID.SilverCoin, 50);
            recipe.AddTile(TileID.Anvils);
            recipe.Register();
        }
    }
}
