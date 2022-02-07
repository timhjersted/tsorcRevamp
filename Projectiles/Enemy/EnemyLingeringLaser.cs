using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Enums;
using Terraria.GameContent.Shaders;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {

    public class EnemyLingeringLaser : EnemyGenericLaser {

       
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Laser");
        }

        public override string Texture => base.Texture;

        public override void SetDefaults() {
            projectile.width = 10;
            projectile.height = 10;
            projectile.friendly = false;
            projectile.hostile = true;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.magic = true;
            projectile.hide = true;
            projectile.timeLeft = 999;

            FollowHost = true;
            LaserOrigin = Main.npc[HostIdentifier].Center;
            TelegraphTime = 180;
            FiringDuration = 120;
            MaxCharge = 180;
            LaserLength = 5000;
            LaserColor = Color.Red;
            TileCollide = false;
            LaserSize = 1.3f;
            LaserTexture = TransparentTextureHandler.TransparentTextureType.RedLaserTransparent;
            LaserTextureHead = new Rectangle(0, 0, 30, 24);
            LaserTextureBody = new Rectangle(0, 26, 30, 30);
            LaserTextureTail = new Rectangle(0, 58, 30, 24);
            softFlicker = true;
            LaserSound = SoundID.Item12.WithVolume(0.5f);

            CastLight = Main.rand.NextBool(); //Literally the biggest performance hit of all of this lmfao

            Additive = true;
        }



        Vector2 target;
        Vector2 initialTarget;
        Vector2 initialPosition;
        Player targetPlayer;
        bool aimLeft = false;
        bool decided = false;
        Vector2 simulatedVelocity;
        public override void AI()
        {
            
            if (projectile.ai[0] >= 0)
            {
                //Big beam. Swings towards the player
                if(projectile.ai[0] >= 2000)
                {
                    if (Main.player[(int)projectile.ai[0] - 2000] != null && Main.player[(int)projectile.ai[0] - 2000].active)
                    {
                        FiringDuration = 90;
                        if (targetPlayer == null)
                        {
                            targetPlayer = Main.player[(int)projectile.ai[0] - 2000];
                            target = Main.player[(int)projectile.ai[0] - 2000].Center;


                        }

                        if (FiringTimeLeft > 30)
                        {
                            float lastLength = simulatedVelocity.LengthSquared();
                            simulatedVelocity += UsefulFunctions.GenerateTargetingVector(target, targetPlayer.Center, 0.5f);
                            if (simulatedVelocity.HasNaNs())
                            {
                                simulatedVelocity = Vector2.Zero;
                            }
                            //Stop once it passes the player and starts slowing down to change directions
                            if (simulatedVelocity == Vector2.Zero || lastLength > simulatedVelocity.LengthSquared())
                            {
                                FiringTimeLeft = 30;
                            }                            
                        }

                        //Move target point according to velocity
                        target += simulatedVelocity;

                        //Update projectile aim to aim at target point
                        projectile.velocity = UsefulFunctions.GenerateTargetingVector(projectile.Center, target, 1);
                    }
                    else
                    {
                        projectile.Kill();
                    }
                }
                else if (projectile.ai[0] >= 1000)
                {
                    TileCollide = false;
                    TelegraphTime = 60;
                    FiringDuration = 15;
                    MaxCharge = 60;
                    if (target == Vector2.Zero)
                    {
                        target = Main.player[(int)projectile.ai[0] - 1000].Center;
                        target += Main.player[(int)projectile.ai[0] - 1000].velocity * 48;
                    }
                    //Failsafe. If the boss charges too close to the focal point it causes the lasers to go haywire. This turns them off if that happens.
                    if (projectile.Distance(target) < 400 || projectile.Distance(Main.player[(int)projectile.ai[0] - 1000].Center) < 400)
                    {
                        projectile.Kill();
                    }
                    projectile.velocity = UsefulFunctions.GenerateTargetingVector(projectile.Center, target, 1);
                    
                    //Failsafe 2. If it is firing and the projectile's angle is too close to the "safe angle", don't fire. This stops lasers from sweeping across the safe area as the destroyer moves relative to it.
                    if ((UsefulFunctions.CompareAngles(projectile.velocity, NPCs.VanillaChanges.destroyerLaserSafeAngle) < MathHelper.PiOver4 || UsefulFunctions.CompareAngles(-projectile.velocity, NPCs.VanillaChanges.destroyerLaserSafeAngle) < MathHelper.PiOver4))
                    {
                        projectile.Kill();
                    }
                }
                //Track the player and aim tangental 300 units next to them. Constrains their movement.
                else
                {
                    if (targetPlayer == null)
                    {
                        targetPlayer = Main.player[(int)projectile.ai[0]];
                        aimLeft = Main.rand.NextBool();
                    }
                    TelegraphTime = 180;
                    FiringDuration = 100;
                    MaxCharge = 180;

                    //All this is to say: Aim 300 units to the left or right of the player, no matter what angle it's shooting at them from
                    //Also, only track the player while it's charging. Once it starts firing its target point is locked in.
                    if (Charge <= MaxCharge - 1)
                    {
                        initialTarget = targetPlayer.Center;
                    }

                    target = UsefulFunctions.GenerateTargetingVector(projectile.Center, initialTarget, 1);
                    target.Normalize();
                    target *= 300;

                    if (aimLeft)
                    {
                        target = target.RotatedBy(MathHelper.PiOver2);
                    }
                    else
                    {
                        target = target.RotatedBy(-MathHelper.PiOver2);
                    }

                    target += initialTarget;
                    projectile.velocity = UsefulFunctions.GenerateTargetingVector(projectile.Center, target, 1);

                    //Failsafe. If the boss charges *through* the circle, it causes the lasers to go haywire. This turns them off if that happens.
                    if (projectile.Distance(target) < 400 || projectile.Distance(targetPlayer.Center) < 400)
                    {
                        projectile.Kill();
                    }
                }
            }
            
            //Projectile stays where it's spawned, and either fires at a point or at a small range around it
            if (projectile.ai[0] == -1 || projectile.ai[0] == -2)
            {
                if (projectile.ai[0] == -1)
                {
                    if (Main.rand.NextBool() && !decided)
                    {
                        projectile.Kill();
                    }
                    decided = true;
                    TelegraphTime = 80;
                    FiringDuration = 95;
                    MaxCharge = 80;
                }
                if (projectile.ai[0] == -2)
                {
                    if (Main.rand.NextBool() && !decided)
                    {
                        projectile.Kill();
                    }
                    TelegraphTime = 210;
                    FiringDuration = 45;
                    MaxCharge = 240;
                    decided = true;
                }
                

                //Make it sit where it spawned
                FollowHost = false;
                if(initialPosition == Vector2.Zero)
                {
                    initialPosition = projectile.Center;
                }
                projectile.Center = initialPosition;
                LaserOrigin = initialPosition;
                if (Main.rand.NextBool())
                {
                    Dust.NewDustPerfect(LaserOrigin, DustID.OrangeTorch, Main.rand.NextVector2Circular(-3, 3), Scale: 7).noGravity = true;
                }
                projectile.velocity.Normalize();
            }

            base.AI();
            if(Charge == MaxCharge - 1)
            {
                LaserSize = 0;
                LaserAlpha = 0;
            }

            if(LaserSize < 1.3f)
            {
                LaserSize += (1.3f / 30f);
                LaserAlpha += 1f / 30f;
            }
        }
    }
}
