using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class Limit : ModProjectile {
		int flip;
		int direction;

		public override string Texture => "tsorcRevamp/Items/Weapons/Melee/Limit";
		public override void SetDefaults() {
			projectile.width = 43;
			projectile.height = 43;
			projectile.friendly = true;
			projectile.melee = true;
			projectile.tileCollide = false;
			projectile.ignoreWater = true;
			projectile.extraUpdates = 1;
			projectile.usesLocalNPCImmunity = true;
			projectile.localNPCHitCooldown = 8; //this thing is way stronger than it looks
			projectile.penetrate = -1;
			projectile.timeLeft = 90;
		}
        
        public override void AI() {
			if (projectile.ai[0] < 1) {
				flip = Main.rand.Next(2);
				direction = (flip == 0 ? 1 : -1); //choose a rotation direction when the projectile is spawned
            }
			projectile.ai[0]++;
			projectile.rotation -= 0.225f * direction;
			if (projectile.ai[0] < 45) {
				projectile.velocity = Vector2.Zero;
			}

			else if (projectile.ai[1] == 0) {
				Vector2 speed;
				speed.X = (Main.mouseX + Main.screenPosition.X) - (projectile.position.X + projectile.width * 0.5f);
				speed.Y = (Main.mouseY + Main.screenPosition.Y) - (projectile.position.Y + projectile.height * 0.5f);
				speed.Normalize();
				speed *= 5f;
				projectile.velocity = speed;
				projectile.ai[1] = 1;
			}
			if (projectile.ai[0] > 75) {
				projectile.alpha += 17;
				
			}
		}
    }
}
