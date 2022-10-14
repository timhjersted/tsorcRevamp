using Terraria.ID;
using Terraria.ModLoader;
using Terraria;

namespace tsorcRevamp.Items.Weapons.Ranged.Bows
{
    class BowOfEarendil : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Bow of Earendil");
            Tooltip.SetDefault("Always aim for the heart" +
                               "\nLegendary");

        }

        public override void SetDefaults()
        {

            Item.damage = 90;
            Item.height = 58;
            Item.width = 20;
            Item.DamageType = DamageClass.Ranged;
            Item.knockBack = 4f;
            Item.maxStack = 1;
            Item.noMelee = true;
            Item.rare = ItemRarityID.Pink;
            Item.scale = 0.9f;
            Item.shoot = AmmoID.Arrow;
            Item.shootSpeed = 16;
            Item.useAmmo = AmmoID.Arrow;
            Item.useAnimation = 16;
            Item.useTime = 16;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item5;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.value = PriceByRarity.Pink_5;

        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SoulofSight, 1);
            recipe.AddIngredient(ItemID.MoltenFury, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 80000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
