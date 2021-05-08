using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Projectiles {
    class BlackFire : ModProjectile {
        public override void SetDefaults() {
            projectile.width = 12;
            projectile.height = 12;
            projectile.scale = 1.5f;
            projectile.alpha = 50;
            projectile.timeLeft = 360;
            projectile.friendly = true;
            projectile.penetrate = 1;
            projectile.magic = true;
            projectile.tileCollide = true;
            projectile.damage = 45;
            projectile.knockBack = 9;
        }

        Vector2 destination = new Vector2(0f, 0f);
        int channeling = 0;

		void Init() {
			if (channeling == -1 || !Main.projectile[channeling].active) {
				channeling = projectile.whoAmI;
			}

			float vecX = (float)Main.mouseX + Main.screenPosition.X;
			float vecY = (float)Main.mouseY + Main.screenPosition.Y;

			destination = new Vector2((float)vecX, (float)vecY);

			projectile.damage = projectile.damage >> 1;
		}
		public override void AI() {

			if (!Main.player[projectile.owner].channel) {
				channeling = -1;
			}
			// Get the length of last frame's velocity
			float lastLength = (float)Math.Sqrt((projectile.velocity.X * projectile.velocity.X + projectile.velocity.Y * projectile.velocity.Y));

			// Determine projectile behavior
			if (channeling == projectile.whoAmI && projectile.timeLeft > 120 && Main.player[projectile.owner].channel) {    // Projectile is being channeled, so projectile flies towards its destination. if it reaches the destination, it explodes.

				// Set acceleration value
				float accel = 8f;

				// Store velocity locally
				Vector2 vel = new Vector2(destination.X - (projectile.position.X + (float)projectile.width * 0.5f), destination.Y - (projectile.position.Y + (float)projectile.height * 0.5f));

				// Get direction vector's length
				float len = (float)Math.Sqrt((vel.X * vel.X + vel.Y * vel.Y));

				// Normalize velocity vector
				vel.X *= 1f / len;
				vel.Y *= 1f / len;

				// Check distance between projectile and destination to see if projectile has arrived at destination
				if (len <= 12f) {
					projectile.Kill();
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
						projectile.Kill();
						return;
					}
					else {  // Apply new velocity to projectile & clamp the values
						vel.X = vel.X > 32f ? 32f : (vel.X < -32f ? -32f : vel.X);
						vel.Y = vel.Y > 32f ? 32f : (vel.Y < -32f ? -32f : vel.Y);

						// Apply half-gravity
						vel.Y += 0.1f;

						projectile.velocity.X = vel.X;
						projectile.velocity.Y = vel.Y;

						projectile.rotation = (float)Math.Atan2(projectile.velocity.Y, projectile.velocity.X) - 2.355f;
					}
				}
			}
			else {  // Projectile is not being channeled anymore or has only two seconds remaining on its timeLeft, so behave similar to projectile ai style 8

				// Apply half-gravity & clamp downward speed
				projectile.velocity.Y = projectile.velocity.Y > 16f ? 16f : projectile.velocity.Y + 0.1f;

				if (projectile.velocity.X < 0f) {   // Dampen left-facing horizontal velocity & clamp to minimum speed
					projectile.velocity.X = projectile.velocity.X > -1f ? -1f : projectile.velocity.X + 0.01f;
				}
				else if (projectile.velocity.X > 0f) {  // Dampen right-facing horizontal velocity & clamp to minimum speed
					projectile.velocity.X = projectile.velocity.X < 1f ? 1f : projectile.velocity.X - 0.01f;
				}

				// Align projectile facing with velocity normal
				projectile.rotation = (float)Math.Atan2(projectile.velocity.Y, projectile.velocity.X) - 2.355f;
			}

			// Render fire particles [every frame]
			int particle = Dust.NewDust(projectile.position, projectile.width, projectile.height, 54, projectile.velocity.X * 0.2f, projectile.velocity.Y * 0.2f, 160, default, 3f);
			Main.dust[particle].noGravity = true;
			Main.dust[particle].velocity *= 1.4f;
			int lol = Dust.NewDust(projectile.position, projectile.width, projectile.height, 58, projectile.velocity.X * 0.2f, projectile.velocity.Y * 0.2f, 160, default, 3f);
			Main.dust[lol].noGravity = true;
			Main.dust[lol].velocity *= 1.4f;


			// Render smoke particles [every other frame]
			if (projectile.timeLeft % 2 == 0) {
				int particle2 = Dust.NewDust(projectile.position, projectile.width, projectile.height, 1, projectile.velocity.X * 0.2f, projectile.velocity.Y * 0.2f - 1f, 180, default, 1f + (float)Main.rand.Next(2));
				Main.dust[particle2].noGravity = true;
				Main.dust[particle2].noLight = true;
				Main.dust[particle2].fadeIn = 3f;
			}
		}

		public override void Kill(int timeLeft) {
			if (!projectile.active) {
				return;
			}
			projectile.timeLeft = 0;

			Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 14);

			float len = 4f;
			int flam = ModContent.ProjectileType<BlackFirelet>();
			int damg = projectile.damage /2;
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

				Projectile.NewProjectile(projectile.position.X, projectile.position.Y, velX, velY, flam, damg, 0, projectile.owner);
			}

			// setup projectile for explosion
			projectile.damage = projectile.damage << 2;
			projectile.penetrate = 20;
			projectile.width = projectile.width << 3;
			projectile.height = projectile.height << 3;

			projectile.Damage();

			// create glowing red embers that fill the explosion's radius
			for (int i = 0; i < 30; i++) {
				float velX = 2f - ((float)Main.rand.Next(20)) / 5f;
				float velY = 2f - ((float)Main.rand.Next(20)) / 5f;
				velX *= 4f;
				velY *= 4f;
				Dust.NewDust(new Vector2(projectile.position.X - (float)(projectile.width / 2), projectile.position.Y - (float)(projectile.height / 2)), projectile.width, projectile.height, 54, velX, velY, 160, default, 1.5f);
				Dust.NewDust(new Vector2(projectile.position.X - (float)(projectile.width / 2), projectile.position.Y - (float)(projectile.height / 2)), projectile.width, projectile.height, 58, velX, velY, 160, default, 1.5f);
			}

			// terminate projectile
			projectile.active = false;
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
