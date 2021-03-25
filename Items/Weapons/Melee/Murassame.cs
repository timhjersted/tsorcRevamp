using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class Murassame : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("A sword crafted for magic users");
        }

        public override void SetDefaults() {
            item.width = 48;
            item.height = 48;
            item.useAnimation = 11;
            item.useTime = 11;
            item.damage = 50;
            item.knockBack = 9;
            item.autoReuse = true;
            item.useTurn = true;
            item.UseSound = SoundID.Item1;
            item.rare = ItemRarityID.LightRed;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.value = 500000;
            item.melee = true;
            item.mana = 5;
            item.shoot = ModContent.ProjectileType<Projectiles.HealingWater>();
            item.shootSpeed = 12f;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SoulofLight, 3);
            recipe.AddIngredient(mod.GetItem("Muramassa"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 5000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
