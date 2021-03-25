using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class Muramassa : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("A sword crafted for magic users" +
                                "\nCan be upgraded with 25,000 Dark Souls & 3 Souls of Light");
        }

        public override void SetDefaults() {
            item.width = 48;
            item.height = 48;
            item.useAnimation = 11;
            item.useTime = 11;
            item.damage = 27;
            item.knockBack = 3;
            item.autoReuse = true;
            item.useTurn = true;
            item.UseSound = SoundID.Item1;
            item.rare = ItemRarityID.Green;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.value = 27000;
            item.melee = true;
            item.mana = 5;
            item.shoot = ModContent.ProjectileType<Projectiles.HealingWater>();
            item.shootSpeed = 11f;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Muramasa, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 5000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
