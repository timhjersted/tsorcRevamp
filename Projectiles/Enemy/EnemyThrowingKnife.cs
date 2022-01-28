using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    public class EnemyThrowingKnife : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Sharpened Blade");
        }
        public override void SetDefaults()
        {

            projectile.aiStyle = 1; //2 makes it spin but has heavy gravity, 
            projectile.friendly = false;
            projectile.hostile = true;
            projectile.height = 8;
            projectile.penetrate = 2;
            projectile.ranged = true;
            projectile.scale = 0.8f;
            projectile.tileCollide = true;
            projectile.width = 8;
            //aiType = ProjectileID.WoodenArrowFriendly; //gives more gravity
        }

        public override void AI()
        {
            //if (projectile.soundDelay == 0 && Math.Abs(projectile.velocity.X) + Math.Abs(projectile.velocity.Y) > 2f)
            //{
            //	projectile.soundDelay = 10;
            //Main.PlaySound(2, (int)projectile.position.X, (int)projectile.position.Y, 17);
            //}

            //projectile.rotation += 1f;
            if (Main.rand.Next(5) == 0)
            {
                int dust = Dust.NewDust(new Vector2((float)projectile.position.X, (float)projectile.position.Y), projectile.width, projectile.height, 6, 0, 0, 50, Color.WhiteSmoke, 1.0f);
                Main.dust[dust].noGravity = false;
            }
            //Lighting.AddLight((int)(projectile.position.X / 16f), (int)(projectile.position.Y / 16f), 0.4f, 0.1f, 0.1f);

            //if (projectile.velocity.X <= 4 && projectile.velocity.Y <= 4 && projectile.velocity.X >= -4 && projectile.velocity.Y >= -4)
            //{
             //   float accel = 1f + (Main.rand.Next(10, 30) * 0.001f);
            //    projectile.velocity.X *= accel;
            //    projectile.velocity.Y *= accel;
            //}


        }
        #region PreKill
        public override bool PreKill(int timeLeft)
        {
            projectile.type = 0;
            Main.PlaySound(0, (int)projectile.position.X, (int)projectile.position.Y, 1);
            for (int i = 0; i < 10; i++)
            {
                Dust.NewDust(projectile.position, projectile.width, projectile.height, 1, 0, 0, 0, default, 1f);
            }
            return true;
        }
        #endregion

        #region Kill
        public void Kill()
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
