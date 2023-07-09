using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items
{

    public class Darksign : ModItem
    {
        public static float BotCSoulDropAmplifier = 20f;
        public static float BotCMaxHPLossPercentage = 20f;
        public static float FlaskMaxManaScaling = 12f;
        public static float BotCMagicDamageAmplifier = 15f;
        public static float BotCMagicAttackSpeedAmplifier = 15f;
        public static float MaxMinionDamageReduction = 90f;
        public static float MinionDamageReductionDecrease = 22f;
        public static int WhipDebuffCounterCap = 5;
        public static float MinionDamageCeiling = MinionDamageReductionDecrease * WhipDebuffCounterCap + 10f;
        public static float WhipRangeReduction = 33f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(BotCSoulDropAmplifier, BotCMaxHPLossPercentage, FlaskMaxManaScaling, BotCMagicDamageAmplifier, BotCMagicAttackSpeedAmplifier, MaxMinionDamageReduction, MinionDamageReductionDecrease, WhipDebuffCounterCap, MinionDamageCeiling, WhipRangeReduction);
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemNoGravity[Item.type] = true; // Makes item float in world.
            Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(20, 6));
        }

        public override void SetDefaults()
        {
            Item refItem = new Item();
            refItem.SetDefaults(ItemID.SoulofSight);
            Item.width = 30;
            Item.height = 40;
            Item.maxStack = 1;
            Item.value = 1;
            Item.consumable = false;
            Item.useAnimation = 60;
            Item.useTime = 60;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noUseGraphic = true;
            Item.rare = ItemRarityID.Red; // Mainly for colour consistency.
        }
        public override void PostUpdate()
        {
            Lighting.AddLight(Item.Center, 0.7f, 0.4f, 0.1f);
            if (Main.rand.NextBool(8))
            {
                Dust dust = Main.dust[Dust.NewDust(Item.position, Item.width, Item.height, 6, 0f, -5f, 50, default, Main.rand.NextFloat(1f, 1.5f))];
                dust.noGravity = true;
            }
        }
        public override bool GrabStyle(Player player)
        { //make pulling souls through walls more consistent
            Vector2 vectorItemToPlayer = player.Center - Item.Center;
            Vector2 movement = vectorItemToPlayer.SafeNormalize(default) * 0.75f;
            Item.velocity = Item.velocity + movement;
            return true;
        }
        public override void GrabRange(Player player, ref int grabRange)
        {
            grabRange *= (2 + Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().SoulReaper);
        }
        public override bool CanUseItem(Player player)
        {
            if (Main.expertMode || Main.masterMode)
            {
                return true;
            } else
            {
                return false;
            }
        }
        public override bool? UseItem(Player player) // Won't consume item without this
        {
            player.GetModPlayer<tsorcRevampCeruleanPlayer>().ceruleanChargesMax = player.GetModPlayer<tsorcRevampEstusPlayer>().estusChargesMax * 2;
            player.GetModPlayer<tsorcRevampCeruleanPlayer>().ceruleanManaGain = ((player.GetModPlayer<tsorcRevampEstusPlayer>().estusHealthGain - tsorcRevampEstusPlayer.DefaultEstusHealthGain) * 2) + 120;
            return true;
        }
        public override void UseStyle(Player player, Rectangle rectangle)
        {
            // Each frame, make some dust
            if (Main.rand.NextBool(2))
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




            if (player.itemTime == 1)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 1f, PitchVariance = 0.3f }, player.Center); // Plays sound.
                if (Main.player[Main.myPlayer].whoAmI == player.whoAmI)
                {
                    //player.QuickSpawnItem(mod.ItemType("DarkSoul"), 2000); // Gives player souls.
                    Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Top, player.velocity, ProjectileID.DD2ExplosiveTrapT2Explosion, 250, 15, 0);
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

                //Main.NewText("What has been done cannot be undone", 200, 60, 40);
                if (player.whoAmI == Main.LocalPlayer.whoAmI)
                {
                    if (!player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
                    {
                        player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse = true;
                        Main.NewText(Language.GetTextValue("Mods.tsorcRevamp.Items.Darksign.Enabled"), 200, 60, 40);

                    }
                    else
                    {
                        player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse = false;
                        Main.NewText(Language.GetTextValue("Mods.tsorcRevamp.Items.Darksign.Disabled"), 200, 60, 40);

                    }
                }
                if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
                {
                    player.statManaMax2 += player.statManaMax2;
                    player.statMana += player.statMana;
                }
                else
                {
                    player.statMana -= player.statMana / 2;
                }
                //Main.NewText("Stamina regen rate: " + player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceRegenRate);
                //Main.NewText("Stamina regen gain mult: " + player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult);
                //Main.NewText("Stamina: " + player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent);
            }
        }


        public int itemframe = 0;
        public int itemframeCounter = 0;

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Item[Item.type];
            var myrectangle = texture.Frame(1, 6, 0, itemframe);
            spriteBatch.Draw(texture, Item.Center - Main.screenPosition, myrectangle, Color.White, 0f, new Vector2(18, 24), Item.scale, SpriteEffects.None, 0);

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