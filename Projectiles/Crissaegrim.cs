using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class Crissaegrim : ModProjectile {
        public override void SetDefaults() {
            projectile.width = 28;
			projectile.height = 28;
			projectile.aiStyle = 3;
			projectile.timeLeft = 3600;
			projectile.friendly = true;
			projectile.tileCollide = true;
			projectile.magic = true;
		}
    }
}
