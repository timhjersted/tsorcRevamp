using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
	class Meteor : ModProjectile {

		public override void SetDefaults() {
			projectile.aiStyle = 9;
			projectile.friendly = true;
			projectile.height = 48;
			projectile.width = 48;
			projectile.light = 0.8f;
			projectile.magic = true;
			projectile.penetrate = 1;
			projectile.tileCollide = false;
			projectile.timeLeft = 50;
		}

	}
}


