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
        public const int MaxTeleportDistanceTiles = 100;
        public const float MaxTeleportDistance = MaxTeleportDistanceTiles * 16f;
        public const int RangeIndicatorVisibilityTiles = 100;
        public const float RangeIndicatorVisibilityDistance = RangeIndicatorVisibilityTiles * 16f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(ChannelTime / 60, MaxTeleportDistanceTiles);
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

        public static bool IsInTeleportRange(Player player)
        {
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            return Vector2.Distance(player.Center, modPlayer.greatMirrorWarpPoint) <= MaxTeleportDistance;
        }

        public static bool ShouldDrawRangeIndicator(Player player)
        {
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            float distanceFromRing = System.Math.Abs(Vector2.Distance(player.Center, modPlayer.greatMirrorWarpPoint) - MaxTeleportDistance);
            return modPlayer.warpSet
                && modPlayer.warpWorld == Main.worldID
                && distanceFromRing <= RangeIndicatorVisibilityDistance;
        }

        private static void SpawnWarpPointMarker(Player player, Item item)
        {
            if (Main.netMode == NetmodeID.Server || Main.gameMenu || Main.mapFullscreen || player.HeldItem != item || !ShouldDrawRangeIndicator(player))
            {
                return;
            }

            Vector2 center = player.GetModPlayer<tsorcRevampPlayer>().greatMirrorWarpPoint;
            // Shader ring version. Keeping this here because it looked good and may be useful again.
            // int ringType = ModContent.ProjectileType<GreatMagicMirrorRangeRing>();
            // bool ringFound = false;
            // for (int i = 0; i < Main.maxProjectiles; i++)
            // {
            //     Projectile projectile = Main.projectile[i];
            //     if (projectile.active && projectile.owner == player.whoAmI && projectile.type == ringType)
            //     {
            //         projectile.timeLeft = 2;
            //         projectile.Center = center;
            //         ringFound = true;
            //         break;
            //     }
            // }
            //
            // if (!ringFound)
            // {
            //     Projectile.NewProjectile(item.GetSource_FromThis(), center, Vector2.Zero, ringType, 0, 0f, player.whoAmI, player.whoAmI);
            // }

            if (Main.GameUpdateCount % 3 != 0)
            {
                return;
            }

            float pulse = 0.5f + 0.5f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 3f);
            float radius = 32f + 4f * pulse;
            float rotation = Main.GlobalTimeWrappedHourly * 1.7f;
            Rectangle screenBounds = new Rectangle((int)Main.screenPosition.X - 96, (int)Main.screenPosition.Y - 96, Main.screenWidth + 192, Main.screenHeight + 192);

            for (int i = 0; i < 12; i++)
            {
                Vector2 direction = (rotation + MathHelper.TwoPi * i / 12f).ToRotationVector2();
                Vector2 dustPosition = center + direction * MaxTeleportDistance;
                if (!screenBounds.Contains(dustPosition.ToPoint()))
                {
                    continue;
                }

                Dust dust = Dust.NewDustPerfect(dustPosition, DustID.GreenTorch, direction.RotatedBy(MathHelper.PiOver2) * 0.25f, 120, new Color(140, 255, 190), 1.15f);
                dust.noGravity = true;
                dust.fadeIn = 0.8f;
            }

            if (screenBounds.Contains(center.ToPoint()))
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 direction = (rotation + MathHelper.TwoPi * i / 3f).ToRotationVector2();
                    Dust dust = Dust.NewDustPerfect(center + direction * radius, DustID.GreenTorch, direction.RotatedBy(MathHelper.PiOver2) * 0.25f, 120, new Color(140, 255, 190), 1.15f);
                    dust.noGravity = true;
                    dust.fadeIn = 0.8f;
                }

                if (Main.rand.NextBool(4))
                {
                    Dust centerDust = Dust.NewDustPerfect(center + Main.rand.NextVector2Circular(12f, 12f), DustID.MagicMirror, Vector2.Zero, 150, new Color(210, 255, 225), 0.9f);
                    centerDust.noGravity = true;
                }
            }
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
            // BotC-only restriction: the Curse blocks free-form teleportation. Unkindled and Classic
            // players can use the Great Magic Mirror normally. This was previously commented out while
            // the mod had a single BotC tier; with the tri-state Unkindled split, "convenience" features
            // like this stay on for the new default mode and only get restricted at the hard tier.
            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                if (player.whoAmI == Main.myPlayer)
                {
                    Main.NewText(LangUtils.GetTextValue("Items.GreatMagicMirror.BotCDisabled"), Color.OrangeRed);
                }
                return false;
            }
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
            if (!IsInTeleportRange(player))
            {
                if (player.whoAmI == Main.myPlayer)
                {
                    Main.NewText(LangUtils.GetTextValue("Items.GreatMagicMirror.TooFar", MaxTeleportDistanceTiles), 255, 240, 20);
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
                if (!IsInTeleportRange(player))
                {
                    Main.NewText(LangUtils.GetTextValue("Items.GreatMagicMirror.TooFar", MaxTeleportDistanceTiles), 255, 240, 20);
                    player.itemTime = 0;
                    player.itemAnimation = 0;
                    return;
                }

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
            // BotC tier: warn the player the mirror won't function for them.
            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                //only insert the tooltip if the last valid line is not the name, the "Equipped in social slot" line, or the "No stats will be gained" line (aka do not insert if in a vanity slot)
                int ttindex = tooltips.FindLastIndex(t => t.Mod == "Terraria" && t.Name != "ItemName" && t.Name != "Social" && t.Name != "SocialDesc" && !t.Name.Contains("Prefix"));
                if (ttindex != -1)
                {
                    tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "BotCNoGreaterMM", LangUtils.GetTextValue("Items.GreatMagicMirror.BotCDisabled")));
                }
            }
        }

        public override void UpdateInventory(Player player)
        {
            if (player == Main.LocalPlayer)
            {
                SpawnWarpPointMarker(player, Item);
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

            SpawnWarpPointMarker(player, Item);
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


