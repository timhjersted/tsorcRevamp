using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {
    class EnemyBolt4Bolt : ModProjectile {

        public override string Texture => "tsorcRevamp/Projectiles/Bolt4Bolt";

        public override void SetDefaults() {
            projectile.width = 250;
            projectile.height = 450;
            projectile.penetrate = 16;
            projectile.aiStyle = 4;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.magic = true;
            projectile.light = 0.8f;
            Main.projFrames[projectile.type] = 16;
        }

        public override bool PreAI() {
            if (projectile.ai[0] == 0) {
                projectile.velocity.X *= 0.001f;
                projectile.velocity.Y *= 0.001f;
                projectile.ai[0] = 1;
            }

            projectile.frameCounter++;
            projectile.frame = (int)Math.Floor((double)projectile.frameCounter / 4);

            if (projectile.frame >= 16) {
                projectile.frame = 15;
            }
            if (projectile.frameCounter > 71) { // (projFrames * 4.5) - 1
                projectile.alpha += 15;
            }

            if (projectile.alpha >= 255) {
                projectile.Kill();
            }
            return true;
        }
    }
}