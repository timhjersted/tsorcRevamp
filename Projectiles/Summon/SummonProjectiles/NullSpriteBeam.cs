using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Summon.SummonProjectiles {
    public class NullSpriteBeam : ModProjectile {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Null Beam");
        }

		public override void SetDefaults() {
			projectile.width = 4;
			projectile.height = 4;
			projectile.friendly = true;
			projectile.minion = true;
			projectile.penetrate = 5;
			projectile.extraUpdates = 100;
			projectile.timeLeft = 180;
			projectile.usesLocalNPCImmunity = true;
			projectile.localNPCHitCooldown = -1;
		}

		public override void AI() {
			projectile.localAI[0] += 1f;
			if (projectile.localAI[0] > 9f) {
				for (int i = 0; i < 4; i++) {
					Vector2 projectilepos = projectile.position;
					projectilepos -= projectile.velocity * (i * 0.25f);
					projectile.alpha = 255;
					int num448 = Dust.NewDust(projectilepos, 1, 1, 227);
					Main.dust[num448].noGravity = true;
					Main.dust[num448].position = projectilepos;
					Main.dust[num448].scale = (float)Main.rand.Next(70, 110) * 0.013f;
					Main.dust[num448].velocity *= 0.2f;
				}
			}
		}
	}
}
