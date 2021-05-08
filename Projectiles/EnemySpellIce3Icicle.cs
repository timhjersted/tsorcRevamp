using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class EnemySpellIce3Icicle : ModProjectile {
        public override void SetDefaults() {
            projectile.width = 32;
            projectile.height = 88;
            projectile.hostile = true;
            projectile.penetrate = 8;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 300;
        }

        public override void AI() {
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.ToRadians(90);
        }
    }
}
