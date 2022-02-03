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
            projectile.aiStyle = 1;
            projectile.hostile = true;
            projectile.height = 16;
            projectile.light = 0.5f;
            projectile.ranged = true;
            projectile.scale = 0.8f;
            projectile.penetrate = 1;
            projectile.tileCollide = true;
            projectile.width = 16;
        }
        

        public override bool PreKill(int timeleft)
        {
            projectile.type = ProjectileID.WoodenArrowHostile;

            Main.PlaySound(SoundID.Dig, (int)projectile.position.X, (int)projectile.position.Y, 1);
            for (int i = 0; i < 10; i++)
            {
                Dust.NewDust(projectile.position, projectile.width, projectile.height, 7, 0, 0, 0, default, 1f);
            }
            return true;
        }
    }
}