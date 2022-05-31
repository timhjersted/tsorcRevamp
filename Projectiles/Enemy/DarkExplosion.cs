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
			Main.projFrames[Projectile.type] = 6;
			Projectile.aiStyle = 0;
			Projectile.hostile = true;
			Projectile.alpha = 215;
			Projectile.height = 76;
			Projectile.scale = 2;
			Projectile.tileCollide = false;
			Projectile.width = 76;
			Projectile.timeLeft = 1500;
		}
		public override void AI()
		{
			//if (projectile.frame < 5)
			Projectile.frameCounter++;

			if (Projectile.frameCounter == 30)
			{
				Projectile.frame = 1;
				Projectile.alpha = 200;
			}

			if (Projectile.frameCounter == 60)
			{
				Projectile.frame = 2;
				Projectile.alpha = 175;
			}

			if (Projectile.frameCounter == 90)
			{
				Projectile.frame = 3;
				Projectile.alpha = 150;
			}
			if (Projectile.frameCounter == 120)
			{
				Projectile.frame = 4;
				Projectile.alpha = 125;
			}
			if (Projectile.frameCounter == 180)
			{
				Projectile.frame = 5;
				Projectile.alpha = 50;
				Projectile.hostile = true;
				for (int num36 = 0; num36 < 20; num36++)
				{
					int dustDeath = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 55, Main.rand.Next(-6, 6), Main.rand.Next(-6, 6), 200, Color.White, 2f);
					Main.dust[dustDeath].noGravity = true;
				}
			}
			if (Projectile.frameCounter >= 182)
			{
				Projectile.active = false;
			}

			Projectile.rotation += 0.1f;

			if (Main.rand.Next(2) == 0)
			{
				int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, 6, 0, 0, Projectile.alpha, Color.White, 2.0f);
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
		public override bool PreDraw(ref Color lightColor)
		{
			if(texture == null || texture.IsDisposed)
			{
				texture = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/DarkExplosion");
			}
			SpriteEffects spriteEffects = SpriteEffects.None;
			if (Projectile.spriteDirection == -1)
			{
				spriteEffects = SpriteEffects.FlipHorizontally;
			}

			//Get the premultiplied, properly transparent texture
			int frameHeight = Main.projectileTexture[Projectile.type].Height / Main.projFrames[Projectile.type];
			int startY = frameHeight * Projectile.frame;
			Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
			Vector2 origin = sourceRectangle.Size() / 2f;
			//origin.X = (float)(projectile.spriteDirection == 1 ? sourceRectangle.Width - 20 : 20);
			Color drawColor = Projectile.GetAlpha(lightColor);
			Main.Main.EntitySpriteDraw(texture,
				Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
				sourceRectangle, drawColor, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0f);

			return false;
		}
	}
}