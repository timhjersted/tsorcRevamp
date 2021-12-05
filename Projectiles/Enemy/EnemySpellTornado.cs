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
            projectile.width = 100;
            projectile.height = 280;
            Main.projFrames[projectile.type] = 12;
            drawOriginOffsetX = 160;
            drawOriginOffsetY = 10;
            projectile.hostile = true;
            projectile.penetrate = 50;
            projectile.scale = 1.3f;
            projectile.magic = true;
            projectile.light = 1f;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 1200;
        }
        int spinPlayer = 0;
        int spinCooldown = 0;
        static float maxSpeed = 7;
        Vector2 maxClamp = new Vector2(maxSpeed, maxSpeed);

        #region AI
        public override void AI()
        {
            projectile.rotation = 0;
            spinCooldown--;
            maxSpeed = 7;
            if (Main.player[(int)projectile.ai[0]] != null || Main.player[(int)projectile.ai[0]].active)
            {
                projectile.velocity += UsefulFunctions.GenerateTargetingVector(projectile.Center, Main.player[(int)projectile.ai[0]].Center, 0.3f);
                if(projectile.velocity.X > maxSpeed)
                {
                    projectile.velocity.X = maxSpeed;
                }
                if (projectile.velocity.X < -maxSpeed)
                {
                    projectile.velocity.X = -maxSpeed;
                }
                if (projectile.velocity.Y > maxSpeed)
                {
                    projectile.velocity.Y = maxSpeed;
                }
                if (projectile.velocity.Y < -maxSpeed)
                {
                    projectile.velocity.Y = -maxSpeed;
                }
            }


            Dust.NewDust(projectile.position, projectile.width, projectile.height, 31, -projectile.velocity.X, -projectile.velocity.Y);
            if (!Main.dedServ)
            {
                projectile.frameCounter++;
                if (projectile.frameCounter > 3)
                {
                    projectile.frame++;
                    projectile.frameCounter = 0;
                }
                if(projectile.frame >= 12)
                {
                    projectile.frame = 0;
                }
            }
        }
        #endregion

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            if (spinCooldown <= 0)
            {
                if ((target.velocity.X > 1) || (spinPlayer < 1))
                {
                    target.velocity.X = -8;
                    spinPlayer++;
                    spinCooldown = 10;
                }
                else
                {
                    target.velocity.X = 8;
                    spinPlayer = 0;
                    spinCooldown = 10;
                }
            }
        }
    }
}