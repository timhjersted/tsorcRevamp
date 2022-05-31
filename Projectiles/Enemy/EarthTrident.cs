using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Projectiles.Enemy
{
    class EarthTrident : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.aiStyle = 1;
            Projectile.hostile = true;
            Projectile.height = 16;
            Projectile.light = 0.5f;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.scale = 0.8f;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.width = 16;
        }
        

        public override bool PreKill(int timeleft)
        {
            Projectile.type = ProjectileID.WoodenArrowHostile;

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, (int)Projectile.position.X, (int)Projectile.position.Y, 1);
            for (int i = 0; i < 10; i++)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 7, 0, 0, 0, default, 1f);
            }
            return true;
        }
    }
}