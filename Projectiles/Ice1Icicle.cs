using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class Ice1Icicle : ModProjectile {

        public override void SetDefaults() {
            projectile.width = 18;
            projectile.height = 38;
            projectile.friendly = true;
            projectile.penetrate = 3;
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
