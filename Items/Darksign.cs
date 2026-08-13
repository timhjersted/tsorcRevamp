using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles;
using tsorcRevamp.Systems;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items
{

    public class Darksign : ModItem
    {
        public static float BotCSoulDropAmplifier = 20f;
        public static float BotCMaxHPLossPercentage = 20f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(BotCSoulDropAmplifier, BotCMaxHPLossPercentage);
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemNoGravity[Item.type] = true; // Makes item float in world.
            Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(20, 6));
        }
        public int ClassCounter = 1;
        public override void SetDefaults()
        {
            Item refItem = new Item();
            refItem.SetDefaults(ItemID.SoulofSight);
            Item.width = 30;
            Item.height = 40;
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
            }
            else
            {
                return false;
            }
        }
        public override bool CanRightClick()
        {
            return true;
        }
        public override void RightClick(Player player)
        {
            Item.stack++;
            ClassCounter++;
            if (ClassCounter > 4)
            {
                ClassCounter = 1;
            }
        }
        public override bool? UseItem(Player player) // Won't consume item without this
        {
            player.GetModPlayer<CeruleanFlaskPlayer>().CeruleanChargesMax = ((player.GetModPlayer<tsorcRevampEstusPlayer>().estusChargesMax - tsorcRevampEstusPlayer.DefaultEstusChargesMax) * 3) + CeruleanFlaskPlayer.DefaultCeruleanChargesMax;
            player.GetModPlayer<CeruleanFlaskPlayer>().CeruleanManaGain = player.GetModPlayer<tsorcRevampEstusPlayer>().estusHealthGain - tsorcRevampEstusPlayer.DefaultEstusHealthGain + CeruleanFlaskPlayer.DefaultCeruleanManaGain;
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
                if (Main.myPlayer == player.whoAmI)
                {
                    //player.QuickSpawnItem(mod.ItemType("DarkSoul"), 2000); // Gives player souls.
                    Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Top + new Vector2(0, -32), player.velocity, ModContent.ProjectileType<ModDD2ExplosiveTrapT2Explosion>(), 200, 15, 0);
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

                if (player.whoAmI == Main.LocalPlayer.whoAmI)
                {
                    var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
                    bool wasSoulsMode = modPlayer.SoulsMode;

                    // Cycle Classic → Unkindled → Bearer of the Curse → Classic.
                    if (!modPlayer.Unkindled && !modPlayer.BearerOfTheCurse)
                    {
                        modPlayer.Unkindled = true;
                        Main.NewText(Language.GetTextValue("Mods.tsorcRevamp.Items.Darksign.Unkindled"), 200, 60, 40);
                    }
                    else if (modPlayer.Unkindled && !modPlayer.BearerOfTheCurse)
                    {
                        modPlayer.Unkindled = false;
                        modPlayer.BearerOfTheCurse = true;
                        Main.NewText(Language.GetTextValue("Mods.tsorcRevamp.Items.Darksign.BearerOfTheCurse"), 200, 60, 40);
                    }
                    else
                    {
                        modPlayer.Unkindled = false;
                        modPlayer.BearerOfTheCurse = false;
                        Main.NewText(Language.GetTextValue("Mods.tsorcRevamp.Items.Darksign.Classic"), 200, 60, 40);
                    }

                    // Mana scales on entry/exit of Souls tier — both Unkindled and BotC get the doubled pool.
                    bool isSoulsMode = modPlayer.SoulsMode;
                    if (!wasSoulsMode && isSoulsMode)
                    {
                        player.statManaMax2 += player.statManaMax2;
                        player.statMana += player.statMana;
                    }
                    else if (wasSoulsMode && !isSoulsMode)
                    {
                        player.statMana -= player.statMana / 2;
                    }
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
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            Player player = Main.LocalPlayer;
            var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            int ttindex = tooltips.FindLastIndex(t => t.Mod == "Terraria");

            // Dynamic current-tier line — tells the player which mode they're in AND what left-clicking
            // will do next. Inserted at the top of the appended tooltips (right after the last vanilla line)
            // so it's the first thing read after the item name/description.
            string stateKey;
            if (modPlayer.BearerOfTheCurse) stateKey = "Mods.tsorcRevamp.Items.Darksign.State_BotC";
            else if (modPlayer.Unkindled) stateKey = "Mods.tsorcRevamp.Items.Darksign.State_Unkindled";
            else stateKey = "Mods.tsorcRevamp.Items.Darksign.State_Classic";
            tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "CurrentState", Language.GetTextValue(stateKey)));

            // Class-mechanic preview (right-click cycles ClassCounter). Inserted after the state line.
            switch (ClassCounter)
            {
                case 1:
                    {
                        tooltips.Insert(ttindex + 2, new TooltipLine(Mod, "LethalTempo", LangUtils.GetTextValue("Items.Darksign.Melee", (int)(modPlayer.BotCMeleeBaseAttackSpeedMult * 100f), (int)(modPlayer.BotCLethalTempoBonus * 100f), (int)(modPlayer.BotCLethalTempoBonus * modPlayer.BotCLethalTempoMaxStacks * 100f), (int)modPlayer.BotCLethalTempoMaxStacks)));
                        break;
                    }
                case 2:
                    {
                        //tooltips.Insert(ttindex + 2, new TooltipLine(Mod, "Accuracy", LangUtils.GetTextValue("Items.Darksign.Ranged", (int)(modPlayer.AccuracyGain * 100f), (int)(modPlayer.AccuracyLoss * 100f), modPlayer.AccuracyMaxFlatCrit, modPlayer.AccuracyRangedBaseCritMult + modPlayer.AccuracyMaxCritMult, modPlayer.AccuracyRangedBaseCritMult)));
                        break;
                    }
                case 3:
                    {
                        var arcaneSorceryPlayer = player.GetModPlayer<ArcaneSorceryPlayer>();
                        tooltips.Insert(ttindex + 2, new TooltipLine(Mod, "CeruleanFlask", LangUtils.GetTextValue("Items.Darksign.Magic", ArcaneSorceryPlayer.CeruleanFlaskMaxManaScaling, arcaneSorceryPlayer.MagicDamageAmp, arcaneSorceryPlayer.MagicAttackSpeedAmp)));
                        break;
                    }
                case 4:
                    {
                        tooltips.Insert(ttindex + 2, new TooltipLine(Mod, "Conqueror", LangUtils.GetTextValue("Items.Darksign.Summon", (int)(modPlayer.BotCSummonBaseDamageMult * 100f), (int)(modPlayer.BotCConquerorBonus * 100f), Math.Round((modPlayer.BotCConquerorMaxStacks * modPlayer.BotCConquerorBonus * 100f)), (int)(modPlayer.BotCConquerorMaxStacks), (int)((modPlayer.BotCFullConquerorBonusTagDuration) * 100f))));
                        break;
                    }
            }
        }
    }
}