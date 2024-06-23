using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class PhantomSeeker : ModProjectile
    {

        private const string TexturePath = "tsorcRevamp/Projectiles/Comet";

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.penetrate = 3;
            Projectile.width = 15;
            Projectile.height = 15;
            Projectile.alpha = 255;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.MaxUpdates = 2;
            Projectile.timeLeft = 400;
        }

        Vector2[] lastpos = new Vector2[20];
        int lastposindex = 0;
        public override void AI()
        {
            Projectile.rotation = (float)Math.Atan2((double)Projectile.velocity.Y, (double)Projectile.velocity.X);

            if (Projectile.timeLeft < 100)
            {
                Projectile.scale *= 0.9f;
                Projectile.damage = 0;
            }

            if (Projectile.timeLeft > 150 && Projectile.timeLeft < 500)
            {
                Projectile.velocity.X -= (Projectile.position.X - Main.player[(int)Projectile.ai[0]].position.X) / 1300f;
                Projectile.velocity.Y -= (Projectile.position.Y - Main.player[(int)Projectile.ai[0]].position.Y) / 1300f;

                Projectile.rotation = (float)Math.Atan2((double)Projectile.velocity.Y, (double)Projectile.velocity.X);
                Projectile.velocity.Y = (float)Math.Sin(Projectile.rotation) * 8;
                Projectile.velocity.X = (float)Math.Cos(Projectile.rotation) * 8;
            }

            lastpos[lastposindex] = Projectile.position;
            lastposindex++;
            if (lastposindex > 19) lastposindex = 0;

        }
        public override void PostDraw(Color lightColor)
        {
            Rectangle fromrect = new Rectangle(0, 0, this.Projectile.width, this.Projectile.height);
            Vector2 PC;
            Color targetColor = new Color(0, 50, 255, 0);
            int modlastposindex = lastposindex;
            for (int i = 0; i < 19; i++)
            {
                float rotmod = Main.rand.Next(-100, 100) / 100f;
                float scalemod = Main.rand.Next(50, 150) / 100f;
                lastpos[modlastposindex].X += Main.rand.Next(-1, 1);
                lastpos[modlastposindex].Y += Main.rand.Next(-1, 1);
                PC = lastpos[modlastposindex] + new Vector2(this.Projectile.width / 2, this.Projectile.height / 2);


                Main.spriteBatch.Draw(
                            (Texture2D)Terraria.GameContent.TextureAssets.Projectile[ModContent.ProjectileType<Projectiles.Comet>()],
                            PC - Main.screenPosition,
                            fromrect,
                            targetColor,
                            this.Projectile.rotation + rotmod,
                            new Vector2(this.Projectile.width / 2, this.Projectile.height / 2),
                            1f * (0.1f * i) * this.Projectile.scale * scalemod,
                            SpriteEffects.None,
                            0f);
                modlastposindex++;
                if (modlastposindex > 19) modlastposindex = 0;

            }
            targetColor = new Color(0, 0, 255, 0);
            modlastposindex = lastposindex;

            for (int i = 0; i < 19; i++)
            {
                float rotmod = Main.rand.Next(-100, 100) / 100f;
                float scalemod = Main.rand.Next(50, 150) / 100f;
                PC = lastpos[modlastposindex] + new Vector2(this.Projectile.width / 2, this.Projectile.height / 2);

                Main.spriteBatch.Draw(
                            (Texture2D)Terraria.GameContent.TextureAssets.Projectile[ModContent.ProjectileType<Projectiles.Comet>()],
                            PC - Main.screenPosition,
                            fromrect,
                            targetColor,
                            this.Projectile.rotation + rotmod,
                            new Vector2(this.Projectile.width / 2, this.Projectile.height / 2),
                            1f * (0.09f * i) * this.Projectile.scale * scalemod,
                            SpriteEffects.None,
                            0f);
                modlastposindex++;
                if (modlastposindex > 19) modlastposindex = 0;

            }
            return;
        }
    }
}
