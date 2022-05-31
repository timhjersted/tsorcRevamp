using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class Crissaegrim : ModProjectile {
		public override string Texture => "tsorcRevamp/Items/Weapons/Ranged/Crissaegrim";
		public override void SetDefaults() {
            Projectile.width = 28;
			Projectile.height = 28;
			Projectile.aiStyle = 3;
			Projectile.timeLeft = 3600;
			Projectile.friendly = true;
			Projectile.tileCollide = true;
			Projectile.DamageType = DamageClass.Magic;
		}
    }
}
