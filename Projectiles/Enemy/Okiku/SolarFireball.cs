using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Okiku {
    public class SolarFireball : ModProjectile {

        public override void SetStaticDefaults()
        {
            //DisplayName.SetDefault("Wave Attack");
        }
        public override string Texture => "Terraria/Projectile_" + ProjectileID.DD2BetsyFireball;
        public override void SetDefaults() {
            projectile.aiStyle = 686;
            projectile.CloneDefaults(ProjectileID.DD2BetsyFireball);
            projectile.tileCollide = false;
        }        
    }
}
