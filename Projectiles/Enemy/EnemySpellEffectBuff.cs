using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Projectiles.Enemy {
    class EnemySpellEffectBuff : ModProjectile {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 5;
        }
        public override void SetDefaults() {
            Projectile.height = 44;
            Projectile.hostile = true;
            Projectile.penetrate = 50;
            Projectile.scale = 1.2f;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI() {
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 3) {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= 5) {
                Projectile.Kill();
                return;
            }
        }
    }
}
