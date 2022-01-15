using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    public class ForgottenBurningFist : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Randomly casts a great fireball explosion.");
        }

        public override void SetDefaults() { 
            item.autoReuse = true;
            item.damage = 62;
            item.width = 22;
            item.height = 18;
            item.knockBack = 3;
            item.melee = true;
            item.scale = 1.1f;
            item.useAnimation = 8;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 21;
            item.value = PriceByRarity.LightRed_4;
            item.rare = ItemRarityID.LightRed;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);

            recipe.AddIngredient(ItemID.AdamantiteBar, 1);
            recipe.AddIngredient(ItemID.SoulofNight, 6);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 30000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override bool UseItem(Player player) {
            if (Main.rand.Next(40) == 0) {
                Projectile.NewProjectile(
                player.position.X,
                player.position.Y,
                (float)(-40 + Main.rand.Next(80)) / 10,
                14.9f,
                ModContent.ProjectileType<Projectiles.GreatFireballBall>(),
                70,
                2.0f,
                player.whoAmI);
            }
            return true;
        }
    }
}
