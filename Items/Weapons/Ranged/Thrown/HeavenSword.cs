using Terraria.ID;
using Terraria.ModLoader;
using Terraria;

namespace tsorcRevamp.Items.Weapons.Ranged.Thrown
{
    class HeavenSword : ModItem
    {
        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("Aside from its adamantite grip, it is a throwing sword made of pure light." +
                                "\nBlessed with a divine aura, it manifests endlessly in the wielder's hand" +
                                "\nand returns if its blade should not pierce into the one whom it was meant for." +
                                "\nPasses through walls."); */
        }
        public override void SetDefaults()
        {
            Item.width = 34;
            Item.height = 34;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 16;
            Item.useTime = 16;
            Item.autoReuse = true;
            Item.maxStack = 1;
            Item.damage = 75;
            Item.knockBack = 5;
            Item.UseSound = SoundID.Item1;
            Item.shootSpeed = 12f;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.value = PriceByRarity.LightRed_4;
            Item.DamageType = DamageClass.Ranged;
            Item.shoot = ModContent.ProjectileType<Projectiles.HeavenSword>();
            Item.rare = ItemRarityID.LightRed;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.AdamantiteBar, 3);
            recipe.AddIngredient(ItemID.SoulofLight, 15);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 70000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
