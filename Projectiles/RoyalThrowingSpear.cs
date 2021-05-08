using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace tsorcRevamp.Projectiles {
    class RoyalThrowingSpear : ModProjectile {

        public override void SetDefaults() {
            projectile.aiStyle = 1;
            projectile.friendly = true;
            projectile.height = 14;
            projectile.penetrate = 1;
            projectile.ranged = true;
            projectile.scale = 0.9f;
            projectile.tileCollide = true;
            projectile.width = 14;
        }

        public override void Kill(int timeLeft) {
            Main.PlaySound(SoundID.Dig, (int)projectile.position.X, (int)projectile.position.Y, 1);
            for (int i = 0; i < 10; i++) {
                Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 7, 0f, 0f, 0, default, 1f);
            }
        }
    }
}
