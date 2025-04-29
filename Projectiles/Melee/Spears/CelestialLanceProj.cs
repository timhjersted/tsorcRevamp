using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Melee.Spears;

namespace tsorcRevamp.Projectiles.Melee.Spears
{
    class CelestialLanceProj : ModdedSpearProjectile
    {
        public override float HoldoutRangeMin => 95f;
        public override float HoldoutRangeMax => 285f;
        public override float HitboxSize => 1;
        public override float Scale => 1;
        public override int dustID => DustID.HallowedTorch;
        bool hasHealed = false;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[Projectile.owner];
            if (Main.rand.NextBool(6) && !hasHealed)
            {
                player.statLife += CelestialLance.HealOnHit;
                player.HealEffect(CelestialLance.HealOnHit, true);
                hasHealed = true;
            }

            // Spawn 2 stars when hitting an enemy, pretty much the same code than starfall
            if (Main.netMode != NetmodeID.MultiplayerClient) 
            {
                for (int i = 0; i < 2; i++) 
                {
                    Vector2 starSpawnPos = target.Center;
                    starSpawnPos.Y -= 500f - 100 * i; 
                    starSpawnPos.X += Main.rand.Next(-200, 201); 

                    float velX = target.Center.X - starSpawnPos.X + Main.rand.Next(-40, 41) * 0.03f; 
                    float velY = target.Center.Y - starSpawnPos.Y;
                    if (velY < 0f) 
                    {
                        velY *= -1f;
                    }
                    if (velY < 20f) 
                    {
                        velY = 20f;
                    }

                    //Choose a random star between starfury and the star veil one
                    Vector2 starVelocity = Vector2.Normalize(new Vector2(velX, velY)) * 10f; 
                    starVelocity.Y += Main.rand.Next(-40, 41) * 0.02f; 

                    int starDamage = damageDone / 2; 

                    int starType;
                    switch (Main.rand.Next(3)) 
                    {
                        case 0:
                            starType = ProjectileID.BeeCloakStar;
                            break;
                        case 1:
                            starType = ProjectileID.StarCloakStar;
                            break;
                        default: 
                            starType = ProjectileID.StarVeilStar;
                            break;
                    }

                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(), 
                        starSpawnPos,                  
                        starVelocity * 2.2f,            
                        starType,                       
                        starDamage,                    
                        3f,                             
                        Projectile.owner                
                    );
                }
            }
        }
    }
}