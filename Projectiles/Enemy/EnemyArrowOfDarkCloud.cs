using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Projectiles.Enemy
{
    class EnemyArrowOfDarkCloud : ModProjectile
    {
        public override void SetDefaults()
        {
            projectile.aiStyle = 1;
            projectile.width = 10;
            projectile.height = 5;
            projectile.hostile = true;
            projectile.penetrate = 2;
            projectile.ranged = true;
            projectile.tileCollide = true;
        }
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Arrow of Dark Cloud");
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

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            Main.player[Main.myPlayer].AddBuff(ModContent.BuffType<Buffs.Crippled>(), 300, false); //jumping and flying is crippled
           
            if (Main.rand.Next(10) == 0)
            {
                Main.player[Main.myPlayer].AddBuff(ModContent.BuffType<Buffs.Crippled>(), 1200, false); //jumping and flying is crippled
            }
        }
    }
}