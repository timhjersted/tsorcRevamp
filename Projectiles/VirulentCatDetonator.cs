using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class VirulentCatDetonator : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[projectile.type] = 4;
        }
        public override void SetDefaults()
        {

            // while the sprite is actually bigger than 15x15, we use 15x15 since it lets the projectile clip into tiles as it bounces. It looks better.
            projectile.width = 18;
            projectile.height = 12;
            projectile.friendly = true;
            projectile.aiStyle = 0;
            projectile.ranged = true;
            projectile.tileCollide = true;
            projectile.timeLeft = 52;
            projectile.penetrate = 2;
            projectile.usesLocalNPCImmunity = true;
            projectile.localNPCHitCooldown = -1;

            //These 2 help the projectile hitbox be centered on the projectile sprite.
            drawOffsetX = 0;
            drawOriginOffsetY = 0;
        }

        public int virulentcatdetotimer;

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];

            spriteBatch.Draw(texture, projectile.Center - Main.screenPosition, new Rectangle(0, 0, 18, 12), Color.White, projectile.rotation, new Vector2(9, 6), projectile.scale, SpriteEffects.None, 0);

            return false;
        }

        public override void AI()
        {
            if (projectile.localAI[0] == 0f)
            {
                Main.PlaySound(SoundID.Item, (int)projectile.Center.X, (int)projectile.Center.Y, SoundID.Item91.Style, .7f, -0.5f);
                projectile.localAI[0] += 1f;
            }

            Lighting.AddLight(projectile.position, 0.455f, 0.826f, 0.238f);
            projectile.rotation = projectile.velocity.ToRotation(); // projectile faces sprite right

            if (projectile.owner == Main.myPlayer && projectile.timeLeft <= 6)
            {
                projectile.alpha += 25;

                if (projectile.alpha > 255)
                {
                    projectile.alpha = 225;
                }
            }
            for (int d = 0; d < 2; d++)
            {
                int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 75, projectile.velocity.X * 0f, projectile.velocity.Y * 0f, 30, default(Color), 1f);
                Main.dust[dust].noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            Main.PlaySound(SoundID.NPCDeath9.WithVolume(.8f));
            for (int d = 0; d < 20; d++)
            {
                int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 75, projectile.velocity.X * 1f, projectile.velocity.Y * 1f, 30, default(Color), 1f);
                Main.dust[dust].velocity.X = +Main.rand.Next(-50, 51) * 0.05f;
                Main.dust[dust].velocity.Y = +Main.rand.Next(-50, 51) * 0.05f;
                Main.dust[dust].noGravity = true;
            }
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Main.PlaySound(SoundID.NPCDeath9.WithVolume(.8f));
            for (int d = 0; d < 20; d++)
            {
                int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 75, projectile.velocity.X * 1f, projectile.velocity.Y * 1f, 30, default(Color), 1f);
                Main.dust[dust].velocity.X = +Main.rand.Next(-50, 51) * 0.05f;
                Main.dust[dust].velocity.Y = +Main.rand.Next(-50, 51) * 0.05f;
                Main.dust[dust].noGravity = true;

            }
            return true;

        }

        public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.NPCDeath9.WithVolume(.4f));
            for (int d = 0; d < 20; d++)
            {
                int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 75, projectile.velocity.X * 1.2f, projectile.velocity.Y * 1.2f, 30, default(Color), 1f);
                Main.dust[dust].velocity.X = +Main.rand.Next(-50, 51) * 0.05f;
                Main.dust[dust].velocity.Y = +Main.rand.Next(-50, 51) * 0.05f;
                Main.dust[dust].noGravity = true;

            }
        }

    }
}
