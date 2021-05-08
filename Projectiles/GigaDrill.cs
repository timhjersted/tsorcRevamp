using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class GigaDrill : ModProjectile {

        public override void SetDefaults() {
            projectile.height = 22;
            projectile.width = 22;
            projectile.scale = 1.1f;
            projectile.aiStyle = 20;
            projectile.timeLeft = 3600;
            projectile.hide = true;
            projectile.ownerHitCheck = true;
            projectile.tileCollide = false;
            projectile.melee = true;
            projectile.penetrate = 6;
        }
    }
}
