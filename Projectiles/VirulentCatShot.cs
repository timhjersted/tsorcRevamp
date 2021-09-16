using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;

namespace tsorcRevamp.Projectiles
{
	class VirulentCatShot : ModProjectile
	{
		public override string Texture => "tsorcRevamp/Projectiles/ToxicCatShot";
		public override void SetStaticDefaults()
		{
			Main.projFrames[projectile.type] = 2;
		}
		public override void SetDefaults()
		{

			projectile.width = 10;
			projectile.height = 10;
			projectile.friendly = true;
			projectile.aiStyle = 0;
			projectile.ranged = true;
			projectile.tileCollide = true;
			projectile.timeLeft = 125;
			projectile.penetrate = 3;
			projectile.usesLocalNPCImmunity = true;
			projectile.localNPCHitCooldown = -1; 

			drawOffsetX = -2;
			drawOriginOffsetY = -2;
		}


		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
		{
			Texture2D texture = Main.projectileTexture[projectile.type];

			spriteBatch.Draw(texture, projectile.Center - Main.screenPosition, new Rectangle(0, projectile.frame * 16, 10, 16), Color.White, projectile.rotation, new Vector2(7, 8), projectile.scale, SpriteEffects.None, 0);

			return false;
		}

		public override void AI()
		{
			//Change these two variables to affect the rotation of your projectile
			float rotationsPerSecond = 3f;
			bool rotateClockwise = true;
			//The rotation is set here
			projectile.rotation += (rotateClockwise ? 1 : -1) * MathHelper.ToRadians(rotationsPerSecond * 30f);

			Lighting.AddLight(projectile.position, 0.2496f, 0.4584f, 0.130f);

			if (projectile.owner == Main.myPlayer && projectile.timeLeft <= 6)
			{
				projectile.alpha += 25;

				if (projectile.alpha > 255)
				{
					projectile.alpha = 225;
				}
			}

			{
				if (IsStickingToTarget) StickyAI();
				else NormalAI();
			}
		}
		private void NormalAI()
		{
			projectile.damage = 1;
		}

		private int virucattimer;
		private void StickyAI()
		{
			// These 2 could probably be moved to the ModifyNPCHit hook, but in vanilla they are present in the AI
			projectile.ignoreWater = true; // Make sure the projectile ignores water
			projectile.tileCollide = false; // Make sure the projectile doesn't collide with tiles anymore
			const int aiFactor = 8; // Change this factor to change the 'lifetime' of this sticking javelin //These are seconds. Keep debuff duration to same duration as is set here.
			projectile.localAI[0] += 1f;

			if (projectile.timeLeft > 2)
			{
				projectile.timeLeft = 100;
			}

			projectile.rotation = projectile.velocity.ToRotation();
			// Every 30 ticks, the javelin will perform a hit effect
			bool hitEffect = projectile.localAI[0] % 30f == 0f;
			int projTargetIndex = (int)TargetWhoAmI;

			if (projectile.localAI[0] >= 60 * aiFactor || projTargetIndex < 0 || projTargetIndex >= 200)
			{ // If the index is past its limits, kill it
				projectile.Kill();
			}

			else if (Main.npc[projTargetIndex].active && !Main.npc[projTargetIndex].dontTakeDamage)
			{ // If the target is active and can take damage
			  // Set the projectile's position relative to the target's center
				projectile.Center = Main.npc[projTargetIndex].Center - projectile.velocity * 2.5f;
				//projectile.rotation = Main.npc[projTargetIndex].Center;
				projectile.gfxOffY = Main.npc[projTargetIndex].gfxOffY;
				if (hitEffect)
				{ // Perform a hit effect here
					Main.npc[projTargetIndex].HitEffect(0, 1.0);
				}
			}

			else
			{ // Otherwise, kill the projectile
				projectile.Kill();
			}

			//ANIMATION
			if (virucattimer > 40)
			{
				virucattimer = 0;
			}

			if (++projectile.frameCounter >= 20) //ticks spent on each frame
			{
				projectile.frameCounter = 0;

				if (++projectile.frame >= 2)
				{
					projectile.frame = 0;
				}
			}
		}

