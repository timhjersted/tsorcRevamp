using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class FireballNova : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.width = 230;
            Projectile.height = 230;
            Projectile.scale = 1.1f;
            Projectile.timeLeft = 5;
            Projectile.hostile = false;
            Projectile.friendly = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.penetrate = 999;
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            target.AddBuff(BuffID.OnFire, 300);
        }

        float size = 130;
        float velocity = 14;
        int dustCount = 0;
        float rotation = 0;

        public override void AI()
        {
            if(Projectile.timeLeft == 5)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item62, Projectile.Center);
            }

            // create glowing red embers that fill the explosion's radius
            for (int i = 0; i < 50; i++)
            {
                Dust.NewDustPerfect(dustPos(), DustID.InfernoFork, dustVel(), 160, default, 2f).noGravity = true;
                Dust.NewDustPerfect(dustPos(), DustID.Torch, dustVel(), 160, default, 3f).noGravity = true;
                Dust.NewDustPerfect(dustPos(), DustID.InfernoFork, dustVel(), 160, default, 3f).noGravity = true;
                Dust.NewDustPerfect(dustPos(), 130, dustVel(), 160, default, 0.75f).noGravity = true;
                Dust.NewDustPerfect(dustPos(), DustID.InfernoFork, dustVel(), 160, default, 3f).noGravity = true;
            }
        }
        Vector2 dustPos()
        {
            return Projectile.Center + Main.rand.NextVector2Circular(Projectile.width / 20f, Projectile.height / 20f);
        }
        Vector2 dustVel()
        {
            //Pick a random angle to offset the whole pattern by, unless one is already set
            if (rotation == 0)
            {
                rotation = Main.rand.NextFloat(0, MathHelper.PiOver2);
            }

            //Pick an angle in the first quadrant (0 - 90 degrees)
            float angle = Main.rand.NextFloat(0, MathHelper.PiOver2);

            //Modify the speed of the projectile based on it
            float speed = Math.Abs((angle / (MathHelper.PiOver4)) - 1f);

            //Since this pattern is symmetrical on both axises, we can just have a 50% chance to flip it on the x-axis
            if (Main.rand.NextBool())
            {
                angle += MathHelper.PiOver2;
            }

            //And another 50% chance to flip it on the y-axis
            if (Main.rand.NextBool())
            {
               angle += MathHelper.Pi;
            }
            if (Main.rand.NextBool(2))
            {
                angle += rotation;

                //Add some variation
                if (Main.rand.NextBool())
                {
                    speed = Main.rand.NextFloat(0, speed);
                }
            }

            //Create the second smaller loop
            else
            {
                angle += rotation + MathHelper.PiOver4;
                speed /= 1.6f;
            }

            //Smooth out the curves slightly
            speed = (float)Math.Pow(speed, 0.9f);

            return new Vector2(speed * 23, 0).RotatedBy(angle);

        }
        public override void Kill(int timeLeft)
        {

        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Vector2.DistanceSquared(projHitbox.Center.ToVector2(), targetHitbox.Center.ToVector2()) < Math.Pow(16 * 16, 2)) //16 tile radius, 16 units per tile, (18*16)^2 = 82944
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
            return false;
        }

    }
}






/*using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class FireballInferno2 : ModProjectile
    {

        public override void SetDefaults()
        {
            
            
           
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            target.AddBuff(BuffID.OnFire, 300);
        }

        
        public override void ModifyHitNPC(NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            Main.NewText("aaa");
            base.ModifyHitNPC(target, ref damage, ref knockback, ref crit, ref hitDirection);
        }
        
    }
}
*/