using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Projectiles.Enemy
{
    class EnemySpellArmageddonBlast : ModProjectile
    {
        public override void SetDefaults()
        {
            projectile.width = 240;
            projectile.height = 240;
            Main.projFrames[projectile.type] = 8;
            projectile.hostile = true;
            projectile.penetrate = 50;
            projectile.scale = 2f;
            projectile.magic = true;
            projectile.light = 1;
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
            }
        }
        #endregion
    }
}