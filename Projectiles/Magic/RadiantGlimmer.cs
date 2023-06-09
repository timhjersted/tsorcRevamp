using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Magic
{
    class RadiantGlimmer : ModProjectile
    {
        //public override string Texture => "Terraria/Images/Projectile_10";
        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.penetrate = -1;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.knockBack = 0f;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 4;
        }

        float radius = 25;
        Vector2 targetPoint = Vector2.Zero;
        float timer = 0;

        int dustIndex;
        int[] dustArray = new int[500];
        float[] dustRotationArray = new float[500];
        float[] dustDistanceArray = new float[500];
        bool[] dustActiveArray = new bool[500];
        float[,] dustOffsetArray = new float[500, 2];
        public override void AI()
        {
            //TODO: Make this use shaders instead of dust
            //If you want to know why, uncomment these lines:
            /*
            int count = 0;
            for(int i = 0; i < Main.maxDust; i++)
            {
                if (Main.dust[i].active)
                {
                    count++;
                }
            }
            Main.NewText(count);*/

            //Target the position of its owners mouse
            if(Projectile.owner == Main.myPlayer)
            {
                targetPoint = Main.MouseWorld;
            }

            //Slow down right before reaching its target so it doesn't overshoot
            if(Vector2.Distance(targetPoint, Projectile.Center) < 300)
            {
                Projectile.velocity *= 0.85f;
            }

            //Home in on the mouse
            Vector2 nextVel = UsefulFunctions.Aim(Projectile.Center, Main.MouseWorld, 1f);
            float dist = Vector2.Distance(Projectile.Center, targetPoint);
            if ((Projectile.velocity + nextVel).Length() < 20 && dist > 16)
            {
                
                Projectile.velocity += nextVel;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, UsefulFunctions.Aim(Projectile.Center, Main.MouseWorld, Math.Min(dist / 20, 20)), 0.2f);
            }

            Player player = Main.player[Projectile.owner];

            if (!player.channel || player.noItems || player.CCed || player.statMana <= 0)
            {
                if (Projectile.timeLeft > 30)
                {
                    Projectile.timeLeft = 30;
                }
            }
            else
            {
                Projectile.timeLeft++;
                if (Main.GameUpdateCount % 2 == 0)
                {
                    player.statMana--;
                }
                player.manaRegenDelay = 180;

                //Expand its radius over time
                if (radius < 250)
                {
                    radius += 1.5f;
                    Projectile.width = 2 * (int)radius;
                    Projectile.height = 2 * (int)radius;
                }
            }

            if(Projectile.timeLeft <= 20)
            {
                Projectile.Center = Vector2.Lerp(Projectile.Center, UsefulFunctions.GetPlayerHandOffset(Main.player[Projectile.owner]), (21f - Projectile.timeLeft) / 20f);
                radius = 8f * Projectile.timeLeft / 4f;
            }



            //Spawn general dust in its radius
            float innerDustRate = MathHelper.Pi * radius * radius;
            for (int i = 0; i < innerDustRate / 25000f; i++)
            {
                Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(radius, radius), DustID.PurificationPowder, Vector2.Zero, Scale: (radius / 250f));
            }

            //Spawn dust on the edge, clearly denoting its area of effect
            float edgeDustRate = MathHelper.Pi * 2 * radius;
            //edgeDustRate /= 1000f * (radius / 250f) * (radius / 250f);


            for (int i = 0; i < edgeDustRate / 100; i++)
            {
                if (radius == 250)
                {
                    //if (!Main.dust[dustArray[dustIndex]].active)
                    {
                        Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2CircularEdge(radius, radius), DustID.PurificationPowder, Vector2.Zero);
                        //Main.dust[dustArray[dustIndex]].active = false;
                        dustArray[dustIndex] = dust.dustIndex;
                        Main.dust[dustArray[dustIndex]] = dust;
                        dustRotationArray[dustIndex] = UsefulFunctions.Aim(Projectile.Center, dust.position, 1).ToRotation();
                        dustDistanceArray[dustIndex] = radius;
                        dustActiveArray[dustIndex] = true;
                        dustIndex++;
                        if (dustIndex >= 500)
                        {
                            dustIndex = 0;
                        }
                    }
                    
                }
                else
                {
                    Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2CircularEdge(radius, radius), DustID.PurificationPowder, Vector2.Zero);
                }
            }

            //Spawn dust homing in on all npcs in the radius, making it react to their presence
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && !Main.npc[i].friendly && Main.npc[i].Center.Distance(Projectile.Center) < radius)
                {
                    Vector2 dustOffset = Main.rand.NextVector2Circular(radius, radius);
                    Dust.NewDustPerfect(Projectile.Center + dustOffset, DustID.PurificationPowder, UsefulFunctions.Aim(Projectile.Center + dustOffset, Main.npc[i].Center, Main.rand.NextFloat(0.01f, 4f))); ;
                }
            }
            
            //For every dust on the edge, move it to its proper position on the radius so that it "keeps up" with the projectile perfectly
            for (int i = 0; i < 500; i++)
            {
                //Check if any of the dust ended, and set it to inactive if so
                //This is needed so that entries in the array don't persist after dust death and mess up other dust in the world
                if (!Main.dust[dustArray[i]].active)
                {
                    dustActiveArray[i] = false;
                    dustArray[i] = 0;
                }
                //If not, then set the position of all active dust
                else if (dustActiveArray[i] == true && dustArray[i] != 0)
                {
                    //Clear dust ring on death
                    if (Projectile.timeLeft == 1)
                    {
                        Main.dust[dustArray[i]].active = false;
                        dustActiveArray[i] = false;
                        dustArray[i] = 0;
                    }
                    else
                    {
                        float retractMultiplier = 1;
                        if (Projectile.timeLeft <= 20)
                        {
                            retractMultiplier = 0.5f / (21f - Projectile.timeLeft);
                        }
                        Main.dust[dustArray[i]].position = Projectile.Center + dustRotationArray[i].ToRotationVector2() * dustDistanceArray[i] * retractMultiplier;
                    }
                }
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if(Vector2.Distance(projHitbox.Center.ToVector2(), targetHitbox.Center.ToVector2()) < radius)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

       

        public override bool PreDraw(ref Color lightColor)
        {
            //For now...
            return false;
        }
    }

}
