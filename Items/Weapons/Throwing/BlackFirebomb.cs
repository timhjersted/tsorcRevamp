using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles;

namespace tsorcRevamp.Items.Weapons.Throwing
{
    class BlackFirebomb : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Black Firebomb");
            /* Tooltip.SetDefault("Explodes, dealing fire damage in a small area" +
                               "\nSets the ground and enemies alight"); */
        }
        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Blue;
            Item.width = 22;
            Item.damage = 200;
            Item.height = 24;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 10f;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.value = 500;
            Item.shoot = ModContent.ProjectileType<BlackFirebombProj>();
            Item.shootSpeed = 6.5f;
            Item.useAnimation = 50;
            Item.useTime = 50;
            Item.UseSound = SoundID.Item1;
            Item.consumable = true;
            Item.maxStack = 999;
            Item.DamageType = DamageClass.Throwing;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe(2);
            recipe.AddIngredient(ModContent.ItemType<Firebomb>(), 2);
            recipe.AddIngredient(ModContent.ItemType<CharcoalPineResin>());
            recipe.AddIngredient(ItemID.SoulofNight);
            recipe.Register();
        }
    }
}