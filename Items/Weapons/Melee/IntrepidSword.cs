using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class IntrepidSword : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Only those who walk lightly can master this sword's true power" +
                                "\nDamage is reduced from its full power with higher defense.");
        }

        public override void SetDefaults() {
            Item.width = 48;
            Item.height = 48;
            Item.useAnimation = 23;
            Item.useTime = 23;
            Item.damage = 200;
            Item.knockBack = 3;
            Item.autoReuse = true;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Lime;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = PriceByRarity.Lime_7;
            Item.melee = true;
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
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(Mod.GetItem("Galaxia"), 1);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 20000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
