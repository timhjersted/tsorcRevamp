using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Utilities;
using static tsorcRevamp.Items.Tools.GreatMagicMirror;

namespace tsorcRevamp.Items.Tools
{
    public class VillageMirror : ModItem
    {
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(ChannelTime / 60);
        double warpSetDelay2;

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
            if (tsorcRevampWorld.BossAlive)
            {
                Main.NewText(LangUtils.GetTextValue("CommonItemTooltip.UnusableDuringBoss"), Color.Yellow);
                return false;
            }
            if (!player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                if (!player.GetModPlayer<tsorcRevampPlayer>().townWarpSet)
                {
                    Main.NewText(LangUtils.GetTextValue("Items.GreatMagicMirror.NoLocation"), 255, 240, 20);
                    return false;
                }
                else if (player.GetModPlayer<tsorcRevampPlayer>().townWarpWorld != Main.worldID)
                {
                    Main.NewText(LangUtils.GetTextValue("Items.GreatMagicMirror.WrongWorld"), 255, 240, 20);
                    return false;
                }
            }
            return base.CanUseItem(player);
        }
        public override void UseStyle(Player player, Rectangle rectangle)
        {
            if (checkWarpLocation(player.GetModPlayer<tsorcRevampPlayer>().townWarpX, player.GetModPlayer<tsorcRevampPlayer>().townWarpY) || player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
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

                    player.PotionOfReturnOriginalUsePosition = player.Bottom;
                    player.SafeTeleport(destination);
                    player.PotionOfReturnHomePosition = player.Bottom;


                    for (int dusts = 0; dusts < 70; dusts++)
                    { //dusts on tp (destination)
                        Dust.NewDust(player.position, player.width, player.height, 57, player.velocity.X * 0.5f, (player.velocity.Y * 0.5f) + 0.5f * 0.5f, 150, default(Color), 1.5f);
                    }

                }
            }
            else
            {
                Main.NewText(LangUtils.GetTextValue("Items.GreatMagicMirror.Oops"), 255, 240, 20);
            }

        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.moveSpeed -= 2f;
            player.statDefense -= player.statDefense;

            double timeDifference2 = Main.time - warpSetDelay2;
            if ((timeDifference2 > 120.0) || (timeDifference2 < 0.0))
            {
                Rectangle villageRect = new Rectangle(4013 * 16, 608 * 16, 500 * 16, 200 * 16);
                if (villageRect.Contains(player.Center.ToPoint()))
                {
                    player.GetModPlayer<tsorcRevampPlayer>().townWarpX = playerXLocation(player);
                    player.GetModPlayer<tsorcRevampPlayer>().townWarpY = playerYLocation(player);
                    player.GetModPlayer<tsorcRevampPlayer>().townWarpWorld = Main.worldID;
                    player.GetModPlayer<tsorcRevampPlayer>().townWarpSet = true;
                    warpSetDelay2 = Main.time;
                    Main.NewText(LangUtils.GetTextValue("Items.GreatMagicMirror.NewLocation"), 255, 240, 30);
                }
                else
                {
                    warpSetDelay2 = Main.time;
                    Main.NewText(LangUtils.GetTextValue("Items.GreatMagicMirror.Invalid"), 255, 240, 30);
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


