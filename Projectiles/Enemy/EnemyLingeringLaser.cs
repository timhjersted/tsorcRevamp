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
            LaserSound = SoundID.Item12.WithVolume(0.5f);

            LaserDebuffs = new List<int>(ModContent.BuffType<Buffs.ThermalRise>()); //Lasers inflict thermal rise on hit, to give people a chance to recover from running out of flight time
            DebuffTimers = new List<int>(300);

            CastLight = false; //Literally the biggest performance hit of all of this lmfao

            Additive = true;
        }



        Vector2 target;
        Vector2 initialTarget;
        Vector2 initialPosition;
        Player targetPlayer;
        bool aimLeft = false;
        Vector2 simulatedVelocity;
        public override void AI()
        {

            if (FiringTimeLeft > 0)
            {
                Vector2 origin = GetOrigin();
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    if (Main.player[i].active && !Main.player[i].dead)
                    {
                        float point = 0;
                        if (Collision.CheckAABBvLineCollision(Main.player[i].Hitbox.TopLeft(), Main.player[i].Hitbox.Size(), origin, origin + projectile.velocity * Distance, 120, ref point))
                        {
                            Main.player[i].AddBuff(ModContent.BuffType<Buffs.ThermalRise>(), 220);
                        }
                    }
                }
            }
            
            if (projectile.ai[0] >= 0)
            {
                //Big beam. Swings towards the player
                if(projectile.ai[0] >= 2000)
                {
                    if (Main.player[(int)projectile.ai[0] - 2000] != null && Main.player[(int)projectile.ai[0] - 2000].active)
                    {
                        LaserName = "Channeled Laser";
                        FiringDuration = 90;
                        LineDust = Main.rand.Next(8) == 0;
                        LaserDust = DustID.OrangeTorch;
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

                //Little beams that it passively spams
                else if (projectile.ai[0] >= 1000)
                {
                    TileCollide = false;
                    TelegraphTime = 60;
                    FiringDuration = 15;
                    MaxCharge = 60;
                    LaserName = "Rapid Laser";

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
                    LaserName = "Confinement Laser";

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
            if (projectile.ai[0] == -1 || projectile.ai[0] == -2 || projectile.ai[0] == -3)
            {
                //Make it sit where it spawned
                FollowHost = false;
                if (initialPosition == Vector2.Zero)
                {
                    initialPosition = projectile.Center;
                }

                LaserOrigin = initialPosition;

                //Circle of lasers
                if (projectile.ai[0] == -3)
                {
                    TelegraphTime = 120;
                    FiringDuration = 120;
                    MaxCharge = 120;
                    LaserName = "Laser Array";
                    LineDust = true;
                    LaserDust = DustID.OrangeTorch;

                    if (FiringTimeLeft > 0)
                    {
                        projectile.velocity = projectile.velocity.RotatedBy((2f/3f) * MathHelper.Pi / 210f);
                    }

                    for (int i = 0; i < 5; i++)
                    {
                        Vector2 offset = Main.rand.NextVector2CircularEdge(1, 1);
                        offset.Normalize();
                        Vector2 velocity = offset * 3;
                        offset *= (Charge - MaxCharge) * 3;

                        if(FiringTimeLeft > 0)
                        {
                            velocity *= 3;
                        }

                        Dust.NewDustPerfect(LaserOrigin + offset, DustID.OrangeTorch, velocity, Scale: 3).noGravity = true;
                    }
                }
                if (projectile.ai[0] == -2)
                {                    
                    TelegraphTime = 60;
                    FiringDuration = 10;
                    MaxCharge = 60;
                    LaserName = "Probe Laser";
                    Dust.NewDustPerfect(LaserOrigin, DustID.OrangeTorch, Main.rand.NextVector2Circular(-3, 3), Scale: 4).noGravity = true;
                }

                //Square moving array
                if (projectile.ai[0] == -1)
                {                    
                    TelegraphTime = 150;
                    FiringDuration = 120;
                    MaxCharge = 150;
                    LaserName = "Laser Array";
                    initialPosition += new Vector2(3, 0).RotatedBy(NPCs.VanillaChanges.laserRotation);
                    if (Main.rand.NextBool())
                    {
                        Dust.NewDustPerfect(LaserOrigin, DustID.OrangeTorch, Main.rand.NextVector2Circular(-3, 3), Scale: 5).noGravity = true;
                    }

                    LaserColor = Color.Red * 5;
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

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            if ((projectile.ai[0] == -3 || projectile.ai[0] == -2 || projectile.ai[0] == -1) && !IsAtMaxCharge)
            {

                Color color;
                if (LaserTargetingTexture == TransparentTextureHandler.TransparentTextureType.GenericLaserTargeting)
                {
                    color = LaserColor;
                }
                else
                {
                    color = Color.White;
                }

                color *= 0.85f + 0.15f * (float)(Math.Sin(Main.GameUpdateCount / 5f));

                for(int i = 0; i < 10; i++)
                {
                    DrawLaser(spriteBatch, TransparentTextureHandler.TransparentTextures[LaserTargetingTexture], GetOrigin(),
                            projectile.velocity, LaserTargetingHead, LaserTargetingBody, LaserTargetingTail, -1.57f, 0.37f, color);
                }
            }
            else
            {
                base.PreDraw(spriteBatch, lightColor);
            }

            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (FiringTimeLeft <= 0 || !IsAtMaxCharge || TargetingMode != 0)
            {
                return false;
            }

            float point = 0f;
            Vector2 origin = GetOrigin();
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), origin,
                origin + projectile.velocity * Distance, 22, ref point);
        }

        public override bool CanHitPlayer(Player target)
        {

            string deathMessage = Terraria.DataStructures.PlayerDeathReason.ByProjectile(-1, projectile.whoAmI).GetDeathText(target.name).ToString();
            deathMessage = deathMessage.Replace("Laser", LaserName);
            target.Hurt(Terraria.DataStructures.PlayerDeathReason.ByCustomReason(deathMessage), projectile.damage * 4, 1);

            return false;
        }
    }
}
