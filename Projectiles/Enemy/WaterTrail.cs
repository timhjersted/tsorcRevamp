using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {
    class WaterTrail : ModProjectile {

        public override void SetDefaults() {
            projectile.penetrate = 4;
            projectile.width = 16;
            projectile.height = 16;
            projectile.ignoreWater = true;
            projectile.tileCollide = true;
            projectile.hostile = true;
        }
        public override void AI() {
            projectile.rotation += 4f;
            if (Main.rand.Next(4) == 0) 
            {
                int dust = Dust.NewDust(new Vector2((float)projectile.position.X, (float)projectile.position.Y), projectile.width, projectile.height, 29, 0, 0, 50, Color.Blue, 2.0f);
                Main.dust[dust].noGravity = false;
            }
            Lighting.AddLight((int)(projectile.position.X / 16f), (int)(projectile.position.Y / 16f), 0.4f, 0.1f, 0.1f);

            if (projectile.velocity.X <= 4 && projectile.velocity.Y <= 4 && projectile.velocity.X >= -4 && projectile.velocity.Y >= -4) {
                float accel = 2f + (Main.rand.Next(10, 30) * 0.5f);
                projectile.velocity.X *= accel;
                projectile.velocity.Y *= accel;
            }
        }
        public override bool OnTileCollide(Vector2 oldVelocity) { //allow the projectile to bounce
            projectile.penetrate--;
            if (projectile.penetrate == 0) {
                projectile.Kill();
            }
            Collision.HitTiles(projectile.position + projectile.velocity, projectile.velocity, projectile.width, projectile.height);
            Main.PlaySound(SoundID.Item10, projectile.position);
            if (projectile.velocity.X != oldVelocity.X) {
                projectile.velocity.X = -oldVelocity.X;
            }
            if (projectile.velocity.Y != oldVelocity.Y) {
                projectile.velocity.Y = -oldVelocity.Y;
            }
            return false;
        }
    }
}
