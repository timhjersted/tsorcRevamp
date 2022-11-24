using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Magic
{
    class CelestialShard : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.penetrate = 1;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.light = 0.3f;
            Projectile.knockBack = 0f;
        }
        public override void AI()
        {
            Projectile.rotation += 0.05f;            
        }
    }

}
