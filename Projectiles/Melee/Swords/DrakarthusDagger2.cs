using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Melee.Swords {
    public class DrakarthusDagger2 : ModProjectile 
    {
        public override void SetDefaults() 
        {
            Projectile.CloneDefaults(ModContent.ProjectileType<DrakarthusDagger>());
            Projectile.penetrate = 1;
            Projectile.timeLeft = 240;
        }

        public override void AI() 
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(45);

            UsefulFunctions.HomeOnEnemy(Projectile, 240, 24);
            Dust.NewDustDirect(Projectile.position, 24, 24, DustID.SolarFlare).noGravity = true;
        }

    }
}
