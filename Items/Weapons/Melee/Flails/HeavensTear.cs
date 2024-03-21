using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Melee.Flails;

namespace tsorcRevamp.Items.Weapons.Melee.Flails
{
    public class HeavensTear : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Heaven's Tear");
            /* Tooltip.SetDefault("Heaven splits with each swing" +
                "\nDeals double damage to mages and ghosts"); */

        }

        public override void SetDefaults()
        {

            Item.width = 32;
            Item.height = 32;
            //item.pretendType=389;
            //item.prefixType=368;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.channel = true;
            Item.useAnimation = 38;
            Item.useTime = 38;
            Item.damage = 200;
            Item.knockBack = 10;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Red;
            Item.shootSpeed = 14;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.value = PriceByRarity.Red_10;
            Item.DamageType = DamageClass.Melee;
            Item.shoot = ModContent.ProjectileType<HeavensTearBall>();
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ItemID.FlowerPow, 1);
            recipe.AddIngredient(ModContent.ItemType<GuardianSoul>(), 1);
            //recipe.AddIngredient(ModContent.ItemType<CursedSoul>(), 10);
            recipe.AddIngredient(ModContent.ItemType<SoulOfArtorias>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 120000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

    }
}
