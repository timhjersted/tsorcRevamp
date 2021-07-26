using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {
    
	class Bubble : ModProjectile {

        public override void SetDefaults() {
			projectile.aiStyle = 12;
			projectile.hostile = true;
			projectile.height = 20;
			projectile.penetrate = 2;
			projectile.ranged = true;
			projectile.tileCollide = false;
			aiType = 1;
			projectile.width = 20;
        }

        public override void Kill(int timeLeft)
        {
			projectile.type = 16;
            base.Kill(timeLeft);
        }
    }
}
