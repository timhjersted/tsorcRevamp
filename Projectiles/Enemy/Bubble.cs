using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {
    
	class Bubble : ModProjectile {

        public override void SetDefaults() {
			projectile.aiStyle = 1;
			projectile.hostile = true;
			projectile.height = 20;
			projectile.penetrate = 2;
			projectile.ranged = true;
			projectile.tileCollide = true;
			//aiType = 1;
			projectile.width = 20;
        }
        public override void AI()
        {
            base.AI();
        }
        public override bool PreKill(int timeLeft)
        {
            projectile.type = ProjectileID.WaterBolt;
            return true;
        }        
    }
}
