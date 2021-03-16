using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons {
    class Galaxia : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Forged from the stars of a distant galaxy");
        }
        public override void SetDefaults() {
            item.width = 40;
            item.height = 40;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useAnimation = 20;
            item.useTime = 20;
            item.damage = 75;
            item.knockBack = 6;
            item.UseSound = SoundID.Item1;
            item.rare = ItemRarityID.LightRed;
            item.value = 300000;
            item.melee = true;
            item.autoReuse = true;
            item.useTurn = true;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.FallenStar, 50);
            recipe.AddIngredient(ItemID.SoulofSight, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 50000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void MeleeEffects(Player player, Rectangle hitbox) {
            int dust = Dust.NewDust(new Vector2((float)hitbox.X, (float)hitbox.Y), hitbox.Width, hitbox.Height, 57, (player.velocity.X), player.velocity.Y, 200, default, 1f);
            Main.dust[dust].noGravity = false;
        }
    }
}
