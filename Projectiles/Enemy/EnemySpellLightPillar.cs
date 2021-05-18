using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {
    class EnemySpellLightPillar : ModProjectile {

        public override void SetStaticDefaults() {
            Main.projFrames[projectile.type] = 15;
        }

        public override void SetDefaults() {
            projectile.height = 40;
            projectile.width = 220;
            projectile.hostile = true;
            projectile.tileCollide = true;
            projectile.ignoreWater = true;
            projectile.timeLeft = 360;
            projectile.penetrate = 50;
        }

        public override void AI() {
            #region
            projectile.direction = Main.npc[projectile.owner].direction;
            projectile.position.X = Main.npc[projectile.owner].position.X + (float)(Main.npc[projectile.owner].width / 2) - (float)(projectile.width / 2);
            projectile.position.Y = Main.npc[projectile.owner].position.Y + (float)(Main.npc[projectile.owner].height / 2) - (float)(projectile.height / 2);
            projectile.rotation = (float)Math.Atan2((double)projectile.velocity.Y, (double)projectile.velocity.X) + 2.355f;
            if (projectile.spriteDirection == -1) {
                projectile.rotation -= 1.57f;
            }
            #endregion
            projectile.frameCounter++;
            if (projectile.frameCounter > 2) {
                projectile.frame++;
                projectile.frameCounter = 0;
            }
            if (projectile.frame >= 15) {
                projectile.Kill();
                return;
            }
        }
    }
}
