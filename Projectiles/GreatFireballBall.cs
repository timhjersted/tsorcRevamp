using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class GreatFireballBall : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = true;
            Projectile.height = 16;
            Projectile.width = 16;
            Projectile.penetrate = 1;
        }

        public override void AI()
        {
            int thisDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 6, 0, 0, 100, default, 2f);
            Main.dust[thisDust].noGravity = true;

            Projectile.rotation += 0.25f;
        }

        public override void OnKill(int timeLeft)
        {

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item10, Projectile.Center);
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center.X, Projectile.Center.Y, 0, 0, ModContent.ProjectileType<GreatFireball>(), Projectile.damage, 6f, Projectile.owner);
            }

            for (int i = 0; i < 5; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(12, 12);
                int thisDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 6, vel.X, vel.Y, 100, default, 2f);
                Main.dust[thisDust].noGravity = true;
            }
        }
    }
}
