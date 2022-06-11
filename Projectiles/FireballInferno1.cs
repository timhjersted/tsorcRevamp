using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class FireballInferno1 : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.width = 180;
            Projectile.height = 180;
            Projectile.scale = 1.1f;
            Projectile.timeLeft = 120;
            Projectile.hostile = false;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = false;
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

        float size = 0;
        int dustCount = 0;
        bool randomDelaySet = false;
        public override void AI()
        {
            if (!randomDelaySet)
            {
                Projectile.ai[0] = Main.rand.Next(0, 19);
                randomDelaySet = true;
            }
            if (Projectile.ai[0] > 0)
            {
                Projectile.ai[0]--;
            }
            else
            {

                if (size < 4 * 16)
                {
                    size += ((4 * 16) / 10f);
                    dustCount = (int)(2 * MathHelper.Pi * size / 10); //Spawn dust according to its size                
                }
                else
                {
                    //Fade out after reaching max radius, and then despawn
                    dustCount /= 2;
                    if (dustCount <= 0)
                    {
                        Projectile.Kill();
                        return;
                    }
                }

                for (int j = 0; j < dustCount; j++)
                {
                    Vector2 dir = Main.rand.NextVector2CircularEdge(size, size);
                    Vector2 dustPos = Projectile.Center + dir;
                    dir.Normalize();
                    Vector2 dustVel = dir;
                    Dust.NewDustPerfect(dustPos, DustID.InfernoFork, dustVel, 200).noGravity = true;
                }
            }
        }
        public override void ModifyHitNPC(NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            damage += target.defense / 2;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if(randomDelaySet && Projectile.ai[0] <= 0)
            {
                return base.Colliding(projHitbox, targetHitbox);
            }
            else
            {
                return false;
            }
        }
        public override void Kill(int timeLeft)
        {
            
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }

    }
}
