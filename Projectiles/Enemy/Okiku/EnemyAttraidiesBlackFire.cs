using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Okiku
{
	class EnemyAttraidiesBlackFire : ModProjectile
	{
		public override string Texture => "tsorcRevamp/Projectiles/Enemy/EnemyBlackFire";

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Black Fire");

		}
		public override void SetDefaults()
		{
			Projectile.width = 12;
			Projectile.height = 12;
			Projectile.scale = 1.5f;
			Projectile.alpha = 50;
			Projectile.aiStyle = -1;
			Projectile.timeLeft = 360;
			Projectile.friendly = false;
			Projectile.hostile = true;
			Projectile.penetrate = 1;
			Projectile.light = 0.8f;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.tileCollide = true;
			Projectile.damage = 1;
			Projectile.knockBack = 9;
		}
		public override void AI()
		{
			if(Main.player[(int)Projectile.ai[0]].position.Y > Projectile.position.Y)
            {
				Projectile.tileCollide = false;
            }
			else
            {
				Projectile.tileCollide = true;
            }


			// Determine projectile behavior
			// Apply half-gravity & clamp downward speed
			Projectile.velocity.Y = Projectile.velocity.Y > 16f ? 16f : Projectile.velocity.Y + 0.1f;

			if (Projectile.velocity.X < 0f)
			{    // Dampen left-facing horizontal velocity & clamp to minimum speed
				Projectile.velocity.X = Projectile.velocity.X > -1f ? -1f : Projectile.velocity.X + 0.01f;
			}
			else if (Projectile.velocity.X > 0f)
			{    // Dampen right-facing horizontal velocity & clamp to minimum speed
				Projectile.velocity.X = Projectile.velocity.X < 1f ? 1f : Projectile.velocity.X - 0.01f;
			}

			// Align projectile facing with velocity normal
			Projectile.rotation = (float)Math.Atan2(Projectile.velocity.Y, Projectile.velocity.X) - 2.355f;
			// Render fire particles [every frame]
			int particle = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 54, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 160, default(Color), 3f);
			Main.dust[particle].noGravity = true;
			Main.dust[particle].velocity *= 1.4f;
			int lol = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 58, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 160, default(Color), 3f);
			Main.dust[lol].noGravity = true;
			Main.dust[lol].velocity *= 1.4f;


			// Render smoke particles [every other frame]
			if (Projectile.timeLeft % 2 == 0)
			{
				int particle2 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 1, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f - 1f, 180, default(Color), 1f + (float)Main.rand.Next(2));
				Main.dust[particle2].noGravity = true;
				Main.dust[particle2].noLight = true;
				Main.dust[particle2].fadeIn = 3f;
			}			
		}

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
			target.AddBuff(ModContent.BuffType<Buffs.DarkInferno>(), 240, false);
		}

        public override bool PreKill(int timeLeft)
		{
			if (!Projectile.active)
			{
				return true;
			}
			Projectile.timeLeft = 0;
			//projectile.AI(false);

			Terraria.Audio.SoundEngine.PlaySound(SoundID.Item, (int)Projectile.position.X, (int)Projectile.position.Y, 14);

			float len = 4f;
			int flam = ModContent.ProjectileType<EnemyBlackFirelet>();
			Vector2 dir = new Vector2(1f, 0f);

			// determine how many flamelets to spew (5~8)
			int children = Main.rand.Next(5) + 3;

			// set the angle offset by the number of flamelets
			int offset = 180 / (children - 1);

			// rotate theta by a random angle between 0 and 90 degrees
			int preOffset = Main.rand.Next(90);
			dir = new Vector2((float)Math.Cos(preOffset) * dir.X - (float)Math.Sin(preOffset) * dir.Y, (float)Math.Sin(preOffset) * dir.X + (float)Math.Cos(preOffset) * dir.Y);

			// create the flaming shrapnel-like projectiles
			for (int i = 0; i < children; i++)
			{
				float velX = (float)Math.Cos(offset) * dir.X - (float)Math.Sin(offset) * dir.Y;
				float velY = (float)Math.Sin(offset) * dir.X + (float)Math.Cos(offset) * dir.Y;

				dir.X = velX;
				dir.Y = velY;

				float var = (float)(Main.rand.Next(10) / 10);

				velX *= len + var;
				velY *= len + var;

				//if(projectile.owner == Main.myPlayer) Projectile.NewProjectile(projectile.position.X + (float)(projectile.width-5), projectile.position.Y + (float)(projectile.height-5), 0, 0, flam, 72, 0, projectile.owner);
				Projectile.NewProjectile(Projectile.position.X, Projectile.position.Y, velX, velY, flam, Projectile.damage, 0, Projectile.owner);
			}


			// create glowing red embers that fill the explosion's radius
			for (int i = 0; i < 30; i++)
			{
				float velX = 2f - ((float)Main.rand.Next(20)) / 5f;
				float velY = 2f - ((float)Main.rand.Next(20)) / 5f;
				velX *= 4f;
				velY *= 4f;
				int p = Dust.NewDust(new Vector2(Projectile.position.X - (float)(Projectile.width >> 1), Projectile.position.Y - (float)(Projectile.height >> 1)), Projectile.width, Projectile.height, 54, velX, velY, 160, default(Color), 1.5f);
				int p2 = Dust.NewDust(new Vector2(Projectile.position.X - (float)(Projectile.width >> 1), Projectile.position.Y - (float)(Projectile.height >> 1)), Projectile.width, Projectile.height, 58, velX, velY, 160, default(Color), 1.5f);
			}

			// terminate projectile
			Projectile.active = false;
			return true;
		}
	}
}