/*
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged
{
    public class ScoutsBlowpipe : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Scout's Blowpipe");
        }

        public override void SetDefaults()
        {
            Item.DamageType = DamageClass.Ranged;
            Item.shoot = ProjectileID.PoisonDart;
            Item.damage = 20;
            Item.knockBack = 1.5f;
            Item.maxStack = 1;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.rare = ItemRarityID.Green;
            Item.shootSpeed = 7.5f;
            Item.useAmmo = AmmoID.Dart;
            Item.useTime = 25;
            Item.useAnimation = 25;
            Item.UseSound = SoundID.Item63;//64 for Blowgun
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.value = PriceByRarity.Green_2;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ItemID.Blowpipe, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 4000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }


    }
}
*/