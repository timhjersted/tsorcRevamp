using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Gwyn {
    class BulletHellSpawner : ModProjectile {
        public int rotationSpeed;
        public float shotVelocity;
        public int lifespan;
        public int shotInterval;

        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Gwyn/Petal";

        float AI_Timer {
            get => projectile.ai[0];
            set => projectile.ai[0] = value;
        }

        public override void SetDefaults() {
            projectile.width = 32;
            projectile.height = 32;
            projectile.friendly = false;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.light = 0.2f;
        }

        public override void AI() {
            if (AI_Timer == 0) {
                if (Math.Abs(rotationSpeed) < 10) {
                    //not 90, because i want them to start point upwards on the outer edge slightly
                    projectile.rotation += MathHelper.ToRadians(75 * Math.Sign(rotationSpeed)); 
                } 
                else {
                    //for part 6
                    rotationSpeed -= (10 * Math.Sign(rotationSpeed));
                }
            }
            AI_Timer++;
            //Dust.NewDust(projectile.Center, 1, 1, DustID.Clentaminator_Purple);
            projectile.rotation += 0.0085f * rotationSpeed;
            if (AI_Timer % shotInterval == 0) {
                for (int i = 0; i < 2; i++) {
                    int flipflop = (((i % 2) * 2) - 1); //alternates -1 and 1
                    Vector2 shotVelocity = new Vector2(0, this.shotVelocity * flipflop).RotatedBy(projectile.rotation);
                    Projectile.NewProjectile(
                            projectile.Center + shotVelocity,
                            shotVelocity,
                            ModContent.ProjectileType<BulletHellShot>(),
                            projectile.damage,
                            projectile.knockBack);
                    Dust a = Dust.NewDustPerfect(projectile.Center + shotVelocity, DustID.Clentaminator_Purple);
                }
            }

            if (AI_Timer > lifespan) {
                projectile.Kill();
            }
        }
    }
}
