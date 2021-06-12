using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class FirstFractal : ModProjectile {

        public override void SetDefaults() {
			projectile.width = 32;
			projectile.height = 32;
			projectile.friendly = true;
			projectile.melee = true;
			projectile.tileCollide = false;
			projectile.ignoreWater = true;
			projectile.extraUpdates = 1;
			projectile.usesLocalNPCImmunity = true;
			projectile.manualDirectionChange = true;
			projectile.penetrate = -1;
			projectile.timeLeft = 150;
		}
        
        public override void AI() {
			projectile.ai[0]++;
			projectile.rotation = (float)Math.PI / 4f * (float)projectile.spriteDirection + projectile.velocity.ToRotation();
			if (projectile.spriteDirection == -1) {
				projectile.rotation += (float)Math.PI;
			}
			if (projectile.ai[0] < 60) {
				Vector2 velocity = projectile.velocity;
				projectile.velocity = velocity.RotatedBy((float)Math.Atan(projectile.ai[0]) * 0.25);
			}
			
			else if (projectile.ai[1] == 0) {
				Vector2 speed;
				speed.X = (Main.mouseX + Main.screenPosition.X) - (projectile.position.X + projectile.width * 0.5f);
				speed.Y = (Main.mouseY + Main.screenPosition.Y) - (projectile.position.Y + projectile.height * 0.5f);
				speed.Normalize();
				speed *= 9f;
				projectile.velocity = speed;
				projectile.ai[1] = 1;
			}
			if (projectile.ai[0] > 120) {
				projectile.alpha += 8;
            }
		}
    }
}
