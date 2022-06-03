using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs;
using tsorcRevamp.Items;
using tsorcRevamp.Projectiles;

namespace tsorcRevamp
{
    //Visuals
    public partial class tsorcRevampPlayer
    {
        public override void DrawEffects(PlayerDrawSet drawInfo, ref float r, ref float g, ref float b, ref float a, ref bool fullBright)
        {

            //This is going here, because unlike most hooks this one keeps running even when the game is paused via AutoPause
            if (Main.mouseItem.type == ModContent.ItemType<DarkSoul>())
            {
                Player.chest = -1;
            }

            if (!Main.gameMenu)
            {
                if (Player.HasBuff(ModContent.BuffType<Chilled>()))
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
            base.ModifyDrawInfo(ref drawInfo);
        }
        public override void FrameEffects()
        {
            if (MiakodaNewBoost)
            {
                Player.armorEffectDrawShadow = true;
            }
        }
    }

    class tsorcRevampPlayerHeldDrawLayers : PlayerDrawLayer {
        public override Position GetDefaultPosition() {
            return new AfterParent(PlayerDrawLayers.HeldItem);
        }

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
            ModLoader.TryGetMod("tsorcRevamp", out Mod mod);
            if (drawInfo.drawPlayer.HeldItem.ModItem != null)
            {
                return drawInfo.drawPlayer.HeldItem.ModItem.Mod == mod;
            }
            else
            {
                return false;
            }
            
        }

