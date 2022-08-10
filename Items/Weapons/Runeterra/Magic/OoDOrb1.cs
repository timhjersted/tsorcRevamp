/*
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Runeterra.Magic
{
    public class OoDOrb1 : ModProjectile
    {
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
            Projectile.width = 24;
            Projectile.height = 24;
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

            int launchTimeLimit = 50;  // How much time the projectile can go before retracting (speed and shootTimer will set the flail's range)
            float launchSpeed = 10f; // How fast the projectile can move
            float maxLaunchLength = 1000f; // How far the projectile's chain can stretch before being forced to retract when in launched state
            float retractAcceleration = 8f; // How quickly the projectile will accelerate back towards the player while retracting
            float maxRetractSpeed = 24f; // The max speed the projectile will have while retracting
            float forcedRetractAcceleration = 16f; // How quickly the projectile will accelerate back towards the player while being forced to retract
            float maxForcedRetractSpeed = 48f; // The max speed the projectile will have while being forced to retract

            if (!owner.active || owner.dead || owner.noItems || owner.CCed || Vector2.Distance(Projectile.Center, owner.Center) > 900f)
            {
                Projectile.Kill();
                return;
            }
			Main.NewText(CurrentAIState);
			Main.NewText(OoDItem1.useOoDItem1);
			switch (CurrentAIState)
			{
				case AIState.InHand:
					{
						if (Projectile.owner == Main.myPlayer)
						{

							Vector2 unitVectorTowardsMouse = owner.Center.DirectionTo(Main.MouseWorld).SafeNormalize(Vector2.UnitX * owner.direction);
							owner.ChangeDir((unitVectorTowardsMouse.X > 0f) ? 1 : (-1));
							if (OoDItem1.useOoDItem1 == 1) // If the player releases then change to moving forward mode
							{
								CurrentAIState = AIState.ThrownForward;
								StateTimer = 0f;
								Projectile.velocity = unitVectorTowardsMouse * launchSpeed;
								//Projectile.Center = unitVectorTowardsMouse;
								Projectile.netUpdate = true;
								break;
							}
						}
						OoDItem1.useOoDItem1 = 0;
						Projectile.damage = 0;
						if (owner.direction == 1)
						{
							Projectile.position = owner.Center + new Vector2(7, -14);//7
						}
						else
						{
							Projectile.position = owner.Center + new Vector2(-29, -14);
						}
						break;
					}
				case AIState.ThrownForward:
					{
						Projectile.damage = Projectile.originalDamage;
						bool shouldSwitchToRetracting = StateTimer++ >= launchTimeLimit;
						shouldSwitchToRetracting |= Projectile.Distance(owner.Center) >= maxLaunchLength;
						if (OoDItem1.useOoDItem1 == 2) // If the player clicks, transition to the ForcedRetract state
						{
							CurrentAIState = AIState.ForcedRetracting;
							StateTimer = 0f;
							Projectile.netUpdate = true;
							Projectile.velocity *= 0.2f;
							break;
						}
						if (shouldSwitchToRetracting)
						{
							CurrentAIState = AIState.Retracting;
							StateTimer = 0f;
							Projectile.netUpdate = true;
							Projectile.velocity *= 0.3f;
						}
						break;
					}
				case AIState.Retracting:
					{
						Projectile.damage = Projectile.originalDamage * 2;
						Vector2 unitVectorTowardsPlayer = Projectile.DirectionTo(owner.Center).SafeNormalize(Vector2.Zero);
						if (Projectile.Distance(owner.Center) <= 5f)
						{
							OoDItem1.useOoDItem1 = 3;
							CurrentAIState = AIState.InHand;
							return;
						}
						if (owner.controlUseItem) // If the player clicks, transition to the ForcedRetract state
						{
							CurrentAIState = AIState.ForcedRetracting;
							StateTimer = 0f;
							Projectile.netUpdate = true;
							Projectile.velocity *= 0.2f;
						}
						else
						{
							Projectile.velocity *= 0.98f;
							Projectile.velocity = Projectile.velocity.MoveTowards(unitVectorTowardsPlayer * maxRetractSpeed, retractAcceleration);
						}
						break;
					}
				case AIState.ForcedRetracting:
					{
						Projectile.damage = Projectile.originalDamage * 2;
						Vector2 unitVectorTowardsPlayer = Projectile.DirectionTo(owner.Center).SafeNormalize(Vector2.Zero);
						if (Projectile.Distance(owner.Center) <= maxForcedRetractSpeed)
						{
							CurrentAIState = AIState.InHand;
							return;
						}
						Projectile.velocity *= 0.98f;
						Projectile.velocity = Projectile.velocity.MoveTowards(unitVectorTowardsPlayer * maxForcedRetractSpeed, forcedRetractAcceleration);
						Vector2 target = Projectile.Center + Projectile.velocity;
						Vector2 value = owner.Center.DirectionFrom(target).SafeNormalize(Vector2.Zero);
						if (Vector2.Dot(unitVectorTowardsPlayer, value) < 0f)
						{
							CurrentAIState = AIState.InHand;
							return;
						}
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

            // Some visuals here
            Lighting.AddLight(Projectile.Center, Color.LightSteelBlue.ToVector3() * 0.78f);
            Dust.NewDust(Projectile.Center, 2, 2, DustID.MagicMirror, 0, 0, 150, default, 0.5f);
        }
    }
}*/