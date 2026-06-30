using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Summon.Archer
{
    public class ArcherSpiritArrow : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.FrostburnArrow;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Archer Spirit Arrow");
        }

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.FrostburnArrow);
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.minion = true;
            Projectile.DamageType = DamageClass.Summon;
            AIType = ProjectileID.FrostburnArrow;
        }

        public override void AI()
        {
            Projectile.velocity.Y = Projectile.velocity.Y;

            if (Main.rand.NextBool(2)) 
            {
                Dust dust = Dust.NewDustDirect(
                    Projectile.position,               
                    Projectile.width,                 
                    Projectile.height,                 
                    135,                               
                    Projectile.velocity.X * 0.2f,     
                    Projectile.velocity.Y * 0.2f,      
                    100,                               
                    default,                           
                    1.1f                              
                );

                dust.noGravity = false;                
                dust.velocity *= 0.5f;                
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Frostburn2, 180); // Inflicts Frostburn for 3 seconds
        }
    }
}
