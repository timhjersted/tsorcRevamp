using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    public class SupersonicBoots : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Supersonic movement speed, rocket boots effect, knockback protection, and water-walking if moving fast enough." +
                                "\nDoes not work if Hermes Boots or Spectre Boots are equipped." + 
                                "\nSpeed boost is multiplied by movement speed boosts." +
                                "\nCan be upgraded eventually with 1 Cloud in a Balloon, 1 Angel Wings & 20000 Dark Souls.");
        }

        public override void SetDefaults() {
            item.width = 32;
            item.height = 28;
            item.accessory = true;
            item.value = PriceByRarity.Orange_3;
            item.rare = ItemRarityID.Orange;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SpectreBoots, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 7000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void UpdateEquip(Player player) {
            player.noKnockback = true;
            player.moveSpeed += 0.2f;
            player.rocketBoots = 2;

            bool restricted = false;
            for (int i = 3; i <= 8; i++) {
                if (player.mount.Active || player.armor[i].type == ItemID.HermesBoots || player.armor[i].type == ItemID.SpectreBoots
					|| player.armor[i].type == ItemID.LightningBoots || player.armor[i].type == ItemID.FlurryBoots
					|| player.armor[i].type == ItemID.FrostsparkBoots || player.armor[i].type == ItemID.SailfishBoots) {
                    restricted = true;
                }
            }
            if (!restricted) {
                player.GetModPlayer<tsorcRevampPlayer>().supersonicLevel = 1;

                /** W1K's original code
                if (player.controlLeft) {
                    if (player.velocity.X > -3) player.velocity.X -= (float)(player.moveSpeed - 1f) / 10;
                    if (player.velocity.X < -3 && player.velocity.X > -6 * player.moveSpeed) {
                        if (player.velocity.Y != 0) player.velocity.X -= 0.1f;
                        else player.velocity.X -= 0.2f;
                        player.velocity.X -= 0.02f + ((player.moveSpeed - 1f) / 10);
                    }
                }
                if (player.controlRight) {
                    if (player.velocity.X < 3) player.velocity.X += (float)(player.moveSpeed - 1f) / 10;
                    if (player.velocity.X > 3 && player.velocity.X < 6 * player.moveSpeed) {
                        if (player.velocity.Y != 0) player.velocity.X += 0.1f;
                        else player.velocity.X += 0.2f;
                        player.velocity.X += 0.02f + ((player.moveSpeed - 1f) / 10);
                    }
                } **/


                if (player.velocity.X > 6 || player.velocity.X < -6) {
                    player.waterWalk = true;
                    int sonicDust = Dust.NewDust(new Vector2((float)player.position.X, (float)player.position.Y), player.width, player.height, 16, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 100, default, 2f);
                    Main.dust[sonicDust].noGravity = true;
                    Main.dust[sonicDust].noLight = false;

                }
            }

        }

    }
}
