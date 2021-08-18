using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class EnemySpellAbyssStorm : ModProjectile
    {
		public override void SetStaticDefaults()
		{
            DisplayName.SetDefault("Dark Wave Storm");
		}

		public override void SetDefaults()
        {
            projectile.width = 194;
            projectile.height = 194;
            drawOriginOffsetX = -96;
            drawOriginOffsetY = 94;
            Main.projFrames[projectile.type] = 7;
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
            if (projectile.frame >= 7)
            {
                projectile.Kill();
            }
        }
        #endregion

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            if (Main.expertMode)
            {
                target.AddBuff(BuffID.OnFire, 450, false);
            }
            else
            {
                target.AddBuff(BuffID.OnFire, 900, false);
            }
        }
    }
}