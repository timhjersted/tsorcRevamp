using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    public class GaeBolg : ModItem {

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Gae Bolg");
            Tooltip.SetDefault("Pierce reality \nCan be upgraded into its mythical form with 70,000 Dark Souls");
        }

        public override void SetDefaults() {
            item.damage = 79;
            item.knockBack = 5.5f;

            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useAnimation = 15;
            item.useTime = 15;
            item.shootSpeed = 8;
            
            item.height = 40;
            item.width = 40;

            item.melee = true;
            item.noMelee = true;
            item.noUseGraphic = true;

            item.value = PriceByRarity.LightPurple_6;
            item.rare = ItemRarityID.LightPurple;
            item.maxStack = 1;
            item.UseSound = SoundID.Item1;
            item.shoot = ModContent.ProjectileType<Projectiles.GaeBolg>();

        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Gungnir);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 40000);
            recipe.SetResult(this, 1);
            recipe.AddTile(TileID.DemonAltar);
            recipe.AddRecipe();
        }
    }
}
