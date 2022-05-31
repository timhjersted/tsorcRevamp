using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class GigaDrill : ModProjectile {

        public override void SetDefaults() {
            Projectile.height = 22;
            Projectile.width = 22;
            Projectile.scale = 1.1f;
            Projectile.aiStyle = 20;
            Projectile.timeLeft = 3600;
            Projectile.hide = true;
            Projectile.ownerHitCheck = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 6;
        }
    }
}
