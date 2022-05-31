using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class ThrowingSpear : ModProjectile {
        public override string Texture => "tsorcRevamp/Items/Weapons/Ranged/ThrowingSpear";
        public override void SetDefaults() {
            Projectile.friendly = true;
            Projectile.height = 14;
            Projectile.width = 14;
            Projectile.scale = 0.8f;
            Projectile.penetrate = 1;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 720;
        }

        public override void AI() {
            Projectile.ai[0] += 1f;
            if (Projectile.ai[0] >= 15f) {
                Projectile.ai[0] = 15f;
                Projectile.velocity.Y += 0.1f;
            }
            if (Projectile.velocity.Y > 16f) {
                Projectile.velocity.Y = 16f;
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(90f); //simplified rotation code (no trig!)
        }
        public override void Kill(int timeLeft) {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, (int)Projectile.position.X, (int)Projectile.position.Y, 1);
            for (int i = 0; i < 10; i++) {
                Vector2 projPosition = new Vector2(Projectile.position.X, Projectile.position.Y);
                Dust.NewDust(projPosition, Projectile.width, Projectile.height, 7, 0f, 0f, 0, default, 1f);
            }
        }
    }
}
