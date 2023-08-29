using Microsoft.Xna.Framework;
using System.Collections.Generic;
using tsorcRevamp.Buffs.Debuffs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Items;

public class GreatMagicMirror : ModItem
{



    public static int playerXLocation(Player player)
    {
        return (int)((player.position.X + player.width / 2.0 + 8.0) / 16.0);
    }
    public static int playerYLocation(Player player)
    {
        return (int)((player.position.Y + player.height) / 16.0);
    }

    public static bool checkWarpLocation(int x, int y)
    {
        if (x < 10 || x > Main.maxTilesX - 10 || y < 10 || y > Main.maxTilesY - 10)
        {
            Main.NewText("Your warp is outside the world!", 255, 240, 20);
            return false;
        }

        for (int sanityX = x - 1; sanityX < x; sanityX++)
        {
            for (int sanityY = y - 1; sanityY < y; sanityY++)
            {
                Tile tile = Framing.GetTileSafely(sanityX, sanityY);
                if (tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType])
                {
                    WorldGen.KillTile(sanityX, sanityY);
                }

            }
        }
        return true;
    }

    double warpSetDelay;
    public override void SetDefaults()
    {
        Item.CloneDefaults(ItemID.MagicMirror);
        Item.accessory = true;
        Item.value = 25000;
        Item.useTime = 300;
        Item.useAnimation = 300;

    }

    public override void SetStaticDefaults()
    {
        Tooltip.SetDefault("Equip this in an accessory slot anywhere to create a new warp point." +
                            "\nActivate by left-clicking the mirror in your toolbar." +
                            "\nWarp point saves on quit." +
                            "\nReduces defense to 0 and slows movement while equipped and setting your warp point." +
                            "\nChannel time is five seconds and you cannot move during the channel.");
    }

    public override bool CanUseItem(Player player)
    {

        if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                Main.NewText("The Curse prevents you from using this!", Color.OrangeRed);
            }
            return false;
        }
        if (tsorcRevampWorld.BossAlive)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                Main.NewText("Can not be used while a boss is alive!", Color.Yellow);
            }
            return false;
        }
        if (!player.GetModPlayer<tsorcRevampPlayer>().warpSet)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                Main.NewText("You haven't set a location!", 255, 240, 20);
            }
            return false;
        }
        else if (player.GetModPlayer<tsorcRevampPlayer>().warpWorld != Main.worldID)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                Main.NewText("This mirror is set in a different world!", 255, 240, 20);
            }
            return false;
        }
        return base.CanUseItem(player);
    }
    public override void UseStyle(Player player, Rectangle rectangle)
    {
        if (player != Main.LocalPlayer)
        {
            return;
        }

        if (checkWarpLocation(player.GetModPlayer<tsorcRevampPlayer>().warpX, player.GetModPlayer<tsorcRevampPlayer>().warpY))
        {
            if (player.itemTime > (int)(Item.useTime / PlayerLoader.UseTimeMultiplier(player, Item)) / 4)
            {
                player.velocity = Vector2.Zero;
                player.gravDir = 1;
                player.fallStart = (int)player.Center.Y;
                player.position.Y -= 0.4f;
            }
            if (Main.rand.NextBool() && player.itemTime != 0)
            { //ambient dust during use

                // position, width, height, type, speed.X, speed.Y, alpha, color, scale
                Dust.NewDust(player.position, player.width, player.height, 57, 0f, 0.5f, 150, default(Color), 1f + (float)(4 - (Item.useAnimation / (Item.useAnimation - player.itemTime))));
            }

            if (player.itemTime == 0)
            {
                Main.NewText("Picking up where you left off...", 255, 240, 20);
                player.itemTime = (int)(Item.useTime / PlayerLoader.UseTimeMultiplier(player, Item));
            }
            else if (player.itemTime == (int)(Item.useTime / PlayerLoader.UseTimeMultiplier(player, Item)) / 4)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item60);


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
                destination.X = (float)(player.GetModPlayer<tsorcRevampPlayer>().warpX * 16) - (float)((float)player.width / 2.0);
                destination.Y = (float)(player.GetModPlayer<tsorcRevampPlayer>().warpY * 16) - (float)player.height;
                player.SafeTeleport(destination);
                player.AddBuff(ModContent.BuffType<Crippled>(), 1); //1

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

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        Player player = Main.LocalPlayer;
        if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
        {
            //only insert the tooltip if the last valid line is not the name, the "Equipped in social slot" line, or the "No stats will be gained" line (aka do not insert if in a vanity slot)
            int ttindex = tooltips.FindLastIndex(t => t.Mod == "Terraria" && t.Name != "ItemName" && t.Name != "Social" && t.Name != "SocialDesc" && !t.Name.Contains("Prefix"));
            if (ttindex != -1)
            {// if we find one
             //insert the extra tooltip line

                tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "BotCNoGreaterMM", "[c/ca1e00:The curse prevents you from using this!]"));

            }
        }
    }

    public override void UpdateAccessory(Player player, bool hideVisual)
    {
        if (player != Main.LocalPlayer)
        {
            return;
        }
        player.moveSpeed -= 2f;
        player.statDefense -= player.statDefense;
        if (!player.GetModPlayer<tsorcRevampPlayer>().warpSet)
        {
            player.GetModPlayer<tsorcRevampPlayer>().warpX = playerXLocation(player);
            player.GetModPlayer<tsorcRevampPlayer>().warpY = playerYLocation(player);
            player.GetModPlayer<tsorcRevampPlayer>().warpWorld = Main.worldID;
            player.GetModPlayer<tsorcRevampPlayer>().warpSet = true;
            Main.NewText("New warp location set!", 255, 240, 30);
        }
        else
        {
            double timeDifference = Main.time - warpSetDelay;
            if ((timeDifference > 120.0) || (timeDifference < 0.0))
            {
                player.GetModPlayer<tsorcRevampPlayer>().warpX = playerXLocation(player);
                player.GetModPlayer<tsorcRevampPlayer>().warpY = playerYLocation(player);
                player.GetModPlayer<tsorcRevampPlayer>().warpWorld = Main.worldID;
                player.GetModPlayer<tsorcRevampPlayer>().warpSet = true;
                warpSetDelay = Main.time;
                Main.NewText("New warp location set!", 255, 240, 30);
            }
        }
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.MagicMirror, 1);
        recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 1000);
        recipe.AddTile(TileID.DemonAltar);

        recipe.Register();
    }

}


