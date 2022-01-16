using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {
    class DragonsBreath : ModProjectile {

        public override void SetDefaults() {

            projectile.width = 6;
            projectile.height = 6;
            aiType = 1; //what's 85? ignores time left. trying 1
            projectile.aiStyle = 23;
            projectile.timeLeft = 3600; //3600 does't even matter
            projectile.friendly = false;
            projectile.hostile = true;
            projectile.penetrate = 3;
            projectile.tileCollide = true;
            projectile.magic = true;
        }
    }
}
