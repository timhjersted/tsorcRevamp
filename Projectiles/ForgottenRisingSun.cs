using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class ForgottenRisingSun : ModProjectile {
        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/ForgottenRisingSun";
        public override void SetDefaults() {
            Projectile.width = 25;
            Projectile.height = 25;
            Projectile.aiStyle = 3;
            Projectile.timeLeft = 2400;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.ownerHitCheck = true;
            Projectile.penetrate = 6;
        }
        public override void AI() {
            Projectile.rotation += Math.Sign(Projectile.velocity.X) * MathHelper.ToRadians(10f);
            if (Projectile.timeLeft < 2340f) {
                Projectile.tileCollide = false;
                Projectile.velocity = (Projectile.velocity + Projectile.DirectionTo(Main.player[Projectile.owner].Center)) * 0.98f;
                if (Main.player[Projectile.owner].Hitbox.Intersects(Projectile.Hitbox)) {
                    Projectile.Kill();
                }
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity) {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, (int)Projectile.position.X, (int)Projectile.position.Y, 9);
            if (Projectile.velocity.X != oldVelocity.X) {
                Projectile.velocity.X = 0f - oldVelocity.X;
            }
            if (Projectile.velocity.Y != oldVelocity.Y) {
                Projectile.velocity.Y = 0f - oldVelocity.Y;
            }

            return false;
        }
    }
}
