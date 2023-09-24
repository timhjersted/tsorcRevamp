using Terraria.ID;
using Terraria.ModLoader;
using Terraria;

namespace tsorcRevamp.Items.Weapons.Melee
{
    class ThrowingAxe : ModItem
    {

        public override void SetDefaults()
        {
            Item.consumable = true;
            Item.damage = 14;
            Item.height = 34;
            Item.knockBack = 6;
            Item.maxStack = 2000;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.DamageType = DamageClass.Melee;
            Item.shootSpeed = 7;
            Item.useAnimation = 22;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 22;
            Item.value = 5;
            Item.width = 22;
            Item.shoot = ModContent.ProjectileType<Projectiles.ThrowingAxe>();

            Item.mana = 3;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe(50);
            recipe.AddIngredient(ItemID.Wood, 5);
            recipe.AddIngredient(ItemID.StoneBlock, 5);
            recipe.AddTile(TileID.Anvils);
            recipe.Register();
        }
    }
}
