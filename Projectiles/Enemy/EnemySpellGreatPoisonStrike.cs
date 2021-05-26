using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    public class EnemySpellGreatPoisonStrike : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Enemy Spell Great Poison Strike");
            Main.projFrames[projectile.type] = 5;
        }
        public override void SetDefaults()
        {

            projectile.aiStyle = 4;
            projectile.friendly = false;
            projectile.hostile = true;
            projectile.width = 30;
            projectile.height = 30;
            projectile.light = 1f;
            projectile.penetrate = 50;
            projectile.magic = true;
            projectile.scale = 1f;
            projectile.tileCollide = true;
            drawOriginOffsetY = -6;
            drawOriginOffsetX = -6;
            drawOffsetX = -6;
        }

        #region AI
        public override void AI()
        {
            projectile.rotation = projectile.velocity.ToRotation();

            projectile.frameCounter++;
            if (projectile.frameCounter > 3)
            {
                projectile.frame++;
                projectile.frameCounter = 0;
            }
            if (projectile.frame >= 5)
            {
                projectile.Kill();
                return;
            }
        }
        #endregion
    }
}