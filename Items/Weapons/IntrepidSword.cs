using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons {
    class IntrepidSword : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Only those who walk lightly can master this sword's true power" +
                                "\nDamage is reduced from its full power with higher defense.");
        }

        public override void SetDefaults() {
            item.width = 48;
            item.height = 48;
            item.useAnimation = 23;
            item.useTime = 23;
            item.damage = 200;
            item.knockBack = 3;
            item.autoReuse = true;
            item.useTurn = true;
            item.UseSound = SoundID.Item1;
            item.rare = ItemRarityID.Green;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.value = 27000;
            item.melee = true;
        }

        public override void MeleeEffects(Player player, Rectangle hitbox) {
            Color color = new Color();
            int dust = Dust.NewDust(new Vector2((float)hitbox.X, (float)hitbox.Y), hitbox.Width, hitbox.Height, 6, (player.velocity.X * 0.2f) + (player.direction * 3), player.velocity.Y * 0.2f, 100, color, 1.9f);
            Main.dust[dust].noGravity = true;
        }

        public override void ModifyWeaponDamage(Player player, ref float add, ref float mult, ref float flat) {
            mult = (-0.01f * player.statDefense) + 1;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("Galaxia"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 20000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
