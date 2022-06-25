using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Okiku
{
    public class ObscureDrop : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 15;
            Projectile.height = 15;
            Projectile.aiStyle = 1;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 300;
        }
        public override bool PreKill(int timeLeft)
        {
            Projectile.type = 44; //killpretendtype
            return true;
        }
        public override bool PreAI()
        {
            if (Projectile.velocity.Y < 0)
            {
                Projectile.alpha = 50;
                if (Main.rand.Next(2) == 0)
                {
                    int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, 62, 0, 0, 200, Color.White, 2.0f);
                    Main.dust[dust].noGravity = true;
                }
            }
            else
            {
                Projectile.alpha = 10;
                if (Main.rand.Next(2) == 0)
                {
                    int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, 62, 0, 0, 100, Color.White, 2.0f);
                    Main.dust[dust].noGravity = true;
                }
            }

            if (Projectile.velocity.Y < 10) Projectile.velocity.Y += 0.1f;

            return true;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            if (Main.rand.Next(2) == 0)
            {
                target.AddBuff(BuffID.Weak, 600);
                target.AddBuff(BuffID.OnFire, 180);
                target.AddBuff(ModContent.BuffType<Buffs.CurseBuildup>(), 7200);
            }

            if (Main.rand.Next(8) == 0)
            {
                target.AddBuff(ModContent.BuffType<Buffs.FracturingArmor>(), 1800);
            }
        }

        //This is too hard to see especially at night, so i'm making it ignore all lighting and always draw at full brightness
        static Texture2D texture = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/Okiku/ObscureDrop");
        public override bool PreDraw(ref Color lightColor)
        {
            if (texture == null || texture.IsDisposed)
            {
                texture = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/Okiku/ObscureDrop");
            }
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (Projectile.spriteDirection == -1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }
            //Get the premultiplied, properly transparent texture
            int frameHeight = ((Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type]).Height / Main.projFrames[Projectile.type];
            int startY = frameHeight * Projectile.frame;
            Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
            Vector2 origin = sourceRectangle.Size() / 2f;
            Main.EntitySpriteDraw(texture,
                Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                sourceRectangle, Color.White, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0);

            return false;
        }
    }
}
