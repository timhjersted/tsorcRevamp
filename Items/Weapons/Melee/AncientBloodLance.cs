using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    public class AncientBloodLance : ModItem {

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Ancient Blood Lance");
            Tooltip.SetDefault("Pierces multiple times on every hit.");
        }

        public override void SetDefaults() {
            Item.damage = 33;
            Item.knockBack = 6.5f;

            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 13;
            Item.useTime = 4;
            Item.shootSpeed = 8;
            //item.shoot = ProjectileID.DarkLance;
            
            Item.height = 50;
            Item.width = 50;

            Item.DamageType = DamageClass.Melee;
            Item.noMelee = true;
            Item.noUseGraphic = true;

            Item.value = PriceByRarity.Orange_3;
            Item.rare = ItemRarityID.Orange;
            Item.maxStack = 1;
            Item.UseSound = SoundID.Item1;
            Item.shoot = ModContent.ProjectileType<Projectiles.AncientBloodLance>();

        }

        public override void AddRecipes() {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.DarkLance);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 6000);
            
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
