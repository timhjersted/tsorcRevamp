using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class EnemySpellBlazeBall : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dark Wave Storm");
        }

        public override void SetDefaults()
        {
            projectile.aiStyle = 1;
            projectile.width = 16;
            projectile.height = 16;
            //projectile.noGravity = true;
            projectile.hostile = true;
            projectile.light = 1;
            projectile.magic = true;
            projectile.penetrate = 1;
            projectile.tileCollide = false;
        }

       
        #region Kill
        public override void Kill(int timeLeft)
        {
            if (!projectile.active)
            {
                return;
            }
            projectile.timeLeft = 0;
            {
                Main.PlaySound(2, (int)projectile.position.X, (int)projectile.position.Y, 10);
                if (Main.rand.Next(5) == 1)
                {
                    if (projectile.owner == Main.myPlayer) Projectile.NewProjectile(projectile.position.X + (float)(projectile.width * (Main.rand.Next(50))), projectile.position.Y + (float)(projectile.height * (Main.rand.Next(60))), ((Main.rand.Next(30)) * -1), ((Main.rand.Next(30)) * -1), ModContent.ProjectileType<Projectiles.Enemy.EnemySpellBlaze>(), projectile.damage, 5f, projectile.owner);
                    if (projectile.owner == Main.myPlayer) Projectile.NewProjectile(projectile.position.X + (float)(projectile.width * (Main.rand.Next(50))), projectile.position.Y + (float)(projectile.height * (Main.rand.Next(60))), ((Main.rand.Next(30)) * -1), ((Main.rand.Next(30)) * -1), ModContent.ProjectileType<Projectiles.Enemy.EnemySpellBlaze>(), projectile.damage, 5f, projectile.owner);
                    if (projectile.owner == Main.myPlayer) Projectile.NewProjectile(projectile.position.X + (float)(projectile.width * (Main.rand.Next(50))), projectile.position.Y + (float)(projectile.height * (Main.rand.Next(60))), ((Main.rand.Next(30)) * -1), ((Main.rand.Next(30)) * -1), ModContent.ProjectileType<Projectiles.Enemy.EnemySpellBlaze>(), projectile.damage, 5f, projectile.owner);
                    if (projectile.owner == Main.myPlayer) Projectile.NewProjectile(projectile.position.X + (float)(projectile.width * (Main.rand.Next(50))), projectile.position.Y + (float)(projectile.height * (Main.rand.Next(60))), (Main.rand.Next(30)), (Main.rand.Next(30)), ModContent.ProjectileType<Projectiles.Enemy.EnemySpellBlaze>(), projectile.damage, 5f, projectile.owner);
                    if (projectile.owner == Main.myPlayer) Projectile.NewProjectile(projectile.position.X + (float)(projectile.width * (Main.rand.Next(50))), projectile.position.Y + (float)(projectile.height * (Main.rand.Next(60))), (Main.rand.Next(30)), (Main.rand.Next(30)), ModContent.ProjectileType<Projectiles.Enemy.EnemySpellBlaze>(), projectile.damage, 5f, projectile.owner);
                    if (projectile.owner == Main.myPlayer) Projectile.NewProjectile(projectile.position.X + (float)(projectile.width * (Main.rand.Next(50))), projectile.position.Y + (float)(projectile.height * (Main.rand.Next(60))), (Main.rand.Next(30)), (Main.rand.Next(30)), ModContent.ProjectileType<Projectiles.Enemy.EnemySpellBlaze>(), projectile.damage, 5f, projectile.owner);
                }

                Vector2 arg_1394_0 = new Vector2(projectile.position.X - projectile.velocity.X, projectile.position.Y - projectile.velocity.Y);
                int arg_1394_1 = projectile.width;
                int arg_1394_2 = projectile.height;
                int arg_1394_3 = 15;
                float arg_1394_4 = 0f;
                float arg_1394_5 = 0f;
                int arg_1394_6 = 100;
                Color newColor = default(Color);
                int num41 = Dust.NewDust(arg_1394_0, arg_1394_1, arg_1394_2, arg_1394_3, arg_1394_4, arg_1394_5, arg_1394_6, newColor, 2f);
                Main.dust[num41].noGravity = true;
                Dust expr_13B1 = Main.dust[num41];
                expr_13B1.velocity *= 2f;
                Vector2 arg_1422_0 = new Vector2(projectile.position.X - projectile.velocity.X, projectile.position.Y - projectile.velocity.Y);
                int arg_1422_1 = projectile.width;
                int arg_1422_2 = projectile.height;
                int arg_1422_3 = 15;
                float arg_1422_4 = 0f;
                float arg_1422_5 = 0f;
                int arg_1422_6 = 100;
                newColor = default(Color);
                num41 = Dust.NewDust(arg_1422_0, arg_1422_1, arg_1422_2, arg_1422_3, arg_1422_4, arg_1422_5, arg_1422_6, newColor, 1f);
            }
            projectile.type = 15;
            projectile.active = false;
        }
        #endregion
    }
}
