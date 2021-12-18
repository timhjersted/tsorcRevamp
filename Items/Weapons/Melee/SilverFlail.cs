using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class SilverFlail : ModItem {

        public override void SetDefaults() {
            item.width = 32;
            item.height = 32;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.channel = true;
            item.useAnimation = 50;
            item.useTime = 50;
            item.damage = 9;
            item.knockBack = 6f;
            item.UseSound = SoundID.Item1;
            item.rare = ItemRarityID.White;
            item.shootSpeed = 14;
            item.noUseGraphic = true;
            item.noMelee = true;
            item.value = 5000;
            item.melee = true;
            item.shoot = ModContent.ProjectileType<Projectiles.SilverBall>();
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SilverBar, 1);
            recipe.AddIngredient(ItemID.Chain, 2);
            recipe.AddIngredient(ModContent.ItemType<Items.DarkSoul>(), 100);
            recipe.AddTile(TileID.Anvils);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
