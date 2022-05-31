using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class Crescent : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 28;
        }

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.penetrate = 3;
            Projectile.friendly = true;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Melee;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[Projectile.type];

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, new Rectangle(0, Projectile.frame * 64, 68, 64), Color.White, Projectile.rotation, new Vector2(34, 32), Projectile.scale, SpriteEffects.None, 0);

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
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            for (int i = 0; i < 30; i++)
            {
                int dust = Dust.NewDust(new Vector2(Projectile.position.X - 11, Projectile.position.Y - 11), Projectile.width + 22, Projectile.height + 22, 89, Projectile.velocity.X, Projectile.velocity.Y, 70, default(Color), 1f);
                Main.dust[dust].noGravity = true;
            }
            return true;
        }

        public bool spawned = false;
        public int ChosenStartFrame = 0;

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
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

            if (!spawned)
            {
                int[] StartFrameChoices = new int[] { 0, 7, 14, 21 };
                int StartFrame = Main.rand.Next(StartFrameChoices);


                spawned = true;
                ChosenStartFrame = StartFrame;
                Projectile.frame = ChosenStartFrame;

            }

            if (spawned && ChosenStartFrame == 0)
            {
                if (++Projectile.frameCounter >= 2)
                {
                    Projectile.frameCounter = 0;
                    if (++Projectile.frame > 7)
                    {
                        Projectile.Kill();
                    }
                }
            }

            if (spawned && ChosenStartFrame == 7)
            {
                if (++Projectile.frameCounter >= 2)
                {
                    Projectile.frameCounter = 0;
                    if (++Projectile.frame > 13)
                    {
                        Projectile.Kill();
                    }
                }
            }

            if (spawned && ChosenStartFrame == 14)
            {
                if (++Projectile.frameCounter >= 2)
                {
                    Projectile.frameCounter = 0;
                    if (++Projectile.frame > 20)
                    {
                        Projectile.Kill();
                    }
                }
            }

            if (spawned && ChosenStartFrame == 21)
            {
                if (++Projectile.frameCounter >= 2)
                {
                    Projectile.frameCounter = 0;
                    if (++Projectile.frame > 27)
                    {
                        Projectile.Kill();
                    }
                }
            }
        }
    }
}

