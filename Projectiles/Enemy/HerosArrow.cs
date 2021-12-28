using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class HerosArrow : ModProjectile
    {
        public override void SetDefaults()
        {
            projectile.hostile = true;
            projectile.height = 14;
            projectile.penetrate = 2;
            projectile.ranged = true;
            projectile.scale = 0.8f;
            projectile.tileCollide = true;
            aiType = 1;
            projectile.width = 14;
        }

        public override bool PreKill(int timeLeft)
        {
            projectile.type = 0;
            Main.PlaySound(0, (int)projectile.position.X, (int)projectile.position.Y, 1);
            for (int i = 0; i < 10; i++)
            {
                Dust.NewDust(projectile.position, projectile.width, projectile.height, 7, 0, 0, 0, default, 1f);
            }
            return true;
        }

        public override void AI()
        {
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }       
    }
}