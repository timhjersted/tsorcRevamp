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
            Main.PlaySound(2, (int)projectile.position.X, (int)projectile.position.Y, 10);
            
            Projectile.NewProjectile(projectile.position.X + (float)(projectile.width * (Main.rand.Next(50))), projectile.position.Y + (float)(projectile.height * (Main.rand.Next(60))), ((Main.rand.Next(30)) * -1), ((Main.rand.Next(30)) * -1), ModContent.ProjectileType<Projectiles.Enemy.EnemySpellBlaze>(), projectile.damage, 5f, projectile.owner);
            Projectile.NewProjectile(projectile.position.X + (float)(projectile.width * (Main.rand.Next(50))), projectile.position.Y + (float)(projectile.height * (Main.rand.Next(60))), ((Main.rand.Next(30)) * -1), ((Main.rand.Next(30)) * -1), ModContent.ProjectileType<Projectiles.Enemy.EnemySpellBlaze>(), projectile.damage, 5f, projectile.owner);
            Projectile.NewProjectile(projectile.position.X + (float)(projectile.width * (Main.rand.Next(50))), projectile.position.Y + (float)(projectile.height * (Main.rand.Next(60))), ((Main.rand.Next(30)) * -1), ((Main.rand.Next(30)) * -1), ModContent.ProjectileType<Projectiles.Enemy.EnemySpellBlaze>(), projectile.damage, 5f, projectile.owner);
            Projectile.NewProjectile(projectile.position.X + (float)(projectile.width * (Main.rand.Next(50))), projectile.position.Y + (float)(projectile.height * (Main.rand.Next(60))), (Main.rand.Next(30)), (Main.rand.Next(30)), ModContent.ProjectileType<Projectiles.Enemy.EnemySpellBlaze>(), projectile.damage, 5f, projectile.owner);
            Projectile.NewProjectile(projectile.position.X + (float)(projectile.width * (Main.rand.Next(50))), projectile.position.Y + (float)(projectile.height * (Main.rand.Next(60))), (Main.rand.Next(30)), (Main.rand.Next(30)), ModContent.ProjectileType<Projectiles.Enemy.EnemySpellBlaze>(), projectile.damage, 5f, projectile.owner);
            Projectile.NewProjectile(projectile.position.X + (float)(projectile.width * (Main.rand.Next(50))), projectile.position.Y + (float)(projectile.height * (Main.rand.Next(60))), (Main.rand.Next(30)), (Main.rand.Next(30)), ModContent.ProjectileType<Projectiles.Enemy.EnemySpellBlaze>(), projectile.damage, 5f, projectile.owner);

            Dust.NewDustDirect(new Vector2(projectile.position.X - projectile.velocity.X, projectile.position.Y - projectile.velocity.Y), projectile.width, projectile.height, 15, 0, 0, 100, default, 2f).noGravity = true;
            Dust.NewDustDirect(new Vector2(projectile.position.X - projectile.velocity.X, projectile.position.Y - projectile.velocity.Y), projectile.width, projectile.height, 15, 0, 0, 100, default, 1f).noGravity = true;
                        
            projectile.type = 15;
            projectile.active = false;
        }
        #endregion
    }
}
