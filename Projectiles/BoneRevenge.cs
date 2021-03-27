using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System;
using Microsoft.Xna.Framework;


namespace tsorcRevamp.Projectiles
{
    public class BoneRevenge : ModProjectile
    {
        public override void SetDefaults()
        {
            projectile.CloneDefaults(ProjectileID.Bone);
            projectile.damage = 35;
            projectile.height = 12;
            projectile.width = 12;
            projectile.friendly = true;
            projectile.penetrate = 2;
            projectile.timeLeft = 120;
            projectile.scale = .9f;
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            projectile.penetrate--;
            if (projectile.penetrate <= 0)
            {
                projectile.Kill();
            }
            // This code makes the projectile bouncy.
            if (projectile.velocity.X != oldVelocity.X && Math.Abs(oldVelocity.X) > 1f)
            {
                projectile.velocity.X = oldVelocity.X * -0.5f;
            }
            if (projectile.velocity.Y != oldVelocity.Y && Math.Abs(oldVelocity.Y) > 1f)
            {
                projectile.velocity.Y = oldVelocity.Y * -0.5f;
            }
            return false;
        }
        public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.Dig, projectile.position);
            // Smoke Dust spawn
            for (int i = 0; i < 5; i++)
            {
                int dustIndex = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 30, 0f, 0f, 100, default(Color), .8f);
                Main.dust[dustIndex].velocity *= .5f;
                // Main.dust[dustIndex].noGravity = true;
            }
        }
    }

}
