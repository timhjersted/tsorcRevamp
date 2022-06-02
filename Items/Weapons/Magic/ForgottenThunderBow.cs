using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    class ForgottenThunderBow : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Casts a bolt of lightning from your bow, doing massive damage over time. ");
        }
        public override void SetDefaults()
        {
            Item.damage = 140;
            Item.height = 58;
            Item.knockBack = 4;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.DamageType = DamageClass.Magic;
            Item.rare = ItemRarityID.Red;
            Item.mana = 100;
            Item.shootSpeed = 33;
            Item.useAnimation = 40;
            Item.UseSound = SoundID.Item5;
            Item.useTime = 40;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.value = PriceByRarity.Red_10;
            Item.width = 16;
            Item.shoot = ModContent.ProjectileType<Projectiles.Bolt4Ball>();
        }
        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("ForgottenThunderBowScroll").Type, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("Bolt4Tome").Type, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("SoulOfArtorias").Type, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("Humanity").Type, 30);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 200000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
