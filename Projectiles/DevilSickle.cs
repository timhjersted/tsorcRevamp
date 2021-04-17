using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Projectiles {
    public class DevilSickle : ModProjectile {
        public override void SetDefaults() {
            projectile.width = 40;
            projectile.height = 40;
            projectile.scale = 0.9f;
            projectile.alpha = 100;
            projectile.timeLeft = 130;
            projectile.friendly = true;
            projectile.penetrate = 5;
            projectile.tileCollide = false;
            projectile.magic = true;
        }
        public override void AI() {
            int dust = Dust.NewDust(
            projectile.position,
            projectile.width,
            projectile.height,
            6,
            projectile.velocity.X,
            projectile.velocity.Y,
            90,
            default,
            1.8f
            );
            Main.dust[dust].noGravity = true;
            projectile.rotation += projectile.direction * 0.8f;

            if (projectile.velocity.X <= 7f && projectile.velocity.Y <= 7f && projectile.velocity.X >= -7f && projectile.velocity.Y >= -7f) {
                projectile.velocity.X *= 1.06f;
                projectile.velocity.Y *= 1.06f;
            }
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit) {
            if (Main.rand.Next(5) == 0) {
                target.AddBuff(BuffID.OnFire, 420); //blaze it 
            }
        }

        public override void OnHitPvp(Player target, int damage, bool crit) {
            if (Main.rand.Next(5) == 0) {
                target.AddBuff(BuffID.OnFire, 420); //blaze it 
            }
        }
        public override void Kill(int timeLeft) {
            Main.PlaySound(SoundID.Dig, (int)projectile.position.X, (int)projectile.position.Y, 9);
        }
    }
}
