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
            Player player = Main.LocalPlayer;
            if (!player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                if (x < 10 || x > Main.maxTilesX - 10 || y < 10 || y > Main.maxTilesY - 10)
                {
                    Main.NewText("Your warp is bugged! (outside the world)", 255, 240, 20);
                    return false;
                }

                for (int sanityX = x - 1; sanityX < x + 1; sanityX++)
                {
                    for (int sanityY = y - 3; sanityY < y; sanityY++)
                    {
                        Tile tile = Framing.GetTileSafely(sanityX, sanityY);
                        if (tile.active() && Main.tileSolid[tile.type] && !Main.tileSolidTop[tile.type])
                        {
                            WorldGen.KillTile(sanityX, sanityY);
                        }

                    }
                }
                return true;
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
            item.useTime = 240;
            item.useAnimation = 240;

        }

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Equip this in an accessory slot anywhere to create a new warp point." +
                                "\nActivate by left-clicking the mirror in your toolbar." +
                                "\nWarp point saves on quit." +
                                "\nReduces defense to 0 and slows movement while equipped and setting your warp point." +
                                "\nChannel time is four seconds and you cannot move during the channel.");
        }
        public override bool CanUseItem(Player player) {

            if (!player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                if (!player.GetModPlayer<tsorcRevampPlayer>().townWarpSet)
                {
                    Main.NewText("You haven't set a location!", 255, 240, 20);
                    return false;
                }
                else if (player.GetModPlayer<tsorcRevampPlayer>().townWarpWorld != Main.worldID)
                {
                    Main.NewText("This mirror is set in a different world!", 255, 240, 20);
                    return false;
                }
            }
            return base.CanUseItem(player);
        }
        public override void UseStyle(Player player) {
            if (checkWarpLocation2(player.GetModPlayer<tsorcRevampPlayer>().townWarpX, player.GetModPlayer<tsorcRevampPlayer>().townWarpY) || player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                if (player.itemTime > (int)(item.useTime / PlayerHooks.TotalUseTimeMultiplier(player, item)) / 4)
                {
                    player.velocity = Vector2.Zero;
                    player.gravDir = 1;
                    player.fallStart = (int)player.Center.Y;
                    player.position.Y -= 0.4f;
                }
                if (Main.rand.NextBool() && player.itemTime != 0)
                { //ambient dust during use

                    // position, width, height, type, speed.X, speed.Y, alpha, color, scale
                    Dust.NewDust(player.position, player.width, player.height, 57, 0f, 0.5f, 150, default(Color), 1f + (float)(4 - (item.useAnimation / (item.useAnimation - player.itemTime))));
                }

                if (player.itemTime == 0)
                {
                    Main.NewText("Picking up where you left off...", 255, 240, 20);
                    player.itemTime = (int)(item.useTime / PlayerHooks.TotalUseTimeMultiplier(player, item));
                }
                else if (player.itemTime == (int)(item.useTime / PlayerHooks.TotalUseTimeMultiplier(player, item)) / 4)
                {
                    Main.PlaySound(SoundID.Item60);


                    for (int dusts = 0; dusts < 70; dusts++)
                    { //dusts on tp (source)
                        Dust.NewDust(player.position, player.width, player.height, 57, player.velocity.X * 0.5f, (player.velocity.Y * 0.5f) + 0.5f, 150, default(Color), 1.5f);
                    }

                    //destroy grapples
                    player.grappling[0] = -1;
                    player.grapCount = 0;
                    for (int p = 0; p < 1000; p++)
                    {
                        if (Main.projectile[p].active && Main.projectile[p].owner == player.whoAmI && Main.projectile[p].aiStyle == 7)
                        {
                            Main.projectile[p].Kill();
                        }
                    }

                    Vector2 destination;
                    if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
                    {
                        destination.X = (float)(4182 * 16) - (float)((float)player.width / 2.0);
                        destination.Y = (float)(714 * 16) - (float)player.height;
                    }
                    else
                    {
                        destination.X = (float)(player.GetModPlayer<tsorcRevampPlayer>().townWarpX * 16) - (float)((float)player.width / 2.0);
                        destination.Y = (float)(player.GetModPlayer<tsorcRevampPlayer>().townWarpY * 16) - (float)player.height;
                    }
                    player.SafeTeleport(destination);
                    player.AddBuff(ModContent.BuffType<Buffs.Crippled>(), 1); //1

                    for (int dusts = 0; dusts < 70; dusts++)
                    { //dusts on tp (destination)
                        Dust.NewDust(player.position, player.width, player.height, 57, player.velocity.X * 0.5f, (player.velocity.Y * 0.5f) + 0.5f * 0.5f, 150, default(Color), 1.5f);
                    }

                }
            }
            else
            {
                Main.NewText("Your warp location is broken! Please file a bug report!", 255, 240, 20);
            }

        }

        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            Player player = Main.LocalPlayer;
            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse) {
                //only insert the tooltip if the last valid line is not the name, the "Equipped in social slot" line, or the "No stats will be gained" line (aka do not insert if in a vanity slot)
                int ttindex = tooltips.FindLastIndex(t => t.mod == "Terraria" && t.Name != "ItemName" && t.Name != "Social" && t.Name != "SocialDesc" && !t.Name.Contains("Prefix"));
                if (ttindex != -1) {// if we find one
                    //insert the extra tooltip line
                    tooltips.Insert(ttindex + 1, new TooltipLine(mod, "BotCNerfedVillageMirror", "Will always take the [c/6d8827:Bearer of the Curse] to the center of the village"));
                    
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


