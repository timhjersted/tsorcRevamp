using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Items.Weapons.Magic {
    class FireSpiritTome3 : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Fire Spirit Tome III");
            Tooltip.SetDefault("Summons fire spirits with incredible speed and damage.");
        }

        public override void SetDefaults() {
            Item.width = 28;
            Item.height = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 5;
            Item.useTime = 5;
            Item.damage = 71;
            Item.knockBack = 11;
            Item.autoReuse = true;
            Item.scale = 1.3f;
            Item.UseSound = SoundID.Item9;
            Item.rare = ItemRarityID.Cyan;
            Item.shootSpeed = 13;
            Item.mana = 5;
            Item.value = PriceByRarity.Cyan_9;
            Item.magic = true;
            Item.shoot = ModContent.ProjectileType<Projectiles.FireSpirit2>();
        }

        public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(Mod.GetItem("FireSpiritTome2"), 1);
            recipe.AddIngredient(Mod.GetItem("RedTitanite"), 5);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 95000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
