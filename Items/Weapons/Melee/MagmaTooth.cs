using Microsoft.Xna.Framework;

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class MagmaTooth : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Chance to light enemies on fire.");
        }
        public override void SetDefaults() {
            item.width = 40;
            item.height = 40;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useAnimation = 25;
            item.useTime = 25;
            item.maxStack = 1;
            item.damage = 39;
            item.knockBack = 8;
            item.scale = 1.2f;
            item.UseSound = SoundID.Item1;
            item.rare = ItemRarityID.LightRed;
            item.value = 50000;
            item.melee = true;
        }

        public override void OnHitNPC(Player player, NPC target, int damage, float knockBack, bool crit) {
            if (Main.rand.Next(2) == 0) {
                target.AddBuff(BuffID.OnFire, 300, false);
            }
        }
        public override void MeleeEffects(Player player, Rectangle hitbox) {
            int dust = Dust.NewDust(new Vector2((float)hitbox.X, (float)hitbox.Y), hitbox.Width, hitbox.Height, 6, player.velocity.X, player.velocity.Y, 100, default, 2f);
            Main.dust[dust].noGravity = true;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.FieryGreatsword, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 1700);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
