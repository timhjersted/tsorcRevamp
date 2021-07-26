using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    public class EnemySpellSuddenDeathStrike : ModProjectile
    {
        public override void SetDefaults()
        {
            projectile.width = 44;
            projectile.height = 40;
            Main.projFrames[projectile.type] = 12;
            drawOriginOffsetX = 15;
            drawOriginOffsetY = 10;
            projectile.hostile = true;
            projectile.penetrate = 50;
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
            if (projectile.frame >= 12)
            {
                projectile.Kill();
                return;
            }
        }
        #endregion
    }
}