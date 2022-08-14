
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Runeterra.Magic
{
    public class OoDOrb3 : ModProjectile
    {
		public static int EssenceThief3 = 0;
		public static bool EssenceFilled3 = false;
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
			Projectile.width = 48;
            Projectile.height = 48;
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

            int launchTimeLimit = 50;  // How much time the projectile can go before retracting (speed and shootTimer will set the flail's range)
            float launchSpeed = 16f; // How fast the projectile can move
            float maxLaunchLength = 1000f; // How far the projectile's chain can stretch before being forced to retract when in launched state
            float retractAcceleration = 8f; // How quickly the projectile will accelerate back towards the player while retracting
            float maxRetractSpeed = 16f; // The max speed the projectile will have while retracting
            float forcedRetractAcceleration = 16f; // How quickly the projectile will accelerate back towards the player while being forced to retract
            float maxForcedRetractSpeed = 32f; // The max speed the projectile will have while being forced to retract

			if (CurrentAIState == AIState.InHand & OoDItem3.useOoDItem3 == 2)
            {
				OoDItem3.useOoDItem3 = 0;
            }

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
							if (OoDItem3.useOoDItem3 == 1) // If the player releases then change to moving forward mode
							{
								if (EssenceThief3 >= 9)
								{
									EssenceFilled3 = true;
								} else
                                {
									EssenceFilled3 = false;
                                }
								CurrentAIState = AIState.ThrownForward;
								StateTimer = 0f;
								Projectile.velocity = unitVectorTowardsMouse * launchSpeed;
								//Projectile.Center = unitVectorTowardsMouse;
								Projectile.netUpdate = true;
								break;
							}
						}
						OoDItem3.useOoDItem3 = 0;
						Projectile.damage = 0;
						break;
					}
				case AIState.ThrownForward:
					{
						Projectile.damage = Projectile.originalDamage;
						bool shouldSwitchToRetracting = StateTimer++ >= launchTimeLimit;
						shouldSwitchToRetracting |= Projectile.Distance(owner.Center) >= maxLaunchLength;

						if (OoDItem3.useOoDItem3 == 2) // If the player clicks, transition to the ForcedRetract state
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
							OoDItem3.useOoDItem3 = 0;
							CurrentAIState = AIState.InHand;
							if(EssenceFilled3 == true)
                            {
								EssenceThief3 -= 9;
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
						if (OoDItem3.useOoDItem3 == 2 | Projectile.Distance(owner.Center) >= 1000f) // If the player clicks, transition to the ForcedRetract state
						{
							OoDItem1.useOoDItem1 = 2;
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
							OoDItem3.useOoDItem3 = 0;
							CurrentAIState = AIState.InHand;
							if (EssenceFilled3 == true)
							{
								EssenceThief3 -= 9;
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
				EssenceThief3 += 2;
			}
			else
			{
				EssenceThief3 += 1;
			}
			if (EssenceFilled3)
			{
				if (crit)
				{
					Projectile.NewProjectile(Projectile.GetSource_NaturalSpawn(), owner.Top, Vector2.One, ModContent.ProjectileType<OoDFlame2>(), owner.GetWeaponDamage(owner.HeldItem), owner.GetWeaponKnockback(owner.HeldItem), Main.myPlayer);
					Projectile.NewProjectile(Projectile.GetSource_NaturalSpawn(), owner.Bottom, Vector2.One, ModContent.ProjectileType<OoDFlame2>(), owner.GetWeaponDamage(owner.HeldItem), owner.GetWeaponKnockback(owner.HeldItem), Main.myPlayer);

					owner.Heal(damage / 5);
				}
				else
				{
					Projectile.NewProjectile(Projectile.GetSource_NaturalSpawn(), owner.Center, Vector2.One, ModContent.ProjectileType<OoDFlame2>(), owner.GetWeaponDamage(owner.HeldItem), owner.GetWeaponKnockback(owner.HeldItem), Main.myPlayer);
					owner.Heal(damage / 10);
				}
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