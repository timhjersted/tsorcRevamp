using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class Laevateinn : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Strike true." + "\nWielded like a shortsword");
        }

        public override void SetDefaults() {
            item.width = 24;
            item.height = 28;
            item.useStyle = ItemUseStyleID.Stabbing;
            item.useAnimation = 9;
            item.useTime = 9;
            item.damage = 50;
            item.knockBack = 3.8f;
            item.autoReuse = true;
            item.useTurn = true;
            item.scale = 0.95f;
            item.UseSound = SoundID.Item1;
            item.rare = ItemRarityID.Pink;
            item.value = PriceByRarity.Pink_5;
            item.melee = true;
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

        public override void MeleeEffects(Player player, Rectangle hitbox) {
            int dust = Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, 15, player.velocity.X * 0.2f + player.direction * 3, player.velocity.Y * 0.2f, 100, default, 1.0f);
            Main.dust[dust].noGravity = true;
        }
    }
}
