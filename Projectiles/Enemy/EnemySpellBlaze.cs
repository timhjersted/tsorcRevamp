using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class EnemySpellBlaze : ModProjectile
    {
        public override void SetDefaults()
        {
            Main.projFrames[projectile.type] = 5;
            drawOriginOffsetX = 15;
            drawOriginOffsetY = 10;
            projectile.width = 86;
            projectile.height = 66;
            projectile.aiStyle = 4;
            projectile.hostile = true;
            projectile.penetrate = 16;
            projectile.magic = true;
            projectile.light = 1f;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 100;
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
            if (projectile.frame >= 5)
            {
                projectile.frame = 0;
                return;
            }
            if (projectile.ai[0] == 0f)
            {
                projectile.alpha -= 50;
                if (projectile.alpha <= 0)
                {
                    projectile.alpha = 0;
                    projectile.ai[0] = 1f;
                    if (projectile.ai[1] == 0f)
                    {
                        projectile.ai[1] += 1f;
                        projectile.position += projectile.velocity * 1f;
                    }
                }
            }
            if (projectile.timeLeft > 60)
            {
                projectile.timeLeft = 60;
            }
            projectile.rotation += 0.3f * (float)projectile.direction;
            return;
        }
        #endregion

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            int expertScaling = 1;
            if (Main.expertMode) expertScaling = 2;
            target.AddBuff(BuffID.OnFire, 900 / expertScaling, false);
        }
    }
}