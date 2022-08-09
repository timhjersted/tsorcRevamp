using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Marilith
{
    class MarilithFirestorm : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Pyroclastic Detonation");
        }

        public override void SetDefaults()
        {
            Projectile.width = 194;
            Projectile.height = 194;
            DrawOriginOffsetX = -96;
            DrawOriginOffsetY = 94;
            Main.projFrames[Projectile.type] = 7;
            Projectile.hostile = true;
            Projectile.penetrate = 50;
            Projectile.scale = 2;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.light = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        float size = 0;
        int dustCount = 0;

        public override void AI()
        {
            Rectangle screenRect = new Rectangle((int)Main.screenPosition.X - 100, (int)Main.screenPosition.Y - 100, Main.screenWidth + 100, Main.screenHeight + 100);

            float maxSize = 60;
            float explosionTime = 45;
            if (Projectile.ai[0] == 0)
            {
                maxSize = 8;
                explosionTime = 8;
            }
            if (size < maxSize * 16)
            {
                size += ((maxSize * 16) / explosionTime);
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

            if (EnemyGenericLaser.FastContainsPoint(screenRect, Projectile.Center))
            {
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

        //Circular collision
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float distance = Vector2.Distance(projHitbox.Center.ToVector2(), targetHitbox.Center.ToVector2());
            if (distance < size && distance > size - 32)
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

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            if (Main.expertMode)
            {
                target.AddBuff(BuffID.OnFire, 450, false);
            }
            else
            {
                target.AddBuff(BuffID.OnFire, 900, false);
            }
        }
    }
}