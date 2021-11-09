using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Microsoft.Xna.Framework.Graphics;

namespace tsorcRevamp.Projectiles.Enemy.DarkCloud {
    class DarkFreezeBolt : ModProjectile {

        public override void SetDefaults() {
            projectile.width = 10;
            projectile.height = 10;
            projectile.alpha = 0;
            projectile.timeLeft = 400;
            projectile.friendly = false;
            projectile.hostile = true;
            projectile.penetrate = 10;
            projectile.tileCollide = true;
            projectile.magic = true;
        }

        public override void AI() {
            if (projectile.type == 96 && projectile.localAI[0] == 0f) {
                projectile.localAI[0] = 1f;
                Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 20);
            }

            if (Main.rand.Next(10) == 0) {
                int dust = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, DustID.MagicMirror, projectile.velocity.X, projectile.velocity.Y, 100, default(Color), 2f);
                Main.dust[dust].noLight = true;
            }
            if (Main.rand.Next(10) == 0)
            {
                int dust = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, DustID.ShadowbeamStaff, projectile.velocity.X, projectile.velocity.Y, 100, default(Color), 2f);
                Main.dust[dust].noLight = true;
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
            
            projectile.velocity *= 0.5f;
            
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

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            UsefulFunctions.DrawSimpleLitProjectile(spriteBatch, projectile);
            return false;
        }
    }
}
