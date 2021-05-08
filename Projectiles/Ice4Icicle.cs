using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class Ice4Icicle : ModProjectile {
        public override void SetDefaults() {
            projectile.width = 42;
            projectile.height = 84;
            projectile.friendly = true;
            projectile.penetrate = 8;
            projectile.magic = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 400;
        }
        public override void AI() {
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.ToRadians(90);
        }
    }
}
