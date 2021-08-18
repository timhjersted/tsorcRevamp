using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Projectiles.Enemy
{
    class EnemyDragoonLance : ModProjectile
    {
        public override void SetDefaults()
        {
            projectile.width = 19;
            projectile.height = 161;
            projectile.aiStyle = 19;
            projectile.timeLeft = 700;
            projectile.penetrate = 12;
            projectile.hostile = true;
            projectile.tileCollide = true;
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("DragoonLance");
        }

        #region Kill
        public override void Kill(int timeLeft)
        {
            if (!projectile.active)
            {
                int num40 = Dust.NewDust(new Vector2(projectile.position.X + projectile.velocity.X, projectile.position.Y + projectile.velocity.Y), projectile.width, projectile.height, 15, projectile.velocity.X, projectile.velocity.Y, 100, default(Color), 3f);
                return;
            }
            projectile.timeLeft = 0;
            {
                Main.PlaySound(0, (int)projectile.position.X, (int)projectile.position.Y, 1);
                for (int i = 0; i < 10; i++)
                {
                    int dust = Dust.NewDust(new Vector2(projectile.position.X + projectile.velocity.X, projectile.position.Y + projectile.velocity.Y), projectile.width, projectile.height, 15, projectile.velocity.X, projectile.velocity.Y, 200, default(Color), 1f);
                    Main.dust[dust].noGravity = false;
                    Dust.NewDust(new Vector2(projectile.position.X + projectile.velocity.X, projectile.position.Y + projectile.velocity.Y), projectile.width, projectile.height, 15, projectile.velocity.X, projectile.velocity.Y, 200, default(Color), 2f);
                    Dust.NewDust(new Vector2(projectile.position.X + projectile.velocity.X, projectile.position.Y + projectile.velocity.Y), projectile.width, projectile.height, 15, projectile.velocity.X, projectile.velocity.Y, 200, default(Color), 1f);
                    Dust.NewDust(new Vector2(projectile.position.X + projectile.velocity.X, projectile.position.Y + projectile.velocity.Y), projectile.width, projectile.width, 15, projectile.velocity.X, projectile.velocity.Y, 200, default(Color), 1f);
                    Dust.NewDust(new Vector2(projectile.position.X + projectile.velocity.X, projectile.position.Y + projectile.velocity.Y), projectile.width, projectile.width, 15, projectile.velocity.X, projectile.velocity.Y, 200, default(Color), 1f);
                    Dust.NewDust(new Vector2(projectile.position.X + projectile.velocity.X, projectile.position.Y + projectile.velocity.Y), projectile.width, projectile.height, 15, projectile.velocity.X, projectile.velocity.Y, 200, default(Color), 2f);
                    Dust.NewDust(new Vector2(projectile.position.X + projectile.velocity.X, projectile.position.Y + projectile.velocity.Y), projectile.width, projectile.height, 15, projectile.velocity.X, projectile.velocity.Y, 200, default(Color), 1f);
                    Dust.NewDust(new Vector2(projectile.position.X + projectile.velocity.X, projectile.position.Y + projectile.velocity.Y), projectile.width, projectile.width, 15, projectile.velocity.X, projectile.velocity.Y, 200, default(Color), 1f);
                    Dust.NewDust(new Vector2(projectile.position.X + projectile.velocity.X, projectile.position.Y + projectile.velocity.Y), projectile.width, projectile.width, 15, projectile.velocity.X, projectile.velocity.Y, 200, default(Color), 1f);
                }
            }
            projectile.active = false;
        }
        #endregion
    }
}