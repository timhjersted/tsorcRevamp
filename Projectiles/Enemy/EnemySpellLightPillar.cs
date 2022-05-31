using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {
    class EnemySpellLightPillar : ModProjectile {

        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 15;
        }

        public override void SetDefaults() {
            Projectile.height = 40;
            Projectile.width = 220;
            Projectile.hostile = true;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 360;
            Projectile.penetrate = 50;
        }

        public override void AI() {
            #region
            Projectile.direction = (int)Projectile.ai[0];
            if (Projectile.spriteDirection == -1) {
                Projectile.rotation -= 1.57f;
            }
            #endregion
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 2) {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= 15) {
                Projectile.Kill();
                return;
            }
        }
    }
}
