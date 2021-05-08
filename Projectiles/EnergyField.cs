using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    public class EnergyField : ModProjectile {

        public override void SetStaticDefaults() {
            Main.projFrames[projectile.type] = 12;
        }

        public override void SetDefaults() {
            projectile.width = 44;
            projectile.height = 40;
            projectile.friendly = true;
            projectile.penetrate = 50;
            projectile.magic = true;
            projectile.tileCollide = true;
            projectile.ignoreWater = true;
            projectile.timeLeft = 360;
        }

        public override void AI() {
            projectile.frameCounter++;
            if (projectile.frameCounter > 3) {
                projectile.frame++;
                projectile.frameCounter = 0;
            }
            if (projectile.frame >= 12) {
                projectile.frame = 0;
                return;
            }
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit) {
            target.AddBuff(BuffID.Slow, 36000);
        }
    }
}
