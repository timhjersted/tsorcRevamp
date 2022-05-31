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
            Projectile.CloneDefaults(ProjectileID.ThrowingKnife);
            Projectile.damage = 20;
            Projectile.height = 8;
            Projectile.width = 8;
            Projectile.hostile = true;
            Projectile.friendly = false;
        }

        public override void Kill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, Projectile.position);
            // Dust spawn
            for (int i = 0; i < 5; i++)
            {
                int dustIndex = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 30, 0f, 0f, 100, default(Color), .8f);
                Main.dust[dustIndex].velocity *= .5f;
            }
        }
    }

}