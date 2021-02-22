using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories {
    [AutoloadEquip(EquipType.Wings)]
    public class SupersonicWings2 : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Supersonic Wings II");
            Tooltip.SetDefault("+60% supersonic movement speed and virtually limitless flight" +
                                "\nPlus all the previous abilities of Supersonic Wings." +
                                "\nDoes not work if Hermes Boots or Spectre Boots are equipped." +
                                "\nCompatible with Dragoon Boots and Dragoon Gear.");
        }

        public override void SetDefaults() {
            item.width = 32;
            item.height = 28;
            item.accessory = true;
            item.value = 200000;
            item.rare = ItemRarityID.LightRed;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("SupersonicWings"), 1);
            recipe.AddIngredient(mod.GetItem("SoulOfAttraidies"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 70000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void VerticalWingSpeeds(Player player, ref float ascentWhenFalling, ref float ascentWhenRising, 
                            ref float maxCanAscendMultiplier, ref float maxAscentMultiplier, ref float constantAscend) {
            ascentWhenFalling = 0.85f;
            ascentWhenRising = 0.15f;
            maxCanAscendMultiplier = 1f;
            maxAscentMultiplier = 3f;
            constantAscend = 0.135f;

        }

        public override void HorizontalWingSpeeds(Player player, ref float speed, ref float acceleration) {
            speed = 9f;
        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            player.jumpBoost = true;
            player.doubleJumpCloud = true;
            player.fireWalk = true;
            player.noKnockback = true;
            player.noFallDmg = true;
            player.canRocket = true;
            player.rocketTime = 1200;
            player.rocketBoots = 2;
            player.rocketTimeMax = 1200;
            player.moveSpeed += 0.6f;
            player.jumpSpeedBoost = 3.2f;
            player.wingTimeMax = 1200;
            

            bool restricted = false;
            for (int i = 2; i <= 6; i++) {
                if (player.armor[i].type == ItemID.HermesBoots || player.armor[i].type == ItemID.SpectreBoots) {
                    restricted = true;
                }
            }
            if (!restricted) {

                if (player.controlLeft) {
                    if (player.velocity.X > -3) player.velocity.X -= (float)(player.moveSpeed - 1f) / 10;

                    if (player.velocity.X < -3 && player.velocity.X > -60 * player.moveSpeed) {
                        if (player.velocity.Y != 0) player.velocity.X -= 0.1f;
                        else player.velocity.X -= 0.2f;
                        player.velocity.X -= 0.02f + ((player.moveSpeed - 1f) / 10);
                    }

                }

                if (player.controlRight) {
                    if (player.velocity.X < 3) player.velocity.X += (float)(player.moveSpeed - 1f) / 10;
                    if (player.velocity.X > 3 && player.velocity.X < 60 * player.moveSpeed) {
                        if (player.velocity.Y != 0) player.velocity.X += 0.1f;
                        else player.velocity.X += 0.2f;
                        player.velocity.X += 0.02f + ((player.moveSpeed - 1f) / 10);
                    }
                }

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
