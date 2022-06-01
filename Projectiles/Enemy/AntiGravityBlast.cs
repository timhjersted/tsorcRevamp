using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Projectiles.Enemy {
    class AntiGravityBlast : ModProjectile {

        public override void SetDefaults() {
            Projectile.width = 100;
            Projectile.height = 100;
			Projectile.scale = 1.3f; //It's working properly now, apparently for scale to work you have to override predraw and tell it to draw the projectile right
            Projectile.hostile = true;
            Projectile.damage = 80;
            Projectile.penetrate = 2;
            Projectile.tileCollide = false;
			Projectile.timeLeft = 600;
        }

        public override void Kill(int timeLeft) {
            Projectile.type = 79;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (Projectile.spriteDirection == -1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }
            //Get the premultiplied, properly transparent texture from the array (if it's not transparent, you can just use Texture2D texture = Main.projectileTexture[projectile.type];
            Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.AntiGravityBlast];
            int frameHeight = Main.projectileTexture[Projectile.type].Height / Main.projFrames[Projectile.type];
            int startY = frameHeight * Projectile.frame;
            Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
            Vector2 origin = sourceRectangle.Size() / 2f;
            Color drawColor = Projectile.GetAlpha(lightColor);
            Main.Main.EntitySpriteDraw(texture,
                Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                sourceRectangle, drawColor, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0f);

            return false;
        }


        public override void AI() {
			Projectile.rotation += 0.5f;
			if (Main.player[(int)Projectile.ai[0]].position.X < Projectile.position.X) {
				if (Projectile.velocity.X > -10) Projectile.velocity.X -= 0.1f;
			}

			if (Main.player[(int)Projectile.ai[0]].position.X > Projectile.position.X) {
				if (Projectile.velocity.X < 10) Projectile.velocity.X += 0.1f;
			}

			if (Main.player[(int)Projectile.ai[0]].position.Y < Projectile.position.Y) {
				if (Projectile.velocity.Y > -10) Projectile.velocity.Y -= 0.1f;
			}

			if (Main.player[(int)Projectile.ai[0]].position.Y > Projectile.position.Y) {
				if (Projectile.velocity.Y < 10) Projectile.velocity.Y += 0.1f;
			}

			if (Main.rand.Next(4) == 0) {
				int dust = Dust.NewDust(new Vector2((float)Projectile.position.X + 10, (float)Projectile.position.Y), Projectile.width, Projectile.height, DustID.Torch, 0, 0, 200, Color.Red, 1f);
				Main.dust[dust].noGravity = true;
			}
			Lighting.AddLight((int)(Projectile.position.X / 16f), (int)(Projectile.position.Y / 16f), 0.7f, 0.2f, 0.2f);
		}

        public override void OnHitPlayer(Player target, int damage, bool crit) {
			target.AddBuff(BuffID.Featherfall, 180);
        }
    }
}
