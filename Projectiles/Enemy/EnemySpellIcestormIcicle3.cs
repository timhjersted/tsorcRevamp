using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {
    class EnemySpellIcestormIcicle3 : ModProjectile {
        public override void SetDefaults() {
            projectile.width = 22;
            projectile.height = 22;
            projectile.hostile = true;
            projectile.penetrate = 16;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 400;
        }
        public override void AI() {
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.ToRadians(90);
        }
    }
}
