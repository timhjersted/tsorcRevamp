using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class ClaiomhSolais : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Seize the day");
        }
        public override void SetDefaults() {
            item.width = 40;
            item.height = 40;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useAnimation = 25;
            item.useTime = 25;
            item.damage = 62;
            item.knockBack = 6f;
            item.autoReuse = true;
            item.scale = 1f;
            item.UseSound = SoundID.Item1;
            item.rare = ItemRarityID.Pink;
            item.value = PriceByRarity.Pink_5;
            item.melee = true;
        }
        public override void MeleeEffects(Player player, Rectangle hitbox) {
            //This is the same general effect done with the Fiery Greatsword
            int dust = Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, 15, player.velocity.X * 0.2f + player.direction * 3, player.velocity.Y * 0.2f, 100, default, 1.0f);
            Main.dust[dust].noGravity = true;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.CobaltBar, 5);
            recipe.AddIngredient(ItemID.MythrilBar, 5);
            recipe.AddIngredient(ItemID.AdamantiteBar, 5);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 20000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
