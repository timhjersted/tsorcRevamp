using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
	class EnemyBlackFire : ModProjectile
	{
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
			Projectile.aiStyle = 1;
			Projectile.timeLeft = 360;
			Projectile.friendly = false;
			Projectile.hostile = true;
			Projectile.penetrate = 1;
			Projectile.light = 0.8f;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.tileCollide = true;
			Projectile.knockBack = 9;
		}
		public override void AI()
		{
			for(int i = 0; i < Main.maxPlayers; i++)
            {
				if(Main.player[i] != null && Main.player[i].active)
                {
					if (Vector2.Distance(Projectile.Center, Main.player[i].Center) < 300)
					{
						Vector2 vectorDiff = UsefulFunctions.GenerateTargetingVector(Projectile.Center, Main.player[i].Center, 1);
						double angleDiff = UsefulFunctions.CompareAngles(Projectile.velocity, vectorDiff);

						if (angleDiff > MathHelper.Pi / 2)
						{
							Projectile.Kill();
							Projectile.active = false;
						}
					}
				}
            }

			//Offset natural projectile gravity slightly to make it floatier (which means bigger arcs)
			Projectile.velocity.Y += 0.01f;

			

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
			Main.player[Main.myPlayer].AddBuff(ModContent.BuffType<Buffs.DarkInferno>(), 240, false);
		}

        public override void Kill(int timeLeft)
		{
			if (!Projectile.active)
			{
				return;
			}
			Projectile.timeLeft = 0;

			Terraria.Audio.SoundEngine.PlaySound(2, (int)Projectile.position.X, (int)Projectile.position.Y, 14);

			float len = 4f;
			int flam = ModContent.ProjectileType<EnemyBlackFirelet>();
			int damg = Projectile.damage;
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
				Projectile.NewProjectile(Projectile.position.X, Projectile.position.Y, velX, velY, flam, damg, 0, Projectile.owner);
			}

			// setup projectile for explosion
			Projectile.damage = Projectile.damage >> 1; // or whatever amount of damage you want it to do
														// not sure if this will make it variate the damage(calling Main.DamageVar())
			Projectile.penetrate = 2;
			Projectile.width = Projectile.width << 3;
			Projectile.height = Projectile.height << 3;
			Projectile.position.X -= Projectile.width / 2;
			Projectile.position.Y -= Projectile.height / 2;

			// do explosion
			Projectile.Damage();

			// create glowing red embers that fill the explosion's radius
			for (int i = 0; i < 300; i++)
			{
				Vector2 projPosition = Main.rand.NextVector2Circular(Projectile.width / 3, Projectile.height / 3) + Projectile.Center;
				Vector2 projVelocity = Main.rand.NextVector2CircularEdge(5, 5);
				Dust.NewDustPerfect(projPosition, 54, projVelocity, 160, default, 1.5f);
				Dust.NewDustPerfect(projPosition, 58, projVelocity, 160, default, 1.5f);
			}

			// terminate projectile
			Projectile.active = false;
		}
	}
}

