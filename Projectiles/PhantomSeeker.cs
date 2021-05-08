using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class PhantomSeeker : ModProjectile {

        private const string TexturePath = "tsorcRevamp/Projectiles/Comet";

        public override void SetDefaults() {
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

            if (projectile.timeLeft < 100) {
                projectile.scale *= 0.9f;
                projectile.damage = 0;
            }

            if (projectile.timeLeft > 150 && projectile.timeLeft < 500) {
                projectile.velocity.X -= (projectile.position.X - Main.player[(int)projectile.ai[0]].position.X) / 1000f;
                projectile.velocity.Y -= (projectile.position.Y - Main.player[(int)projectile.ai[0]].position.Y) / 1000f;

                projectile.rotation = (float)Math.Atan2((double)projectile.velocity.Y, (double)projectile.velocity.X);
                projectile.velocity.Y = (float)Math.Sin(projectile.rotation) * 8;
                projectile.velocity.X = (float)Math.Cos(projectile.rotation) * 8;
            }

            lastpos[lastposindex] = projectile.position;
            lastposindex++;
            if (lastposindex > 19) lastposindex = 0;

        }
        public override void PostDraw(SpriteBatch sp, Color lightColor) {
            Texture2D MyTexture = ModContent.GetTexture(TexturePath);
            Rectangle fromrect = new Rectangle(0, 0, this.projectile.width, this.projectile.height);
            Vector2 PC;
            Color targetColor = new Color(0, 50, 255, 0);
            int modlastposindex = lastposindex;
            for (int i = 0; i < 19; i++) {
                float rotmod = Main.rand.Next(-100, 100) / 100f;
                float scalemod = Main.rand.Next(50, 150) / 100f;
                lastpos[modlastposindex].X += Main.rand.Next(-1, 1);
                lastpos[modlastposindex].Y += Main.rand.Next(-1, 1);
                PC = lastpos[modlastposindex] + new Vector2(this.projectile.width / 2, this.projectile.height / 2);


                sp.Draw(
                            MyTexture,
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

            for (int i = 0; i < 19; i++) {
                float rotmod = Main.rand.Next(-100, 100) / 100f;
                float scalemod = Main.rand.Next(50, 150) / 100f;
                PC = lastpos[modlastposindex] + new Vector2(this.projectile.width / 2, this.projectile.height / 2);

                sp.Draw(
                            MyTexture,
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
        }
    }
}
