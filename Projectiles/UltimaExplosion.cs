using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Projectiles {
    class UltimaExplosion : ModProjectile {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 8;
        }
        public override void SetDefaults() {
            Projectile.width = 250;
            Projectile.height = 172;
            Projectile.aiStyle = 5;
            Projectile.friendly = true;
            Projectile.penetrate = 50;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = 5;
        }
        public override void AI() {
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 3) {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= 8) {
                Projectile.Kill();
                return;
            }
        }
    }
}
