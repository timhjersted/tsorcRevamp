using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items
{

    public class Darksign : ModItem
    {
        public override bool Autoload(ref string name) => !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Consume to become the [c/6d8827:Bearer of the Curse]" +
                               "\nPlaying as the [c/6d8827:Bearer of the Curse] has the following effects:" +
                               "\nYou deal +20% damage, receive +20% more souls and your stamina recovers much faster" +
                               "\nHowever, using weapons drains stamina, life regen is halved while stamina isn't at max," +
                               "\nand each time you die you lose 20 of your max HP (doesn't drop lower than 200)" +
                               "\nShine potions have no effect, you'll have to make use of other sources of light" +
                               "\nGreater Magic Mirror use is inhibited; the Village Mirror will only" +
                               "\ntake you to the village. Additionally, instant-heal items won't heal you" +
                               "\nSeek out the Emerald Herald, perhaps she has a gift for you..." +
                               "\n[c/ca1e00:Currently toggleable and non-consumable]" +
                               "\n[c/ca1e00:Experimental mode in alpha]" +
                               "\nIf you find any bugs please report on our [c/7e61ab:Discord] server");

            ItemID.Sets.ItemNoGravity[item.type] = true; // Makes item float in world.
            Main.RegisterItemAnimation(item.type, new DrawAnimationVertical(20, 6));

        }

        public override void SetDefaults()
        {
            Item refItem = new Item();
            refItem.SetDefaults(ItemID.SoulofSight);
            item.width = 30;
            item.height = 40;
            item.maxStack = 1;
            item.value = 1;
            item.consumable = false;
            item.useAnimation = 61; // Needs to be 1 tick more than use time for it to work properly. Not sure why.
            item.useTime = 60;
            item.useStyle = ItemUseStyleID.HoldingUp;
            item.noUseGraphic = true;
            item.rare = ItemRarityID.Red; // Mainly for colour consistency.
        }
        public override void PostUpdate()
        {
            Lighting.AddLight(item.Center, 0.7f, 0.4f, 0.1f);
            if (Main.rand.Next(8) == 0)
            {
                Dust dust = Main.dust[Dust.NewDust(item.position, item.width, item.height, 6, 0f, -5f, 50, default, Main.rand.NextFloat(1f, 1.5f))];
                dust.noGravity = true;
            }
        }
        public override bool GrabStyle(Player player)
        { //make pulling souls through walls more consistent
            Vector2 vectorItemToPlayer = player.Center - item.Center;
            Vector2 movement = vectorItemToPlayer.SafeNormalize(default) * 0.75f;
            item.velocity = item.velocity + movement;
            return true;
        }
        public override void GrabRange(Player player, ref int grabRange)
        {
            grabRange *= (2 + Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().SoulReaper);
        }

        public override bool UseItem(Player player) // Won't consume item without this
        {
            //Main.NewText("What has been done cannot be undone", 200, 60, 40);
            if (player.whoAmI == Main.LocalPlayer.whoAmI)
            {
                if (!player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
                {
                    player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse = true;
                    Main.NewText("You have become the Bearer of the Curse", 200, 60, 40);

                }
                else
                {
                    player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse = false;
                    Main.NewText("The curse has been lifted", 200, 60, 40);

                }
            }
            //Main.NewText("Stamina regen rate: " + player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceRegenRate);
            //Main.NewText("Stamina regen gain mult: " + player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult);
            //Main.NewText("Stamina: " + player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent);
            return true;
        }
        public override void UseStyle(Player player)
        {
            // Each frame, make some dust
            if (Main.rand.Next(2) == 0)
            {
                Dust dust = Main.dust[Dust.NewDust(player.BottomLeft, player.width, player.height - 40, 6, 0f, -5f, 50, default, 1.2f)];
                dust.velocity.Y = Main.rand.NextFloat(-5, -2.5f);
                dust.velocity.X = Main.rand.NextFloat(-1, 1);
            }

            if (Main.rand.NextFloat() < .5f)
            {
                Dust dust = Main.dust[Dust.NewDust(player.BottomLeft, player.width, player.height - 40, 6, 0f, -5f, 100, default, 1.8f)];
                dust.velocity.Y = Main.rand.NextFloat(-5, -2.5f);
                dust.velocity.X = Main.rand.NextFloat(-1, 1);
            }

            // This sets up the itemTime correctly.
            if (player.itemTime == 0)
            {
                player.itemTime = (int)(item.useTime / PlayerHooks.TotalUseTimeMultiplier(player, item));
            }

            else if (player.itemTime == (int)(item.useTime / PlayerHooks.TotalUseTimeMultiplier(player, item)) / 2)
            {
                // This code runs once halfway through the useTime of the item. 
                Main.PlaySound(SoundID.Item20.WithVolume(1f).WithPitchVariance(.3f), player.position); // Plays sound.
                if (Main.player[Main.myPlayer].whoAmI == player.whoAmI)
                {
                    //player.QuickSpawnItem(mod.ItemType("DarkSoul"), 2000); // Gives player souls.
                    Projectile.NewProjectile(player.Top, player.velocity, ProjectileID.DD2ExplosiveTrapT2Explosion, 250, 15, 0);
                }

                for (int d = 0; d < 90; d++) // Upwards
                {
                    Dust dust = Main.dust[Dust.NewDust(player.BottomLeft, player.width, player.height - 40, 6, 0f, -5f, 30, default, Main.rand.NextFloat(1, 1.8f))]; // player.Bottom if offset to the right for some reason, player.BottomLeft is centered
                    dust.velocity.Y = Main.rand.NextFloat(-5, -0f);
                    dust.velocity.X = Main.rand.NextFloat(-1.5f, 1.5f);
                }

                for (int d = 0; d < 30; d++) // Left
                {
                    Dust dust = Main.dust[Dust.NewDust(player.BottomLeft, player.width, player.height - 55, 6, -6f, -4f, 30, default, Main.rand.NextFloat(1, 1.8f))]; // player.Bottom if offset to the right for some reason, player.BottomLeft is centered
                    dust.velocity.Y = Main.rand.NextFloat(-4, -0f);
                    dust.velocity.X = Main.rand.NextFloat(-5, -1.5f);
                }

                for (int d = 0; d < 30; d++) // Right
                {
                    Dust dust = Main.dust[Dust.NewDust(player.BottomLeft, player.width, player.height - 55, 6, 6f, -4f, 30, default, Main.rand.NextFloat(1, 1.8f))]; // player.Bottom if offset to the right for some reason, player.BottomLeft is centered
                    dust.velocity.Y = Main.rand.NextFloat(-4, -0f);
                    dust.velocity.X = Main.rand.NextFloat(5, 1.5f);
                }
            }
        }


        public int itemframe = 0;
        public int itemframeCounter = 0;

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Texture2D texture = Main.itemTexture[item.type];
            var myrectangle = texture.Frame(1, 6, 0, itemframe);
            spriteBatch.Draw(texture, item.Center - Main.screenPosition, myrectangle, Color.White, 0f, new Vector2(18, 24), item.scale, SpriteEffects.None, 0f);

            itemframeCounter++;

            if (itemframeCounter < 20)
            {
                itemframe = 0;
            }
            else if (itemframeCounter < 40)
            {
                itemframe = 1;
            }
            else if (itemframeCounter < 60)
            {
                itemframe = 2;
            }
            else if (itemframeCounter < 80)
            {
                itemframe = 3;
            }
            else if (itemframeCounter < 100)
            {
                itemframe = 4;
            }
            else if (itemframeCounter < 120)
            {
                itemframe = 5;
            }
            else
            {
                itemframeCounter = 0;
            }

            return false;
        }
    }
}