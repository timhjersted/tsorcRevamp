using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class FireballInferno2 : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.width = 230;
            Projectile.height = 230;
            Projectile.scale = 1.1f;
            Projectile.timeLeft = 9999;
            Projectile.hostile = false;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = false;
            Projectile.friendly = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.penetrate = 9999;
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            target.AddBuff(BuffID.OnFire, 300);
        }

        float size = 10;
        float velocity = 22;
        int dustCount = 0;
        bool randomDelaySet = false;
        public override void AI()
        {
            Main.NewText("Size:" + size);
            Main.NewText("Velocity:" + velocity);
            Main.NewText("dustCount:" + dustCount);
            if (size > 1)
            {
                size += velocity;
                velocity--;
                dustCount = (int)(2 * MathHelper.Pi * size / 10); //Spawn dust according to its size                
            }
            else
            {
                //Fade out after reaching max radius, and then despawn
                dustCount /= 2;
                if (dustCount <= 0)
                {
                    //Projectile.NewProjectile(Projectile.GetSource_FromThis, Projectile.Center, Vector2.Zero, ModContent.ProjectileType<Nova>(), Projectile.damage, 0);
                    Projectile.Kill();
                    return;
                }
            }

            for (int j = 0; j < dustCount * 2; j++)
            {
               
                if(velocity > 0)
                {
                    Vector2 dir = Main.rand.NextVector2CircularEdge(size, size);
                    Vector2 dustPos = Projectile.Center + dir + Main.rand.NextVector2Circular(8, 8);
                    dir.Normalize();
                    Vector2 dustVel = dir;
                    Dust.NewDustPerfect(dustPos, DustID.InfernoFork, dustVel, 200, default, 1f).noGravity = true;
                }
                else
                {
                    Vector2 dir = Main.rand.NextVector2CircularEdge(size, size);
                    Vector2 dustPos = Projectile.Center + dir;
                    dir.Normalize();
                    Vector2 dustVel = -dir;
                    Dust.NewDustPerfect(dustPos, DustID.InfernoFork, dustVel, 200, default, 1f).noGravity = true;
                }
            }
        }
        public override void Kill(int timeLeft)
        {
           
        }
        public override void ModifyHitNPC(NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            base.ModifyHitNPC(target, ref damage, ref knockback, ref crit, ref hitDirection);
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Vector2.DistanceSquared(projHitbox.Center.ToVector2(), targetHitbox.Center.ToVector2()) < size * size)
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
