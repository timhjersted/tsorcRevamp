using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System;
using Microsoft.Xna.Framework;


namespace tsorcRevamp.Projectiles
{
    public class BoneHostile : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.Bone);
            Projectile.damage = 40;
            Projectile.height = 12;
            Projectile.width = 12;
            Projectile.hostile = true;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 120;
            Projectile.scale = .9f;
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.penetrate--;
            if (Projectile.penetrate <= 0)
            {
                Projectile.Kill();
            }
                // This code makes the projectile bouncy.
                if (Projectile.velocity.X != oldVelocity.X && Math.Abs(oldVelocity.X) > 1f)
            {
                Projectile.velocity.X = oldVelocity.X * -0.5f;
            }
            if (Projectile.velocity.Y != oldVelocity.Y && Math.Abs(oldVelocity.Y) > 1f)
            {
                Projectile.velocity.Y = oldVelocity.Y * -0.5f;
            }
            return false;
        }
        public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.Dig, Projectile.position);
            // Smoke Dust spawn
            for (int i = 0; i < 5; i++)
            {
                int dustIndex = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 30, 0f, 0f, 100, default(Color), .8f);
                Main.dust[dustIndex].velocity *= .5f;
                // Main.dust[dustIndex].noGravity = true;
            }
        }
    }

}
