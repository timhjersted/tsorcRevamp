using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Swords {
    public class DrakarthusDagger2 : ModProjectile {
        public override void SetDefaults() {
            Projectile.CloneDefaults(ModContent.ProjectileType<DrakarthusDagger>());
            Projectile.penetrate = 1;
            Projectile.timeLeft = 240;
        }

        public override void AI() {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(45);

            UsefulFunctions.HomeOnEnemy(Projectile, 240, 24);
            Dust.NewDustDirect(Projectile.position, 24, 24, DustID.SolarFlare).noGravity = true;
        }

    }
}
