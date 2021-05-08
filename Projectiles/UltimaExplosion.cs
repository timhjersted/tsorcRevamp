using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Projectiles {
    class UltimaExplosion : ModProjectile {
        public override void SetStaticDefaults() {
            Main.projFrames[projectile.type] = 8;
        }
        public override void SetDefaults() {
            projectile.width = 250;
            projectile.height = 172;
            projectile.aiStyle = 5;
            projectile.friendly = true;
            projectile.penetrate = 50;
            projectile.magic = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.aiStyle = 5;
        }
        public override void AI() {
            projectile.frameCounter++;
            if (projectile.frameCounter > 3) {
                projectile.frame++;
                projectile.frameCounter = 0;
            }
            if (projectile.frame >= 8) {
                projectile.Kill();
                return;
            }
        }
    }
}
