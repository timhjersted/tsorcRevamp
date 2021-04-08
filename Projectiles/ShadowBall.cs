using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class ShadowBall : ModProjectile {

        public override void SetDefaults() {
            projectile.aiStyle = 0;
            projectile.height = 15;
            projectile.penetrate = 4;
            projectile.tileCollide = true;
            projectile.timeLeft = 3600;
            projectile.friendly = true;
            projectile.width = 15;
            projectile.magic = true;
            projectile.alpha = 50;
        }

        public override void AI() {
            int dust = Dust.NewDust(new Vector2((float)projectile.position.X, (float)projectile.position.Y), projectile.width, projectile.height, 52, projectile.velocity.X * 0, -4, 100, default, 2.5f);
            Main.dust[dust].noGravity = true;
        }
        public override void Kill(int timeLeft) {
            for (int d = 0; d < 25; d++) {
                int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 52, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 100, default(Color), 2.5f);
                Main.dust[dust].noGravity = true;
            }
            Main.PlaySound(SoundID.NPCHit3.WithVolume(.45f), projectile.position);
        }
        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit) {
            for (int d = 0; d < 20; d++) {
                int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 52, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 100, default(Color), 2f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
