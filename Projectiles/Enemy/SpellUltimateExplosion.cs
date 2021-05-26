using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    public class SpellUltimateExplosion : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Spell Ultimate Explosion");
            Main.projFrames[projectile.type] = 9;
        }
        public override void SetDefaults()
        {

            projectile.aiStyle = 1;
            projectile.friendly = true;
            projectile.width = 300;
            projectile.height = 300;
            projectile.light = 1f;
            projectile.penetrate = 50;
            projectile.magic = true;
            projectile.scale = 2f;
            projectile.ignoreWater = true;
            projectile.tileCollide = true;
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
            if (projectile.frame >= 9)
            {
                projectile.Kill();
                return;
            }
        }
        #endregion


    }
}