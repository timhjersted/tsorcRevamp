using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Summon;
using tsorcRevamp.Buffs.Weapons.Summon;
using tsorcRevamp.Items.Weapons.Summon;
using tsorcRevamp.Projectiles.Summon.SummonProjectiles;
using tsorcRevamp.Projectiles.VFX;

namespace tsorcRevamp.Projectiles.Summon.Runeterra.Dragon
{
	public class CotUDragon : DynamicTrail
	{
		public int WarmupStacksFallOffTimer = 0;
		public int WarmupStacksTimer = 0;
		public int WarmupStacks = 0;
		public int BaseAttackSpeedCooldown = 33; //Lower is better

        public override string Texture => "tsorcRevamp/Projectiles/Summon/Runeterra/Dragon/FullSample";


		public override void SetStaticDefaults()
		{
			Main.projFrames[Projectile.type] = 8;
			// This is necessary for right-click targeting
			ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;

			Main.projPet[Projectile.type] = true; // Denotes that this projectile is a pet or minion

			ProjectileID.Sets.MinionSacrificable[Projectile.type] = true; // This is needed so your minion can properly spawn when summoned and replaced when other minions are summoned
			ProjectileID.Sets.CultistIsResistantTo[Projectile.type] = true; // Make the cultist resistant to this projectile, as it's resistant to all homing projectiles.

			ProjectileID.Sets.SummonTagDamageMultiplier[Projectile.type] = PhoenixEgg.SummonTagDmgMult / 100f;
		}

		public sealed override void SetDefaults()
		{
			Projectile.width = 104;
			Projectile.height = 93;
			Projectile.tileCollide = false; // Makes the minion go through tiles freely

			// These below are needed for a minion weapon
			Projectile.friendly = true; // Only controls if it deals damage to enemies on contact (more on that later)
			Projectile.minion = true; // Declares this as a minion (has many effects)
			Projectile.DamageType = DamageClass.Summon; // Declares the damage type (needed for it to deal damage)
			Projectile.minionSlots = 2f; // Amount of slots this minion occupies from the total minion slots available to the player (more on that later)
			Projectile.penetrate = -1; // Needed so the minion doesn't despawn on collision with enemies or tiles
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = BaseAttackSpeedCooldown;

			newPointDistance = 0.3f;
			trailCollision = true;
			trailWidth = 65;
			trailMaxLength = 200;
			trailPointLimit = 150;
			trailYOffset = 30;
			NPCSource = false;
			collisionFrequency = 2;
			noFadeOut = true;
			customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/DrawSprite", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
		}
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
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

		// The AI of this minion is split into multiple methods to avoid bloat. This method just passes values between calls actual parts of the AI.
		public override void AI()
		{
			trailWidth = 45;
			trailMaxLength = 200;
			base.AI();
			Player owner = Main.player[Projectile.owner];

			if (!CheckActive(owner))
			{
				return;
			}

			GeneralBehavior(owner, out Vector2 vectorToIdlePosition, out float distanceToIdlePosition);
			SearchForTargets(owner, out bool foundTarget, out float distanceFromTarget, out Vector2 targetCenter);
			Movement(foundTarget, distanceFromTarget, targetCenter, distanceToIdlePosition, vectorToIdlePosition);
			Visuals();
        }

		// This is the "active check", makes sure the minion is alive while the player is alive, and despawns if not
		private bool CheckActive(Player owner)
		{
			if (owner.dead || !owner.active)
			{
				owner.ClearBuff(ModContent.BuffType<CenterOfTheUniverseBuff>());

				return false;
			}

			if (owner.HasBuff(ModContent.BuffType<CenterOfTheUniverseBuff>()))
			{
				Projectile.timeLeft = 2;
			}

			return true;
		}

		private void GeneralBehavior(Player owner, out Vector2 vectorToIdlePosition, out float distanceToIdlePosition)
		{
			Vector2 idlePosition = owner.Center + new Vector2(150, 0).RotatedBy(Main.GameUpdateCount / 20f);

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
				Dust.NewDustPerfect(Projectile.Center, DustID.SolarFlare, null, 0, Color.DarkOrange, 1);
				Projectile.netUpdate = true;
			}
		}

