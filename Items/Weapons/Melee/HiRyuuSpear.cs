using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee
{
    public class HiRyuuSpear : ModItem
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("HiRyuu Spear");
        }

        public override void SetDefaults()
        {
            Item.damage = 128; //was 78
            Item.knockBack = 7f;

            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 15;
            Item.useTime = 5;
            Item.shootSpeed = 5;
            //item.shoot = ProjectileID.DarkLance;

            Item.height = 50;
            Item.width = 50;

            Item.DamageType = DamageClass.Melee;
            Item.noMelee = true;
            Item.noUseGraphic = true;

            Item.value = PriceByRarity.LightPurple_6;
            Item.rare = ItemRarityID.LightPurple;
            Item.maxStack = 1;
            Item.UseSound = SoundID.Item1;
            Item.shoot = ModContent.ProjectileType<Projectiles.HiRyuuSpear>();

        }

        /*public override void AddRecipes() {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.AdamantiteGlaive);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 32000);
            
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
        */
    }
}
