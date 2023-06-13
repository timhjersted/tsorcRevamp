using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.VFX;

namespace tsorcRevamp.Projectiles.Enemy.Triad
{
    class HomingStar : DynamicTrail
    {

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Seeking Star");
        }
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.scale = 1.1f;
            Projectile.timeLeft = 600;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.friendly = false;


            trailWidth = 35;
            trailPointLimit = 900;
            trailCollision = true;
            NPCSource = false;
            collisionFrequency = 5;
            trailYOffset = 50;
            trailMaxLength = 700;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/HomingStarShader", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }

        bool playedSound = false;
        float homingAcceleration = 0;
        float rotationProgress = 0;
        float speedCap = 999;

        float transitionTimer;
        bool forcePink = false;
        bool forceBlue = false;
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (!playedSound)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item43 with { Volume = 0.5f }, Projectile.Center);

                switch (Projectile.ai[0])
                {
                    //Default phase 1 firing
                    case 0:
                        homingAcceleration = 0.12f;
                        trailMaxLength = 400;
                        break;

                    //Default phase 2 firing
                    case 1:
                        homingAcceleration = 0.12f;
                        break;

                    //Phase 1 starfall falling
                    case 2:
                        speedCap = 8;
                        break;

                    //Phase 1 starfall firing up
                    case 3:
                        speedCap = 8;
                        break;

                    //Phase 2 starfall falling
                    case 4:
                        speedCap = 8;
                        break;

                    //Phase 2 starfall firing up
                    case 5:
                        speedCap = 8;
                        break;

                    //Small blue ones in final stand part 1
                    case 6:
                        trailMaxLength = 150;
                        Projectile.timeLeft = 600;
                        forceBlue = true;
                        break;

                    //Bigger pink ones in final stand part 1
                    case 7:
                        trailMaxLength = 400;
                        Projectile.timeLeft = 600;
                        break;

                    //Large blue ones in final stand part 2
                    case 8:
                        forceBlue = true;
                        Projectile.timeLeft = 600;
                        break;

                    //Large pink ones in final stand part 2
                    case 9:
                        Projectile.timeLeft = 600;
                        break;

                    //Homing blue ones in final stand part 1
                    case 10:
                        homingAcceleration = 0.12f;
                        forceBlue = true;
                        break;
                }

                playedSound = true;
            }

            base.AI();
            timeFactor++;


            int? catIndex = UsefulFunctions.GetFirstNPC(ModContent.NPCType<NPCs.Bosses.Cataluminance>());
            if (catIndex != null)
            {
                if (Main.npc[catIndex.Value].life < Main.npc[catIndex.Value].lifeMax * 2f / 3f)
                {
                    if (Projectile.timeLeft == 99999999)
                    {
                        forcePink = true;
                    }
                    else
                    {
                        transitionTimer++;
                    }
                }
            }

            if (Projectile.ai[0] == 2 || Projectile.ai[0] == 4)
            {
                if (Projectile.velocity.Y < speedCap)
                {
                    Projectile.velocity.Y += 1f;
                }
            }

            //Small blue ones in final stand part 1
            if (Projectile.ai[0] == 6)
            {
                Projectile.velocity = Projectile.velocity.RotatedBy(-0.0055f);
                if (Projectile.timeLeft < 350)
                {
                    float rotationSpeed = -0.05f;
                    if (Math.Abs(rotationProgress) <= MathHelper.PiOver2 + MathHelper.PiOver4)
                    {
                        Projectile.velocity = Projectile.velocity.RotatedBy(rotationSpeed);
                        rotationProgress += rotationSpeed;
                    }
                    else
                    {
                        Projectile.velocity = Projectile.velocity.RotatedBy(-0.0035f);
                    }
                }
            }

            //Bigger pink ones in final stand part 1
            if (Projectile.ai[0] == 7)
            {
                Projectile.velocity = Projectile.velocity.RotatedBy(0.0055f);
                return;
            }

            //Curve counter-clockwise (final stand part 2)
            if (Projectile.ai[0] == 8)
            {
                Projectile.velocity = Projectile.velocity.RotatedBy(0.0085f);
            }

            //Curve clockwise (final stand part 2)
            if (Projectile.ai[0] == 9)
            {
                Projectile.velocity = Projectile.velocity.RotatedBy(-0.0085f);
            }

            //Stop homing after a few seconds
            if (Projectile.timeLeft > 400)
            {
                Player target = UsefulFunctions.GetClosestPlayer(Projectile.Center);
                if (target != null)
                {
                    //Perform homing
                    UsefulFunctions.SmoothHoming(Projectile, target.Center, homingAcceleration, 20, target.velocity, false);
                }
            }
        }
        public override float CollisionWidthFunction(float progress)
        {
            if (progress >= 0.85)
            {
                float scale = (1f - progress) / 0.15f;
                return (float)Math.Pow(scale, 0.1) * (float)trailWidth * 0.5f;
            }
            else
            {
                return (float)Math.Pow(progress, 0.6f) * trailWidth * 0.5f;
            }
        }

        float timeFactor = 0;
        int ꙮ; //Note: ​​̵̲̹̞͘​̶̝̥̰̓͐̽​̶̛͍͌̑​̴̜͉̀​̵̨̦̜̈́̕​̴̞̰̖̆​̸̒͜​̸͚̖͌̎​̸̝̊͠​̵̩̒͗͝​̵̟̩͐
        public override void SetEffectParameters(Effect effect)
        {
            float intensity = 0.07f;
            float trueFadeOut = fadeOut;
            Color shaderColor;
            if (forceBlue)
            {
                shaderColor = new Color(0.1f, 0.5f, 1f);
            }
            else if (forcePink)
            {
                shaderColor = new Color(1f, 0.3f, 0.85f) * 0.5f;
                intensity = 0.1f;
            }
            else if (transitionTimer <= 120)
            {
                shaderColor = Color.Lerp(new Color(0.1f, 0.5f, 1f), new Color(1f, 0.3f, 0.85f), transitionTimer / 120);
                trueFadeOut += trueFadeOut * (transitionTimer / 120);
                intensity = 0.07f + 0.03f * (transitionTimer / 120);
            }
            else
            {
                shaderColor = new Color(1f, 0.3f, 0.85f);
            }

            ꙮ += 1;

            collisionEndPadding = (int)trailCurrentLength / 30;
            visualizeTrail = false;

            //Shifts its color slightly over time
            Color rgbColor = UsefulFunctions.ShiftColor(shaderColor, ꙮ, intensity);

            effect.Parameters["noiseTexture"].SetValue(tsorcRevamp.tNoiseTextureTurbulent);
            effect.Parameters["fadeOut"].SetValue(trueFadeOut);
            effect.Parameters["time"].SetValue(timeFactor / 100f);
            effect.Parameters["shaderColor"].SetValue(rgbColor.ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }
    }
}
