using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class HealingWater : ModProjectile {
        public override string Texture => "tsorcRevamp/Projectiles/MusicalNote";
        public override void SetDefaults() {
            projectile.aiStyle = 0;
            projectile.friendly = true;
            projectile.ignoreWater = true;
            projectile.magic = true;
            projectile.tileCollide = true;
            projectile.height = 18;
            projectile.width = 18;
            projectile.timeLeft = 30;
            projectile.alpha = 255;
        }

        public override void AI() {
            for (int i = 0; i < 4; i++) {
                var dust = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, 29, projectile.velocity.X * 0.4f, projectile.velocity.Y * 0.4f, 0, default, 2f);
                dust.noGravity = true;
            }
        }
    }
}
