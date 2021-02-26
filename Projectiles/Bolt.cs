using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    public class Bolt : ModProjectile {
        public override void SetDefaults() {

            projectile.aiStyle = 1;
            projectile.friendly = true;
            projectile.height = 10;
            projectile.penetrate = 2;
            projectile.damage = 500;
            projectile.ranged = true;
            projectile.scale = (float)1;
            projectile.tileCollide = true;
            projectile.width = 5;
            aiType = ProjectileID.WoodenArrowFriendly;
            projectile.aiStyle = 1;
        }

        public override void Kill(int timeLeft) {
            Collision.HitTiles(projectile.position + projectile.velocity, projectile.velocity, projectile.width, projectile.height);
            Main.PlaySound(SoundID.Dig, projectile.position);
        }
    }

}
