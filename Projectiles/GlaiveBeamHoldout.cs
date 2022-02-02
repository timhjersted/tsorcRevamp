using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class GlaiveBeamHoldout : ModProjectile {

		public override string Texture => "tsorcRevamp/Projectiles/GlaiveBeamHoldout";

		// The vanilla Last Prism is an animated item with 5 frames of animation. We copy that here.
		private const int NumAnimationFrames = 11;


		// This value controls how many frames it takes for the Prism to reach "max charge". 60 frames = 1 second.
		public static float MaxCharge = 120f;


		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Glaive Beam");
			Main.projFrames[projectile.type] = NumAnimationFrames;

			// Signals to Terraria that this projectile requires a unique identifier beyond its index in the projectile array.
			// This prevents the issue with the vanilla Last Prism where the beams are invisible in multiplayer.
			ProjectileID.Sets.NeedsUUID[projectile.type] = true;
		}

		public override void SetDefaults()
		{
			// Use CloneDefaults to clone all basic projectile statistics from the vanilla Last Prism.
			projectile.CloneDefaults(ProjectileID.LastPrism);
			projectile.ranged = true;
		}

		int charge = 0;
		bool spawnedLaser = false;
		public override void AI()
		{
			Player player = Main.player[projectile.owner];
			Vector2 rrp = player.RotatedRelativePoint(player.MountedCenter, true);

			charge++;
			//If the charge is negative, that means we're in the "firing" state
			if (charge <= 0)
			{
				projectile.frame = 10;
				//Drain BotC players stamina
				if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
				{
					player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= 1.5f;
				}
			}
			//If not, move up the animation sheet as the weapon charges
			else
			{
				projectile.frame = (int)((charge / MaxCharge) * (NumAnimationFrames));
				//Stop the BotC player from using the Glaive Beam if they don't have full stamina
				if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent < player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax2)
                {
					player.channel = false;
                }
			}
			//If it's finished charging, there will be a delay before the next. Set it to negative for the duration, so it stays in the "firing" state.
			if (charge == MaxCharge) {
				charge = -GlaiveBeamLaser.FIRING_TIME;
			}
			

			// Update the Prism's position in the world and relevant variables of the player holding it.
			UpdatePlayerVisuals(player, rrp);

			// Update the Prism's behavior: project beams on frame 1, consume mana, and despawn if out of mana.
			if (projectile.owner == Main.myPlayer)
			{
				// Slightly re-aim the Prism every frame so that it gradually sweeps to point towards the mouse.
				UpdateAim(rrp, player.HeldItem.shootSpeed);

				// The Prism immediately stops functioning if the player is Cursed (player.noItems) or "Crowd Controlled", e.g. the Frozen debuff.
				// player.channel indicates whether the player is still holding down the mouse button to use the item.
				bool stillInUse = player.channel && !player.noItems && !player.CCed;

				// Spawn in the Prism's lasers on the first frame if the player is capable of using the item.
				if (stillInUse && !spawnedLaser)
				{
					FireBeams();
					spawnedLaser = true;
				}

				// If the Prism cannot continue to be used, then destroy it immediately.
				else if (!stillInUse)
				{
					projectile.Kill();
				}
			}

			// This ensures that the Prism never times out while in use.
			projectile.timeLeft = 2;
		}

        public override bool CanDamage()
        {
            return false;
        }

		//Moves the centerpoint of the beam so it's always aligned with the sprite
		//Was there a better way to do this? Probably. This works though.
		private Vector2 GetOrigin()
		{
			Vector2 origin = Main.player[projectile.owner].Center;
			origin.X -= 5 * Main.player[projectile.owner].direction;
			origin.Y -= 15;

			if (Main.player[projectile.owner].itemRotation < 0)
			{
				//If aiming in the upper right quadrant
				if (Main.player[projectile.owner].direction == 1)
				{
					origin.X -= 10 * (float)Math.Cos(Math.Abs(Main.player[projectile.owner].itemRotation));
					origin.Y -= -10 * (float)Math.Sin(Math.Abs(Main.player[projectile.owner].itemRotation));
				}
				//Bottom left
				else
				{
					origin.X -= 5 + -10 * (float)Math.Cos(Math.Abs(Main.player[projectile.owner].itemRotation));
					origin.Y += -10 * (float)Math.Sin(Math.Abs(Main.player[projectile.owner].itemRotation));
				}
			}
			else
			{
				//Bottom right
				if (Main.player[projectile.owner].direction == 1)
				{
					origin.X += 6 + -11 * (float)Math.Cos(Math.Abs(Main.player[projectile.owner].itemRotation));
					origin.Y += -10 * (float)Math.Sin(Math.Abs(Main.player[projectile.owner].itemRotation));
				}
				//Upper left
				else
				{
					origin.X += 10 * (float)Math.Cos(Math.Abs(Main.player[projectile.owner].itemRotation));
					origin.Y -= -10 * (float)Math.Sin(Math.Abs(Main.player[projectile.owner].itemRotation));
				}
			}
			return origin;
		}

		private void UpdatePlayerVisuals(Player player, Vector2 playerHandPos)
		{
			// Place the Prism directly into the player's hand at all times.
			Vector2 origin = GetOrigin();
			projectile.Center = new Vector2(origin.X + (-25 * player.direction), origin.Y - 15);
			// The beams emit from the tip of the Prism, not the side. As such, rotate the sprite by pi/2 (90 degrees).
			projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
			projectile.spriteDirection = projectile.direction;

			// The Prism is a holdout projectile, so change the player's variables to reflect that.
			// Constantly resetting player.itemTime and player.itemAnimation prevents the player from switching items or doing anything else.
			player.ChangeDir(projectile.direction);
			//player.heldProj = projectile.whoAmI;
			player.itemTime = 2;
			player.itemAnimation = 2;

			// If you do not multiply by projectile.direction, the player's hand will point the wrong direction while facing left.
			player.itemRotation = (projectile.velocity * projectile.direction).ToRotation();
		}


		private void UpdateAim(Vector2 source, float speed)
		{
			// Get the player's current aiming direction as a normalized vector.
			Vector2 aim = Vector2.Normalize(Main.MouseWorld - source);
			if (aim.HasNaNs())
			{
				aim = -Vector2.UnitY;
			}

			// Change a portion of the Prism's current velocity so that it points to the mouse. This gives smooth movement over time.
			aim = Vector2.Normalize(Vector2.Lerp(Vector2.Normalize(projectile.velocity), aim, 1));
			aim *= speed;

			if (aim != projectile.velocity)
			{
				projectile.netUpdate = true;
			}
			projectile.velocity = aim;
		}

		private void FireBeams()
		{
			// If for some reason the beam velocity can't be correctly normalized, set it to a default value.
			Vector2 beamVelocity = Vector2.Normalize(projectile.velocity);
			if (beamVelocity.HasNaNs())
			{
				beamVelocity = -Vector2.UnitY;
			}

			// This UUID will be the same between all players in multiplayer, ensuring that the beams are properly anchored on the Prism on everyone's screen.
			int uuid = Projectile.GetByUUID(projectile.owner, projectile.whoAmI);

			int damage = projectile.damage;
			float knockback = projectile.knockBack;

			Projectile.NewProjectile(projectile.Center, beamVelocity, ModContent.ProjectileType<GlaiveBeamLaser>(), damage, knockback, projectile.owner, 0, uuid);

			// After creating the beams, mark the Prism as having an important network event. This will make Terraria sync its data to other players ASAP.
			projectile.netUpdate = true;
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
		{
			return false;			
		}
	}
}
