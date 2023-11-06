using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.Tools
{
    public class GreatMagicMirror : ModItem
    {
        public static int ChannelTime = 180;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(ChannelTime / 60);
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
                Main.NewText(LangUtils.GetTextValue("Items.GreatMagicMirror.OutOfBounds"), 255, 240, 20);
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
            Item.useTime = ChannelTime;
            Item.useAnimation = ChannelTime;

        }

        public override void SetStaticDefaults()
        {
        }

        public override bool CanUseItem(Player player)
        {
            // Removing this restriction for BOTC players and making it a master-mode only restriction was part of our big vision overhaul for allowing more players to enjoy all the fun BOTC mechanics like flasks and stamina while moving the more hard core elements to master mode 
            // Just don't know how to switch the restriction to master...
            /*if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                if (player.whoAmI == Main.myPlayer)
                {
                    Main.NewText(LangUtils.GetTextValue("Items.GreatMagicMirror.BotCDisabled"), Color.OrangeRed);
                }
                return false;
            }
            */
            if (tsorcRevampWorld.BossAlive)
            {
                if (player.whoAmI == Main.myPlayer)
                {
                    Main.NewText(LangUtils.GetTextValue("CommonItemTooltip.UnusableDuringBoss"), Color.Yellow);
                }
                return false;
            }
            if (!player.GetModPlayer<tsorcRevampPlayer>().warpSet)
            {
                if (player.whoAmI == Main.myPlayer)
                {
                    Main.NewText(LangUtils.GetTextValue("Items.GreatMagicMirror.NoLocation"), 255, 240, 20);
                }
                return false;
            }
            else if (player.GetModPlayer<tsorcRevampPlayer>().warpWorld != Main.worldID)
            {
                if (player.whoAmI == Main.myPlayer)
                {
                    Main.NewText(LangUtils.GetTextValue("Items.GreatMagicMirror.WrongWorld"), 255, 240, 20);
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

            if (player.itemTime > (int)(Item.useTime / PlayerLoader.UseTimeMultiplier(player, Item)) / 4)
            {
                player.velocity.X = 0;
                player.gravDir = 1;
                player.fallStart = (int)player.Center.Y;
            }
            if (Main.rand.NextBool() && player.itemTime != 0)
            { //ambient dust during use

                // position, width, height, type, speed.X, speed.Y, alpha, color, scale
                Dust.NewDust(player.position, player.width, player.height, 57, 0f, 0.5f, 150, default(Color), 1f + (float)(4 - (Item.useAnimation / (Item.useAnimation - player.itemTime))));
            }

            if (player.itemTime == 0)
            {
                Main.NewText(LangUtils.GetTextValue("Items.GreatMagicMirror.OnUse"), 255, 240, 20);
                player.itemTime = (int)(Item.useTime / PlayerLoader.UseTimeMultiplier(player, Item));
            }
            else if (player.itemTime == (int)(Item.useTime / PlayerLoader.UseTimeMultiplier(player, Item)) / 4)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item60);


                for (int dusts = 0; dusts < 70; dusts++)
                { //dusts on tp (source)
                    Dust.NewDust(player.position, player.width, player.height, 57, player.velocity.X * 0.5f, (player.velocity.Y * 0.5f) + 0.5f, 150, default(Color), 1.5f);
                }

                player.SafeTeleport(player.GetModPlayer<tsorcRevampPlayer>().greatMirrorWarpPoint);

                for (int dusts = 0; dusts < 70; dusts++)
                { //dusts on tp (destination)
                    Dust.NewDust(player.position, player.width, player.height, 57, player.velocity.X * 0.5f, (player.velocity.Y * 0.5f) + 0.5f * 0.5f, 150, default(Color), 1.5f);
                }

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

                    tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "BotCNoGreaterMM", LangUtils.GetTextValue("Items.GreatMagicMirror.BotCDisabled")));

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
                player.GetModPlayer<tsorcRevampPlayer>().greatMirrorWarpPoint = player.Center;
                player.GetModPlayer<tsorcRevampPlayer>().warpWorld = Main.worldID;
                player.GetModPlayer<tsorcRevampPlayer>().warpSet = true;
                Main.NewText(LangUtils.GetTextValue("Items.GreatMagicMirror.NewLocation"), 255, 240, 30);
            }
            else
            {
                double timeDifference = Main.time - warpSetDelay;
                if ((timeDifference > 120.0) || (timeDifference < 0.0))
                {
                    player.GetModPlayer<tsorcRevampPlayer>().greatMirrorWarpPoint = player.Center;
                    player.GetModPlayer<tsorcRevampPlayer>().warpWorld = Main.worldID;
                    player.GetModPlayer<tsorcRevampPlayer>().warpSet = true;
                    warpSetDelay = Main.time;
                    Main.NewText(LangUtils.GetTextValue("Items.GreatMagicMirror.NewLocation"), 255, 240, 30);
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
}


