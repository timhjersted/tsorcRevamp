using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Items.Weapons {
    class FireSpiritTome2 : ModItem {

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Fire Spirit Tome II");
            Tooltip.SetDefault("Summons fire spirits with incredible speed and damage.");
        }

        public override void SetDefaults() {
            item.width = 28;
            item.height = 30;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useAnimation = 5;
            item.useTime = 5;
            item.maxStack = 1;
            item.damage = 40;
            item.knockBack = 8;
            item.autoReuse = true;
            item.scale = 1.3f;
            item.UseSound = SoundID.Item9;
            item.rare = ItemRarityID.Pink;
            item.shootSpeed = 12;
            item.mana = 5;
            item.value = 700000;
            item.magic = true;
            item.shoot = ModContent.ProjectileType<Projectiles.FireSpirit2>();
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("FireSpiritTome"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 45000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
