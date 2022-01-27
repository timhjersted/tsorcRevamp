using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {
    class EnemySpellIcestormIcicle1 : ModProjectile {
        public override void SetDefaults() {
            projectile.width = 16;
            projectile.height = 52;
            projectile.hostile = true;
            projectile.penetrate = 16;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 400;
        }
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Enemy Spell Ice Storm");
        }
        public override void AI() {
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.ToRadians(90);
        }
    }
}
