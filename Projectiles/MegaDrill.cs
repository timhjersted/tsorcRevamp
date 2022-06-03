using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class MegaDrill : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.penetrate = 10;
            Projectile.width = 15;
            Projectile.height = 15;
            Projectile.alpha = 255;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.MaxUpdates = 2;
            Projectile.light = 5;
            DrawHeldProjInFrontOfHeldItemAndArms = true;
        }

        Vector2[] lastpos = new Vector2[20];
        int lastposindex = 0;
        public override void AI()
        {

            Player projOwner = Main.player[Projectile.owner];
            projOwner.heldProj = Projectile.whoAmI; //this makes it appear in front of the player

            Projectile.velocity.Y = (float)Math.Sin(Projectile.rotation) * 10;
            Projectile.velocity.X = (float)Math.Cos(Projectile.rotation) * 10;

            if (Projectile.timeLeft < 100)
            {
                Projectile.scale *= 0.9f;
                Projectile.damage = 500;
            }
            if (Projectile.scale <= 0.2f && Projectile.timeLeft > 1) Projectile.timeLeft = 1;

            int projectilePosX = (int)(Projectile.position.X + Projectile.width / 2) / 16;
            int projectilePosY = (int)(Projectile.position.Y + Projectile.width / 2) / 16;
            if (Main.tile[projectilePosX, projectilePosY].HasTile)
            {
                WorldGen.KillTile(projectilePosX, projectilePosY, false, false, false);
                Projectile.timeLeft -= 50;
            }

            lastpos[lastposindex] = Projectile.position;
            lastposindex++;
            if (lastposindex > 19) lastposindex = 0;
        }


        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D MyTexture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[ModContent.ProjectileType<MegaDrill>()];
            Rectangle fromrect = new Rectangle(0, 0, Projectile.width, Projectile.height);
            Vector2 PC;
            Color targetColor = new Color(10, 50, 255, 0);
            int modlastposindex = lastposindex;
            for (int i = 0; i < 19; i++)
            {
                float rotmod = Main.rand.Next(-100, 100) / 100f;
                float scalemod = Main.rand.Next(50, 150) / 100f;
                lastpos[modlastposindex].X += Main.rand.Next(-1, 1);
                lastpos[modlastposindex].Y += Main.rand.Next(-1, 1);
                PC = lastpos[modlastposindex] + new Vector2(Projectile.width / 2, Projectile.height / 2);


                Main.spriteBatch.Draw(
                            MyTexture,
                            PC - Main.screenPosition,
                            fromrect,
                            targetColor,
                            Projectile.rotation + rotmod,
                            new Vector2(Projectile.width / 2, Projectile.height / 2),
                            1f * (0.1f * i) * Projectile.scale * scalemod,
                            SpriteEffects.None,
                            0f);
                modlastposindex++;
                if (modlastposindex > 19) modlastposindex = 0;
                targetColor = new Color(10, 50, 255, 25);
            }
            modlastposindex = lastposindex;

            for (int i = 0; i < 19; i++)
            {
                float rotmod = Main.rand.Next(-100, 100) / 100f;
                float scalemod = Main.rand.Next(50, 150) / 100f;
                PC = lastpos[modlastposindex] + new Vector2(Projectile.width / 2, Projectile.height / 2);

                Main.spriteBatch.Draw(
                            MyTexture,
                            PC - Main.screenPosition,
                            fromrect,
                            targetColor,
                            Projectile.rotation + rotmod,
                            new Vector2(Projectile.width / 2, Projectile.height / 2),
                            1f * (0.09f * i) * Projectile.scale * scalemod,
                            SpriteEffects.None,
                            0f);
                modlastposindex++;
                if (modlastposindex > 19) modlastposindex = 0;

            }
            return true;
        }
    }
}
