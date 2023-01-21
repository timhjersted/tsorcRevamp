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
        float homingAcceleration = 0.12f;
        float rotationProgress = 0;
        VFX.HomingStarTrail trail;
        Vector2 initialVelocity;
        public override void AI()
        {
            Player target = UsefulFunctions.GetClosestPlayer(Projectile.Center);
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (!playedSound)
            {
                initialVelocity = Projectile.velocity;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item43 with { Volume = 0.5f }, Projectile.Center);

                trail = Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity, ModContent.ProjectileType<Projectiles.VFX.HomingStarTrail>(), Projectile.damage, 0, Main.myPlayer, Projectile.ai[1], Projectile.whoAmI).ModProjectile as VFX.HomingStarTrail;

                //Basic, fired from the boss right at the player
                if (Projectile.ai[0] == 0)
                {
                    homingAcceleration = 0.12f;
                    if(Projectile.ai[1] == 0)
                    {
                        trail.trailMaxLength = 400;
                    }
                }

                //Accelerate downwards, do not despawn until impact
                if (Projectile.ai[0] == 1)
                {
                    Projectile.timeLeft = 1000;
                    float speedCap = 8;
                    if (Projectile.velocity.Y < speedCap)
                    {
                        Projectile.velocity.Y += 1f;
                    }
                    homingAcceleration = 0;
                }

                //No homing for certain attacks
                if (Projectile.ai[0] == 2)
                {
                    homingAcceleration = 0;
                    Projectile.timeLeft = 400;
                }

                //Lower homing in phase 2
                if (Projectile.ai[0] == 3)
                {
                    homingAcceleration = 0.05f;
                }
                //Lower homing in finale
                if (Projectile.ai[0] == 3)
                {
                    homingAcceleration = 0.09f;
                }

                playedSound = true;
            }

            if (Projectile.ai[0] == 2)
            {
                if (Projectile.ai[1] == 3)
                {
                    Projectile.velocity = Projectile.velocity.RotatedBy(0.0055f);
                    trail.trailMaxLength = 400;
                    if (Projectile.timeLeft < 155 && Projectile.ai[0] == 2)
                    {


                        float rotationSpeed = 0.05f;
                        if (rotationProgress <= MathHelper.PiOver4)
                        {
                            Projectile.velocity = Projectile.velocity.RotatedBy(rotationSpeed);
                            rotationProgress += rotationSpeed;
                        }
                        else
                        {
                            Projectile.velocity = Projectile.velocity.RotatedBy(0.0035f);
                            if (trail != null)
                            {
                                trail.trailMaxLength = 700;
                            }
                        }
                    }
                }
                else
                {
                    Projectile.velocity = Projectile.velocity.RotatedBy(-0.0035f);
                    if (Projectile.timeLeft < 190 && Projectile.ai[0] == 2)
                    {


                        float rotationSpeed = 0.05f;
                        if (rotationProgress <= MathHelper.PiOver2 + MathHelper.PiOver4)
                        {
                            Projectile.velocity = Projectile.velocity.RotatedBy(rotationSpeed);
                            rotationProgress += rotationSpeed;
                        }
                        else
                        {
                            Projectile.velocity = Projectile.velocity.RotatedBy(0.0035f);
                            if (trail != null)
                            {
                                trail.trailMaxLength = 700;
                            }
                        }
                    }
                }
            }

            //Curve counter-clockwise
            if (Projectile.ai[0] == 4)
            {
                Projectile.velocity = Projectile.velocity.RotatedBy(0.0015f);
            }

            //Curve counter-clockwise
            if (Projectile.ai[0] == 5)
            {
                homingAcceleration = 0;
                Projectile.velocity = Projectile.velocity.RotatedBy(0.0085f);
                trail.trailMaxLength = 700;
                if (Projectile.timeLeft > 600)
                {
                    Projectile.timeLeft = 600;
                }
            }

            //Curve clockwise
            if (Projectile.ai[0] == 6)
            {
                homingAcceleration = 0;
                Projectile.velocity = Projectile.velocity.RotatedBy(-0.0085f);
                trail.trailMaxLength = 700;
                if (Projectile.timeLeft > 600)
                {
                    Projectile.timeLeft = 600;
                }
            }



            //Stop homing after a few seconds
            if (Projectile.timeLeft > 800)
            {
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
