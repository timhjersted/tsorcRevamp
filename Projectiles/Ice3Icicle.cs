using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class Ice3Icicle : ModProjectile {
        public override void SetDefaults() {
            projectile.width = 32;
            projectile.height = 88;
            projectile.friendly = true;
            projectile.penetrate = 5;
            projectile.magic = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 200;
        }
        public override void AI() {
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.ToRadians(90);
        }
    }
}
