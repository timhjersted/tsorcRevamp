using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria;

namespace tsorcRevamp.Projectiles {
    class MusicalNote : ModProjectile {
        public override void SetDefaults() {
            projectile.aiStyle = 8;
            projectile.friendly = true;
            projectile.height = 10;
            projectile.penetrate = 8;
            projectile.magic = true;
            projectile.tileCollide = true;
            projectile.width = 10;
        }
    }
}
