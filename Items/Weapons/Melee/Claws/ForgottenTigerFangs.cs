using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using tsorcRevamp.Projectiles;

namespace tsorcRevamp.Items.Weapons.Melee.Claws
{
    class ForgottenTigerFangs : ModItem
    {
        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("Long and extremely sharp fighting claws.");
        }

        public override void SetDefaults()
        {
            Item.autoReuse = true;
            Item.useTurn = false;
            Item.rare = ItemRarityID.Cyan;
            Item.damage = 150;
            Item.width = 22;
            Item.height = 22;
            Item.knockBack = 3;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 8;
            Item.useTime = 8;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = PriceByRarity.Cyan_9;
            Item.shoot = ModContent.ProjectileType<Nothing>();
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<ForgottenKaiserKnuckles>());
            recipe.AddIngredient(ModContent.ItemType<GuardianSoul>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 80000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
