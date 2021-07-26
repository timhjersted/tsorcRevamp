using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class HerosArrow : ModProjectile
    {
        public override void SetDefaults()
        {
            projectile.hostile = true;
            projectile.height = 14;
            projectile.penetrate = 2;
            projectile.ranged = true;
            projectile.scale = 0.8f;
            projectile.tileCollide = true;
            aiType = 1;
            projectile.width = 14;
        }

        public override bool PreKill(int timeLeft)
        {
            projectile.type = 1;
            return true;
        }

        #region AI
        public void Kill()
        {
            int num98 = -1;
            if (!projectile.active)
            {
                return;
            }
            projectile.timeLeft = 0;
            {
                Main.PlaySound(0, (int)projectile.position.X, (int)projectile.position.Y, 1);
                for (int i = 0; i < 10; i++)
                {
                    Vector2 arg_92_0 = new Vector2(projectile.position.X, projectile.position.Y);
                    int arg_92_1 = projectile.width;
                    int arg_92_2 = projectile.height;
                    int arg_92_3 = 7;
                    float arg_92_4 = 0f;
                    float arg_92_5 = 0f;
                    int arg_92_6 = 0;
                    Color newColor = default(Color);
                    Dust.NewDust(arg_92_0, arg_92_1, arg_92_2, arg_92_3, arg_92_4, arg_92_5, arg_92_6, newColor, 1f);
                }
            }
            projectile.active = false;
        }
        #endregion
    }
}