using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Ammo {
    public class ArrowOfBard : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Arrow of Bard");
            Tooltip.SetDefault("\"The arrow which slew Smaug.\"");

        }

        public override void SetDefaults() {

            item.stack = 1;
            item.consumable = true;
            item.ammo = AmmoID.Arrow;
            item.shoot = ModContent.ProjectileType<Projectiles.ArrowOfBard>();
            item.damage = 500;
            item.height = 28;
            item.knockBack = (float)4;
            item.maxStack = 2000;
            item.ranged = true;
            item.scale = (float)1;
            item.shootSpeed = (float)3.5f;
            item.useAnimation = 100;
            item.useTime = 100;
            item.value = 500000;
            item.width = 10;
            item.rare = ItemRarityID.Orange;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);

            recipe.AddIngredient(ItemID.MythrilBar, 1);
            recipe.AddIngredient(ItemID.SoulofLight, 10);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 2000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 10);
            recipe.AddRecipe();
        }


    }
}