        protected override void Draw(ref PlayerDrawSet drawInfo) {
            tsorcRevampPlayer modPlayer = drawInfo.drawPlayer.GetModPlayer<tsorcRevampPlayer>();
            Item thisItem = modPlayer.Player.HeldItem;

            #region Glaive Beam HeldItem glowmask and animation
            //If the player is holding the glaive beam
            if (thisItem.type == ModContent.ItemType<Items.Weapons.Ranged.GlaiveBeam>()) {
                //And the projectile that creates the laser exists
                if (modPlayer.Player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.GlaiveBeamLaser>()] > 0) {
                    //Projectiles.GlaiveBeamLaser heldBeam;

                    //Then find the laser in the projectile array
                    for (int i = 0; i < Main.projectile.Length; i++) {
                        //If it found it, we're in business.
                        if (Main.projectile[i].type == ModContent.ProjectileType<Projectiles.GlaiveBeamLaser>() && Main.projectile[i].owner == modPlayer.Player.whoAmI) {
                            //Get the transparent texture
                            Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.GlaiveBeamHeldGlowmask];

                            //Get the animation frame
                            //heldBeam = (Projectiles.GlaiveBeamLaser)Main.projectile[i].ModProjectile;
                            int textureFrames = 10;
                            //int frameHeight = (int)texture.Height / textureFrames;
                            //int startY = frameHeight * (int)Math.Floor(9 * (heldBeam.Charge / GlaiveBeamHoldout.MaxCharge));
                            //Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);

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
                            if (drawInfo.drawPlayer.direction == -1) {
                                origin.X = texture.Width + originOffset.X;
                            }

                            ///Draw, partner.
                            //DrawData data = new DrawData(texture, drawPos, sourceRectangle, Color.White, drawPlayer.itemRotation, origin, modPlayer.Player.HeldItem.scale, drawInfo.spriteEffects, 0);
                            //Main.playerDrawData.Add(data);

                            drawInfo.DrawDataCache.Add(new DrawData(
                                texture, // The texture to render.
                                drawPos, // Position to render at.
                                null, // Source rectangle.
                                Color.White, // Color.
                                0f, // Rotation.
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
            if (modPlayer.Player.itemAnimation > 0) {
                //Make sure it's from our mod
                if (thisItem.ModItem != null && thisItem.ModItem.Mod == ModLoader.GetMod("tsorcRevamp")) {
                    Texture2D texture = null;
                    if (modPlayer.Player.HeldItem.type == ModContent.ItemType<Items.Weapons.Ranged.Pulsar>()) {
                        texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.PulsarGlowmask];
                    }
                    if (modPlayer.Player.HeldItem.type == ModContent.ItemType<Items.Weapons.Ranged.GWPulsar>()) {
                        texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.GWPulsarGlowmask];
                    }
                    if (modPlayer.Player.HeldItem.type == ModContent.ItemType<Items.Weapons.Ranged.Polaris>()) {
                        texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.PolarisGlowmask];
                    }
                    if (modPlayer.Player.HeldItem.type == ModContent.ItemType<Items.Weapons.Ranged.ToxicCatalyzer>()) {
                        texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.ToxicCatalyzerGlowmask];
                    }
                    if (modPlayer.Player.HeldItem.type == ModContent.ItemType<Items.Weapons.Ranged.VirulentCatalyzer>()) {
                        texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.VirulentCatalyzerGlowmask];
                    }
                    if (modPlayer.Player.HeldItem.type == ModContent.ItemType<Items.Weapons.Ranged.Biohazard>()) {
                        texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.BiohazardGlowmask];
                    }
                    if (modPlayer.Player.HeldItem.type == ModContent.ItemType<Items.Weapons.Melee.MoonlightGreatsword>() && !Main.dayTime) {
                        texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.MoonlightGreatswordGlowmask];
                    }
                    if (modPlayer.Player.HeldItem.type == ModContent.ItemType<Items.Weapons.Melee.UltimaWeapon>()) {
                        texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.UltimaWeaponGlowmask];
                    }
                    if (modPlayer.Player.HeldItem.type == ModContent.ItemType<Items.Weapons.Melee.BarbarousThornBlade>()) {
                        texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.BarbarousThornBladeGlowmask];
                    }
                    //If it's not on the list, don't bother.
                    if (texture != null) {
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
                        //TODO this is probably needed...
                        //ItemLoader.HoldoutOffset(drawPlayer.gravDir, drawPlayer.HeldItem.type, ref originOffset);

                        holdOffset.Y = originOffset.Y;
                        drawPos += holdOffset;


                        //Set the origin based on the offset point
                        Vector2 origin = new Vector2(-originOffset.X, textureMidpoint);

                        //Sword+stab support
                        if (modPlayer.Player.HeldItem.useStyle == ItemUseStyleID.Swing || modPlayer.Player.HeldItem.useStyle == ItemUseStyleID.Thrust) {
                            drawPos -= new Vector2(modPlayer.Player.HeldItem.width / 2, modPlayer.Player.HeldItem.height / 2);
                            origin.Y = modPlayer.Player.HeldItem.height;

                            //Reversed grav fix support
                            if (drawInfo.drawPlayer.gravDir != 1 && ModContent.GetInstance<tsorcRevampConfig>().GravityFix) {
                                origin.Y = 0;
                            }
                        }


                        // Shift everything if the player is facing the other way
                        if (drawInfo.drawPlayer.direction == -1) {
                            origin.X = texture.Width + originOffset.X;
                        }

                        Dust d = Dust.NewDustPerfect(drawPos * 16, DustID.MagicMirror);
                        Dust.NewDustPerfect((drawPos + origin) * 16, DustID.MagicMirror);

                        //DrawData data = new DrawData(texture, drawPos, sourceRectangle, Color.White, drawPlayer.itemRotation, origin, modPlayer.Player.HeldItem.scale, drawInfo.spriteEffects, 3);
                        //Main.playerDrawData.Add(data);

                        drawInfo.DrawDataCache.Add(new DrawData(
                            texture, // The texture to render.
                            drawPos, // Position to render at.
                            null, // Source rectangle.
                            Color.White, // Color.
                            drawInfo.drawPlayer.itemRotation, // Rotation.
                            origin, // Origin. Uses the texture's center.
                            drawInfo.drawPlayer.HeldItem.scale, // Scale.
                            SpriteEffects.None, // SpriteEffects.
                            0 // 'Layer'. This is always 0 in Terraria.
                        ));
                    }
                }
            }
            #endregion

            #region estus flask
            tsorcRevampEstusPlayer estusPlayer = drawInfo.drawPlayer.GetModPlayer<tsorcRevampEstusPlayer>();

            if (estusPlayer.isDrinking && !estusPlayer.Player.dead) {

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

                if (estusPlayer.estusDrinkTimer >= estusPlayer.estusDrinkTimerMax * 0.4f) {
                    //DrawData data = new DrawData(texture, new Vector2(drawX + (12 * drawInfo.drawPlayer.direction), drawY - 14), sourceRectangle, newColor, rotation, origin, estusScale, effects, 0);
                    //Main.playerDrawData.Add(data);

                    drawInfo.DrawDataCache.Add(new DrawData(
                            texture, // The texture to render.
                            new Vector2(drawX + (12 * drawInfo.drawPlayer.direction), drawY - 14), // Position to render at.
                            null, // Source rectangle.
                            newColor, // Color.
                            rotation, // Rotation.
                            origin, // Origin. Uses the texture's center.
                            estusScale, // Scale.
                            effects, // SpriteEffects.
                            0 // 'Layer'. This is always 0 in Terraria.
                        ));

                }

            }
            else {
                estusPlayer.isDrinking = false;
            }
            #endregion
        }
    }

    class tsorcRevampPlayerConstantDrawLayers : PlayerDrawLayer {

        public override Position GetDefaultPosition() {
            return new AfterParent(PlayerDrawLayers.HeldItem);
        }

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
            return true;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo) {
            tsorcRevampPlayer modPlayer = drawInfo.drawPlayer.GetModPlayer<tsorcRevampPlayer>();

            #region mana shield
            if (modPlayer.manaShield > 0 && !modPlayer.Player.dead) {
                if (modPlayer.Player.statMana > Items.Accessories.ManaShield.manaCost) {
                    //If they didn't have enough mana for the shield last frame but do now, play a sound to let them know it's back up
                    if (!modPlayer.shieldUp) {
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
                            null, // Source rectangle.
                            newColor, // Color.
                            0f, // Rotation.
                            origin, // Origin. Uses the texture's center.
                            shieldScale, // Scale.
                            SpriteEffects.None, // SpriteEffects.
                            0 // 'Layer'. This is always 0 in Terraria.
                        ));
                }
                else {
                    if (modPlayer.shieldUp) {
                        //Soundtype Item SoundStyle 60 is the Terra Beam
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item60, modPlayer.Player.position);
                        modPlayer.shieldUp = false;
                    }
                    //If the player doesn't have enough mana to tank a hit, then draw particle effects to indicate their mana is too low for it to function.
                    int dust = Dust.NewDust(modPlayer.Player.Center, 1, 1, 221, modPlayer.Player.velocity.X + Main.rand.Next(-3, 3), modPlayer.Player.velocity.Y + Main.rand.Next(-3, 3), 180, Color.Cyan, 1f);
                    Main.dust[dust].noGravity = true;
                    modPlayer.shieldUp = false;
                }
            }
            else {
                modPlayer.shieldUp = false;
            }
            #endregion
        }
    }
}
