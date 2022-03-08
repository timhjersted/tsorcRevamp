using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace  tsorcRevamp.Projectiles.Enemy.Gwyn {
    class GreatFarronDartSpawner : ModProjectile {

        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public override void SetDefaults() {
            projectile.width = 16;
            projectile.height = 16;
            projectile.friendly = false;
            projectile.alpha = 255;
            projectile.timeLeft = 225; //arbitrary, so long as it's greater than Interval
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
                    //zero damage means it's a telegraphing tracer not an actual bullet
                    Projectile.NewProjectile(projectile.position, projectile.velocity * 0.75f, ModContent.ProjectileType<GreatFarronDart>(), 0, 0);
                }
                projectile.velocity *= 0.01f;
            }

            if (AI_Timer == Math.Floor(AI_Interval / 2)) {

                if (Main.netMode != NetmodeID.MultiplayerClient) {
                    Projectile.NewProjectile(projectile.position, projectile.velocity * 100, ModContent.ProjectileType<GreatFarronDart>(), projectile.damage, 1);
                }

            }


            if (AI_Timer > (AI_Interval)) {
                projectile.Kill();
            }
        }
    }
}
