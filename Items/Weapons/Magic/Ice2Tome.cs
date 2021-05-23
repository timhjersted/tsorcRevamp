using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    class Ice2Tome : ModItem {

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Ice 2 Tome");
            Tooltip.SetDefault("A lost tome for artisans, with a high rate of casting." +
                                "\nCan be upgraded with 25,000 Dark Souls and 15 Souls of Light.");
        }
        public override void SetDefaults() {
            item.damage = 15;
            item.height = 10;
            item.knockBack = 0f;
            item.rare = ItemRarityID.Green;
            item.channel = true;
            item.autoReuse = true;
            item.shootSpeed = 10;
            item.magic = true;
            item.noMelee = true;
            item.mana = 7;
            item.useAnimation = 10;
            item.UseSound = SoundID.Item21;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = 10;
            item.value = 140000;
            item.width = 34;
            item.shoot = ModContent.ProjectileType<Projectiles.Ice2Ball>();
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("Ice1Tome"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 8000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
