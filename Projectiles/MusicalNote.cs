using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria;

namespace tsorcRevamp.Projectiles {
    class MusicalNote : ModProjectile {
        public override void SetDefaults() {
            Projectile.aiStyle = 8;
            Projectile.friendly = true;
            Projectile.height = 10;
            Projectile.penetrate = 8;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = true;
            Projectile.width = 10;
        }
    }
}
