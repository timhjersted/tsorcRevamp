using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Projectiles {
    class BlackFire : ModProjectile {
        public override void SetDefaults() {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.scale = 1.5f;
            Projectile.alpha = 50;
            Projectile.timeLeft = 360;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = true;
            Projectile.damage = 45;
            Projectile.knockBack = 9;
        }

        Vector2 destination = new Vector2(0f, 0f);
        int channeling = 0;

		void Init() {
			if (channeling == -1 || !Main.projectile[channeling].active) {
				channeling = Projectile.whoAmI;
			}

			float vecX = (float)Main.mouseX + Main.screenPosition.X;
			float vecY = (float)Main.mouseY + Main.screenPosition.Y;

			destination = new Vector2((float)vecX, (float)vecY);

			Projectile.damage = Projectile.damage >> 1;
		}
		public override void AI() {

			if (!Main.player[Projectile.owner].channel) {
				channeling = -1;
			}
			// Get the length of last frame's velocity
			float lastLength = (float)Math.Sqrt((Projectile.velocity.X * Projectile.velocity.X + Projectile.velocity.Y * Projectile.velocity.Y));

			// Determine projectile behavior
			if (channeling == Projectile.whoAmI && Projectile.timeLeft > 120 && Main.player[Projectile.owner].channel) {    // Projectile is being channeled, so projectile flies towards its destination. if it reaches the destination, it explodes.

				// Set acceleration value
				float accel = 8f;

				// Store velocity locally
				Vector2 vel = new Vector2(destination.X - (Projectile.position.X + (float)Projectile.width * 0.5f), destination.Y - (Projectile.position.Y + (float)Projectile.height * 0.5f));

				// Get direction vector's length
				float len = (float)Math.Sqrt((vel.X * vel.X + vel.Y * vel.Y));

				// Normalize velocity vector
				vel.X *= 1f / len;
				vel.Y *= 1f / len;

				// Check distance between projectile and destination to see if projectile has arrived at destination
				if (len <= 12f) {
					Projectile.Kill();
					return;
				}
				else {
					// Calculate acceleration
					len = accel / len + lastLength;

					// Apply acceleration
					vel.X *= len;
					vel.Y *= len;

					// Redundant magnitude check (not sure if actually needed)
					if (vel.X == 0f && vel.Y == 0f) {
						Projectile.Kill();
						return;
					}
					else {  // Apply new velocity to projectile & clamp the values
						vel.X = vel.X > 32f ? 32f : (vel.X < -32f ? -32f : vel.X);
						vel.Y = vel.Y > 32f ? 32f : (vel.Y < -32f ? -32f : vel.Y);

						// Apply half-gravity
						vel.Y += 0.1f;

						Projectile.velocity.X = vel.X;
						Projectile.velocity.Y = vel.Y;

						Projectile.rotation = (float)Math.Atan2(Projectile.velocity.Y, Projectile.velocity.X) - 2.355f;
					}
				}
			}
			else {  // Projectile is not being channeled anymore or has only two seconds remaining on its timeLeft, so behave similar to projectile ai style 8

				// Apply half-gravity & clamp downward speed
				Projectile.velocity.Y = Projectile.velocity.Y > 16f ? 16f : Projectile.velocity.Y + 0.1f;

				if (Projectile.velocity.X < 0f) {   // Dampen left-facing horizontal velocity & clamp to minimum speed
					Projectile.velocity.X = Projectile.velocity.X > -1f ? -1f : Projectile.velocity.X + 0.01f;
				}
				else if (Projectile.velocity.X > 0f) {  // Dampen right-facing horizontal velocity & clamp to minimum speed
					Projectile.velocity.X = Projectile.velocity.X < 1f ? 1f : Projectile.velocity.X - 0.01f;
				}

				// Align projectile facing with velocity normal
				Projectile.rotation = (float)Math.Atan2(Projectile.velocity.Y, Projectile.velocity.X) - 2.355f;
			}

			// Render fire particles [every frame]
			int particle = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 54, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 160, default, 3f);
			Main.dust[particle].noGravity = true;
			Main.dust[particle].velocity *= 1.4f;
			int lol = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 58, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 160, default, 3f);
			Main.dust[lol].noGravity = true;
			Main.dust[lol].velocity *= 1.4f;


			// Render smoke particles [every other frame]
			if (Projectile.timeLeft % 2 == 0) {
				int particle2 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 1, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f - 1f, 180, default, 1f + (float)Main.rand.Next(2));
				Main.dust[particle2].noGravity = true;
				Main.dust[particle2].noLight = true;
				Main.dust[particle2].fadeIn = 3f;
			}
		}

		public override void Kill(int timeLeft) {
			if (!Projectile.active) {
				return;
			}
			Projectile.timeLeft = 0;

			Terraria.Audio.SoundEngine.PlaySound(SoundID.Item, (int)Projectile.position.X, (int)Projectile.position.Y, 14);

			float len = 4f;
			int flam = ModContent.ProjectileType<BlackFirelet>();
			int damg = Projectile.damage /2;
			Vector2 dir = new Vector2(1f, 0f);

			// determine how many flamelets to spew (5~8)
			int children = Main.rand.Next(5) + 3;

			// set the angle offset by the number of flamelets
			int offset = 180 / (children - 1);

			// rotate theta by a random angle between 0 and 90 degrees
			int preOffset = Main.rand.Next(90);
			dir = new Vector2((float)Math.Cos(preOffset) * dir.X - (float)Math.Sin(preOffset) * dir.Y, (float)Math.Sin(preOffset) * dir.X + (float)Math.Cos(preOffset) * dir.Y);

			// create the flaming shrapnel-like projectiles
			for (int i = 0; i < children; i++) {
				float velX = (float)Math.Cos(offset) * dir.X - (float)Math.Sin(offset) * dir.Y;
				float velY = (float)Math.Sin(offset) * dir.X + (float)Math.Cos(offset) * dir.Y;

				dir.X = velX;
				dir.Y = velY;

				float var = (float)(Main.rand.Next(10) / 10);

				velX *= len + var;
				velY *= len + var;

				Projectile.NewProjectile(Projectile.position.X, Projectile.position.Y, velX, velY, flam, damg, 0, Projectile.owner);
			}

			// setup projectile for explosion
			Projectile.damage = Projectile.damage * 2;
			Projectile.penetrate = 20;
			Projectile.width = Projectile.width << 3;
			Projectile.height = Projectile.height << 3;

			Projectile.Damage();

			// create glowing red embers that fill the explosion's radius
			for (int i = 0; i < 30; i++) {
				float velX = 2f - ((float)Main.rand.Next(20)) / 5f;
				float velY = 2f - ((float)Main.rand.Next(20)) / 5f;
				velX *= 4f;
				velY *= 4f;
				Dust.NewDust(new Vector2(Projectile.position.X - (float)(Projectile.width / 2), Projectile.position.Y - (float)(Projectile.height / 2)), Projectile.width, Projectile.height, 54, velX, velY, 160, default, 1.5f);
				Dust.NewDust(new Vector2(Projectile.position.X - (float)(Projectile.width / 2), Projectile.position.Y - (float)(Projectile.height / 2)), Projectile.width, Projectile.height, 58, velX, velY, 160, default, 1.5f);
			}

			// terminate projectile
			Projectile.active = false;
		}

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit) {
            if (Main.rand.Next(5) == 0) {
				target.AddBuff(ModContent.BuffType<Buffs.DarkInferno>(), 240);
            }
        }

        public override void OnHitPvp(Player target, int damage, bool crit) {
			if (Main.rand.Next(5) == 0) {
				target.AddBuff(ModContent.BuffType<Buffs.DarkInferno>(), 240);
			}
		}
    }
}
