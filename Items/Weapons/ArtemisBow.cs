using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons {
    public class ArtemisBow : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("A bow forged to slay demon-gods.");

        }

        public override void SetDefaults() {
            item.ranged = true;
            item.shoot = ProjectileID.PurificationPowder;

            item.damage = 400;
            item.width = 24;
            item.height = 60;
            item.knockBack = (float)19;
            item.maxStack = 1;
            item.noMelee = true;
            item.rare = ItemRarityID.Pink;
            item.scale = (float)0.8;
            item.shootSpeed = (float)16;
            item.useAmmo = AmmoID.Arrow;
            item.UseSound = SoundID.Item5;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useAnimation = 50;
            item.useTime = 50;
            item.value = 3100000;

        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);

            recipe.AddIngredient(ItemID.GoldBow, 1);
            recipe.AddIngredient(ItemID.CobaltBar, 12);
            recipe.AddIngredient(ItemID.SoulofLight, 18);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 75000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }


    }
}
