using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Melee.Flails;

namespace tsorcRevamp.Items.Weapons.Melee.Flails
{

    public class DiamondCrusher : ModItem
    {

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.channel = true;
            Item.useAnimation = 45;
            Item.useTime = 45;
            Item.damage = 24;
            Item.knockBack = 5;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Green;
            Item.shootSpeed = 10;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.value = PriceByRarity.Green_2;
            Item.DamageType = DamageClass.Melee;
            Item.shoot = ModContent.ProjectileType<DiamondCrusherBall>();
        }

        public override void AddRecipes()
        {
            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.FlamingMace, 1);
            recipe2.AddIngredient(ItemID.Diamond, 8);
            recipe2.AddIngredient(ModContent.ItemType<DarkSoul>(), 1300);
            recipe2.AddTile(TileID.DemonAltar);

            recipe2.Register();

            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Mace, 1);
            recipe.AddIngredient(ItemID.Diamond, 8);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 1500);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }


    }
}
