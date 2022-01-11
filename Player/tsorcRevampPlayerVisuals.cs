using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items;
using tsorcRevamp.Buffs;
using System;
using Microsoft.Xna.Framework.Graphics;

namespace tsorcRevamp {
    //Visuals
    public partial class tsorcRevampPlayer {
        public override void DrawEffects(PlayerDrawInfo drawInfo, ref float r, ref float g, ref float b, ref float a, ref bool fullBright) {

            //This is going here, because unlike most hooks this one keeps running even when the game is paused via AutoPause
            if (Main.mouseItem.type == ModContent.ItemType<DarkSoul>()) {
                player.chest = -1;
            }

            if (!Main.gameMenu) {
                if (player.HasBuff(ModContent.BuffType<Chilled>())) {
                    r *= 0.3804f;
                    g *= 0.6902f;
                    b *= 254 / 255;
                }
                if (Shockwave) {
                    r *= 0.7372f;
                    g *= 0.5176f;
                    b *= 0.3686f;
                }
            }
        }
        public override void ModifyDrawLayers(List<PlayerLayer> layers) {
            if (layers.Contains(PlayerLayer.HeldItem)) {
                layers.Insert(layers.IndexOf(PlayerLayer.HeldItem) + 1, tsorcRevampGlowmasks);
            }
            if (layers.Contains(PlayerLayer.HeldItem))
            {
                layers.Insert(layers.IndexOf(PlayerLayer.HeldItem) + 1, tsorcRevampEstusFlask);
            }
            layers.Add(tsorcRevampManaShield);


        }
        public override void ModifyDrawInfo(ref PlayerDrawInfo drawInfo) {
            base.ModifyDrawInfo(ref drawInfo);
        }
        public override void FrameEffects() {
            if (MiakodaNewBoost) {
                player.armorEffectDrawShadow = true;
            }
        }
        public static readonly PlayerLayer tsorcRevampGlowmasks = new PlayerLayer("tsorcRevamp", "tsorcRevampGlowmasks", PlayerLayer.HeldItem, delegate (PlayerDrawInfo drawInfo) {

            tsorcRevampPlayer modPlayer = drawInfo.drawPlayer.GetModPlayer<tsorcRevampPlayer>();
            Item thisItem = modPlayer.player.HeldItem;

            #region Glaive Beam HeldItem glowmask and animation
            //If the player is holding the glaive beam
            if (thisItem.type == ModContent.ItemType<Items.Weapons.Ranged.GlaiveBeam>()) {
                //And the projectile that creates the laser exists
                if (modPlayer.player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.GlaiveBeamLaser>()] > 0) {
                    Projectiles.GlaiveBeamLaser heldBeam;

                    //Then find the laser in the projectile array
                    for (int i = 0; i < Main.projectile.Length; i++) {
                        //If it found it, we're in business.
                        if (Main.projectile[i].type == ModContent.ProjectileType<Projectiles.GlaiveBeamLaser>() && Main.projectile[i].owner == modPlayer.player.whoAmI) {
                            //Get the transparent texture
                            Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.GlaiveBeamHeldGlowmask];

                            //Get the animation frame
                            heldBeam = (Projectiles.GlaiveBeamLaser)Main.projectile[i].modProjectile;
                            int textureFrames = 10;
                            int frameHeight = (int)texture.Height / textureFrames;
                            int startY = frameHeight * (int)Math.Floor(9 * (heldBeam.Charge / 300));
                            Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);

                            //Get the offsets and shift the draw position based on them
                            Player drawPlayer = drawInfo.drawPlayer;
                            float textureMidpoint = texture.Height / (2 * textureFrames);
                            Vector2 drawPos = drawInfo.itemLocation - Main.screenPosition;
                            Vector2 holdOffset = new Vector2(texture.Width / 2, textureMidpoint);
                            Vector2 originOffset = new Vector2(0, textureMidpoint);
                            ItemLoader.HoldoutOffset(drawPlayer.gravDir, drawPlayer.HeldItem.type, ref originOffset);
                            holdOffset.Y = originOffset.Y;
                            drawPos += holdOffset;

                            //Set the origin based on the offset point
                            Vector2 origin = new Vector2(-originOffset.X, textureMidpoint);

                            //Shift everything if the player is facing the other way
                            if (drawPlayer.direction == -1) {
                                origin.X = texture.Width + originOffset.X;
                            }

                            ///Draw, partner.
                            DrawData data = new DrawData(texture, drawPos, sourceRectangle, Color.White, drawPlayer.itemRotation, origin, modPlayer.player.HeldItem.scale, drawInfo.spriteEffects, 0);
                            Main.playerDrawData.Add(data);
                            break;
                        }
                    }
                }
            }
            #endregion
            //Make sure it's actually being displayed, not just selected
            if (modPlayer.player.itemAnimation > 0) {
                //Make sure it's from our mod
                if (thisItem.modItem != null && thisItem.modItem.mod == ModLoader.GetMod("tsorcRevamp")) {
                    Texture2D texture = null;
                    if (modPlayer.player.HeldItem.type == ModContent.ItemType<Items.Weapons.Ranged.Pulsar>()) {
                        texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.PulsarGlowmask];
                    }
                    if (modPlayer.player.HeldItem.type == ModContent.ItemType<Items.Weapons.Ranged.GWPulsar>()) {
                        texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.GWPulsarGlowmask];
                    }
                    if (modPlayer.player.HeldItem.type == ModContent.ItemType<Items.Weapons.Ranged.Polaris>()) {
                        texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.PolarisGlowmask];
                    }
                    if (modPlayer.player.HeldItem.type == ModContent.ItemType<Items.Weapons.Ranged.ToxicCatalyzer>()) {
                        texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.ToxicCatalyzerGlowmask];
                    }
                    if (modPlayer.player.HeldItem.type == ModContent.ItemType<Items.Weapons.Ranged.VirulentCatalyzer>()) {
                        texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.VirulentCatalyzerGlowmask];
                    }
                    if (modPlayer.player.HeldItem.type == ModContent.ItemType<Items.Weapons.Ranged.Biohazard>()) {
                        texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.BiohazardGlowmask];
                    }
                    if (modPlayer.player.HeldItem.type == ModContent.ItemType<Items.Weapons.Melee.MoonlightGreatsword>() && !Main.dayTime)
                    {
                        texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.MoonlightGreatswordGlowmask];
                    }
                    if (modPlayer.player.HeldItem.type == ModContent.ItemType<Items.Weapons.Melee.UltimaWeapon>())
                    {
                        texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.UltimaWeaponGlowmask];
                    }
                    //If it's not on the list, don't bother.
                    if (texture != null) {
                        #region animation
                        //These lines also can handle animation. Since this glowmask isn't animated a few of these lines are redundant, but they serve as an example for how it could be done.
                        //It's essentially the same to all other animation, you're just picking different parts of the texture to draw.
                        //In this case animationFrame is set as a function that depends on game time, making it animate as time passes. For another example, the Glaive Beam above animates based on weapon charge.
                        int textureFrames = 1;
                        int animationFrame = (int)Math.Floor(textureFrames * (double)(((Main.GameUpdateCount / 5) % 10) / 10));
                        int frameHeight = (int)texture.Height / textureFrames;
                        int startY = frameHeight * animationFrame;
                        Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
                        float textureMidpoint = texture.Height / (2 * textureFrames);
                        #endregion

                        //Since we're not doing animation, we can actually just 
                        //sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);

                        //Get the offsets and shift the draw position based on them
                        Player drawPlayer = drawInfo.drawPlayer;
                        Vector2 drawPos = drawInfo.itemLocation - Main.screenPosition;
                        Vector2 holdOffset = new Vector2(texture.Width / 2, textureMidpoint);
                        Vector2 originOffset = new Vector2(0, textureMidpoint);
                        ItemLoader.HoldoutOffset(drawPlayer.gravDir, drawPlayer.HeldItem.type, ref originOffset);

                        holdOffset.Y = originOffset.Y;
                        drawPos += holdOffset;
                        

                        //Set the origin based on the offset point
                        Vector2 origin = new Vector2(-originOffset.X, textureMidpoint);

                        //Sword support
                        if (modPlayer.player.HeldItem.useStyle == ItemUseStyleID.SwingThrow)
                        {
                            drawPos -= new Vector2(modPlayer.player.HeldItem.width / 2, modPlayer.player.HeldItem.height / 2);
                            origin.Y = modPlayer.player.HeldItem.height;

                            //Reversed grav fix support
                            if (drawPlayer.gravDir != 1 && ModContent.GetInstance<tsorcRevampConfig>().GravityFix)
                            {
                                origin.Y = 0;
                            }
                        }
                       
                        // Shift everything if the player is facing the other way
                        if (drawPlayer.direction == -1)
                        {
                            origin.X = texture.Width + originOffset.X;
                        }

                        Dust d = Dust.NewDustPerfect(drawPos * 16, DustID.MagicMirror);
                        Dust.NewDustPerfect((drawPos + origin) * 16, DustID.MagicMirror);

                        DrawData data = new DrawData(texture, drawPos, sourceRectangle, Color.White, drawPlayer.itemRotation, origin, modPlayer.player.HeldItem.scale, drawInfo.spriteEffects, 3);
                        Main.playerDrawData.Add(data);
                    }
                }
            }
        });
        public static readonly PlayerLayer tsorcRevampManaShield = new PlayerLayer("tsorcRevamp", "tsorcRevampManaShield", PlayerLayer.MiscEffectsFront, delegate (PlayerDrawInfo drawInfo) {
            tsorcRevampPlayer modPlayer = drawInfo.drawPlayer.GetModPlayer<tsorcRevampPlayer>();

            if (modPlayer.manaShield > 0 && !modPlayer.player.dead) {
                if (modPlayer.player.statMana > Items.Accessories.ManaShield.manaCost) {
                    //If they didn't have enough mana for the shield last frame but do now, play a sound to let them know it's back up
                    if (!modPlayer.shieldUp) {
                        //Soundtype Item SoundStyle 28 is powerful magic cast
                        Main.PlaySound(SoundID.Item, modPlayer.player.position, 28);
                        modPlayer.shieldUp = true;
                    }

                    Lighting.AddLight(modPlayer.player.Center, 0f, 0.2f, 0.3f);

                    int shieldFrameCount = 8;
                    float shieldScale = 2.5f;

                    Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.ManaShield];
                    Player drawPlayer = drawInfo.drawPlayer;
                    int drawX = (int)(drawInfo.position.X + drawPlayer.width / 2f - Main.screenPosition.X);
                    int drawY = (int)(drawInfo.position.Y + drawPlayer.height / 2f - Main.screenPosition.Y);
                    int frameHeight = texture.Height / shieldFrameCount;
                    int startY = frameHeight * (modPlayer.shieldFrame / 3);
                    Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
                    Color newColor = Color.White;// Lighting.GetColor((int)((drawInfo.position.X + drawPlayer.width / 2f) / 16f), (int)((drawInfo.position.Y + drawPlayer.height / 2f) / 16f));
                    Vector2 origin = sourceRectangle.Size() / 2f;

                    DrawData data = new DrawData(texture, new Vector2(drawX, drawY), sourceRectangle, newColor, 0f, origin, shieldScale, SpriteEffects.None, 0);
                    Main.playerDrawData.Add(data);
                }
                else {
                    if (modPlayer.shieldUp) {
                        //Soundtype Item SoundStyle 60 is the Terra Beam
                        Main.PlaySound(SoundID.Item, modPlayer.player.position, 60);
                        modPlayer.shieldUp = false;
                    }
                    //If the player doesn't have enough mana to tank a hit, then draw particle effects to indicate their mana is too low for it to function.
                    int dust = Dust.NewDust(modPlayer.player.Center, 1, 1, 221, modPlayer.player.velocity.X + Main.rand.Next(-3, 3), modPlayer.player.velocity.Y + Main.rand.Next(-3, 3), 180, Color.Cyan, 1f);
                    Main.dust[dust].noGravity = true;
                    modPlayer.shieldUp = false;
                }
            }
            else {
                modPlayer.shieldUp = false;
            }
        });

        public static readonly PlayerLayer tsorcRevampEstusFlask = new PlayerLayer("tsorcRevamp", "tsorcRevampEstusFlask", PlayerLayer.HeldItem, delegate (PlayerDrawInfo drawInfo) {
            tsorcRevampEstusPlayer estusPlayer = drawInfo.drawPlayer.GetModPlayer<tsorcRevampEstusPlayer>();

            if (estusPlayer.isDrinking && !estusPlayer.player.dead)
            {

                int estusFrameCount = 3;
                float estusScale = 0.8f;
                Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.EstusFlask];
                Player drawPlayer = drawInfo.drawPlayer;
                SpriteEffects effects = drawPlayer.direction > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                int rotation = drawPlayer.direction > 0 ? -90 : 90;
                int drawX = (int)(drawInfo.position.X + drawPlayer.width / 2f - Main.screenPosition.X);
                int drawY = (int)(drawInfo.position.Y + drawPlayer.height / 2f - Main.screenPosition.Y);
                int frameHeight = texture.Height / estusFrameCount;
                int frame = 0;
                if (estusPlayer.estusChargesCurrent == estusPlayer.estusChargesMax) { frame = 0; }
                else if (estusPlayer.estusChargesCurrent == 1) { frame = 2; }
                else { frame = 1; }

                int startY = frameHeight * frame;

                Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
                float chargesPercentage = (float)estusPlayer.estusChargesCurrent / estusPlayer.estusChargesMax;
                chargesPercentage = Utils.Clamp(chargesPercentage, 0f, 1f); // Clamping it to 0-1f so it doesn't go over that.

                Color newColor = Color.White * 0.8f;

                Vector2 origin = sourceRectangle.Size() / 2f;

                if (estusPlayer.estusDrinkTimer >= estusPlayer.estusDrinkTimerMax * 0.4f)
                {
                    if (drawPlayer.direction == 1)
                    {
                        DrawData data = new DrawData(texture, new Vector2(drawX + 12, drawY - 14), sourceRectangle, newColor, rotation, origin, estusScale, effects, 0);
                        Main.playerDrawData.Add(data);
                    }
                    else
                    {
                        DrawData data = new DrawData(texture, new Vector2(drawX - 12, drawY - 14), sourceRectangle, newColor, rotation, origin, estusScale, effects, 0);
                        Main.playerDrawData.Add(data);
                    }
                }

            }
            else
            {
                estusPlayer.isDrinking = false;
            }
        });
    }
}
