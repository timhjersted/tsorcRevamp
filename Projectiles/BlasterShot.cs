using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class BlasterShot : ModProjectile
    {
        public override void SetDefaults()
        {

            // while the sprite is actually bigger than 15x15, we use 15x15 since it lets the projectile clip into tiles as it bounces. It looks better.
            projectile.width = 8;
            projectile.height = 8;
            projectile.friendly = true;
            projectile.aiStyle = 0;
            projectile.penetrate = 2;
            projectile.ranged = true;
            projectile.tileCollide = true;
            projectile.timeLeft = 38;
            //These 2 help the projectile hitbox be centered on the projectile sprite.
            drawOffsetX = -2;
            drawOriginOffsetY = -2;
        }

        public override void AI()
        {

            // Rotation increased by velocity.X 
            projectile.rotation += projectile.velocity.X * 0.08f;
            //projectile.rotation = projectile.velocity.ToRotation(); // projectile faces sprite right

            if (projectile.localAI[0] == 0f)
            {
                Main.PlaySound(SoundID.Item, (int)projectile.Center.X, (int)projectile.Center.Y, SoundID.Item91.Style, .7f, -0.6f);
                projectile.localAI[0] += 1f;
            }

            Lighting.AddLight(projectile.position, 0.0452f, 0.24f, 0.24f);

            if (projectile.owner == Main.myPlayer && projectile.timeLeft <= 6)
            {
                projectile.alpha += 25;

                if (projectile.alpha > 255)
                {
                    projectile.alpha = 225;
                }
            }
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            Main.PlaySound(SoundID.Item, (int)projectile.Center.X, (int)projectile.Center.Y, SoundID.Item10.Style, .8f, -0.25f);
            for (int d = 0; d < 30; d++)
            {
                int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 111, projectile.velocity.X * 1f, projectile.velocity.Y * 1f, 30, default(Color), .7f);
                Main.dust[dust].velocity.X = +Main.rand.Next(-50, 51) * 0.085f;
                Main.dust[dust].velocity.Y = +Main.rand.Next(-50, 51) * 0.085f;
                Main.dust[dust].noGravity = true;
            }
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Main.PlaySound(SoundID.Item, (int)projectile.Center.X, (int)projectile.Center.Y, SoundID.Item10.Style, .8f, -0.25f);
            for (int d = 0; d < 30; d++)
            {
                int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 111, projectile.velocity.X * 1f, projectile.velocity.Y * 1f, 30, default(Color), .7f);
                Main.dust[dust].velocity.X = +Main.rand.Next(-50, 51) * 0.085f;
                Main.dust[dust].velocity.Y = +Main.rand.Next(-50, 51) * 0.085f;
                Main.dust[dust].noGravity = true;

            }
            return true;

        }

        public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.Item, (int)projectile.Center.X, (int)projectile.Center.Y, SoundID.Item10.Style, .4f, -0.25f);
            for (int d = 0; d < 30; d++)
            {
                int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 111, projectile.velocity.X * 1.2f, projectile.velocity.Y * 1.2f, 30, default(Color), .7f);
                Main.dust[dust].velocity.X = +Main.rand.Next(-50, 51) * 0.085f;
                Main.dust[dust].velocity.Y = +Main.rand.Next(-50, 51) * 0.085f;
                Main.dust[dust].noGravity = true;

            }
        }

    }
}
