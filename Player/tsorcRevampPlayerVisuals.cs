using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs;
using tsorcRevamp.Projectiles;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp
{
    //Visuals
    public partial class tsorcRevampPlayer
    {
        public float effectRadius = 150;
        public float effectIntensity = 0.1f;
        public float expandLimit = 1;
        public float collapseSpeed = 1.05f;
        public int collapseDelay = 0;
        public static int baseRadius = 150;

        public override void DrawEffects(PlayerDrawSet drawInfo, ref float r, ref float g, ref float b, ref float a, ref bool fullBright)
        {

            //This is going here, because unlike most hooks this one keeps running even when the game is paused via AutoPause
            if (Main.mouseItem.type == ModContent.ItemType<DarkSoul>())
            {
                Player.chest = -1;
            }

            if (!Main.gameMenu)
            {
                if (Player.HasBuff(ModContent.BuffType<Frostbite>()))
                {
                    r *= 0.3804f;
                    g *= 0.6902f;
                    b *= 254 / 255;
                }
                if (Shockwave)
                {
                    r *= 0.7372f;
                    g *= 0.5176f;
                    b *= 0.3686f;
                }
            }
        }

        public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
        {
            
        }
        public override void FrameEffects()
        {
            if (MiakodaNewBoost)
            {
                Player.armorEffectDrawShadow = true;
            }
        }

        //Only one aura can be applied, highest in the enum list takes priority
        public void SetAuraState(tsorcAuraState state)
        {
            if (Player.GetModPlayer<tsorcRevampPlayer>().CurrentAuraState < state)
            {
                Player.GetModPlayer<tsorcRevampPlayer>().CurrentAuraState = state;
            }
        }
    }

    class tsorcRevampPlayerReflectionDrawLayers : PlayerDrawLayer
    {
        public override Position GetDefaultPosition()
        {
            return new AfterParent(PlayerDrawLayers.FrontAccFront);
        }
        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            return true;
        }
        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            if (drawInfo.drawPlayer.dead)
            {
                return;
            }

            List<Vector2> shatterPositions = new List<Vector2>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].type == ModContent.ProjectileType<Projectiles.Summon.ShatteredReflectionProjectile>() && Main.projectile[i].owner == drawInfo.drawPlayer.whoAmI && Main.projectile[i].active)
                {
                    shatterPositions.Add(Main.projectile[i].Center - Main.player[Main.projectile[i].owner].Center);
                }
            }

            for (int i = 0; i < shatterPositions.Count; i++)
            {
                for (int j = 0; j < drawInfo.DrawDataCache.Count; j++)
                {
                    DrawData currentData = drawInfo.DrawDataCache[j];
                    Main.EntitySpriteDraw(currentData.texture, currentData.position + shatterPositions[i], currentData.sourceRect, currentData.color, drawInfo.drawPlayer.fullRotation, currentData.origin, currentData.scale, currentData.effect, 0);
                }
            }
        }
    }

    class tsorcRevampPlayerHeldDrawLayers : PlayerDrawLayer
    {
        public override Position GetDefaultPosition()
        {
            return new AfterParent(PlayerDrawLayers.HeldItem);
        }

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            ModLoader.TryGetMod("tsorcRevamp", out Mod mod);
            if (drawInfo.drawPlayer.HeldItem.ModItem != null)
                return drawInfo.drawPlayer.HeldItem.ModItem.Mod == mod;
            else if (drawInfo.drawPlayer.GetModPlayer<tsorcRevampEstusPlayer>().isDrinking)
                return true;
            else if (drawInfo.drawPlayer.GetModPlayer<tsorcRevampCeruleanPlayer>().isDrinking)
                return true;
            return false;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            tsorcRevampPlayer modPlayer = drawInfo.drawPlayer.GetModPlayer<tsorcRevampPlayer>();
            Item thisItem = modPlayer.Player.HeldItem;

            #region Glaive Beam HeldItem glowmask and animation
            //If the player is holding the glaive beam
            if (thisItem.type == ModContent.ItemType<Items.Weapons.Ranged.GlaiveBeam>())
            {
                //And the projectile that creates the laser exists
                if (modPlayer.Player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.GlaiveBeamLaser>()] > 0)
                {
                    Projectiles.GlaiveBeamLaser heldBeam;

                    //Then find the laser in the projectile array
                    for (int i = 0; i < Main.projectile.Length; i++)
                    {
                        //If it found it, we're in business.
                        if (Main.projectile[i].type == ModContent.ProjectileType<Projectiles.GlaiveBeamLaser>() && Main.projectile[i].owner == modPlayer.Player.whoAmI)
                        {
                            //Get the transparent texture
                            Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.GlaiveBeamHeldGlowmask];

                            //Get the animation frame
                            heldBeam = (Projectiles.GlaiveBeamLaser)Main.projectile[i].ModProjectile;
                            int textureFrames = 10;
                            int frameHeight = (int)texture.Height / textureFrames;
                            int startY = frameHeight * (int)Math.Floor(9 * (heldBeam.Charge / GlaiveBeamHoldout.MaxCharge));
                            Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);

                            //Get the offsets and shift the draw position based on them
                            float textureMidpoint = texture.Height / (2 * textureFrames);
                            Vector2 drawPos = drawInfo.ItemLocation - Main.screenPosition;
                            Vector2 holdOffset = new Vector2(texture.Width / 2, textureMidpoint);
                            Vector2 originOffset = new Vector2(0, textureMidpoint);
                            ItemLoader.HoldoutOffset(drawInfo.drawPlayer.gravDir, drawInfo.drawPlayer.HeldItem.type, ref originOffset);
                            holdOffset.Y = originOffset.Y;
                            drawPos += holdOffset;

                            //Set the origin based on the offset point
                            Vector2 origin = new Vector2(-originOffset.X, textureMidpoint);

                            //Shift everything if the player is facing the other way
                            if (drawInfo.drawPlayer.direction == -1)
                            {
                            }

                            float rotation = drawInfo.drawPlayer.itemRotation;

                            if (drawInfo.drawPlayer.direction == -1)
                            {
                                rotation += MathHelper.Pi;

                                //Don't ask me why this is necessary, or why these exact values work. I don't know. I don't think i'll ever know.
                                origin.X -= 1f;
                                origin.Y -= 8f;
                            }

                            ///Draw, partner.
                            //DrawData data = new DrawData(texture, drawPos, sourceRectangle, Color.White, drawPlayer.itemRotation, origin, modPlayer.Player.HeldItem.scale, drawInfo.spriteEffects, 0);
                            //Main.playerDrawData.Add(data);

                            drawInfo.DrawDataCache.Add(new DrawData(
                                texture, // The texture to render.
                                drawPos, // Position to render at.
                                sourceRectangle, // Source rectangle.
                                Color.White, // Color.
                                rotation, // Rotation.
                                origin, // Origin. Uses the texture's center.
                                drawInfo.drawPlayer.HeldItem.scale, // Scale.
                                SpriteEffects.None, // SpriteEffects.
                                0 // 'Layer'. This is always 0 in Terraria.
                            ));

                            break;
                        }
                    }
                }
            }
            #endregion
            //Make sure it's actually being displayed, not just selected
            #region other glowmasks
            if (modPlayer.Player.itemAnimation > 0)
            {
                //Make sure it's from our mod
                if (thisItem.ModItem != null && thisItem.ModItem.Mod == ModLoader.GetMod("tsorcRevamp"))
                {
                    Texture2D texture = null;
                    if (modPlayer.Player.HeldItem.type == ModContent.ItemType<Items.Weapons.Ranged.Pulsar>())
                    {
                        texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.PulsarGlowmask];
                    }
                    if (modPlayer.Player.HeldItem.type == ModContent.ItemType<Items.Weapons.Ranged.GWPulsar>())
                    {
                        texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.GWPulsarGlowmask];
                    }
                    if (modPlayer.Player.HeldItem.type == ModContent.ItemType<Items.Weapons.Ranged.Polaris>())
                    {
                        texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.PolarisGlowmask];
                    }
                    if (modPlayer.Player.HeldItem.type == ModContent.ItemType<Items.Weapons.Ranged.ToxicCatalyzer>())
                    {
                        texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.ToxicCatalyzerGlowmask];
                    }
                    if (modPlayer.Player.HeldItem.type == ModContent.ItemType<Items.Weapons.Ranged.VirulentCatalyzer>())
                    {
                        texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.VirulentCatalyzerGlowmask];
                    }
                    if (modPlayer.Player.HeldItem.type == ModContent.ItemType<Items.Weapons.Ranged.Biohazard>())
                    {
                        texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.BiohazardGlowmask];
                    }
                    if (modPlayer.Player.HeldItem.type == ModContent.ItemType<Items.Weapons.Melee.Broadswords.MoonlightGreatsword>() && !Main.dayTime)
                    {
                        texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.MoonlightGreatswordGlowmask];
                    }
                    if (modPlayer.Player.HeldItem.type == ModContent.ItemType<Items.Weapons.Melee.Broadswords.UltimaWeapon>())
                    {
                        texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.UltimaWeaponGlowmask];
                    }
                    if (modPlayer.Player.HeldItem.type == ModContent.ItemType<Items.Weapons.Magic.Tomes.LightOfDawn>())
                    {
                        texture = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Items/Weapons/Magic/Tomes/LightOfDawnCrystal", ReLogic.Content.AssetRequestMode.ImmediateLoad);
                    }
                    if (modPlayer.Player.HeldItem.type == ModContent.ItemType<Items.Weapons.Melee.Broadswords.SeveringDusk>())
                    {
                        texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.SeveringDuskGlowmask];
                    }

                    //If it's not on the list, don't bother.
                    if (texture != null)
                    {
                        #region animation
                        //These lines also can handle animation. Since this glowmask isn't animated a few of these lines are redundant, but they serve as an example for how it could be done.
                        //It's essentially the same to all other animation, you're just picking different parts of the texture to draw.
                        //In this case animationFrame is set as a function that depends on game time, making it animate as time passes. For another example, the Glaive Beam above animates based on weapon charge.
                        int textureFrames = 1;
                        //int animationFrame = (int)Math.Floor(textureFrames * (double)(((Main.GameUpdateCount / 5) % 10) / 10));
                        //int frameHeight = (int)texture.Height / textureFrames;
                        //int startY = frameHeight * animationFrame;
                        //Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
                        float textureMidpoint = texture.Height / (2 * textureFrames);
                        #endregion

                        //Since we're not doing animation, we can actually just 
                        //sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);

                        //Get the offsets and shift the draw position based on them
                        Vector2 drawPos = drawInfo.ItemLocation - Main.screenPosition;
                        Vector2 holdOffset = new Vector2(texture.Width / 2, textureMidpoint);
                        Vector2 originOffset = new Vector2(0, textureMidpoint);

                        ItemLoader.HoldoutOffset(modPlayer.Player.gravDir, modPlayer.Player.HeldItem.type, ref originOffset);

                        holdOffset.Y = originOffset.Y;
                        drawPos += holdOffset;

                        SpriteEffects effect = SpriteEffects.None;
                        Color drawColor = Color.White;

                        //Idk why this was necessary, but it was
                        if (modPlayer.Player.HeldItem.type == ModContent.ItemType<Items.Weapons.Magic.Tomes.LightOfDawn>())
                        {
                            originOffset.X += 10; 
                            Color baseColor = Color.Lerp(new Color(0.1f, 0.5f, 1f), new Color(1f, 0.3f, 0.85f), (float)Math.Pow(Math.Sin((float)Main.timeForVisualEffects / 60f), 2));
                            drawColor = UsefulFunctions.ShiftColor(baseColor, (float)Main.timeForVisualEffects, 0.03f);

                            if (drawInfo.drawPlayer.direction == -1)
                            {
                                effect = SpriteEffects.FlipVertically;
                            }
                        }

                        //Set the origin based on the offset point
                        Vector2 origin = new Vector2(-originOffset.X, textureMidpoint);

                        float rotation = drawInfo.drawPlayer.itemRotation;

                        if (drawInfo.drawPlayer.direction == -1)
                        {
                            rotation += MathHelper.Pi;
                        }

                        //Sword+stab support
                        if (modPlayer.Player.HeldItem.useStyle == ItemUseStyleID.Swing || modPlayer.Player.HeldItem.useStyle == ItemUseStyleID.Thrust)
                        {
                            drawPos -= new Vector2(modPlayer.Player.HeldItem.width / 2, modPlayer.Player.HeldItem.height / 2);
                            origin.Y = modPlayer.Player.HeldItem.height;

                            //Reversed grav fix support
                            if (drawInfo.drawPlayer.gravDir != 1 && ModContent.GetInstance<tsorcRevampConfig>().GravityFix)
                            {
                                origin.Y = 0;
                            }

                            //No clue why this it only needs to be rotated a quarter of a turn when facing left. It does, though.
                            if (drawInfo.drawPlayer.direction == -1)
                            {
                                rotation += MathHelper.PiOver2;
                            }
                        }


                        


                        //DrawData data = new DrawData(texture, drawPos, sourceRectangle, Color.White, drawPlayer.itemRotation, origin, modPlayer.Player.HeldItem.scale, drawInfo.spriteEffects, 3);
                        //Main.playerDrawData.Add(data);

                        drawInfo.DrawDataCache.Add(new DrawData(
                            texture, // The texture to render.
                            drawPos, // Position to render at.
                            null, // Source rectangle.
                            drawColor, // Color.
                            rotation, // Rotation.
                            origin, // Origin. Uses the texture's center.
                            drawInfo.drawPlayer.HeldItem.scale, // Scale.
                            effect, // SpriteEffects.
                            0 // 'Layer'. This is always 0 in Terraria.
                        ));
                    }
                }
            }
            #endregion

            #region estus flask
            tsorcRevampEstusPlayer estusPlayer = drawInfo.drawPlayer.GetModPlayer<tsorcRevampEstusPlayer>();
            if (!estusPlayer.Player.dead)
            {
                int estusFrameCount = 3;
                float estusScale = 0.8f;
                Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.EstusFlask];
                SpriteEffects effects = drawInfo.drawPlayer.direction > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                int rotation = drawInfo.drawPlayer.direction > 0 ? -90 : 90;
                int drawX = (int)(drawInfo.Position.X + drawInfo.drawPlayer.width / 2f - Main.screenPosition.X);
                int drawY = (int)(drawInfo.Position.Y + drawInfo.drawPlayer.height / 2f - Main.screenPosition.Y);
                int frameHeight = texture.Height / estusFrameCount;
                int frame;
                if (estusPlayer.estusChargesCurrent == estusPlayer.estusChargesMax) { frame = 0; }
                else if (estusPlayer.estusChargesCurrent == 1) { frame = 2; }
                else { frame = 1; }

                int startY = frameHeight * frame;

                Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);

                //float chargesPercentage = (float)estusPlayer.estusChargesCurrent / estusPlayer.estusChargesMax;
                //chargesPercentage = Utils.Clamp(chargesPercentage, 0f, 1f); // Clamping it to 0-1f so it doesn't go over that.

                Color newColor = Color.White * 0.8f;

                Vector2 origin = sourceRectangle.Size() / 2f;

                if (estusPlayer.estusDrinkTimer >= estusPlayer.estusDrinkTimerMax * 0.4f)
                {
                    //DrawData data = new DrawData(texture, new Vector2(drawX + (12 * drawInfo.drawPlayer.direction), drawY - 14), sourceRectangle, newColor, rotation, origin, estusScale, effects, 0);
                    //Main.playerDrawData.Add(data);

                    drawInfo.DrawDataCache.Add(new DrawData(
                            texture, // The texture to render.
                            new Vector2(drawX + (12 * drawInfo.drawPlayer.direction), drawY - 14), // Position to render at.
                            sourceRectangle, // Source rectangle.
                            newColor, // Color.
                            rotation, // Rotation.
                            origin, // Origin. Uses the texture's center.
                            estusScale, // Scale.
                            effects, // SpriteEffects.
                            0 // 'Layer'. This is always 0 in Terraria.
                        ));

                }

            }
            else
            {
                estusPlayer.isDrinking = false;
            }
            #endregion
            #region cerulean flask
            tsorcRevampCeruleanPlayer ceruleanPlayer = drawInfo.drawPlayer.GetModPlayer<tsorcRevampCeruleanPlayer>();
            if (!ceruleanPlayer.Player.dead)
            {
                int ceruleanFrameCount = 3;
                float ceruleanScale = 0.8f;
                Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.CeruleanFlask];
                SpriteEffects effects = drawInfo.drawPlayer.direction > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                int rotation = drawInfo.drawPlayer.direction > 0 ? -90 : 90;
                int drawX = (int)(drawInfo.Position.X + drawInfo.drawPlayer.width / 2f - Main.screenPosition.X);
                int drawY = (int)(drawInfo.Position.Y + drawInfo.drawPlayer.height / 2f - Main.screenPosition.Y);
                int frameHeight = texture.Height / ceruleanFrameCount;
                int frame;
                if (ceruleanPlayer.ceruleanChargesCurrent == ceruleanPlayer.ceruleanChargesMax) { frame = 0; }
                else if (ceruleanPlayer.ceruleanChargesCurrent == 1) { frame = 2; }
                else { frame = 1; }

                int startY = frameHeight * frame;

                Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);

                //float chargesPercentage = (float)estusPlayer.estusChargesCurrent / estusPlayer.estusChargesMax;
                //chargesPercentage = Utils.Clamp(chargesPercentage, 0f, 1f); // Clamping it to 0-1f so it doesn't go over that.

                Color newColor = Color.White * 0.8f;

                Vector2 origin = sourceRectangle.Size() / 2f;

                if (ceruleanPlayer.ceruleanDrinkTimer >= ceruleanPlayer.ceruleanDrinkTimerMax * 0.4f)
                {
                    //DrawData data = new DrawData(texture, new Vector2(drawX + (12 * drawInfo.drawPlayer.direction), drawY - 14), sourceRectangle, newColor, rotation, origin, estusScale, effects, 0);
                    //Main.playerDrawData.Add(data);

                    drawInfo.DrawDataCache.Add(new DrawData(
                            texture, // The texture to render.
                            new Vector2(drawX + (12 * drawInfo.drawPlayer.direction), drawY - 14), // Position to render at.
                            sourceRectangle, // Source rectangle.
                            newColor, // Color.
                            rotation, // Rotation.
                            origin, // Origin. Uses the texture's center.
                            ceruleanScale, // Scale.
                            effects, // SpriteEffects.
                            0 // 'Layer'. This is always 0 in Terraria.
                        ));

                }

            }
            else
            {
                ceruleanPlayer.isDrinking = false;
            }
            #endregion
        }
    }

    class tsorcRevampPlayerConstantDrawLayers : PlayerDrawLayer
    {

        public override Position GetDefaultPosition()
        {
            return new AfterParent(PlayerDrawLayers.HandOnAcc);
        }

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            return true;
        }


        public static Texture2D meterFull;
        public static Texture2D powerfulMeterFull;
        public static Texture2D meterEmpty;
        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            tsorcRevampPlayer modPlayer = drawInfo.drawPlayer.GetModPlayer<tsorcRevampPlayer>();
            Player drawPlayer = drawInfo.drawPlayer;
            #region mana shield
            if (modPlayer.manaShield > 0 && !modPlayer.Player.dead)
            {
                if (modPlayer.Player.statMana > Items.Accessories.Defensive.ManaShield.manaCost)
                {
                    //If they didn't have enough mana for the shield last frame but do now, play a sound to let them know it's back up
                    if (!modPlayer.shieldUp)
                    {
                        //Soundtype Item SoundStyle 28 is powerful magic cast
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item28, modPlayer.Player.position);
                        modPlayer.shieldUp = true;
                    }

                    Lighting.AddLight(modPlayer.Player.Center, 0f, 0.2f, 0.3f);

                    int shieldFrameCount = 8;
                    float shieldScale = 2.5f;

                    Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.ManaShield];
                    int drawX = (int)(drawInfo.Position.X + drawInfo.drawPlayer.width / 2f - Main.screenPosition.X);
                    int drawY = (int)(drawInfo.Position.Y + drawInfo.drawPlayer.height / 2f - Main.screenPosition.Y);
                    int frameHeight = texture.Height / shieldFrameCount;
                    int startY = frameHeight * (modPlayer.shieldFrame / 3);
                    Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
                    Color newColor = Color.White;// Lighting.GetColor((int)((drawInfo.position.X + drawPlayer.width / 2f) / 16f), (int)((drawInfo.position.Y + drawPlayer.height / 2f) / 16f));
                    Vector2 origin = sourceRectangle.Size() / 2f;

                    //DrawData data = new DrawData(texture, new Vector2(drawX, drawY), sourceRectangle, newColor, 0f, origin, shieldScale, SpriteEffects.None, 0);
                    //Main.playerDrawData.Add(data);

                    drawInfo.DrawDataCache.Add(new DrawData(
                            texture, // The texture to render.
                            new Vector2(drawX, drawY), // Position to render at.
                            sourceRectangle, // Source rectangle.
                            newColor, // Color.
                            0f, // Rotation.
                            origin, // Origin. Uses the texture's center.
                            shieldScale, // Scale.
                            SpriteEffects.None, // SpriteEffects.
                            0 // 'Layer'. This is always 0 in Terraria.
                        ));
                }
                else
                {
                    if (modPlayer.shieldUp)
                    {
                        //Soundtype Item SoundStyle 60 is the Terra Beam
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item60, modPlayer.Player.position);
                        modPlayer.shieldUp = false;
                    }
                    //If the player doesn't have enough mana to tank a hit, then draw particle effects to indicate their mana is too low for it to function.
                    //int dust = Dust.NewDust(modPlayer.Player.Center, 1, 1, 221, modPlayer.Player.velocity.X + Main.rand.Next(-3, 3), modPlayer.Player.velocity.Y, 180, Color.Cyan, 1f);
                    int dust = Dust.NewDust(new Vector2((float)modPlayer.Player.position.X, (float)modPlayer.Player.position.Y), modPlayer.Player.width, modPlayer.Player.height, DustID.AncientLight, modPlayer.Player.velocity.X, modPlayer.Player.velocity.Y, 150, Color.Teal, 1f);
                    Main.dust[dust].noGravity = true;
                    modPlayer.shieldUp = false;
                }
            }
            else
            {
                modPlayer.shieldUp = false;
            }
            #endregion

            #region stamina bar
            if (drawPlayer.whoAmI == Main.myPlayer && !Main.gameMenu)
            {
                float staminaCurrent = drawPlayer.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent;
                float staminaMax = drawPlayer.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax2;
                float staminaPercentage = (float)staminaCurrent / staminaMax;
                if (staminaPercentage < 1f && !drawPlayer.dead)
                {
                    float abovePlayer = 45f; //how far above the player should the bar be?
                    Texture2D barFill = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Textures/StaminaBar_full");
                    Texture2D barEmpty = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Textures/StaminaBar_empty");

                    //this is the position on the screen. it should remain relatively constant unless the window is resized
                    Point barOrigin = (drawPlayer.Center - new Vector2(barEmpty.Width / 2, abovePlayer) - Main.screenPosition).ToPoint();
                    //Main.NewText("" + barOrigin.X + ", " + barOrigin.Y);

                    Rectangle emptyDestination = new Rectangle(barOrigin.X, barOrigin.Y, barEmpty.Width, barEmpty.Height);

                    //empty bar has detailing, so offset the filled bar's destination
                    int padding = 5;
                    //scale the width by the stam percentage
                    Rectangle fillDestination = new Rectangle(barOrigin.X + padding, barOrigin.Y, (int)(staminaPercentage * barFill.Width), barFill.Height);

                    //draw a line at the amount of stamina needed to roll
                    float stamPercentToRoll = 30 / staminaMax;
                    Rectangle minRollStamDestination = new Rectangle(barOrigin.X + padding + (int)(stamPercentToRoll * barFill.Width), barOrigin.Y, 2, barFill.Height); //displays 2px of the bar

                    Main.spriteBatch.Draw(barEmpty, emptyDestination, Color.White);
                    Main.spriteBatch.Draw(barFill, fillDestination, Color.DodgerBlue);
                    Main.spriteBatch.Draw(barFill, minRollStamDestination, Color.White);
                }
            }
            #endregion

            #region curse meter
            if (drawPlayer.whoAmI == Main.myPlayer && !Main.gameMenu)
            {
                float curseCurrent = drawPlayer.GetModPlayer<tsorcRevampPlayer>().CurseLevel;
                float powerfulCurseCurrent = drawPlayer.GetModPlayer<tsorcRevampPlayer>().PowerfulCurseLevel;

                float curseMax = 100;
                float cursePercentage = (float)curseCurrent / curseMax;
                cursePercentage = Utils.Clamp(cursePercentage, 0f, 1f); // Clamping it to 0-1f so it doesn't go over that.

                //Main.NewText(cursePercentage);

                float powerfulCurseMax = 500;
                float powerfulCursePercentage = (float)powerfulCurseCurrent / powerfulCurseMax;
                powerfulCursePercentage = Utils.Clamp(powerfulCursePercentage, 0f, 1f); // Clamping it to 0-1f so it doesn't go over that.

                if ((cursePercentage > 0.01f || powerfulCursePercentage > 0.01f) && !drawPlayer.dead) //0f wasn't working because aparently the minimum % it sits at is 0.01f, so dumb
                {
                    float abovePlayer = 82f; //how far above the player should the bar be?
                    UsefulFunctions.EnsureLoaded(ref meterFull, "tsorcRevamp/Textures/CurseMeter_full");
                    UsefulFunctions.EnsureLoaded(ref meterFull, "tsorcRevamp/Textures/CurseMeter_powerfulFull");
                    UsefulFunctions.EnsureLoaded(ref meterFull, "tsorcRevamp/Textures/CurseMeter_empty");


                    //this is the position on the screen. it should remain relatively constant unless the window is resized
                    Point barOrigin = (drawPlayer.Center - new Vector2(meterEmpty.Width / 2, abovePlayer) - Main.screenPosition).ToPoint(); //As they are all the same size, they can use the same origin

                    Rectangle barDestination = new Rectangle(barOrigin.X, barOrigin.Y, meterEmpty.Width, meterEmpty.Height);
                    Rectangle fullBarDestination = new Rectangle(barOrigin.X, barOrigin.Y + (int)(meterFull.Height * (1 - cursePercentage)), meterEmpty.Width, (int)(meterFull.Height));
                    Rectangle powerfulFullBarDestination = new Rectangle(barOrigin.X, barOrigin.Y + (int)(powerfulMeterFull.Height * (1 - powerfulCursePercentage)), meterEmpty.Width, (int)(powerfulMeterFull.Height));


                    Main.spriteBatch.Draw(meterEmpty, barDestination, Color.White);
                    Main.spriteBatch.Draw(UsefulFunctions.Crop(meterFull, new Rectangle(0, (int)(meterFull.Height * (1 - cursePercentage)), meterFull.Width, meterFull.Height)), fullBarDestination, Color.White);
                    Main.spriteBatch.Draw(UsefulFunctions.Crop(powerfulMeterFull, new Rectangle(0, (int)(powerfulMeterFull.Height * (1 - powerfulCursePercentage)), powerfulMeterFull.Width, powerfulMeterFull.Height)), powerfulFullBarDestination, Color.White);
                }
            }
            #endregion

            #region Scorching Point
            if (drawPlayer.HeldItem.type == ModContent.ItemType<Items.Weapons.Summon.Runeterra.ScorchingPoint>() && drawPlayer.HeldItem.type != 0)
            {
                //1) Get texture
                Texture2D scorchingPointTexture = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Items/Weapons/Summon/Runeterra/ScorchingPoint_Hand");

                //2) Get the players hand position
                Vector2 drawPosition = drawPlayer.GetFrontHandPosition(Player.CompositeArmStretchAmount.None, drawPlayer.itemRotation);

                //3) Shift the draw position over a bit so it looks right. Feel free to tune this.
                Vector2 handOffset = new Vector2(12, 2);

                //Flip it if the player is facing left
                SpriteEffects effect = SpriteEffects.None;
                if (drawPlayer.direction == -1)
                {
                    effect = SpriteEffects.FlipHorizontally;
                    handOffset.X *= -1;
                }

                //If the player is on a pully, offset and rotate the sprite to match
                float drawRotation = 0;
                if (drawInfo.drawPlayer.pulley)
                {
                    //If their hand is pointed straight up, use these values
                    if (drawPlayer.pulleyDir == 2)
                    {
                        drawRotation = -MathHelper.PiOver2;
                        handOffset = new Vector2(0, -12);

                        //And flip it if they're looking left
                        if (drawPlayer.direction == -1)
                        {
                            effect = SpriteEffects.FlipVertically;
                        }
                    }
                    else
                    {
                        //If their hand is angled, use these ones
                        drawRotation = -MathHelper.PiOver4;
                        handOffset = new Vector2(10, -10);

                        //And flip it if they're looking left
                        if (drawPlayer.direction == -1)
                        {
                            effect = SpriteEffects.FlipVertically;
                            drawRotation -= MathHelper.PiOver2;
                            handOffset.X *= -1;
                        }
                    }
                }

                //Add the offset to the position
                drawPosition += handOffset;

                //3) Set up its sprite sheet source and origin variables
                Rectangle sourceRectangle = new Rectangle(0, 0, scorchingPointTexture.Width, scorchingPointTexture.Height);
                Vector2 origin = sourceRectangle.Size() / 2f;

                //4) Call the draw function with all the info
                drawInfo.DrawDataCache.Add(new DrawData(scorchingPointTexture, drawPosition - Main.screenPosition, sourceRectangle, Color.White, drawRotation, origin, 1, effect, 0));
            }
            #endregion
        }
    }

    class tsorcRevampPlayerBackDrawLayers : PlayerDrawLayer
    {
        public override Position GetDefaultPosition()
        {
            return new AfterParent(PlayerDrawLayers.Backpacks);
        }

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            return true;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            if (!drawInfo.drawPlayer.dead &&
               ((drawInfo.drawPlayer.armor[1].type == ModContent.ItemType<Items.Armors.Summon.TarantulaCarapace>() && drawInfo.drawPlayer.armor[11].type == 0) || 
               (drawInfo.drawPlayer.armor[11].type == ModContent.ItemType<Items.Armors.Summon.TarantulaCarapace>())))
            {
                Texture2D texture = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Items/Armors/Summon/TarantulaCarapace_Backpack");
                Vector2 drawPos = drawInfo.Center - Main.screenPosition;
                Vector2 drawOffset = new Vector2(0, 0);
                Rectangle sourceRectangle = drawInfo.drawPlayer.bodyFrame;
                sourceRectangle.Width = texture.Width;
                Color newColor = Lighting.GetColor((int)((drawInfo.drawPlayer.position.X + drawInfo.drawPlayer.width / 2f) / 16f), (int)((drawInfo.drawPlayer.position.Y + drawInfo.drawPlayer.height / 2f) / 16f));
                Vector2 origin = sourceRectangle.Size() / 2f;

                drawInfo.DrawDataCache.Add(new DrawData(
                        texture, // The texture to render.
                        drawPos + drawOffset, // Position to render at.
                        sourceRectangle, // Source rectangle.
                        newColor, // Color.
                        0f, // Rotation.
                        origin, // Origin. Uses the texture's center.
                        1f, // Scale.
                        SpriteEffects.None, // SpriteEffects.
                        0 // 'Layer'. This is always 0 in Terraria.
                    ));
            }
        }
    }
    public enum tsorcAuraState
    {
        None,
        Poison,
        Nebula,
        TripleThreat,
        Cataluminance,
        Spazmatism,
        Darkness,
        Light,
        Retinazer,
    };

    public class tsorcRevampPlayerAuraDrawLayers : PlayerDrawLayer
    {
        public override Position GetDefaultPosition()
        {
            return new BeforeParent(PlayerDrawLayers.BackAcc);
        }

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            return true;
        }

        public static void StartAura(Player player, float radius = -1, float intensity = 0.1f, float collapseSpeed = 1.05f)
        {
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            if (radius == -1)
            {
                modPlayer.effectRadius = tsorcRevampPlayer.baseRadius;
            }
            else
            {
                modPlayer.effectRadius = radius;
            }
            modPlayer.effectIntensity = intensity;
            modPlayer.collapseSpeed = collapseSpeed;
            
        }

        public static void HandleAura(tsorcRevampPlayer modPlayer)
        {
            if (modPlayer.collapseDelay > 0)
            {
                if (modPlayer.effectIntensity < modPlayer.expandLimit)
                {
                    modPlayer.effectIntensity *= 1.07f;
                }

                modPlayer.collapseDelay--;
            }
            else
            {
                if (modPlayer.effectIntensity > 0.1f)
                {
                    modPlayer.effectIntensity /= modPlayer.collapseSpeed;
                }

                if (modPlayer.effectRadius > tsorcRevampPlayer.baseRadius)
                {
                    modPlayer.effectRadius /= modPlayer.collapseSpeed;
                }
                else
                {
                    modPlayer.effectRadius = tsorcRevampPlayer.baseRadius;
                }
            }
            
            if(modPlayer.effectIntensity < 0.1f)
            {
                modPlayer.effectIntensity = 0.1f;
            }
            if (modPlayer.effectIntensity > modPlayer.expandLimit)
            {
                modPlayer.effectIntensity = modPlayer.expandLimit;
            }
        }


        int effectTimer;
        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            tsorcRevampPlayer modPlayer = drawInfo.drawPlayer.GetModPlayer<tsorcRevampPlayer>();
            if (drawInfo.drawPlayer.dead || modPlayer.CurrentAuraState == tsorcAuraState.None)
            {
                return;
            }
            
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            //Draw the aura and apply the shader
            switch (modPlayer.CurrentAuraState)
            {
                case tsorcAuraState.Cataluminance:
                    {
                        DrawCatAura(drawInfo);
                        break;
                    }
                case tsorcAuraState.Poison:
                    {
                        DrawAttraidiesAura(drawInfo, Color.GreenYellow);
                        break;
                    }
                case tsorcAuraState.Retinazer:
                    {
                        DrawRetAura(drawInfo);
                        break;
                    }
                case tsorcAuraState.Spazmatism:
                    {
                        DrawSpazAura(drawInfo);
                        break;
                    }
                case tsorcAuraState.Nebula:
                    {
                        DrawAttraidiesAura(drawInfo, Color.Purple * 3);
                        break;
                    }
                case tsorcAuraState.Darkness:
                    {
                        DrawAttraidiesAura(drawInfo, Color.Purple * 3);
                        break;
                    }
                case tsorcAuraState.Light:
                    {
                        DrawAttraidiesAura(drawInfo, Color.White);
                        break;
                    }
                case tsorcAuraState.TripleThreat:
                    {
                        DrawTripleThreatAura(drawInfo);
                        break;
                    }
            }

            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
        }

        public static Effect catEffect;
        void DrawCatAura(PlayerDrawSet drawInfo)
        {
            tsorcRevampPlayer modPlayer = drawInfo.drawPlayer.GetModPlayer<tsorcRevampPlayer>();


            Lighting.AddLight((int)drawInfo.drawPlayer.Center.X / 16, (int)drawInfo.drawPlayer.Center.Y / 16, 0f, 0.4f, 0.8f);
            Color shaderColor = Color.Lerp(new Color(0.1f, 0.5f, 1f), new Color(1f, 0.3f, 0.85f), (float)Math.Pow(Math.Sin((float)Main.timeForVisualEffects / 60f), 2));
            Color rgbColor = UsefulFunctions.ShiftColor(shaderColor, (float)Main.timeForVisualEffects, 0.03f);

            //Apply the shader, caching it as well
            if (catEffect == null)
            {
                catEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/CatAura", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }

            Rectangle sourceRectangle = new Rectangle(0, 0, (int)(modPlayer.effectRadius / 0.7f), (int)(modPlayer.effectRadius / 0.7f));
            Vector2 origin = sourceRectangle.Size() / 2f;



            //Pass relevant data to the shader via these parameters
            catEffect.Parameters["textureSize"].SetValue(tsorcRevamp.tNoiseTextureWavy.Width);
            catEffect.Parameters["effectSize"].SetValue(sourceRectangle.Size());
            catEffect.Parameters["effectColor"].SetValue(rgbColor.ToVector4());
            catEffect.Parameters["ringProgress"].SetValue(modPlayer.effectIntensity);
            catEffect.Parameters["fadePercent"].SetValue(0);
            float timeFactor = 1;

            catEffect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * timeFactor);

            //Apply the shader
            catEffect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.tNoiseTextureWavy, drawInfo.drawPlayer.Center - Main.screenPosition, sourceRectangle, Color.White, 0, origin, 1, SpriteEffects.None, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Rectangle baseRectangle = new Rectangle(0, 0, 200, 200);
            Vector2 baseOrigin = baseRectangle.Size() / 2f;


            //Pass relevant data to the shader via these parameters
            catEffect.Parameters["textureSize"].SetValue(tsorcRevamp.tNoiseTextureWavy.Width);
            catEffect.Parameters["effectSize"].SetValue(baseRectangle.Size());
            catEffect.Parameters["effectColor"].SetValue(rgbColor.ToVector4());
            catEffect.Parameters["ringProgress"].SetValue(modPlayer.effectIntensity);
            catEffect.Parameters["fadePercent"].SetValue(0);
            catEffect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * timeFactor);

            //Apply the shader
            catEffect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.tNoiseTextureWavy, drawInfo.drawPlayer.Center - Main.screenPosition, baseRectangle, Color.White, MathHelper.PiOver2, baseOrigin, 1, SpriteEffects.None, 0);
        }

        public static Effect attraidiesEffect;
        void DrawAttraidiesAura(PlayerDrawSet drawInfo, Color auraColor)
        {

            tsorcRevampPlayer modPlayer = drawInfo.drawPlayer.GetModPlayer<tsorcRevampPlayer>();
            Lighting.AddLight(drawInfo.drawPlayer.Center, auraColor.ToVector3());
            Rectangle baseRectangle = new Rectangle(0, 0, (int)modPlayer.effectRadius * 2, (int)modPlayer.effectRadius * 2);
            Vector2 baseOrigin = baseRectangle.Size() / 2f;
            effectTimer++;

            //Apply the shader, caching it as well
            if (attraidiesEffect == null)
            {
                attraidiesEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/AttraidiesAura", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }

            //Pass relevant data to the shader via these parameters
            attraidiesEffect.Parameters["textureSize"].SetValue(tsorcRevamp.tNoiseTextureTurbulent.Width);
            attraidiesEffect.Parameters["effectSize"].SetValue(baseRectangle.Size());

            attraidiesEffect.Parameters["effectColor1"].SetValue(UsefulFunctions.ShiftColor(auraColor, effectTimer / 25f).ToVector4());
            attraidiesEffect.Parameters["effectColor2"].SetValue(UsefulFunctions.ShiftColor(auraColor, effectTimer / 25f).ToVector4());
            attraidiesEffect.Parameters["ringProgress"].SetValue(modPlayer.effectIntensity);
            attraidiesEffect.Parameters["fadePercent"].SetValue(0);
            attraidiesEffect.Parameters["scaleFactor"].SetValue(.5f * 150);
            attraidiesEffect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 0.05f);
            attraidiesEffect.Parameters["colorSplitAngle"].SetValue(MathHelper.TwoPi);

            //Apply the shader
            attraidiesEffect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.tNoiseTextureTurbulent, drawInfo.drawPlayer.Center - Main.screenPosition, baseRectangle, Color.White, MathHelper.TwoPi, baseOrigin, 1, SpriteEffects.None, 0);
        }

        float fadeValue = 0;
        float timeFactor = 1;
        public static Effect retEffect;
        void DrawRetAura(PlayerDrawSet drawInfo)
        {
            tsorcRevampPlayer modPlayer = drawInfo.drawPlayer.GetModPlayer<tsorcRevampPlayer>();
            Lighting.AddLight((int)drawInfo.drawPlayer.Center.X / 16, (int)drawInfo.drawPlayer.Center.Y / 16, 1f, 0.4f, 0.4f);

            //Apply the shader, caching it as well
            if (retEffect == null)
            {
                retEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/RetAura", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }


            if (modPlayer.PiercingGazeCharge >= 16)
            {
                modPlayer.collapseDelay = 30;
                timeFactor = 2;
                fadeValue = -1;
            }
            else if (drawInfo.drawPlayer.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Ranged.PiercingGaze>()] != 0)
            {
                modPlayer.effectRadius = 300;
                fadeValue = -5;
                timeFactor = 5;
                modPlayer.collapseDelay = 0;
            }
            else
            {
                if(modPlayer.effectRadius > 150)
                {
                    modPlayer.effectRadius -= 5;
                }
                if (fadeValue > 0)
                {
                    fadeValue -= 0.1f;
                }
                timeFactor = 1;
            }

            Rectangle sourceRectangle = new Rectangle(0, 0, (int)(modPlayer.effectRadius / 0.7f), (int)(modPlayer.effectRadius / 0.7f));
            Vector2 origin = sourceRectangle.Size() / 2f;

            Vector3 hslColor = Main.rgbToHsl(Color.Red);
            hslColor.X += 0.03f * (float)Math.Cos(effectTimer / 250f);
            effectTimer++;
            Color rgbColor = Main.hslToRgb(hslColor);

            

            //Pass relevant data to the shader via these parameters
            retEffect.Parameters["textureSize"].SetValue(tsorcRevamp.tNoiseTextureTurbulent.Width);
            retEffect.Parameters["effectSize"].SetValue(sourceRectangle.Size());
            retEffect.Parameters["effectColor"].SetValue(rgbColor.ToVector4());
            retEffect.Parameters["ringProgress"].SetValue(modPlayer.effectIntensity);
            retEffect.Parameters["fadePercent"].SetValue(fadeValue / 2f);
            retEffect.Parameters["scaleFactor"].SetValue(0);
            retEffect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * timeFactor);

            //Apply the shader
            retEffect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.tNoiseTextureTurbulent, drawInfo.drawPlayer.Center - Main.screenPosition, sourceRectangle, Color.White, 0, origin, 1, SpriteEffects.None, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Rectangle baseRectangle = new Rectangle(0, 0, 200, 200);
            Vector2 baseOrigin = baseRectangle.Size() / 2f;


            //Pass relevant data to the shader via these parameters
            retEffect.Parameters["textureSize"].SetValue(tsorcRevamp.tNoiseTextureTurbulent.Width);
            retEffect.Parameters["effectSize"].SetValue(baseRectangle.Size());
            retEffect.Parameters["effectColor"].SetValue(rgbColor.ToVector4());
            retEffect.Parameters["ringProgress"].SetValue(modPlayer.effectIntensity);
            retEffect.Parameters["fadePercent"].SetValue(fadeValue);
            retEffect.Parameters["scaleFactor"].SetValue(.5f);
            retEffect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 0.5f * timeFactor);

            //Apply the shader
            retEffect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.tNoiseTextureTurbulent, drawInfo.drawPlayer.Center - Main.screenPosition, baseRectangle, Color.White, 0, baseOrigin, 1, SpriteEffects.None, 0);
        }


        public static Effect spazEffect;
        void DrawSpazAura(PlayerDrawSet drawInfo)
        {
            tsorcRevampPlayer modPlayer = drawInfo.drawPlayer.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.effectRadius = 200;
            Lighting.AddLight((int)drawInfo.drawPlayer.Center.X / 16, (int)drawInfo.drawPlayer.Center.Y / 16, 0f, 0.4f, 0.8f);
            
            if (drawInfo.drawPlayer.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Spears.FetidExhaust>()] != 0)
            {
                if(fadeValue > -5)
                {
                    fadeValue -= 0.2f;
                }
            }
            else
            {
                fadeValue *= 0.98f;
            }

            //Apply the shader, caching it as well
            if (spazEffect == null)
            {
                spazEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/SpazAura", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }

            Rectangle sourceRectangle = new Rectangle(0, 0, (int)(modPlayer.effectRadius / 0.7f), (int)(modPlayer.effectRadius / 0.7f));
            Vector2 origin = sourceRectangle.Size() / 2f;

            Vector3 hslColor = Main.rgbToHsl(Color.GreenYellow);

            hslColor.X += 0.03f * (float)Math.Cos(effectTimer / 25f);
            effectTimer++;
            Color rgbColor = Main.hslToRgb(hslColor);

            //Pass relevant data to the shader via these parameters
            spazEffect.Parameters["textureSize"].SetValue(tsorcRevamp.tNoiseTextureTurbulent.Width);
            spazEffect.Parameters["effectSize"].SetValue(sourceRectangle.Size());
            spazEffect.Parameters["effectColor"].SetValue(rgbColor.ToVector4());
            spazEffect.Parameters["ringProgress"].SetValue(modPlayer.effectIntensity);
            spazEffect.Parameters["fadePercent"].SetValue(fadeValue);
            spazEffect.Parameters["scaleFactor"].SetValue(0.3f);
            spazEffect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly / 4f);

            //Apply the shader
            spazEffect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.tNoiseTextureTurbulent, drawInfo.drawPlayer.Center - Main.screenPosition, sourceRectangle, Color.White, 0, origin, 1, SpriteEffects.None, 0);
        }        

        void DrawTripleThreatAura(PlayerDrawSet drawInfo)
        {
            Lighting.AddLight(drawInfo.drawPlayer.Center, Color.White.ToVector3());

            Vector3 spazColorPre = Main.rgbToHsl(Color.GreenYellow);
            spazColorPre.X += 0.01f * (float)Math.Cos(effectTimer / 25f);
            Color spazColor = Main.hslToRgb(spazColorPre);

            Vector3 retColorPre = Main.rgbToHsl(Color.Red);
            retColorPre.X += 0.01f * (float)Math.Cos(effectTimer / 250f);
            effectTimer++;
            Color retColor = Main.hslToRgb(retColorPre);

            Color catColorPre = Color.Lerp(new Color(0.1f, 0.5f, 1f), new Color(1f, 0.3f, 0.85f), (float)Math.Pow(Math.Sin((float)Main.timeForVisualEffects / 60f), 2));
            Color catColor = UsefulFunctions.ShiftColor(catColorPre, (float)Main.timeForVisualEffects, 0.01f);

            tsorcRevampPlayer modPlayer = drawInfo.drawPlayer.GetModPlayer<tsorcRevampPlayer>();
            Rectangle baseRectangle = new Rectangle(0, 0, (int)modPlayer.effectRadius * 2, (int)modPlayer.effectRadius * 2);
            Vector2 baseOrigin = baseRectangle.Size() / 2f;
            effectTimer++;

            //Apply the shader, caching it as well
            if (attraidiesEffect == null)
            {
                attraidiesEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/AttraidiesAura", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }

            //Pass relevant data to the shader via these parameters
            attraidiesEffect.Parameters["textureSize"].SetValue(tsorcRevamp.tNoiseTextureTurbulent.Width);
            attraidiesEffect.Parameters["effectSize"].SetValue(baseRectangle.Size());
            attraidiesEffect.Parameters["effectColor1"].SetValue(spazColor.ToVector4());
            attraidiesEffect.Parameters["effectColor2"].SetValue(retColor.ToVector4());
            attraidiesEffect.Parameters["ringProgress"].SetValue(modPlayer.effectIntensity);
            attraidiesEffect.Parameters["fadePercent"].SetValue(0);
            attraidiesEffect.Parameters["scaleFactor"].SetValue(.5f * 150);
            attraidiesEffect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 0.05f);
            attraidiesEffect.Parameters["colorSplitAngle"].SetValue(MathHelper.TwoPi / 3f);

            //Apply the shader
            attraidiesEffect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.tNoiseTextureTurbulent, drawInfo.drawPlayer.Center - Main.screenPosition, baseRectangle, Color.White, (float)Main.timeForVisualEffects / 25f, baseOrigin, 1, SpriteEffects.None, 0);


            attraidiesEffect.Parameters["effectColor1"].SetValue(UsefulFunctions.ShiftColor(retColor, effectTimer / 25f).ToVector4());
            attraidiesEffect.Parameters["effectColor2"].SetValue(UsefulFunctions.ShiftColor(catColor, effectTimer / 25f).ToVector4());
            attraidiesEffect.CurrentTechnique.Passes[0].Apply();
            Main.EntitySpriteDraw(tsorcRevamp.tNoiseTextureTurbulent, drawInfo.drawPlayer.Center - Main.screenPosition, baseRectangle, Color.White, ((float)Main.timeForVisualEffects / 25f) + MathHelper.TwoPi / 3f, baseOrigin, 1, SpriteEffects.None, 0);

            attraidiesEffect.Parameters["effectColor1"].SetValue(UsefulFunctions.ShiftColor(catColor, effectTimer / 25f).ToVector4());
            attraidiesEffect.Parameters["effectColor2"].SetValue(UsefulFunctions.ShiftColor(spazColor, effectTimer / 25f).ToVector4());
            attraidiesEffect.CurrentTechnique.Passes[0].Apply();
            Main.EntitySpriteDraw(tsorcRevamp.tNoiseTextureTurbulent, drawInfo.drawPlayer.Center - Main.screenPosition, baseRectangle, Color.White, ((float)Main.timeForVisualEffects / 25f) + MathHelper.TwoPi / 1.5f, baseOrigin, 1, SpriteEffects.None, 0);
        }
    }
}
