using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Melee.Spears
{
    public class AncientDragonLance : ModItem
    {

        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("Said to pierce any armor, even through walls" +
                "\nCan hit multiple times" +
                "\nInherits Trident water effect"); */
        }

        public override void SetDefaults()
        {
            Item.damage = 12;
            Item.knockBack = 4f;

            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 11;
            Item.useTime = 3;
            Item.shootSpeed = 7;
            //item.shoot = ProjectileID.DarkLance;

            Item.height = 50;
            Item.width = 50;

            Item.DamageType = DamageClass.Melee;
            Item.noMelee = true;
            Item.noUseGraphic = true;

            Item.rare = ItemRarityID.Green;
            Item.maxStack = 1;
            Item.UseSound = SoundID.Item1;
            Item.shoot = ModContent.ProjectileType<Projectiles.Spears.AncientDragonLanceProj>();

        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Trident);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
