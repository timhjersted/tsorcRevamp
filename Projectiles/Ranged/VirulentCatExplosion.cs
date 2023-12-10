using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;


namespace tsorcRevamp.Projectiles.Ranged
{
    class VirulentCatExplosion : ModProjectile
    {
        public override void SetDefaults()
        {
            // while the sprite is actually bigger than 15x15, we use 15x15 since it lets the projectile clip into tiles as it bounces. It looks better.
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.aiStyle = 0;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 2;
            Projectile.penetrate = -1; //this can be removed to only damage the host
            DrawOffsetX = -2;
            DrawOriginOffsetY = -2;
            Projectile.usesLocalNPCImmunity = true; //any amount of explosions can damage a target simultaneously
            Projectile.localNPCHitCooldown = -1; //but a single explosion can never damage the same enemy more than once
            Projectile.alpha = 255;
        }

        public override void AI()
        {

            float tags = Projectile.ai[0];

            Projectile.position.X = Projectile.position.X + (float)(Projectile.width / 2);
            Projectile.position.Y = Projectile.position.Y + (float)(Projectile.height / 2);
            Projectile.width = ((int)tags * 17) + 30;
            Projectile.height = ((int)tags * 17) + 30;
            Projectile.position.X = Projectile.position.X - (float)(Projectile.width / 2);
            Projectile.position.Y = Projectile.position.Y - (float)(Projectile.height / 2);
            Projectile.damage += (int)tags * 5;
            Projectile.knockBack = (tags * 1.2f) + 4.5f;


            float loops = (tags * 2) + 10;

            if (tags < 4)
            {
                for (int i = 0; i < loops; i++)
                {
                    int dustIndex = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 75, 0f, 0f, 100, default(Color), 2f);
                    Main.dust[dustIndex].noGravity = true;
                    Main.dust[dustIndex].velocity *= 2.5f;
                    dustIndex = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 107, 0f, 0f, 100, default(Color), 1f);
                    Main.dust[dustIndex].velocity *= 3.5f;
                    Main.dust[dustIndex].noGravity = true;
                    dustIndex = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 107, 0f, 0f, 100, default(Color), 1.5f);
                    Main.dust[dustIndex].velocity *= 2.5f;
                    Main.dust[dustIndex].noGravity = true;
                    dustIndex = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 75, 0f, 0f, 100, default(Color), 1.5f);
                    Main.dust[dustIndex].velocity *= 3f;
                    Main.dust[dustIndex].noGravity = true;
                }
            }

            if (tags >= 4 && tags < 8)
            {
                for (int i = 0; i < loops; i++)
                {
                    int dustIndex = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 75, 0f, 0f, 100, default(Color), 2f);
                    Main.dust[dustIndex].noGravity = true;
                    Main.dust[dustIndex].velocity *= 3.5f;
                    dustIndex = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 107, 0f, 0f, 100, default(Color), 1f);
                    Main.dust[dustIndex].velocity *= 4.5f;
                    Main.dust[dustIndex].noGravity = true;
                    dustIndex = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 107, 0f, 0f, 100, default(Color), 1.5f);
                    Main.dust[dustIndex].velocity *= 3.5f;
                    Main.dust[dustIndex].noGravity = true;
                    dustIndex = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 75, 0f, 0f, 100, default(Color), 1.5f);
                    Main.dust[dustIndex].velocity *= 4f;
                    Main.dust[dustIndex].noGravity = true;
                }
            }

            if (tags >= 8 && tags <= 10)
            {
                for (int i = 0; i < loops; i++)
                {
                    int dustIndex = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 75, 1f, 1f, 100, default(Color), 2f);
                    Main.dust[dustIndex].noGravity = true;
                    Main.dust[dustIndex].velocity *= 4f;
                    dustIndex = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 107, 1f, 1f, 100, default(Color), 1f);
                    Main.dust[dustIndex].velocity *= 5f;
                    Main.dust[dustIndex].noGravity = true;
                    dustIndex = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 107, 0f, 0f, 100, default(Color), 1.5f);
                    Main.dust[dustIndex].velocity *= 4f;
                    Main.dust[dustIndex].noGravity = true;
                    dustIndex = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 75, 0f, 0f, 100, default(Color), 1.5f);
                    Main.dust[dustIndex].velocity *= 4.5f;
                    Main.dust[dustIndex].noGravity = true;
                }
            }
        }
    }
}