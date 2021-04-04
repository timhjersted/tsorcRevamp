using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System;
using Microsoft.Xna.Framework;


namespace tsorcRevamp.Projectiles
{
    public class ThrowingKnifeHostile : ModProjectile
    {
        public override void SetDefaults()
        {
            projectile.CloneDefaults(ProjectileID.ThrowingKnife);
            projectile.damage = 20;
            projectile.height = 8;
            projectile.width = 8;
            projectile.hostile = true;
            projectile.friendly = false;
        }

        public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.Dig, projectile.position);
            // Dust spawn
            for (int i = 0; i < 5; i++)
            {
                int dustIndex = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 30, 0f, 0f, 100, default(Color), .8f);
                Main.dust[dustIndex].velocity *= .5f;
            }
        }
    }

}