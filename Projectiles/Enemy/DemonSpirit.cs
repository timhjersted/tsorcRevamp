using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.Achievements;

namespace tsorcRevamp.Projectiles.Enemy
{
    class DemonSpirit : ModProjectile
    {
        public override void SetDefaults()
        {
            projectile.width = 38;
            projectile.height = 46;
            projectile.aiStyle = 0;
            projectile.timeLeft = 120;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.damage = 20;
            projectile.friendly = false;
            projectile.penetrate = 3;
            projectile.light = .5f;
            }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Purple Crush");
        }

        public override bool PreKill(int timeLeft)
        {
            projectile.type = 44; //killpretendtype
            return true;
        }

        Player targetPlayer;

        public override void AI()
        {

            Color color = new Color();
            int dust = Dust.NewDust(new Vector2((float)projectile.position.X, (float)projectile.position.Y - 10), projectile.width, projectile.height, DustID.Shadowflame, 0, 0, 100, color, 1.0f);
            Main.dust[dust].noGravity = true;

            if (this.projectile.timeLeft <= 120)
            {
                this.projectile.scale = 0.7f;
                this.projectile.damage = 15;
            }
            if (this.projectile.timeLeft <= 110)
            {
                this.projectile.scale = 0.8f;
                this.projectile.damage = 20;
            }
            if (this.projectile.timeLeft <= 90)
            {
                this.projectile.scale = 0.9f;
                this.projectile.damage = 22;
            }
            if (this.projectile.timeLeft <= 70)
            {
                this.projectile.scale = 1f;
                this.projectile.damage = 25;
            }
            if (this.projectile.timeLeft <= 50)
            {
                this.projectile.scale = 1.2f;
                this.projectile.damage = 30;
            }
            if (this.projectile.timeLeft <= 30)
            {
                this.projectile.scale = 1.4f;
                this.projectile.damage = 40;
            }

            this.projectile.ai[0] += 1f;


            projectile.rotation = (float)Math.Atan2(projectile.velocity.Y, projectile.velocity.X) + 1.57f;
            if (projectile.velocity.Y > 16f)
            {
                projectile.velocity.Y = 16f;
                return;
            }

            #region Homing Code
            Vector2 move = Vector2.Zero;
            float distance = 900f;
            bool target = false;
            float speed = 3;
            if (!target)
            {
                int targetIndex = GetClosestPlayer();
                if(targetIndex != -1)
                {
                    targetPlayer = Main.player[targetIndex];
                    target = true;
                }
            }
                
            if (target)
            {
                Vector2 newMove = targetPlayer.Center - projectile.Center;
                float distanceTo = (float)Math.Sqrt(newMove.X * newMove.X + newMove.Y * newMove.Y);
                if (distanceTo < distance)
                {
                    move = newMove;
                    distance = distanceTo;
                }

                projectile.velocity.X = (move.X / distance) * speed;
                projectile.velocity.Y = (move.Y / distance) * speed;
            }
            #endregion
        }

        private int GetClosestPlayer()
        {
            int closest = -1;
            float distance = 9999;
            for (int i = 0; i < Main.player.Length; i++)
            {
                Vector2 diff = Main.player[i].Center - projectile.Center;
                float distanceTo = (float)Math.Sqrt(diff.X * diff.X + diff.Y * diff.Y);
                if(distanceTo < distance)
                {
                    distance = distanceTo;
                    closest = i;
                }
            }
            return closest;
        }
    }
}