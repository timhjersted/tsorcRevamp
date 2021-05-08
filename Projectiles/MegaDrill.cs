using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class MegaDrill : ModProjectile {

        public override void SetDefaults() {
            projectile.aiStyle = -1;
            projectile.friendly = true;
            projectile.penetrate = 10;
            projectile.width = 15;
            projectile.height = 15;
            projectile.alpha = 255;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.MaxUpdates = 2;
            projectile.light = 5;
            drawHeldProjInFrontOfHeldItemAndArms = true;
        }

        Vector2[] lastpos = new Vector2[20];
        int lastposindex = 0;
        public override void AI() {

            Player projOwner = Main.player[projectile.owner];
            projOwner.heldProj = projectile.whoAmI; //this makes it appear in front of the player

            projectile.velocity.Y = (float)Math.Sin(projectile.rotation) * 10;
            projectile.velocity.X = (float)Math.Cos(projectile.rotation) * 10;

            if (projectile.timeLeft < 100) {
                projectile.scale *= 0.9f;
                projectile.damage = 500;
            }
            if (projectile.scale <= 0.2f && projectile.timeLeft > 1) projectile.timeLeft = 1;

            int projectilePosX = (int)(projectile.position.X + projectile.width / 2) / 16;
            int projectilePosY = (int)(projectile.position.Y + projectile.width / 2) / 16;
            if (Main.tile[projectilePosX, projectilePosY].active()) {
                WorldGen.KillTile(projectilePosX, projectilePosY, false, false, false);
                projectile.timeLeft -= 50;
            }

            lastpos[lastposindex] = projectile.position;
            lastposindex++;
            if (lastposindex > 19) lastposindex = 0;
        }

        public override bool PreDraw(SpriteBatch sp, Color lightColor) {
            Texture2D MyTexture = Main.projectileTexture[ModContent.ProjectileType<MegaDrill>()];
            Rectangle fromrect = new Rectangle(0, 0, projectile.width, projectile.height);
            Vector2 PC;
            Color targetColor = new Color(10, 50, 255, 0);
            int modlastposindex = lastposindex;
            for (int i = 0; i < 19; i++) {
                float rotmod = Main.rand.Next(-100, 100) / 100f;
                float scalemod = Main.rand.Next(50, 150) / 100f;
                lastpos[modlastposindex].X += Main.rand.Next(-1, 1);
                lastpos[modlastposindex].Y += Main.rand.Next(-1, 1);
                PC = lastpos[modlastposindex] + new Vector2(projectile.width / 2, projectile.height / 2);


                sp.Draw(
                            MyTexture,
                            PC - Main.screenPosition,
                            fromrect,
                            targetColor,
                            projectile.rotation + rotmod,
                            new Vector2(projectile.width / 2, projectile.height / 2),
                            1f * (0.1f * i) * projectile.scale * scalemod,
                            SpriteEffects.None,
                            0f);
                modlastposindex++;
                if (modlastposindex > 19) modlastposindex = 0;
                targetColor = new Color(10, 50, 255, 25);
            }
            modlastposindex = lastposindex;

            for (int i = 0; i < 19; i++) {
                float rotmod = Main.rand.Next(-100, 100) / 100f;
                float scalemod = Main.rand.Next(50, 150) / 100f;
                PC = lastpos[modlastposindex] + new Vector2(projectile.width / 2, projectile.height / 2);

                sp.Draw(
                            MyTexture,
                            PC - Main.screenPosition,
                            fromrect,
                            targetColor,
                            projectile.rotation + rotmod,
                            new Vector2(projectile.width / 2, projectile.height / 2),
                            1f * (0.09f * i) * projectile.scale * scalemod,
                            SpriteEffects.None,
                            0f);
                modlastposindex++;
                if (modlastposindex > 19) modlastposindex = 0;

            }
            return true;
        }
    }
}
