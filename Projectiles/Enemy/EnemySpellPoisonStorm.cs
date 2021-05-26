using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    public class EnemySpellPoisonStorm : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Enemy Spell Poison Storm");
            Main.projFrames[projectile.type] = 7;
        }
        public override void SetDefaults()
        {

            projectile.aiStyle = -1;
            projectile.friendly = false;
            projectile.hostile = true;
            projectile.width = 190;
            projectile.height = 190;
            projectile.light = 1f;
            projectile.penetrate = 1;
            projectile.magic = true;
            projectile.scale = 2f;
            projectile.tileCollide = true;
            drawOriginOffsetY = 95;
            drawOriginOffsetX = -95;
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
            if (projectile.frame >= 7)
            {
                projectile.Kill();
                return;
            }
            Rectangle projrec = new Rectangle((int)projectile.position.X + (int)projectile.velocity.X, (int)projectile.position.Y + (int)projectile.velocity.Y,
            projectile.width, projectile.height);
            Rectangle prec = new Rectangle((int)Main.player[Main.myPlayer].position.X, (int)Main.player[Main.myPlayer].position.Y, (int)Main.player
            [Main.myPlayer].width, (int)Main.player[Main.myPlayer].height);
            if (projrec.Intersects(prec))
            {
                Main.player[Main.myPlayer].AddBuff(24, 900, false);
            }
        }
        #endregion
    }
}