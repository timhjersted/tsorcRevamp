using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Triplets
{
    public class IncineratingGaze : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 99999999;
            DisplayName.SetDefault("Incinerating Gaze");
        }
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Triplets/HomingStarStar";

        public override void SetDefaults()
        {

            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = 50;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 586;
            Projectile.width = 10;
            Projectile.height = 250;
        }

        float chargeProgress;
        float laserWidth = 250;
        float firingTime = 216;

        //Sound Effect by Adam Wilson
        //https://www.youtube.com/watch?v=q_41f7Xp9_A
        SlotId soundSlotID;
        SoundStyle LaserSoundStyle = new SoundStyle("tsorcRevamp/Sounds/Custom/ChargeBeam") with { PlayOnlyIfFocused = false };
        bool soundPaused;
        ActiveSound laserSound;
        public override void AI()
        {
            //Extra long boi for final stand
            if (Projectile.ai[1] == 1)
            {
                Projectile.timeLeft = 90000;
                Projectile.ai[1] = 0;
            }

            if(chargeProgress < firingTime)
            {
                if (chargeProgress == 0)
                {
                    soundSlotID = Terraria.Audio.SoundEngine.PlaySound(LaserSoundStyle, Main.LocalPlayer.Center);                    
                }
                chargeProgress++;
            }
            else
            {
                laserWidth += 60;
            }

            if (laserSound == null)
            {
                SoundEngine.TryGetActiveSound(soundSlotID, out laserSound);
            }
            else
            {
                if (SoundEngine.AreSoundsPaused && !soundPaused)
                {
                    laserSound.Pause();
                    soundPaused = true;
                }
                else if (!SoundEngine.AreSoundsPaused && soundPaused)
                {
                    laserSound.Resume();
                    soundPaused = false;
                }
                laserSound.Position = Main.LocalPlayer.Center;
            }

            //Stick to retinazer and rotate to face wherever it is looking            
            if (Main.npc[(int)Projectile.ai[0]] != null && Main.npc[(int)Projectile.ai[0]].active && Main.npc[(int)Projectile.ai[0]].type == ModContent.NPCType<NPCs.Bosses.RetinazerV2>())
            {
                Projectile.rotation = Main.npc[(int)Projectile.ai[0]].rotation + MathHelper.PiOver2;
                Projectile.Center = Main.npc[(int)Projectile.ai[0]].Center + new Vector2(40, 0).RotatedBy(Projectile.rotation);
            }
            //If ret is dead then fade out
            else
            {
                if(Projectile.timeLeft > 130)
                {
                    Projectile.timeLeft = 130;
                }
            }

            //Cast light
            Vector3 colorVector = Color.OrangeRed.ToVector3() * 2f;
            
            Vector2 startPoint = Projectile.Center;
            Vector2 endpoint = Projectile.Center + Projectile.rotation.ToRotationVector2() * laserWidth;
            
            if (chargeProgress < firingTime)
            {
                colorVector *= chargeProgress / firingTime;
                endpoint = Projectile.Center + Projectile.rotation.ToRotationVector2() * 1000;
            }
            if (Projectile.timeLeft < 130)
            {
                colorVector *= Projectile.timeLeft / 130f;
                colorVector *= Projectile.timeLeft / 130f;
            }
            DelegateMethods.v3_1 = colorVector;

            Utils.PlotTileLine(startPoint, endpoint, 100, DelegateMethods.CastLight);

            float point = 0;

            if (chargeProgress == firingTime && Projectile.timeLeft > 130)
            {
                //Collision (TODO: Update)
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    if (Main.player[i].active && !Main.player[i].dead)
                    {
                        Rectangle targetHitbox = Main.player[i].Hitbox;
                        if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center,
                        Projectile.Center + Projectile.rotation.ToRotationVector2() * laserWidth, Projectile.height / 3f, ref point))
                        {
                            for (int j = 0; j < 6; j++)
                            {
                                Rectangle randBox = Main.player[i].Hitbox;
                                randBox.X += (int)Main.rand.NextFloat(-16, 16);
                                randBox.Y += (int)Main.rand.NextFloat(-16, 16);
                                CombatText.NewText(randBox, Color.OrangeRed, 999999999, true);
                            }
                            Main.player[i].statLife -= 999999999;
                            Main.player[i].KillMe(PlayerDeathReason.ByProjectile(999, Projectile.whoAmI), 999999999, 0);
                        }
                    }
                }
            }
        }

        public static Texture2D flameJetTexture;
        public static ArmorShaderData data;
        public static ArmorShaderData targetingData;
        public override bool PreDraw(ref Color lightColor)
        {
            if (flameJetTexture == null || flameJetTexture.IsDisposed)
            {
                flameJetTexture = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/Marilith/CataclysmicFirestorm", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            }
            if (chargeProgress < firingTime)
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

                //if (targetingData == null)
                {
                    targetingData = new ArmorShaderData(new Ref<Effect>(ModContent.Request<Effect>("tsorcRevamp/Effects/ScreenFilters/IncineratingGazeTargeting", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value), "IncineratingGazeTargetingPass");
                }

                Rectangle targetingSourceRectangle = new Rectangle(0, 0, (int)10000, Projectile.height);

                //Pass relevant data to the shader via these parameters
                targetingData.UseTargetPosition(new Vector2(10000, 250));
                float targetingScaleUp = 1;
                if (chargeProgress <= 170)
                {
                    targetingScaleUp = (float)Math.Pow(chargeProgress / firingTime, 0.2f);
                }
                else if(chargeProgress < firingTime - 20)
                {
                    targetingScaleUp = (float)Math.Pow(1 - ((chargeProgress - (firingTime - 40)) / 20), 0.2f);
                }
                else
                {
                    targetingScaleUp = 0;
                }

                targetingData.UseSaturation((targetingScaleUp / 1.2f));
                targetingData.UseOpacity(1);

                //Apply the shader
                targetingData.Apply(null);

                SpriteEffects targetingSpriteEffects = SpriteEffects.None;
                if (Projectile.spriteDirection == -1)
                {
                    targetingSpriteEffects = SpriteEffects.FlipHorizontally;
                }

                Vector2 targetingOrigin = new Vector2(0, targetingSourceRectangle.Height / 2);

                Main.EntitySpriteDraw(flameJetTexture, Projectile.Center - Main.screenPosition, targetingSourceRectangle, Color.White, Projectile.rotation, targetingOrigin, Projectile.scale, targetingSpriteEffects, 0);
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            //Apply the shader, caching it as well
            //if (data == null)
            {
                data = new ArmorShaderData(new Ref<Effect>(ModContent.Request<Effect>("tsorcRevamp/Effects/ScreenFilters/IncineratingGaze", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value), "IncineratingGazePass");
            }

            

            Rectangle sourceRectangle = new Rectangle(0, 0, (int)laserWidth, Projectile.height);

            //Pass relevant data to the shader via these parameters
            data.UseTargetPosition(new Vector2(laserWidth, 250));
            float scaleDown = 1;
            if (Projectile.timeLeft < 130)
            {
                scaleDown = Projectile.timeLeft / 130f;

            }
            if (chargeProgress <= 170)
            {
                scaleDown = (float)Math.Pow(chargeProgress / firingTime, 0.2f);
            }
            else if(chargeProgress < firingTime - 20)
            {
                scaleDown = (float)Math.Pow(1 - ((chargeProgress - (firingTime - 40)) / 20), 0.2f);
            }
            else if(chargeProgress < firingTime)
            {
                scaleDown = 0;
            }

            data.UseOpacity(scaleDown);
            //data.UseSecondaryColor(1, 1, Main.time);

            //Apply the shader
            data.Apply(null);

            SpriteEffects spriteEffects = SpriteEffects.None;
            if (Projectile.spriteDirection == -1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }
            
            Vector2 origin = new Vector2(0, sourceRectangle.Height / 2);

            Main.EntitySpriteDraw(flameJetTexture, Projectile.Center - Main.screenPosition, sourceRectangle, Color.White, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0);


            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }
}
