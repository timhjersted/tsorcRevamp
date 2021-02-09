using Microsoft.Xna.Framework;
using System.Collections.Generic;

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using tsorcRevamp;


namespace tsorcRevamp.Items {
    public class VillageMirror : ModItem {


        int playerXLocation2(Player player) {
            return (int)((player.position.X + player.width / 2.0 + 8.0) / 16.0);
        }
        int playerYLocation2(Player player) {
            return (int)((player.position.Y + player.height) / 16.0);
        }

        bool checkWarpLocation2(int x, int y) {
            if (x < 10 || x > Main.maxTilesX - 10 || y < 10 || y > Main.maxTilesY - 10) {
                Main.NewText("Your warp is bugged! (outside the world)", 255, 240, 20);
                return false;
            }

            for (int sanityX2 = x - 1; sanityX2 < x + 1; sanityX2++) {
                for (int sanityY2 = y - 3; sanityY2 < y; sanityY2++) {
                    if (Main.tile[sanityX2, sanityY2] == null) {
                        Main.NewText("Warp tile null! (" + sanityX2 + ", " + sanityY2 + ")!", 255, 240, 20);
                        return false;
                    }

                    if (Main.tile[sanityX2, sanityY2].active() && Main.tileSolid[Main.tile[sanityX2, sanityY2].type] && !Main.tileSolidTop[Main.tile[sanityX2, sanityY2].type]) {
                        WorldGen.KillTile(sanityX2, sanityY2, false, false, false);
                    }

                }
            }
            return true;
        }

        double warpSetDelay2;
        public void Initialize() {
            warpSetDelay2 = Main.time - 120.0;
        }
        public override void SetDefaults() {
            item.CloneDefaults(ItemID.MagicMirror);
            item.accessory = true;
            item.value = 25000;
        }

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Equip this in an accessory slot anywhere to create a new warp point." +
                                "\nActivate by left-clicking the mirror in your toolbar." +
                                "\nWarp point saves on quit." +
                                "\nReduces defense to 0 and slows movement while equipped and setting your warp point.");
        }

        public override void UseStyle(Player player) {
            if (!player.GetModPlayer<tsorcRevampPlayer>().townWarpSet) {
                Main.NewText("You haven't set a location!", 255, 240, 20);
            }
            else if (player.GetModPlayer<tsorcRevampPlayer>().townWarpWorld != Main.worldID) {
                Main.NewText("This mirror is set in a different world!", 255, 240, 20);
            }
            else {
                if (checkWarpLocation2(player.GetModPlayer<tsorcRevampPlayer>().townWarpX, player.GetModPlayer<tsorcRevampPlayer>().townWarpY)) {
                    if (Main.rand.NextBool()) { //ambient dust during use

                        // position, width, height, type, speed.X, speed.Y, alpha, color, scale
                        Dust.NewDust(player.position, player.width, player.height, 57, 0f, 0.5f, 150, default(Color), 1f);
                    }

                    if (player.itemTime == 0) {
                        Main.NewText("Picking up where you left off...", 255, 240, 20);
                        player.itemTime = (int)(item.useTime / PlayerHooks.TotalUseTimeMultiplier(player, item));
                    }
                    else if (player.itemTime == (int)(item.useTime / PlayerHooks.TotalUseTimeMultiplier(player, item)) / 2) {



                        for (int dusts = 0; dusts < 70; dusts++) { //dusts on tp (source)
                            Dust.NewDust(player.position, player.width, player.height, 57, player.velocity.X * 0.5f, (player.velocity.Y * 0.5f) + 0.5f, 150, default(Color), 1.5f);
                        }

                        //destroy grapples
                        player.grappling[0] = -1;
                        player.grapCount = 0;
                        for (int p = 0; p < 1000; p++) {
                            if (Main.projectile[p].active && Main.projectile[p].owner == player.whoAmI && Main.projectile[p].aiStyle == 7) {
                                Main.projectile[p].Kill();
                            }
                        }

                        player.position.X = (float)(player.GetModPlayer<tsorcRevampPlayer>().townWarpX * 16) - (float)((float)player.width / 2.0);
                        player.position.Y = (float)(player.GetModPlayer<tsorcRevampPlayer>().townWarpY * 16) - (float)player.height;
                        player.velocity.X = 0f;
                        player.velocity.Y = 0f;

                        for (int dusts = 0; dusts < 70; dusts++) { //dusts on tp (destination)
                            Dust.NewDust(player.position, player.width, player.height, 57, player.velocity.X * 0.5f, (player.velocity.Y * 0.5f) + 0.5f * 0.5f, 150, default(Color), 1.5f);
                        }

                    }
                }
                else {
                    Main.NewText("Your warp location is broken! Please file a bug report!", 255, 240, 20);
                }
            }
        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            player.moveSpeed -= 2f;
            player.statDefense -= player.statDefense;
            if (!player.GetModPlayer<tsorcRevampPlayer>().townWarpSet) {
                player.GetModPlayer<tsorcRevampPlayer>().townWarpX = playerXLocation2(player);
                player.GetModPlayer<tsorcRevampPlayer>().townWarpY = playerYLocation2(player);
                player.GetModPlayer<tsorcRevampPlayer>().townWarpWorld = Main.worldID;
                player.GetModPlayer<tsorcRevampPlayer>().townWarpSet = true;
                Main.NewText("New warp location set!", 255, 240, 30);
            }
            else {
                double timeDifference2 = Main.time - warpSetDelay2;
                if ((timeDifference2 > 120.0) || (timeDifference2 < 0.0)) {
                    player.GetModPlayer<tsorcRevampPlayer>().townWarpX = playerXLocation2(player);
                    player.GetModPlayer<tsorcRevampPlayer>().townWarpY = playerYLocation2(player);
                    player.GetModPlayer<tsorcRevampPlayer>().townWarpWorld = Main.worldID;
                    player.GetModPlayer<tsorcRevampPlayer>().townWarpSet = true;
                    warpSetDelay2 = Main.time;
                    Main.NewText("New warp location set!", 255, 240, 30);
                }
            }
        }
        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.MagicMirror, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 100);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}


