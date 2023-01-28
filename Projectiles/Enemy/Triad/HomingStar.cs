using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Triad
{
    class HomingStar : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Seeking Star");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 60;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 3;

        }
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.scale = 1.1f;
            Projectile.timeLeft = 1000;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.friendly = false;
        }

        bool playedSound = false;
        float homingAcceleration = 0;
        float rotationProgress = 0;
        float speedCap = 999;
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (!playedSound)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item43 with { Volume = 0.5f }, Projectile.Center);

                bool forceBlue = false;
                float length = 700;
                switch (Projectile.ai[0])
                {
                    //Default phase 1 firing
                    case 0:
                        homingAcceleration = 0.12f;
                        length = 400;
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
                        length = 150;
                        forceBlue = true;
                        break;

                    //Bigger pink ones in final stand part 1
                    case 7:
                        length = 400;
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
                }

                //No homing for certain attacks
                if (forceBlue)
                {
                    length *= -1;
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity, ModContent.ProjectileType<Projectiles.VFX.HomingStarTrail>(), Projectile.damage, 0, Main.myPlayer, length, UsefulFunctions.EncodeID(Projectile));
                }

                playedSound = true;
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
                if (Projectile.timeLeft < 750)
                {
                    float rotationSpeed = -0.05f;
                    if (rotationProgress <= MathHelper.PiOver2 + MathHelper.PiOver4)
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
            if (Projectile.timeLeft > 800)
            {
                Player target = UsefulFunctions.GetClosestPlayer(Projectile.Center);
                if (target != null)
                {
                    //Perform homing
                    UsefulFunctions.SmoothHoming(Projectile, target.Center, homingAcceleration, 20, target.velocity, false);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}
