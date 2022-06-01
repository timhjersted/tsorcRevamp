using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class Comet : ModProjectile
    {
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
        }


        Vector2[] lastpos = new Vector2[20];
        int lastposindex = 0;
        public override void AI()
        {
            Projectile.rotation = (float)Math.Atan2((double)Projectile.velocity.Y, (double)Projectile.velocity.X);


            Projectile.rotation += (Main.rand.Next(-100, 100)) / 2400f;

            Projectile.velocity.Y = (float)Math.Sin(Projectile.rotation) * Projectile.ai[1];
            Projectile.velocity.X = (float)Math.Cos(Projectile.rotation) * Projectile.ai[1];

            if (Projectile.timeLeft < 100)
            {
                Projectile.scale *= 0.9f;
                Projectile.damage = 0;
            }

            lastpos[lastposindex] = Projectile.position;
            lastposindex++;
            if (lastposindex > 19) lastposindex = 0;

        }

        public override void PostDraw(Color lightColor)
        {
            Random rand1 = new Random((int)Main.GameUpdateCount);
            Rectangle fromrect = new Rectangle(0, 0, this.Projectile.width, this.Projectile.height);
            Vector2 PC;
            Color targetColor = new Color(0, 50, 255, 0);
            int modlastposindex = lastposindex;
            for (int i = 0; i < 19; i++)
            {
                float rotmod = rand1.Next(-100, 100) / 100f;
                float scalemod = rand1.Next(50, 150) / 100f;
                lastpos[modlastposindex].X += rand1.Next(-1, 1);
                lastpos[modlastposindex].Y += rand1.Next(-1, 1);
                PC = lastpos[modlastposindex] + new Vector2(this.Projectile.width / 2, this.Projectile.height / 2);


                Main.EntitySpriteDraw(
                            (Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type],
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
            rand1 = new Random((int)Main.GameUpdateCount);

            for (int i = 0; i < 19; i++)
            {
                float rotmod = rand1.Next(-100, 100) / 100f;
                float scalemod = rand1.Next(50, 150) / 100f;
                PC = lastpos[modlastposindex] + new Vector2(this.Projectile.width / 2, this.Projectile.height / 2);

                Main.EntitySpriteDraw(
                            (Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type],
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
