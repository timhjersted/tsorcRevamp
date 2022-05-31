using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {
    class EnemySpellIce3Icicle : ModProjectile {
        public override string Texture => "tsorcRevamp/Projectiles/Ice3Icicle";
        public override void SetDefaults() {
            Projectile.width = 32;
            Projectile.height = 88;
            Projectile.hostile = true;
            Projectile.penetrate = 8;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 300;
        }

        public override void AI() {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(90);
        }
    }
}
