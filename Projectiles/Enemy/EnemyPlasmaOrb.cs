using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class EnemyPlasmaOrb : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.aiStyle = 0;
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.hostile = true;
            Projectile.timeLeft = 1500;
            Projectile.scale = 2.2f;
            Projectile.tileCollide = false;
            Main.projFrames[Projectile.type] = 4;
            Projectile.light = 1;
            Projectile.timeLeft = 150;
            //DrawOffsetX = 50;
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Omega Blast");
        }

        public override bool PreKill(int timeLeft)
        {
            Projectile.type = 44; //killpretendtype
            for (int num36 = 0; num36 < 10; num36++)
            {
                int dust = Dust.NewDust(Projectile.position, (int)(Projectile.width), (int)(Projectile.height), DustID.Firework_Blue, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 100, new Color(), 9f);
                Main.dust[dust].noGravity = true;
            }
            return true;
        }

        public override void AI()
        {
            Projectile.rotation += 1f;

            if (Main.rand.Next(2) == 0)
            {

                Lighting.AddLight((int)Projectile.position.X / 16, (int)Projectile.position.Y / 16, 15f, 0f, 0.1f);
                int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, DustID.Shadowflame, 0, 0, 100, Color.Green, 1.0f);
                Main.dust[dust].noGravity = true;
                int pdust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, DustID.CorruptGibs, 0, 0, 100, Color.Green, 1.0f);
                Main.dust[pdust].noGravity = true;
            }

            Projectile.frameCounter++;
            if (Projectile.frameCounter > 2)
            {
                Projectile.frame++;
                Projectile.frameCounter = 3;
            }
            if (Projectile.frame >= 4)
            {
                Projectile.frame = 0;
            }
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            int buffLengthMod = 1;
            if (Main.expertMode)
            {
                buffLengthMod = 2;
            }

            base.OnHitPlayer(target, damage, crit);
            target.AddBuff(BuffID.Weak, 1200 / buffLengthMod, false);
            target.AddBuff(BuffID.Slow, 600 / buffLengthMod, false);

        }

        public override bool PreDraw(ref Color lightColor)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (Projectile.spriteDirection == -1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }
            //Get the premultiplied, properly transparent texture
            Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.EnemyPlasmaOrb];
            int frameHeight = texture.Height / Main.projFrames[Projectile.type];
            int startY = frameHeight * Projectile.frame;
            Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
            Vector2 origin = sourceRectangle.Size() / 2f;
            //origin.X = (float)(projectile.spriteDirection == 1 ? sourceRectangle.Width - 20 : 20);
            Color drawColor = Projectile.GetAlpha(lightColor);
            Main.EntitySpriteDraw(texture,
                Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                sourceRectangle, drawColor, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0);

            return false;
        }
    }
}