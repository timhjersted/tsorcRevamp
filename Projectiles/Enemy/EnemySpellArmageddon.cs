using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class EnemySpellArmageddon : ModProjectile
    {
        public override void SetDefaults()
        {
            projectile.width = 240;
            projectile.height = 240;
            Main.projFrames[projectile.type] = 8;
            projectile.hostile = true;
            projectile.penetrate = 50;
            projectile.scale = 2;
            projectile.magic = true;
            projectile.light = 1;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
        }

        #region AI
        public override void AI()
        {
            projectile.frameCounter++;
            if (projectile.frameCounter > 3)
            {
                projectile.frame++;
                projectile.frameCounter = 0;
            }
            if (projectile.frame >= 8)
            {
                projectile.Kill();
                return;
            }
        }
        #endregion
    }
}