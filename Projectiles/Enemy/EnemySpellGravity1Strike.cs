using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Projectiles.Enemy
{
    class EnemySpellGravity1Strike : ModProjectile
    {
        public override void SetDefaults()
        {
            projectile.width = 30;
            projectile.height = 30;
            Main.projFrames[projectile.type] = 4;
            drawOriginOffsetX = 20;
            drawOriginOffsetY = 20;
            projectile.hostile = true;
            projectile.penetrate = 50;
            projectile.magic = true;
            projectile.light = 1;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
        }
        int hitPlayer = 0;

        #region AI
        public override void AI()
        {
            projectile.frameCounter++;
            if (projectile.frameCounter > 3)
            {
                projectile.frame++;
                projectile.frameCounter = 0;
            }
            if (projectile.frame >= 4)
            {
                projectile.Kill();
                return;
            }           
        }
        #endregion

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            if (hitPlayer <= 0)
            {
                int defense;
                if (Main.expertMode)
                {
                    defense = (int)(target.statDefense * .75);
                }
                else
                {
                    defense = (int)(target.statDefense * .5);
                }

                projectile.damage = (int)((0.75 * target.statLife) + defense);
                hitPlayer = 1;
            }            
        }
    }
}