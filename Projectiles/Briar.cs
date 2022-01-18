using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class Briar : ModProjectile
    {
        public override void SetDefaults()
        {
            projectile.width = 14;
            projectile.height = 10;
            projectile.friendly = true;
            projectile.penetrate = 2;
            projectile.scale = 1f;
            projectile.alpha = 50;
            //projectile.light = .25f;
            projectile.melee = true;
            projectile.tileCollide = true;
            projectile.timeLeft = 16;
        }
        public override void AI()
        {
            projectile.rotation = projectile.velocity.ToRotation(); // projectile faces sprite right
            int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 96, projectile.velocity.X * -0.3f, projectile.velocity.Y * -.3f, 30, default(Color), 1.3f);
            Main.dust[dust].noGravity = true;
            projectile.ai[0]++;
        }
        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            for (int d = 0; d < 20; d++)
            {
                int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 5, projectile.velocity.X * 1f, projectile.velocity.Y * 1f, 30, default(Color), 1.5f);
                Main.dust[dust].velocity.X = +Main.rand.Next(-50, 51) * 0.1f;
                Main.dust[dust].velocity.Y = +Main.rand.Next(-50, 51) * 0.1f;
                Main.dust[dust].noGravity = true;
            }
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Main.PlaySound(SoundID.NPCDeath9.WithVolume(.5f), projectile.position);
            for (int d = 0; d < 15; d++)
            {
                int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 5, projectile.velocity.X * 1.2f, projectile.velocity.Y * 1.2f, 30, default(Color), 1.4f);
                Main.dust[dust].noGravity = true;

            }
            for (int d = 0; d < 5; d++)
            {
                int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 96, projectile.velocity.X * 0.8f, projectile.velocity.Y * 0.8f, 30, default(Color), 1.2f);
                Main.dust[dust].noGravity = true;
            }
            return true;
        }

        float colorMult = 0f;

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];

            if (projectile.ai[0] > 10)
            {
                colorMult += 0.12f;
            }

            spriteBatch.Draw(texture, projectile.Center, Color.White * (1 - colorMult));
            spriteBatch.Draw(texture, projectile.Center - Main.screenPosition, new Rectangle(0, 0, 18, 14), Color.White * (1 - colorMult), projectile.rotation, new Vector2(9, 7), projectile.scale, SpriteEffects.None, 0);

            return false;
        }

    }
}
