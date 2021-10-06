using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    public class ForgottenPolearm : ModItem {

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Forgotten Polearm");
            Tooltip.SetDefault("Shimmering ephemeral energy.");
        }

        public override void SetDefaults() {
            item.damage = 27;
            item.knockBack = 3f;

            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useAnimation = 11;
            item.useTime = 6;
            item.shootSpeed = 10;
            //item.shoot = ProjectileID.DarkLance;
            
            item.height = 50;
            item.width = 50;

            item.melee = true;
            item.noMelee = true;
            item.noUseGraphic = true;

            item.value = 120000;
            item.rare = ItemRarityID.Orange;
            item.maxStack = 1;
            item.UseSound = SoundID.Item1;
            item.shoot = ModContent.ProjectileType<Projectiles.ForgottenPolearm>();
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ModContent.ItemType<EphemeralDust>(), 35);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 6000);
            recipe.SetResult(this, 1);
            recipe.AddTile(TileID.DemonAltar);
            recipe.AddRecipe();
        }
    }
}
