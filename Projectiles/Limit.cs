using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class Limit : ModProjectile {

		public override string Texture => "tsorcRevamp/Items/Weapons/Melee/Limit";
		public override void SetDefaults() {
			projectile.width = 58;
			projectile.height = 58;
			projectile.friendly = true;
			projectile.melee = true;
			projectile.tileCollide = false;
			projectile.ignoreWater = true;
			projectile.extraUpdates = 1;
			projectile.usesLocalNPCImmunity = true;
			projectile.localNPCHitCooldown = 8; //this thing is way stronger than it looks
			projectile.penetrate = -1;
			projectile.timeLeft = 90;
			projectile.light = 0.6f;
		}
        public override void AI() {
			int toEdge = projectile.height / 4;
			projectile.ai[0]++;
			projectile.rotation -= 0.225f * projectile.direction;
			if (projectile.ai[0] == 45) {
				projectile.velocity *= 20;
			}
			if (projectile.ai[0] > 60) {
				projectile.alpha += 25;	
			}
			if (projectile.alpha > 250) {
				projectile.Kill();
            }
		}
    }
}
