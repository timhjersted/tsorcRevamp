using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Projectiles.Enemy.DarkCloud
{
    class EnemyArrowOfDarkCloud : ModProjectile
    {
        public override void SetDefaults()
        {
            projectile.width = 10;
            projectile.height = 5;
            projectile.hostile = true;
            projectile.penetrate = 2;
            projectile.ranged = true;
            projectile.tileCollide = false;
        }
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Arrow of Dark Cloud");
        }

        
        public override void AI()
        {
            //Secret forbidden color unlocked: Green 3
            Lighting.AddLight(projectile.Center, Color.SeaGreen.ToVector3() * 3);
            projectile.velocity.Y += 0.05f;
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        static Texture2D texture;
        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            UsefulFunctions.DrawSimpleLitProjectile(spriteBatch, projectile, ref texture);
            return false;
        }

        #region Kill
        public override void Kill(int timeLeft)
        {
            //int num98 = -1;
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