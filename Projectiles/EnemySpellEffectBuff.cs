using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Projectiles {
    class EnemySpellEffectBuff : ModProjectile {
        public override void SetStaticDefaults() {
            Main.projFrames[projectile.type] = 5;
        }
        public override void SetDefaults() {
            projectile.height = 44;
            projectile.hostile = true;
            projectile.penetrate = 50;
            projectile.scale = 1.2f;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
        }

        public override void AI() {
            projectile.frameCounter++;
            if (projectile.frameCounter > 3) {
                projectile.frame++;
                projectile.frameCounter = 0;
            }
            if (projectile.frame >= 5) {
                projectile.Kill();
                return;
            }
        }
    }
}
