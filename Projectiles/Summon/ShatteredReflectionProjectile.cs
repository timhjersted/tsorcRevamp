using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Summon
{
	// This minion shows a few mandatory things that make it behave properly.
	// Its attack pattern is simple: If an enemy is in range of 43 tiles, it will fly to it and deal contact damage
	// If the player targets a certain NPC with right-click, it will fly through tiles to it
	// If it isn't attacking, it will float near the player with minimal movement
	public class ShatteredReflectionProjectile : ModProjectile
	{
		public override void SetStaticDefaults()
		{

			// This is necessary for right-click targeting
			ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;

			Main.projPet[Projectile.type] = true; // Denotes that this projectile is a pet or minion

			ProjectileID.Sets.MinionSacrificable[Projectile.type] = true; // This is needed so your minion can properly spawn when summoned and replaced when other minions are summoned
			ProjectileID.Sets.CultistIsResistantTo[Projectile.type] = true; // Make the cultist resistant to this projectile, as it's resistant to all homing projectiles.
		}

		public sealed override void SetDefaults()
		{
			Main.projFrames[Projectile.type] = 2;
			Projectile.width = 40;
			Projectile.height = 50;
			Projectile.tileCollide = false; // Makes the minion go through tiles freely

			// These below are needed for a minion weapon
			Projectile.friendly = true; // Only controls if it deals damage to enemies on contact (more on that later)
			Projectile.minion = true; // Declares this as a minion (has many effects)
			Projectile.DamageType = DamageClass.Summon; // Declares the damage type (needed for it to deal damage)
			Projectile.minionSlots = 2f; // Amount of slots this minion occupies from the total minion slots available to the player (more on that later)
			Projectile.penetrate = -1; // Needed so the minion doesn't despawn on collision with enemies or tiles

			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 15;
		}

		// Here you can decide if your minion breaks things like grass or pots
		public override bool? CanCutTiles()
		{
			return false;
		}

		// This is mandatory if your minion deals contact damage (further related stuff in AI() in the Movement region)
		public override bool MinionContactDamage()
		{
			return true;
		}

		bool indexSet = false;
		List<float> foundIndicies = new List<float>();
		// The AI of this minion is split into multiple methods to avoid bloat. This method just passes values between calls actual parts of the AI.

		int timer = 0;
		public override void AI()
		{
			timer++;
			Player owner = Main.player[Projectile.owner];

			if (!indexSet)
			{
				for (int i = 0; i < Main.maxProjectiles; i++)
				{
					if (Main.projectile[i].active && Main.projectile[i].type == Projectile.type && Main.projectile[i].owner == Projectile.owner && Main.projectile[i].whoAmI != Projectile.whoAmI)
					{
						foundIndicies.Add(Main.projectile[i].ai[0]);
					}
				}

				for (int i = 0; i < 6; i++)
				{
					if (foundIndicies.Contains(i))
					{
						continue;
					}
					else
					{
						Projectile.ai[0] = i;
						break;
					}
				}
				indexSet = true;
			}

			CheckActive(owner);

			float rotationTime = 820;
			float percentage = MathHelper.TwoPi * ((Main.GameUpdateCount % rotationTime) / rotationTime);

			float radius = 300;
			float offset = 0;
			if (timer < 60)
			{
				radius *= timer / 60f;
				offset = 1 - timer / 60f;
			}
			if (Projectile.timeLeft < 60)
			{
				radius *= (Projectile.timeLeft) / 60f;
				offset = 1 - (Projectile.timeLeft) / 60f;
			}
			int ownedCount = owner.ownedProjectileCounts[ModContent.ProjectileType<ShatteredReflectionProjectile>()];
			Vector2 target = owner.Center + new Vector2(radius, 0).RotatedBy(offset + percentage + MathHelper.TwoPi * Projectile.ai[0] / ownedCount);
			UsefulFunctions.SmoothHoming(Projectile, target, 0.5f, 20);

			//Just skip the smoothing if it's in the despawn animation
			if(Projectile.timeLeft < 60)
            {
				Projectile.Center = target;
            }
			

			if (owner.whoAmI == Main.myPlayer && Main.GameUpdateCount % 60 == Projectile.ai[0] * 60f / ownedCount)
            {
				int? closest = UsefulFunctions.GetClosestEnemyNPC(Projectile.Center);
                if (closest.HasValue && (Main.npc[closest.Value].type != NPCID.TargetDummy || Main.npc[closest.Value].Distance(Projectile.Center) < 2000))
                {
					Vector2 velocity = UsefulFunctions.GenerateTargetingVector(Projectile.Center, Main.npc[closest.Value].Center, 3);
					Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity, ModContent.ProjectileType<Projectiles.BlackFire>(), Projectile.damage, 0, Main.myPlayer, 1);
				}
            }
		}

		private void Attack()
        {
			if (target != null && target.active && target.Distance(Projectile.Center) < 1000)
			{
				Vector2 projVel = UsefulFunctions.GenerateTargetingVector(Projectile.Center, target.Center, 1);
				if (Main.GameUpdateCount % 240 == 0) {
					Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, projVel, ModContent.ProjectileType<Projectiles.Summon.FriendlyRedLaser>(), Projectile.damage * 4, 0, Main.myPlayer, target.whoAmI, Projectile.whoAmI);
				}

				if (Main.GameUpdateCount % 240 >= 180 && Main.GameUpdateCount % 240 <= 220 && Main.GameUpdateCount % 3 == 0)
				{
					Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, target.velocity + projVel * 10, ModContent.ProjectileType<Projectiles.Summon.FriendlyDragonsBreath>(), Projectile.damage / 4, 0, Main.myPlayer, target.whoAmI, Projectile.whoAmI);
				}

				if (Main.GameUpdateCount % 60 < 15 && Main.GameUpdateCount % 4 == 0)
                {
					Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, target.velocity + projVel * 17, ModContent.ProjectileType<Projectiles.Summon.FriendlyTetsujinLaser>(), Projectile.damage, 0, Main.myPlayer, target.whoAmI, Projectile.whoAmI);					
				}
			}
		}

		// This is the "active check", makes sure the minion is alive while the player is alive, and despawns if not
		private bool CheckActive(Player owner)
		{
			if (owner.dead || !owner.active)
			{
				owner.ClearBuff(ModContent.BuffType<Buffs.Summon.ShatteredReflectionBuff>());

				return false;
			}

			if (owner.HasBuff(ModContent.BuffType<Buffs.Summon.ShatteredReflectionBuff>()))
			{
				Projectile.timeLeft = 60;
			}

			return true;
		}

		private void GeneralBehavior(Player owner, out Vector2 vectorToIdlePosition, out float distanceToIdlePosition)
		{
			Vector2 idlePosition = owner.Center;
			idlePosition.Y -= 48f; // Go up 48 coordinates (three tiles from the center of the player)

			// If your minion doesn't aimlessly move around when it's idle, you need to "put" it into the line of other summoned minions
			// The index is projectile.minionPos
			float minionPositionOffsetX = (10 + Projectile.minionPos * 40) * -owner.direction;
			idlePosition.X += minionPositionOffsetX; // Go behind the player

			// All of this code below this line is adapted from Spazmamini code (ID 388, aiStyle 66)

			// Teleport to player if distance is too big
			vectorToIdlePosition = idlePosition - Projectile.Center;
			distanceToIdlePosition = vectorToIdlePosition.Length();

			if (Main.myPlayer == owner.whoAmI && distanceToIdlePosition > 2000f)
			{
				// Whenever you deal with non-regular events that change the behavior or position drastically, make sure to only run the code on the owner of the projectile,
				// and then set netUpdate to true
				Projectile.position = idlePosition;
				Projectile.velocity *= 0.1f;
				Projectile.netUpdate = true;
			}

			// If your minion is flying, you want to do this independently of any conditions
			float overlapVelocity = 0.04f;

			// Fix overlap with other minions
			for (int i = 0; i < Main.maxProjectiles; i++)
			{
				Projectile other = Main.projectile[i];

				if (i != Projectile.whoAmI && other.active && other.owner == Projectile.owner && Math.Abs(Projectile.position.X - other.position.X) + Math.Abs(Projectile.position.Y - other.position.Y) < Projectile.width)
				{
					if (Projectile.position.X < other.position.X)
					{
						Projectile.velocity.X -= overlapVelocity;
					}
					else
					{
						Projectile.velocity.X += overlapVelocity;
					}

					if (Projectile.position.Y < other.position.Y)
					{
						Projectile.velocity.Y -= overlapVelocity;
					}
					else
					{
						Projectile.velocity.Y += overlapVelocity;
					}
				}
			}
		}

		Vector2 targetCenter;
		NPC target;
		private void SearchForTargets(Player owner, out bool foundTarget, out float distanceFromTarget)
		{
			// Starting search distance
			distanceFromTarget = 700f;
			targetCenter = Projectile.position;
			foundTarget = false;

			// This code is required if your minion weapon has the targeting feature
			if (owner.HasMinionAttackTargetNPC)
			{
				NPC npc = Main.npc[owner.MinionAttackTargetNPC];
				float between = Vector2.Distance(npc.Center, Projectile.Center);

				// Reasonable distance away so it doesn't target across multiple screens
				if (between < 1400f)
				{
					distanceFromTarget = between;
					targetCenter = npc.Center;
					foundTarget = true;
					target = npc;
				}
			}

			if (!foundTarget)
			{
				// This code is required either way, used for finding a target
				for (int i = 0; i < Main.maxNPCs; i++)
				{
					NPC npc = Main.npc[i];

					if (npc.CanBeChasedBy())
					{
						float between = Vector2.Distance(npc.Center, Projectile.Center);
						bool closest = Vector2.Distance(Projectile.Center, targetCenter) > between;
						bool inRange = between < distanceFromTarget;
						// Additional check for this specific minion behavior, otherwise it will stop attacking once it dashed through an enemy while flying though tiles afterwards
						// The number depends on various parameters seen in the movement code below. Test different ones out until it works alright
						bool closeThroughWall = between < 700f;

						if (((closest && inRange) || (!foundTarget && inRange)) && (closeThroughWall))

						{
							distanceFromTarget = between;
							targetCenter = npc.Center;
							foundTarget = true;
							target = npc;
						}
					}
				}
			}

			float angle = 0;
			if(owner.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.TetsujinProjectile>()] > 0)
            {
				angle = (Main.GameUpdateCount / 120f) + (int)(Main.GameUpdateCount / 480f) + MathHelper.TwoPi * (Projectile.ai[0] / owner.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Summon.TetsujinProjectile>()]);

			}
			targetCenter += new Vector2(300, 0).RotatedBy(angle);

			// friendly needs to be set to true so the minion can deal contact damage
			// friendly needs to be set to false so it doesn't damage things like target dummies while idling
			// Both things depend on if it has a target or not, so it's just one assignment here
			// You don't need this assignment if your minion is shooting things instead of dealing contact damage
			Projectile.friendly = foundTarget;

		}

		Vector2 acceleration = Vector2.Zero;
		float accelerationMagnitude = 5f / 60f; //Jerk is change in acceleration
		float topSpeed = 10;
		float flyingTime = 0;
		private void Movement(bool foundTarget, float distanceFromTarget, float distanceToIdlePosition, Vector2 vectorToIdlePosition)
		{
			if (foundTarget)
			{
				float distance = Vector2.Distance(Projectile.Center, targetCenter);
				if (distance > 35)
				{
					accelerationMagnitude = 0.7f + flyingTime / 60;
					acceleration = UsefulFunctions.GenerateTargetingVector(Projectile.Center, targetCenter, accelerationMagnitude);
					if(distance < 100)
                    {
						acceleration *= 5;
                    }

					if (!acceleration.HasNaNs())
					{
						Projectile.velocity += acceleration;
					}
					if (Projectile.velocity.Length() > topSpeed)
					{
						Projectile.velocity.Normalize();
						Projectile.velocity *= topSpeed;
					}
                }
                else
				{
					Projectile.velocity = UsefulFunctions.GenerateTargetingVector(Projectile.Center, targetCenter, 1).RotatedBy(0);
					Projectile.Center = targetCenter;
                }
			}
			else
			{
				float speed;
				float inertia;
				// Minion doesn't have a target: return to player and idle
				if (distanceToIdlePosition > 100f)
				{
					// Speed up the minion if it's away from the player
					speed = 24f;
					inertia = 60f;
				}
				else
				{
					// Slow down the minion if closer to the player
					speed = 8f;
					inertia = 80f;
				}

				if (distanceToIdlePosition > 20f)
				{
					// The immediate range around the player (when it passively floats about)

					// This is a simple movement formula using the two parameters and its desired direction to create a "homing" movement
					vectorToIdlePosition.Normalize();
					vectorToIdlePosition *= speed;
					Projectile.velocity = (Projectile.velocity * (inertia - 1) + vectorToIdlePosition) / inertia;
				}
				else if (Projectile.velocity == Vector2.Zero)
				{
					// If there is a case where it's not moving at all, give it a little "poke"
					Projectile.velocity.X = -0.15f;
					Projectile.velocity.Y = -0.05f;
				}
			}
		}

		private void Visuals()
		{
			// So it will lean slightly towards the direction it's moving
			Projectile.rotation = Projectile.velocity.X * 0.1f;

			// This is a simple "loop through all frames from top to bottom" animation
			int frameSpeed = 5;

			


			if (Projectile.velocity.Y < 0)
			{
				Projectile.frameCounter++;

				if (Projectile.frameCounter >= frameSpeed)
				{
					Projectile.frameCounter = 0;
					Projectile.frame++;

					if (Projectile.frame >= Main.projFrames[Projectile.type])
					{
						Projectile.frame = 0;
					}
				}
			}

			// Some visuals here
			Lighting.AddLight(Projectile.Center, Color.OrangeRed.ToVector3() * 0.78f);
		}

		public static Effect attraidiesEffect;
		float effectTimer;
        public override bool PreDraw(ref Color lightColor)
        {
			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

			Color color = Color.Purple * 3;
			Lighting.AddLight(Projectile.Center, color.ToVector3());
			Rectangle baseRectangle = new Rectangle(0, 0, 200, 200);
			Vector2 baseOrigin = baseRectangle.Size() / 2f;
			effectTimer++;

			//Apply the shader, caching it as well
			if (attraidiesEffect == null)
			{
				attraidiesEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/AttraidiesAura", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
			}

			//Pass relevant data to the shader via these parameters
			attraidiesEffect.Parameters["textureSize"].SetValue(tsorcRevamp.tNoiseTexture1.Width);
			attraidiesEffect.Parameters["effectSize"].SetValue(baseRectangle.Size());

			attraidiesEffect.Parameters["effectColor1"].SetValue(UsefulFunctions.ShiftColor(color, effectTimer / 25f).ToVector4());
			attraidiesEffect.Parameters["effectColor2"].SetValue(UsefulFunctions.ShiftColor(color, effectTimer / 25f).ToVector4());
			attraidiesEffect.Parameters["ringProgress"].SetValue(0.1f);
			attraidiesEffect.Parameters["fadePercent"].SetValue(0);
			attraidiesEffect.Parameters["scaleFactor"].SetValue(.5f * 150);
			attraidiesEffect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 0.05f);
			attraidiesEffect.Parameters["colorSplitAngle"].SetValue(MathHelper.TwoPi);

			//Apply the shader
			attraidiesEffect.CurrentTechnique.Passes[0].Apply();

			Main.EntitySpriteDraw(tsorcRevamp.tNoiseTexture1, Projectile.Center - Main.screenPosition, baseRectangle, Color.White, MathHelper.TwoPi, baseOrigin, 1, SpriteEffects.None, 0);
			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
			return false;
		}
    }
}