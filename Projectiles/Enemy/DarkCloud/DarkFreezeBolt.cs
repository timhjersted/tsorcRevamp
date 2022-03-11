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
                Dust.NewDustPerfect(projectile.Center, DustID.MagicMirror, projectile.velocity * 0.5f, 100, default, 2f).noLight = true;
            }
            if (Main.rand.Next(10) == 0)
            {
                Dust.NewDustPerfect(projectile.Center, DustID.ShadowbeamStaff, projectile.velocity * 0.5f, 100, default, 2f).noLight = true;
            }

            projectile.rotation += 0.3f * (float)projectile.direction;
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

        static Texture2D texture;
        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            UsefulFunctions.DrawSimpleLitProjectile(spriteBatch, projectile, ref texture);
            return false;
        }
    }
}
