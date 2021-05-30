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
		}

        public override void AI() {
			float num = 60f;
			if ((projectile.localAI[0] += 1f) >= num - 1f) {
				projectile.Kill();
				return;
			}
			projectile.velocity = projectile.velocity.RotatedBy(projectile.ai[0]);
			projectile.direction = ((projectile.velocity.X > 0f) ? 1 : (-1));
			projectile.spriteDirection = projectile.direction;
			projectile.rotation = (float)Math.PI / 4f * (float)projectile.spriteDirection + projectile.velocity.ToRotation();
			if (projectile.spriteDirection == -1) {
				projectile.rotation += (float)Math.PI;
			}
			if (projectile.localAI[0] > 7f) {
				if (Main.rand.Next(15) == 0) {
					Dust dust = Dust.NewDustPerfect(projectile.Center, 278, null, 100, Color.Lerp(Main.hslToRgb(projectile.ai[1], 1f, 0.5f), Color.White, Main.rand.NextFloat() * 0.3f));
					dust.scale = 0.7f;
					dust.noGravity = true;
					dust.velocity *= 0.5f;
					dust.velocity += projectile.velocity * 2f;
				}
			}
		}

	}
}
