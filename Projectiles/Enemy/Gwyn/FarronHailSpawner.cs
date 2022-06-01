using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace  tsorcRevamp.Projectiles.Enemy.Gwyn {
    class FarronHailSpawner : ModProjectile {

        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public override void SetDefaults() {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = false;
            Projectile.alpha = 255;
            Projectile.timeLeft = 225;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 500;
        }

        internal float AI_Timer {
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }
        internal float AI_Interval {
            get => Projectile.ai[1];
            set => Projectile.ai[1] = value;
        }

        public override void AI() {
            AI_Timer++;
            if (AI_Timer == 1) {
                if (Main.netMode != NetmodeID.MultiplayerClient) {
                    //this one is spawned with 0 damage, which means it's a telegraph not an actual bullet
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position, Projectile.velocity * 0.75f, ModContent.ProjectileType<FarronHail>(), 0, 0); 
                }
                Projectile.velocity *= 0.01f;
            }
            
            //you dont get a lot of time to dodge these on reaction and its on purpose
            //youre supposed to see the cyan cross indicator and start dodging then
            if (AI_Timer == Math.Floor(AI_Interval / 2)) {

                if (Main.netMode != NetmodeID.MultiplayerClient) {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position, Projectile.velocity * 100, ModContent.ProjectileType<FarronHail>(), Projectile.damage, 1);
                }

            }


            if (AI_Timer > (AI_Interval)) {
                Projectile.Kill();
            }
        }
    }
}
