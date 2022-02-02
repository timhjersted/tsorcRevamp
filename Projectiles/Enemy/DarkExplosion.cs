using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
	public class DarkExplosion : ModProjectile
	{
		public override void SetDefaults()
		{
			Main.projFrames[projectile.type] = 6;
			projectile.aiStyle = 0;
			projectile.hostile = true;
			projectile.alpha = 215;
			projectile.height = 76;
			projectile.scale = 2;
			projectile.tileCollide = false;
			projectile.width = 76;
			projectile.timeLeft = 1500;
		}
		public override void AI()
		{
			//if (projectile.frame < 5)
			projectile.frameCounter++;

			if (projectile.frameCounter == 30)
			{
				projectile.frame = 1;
				projectile.alpha = 200;
			}

			if (projectile.frameCounter == 60)
			{
				projectile.frame = 2;
				projectile.alpha = 175;
			}

			if (projectile.frameCounter == 90)
			{
				projectile.frame = 3;
				projectile.alpha = 150;
			}
			if (projectile.frameCounter == 120)
			{
				projectile.frame = 4;
				projectile.alpha = 125;
			}
			if (projectile.frameCounter == 180)
			{
				projectile.frame = 5;
				projectile.alpha = 50;
				projectile.hostile = true;
				for (int num36 = 0; num36 < 20; num36++)
				{
					int dustDeath = Dust.NewDust(projectile.position, projectile.width, projectile.height, 55, Main.rand.Next(-6, 6), Main.rand.Next(-6, 6), 200, Color.White, 2f);
					Main.dust[dustDeath].noGravity = true;
				}
			}
			if (projectile.frameCounter >= 182)
			{
				projectile.active = false;
			}

			projectile.rotation += 0.1f;

			if (Main.rand.Next(2) == 0)
			{
				int dust = Dust.NewDust(new Vector2((float)projectile.position.X, (float)projectile.position.Y), projectile.width, projectile.height, 6, 0, 0, projectile.alpha, Color.White, 2.0f);
				Main.dust[dust].noGravity = true;
			}
		}

		public override void OnHitPlayer(Player target, int damage, bool crit)
		{
			//Vanilla Debuffs cut in half to counter expert mode doubling them
			target.AddBuff(ModContent.BuffType<Buffs.PowerfulCurseBuildup>(), 36000, false);
			target.AddBuff(39, 150, false); //cursed flames
			target.AddBuff(30, 1800, false); //bleeding
			target.AddBuff(33, 1800, false); //week
		}

		public static Texture2D texture;
		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
		{
			if(texture == null || texture.IsDisposed)
			{
				texture = ModContent.GetTexture("tsorcRevamp/Projectiles/Enemy/DarkExplosion");
			}
			SpriteEffects spriteEffects = SpriteEffects.None;
			if (projectile.spriteDirection == -1)
			{
				spriteEffects = SpriteEffects.FlipHorizontally;
			}

			//Get the premultiplied, properly transparent texture
			int frameHeight = Main.projectileTexture[projectile.type].Height / Main.projFrames[projectile.type];
			int startY = frameHeight * projectile.frame;
			Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
			Vector2 origin = sourceRectangle.Size() / 2f;
			//origin.X = (float)(projectile.spriteDirection == 1 ? sourceRectangle.Width - 20 : 20);
			Color drawColor = projectile.GetAlpha(lightColor);
			Main.spriteBatch.Draw(texture,
				projectile.Center - Main.screenPosition + new Vector2(0f, projectile.gfxOffY),
				sourceRectangle, drawColor, projectile.rotation, origin, projectile.scale, spriteEffects, 0f);

			return false;
		}
	}
}