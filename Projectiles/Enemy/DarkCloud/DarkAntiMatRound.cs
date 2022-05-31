using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.DarkCloud
{
    class DarkAntiMatRound : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = false;
			Projectile.hostile = true;
            Projectile.aiStyle = 0;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = false;
			Projectile.penetrate = 5;
			//In theory this means the projectile can only ever hit a NPC once.
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
        }
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Anti-Material Round");
		}

		bool reposition = true;
		float sinwaveCounter = -1.4f;
		public override void AI()
		{
			if (reposition)
			{
				Projectile.rotation = (float)Math.Atan2((double)Projectile.velocity.Y, (double)Projectile.velocity.X) + 1.57f;

				Projectile.velocity.X *= 0.45f;
				Projectile.velocity.Y *= 0.45f;

				reposition = false;
				Projectile.alpha = 255;
			}
			Projectile.alpha -= 51;

			for (int i = 0; i < 10; i++)
			{			

				//dust fx
				if (i % 2 == 0)
				{
					if (sinwaveCounter > 0)
					{//tracer
						Vector2 DustPos = Projectile.position;

						int DustIndex = Dust.NewDust(DustPos, 0, 0, 6, 0, 0, 100, default(Color), 1.7f);
						Main.dust[DustIndex].noGravity = true;
						Main.dust[DustIndex].velocity = new Vector2(0, 0);
					}
					else
					{//fire effect
						Vector2 DustPos = Projectile.position;
						int DustWidth = Projectile.width;
						int DustHeight = Projectile.height;

						Dust.NewDust(DustPos, DustWidth, DustHeight, 6, 0, 0, 100, default(Color), 1.1f);
						int DustIndex = Dust.NewDust(DustPos, DustWidth, DustHeight, 31, 0, 0, 100, default(Color), 3f);
						Main.dust[DustIndex].noGravity = true;
					}
				}
				if (sinwaveCounter > 0)
				{
					//sine wave top
					Vector2 DustTopPos = Projectile.position +
						new Vector2((float)(15 * Math.Cos(sinwaveCounter) * Math.Cos(Projectile.rotation)),
						(float)(15 * Math.Sin(sinwaveCounter) * Math.Sin(Projectile.rotation)));

					int sineTop = Dust.NewDust(DustTopPos, 0, 0, 60, 0, 0, 100, default(Color), 1f);
					Main.dust[sineTop].noGravity = true;
					Main.dust[sineTop].velocity = new Vector2(0, 0);
					//sine wave bottom
					Vector2 DustBotPos = Projectile.position +
						new Vector2((float)(-15 * Math.Cos(sinwaveCounter) * Math.Cos(Projectile.rotation)),
						(float)(-15 * Math.Sin(sinwaveCounter) * Math.Sin(Projectile.rotation)));

					int sineBot = Dust.NewDust(DustBotPos, 0, 0, 60, 0, 0, 100, default(Color), 1f);
					Main.dust[sineBot].noGravity = true;
					Main.dust[sineBot].velocity = new Vector2(0, 0);
				}
				sinwaveCounter += 0.2f;				

				Projectile.position += Projectile.velocity;
			}

			Lighting.AddLight((int)((Projectile.position.X + (float)(Projectile.width / 2)) / 16f), (int)((Projectile.position.Y + (float)(Projectile.height / 2)) / 16f), 0.9f, 0.2f, 0.1f);
		}
		public override bool PreDraw(ref Color lightColor)
		{
			SpriteEffects spriteEffects = SpriteEffects.None;
			if (Projectile.spriteDirection == -1)
			{
				spriteEffects = SpriteEffects.FlipHorizontally;
			}
			//Get the premultiplied, properly transparent texture
			Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.AntiMaterialRound];
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

        public override void ModifyHitNPC(NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
		{
			Terraria.Audio.SoundEngine.PlaySound(4, (int)Projectile.position.X, (int)Projectile.position.Y, 43);
			damage = target.defense + Projectile.damage;
			if (Projectile.penetrate <= 0)
			{
				Projectile.Kill();
			}
		}

        public override bool PreKill(int timeLeft)
        {
			for (int num36 = 0; num36 < 10; num36++)
			{
				Dust.NewDustPerfect(Projectile.position, 127, Projectile.velocity + new Vector2(Main.rand.Next(-5, 5), Main.rand.Next(-5, 5)), 100, new Color(), 1f).noGravity = true;
			}
			for (int num36 = 0; num36 < 7; num36++)
			{
				Dust.NewDustPerfect(Projectile.position, 130, Projectile.velocity + new Vector2(Main.rand.Next(-5, 5), Main.rand.Next(-5, 5)), 100, new Color(), 2f).noGravity = true;
			}
			return base.PreKill(timeLeft);
        }
	}
}
