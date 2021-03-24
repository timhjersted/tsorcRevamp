using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Items.Weapons {
    class FireSpiritTome4 : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Fire Spirit Tome IV");
            Tooltip.SetDefault("Summons fire spirits with incredible speed and damage.");
        }

        public override void SetDefaults() {
            item.width = 28;
            item.height = 30;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useAnimation = 5;
            item.useTime = 5;
            item.maxStack = 1;
            item.damage = 133;
            item.knockBack = 8;
            item.autoReuse = true;
            item.scale = 1.3f;
            item.UseSound = SoundID.Item9;
            item.rare = ItemRarityID.Pink;
            item.shootSpeed = 14;
            item.mana = 5;
            item.value = 900000;
            item.magic = true;
            item.shoot = ModContent.ProjectileType<Projectiles.FireSpirit2>();
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("FireSpiritTome3"), 1);
            recipe.AddIngredient(mod.GetItem("Humanity"), 3);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 215000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
