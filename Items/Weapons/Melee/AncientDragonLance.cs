using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    public class AncientDragonLance : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Said to pierce any armor, even through walls" +
                "\nCan hit multiple times");
        }

        public override void SetDefaults() {
            item.damage = 12;
            item.knockBack = 4f;

            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useAnimation = 11;
            item.useTime = 3;
            item.shootSpeed = 7;
            //item.shoot = ProjectileID.DarkLance;
            
            item.height = 50;
            item.width = 50;

            item.melee = true;
            item.noMelee = true;
            item.noUseGraphic = true;

            item.rare = ItemRarityID.Green;
            item.maxStack = 1;
            item.UseSound = SoundID.Item1;
            item.shoot = ModContent.ProjectileType<Projectiles.AncientDragonLance>();

        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Trident);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 3000);
            recipe.SetResult(this, 1);
            recipe.AddTile(TileID.DemonAltar);
            recipe.AddRecipe();
        }
    }
}
