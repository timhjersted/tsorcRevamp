using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons {
    class Ice1Tome : ModItem {

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Ice 1 Tome");
            Tooltip.SetDefault("A lost beginner's tome");
        }
        public override void SetDefaults() {
            item.damage = 10;
            item.height = 10;
            item.knockBack = 0f;
            item.channel = true;
            item.rare = ItemRarityID.Green;
            item.shootSpeed = 9;
            item.magic = true;
            item.noMelee = true;
            item.mana = 5;
            item.useAnimation = 10;
            item.UseSound = SoundID.Item21;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = 10;
            item.value = 200000;
            item.width = 34;
            item.shoot = ModContent.ProjectileType<Projectiles.Ice1Ball>();
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 4000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
