using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{

    public class EnemyLingeringLaser : EnemyGenericLaser
    {


        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Laser");
        }

        public override string Texture => base.Texture;

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.hide = true;
            Projectile.timeLeft = 999;

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
                        if (Collision.CheckAABBvLineCollision(Main.player[i].Hitbox.TopLeft(), Main.player[i].Hitbox.Size(), origin, origin + Projectile.velocity * Distance, 120, ref point))
                        {
                            Main.player[i].AddBuff(ModContent.BuffType<Buffs.ThermalRise>(), 220);
                        }
                    }
                }
            }

            if (Projectile.ai[0] >= 0)
            {
                //Big beam. Swings towards the player
                if (Projectile.ai[0] >= 2000)
                {
                    if (Main.player[(int)Projectile.ai[0] - 2000] != null && Main.player[(int)Projectile.ai[0] - 2000].active)
                    {
                        LaserName = "Channeled Laser";
                        FiringDuration = 90;
                        LineDust = Main.rand.Next(8) == 0;
                        LaserDust = DustID.OrangeTorch;
                        if (targetPlayer == null)
                        {
                            targetPlayer = Main.player[(int)Projectile.ai[0] - 2000];
                            target = Main.player[(int)Projectile.ai[0] - 2000].Center;


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
                        Projectile.velocity = UsefulFunctions.GenerateTargetingVector(Projectile.Center, target, 1);
                    }
                    else
                    {
                        Projectile.Kill();
                    }
                }

                //Little beams that it passively spams
                else if (Projectile.ai[0] >= 1000)
                {
                    TileCollide = false;
                    TelegraphTime = 60;
                    FiringDuration = 15;
                    MaxCharge = 60;
                    LaserName = "Rapid Laser";

                    if (target == Vector2.Zero)
                    {
                        target = Main.player[(int)Projectile.ai[0] - 1000].Center;
                        target += Main.player[(int)Projectile.ai[0] - 1000].velocity * 48;
                    }
                    //Failsafe. If the boss charges too close to the focal point it causes the lasers to go haywire. This turns them off if that happens.
                    if (Projectile.Distance(target) < 400 || Projectile.Distance(Main.player[(int)Projectile.ai[0] - 1000].Center) < 400)
                    {
                        Projectile.Kill();
                    }
                    Projectile.velocity = UsefulFunctions.GenerateTargetingVector(Projectile.Center, target, 1);

                    //Failsafe 2. If it is firing and the projectile's angle is too close to the "safe angle", don't fire. This stops lasers from sweeping across the safe area as the destroyer moves relative to it.
                    if ((UsefulFunctions.CompareAngles(Projectile.velocity, NPCs.VanillaChanges.destroyerLaserSafeAngle) < MathHelper.PiOver4 || UsefulFunctions.CompareAngles(-Projectile.velocity, NPCs.VanillaChanges.destroyerLaserSafeAngle) < MathHelper.PiOver4))
                    {
                        Projectile.Kill();
                    }
                }
                //Track the player and aim tangental 300 units next to them. Constrains their movement.
                else
                {
                    if (targetPlayer == null)
                    {
                        targetPlayer = Main.player[(int)Projectile.ai[0]];
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

                    target = UsefulFunctions.GenerateTargetingVector(Projectile.Center, initialTarget, 1);
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
                    Projectile.velocity = UsefulFunctions.GenerateTargetingVector(Projectile.Center, target, 1);

                    //Failsafe. If the boss charges *through* the circle, it causes the lasers to go haywire. This turns them off if that happens.
                    if (Projectile.Distance(target) < 400 || Projectile.Distance(targetPlayer.Center) < 400)
                    {
                        Projectile.Kill();
                    }
                }
            }

            //Projectile stays where it's spawned, and either fires at a point or at a small range around it
            if (Projectile.ai[0] == -1 || Projectile.ai[0] == -2 || Projectile.ai[0] == -3)
            {
                //Make it sit where it spawned
                FollowHost = false;
                if (initialPosition == Vector2.Zero)
                {
                    initialPosition = Projectile.Center;
                }

                LaserOrigin = initialPosition;

                //Circle of lasers
                if (Projectile.ai[0] == -3)
                {
                    TelegraphTime = 120;
                    FiringDuration = 120;
                    MaxCharge = 120;
                    LaserName = "Laser Array";
                    LineDust = true;
                    LaserDust = DustID.OrangeTorch;

                    if (FiringTimeLeft > 0)
                    {
                        Projectile.velocity = Projectile.velocity.RotatedBy((2f / 3f) * MathHelper.Pi / 210f);
                    }

                    for (int i = 0; i < 5; i++)
                    {
                        Vector2 offset = Main.rand.NextVector2CircularEdge(1, 1);
                        offset.Normalize();
                        Vector2 velocity = offset * 3;
                        offset *= (Charge - MaxCharge) * 3;

                        if (FiringTimeLeft > 0)
                        {
                            velocity *= 3;
                        }

                        Dust.NewDustPerfect(LaserOrigin + offset, DustID.OrangeTorch, velocity, Scale: 3).noGravity = true;
                    }
                }
                if (Projectile.ai[0] == -2)
                {
                    TelegraphTime = 60;
                    FiringDuration = 10;
                    MaxCharge = 60;
                    LaserName = "Probe Laser";
                    Dust.NewDustPerfect(LaserOrigin, DustID.OrangeTorch, Main.rand.NextVector2Circular(-3, 3), Scale: 4).noGravity = true;
                }

                //Square moving array
                if (Projectile.ai[0] == -1)
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

                Projectile.velocity.Normalize();
            }

            base.AI();
            if (Charge == MaxCharge - 1)
            {
                LaserSize = 0;
                LaserAlpha = 0;
            }

            if (LaserSize < 1.3f)
            {
                LaserSize += (1.3f / 30f);
                LaserAlpha += 1f / 30f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if ((Projectile.ai[0] == -3 || Projectile.ai[0] == -2 || Projectile.ai[0] == -1) && !IsAtMaxCharge)
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

                for (int i = 0; i < 10; i++)
                {
                    DrawLaser(Main.spriteBatch, TransparentTextureHandler.TransparentTextures[LaserTargetingTexture], GetOrigin(),
                            Projectile.velocity, LaserTargetingHead, LaserTargetingBody, LaserTargetingTail, -1.57f, 0.37f, color);
                }
            }
            else
            {
                base.PreDraw(lightColor);
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
                origin + Projectile.velocity * Distance, 22, ref point);
        }

        public override bool CanHitPlayer(Player target)
        {

            string deathMessage = Terraria.DataStructures.PlayerDeathReason.ByProjectile(-1, Projectile.whoAmI).GetDeathText(target.name).ToString();
            deathMessage = deathMessage.Replace("Laser", LaserName);
            target.Hurt(Terraria.DataStructures.PlayerDeathReason.ByCustomReason(deathMessage), Projectile.damage * 4, 1);

            return false;
        }
    }
}
