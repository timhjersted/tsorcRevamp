
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Weapons.Magic.Tomes;

namespace tsorcRevamp.Items.Weapons.Magic
{
    class ForgottenIceBow : ModItem
    {

        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("Casts magic shards of ice from your bow." +
                                "\nAttuned with the greatest powers when wielded by mages." +
                                "\nEach shot can be channeled with the powers of your mind once in the air." +
                                "\nChanneling is useful for directing the shot directly above your enemies for maximum damage"); */
        }

        public override void SetDefaults()
        {
            Item.damage = 160;
            Item.height = 54;
            Item.knockBack = 4;
            Item.noMelee = true;
            Item.DamageType = DamageClass.Magic;
            Item.rare = ItemRarityID.Red;
            Item.mana = 40;
            Item.channel = true;
            Item.autoReuse = true;
            Item.scale = 0.9f;
            Item.shootSpeed = 34;
            Item.useAnimation = 15;
            Item.UseSound = SoundID.Item5;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 15;
            Item.value = PriceByRarity.Red_10;
            Item.width = 28;
            Item.shoot = ModContent.ProjectileType<Projectiles.Ice5Ball>();
        }

        public override void AddRecipes()
        {
            //todo add ingredients
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<ForgottenIceBowScroll>(), 1);
            recipe.AddIngredient(ModContent.ItemType<Ice4Tome>(), 1);
            recipe.AddIngredient(ModContent.ItemType<SoulOfArtorias>(), 1);
            recipe.AddIngredient(ModContent.ItemType<Humanity>(), 9);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 160000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
