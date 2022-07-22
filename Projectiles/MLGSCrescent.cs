using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class MLGSCrescent : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.penetrate = 5;
            Projectile.friendly = true;
            if (Main.dayTime)
            {
                Projectile.tileCollide = true;
            }
            else
            {
                Projectile.tileCollide = false;
            }
            Projectile.DamageType = DamageClass.Melee;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type];
            Color color;
            if (Main.dayTime)
            {
                color = lightColor;
            }
            else
            {
                color = Color.White;
            }

            if (Projectile.ai[0] > 8)
            {
                Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, new Rectangle(0, Projectile.frame * 64, 68, 64), color, Projectile.rotation, new Vector2(34, 32), Projectile.scale, SpriteEffects.None, 0);
            }

            return false;
        }

        public override void Kill(int timeLeft)
        {
            for (int i = 0; i < 20; i++)
            {
                int dust = Dust.NewDust(new Vector2(Projectile.position.X - 11, Projectile.position.Y - 11), Projectile.width + 22, Projectile.height + 22, 89, 0, 0, 70, default(Color), 1f);
                Main.dust[dust].noGravity = true;
            }
        }


        public override void AI()
        {
            Projectile.ai[0]++;
            Projectile.rotation = Projectile.velocity.ToRotation();

            Projectile.velocity *= 1.2f;

            if (Projectile.ai[0] > 8)
            {

                Lighting.AddLight(Projectile.position, 0.0452f, 0.21f, 0.1f);

                for (int d = 0; d < 2; d++)
                {
                    int dust = Dust.NewDust(new Vector2(Projectile.position.X - 11, Projectile.position.Y - 11), Projectile.width + 22, Projectile.height + 22, 89, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 30, default(Color), 1f);
                    Main.dust[dust].noGravity = true;

                }

                for (int d = 0; d < 2; d++)
                {
                    int dust = Dust.NewDust(new Vector2(Projectile.position.X - 11, Projectile.position.Y - 11), Projectile.width + 22, Projectile.height + 22, 110, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 30, default(Color), 1f);
                    Main.dust[dust].noGravity = true;
                }

                if (++Projectile.frameCounter >= 3)
                {
                    Projectile.frameCounter = 0;
                    if (++Projectile.frame > 2)
                    {
                        Projectile.Kill();
                    }
                }
            }

        }
    }
}
