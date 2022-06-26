using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Gwyn
{
    class BulletHellSpawner : ModProjectile
    {
        public int rotationSpeed;
        public float shotVelocity;
        public int lifespan;
        public int shotInterval;

        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Gwyn/Petal";

        float AI_Timer
        {
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.light = 0.2f;
        }

        public override void AI()
        {
            if (AI_Timer == 0)
            {
                if (Math.Abs(rotationSpeed) < 10)
                {
                    //not 90, because i want them to start point upwards on the outer edge slightly
                    Projectile.rotation += MathHelper.ToRadians(75 * Math.Sign(rotationSpeed));
                }
                else
                {
                    //for part 6
                    rotationSpeed -= (10 * Math.Sign(rotationSpeed));
                }
            }
            AI_Timer++;
            //Dust.NewDust(projectile.Center, 1, 1, DustID.Clentaminator_Purple);
            Projectile.rotation += 0.0085f * rotationSpeed;
            if (AI_Timer % shotInterval == 0)
            {
                for (int i = 0; i < 2; i++)
                {
                    int flipflop = (((i % 2) * 2) - 1); //alternates -1 and 1
                    Vector2 shotVelocity = new Vector2(0, this.shotVelocity * flipflop).RotatedBy(Projectile.rotation);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(),
                            Projectile.Center + shotVelocity,
                            shotVelocity,
                            ModContent.ProjectileType<BulletHellShot>(),
                            Projectile.damage,
                            Projectile.knockBack);
                    Dust a = Dust.NewDustPerfect(Projectile.Center + shotVelocity, DustID.Clentaminator_Purple);
                }
            }

            if (AI_Timer > lifespan)
            {
                Projectile.Kill();
            }
        }
    }
}
