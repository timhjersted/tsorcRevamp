using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    class PoisonFieldRune : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Creates a poisonous cloud at the point of impact." +
                                "\nHigh duration and can damage many targets.");
        }

        public override void SetDefaults()
        {
            Item.damage = 14;
            Item.height = 28;
            Item.knockBack = 3f;
            Item.rare = ItemRarityID.Green;
            Item.shootSpeed = 6;
            Item.DamageType = DamageClass.Magic;
            Item.autoReuse = true;
            Item.mana = 15;
            Item.noMelee = true;
            Item.useAnimation = 45;
            Item.UseSound = SoundID.Item21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 45;
            Item.value = PriceByRarity.Green_2;
            Item.width = 20;
            Item.shoot = ModContent.ProjectileType<Projectiles.PoisonFieldBall>();
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(ItemID.Stinger, 5);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 3000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
