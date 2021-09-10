using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
	class Sand : ModProjectile {

        public override void SetDefaults() {
			projectile.damage = 166;
			projectile.aiStyle = 0;
			projectile.friendly = true;
			projectile.penetrate = 1;
			projectile.tileCollide = true;
			projectile.usesIDStaticNPCImmunity = true;
			projectile.idStaticNPCHitCooldown = 6;
        }
        public override bool PreKill(int timeLeft) {
			projectile.type = 15;
			return true;
        }
        public override void AI() {
			int D = Dust.NewDust(new Vector2((float)projectile.position.X, (float)projectile.position.Y), projectile.width, projectile.height, 10, 0, 0, 100, default, 2.0f);
			Main.dust[D].noGravity = true;
		}
	}
}