		public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
		{
			Main.PlaySound(SoundID.NPCDeath9.WithVolume(.8f), projectile.Center);
			for (int d = 0; d < 20; d++)
			{
				int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 75, projectile.velocity.X * 1f, projectile.velocity.Y * 1f, 30, default(Color), 1f);
				Main.dust[dust].velocity.X = +Main.rand.Next(-50, 51) * 0.05f;
				Main.dust[dust].velocity.Y = +Main.rand.Next(-50, 51) * 0.05f;
				Main.dust[dust].noGravity = true;
			}

		}
		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			Main.PlaySound(SoundID.NPCDeath9.WithVolume(.8f), projectile.Center);
			for (int d = 0; d < 20; d++)
			{
				int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 75, projectile.velocity.X * 1f, projectile.velocity.Y * 1f, 30, default(Color), 1f);
				Main.dust[dust].velocity.X = +Main.rand.Next(-50, 51) * 0.05f;
				Main.dust[dust].velocity.Y = +Main.rand.Next(-50, 51) * 0.05f;
				Main.dust[dust].noGravity = true;

			}
			return true;

		}

		public override void Kill(int timeLeft)
		{
			Main.PlaySound(SoundID.NPCDeath9.WithVolume(.4f), projectile.Center);
			for (int d = 0; d < 20; d++)
			{
				int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 75, projectile.velocity.X * 1.2f, projectile.velocity.Y * 1.2f, 30, default(Color), 1f);
				Main.dust[dust].velocity.X = +Main.rand.Next(-50, 51) * 0.05f;
				Main.dust[dust].velocity.Y = +Main.rand.Next(-50, 51) * 0.05f;
				Main.dust[dust].noGravity = true;

			}
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			// Inflate some target hitboxes if they are beyond 8,8 size
			if (targetHitbox.Width > 8 && targetHitbox.Height > 8)
			{
				targetHitbox.Inflate(-targetHitbox.Width / 8, -targetHitbox.Height / 8);
			}
			// Return if the hitboxes intersects, which means the javelin collides or not
			return projHitbox.Intersects(targetHitbox);
		}

		/*
		 * The following showcases recommended practice to work with the ai field
		 * You make a property that uses the ai as backing field
		 * This allows you to contextualize ai better in the code
		 */

		// Are we sticking to a target?
		public bool IsStickingToTarget
		{
			get => projectile.ai[0] == 1f;
			set => projectile.ai[0] = value ? 1f : 0f;
		}

		// Index of the current target
		public int TargetWhoAmI
		{
			get => (int)projectile.ai[1];
			set => projectile.ai[1] = value;
		}

		private const int MAX_STICKY_JAVELINS = 10; // This is the max. amount of javelins being able to attach
		private readonly Point[] _stickingJavelins = new Point[MAX_STICKY_JAVELINS]; // The point array holding for sticking javelins

		public override void ModifyHitNPC(NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
		{
			IsStickingToTarget = true; // we are sticking to a target
			TargetWhoAmI = target.whoAmI; // Set the target whoAmI
			projectile.velocity =
				(target.Center - projectile.Center) *
				0.75f; // Change velocity based on delta center of targets (difference between entity centers)
			projectile.netUpdate = true; // netUpdate this javelin
			target.AddBuff(ModContent.BuffType<Buffs.ViruCatDrain>(), 480); // Adds the ExampleJavelin debuff for a very small DoT

			projectile.damage = 0; // Makes sure the sticking javelins do not deal damage anymore


			// It is recommended to split your code into separate methods to keep code clean and clear
			UpdateStickyJavelins(target);


		}
		int currentJavelinIndex = 0; // The javelin index
		/*
		 * The following code handles the javelin sticking to the enemy hit.
		 */
		private void UpdateStickyJavelins(NPC target)
		{
			// int currentJavelinIndex = 0; // The javelin index

			for (int i = 0; i < Main.maxProjectiles; i++) // Loop all projectiles
			{
				Projectile currentProjectile = Main.projectile[i];
				if (i != projectile.whoAmI // Make sure the looped projectile is not the current javelin
					&& currentProjectile.active // Make sure the projectile is active
					&& currentProjectile.owner == Main.myPlayer // Make sure the projectile's owner is the client's player
					&& currentProjectile.type == projectile.type // Make sure the projectile is of the same type as this javelin
					&& currentProjectile.modProjectile is VirulentCatShot javelinProjectile // Use a pattern match cast so we can access the projectile like an ExampleJavelinProjectile
					&& javelinProjectile.IsStickingToTarget // the previous pattern match allows us to use our properties
					&& javelinProjectile.TargetWhoAmI == target.whoAmI)
				{

					_stickingJavelins[currentJavelinIndex++] = new Point(i, currentProjectile.timeLeft); // Add the current projectile's index and timeleft to the point array
					if (currentJavelinIndex >= _stickingJavelins.Length)  // If the javelin's index is bigger than or equal to the point array's length, break
						break;
				}
			}

			// Remove the oldest sticky javelin if we exceeded the maximum
			if (currentJavelinIndex >= MAX_STICKY_JAVELINS)
			{
				int oldJavelinIndex = 0;
				// Loop our point array
				for (int i = 1; i < MAX_STICKY_JAVELINS; i++)
				{
					// Remove the already existing javelin if it's timeLeft value (which is the Y value in our point array) is smaller than the new javelin's timeLeft
					if (_stickingJavelins[i].Y < _stickingJavelins[oldJavelinIndex].Y)
					{
						oldJavelinIndex = i; // Remember the index of the removed javelin
					}
				}
				// Remember that the X value in our point array was equal to the index of that javelin, so it's used here to kill it.
				Main.projectile[_stickingJavelins[oldJavelinIndex].X].Kill();
			}
		}
	}
}
