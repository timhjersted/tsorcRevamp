using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {
    class EnemySpellGravity4Strike : ModProjectile {
        public override void SetStaticDefaults() {
            Main.projFrames[projectile.type] = 7;
        }

        public override void SetDefaults() {
            projectile.width = 110;
            projectile.height = 110;
            projectile.hostile = true;
            projectile.penetrate = 50;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
        }

        public override void AI() {
            projectile.frameCounter++;
            if (projectile.frameCounter > 3) {
                projectile.frame++;
                projectile.frameCounter = 0;
            }
            if (projectile.frame >= 7) {
                projectile.Kill();
                return;
            }
        }

        public override void OnHitPlayer(Player target, int damage, bool crit) {
            int gravDamage = (int)(target.statLifeMax2 * 0.05f);
            target.statLife -= gravDamage;
            if (target.statLife < 0) {
                target.KillMe(PlayerDeathReason.ByCustomReason(target.name + " experienced overwhelming gravity."), 75, 0, false);
            }
        }

    }
}
