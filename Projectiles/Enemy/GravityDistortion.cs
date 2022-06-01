using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Projectiles.Enemy {
    class GravityDistortion : ModProjectile {

        public override void SetDefaults() {
            Projectile.width = 30;
            Projectile.height = 30;
			Projectile.scale = 1.5f; //It's working properly now, apparently for scale to work you have to override predraw and tell it to draw the projectile right
            Projectile.hostile = true;
            Projectile.damage = 80;
            Projectile.penetrate = 2;
            Projectile.tileCollide = false;
			Projectile.timeLeft = 200;
        }
        public override void Kill(int timeLeft) {
            Projectile.type = 79;
			for (int num36 = 0; num36 < 20; num36++)
			{
				int dust = Dust.NewDust(Projectile.position, (int)(Projectile.width), (int)(Projectile.height), DustID.PurpleTorch, Main.rand.Next(-15, 15), Main.rand.Next(-15, 15), 100, new Color(), 9f);
				Main.dust[dust].noGravity = true;
			}
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
            Main.EntitySpriteDraw(texture,
                Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                sourceRectangle, drawColor, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0f);

            return false;
        }

		float homingStrength = 0.08f;
		//ai[1] is the projectie's maximum speed, as specified when it's spawned
        public override void AI() {
			Projectile.rotation += 0.5f;
			if (Main.player[(int)Projectile.ai[0]].position.X < Projectile.position.X) {
				if (Projectile.velocity.X > -10) Projectile.velocity.X -= homingStrength;
			}

			if (Main.player[(int)Projectile.ai[0]].position.X > Projectile.position.X) {
				if (Projectile.velocity.X < 10) Projectile.velocity.X += homingStrength;
			}

			if (Main.player[(int)Projectile.ai[0]].position.Y < Projectile.position.Y) {
				if (Projectile.velocity.Y > -10) Projectile.velocity.Y -= homingStrength;
			}

			if (Main.player[(int)Projectile.ai[0]].position.Y > Projectile.position.Y) {
				if (Projectile.velocity.Y < 10) Projectile.velocity.Y += homingStrength;
			}

			if(Projectile.velocity.Y > Projectile.ai[1])
            {
				Projectile.velocity.Y = Projectile.ai[1];
			}
			if (Projectile.velocity.Y < -Projectile.ai[1])
			{
				Projectile.velocity.Y = -Projectile.ai[1];
			}
			if (Projectile.velocity.X > Projectile.ai[1])
			{
				Projectile.velocity.X = Projectile.ai[1];
			}
			if (Projectile.velocity.X < -Projectile.ai[1])
			{
				Projectile.velocity.X = -Projectile.ai[1];
			}

			if (Main.rand.Next(4) == 0) {
				int dust = Dust.NewDust(new Vector2((float)Projectile.position.X + 10, (float)Projectile.position.Y), Projectile.width, Projectile.height, DustID.Torch, 0, 0, 200, Color.Red, 1f);
				Main.dust[dust].noGravity = true;
			}
			Lighting.AddLight((int)(Projectile.position.X / 16f), (int)(Projectile.position.Y / 16f), 0.7f, 0.2f, 0.2f);
		}

        public override void OnHitPlayer(Player target, int damage, bool crit) {
			target.AddBuff(BuffID.Gravitation, 180);
        }
    }
}
