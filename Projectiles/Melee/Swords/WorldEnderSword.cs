using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Melee.Swords
{
    public class WorldEnderSword : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 132;
            Projectile.height = 132;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 60;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(1);

            Dust.NewDustDirect(Projectile.position, 24, 24, DustID.SolarFlare).noGravity = true;
        }
        public override bool? CanDamage()
        {
            return false;
        }
    }
}