		private void SearchForTargets(Player owner, out bool foundTarget, out float distanceFromTarget, out Vector2 targetCenter)
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
						bool lineOfSight = Collision.CanHitLine(Projectile.position, Projectile.width, Projectile.height, npc.position, npc.width, npc.height);
						// Additional check for this specific minion behavior, otherwise it will stop attacking once it dashed through an enemy while flying though tiles afterwards
						// The number depends on various parameters seen in the movement code below. Test different ones out until it works alright
						bool closeThroughWall = between < 700f;

						if (((closest && inRange) || (!foundTarget && inRange)) && (lineOfSight || closeThroughWall))

						{
							distanceFromTarget = between;
							targetCenter = npc.Center;
							foundTarget = true;
						}
					}
				}
			}

			// friendly needs to be set to true so the minion can deal contact damage
			// friendly needs to be set to false so it doesn't damage things like target dummies while idling
			// Both things depend on if it has a target or not, so it's just one assignment here
			// You don't need this assignment if your minion is shooting things instead of dealing contact damage
			Projectile.friendly = foundTarget;
		}

		bool chargingTarget = false;
		bool activelyCharging = false;
		Vector2 offsetPoint = Vector2.Zero;
		private void Movement(bool foundTarget, float distanceFromTarget, Vector2 targetCenter, float distanceToIdlePosition, Vector2 vectorToIdlePosition)
		{
			activelyCharging = false;
			if (foundTarget)
			{
				if (!chargingTarget)
				{
					if (offsetPoint == Vector2.Zero)
					{
						offsetPoint = targetCenter + new Vector2(400, 0).RotatedBy(Projectile.velocity.ToRotation() + MathHelper.Pi / 3);
					}

					targetCenter = offsetPoint;
				}

				//UsefulFunctions.DustRing(targetCenter, 50, DustID.ShadowbeamStaff);

				// Minion has a target: attack (here, fly towards the enemy)
				if (Vector2.Distance(Projectile.Center, targetCenter) > 30)
				{
					UsefulFunctions.SmoothHoming(Projectile, targetCenter, 0.5f, 15f, bufferZone: false);
                    if (chargingTarget)
                    {
						activelyCharging = true;
						fade = 30;
                    }
				}
                else
                {
					chargingTarget = !chargingTarget;
					offsetPoint = Vector2.Zero;
                }
			}
			else
			{
				chargingTarget = true;
				offsetPoint = Vector2.Zero;
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
			Projectile.rotation = Projectile.velocity.ToRotation();

			Projectile.frameCounter++;
			if(Projectile.frameCounter > 3)
            {
				Projectile.frameCounter = 0;
				Projectile.frame++;
				if(Projectile.frame >= Main.projFrames[Projectile.type])
                {
					Projectile.frame = 0;
				}
            }

			//Breath weapon size calculations
            if (activelyCharging)
			{
				if(trueSizeMultiplier < 0.8) //Its real max size (ignore the other variable)
                {
					trueSizeMultiplier += 0.02f; //How fast the breath attack flame grows
                }
				if (size < maxSize)
				{
					size += 10f;
				}
            }
            else
			{
				if (fade > 0)
				{
					fade--;
				}
                else
				{
					trueSizeMultiplier = 0;
					size = 0;
                }
			}
		}

		float fade;
		float size;
		float maxSize = 2700;
		float trueSizeMultiplier;
		//If the flamethrower is on, it uses that as part of its hitbox
		//It checks if the flamethrower is colliding first, then if it is not it checks the trail body for collisions (base.Colliding)
		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			float distance = Vector2.Distance(Projectile.Center, targetHitbox.Center.ToVector2());
			float angleBetween = (float)UsefulFunctions.CompareAngles(Vector2.Normalize(Projectile.Center - targetHitbox.Center.ToVector2()), Projectile.rotation.ToRotationVector2());
            if (distance < trueSizeMultiplier * (600 + (size / 6f)) && Math.Abs(angleBetween - MathHelper.Pi) < angle / 2.85f)
            {
				return true;
            }
            else
            {
				return base.Colliding(projHitbox, targetHitbox);
            }
		}

		public static Effect effect;
		public float angle;
		public override bool PreDraw(ref Color lightColor)
		{
			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
			base.PreDraw(ref lightColor); //Draw the dragon

			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

			//Draw the breath weapon
			//if (effect == null)
			{
				effect = ModContent.Request<Effect>("tsorcRevamp/Effects/DragonBreath", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
				//effect = new Effect(Main.graphics.GraphicsDevice, Mod.GetFileBytes("Effects/SyntheticFirestorm"));
			}

			angle = MathHelper.TwoPi / 10f;
			float shaderRotation = 0;
			shaderRotation %= MathHelper.TwoPi;
			effect.Parameters["splitAngle"].SetValue(angle);
			effect.Parameters["rotation"].SetValue(shaderRotation);
			effect.Parameters["time"].SetValue((float)Main.timeForVisualEffects / 252);
			effect.Parameters["length"].SetValue(.01f * size / maxSize);
			float opacity = 1;

			if (fade < 30)
			{
				MathHelper.Lerp(0.01f, 1, fade / 30f);
				opacity *= fade / 30f;
				opacity *= fade / 30f;
			}

			effect.Parameters["opacity"].SetValue(opacity * 5);
			effect.Parameters["texScale"].SetValue(tsorcRevamp.NoiseSmooth.Size() / 500);
			effect.Parameters["texScale3"].SetValue(tsorcRevamp.NoiseWavy.Size() / 1000);
			effect.Parameters["noiseTexture2"].SetValue(tsorcRevamp.NoiseWavy);

			//I precompute many values once here so that I don't have to calculate them for every single pixel in the shader. Enormous performance save.
			effect.Parameters["rotationMinusPI"].SetValue(shaderRotation - MathHelper.Pi);
			effect.Parameters["splitAnglePlusRotationMinusPI"].SetValue(shaderRotation + angle - MathHelper.Pi);
			effect.Parameters["RotationMinus2PIMinusSplitAngleMinusPI"].SetValue((shaderRotation - (MathHelper.TwoPi - angle)) - MathHelper.Pi);

			//Apply the shader
			effect.CurrentTechnique.Passes[0].Apply();

			Rectangle recsize = new Rectangle(0, 0, tsorcRevamp.NoiseSmooth.Width, tsorcRevamp.NoiseSmooth.Height);
			Vector2 origin = new Vector2(recsize.Width * 0.5f, recsize.Height * 0.5f);
			Vector2 fudgeFactor = new Vector2(20, 0).RotatedBy(Projectile.rotation); //Shifts the start of the fire backwards a bit toward its mouth

			//Draw the rendertarget with the shader
			Main.spriteBatch.Draw(tsorcRevamp.NoiseSmooth, Projectile.Center - Main.screenPosition - fudgeFactor, recsize, Color.White, Projectile.rotation + (MathHelper.Pi - angle / 2f), origin, trueSizeMultiplier * trueSizeMultiplier * 7.5f, SpriteEffects.None, 0);

			//Restart the spritebatch so the shader doesn't get applied to the rest of the game
			UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);

			return false;
		}

		Texture2D dragonTexture;
        public override void SetEffectParameters(Effect effect)
		{
			//This is quicker than EnsureLoaded, since the game already loaded it into TextureAssets
			if(dragonTexture == null || dragonTexture.IsDisposed)
            {
				dragonTexture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type];
			}
			customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/DrawSprite", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
			effect = ModContent.Request<Effect>("tsorcRevamp/Effects/DrawSprite", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;

			effect.Parameters["spriteTexture"].SetValue(dragonTexture);
			effect.Parameters["spriteFramecount"].SetValue(Main.projFrames[Projectile.type]);
			effect.Parameters["spriteCurrentFrame"].SetValue(Projectile.frame); //Set this 0-7 to pick the frame to draw
			int flipSprite = 1; //Controls sprite dirction
			if(Projectile.velocity.X < 0)
            {
				flipSprite = -1;
            }
			effect.Parameters["flipSprite"].SetValue(flipSprite);
			effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
		}
    }
}