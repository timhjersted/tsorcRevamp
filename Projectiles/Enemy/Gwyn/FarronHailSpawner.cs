using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace  tsorcRevamp.Projectiles.Enemy.Gwyn {
    class FarronHailSpawner : ModProjectile {

        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public override void SetDefaults() {
            projectile.width = 16;
            projectile.height = 16;
            projectile.friendly = false;
            projectile.alpha = 255;
            projectile.timeLeft = 225;
            projectile.tileCollide = false;
            projectile.timeLeft = 500;
        }

        internal float AI_Timer {
            get => projectile.ai[0];
            set => projectile.ai[0] = value;
        }
        internal float AI_Interval {
            get => projectile.ai[1];
            set => projectile.ai[1] = value;
        }

        public override void AI() {
            AI_Timer++;
            if (AI_Timer == 1) {
                if (Main.netMode != NetmodeID.MultiplayerClient) {
                    //this one is spawned with 0 damage, which means it's a telegraph not an actual bullet
                    Projectile.NewProjectile(projectile.position, projectile.velocity * 0.75f, ModContent.ProjectileType<FarronHail>(), 0, 0); 
                }
                projectile.velocity *= 0.01f;
            }
            
            //you dont get a lot of time to dodge these on reaction and its on purpose
            //youre supposed to see the cyan cross indicator and start dodging then
            if (AI_Timer == Math.Floor(AI_Interval / 2)) {

                if (Main.netMode != NetmodeID.MultiplayerClient) {
                    Projectile.NewProjectile(projectile.position, projectile.velocity * 100, ModContent.ProjectileType<FarronHail>(), projectile.damage, 1);
                }

            }


            if (AI_Timer > (AI_Interval)) {
                projectile.Kill();
            }
        }
    }
}
