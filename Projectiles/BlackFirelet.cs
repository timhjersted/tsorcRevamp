using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.ID;

namespace tsorcRevamp.Projectiles {
    class BlackFirelet : ModProjectile {

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Black Fire");
        }

        public override void SetDefaults() {
            projectile.width = 8;
            projectile.height = 8;
            projectile.alpha = 100;
            projectile.timeLeft = 200;
            projectile.friendly = true;
            projectile.penetrate = 2;
            projectile.magic = true;
            projectile.tileCollide = true;
            projectile.knockBack = 4;
        }

        public override void AI() {
            
            for (int i = 0; i < 2; i++) {
                int num43 = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 54, projectile.velocity.X * 0.2f, projectile.velocity.Y * 0.2f, 100, default, 2f);
                Main.dust[num43].noGravity = true;
                Dust dust1 = Main.dust[num43];
                dust1.velocity.X *= 0.3f;
                dust1.velocity.Y *= 0.3f;
                int dust = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 58, projectile.velocity.X * 0.2f, projectile.velocity.Y * 0.2f, 100, default, 2f);
                Main.dust[dust].noGravity = true;
                Dust dust2 = Main.dust[dust];
                dust2.velocity.X *= 0.3f;
                dust2.velocity.Y *= 0.3f;
            }
            projectile.ai[1] += 1f;

            if (projectile.ai[1] >= 20f) {
                projectile.velocity.Y = projectile.velocity.Y + 0.2f;
            }
            projectile.rotation += 0.3f * (float)projectile.direction;
            if (projectile.velocity.Y > 16f) {
                projectile.velocity.Y = 16f;
                return;
            }
        }

        public override bool OnTileCollide(Vector2 CollideVel) {
            Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 10);
            projectile.ai[0] += 1f;
            if (projectile.ai[0] >= 3f) {
                projectile.position += projectile.velocity;
                projectile.Kill();
            }
            else {
                if (projectile.velocity.Y > 4f) {
                    if (projectile.velocity.Y != CollideVel.Y) {
                        projectile.velocity.Y = -CollideVel.Y * 0.8f;
                    }
                }
                else {
                    if (projectile.velocity.Y != CollideVel.Y) {
                        projectile.velocity.Y = -CollideVel.Y;
                    }
                }
                if (projectile.velocity.X != CollideVel.X) {
                    projectile.velocity.X = -CollideVel.X;
                }
            }
            return false;
        }
        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit) {
            if (Main.rand.Next(5) == 0) {
                target.AddBuff(ModContent.BuffType<Buffs.DarkInferno>(), 240);
            }
        }

        public override void OnHitPvp(Player target, int damage, bool crit) {
            if (Main.rand.Next(5) == 0) {
                target.AddBuff(ModContent.BuffType<Buffs.DarkInferno>(), 240);
            }
        }
    }
}
