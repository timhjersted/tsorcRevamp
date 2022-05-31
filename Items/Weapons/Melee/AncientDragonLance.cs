using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    public class AncientDragonLance : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Said to pierce any armor, even through walls" +
                "\nCan hit multiple times");
        }

        public override void SetDefaults() {
            Item.damage = 12;
            Item.knockBack = 4f;

            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 11;
            Item.useTime = 3;
            Item.shootSpeed = 7;
            //item.shoot = ProjectileID.DarkLance;
            
            Item.height = 50;
            Item.width = 50;

            Item.melee = true;
            Item.noMelee = true;
            Item.noUseGraphic = true;

            Item.rare = ItemRarityID.Green;
            Item.maxStack = 1;
            Item.UseSound = SoundID.Item1;
            Item.shoot = ModContent.ProjectileType<Projectiles.AncientDragonLance>();

        }

        public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(ItemID.Trident);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 3000);
            recipe.SetResult(this, 1);
            recipe.AddTile(TileID.DemonAltar);
            recipe.AddRecipe();
        }
    }
}
