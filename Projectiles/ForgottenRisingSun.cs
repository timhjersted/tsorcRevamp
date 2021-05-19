using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class ForgottenRisingSun : ModProjectile {
        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/ForgottenRisingSun";
        public override void SetDefaults() {
            projectile.width = 25;
            projectile.height = 25;
            projectile.aiStyle = 3;
            projectile.timeLeft = 2400;
            projectile.friendly = true;
            projectile.melee = true;
            projectile.ownerHitCheck = true;
            projectile.penetrate = 6;
        }
        public override void AI() {
            projectile.rotation += Math.Sign(projectile.velocity.X) * MathHelper.ToRadians(10f);
            if (projectile.timeLeft < 2340f) {
                projectile.tileCollide = false;
                projectile.velocity = (projectile.velocity + projectile.DirectionTo(Main.player[projectile.owner].Center)) * 0.98f;
                if (Main.player[projectile.owner].Hitbox.Intersects(projectile.Hitbox)) {
                    projectile.Kill();
                }
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity) {
            Main.PlaySound(SoundID.Dig, (int)projectile.position.X, (int)projectile.position.Y, 9);
            if (projectile.velocity.X != oldVelocity.X) {
                projectile.velocity.X = 0f - oldVelocity.X;
            }
            if (projectile.velocity.Y != oldVelocity.Y) {
                projectile.velocity.Y = 0f - oldVelocity.Y;
            }

            return false;
        }
    }
}
