using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Projectiles.Enemy
{
    class EarthTrident : ModProjectile
    {

        public override void SetDefaults()
        {
            projectile.aiStyle = 1;
            projectile.hostile = true;
            projectile.height = 16;
            projectile.light = 0.5f;
            projectile.ranged = true;
            projectile.scale = 0.8f;
            projectile.penetrate = 1;
            projectile.tileCollide = true;
            projectile.width = 16;
        }
        

        #region Kill
        public override bool PreKill(int timeleft)
        {
            projectile.type = 1;
            //int num98 = -1;
            if (!projectile.active)
            {
                return true;
            }
            projectile.timeLeft = 0;
            {
                Main.PlaySound(SoundID.Dig, (int)projectile.position.X, (int)projectile.position.Y, 1);
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
            return true;
        }
        #endregion
    }
}