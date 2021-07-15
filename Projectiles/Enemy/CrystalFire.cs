using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {
    class CrystalFire : ModProjectile {

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Crystal Fire");

        }
        public override void SetDefaults() {
            projectile.aiStyle = 8;
            projectile.hostile = true;
            projectile.width = 16;
            projectile.height = 16;
            projectile.tileCollide = true;
            projectile.damage = 21;
            projectile.timeLeft = 350;
            projectile.light = .8f;
            projectile.penetrate = 2;
            projectile.magic = true;
            
            Main.projFrames[projectile.type] = 4;
        }

        /**
        public override bool PreKill(int timeLeft) {
            projectile.type = 44;
            return true;
        }**/


        /**
        public override void AI() {
            projectile.rotation++;
            if (projectile.velocity.X <= 10 && projectile.velocity.Y <= 10 && projectile.velocity.X >= -10 && projectile.velocity.Y >= -10) {
                projectile.velocity.X *= 1.01f;
                projectile.velocity.Y *= 1.01f;
            }

            if (Main.rand.Next(2) == 0) {

                Lighting.AddLight((int)projectile.position.X / 16, (int)projectile.position.Y / 16, 0f, 0.3f, 0.8f);
                return;

            }

            projectile.frameCounter++;
            if (projectile.frameCounter > 2) {
                projectile.frame++;
                projectile.frameCounter = 0;
            }
            if (projectile.frame >= 4) {
                projectile.frame = 0;
            }

        }
        **/
    }
}
