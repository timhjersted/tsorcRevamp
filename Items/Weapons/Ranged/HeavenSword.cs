using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged {
    class HeavenSword : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Aside from its adamantite grip, it is a throwing sword made of pure light." +
                                "\nBlessed with a divine aura, it manifests endlessly in the wielder's hand" +
                                "\nand returns if its blade should not pierce into the one whom it was meant for." +
                                "\nPasses through walls.");
        }
        public override void SetDefaults() {
            item.width = 34;
            item.height = 34;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useAnimation = 16;
            item.useTime = 16;
            item.autoReuse = true;
            item.maxStack = 1;
            item.damage = 75;
            item.knockBack = 5;
            item.UseSound = SoundID.Item1;
            item.shootSpeed = 12f;
            item.noUseGraphic = true;
            item.noMelee = true;
            item.value = 700000;
            item.ranged = true;
            item.shoot = ModContent.ProjectileType<Projectiles.HeavenSword>();
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.AdamantiteBar, 5);
            recipe.AddIngredient(ItemID.SoulofLight, 25);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 70000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
