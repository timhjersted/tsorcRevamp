using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class FireField : ModProjectile {

        public override string Texture => "tsorcRevamp/Projectiles/GreatFireballBall";

        public override void SetStaticDefaults() {
            Main.projFrames[projectile.type] = 5;
        }

        public override void SetDefaults() {
            projectile.width = 26;
            projectile.height = 40;
            projectile.aiStyle = -1; ;
            projectile.friendly = true;
            projectile.magic = true;
            projectile.light = 1;
            projectile.tileCollide = true;
            projectile.ignoreWater = true;
            projectile.penetrate = 50;
            projectile.timeLeft = 360;
        }

        public override void AI() {
            projectile.frameCounter++;
            if (projectile.frameCounter > 3) {
                projectile.frame++;
                projectile.frameCounter = 0;
            }
            if (projectile.frame >= 5) {
                projectile.frame = 0;
                return;
            }
        }
    }
}
