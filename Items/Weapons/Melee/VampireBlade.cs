using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class VampireBlade : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Heals the player when dealing damage to enemies");
        }
        public override void SetDefaults() {
            item.width = 40;
            item.height = 40;
            item.autoReuse = true;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useAnimation = 27;
            item.useTime = 23;
            item.damage = 42;
            item.knockBack = 2;
            item.scale = 1.1f;
            item.UseSound = SoundID.Item1;
            item.rare = ItemRarityID.LightRed;
            item.value = 300000;
            item.melee = true;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.AdamantiteBar, 25);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 20000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void OnHitNPC(Player player, NPC target, int damage, float knockBack, bool crit) {
            player.statLife += damage / 20;
            if (player.statLife < player.statLifeMax2) {
                player.statLife = player.statLifeMax2;
            }
        }

        public override void OnHitPvp(Player player, Player target, int damage, bool crit) {
            player.statLife += damage / 20;
            if (player.statLife < player.statLifeMax2) {
                player.statLife = player.statLifeMax2;
            }
        }

        public override void MeleeEffects(Player player, Rectangle hitbox) {
            int dust = Dust.NewDust(new Vector2(hitbox.X, hitbox.Y), hitbox.Width, hitbox.Height, 54, player.velocity.X * 0.2f + player.direction * 3, player.velocity.Y * 0.2f, 100, default, 1.9f);
            Main.dust[dust].noGravity = true;
        }
    }
}
