using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class EnemySpellTornado : ModProjectile
    {
        public override void SetDefaults()
        {
            projectile.width = 250;
            projectile.height = 172;
            Main.projFrames[projectile.type] = 12;
            drawOriginOffsetX = 15;
            drawOriginOffsetY = 10;
            projectile.aiStyle = 5;
            projectile.hostile = true;
            projectile.penetrate = 50;
            projectile.scale = 2.6f;
            projectile.magic = true;
            projectile.light = 1f;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
        }
        int spinPlayer = 0;

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

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            if ((Main.player[Main.myPlayer].velocity.X > 1) || (spinPlayer < 2))
            {
                Main.player[Main.myPlayer].velocity.X = -8;
                spinPlayer++;
            }
            else
            {
                Main.player[Main.myPlayer].velocity.X = 8;
                spinPlayer = 0;
            }
        }
    }
}