using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Projectiles {
    class FreezeBolt : ModProjectile {

        public override void SetDefaults() {
            projectile.width = 10;
            projectile.height = 10;
            projectile.alpha = 0;
            projectile.timeLeft = 1800;
            projectile.friendly = true;
            projectile.penetrate = 10;
            projectile.tileCollide = true;
            projectile.magic = true;
        }

        public override void AI() {
            if (projectile.type == 96 && projectile.localAI[0] == 0f) {
                projectile.localAI[0] = 1f;
                Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 20);
            }
            int num40 = Dust.NewDust(new Vector2(projectile.position.X + projectile.velocity.X, projectile.position.Y + projectile.velocity.Y), projectile.width, projectile.height, 15, projectile.velocity.X, projectile.velocity.Y, 100, default(Color), 3f);
            Main.dust[num40].noGravity = true;
            if (Main.rand.Next(10) == 0) {
                Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 15, projectile.velocity.X, projectile.velocity.Y, 100, default(Color), 1.4f);
            }
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
            if (projectile.ai[0] >= 6f) {
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
            if (Main.rand.Next(4) == 0) {
                target.AddBuff(BuffID.Frozen, 240);
            }
        }
        public override void OnHitPvp(Player target, int damage, bool crit) {
            if (Main.rand.Next(4) == 0) {
                target.AddBuff(BuffID.Frozen, 240);
            }
        }
    }
}
