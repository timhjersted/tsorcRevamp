using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Projectiles {
    class Comet : ModProjectile {
        public override void SetDefaults()
        {
            projectile.hostile = true;
            projectile.penetrate = 3;
            projectile.width = 15;
            projectile.height = 15;
            projectile.alpha = 255;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.MaxUpdates = 2;
        }


        Vector2[] lastpos = new Vector2[20];
        int lastposindex = 0;
        public override void AI() {
            projectile.rotation = (float)Math.Atan2((double)projectile.velocity.Y, (double)projectile.velocity.X);


            projectile.rotation += (Main.rand.Next(-100, 100)) / 2400f;

            projectile.velocity.Y = (float)Math.Sin(projectile.rotation) * projectile.ai[1];
            projectile.velocity.X = (float)Math.Cos(projectile.rotation) * projectile.ai[1];

            if (projectile.timeLeft < 100) {
                projectile.scale *= 0.9f;
                projectile.damage = 0;
            }

            lastpos[lastposindex] = projectile.position;
            lastposindex++;
            if (lastposindex > 19) lastposindex = 0;

        }

        public override void PostDraw(SpriteBatch spriteBatch, Color lightColor) {
            Random rand1 = new Random((int)Main.time);
            Rectangle fromrect = new Rectangle(0, 0, this.projectile.width, this.projectile.height);
            Vector2 PC;
            Color targetColor = new Color(0, 50, 255, 0);
            int modlastposindex = lastposindex;
            for (int i = 0; i < 19; i++) {
                float rotmod = rand1.Next(-100, 100) / 100f;
                float scalemod = rand1.Next(50, 150) / 100f;
                lastpos[modlastposindex].X += rand1.Next(-1, 1);
                lastpos[modlastposindex].Y += rand1.Next(-1, 1);
                PC = lastpos[modlastposindex] + new Vector2(this.projectile.width / 2, this.projectile.height / 2);


                spriteBatch.Draw(
                            Main.projectileTexture[projectile.type],
                            PC - Main.screenPosition,
                            fromrect,
                            targetColor,
                            this.projectile.rotation + rotmod,
                            new Vector2(this.projectile.width / 2, this.projectile.height / 2),
                            1f * (0.1f * i) * this.projectile.scale * scalemod,
                            SpriteEffects.None,
                            0f);
                modlastposindex++;
                if (modlastposindex > 19) modlastposindex = 0;

            }
            targetColor = new Color(0, 0, 255, 0);
            modlastposindex = lastposindex;
            rand1 = new Random((int)Main.time);

            for (int i = 0; i < 19; i++) {
                float rotmod = rand1.Next(-100, 100) / 100f;
                float scalemod = rand1.Next(50, 150) / 100f;
                PC = lastpos[modlastposindex] + new Vector2(this.projectile.width / 2, this.projectile.height / 2);

                spriteBatch.Draw(
                            Main.projectileTexture[projectile.type],
                            PC - Main.screenPosition,
                            fromrect,
                            targetColor,
                            this.projectile.rotation + rotmod,
                            new Vector2(this.projectile.width / 2, this.projectile.height / 2),
                            1f * (0.09f * i) * this.projectile.scale * scalemod,
                            SpriteEffects.None,
                            0f);
                modlastposindex++;
                if (modlastposindex > 19) modlastposindex = 0;

            }
            return;
        }
    }
}
