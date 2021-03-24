using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons {
    class FreezeBolt : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Casts a fast-moving bolt of ice");
        }

        public override void SetDefaults() {
            item.width = 24;
            item.height = 28;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useAnimation = 17;
            item.useTime = 17;
            item.damage = 27;
            item.knockBack = 5;
            item.autoReuse = true;
            item.UseSound = SoundID.Item21;
            item.rare = ItemRarityID.Orange;
            item.shootSpeed = 7;
            item.mana = 12;
            item.value = 50000;
            item.magic = true;
            item.shoot = ModContent.ProjectileType<Projectiles.FreezeBolt>();
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.WaterBolt, 1);
            recipe.AddIngredient(ItemID.FallenStar, 50);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 10000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
