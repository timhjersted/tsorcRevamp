using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {
    class EnemySpellGravity4Strike : ModProjectile {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 7;
        }

        public override void SetDefaults() {
            Projectile.width = 110;
            Projectile.height = 110;
            Projectile.hostile = true;
            Projectile.penetrate = 50;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI() {
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 3) {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= 7) {
                Projectile.Kill();
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
