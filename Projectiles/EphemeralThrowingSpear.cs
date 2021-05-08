using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class EphemeralThrowingSpear : ModProjectile {
        public override void SetStaticDefaults() {
            Main.projFrames[projectile.type] = 4;
        }
        public override void SetDefaults() {
            projectile.width = 19;
            projectile.height = 19;
            projectile.timeLeft = 180;
            projectile.friendly = true;
            projectile.height = 14;
            projectile.penetrate = 2;
            projectile.melee = true;
            projectile.scale = 0.9f;
            projectile.tileCollide = false;
            projectile.width = 14;
            drawOffsetX = -10;
        }
        public override void AI() {
            projectile.ai[0] += 1f;
            if (++projectile.frameCounter >= 6) {
                projectile.frameCounter = 0;
                if (++projectile.frame >= 4) {
                    projectile.frame = 0;
                }
            }
            if (projectile.ai[0] >= 15f) { 
                projectile.ai[0] = 15f;
                projectile.velocity.Y += 0.1f;
            }
            if (projectile.velocity.Y > 16f) {
                projectile.velocity.Y = 16f;
            }
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.ToRadians(90f);

            int dust = Dust.NewDust(new Vector2((float)projectile.position.X, (float)projectile.position.Y), projectile.width, projectile.height, 245, 0, 0, 50, default, 1.2f);
            Main.dust[dust].noGravity = true;
        }
        public override void Kill(int timeLeft) {
            Main.PlaySound(SoundID.Dig, (int)projectile.position.X, (int)projectile.position.Y, 1, .6f);
            for (int i = 0; i < 10; i++) {
                Vector2 projPosition = new Vector2(projectile.position.X, projectile.position.Y);
                Dust.NewDust(projPosition, projectile.width, projectile.height, 245, 0f, 0f, 0, default, 1f);
            }
        }
        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            for (int d = 0; d < 20; d++)
            {
                int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 245, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 50, default(Color), 1.2f);
                Main.dust[dust].noGravity = true;
            }
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];

            spriteBatch.Draw(texture, projectile.Center - Main.screenPosition, new Rectangle(0, projectile.frame * 106, 34, 106), Color.White, projectile.rotation, new Vector2(16, 16), projectile.scale, SpriteEffects.None, 0);

            return false;
        }
    }
}
