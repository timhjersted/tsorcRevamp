
using tsorcRevamp.Items.Weapons.Magic.Runeterra;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Magic.Runeterra
{
    public class OrbofDeceptionProj : ModProjectile
    {
		public static int EssenceThief1 = 0;
		public static bool EssenceFilled1 = false;
        private enum AIState
        {
            InHand,
            ThrownForward,
            Retracting,
            ForcedRetracting,
            UnusedState,
            UnusedState2,
            UnusedState3
        }

        // These properties wrap the usual ai and localAI arrays for cleaner and easier to understand code.
        private AIState CurrentAIState
        {
            get => (AIState)Projectile.ai[0];
            set => Projectile.ai[0] = (float)value;
        }
        public ref float StateTimer => ref Projectile.ai[1];
        public ref float CollisionCounter => ref Projectile.localAI[0];
        public ref float SpinningStateTimer => ref Projectile.localAI[1];
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 9;
        }
        public override void SetDefaults()
        {
            Projectile.netImportant = true;
			Projectile.width = 28;
            Projectile.height = 28;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
        }
        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
			Projectile.CritChance = owner.GetWeaponCrit(owner.HeldItem);

			if (CurrentAIState == AIState.InHand & OrbofDeception.useOrbofDeception == 2)
			{
				OrbofDeception.useOrbofDeception = 0;
			}

			int launchTimeLimit = 50;  // How much time the projectile can go before retracting (speed and shootTimer will set the flail's range)
            float launchSpeed = 10f; // How fast the projectile can move
            float maxLaunchLength = 1000f; // How far the projectile's chain can stretch before being forced to retract when in launched state
            float retractAcceleration = 5f; // How quickly the projectile will accelerate back towards the player while retracting
            float maxRetractSpeed = 10f; // The max speed the projectile will have while retracting
            float forcedRetractAcceleration = 10f; // How quickly the projectile will accelerate back towards the player while being forced to retract
            float maxForcedRetractSpeed = 20f; // The max speed the projectile will have while being forced to retract

            if (!owner.active || owner.dead || owner.noItems || owner.CCed || Vector2.Distance(Projectile.Center, owner.Center) > 1500f)
            {
                Projectile.Kill();
                return;
            }
			switch (CurrentAIState)
			{
				case AIState.InHand:
					{
						if (owner.direction == 1)
						{
							Projectile.position = owner.Center + new Vector2(7, -14);//7
						}
						else
						{
							Projectile.position = owner.Center + new Vector2(-29, -14);
						}
						if (Projectile.owner == Main.myPlayer)
						{

							Vector2 unitVectorTowardsMouse = owner.Center.DirectionTo(Main.MouseWorld).SafeNormalize(Vector2.UnitX * owner.direction);
							owner.ChangeDir((unitVectorTowardsMouse.X > 0f) ? 1 : (-1));
							if (OrbofDeception.useOrbofDeception == 1) // If the player releases then change to moving forward mode
							{
								if (EssenceThief1 >= 9)
								{
									EssenceFilled1 = true;
								} else
                                {
									EssenceFilled1 = false;
                                }
								CurrentAIState = AIState.ThrownForward;
								StateTimer = 0f;
								Projectile.velocity = unitVectorTowardsMouse * launchSpeed;
								//Projectile.Center = unitVectorTowardsMouse;
								Projectile.netUpdate = true;
								break;
							}
						}
						OrbofDeception.useOrbofDeception = 0;
						Projectile.damage = 0;
						break;
					}
				case AIState.ThrownForward:
					{
						Projectile.damage = Projectile.originalDamage;
						bool shouldSwitchToRetracting = StateTimer++ >= launchTimeLimit;
						shouldSwitchToRetracting |= Projectile.Distance(owner.Center) >= maxLaunchLength;

						if (OrbofDeception.useOrbofDeception == 2) // If the player clicks, transition to the ForcedRetract state
						{
							CurrentAIState = AIState.ForcedRetracting;
							Projectile.ResetLocalNPCHitImmunity();
							Projectile.damage *= 2;
							StateTimer = 0f;
							Projectile.netUpdate = true;
							break;
						}
						if (shouldSwitchToRetracting)
						{
							CurrentAIState = AIState.Retracting;
							Projectile.ResetLocalNPCHitImmunity();
							Projectile.damage *= 2;
							StateTimer = 0f;
							Projectile.netUpdate = true;
						}
						break;
					}
				case AIState.Retracting:
					{
						Projectile.damage = Projectile.originalDamage * 2;
						Vector2 unitVectorTowardsPlayer = Projectile.DirectionTo(owner.Center).SafeNormalize(Vector2.Zero);
						if (Projectile.Distance(owner.Center) <= 25f)
						{
							OrbofDeception.useOrbofDeception = 0;
							CurrentAIState = AIState.InHand;
							if(EssenceFilled1 == true)
                            {
								EssenceThief1 -= 9;
                            }
							if (owner.direction == 1)
							{
								Projectile.position = owner.Center + new Vector2(7, -14);//7
							}
							else
							{
								Projectile.position = owner.Center + new Vector2(-29, -14);
							}
							return;
						}
						if (OrbofDeception.useOrbofDeception == 2 | Projectile.Distance(owner.Center) >= 1000f) // If the player clicks, transition to the ForcedRetract state
						{
							OrbofDeception.useOrbofDeception = 2;
							CurrentAIState = AIState.ForcedRetracting;
							StateTimer = 0f;
							Projectile.netUpdate = true;
						}
						else
						{
							Projectile.velocity = Projectile.velocity.MoveTowards(unitVectorTowardsPlayer * maxRetractSpeed, retractAcceleration);
						}
						break;
					}
				case AIState.ForcedRetracting:
					{
						Projectile.damage = Projectile.originalDamage * 2;
						Vector2 unitVectorTowardsPlayer = Projectile.DirectionTo(owner.Center).SafeNormalize(Vector2.Zero);
						if (Projectile.Distance(owner.Center) <= 25f)
						{
							OrbofDeception.useOrbofDeception = 0;
							CurrentAIState = AIState.InHand;
							if (EssenceFilled1 == true)
							{
								EssenceThief1 -= 9;
							}
							return;
						}
						Projectile.velocity = Projectile.velocity.MoveTowards(unitVectorTowardsPlayer * maxForcedRetractSpeed, forcedRetractAcceleration);
						break;
					}
				case AIState.UnusedState:
					{
						break;
					}
				case AIState.UnusedState2:
					break;
				case AIState.UnusedState3:
					break;
			}



            Visuals();
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
			Player owner = Main.player[Projectile.owner];
			if (crit)
			{
				EssenceThief1 += 2;
			}
			else
			{
				EssenceThief1 += 1;
			}
			if (EssenceFilled1)
			{
				if(crit)
                {
					owner.Heal(damage / 5);
                } else
				owner.Heal(damage / 10);
			}

		}
        private void Visuals()
        {
            int frameSpeed = 5;

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

            Lighting.AddLight(Projectile.Center, Color.LightSteelBlue.ToVector3() * 0.78f);
            Dust.NewDust(Projectile.Center, 2, 2, DustID.MagicMirror, 0, 0, 150, default, 0.5f);
        }
    }
}