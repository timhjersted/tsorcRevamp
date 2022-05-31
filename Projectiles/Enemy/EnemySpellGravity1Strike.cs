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
            Projectile.width = 30;
            Projectile.height = 30;
            Main.projFrames[Projectile.type] = 4;
            drawOriginOffsetX = 20;
            drawOriginOffsetY = 20;
            Projectile.hostile = true;
            Projectile.penetrate = 50;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.light = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }
        int hitPlayer = 0;

        #region AI
        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 3)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= 4)
            {
                Projectile.Kill();
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

                Projectile.damage = (int)((0.75 * target.statLife) + defense);
                hitPlayer = 1;
            }            
        }
    }
}