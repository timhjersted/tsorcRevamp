﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Bosses.JungleWyvern;
using tsorcRevamp.NPCs.Enemies.JungleWyvernJuvenile;

namespace tsorcRevamp.Projectiles
{
    class Freezethrower : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.hostile = false;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.light = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Triad/HomingStarStar";
        Player owner
        {
            get
            {
                return Main.player[Projectile.owner];
            }
        }

        bool dying = false;
        float size = 0;
        Vector2 truePosition;
        float maxSize = 1000;
        float fadeIn;
        float trueSize = 1;
        public override void AI()
        {
            Projectile.timeLeft = 2;

            if (dying)
            {
                fadeIn--;
                if (fadeIn <= 0)
                {
                    Projectile.Kill();
                    return;
                }
            }

            if (Main.GameUpdateCount % 20 == 0)
            {
                SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.5f }, truePosition);
            }


            if (owner.whoAmI == Main.myPlayer)
            {
                Item dummyItem = new Item(ModContent.ItemType<Items.Weapons.Ranged.Flamethrowers.Freezethrower>());
                Item gelStack = owner.ChooseAmmo(dummyItem);
                if (gelStack == null)
                {
                    dying = true;
                    return;
                }
                else
                {

                    Projectile.rotation = UsefulFunctions.Aim(owner.Center, Main.MouseWorld, 1).ToRotation();
                    Projectile.direction = Main.MouseWorld.X > owner.Center.X ? 1 : -1;
                    Vector2 rotDir = Projectile.rotation.ToRotationVector2();
                    Projectile.Center = owner.Center + rotDir * 30;
                    truePosition = owner.Center + rotDir * 30;
                    if (Projectile.direction == -1)
                    {
                        rotDir *= -1;
                    }
                    owner.itemRotation = rotDir.ToRotation();
                    owner.ChangeDir(Projectile.direction);
                    owner.itemTime = 2; // Set item time to 2 frames while we are used
                    owner.itemAnimation = 2; // Set item animation time to 2 frames while we are used

                    if (Main.GameUpdateCount % 60 == 0)
                    {
                        gelStack.stack--;
                        if (gelStack.stack <= 0)
                        {
                            Projectile.damage = 0;
                            dying = true;
                            gelStack.TurnToAir();
                        }
                    }
                }
            }



            float newTrueSize = (float)Math.Pow((UsefulFunctions.GetFirstCollision(Projectile.Center, Projectile.rotation.ToRotationVector2(), ignoreNPCs: true) - Projectile.Center).Length() / 800f, 0.5f);
            if (newTrueSize < trueSize)
            {
                trueSize = newTrueSize;
            }
            else if (newTrueSize > trueSize + 0.006f)
            {
                trueSize += 0.006f;
            }
            if (trueSize > 0.65f)
            {
                trueSize = 0.65f;
            }

            Vector2 unit = Projectile.rotation.ToRotationVector2();
            Vector2 endpoint = Projectile.Center + trueSize * (unit * (400 + (size / 6f)));
            DelegateMethods.v3_1 = Color.Blue.ToVector3();
            Utils.PlotTileLine(Projectile.Center, endpoint, 32, DelegateMethods.CastLight);
            DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
            DelegateMethods.tileCutIgnore = TileID.Sets.TileCutIgnore.None;
            Utils.PlotTileLine(Projectile.Center, endpoint, 32, DelegateMethods.CutTiles);


            if (!owner.channel || owner.noItems || owner.CCed)
            {
                Projectile.damage = 0;
                dying = true;
                return;
            }

            if (fadeIn < 15)
            {
                fadeIn++;
                return;
            }

            if (size < maxSize)
            {
                size += 30f;
            }
        }

        //Custom collision
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float distance = Vector2.Distance(truePosition, targetHitbox.Center.ToVector2());
            float angleBetween = (float)UsefulFunctions.CompareAngles(Vector2.Normalize(truePosition - targetHitbox.Center.ToVector2()), Projectile.rotation.ToRotationVector2());
            return distance < trueSize * (300 + (size / 2f)) && Math.Abs(angleBetween - MathHelper.Pi) < angle / 2.85f;
        }

        public static Effect effect;
        public float angle;
        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            if (effect == null)
            {
                effect = ModContent.Request<Effect>("tsorcRevamp/Effects/SyntheticBlizzard", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }

            angle = MathHelper.TwoPi / 10f;
            float shaderRotation = 0;
            shaderRotation %= MathHelper.TwoPi;
            effect.Parameters["splitAngle"].SetValue(angle);
            effect.Parameters["rotation"].SetValue(shaderRotation);
            effect.Parameters["time"].SetValue((float)Main.timeForVisualEffects / 252);
            effect.Parameters["length"].SetValue(.07f * size / maxSize);
            effect.Parameters["texScale"].SetValue(12);
            effect.Parameters["texScale2"].SetValue(1);
            effect.Parameters["noiseTexture"].SetValue(tsorcRevamp.NoiseTurbulent);
            float opacity = 1;

            if (fadeIn < 30)
            {
                MathHelper.Lerp(0.01f, 1, fadeIn / 30f);
                opacity *= fadeIn / 30f;
                opacity *= fadeIn / 30f;
            }

            effect.Parameters["opacity"].SetValue(opacity * 5);

            //I precompute many values once here so that I don't have to calculate them for every single pixel in the shader. Enormous performance save.
            effect.Parameters["rotationMinusPI"].SetValue(shaderRotation - MathHelper.Pi);
            effect.Parameters["splitAnglePlusRotationMinusPI"].SetValue(shaderRotation + angle - MathHelper.Pi);
            effect.Parameters["RotationMinus2PIMinusSplitAngleMinusPI"].SetValue((shaderRotation - (MathHelper.TwoPi - angle)) - MathHelper.Pi);

            //Apply the shader
            effect.CurrentTechnique.Passes[0].Apply();

            Rectangle recsize = new Rectangle(0, 0, tsorcRevamp.NoiseVoronoi.Width, tsorcRevamp.NoiseVoronoi.Height);
            Vector2 origin = new Vector2(recsize.Width * 0.5f, recsize.Height * 0.5f);

            //Draw the rendertarget with the shader
            Main.spriteBatch.Draw(tsorcRevamp.NoiseVoronoi, truePosition - Main.screenPosition, recsize, Color.White, Projectile.rotation + (MathHelper.Pi - angle / 2f), origin, trueSize * trueSize * 15f, SpriteEffects.None, 0);

            //Restart the spritebatch so the shader doesn't get applied to the rest of the game
            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);

            return false;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Slow, 300, false);
            if (Main.rand.NextBool(10))
            {
                target.AddBuff(BuffID.Frozen, 60);
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Slow, 300, false);
            if (Main.rand.NextBool(10))
            {
                target.AddBuff(BuffID.Frozen, 60);
            }
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (target.type == ModContent.NPCType<JungleWyvernHead>() || target.type == ModContent.NPCType<JungleWyvernBody>() || target.type == ModContent.NPCType<JungleWyvernBody2>() ||
                target.type == ModContent.NPCType<JungleWyvernBody3>() || target.type == ModContent.NPCType<JungleWyvernLegs>() || target.type == ModContent.NPCType<JungleWyvernTail>() ||
                target.type == ModContent.NPCType<JungleWyvernJuvenileHead>() || target.type == ModContent.NPCType<JungleWyvernJuvenileBody>() || target.type == ModContent.NPCType<JungleWyvernJuvenileBody2>() ||
                target.type == ModContent.NPCType<JungleWyvernJuvenileBody3>() || target.type == ModContent.NPCType<JungleWyvernJuvenileLegs>() || target.type == ModContent.NPCType<JungleWyvernJuvenileTail>())
            {
                modifiers.FinalDamage *= 0.75f;
            }
            base.ModifyHitNPC(target, ref modifiers);
        }
    }
}

