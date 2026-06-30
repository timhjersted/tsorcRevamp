using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    public class EnemyFrostburnArrow : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.FrostburnArrow;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Enemy Frostburn Arrow");
        }

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.FrostburnArrow);
            Projectile.friendly = false;
            Projectile.hostile = true;
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

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Frostburn, 180); // 3 seconds of Frostburn debuff
        }

        public override void OnKill(int timeLeft)
        {
            // A modded projectile type does NOT inherit vanilla's hardcoded type-specific wall-hit puff, so
            // without this the arrow vanishes silently on hitting a tile (reads as "no kill animation" / looks
            // like it phased through). Emit a small frost burst matching the Frostburn theme — same idea as
            // InvaderCrossbowBolt.OnKill.
            for (int i = 0; i < 6; i++)
            {
                Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height,
                                               DustID.Frost, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f);
                dust.noGravity = true;
                dust.scale *= 1.1f;
            }
        }
    }
}
