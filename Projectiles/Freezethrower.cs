using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class Freezethrower : ModProjectile {

        public override void SetDefaults() {
            projectile.width = 24;
            projectile.height = 24;
            projectile.alpha = 255;
            projectile.timeLeft = 3600;
            projectile.friendly = true;
            projectile.penetrate = 13;
            projectile.ignoreWater = true;
            projectile.tileCollide = true;
            projectile.MaxUpdates = 2;
            projectile.ranged = true;
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit) {
            if ((Main.rand.Next(5)) == 0) {
                target.AddBuff(BuffID.Frozen, 120);
            }
        }

        public override void OnHitPvp(Player target, int damage, bool crit) {
            if ((Main.rand.Next(5)) == 0) {
                target.AddBuff(BuffID.Frozen, 120);
            }
        }

        public override void AI() {
            if (projectile.timeLeft > 80) {
                projectile.timeLeft = 80;
            }
            if (projectile.ai[0] > 7f) {
                float num152 = 1f;
                if (projectile.ai[0] == 8f) {
                    num152 = 0.25f;
                }
                else {
                    if (projectile.ai[0] == 9f) {
                        num152 = 0.5f;
                    }
                    else {
                        if (projectile.ai[0] == 10f) {
                            num152 = 0.75f;
                        }
                    }
                }
                projectile.ai[0] += 1f;
                if (Main.rand.Next(2) == 0) {
                    for (int i = 0; i < 1; i++) {
                        int num155 = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 76, projectile.velocity.X * 0.2f, projectile.velocity.Y * 0.2f, 100, default, 1f);
                        if (Main.rand.Next(3) == 0) {
                            Main.dust[num155].noGravity = true;
                            Main.dust[num155].scale *= 3f;
                            Main.dust[num155].velocity *= 2f;
                        }
                        Main.dust[num155].scale *= 1.5f;
                        Main.dust[num155].velocity *= 1.2f;
                        Main.dust[num155].scale *= num152;
                    }
                }
            }
            else {
                projectile.ai[0] += 1f;
            }
            projectile.rotation += 0.3f * (float)projectile.direction;
            return;
        }
    }
}
