using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons {
    public class Bolt2Tome : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Bolt 2 Tome");
            Tooltip.SetDefault("A lost tome for artisans." +
                                "\nCan be upgraded with 25,000 Dark Souls and 15 Soul of Light.");

        }

        public override void SetDefaults() {

            item.damage = 18;
            item.height = 10;
            item.width = 34;
            item.knockBack = 0.1f;
            item.autoReuse = true;
            item.rare = ItemRarityID.Green;
            item.shootSpeed = 6f;
            item.magic = true;
            item.noMelee = true;
            item.mana = 20;
            item.useAnimation = 22;
            item.UseSound = SoundID.Item21;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = 22;
            item.value = 4200;
            item.shoot = ModContent.ProjectileType<Projectiles.Bolt2Ball>();
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("Bolt1Tome"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 9000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }


    }
}
