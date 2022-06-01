using Microsoft.Xna.Framework;
using Terraria;
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
            Projectile.aiStyle = 1;
            Projectile.width = 16;
            Projectile.height = 16;
            //projectile.noGravity = true;
            Projectile.hostile = true;
            Projectile.light = 1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
        }


        #region Kill
        public override void Kill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(2, (int)Projectile.position.X, (int)Projectile.position.Y, 10);

            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width * (Main.rand.Next(50))), Projectile.position.Y + (float)(Projectile.height * (Main.rand.Next(60))), ((Main.rand.Next(30)) * -1), ((Main.rand.Next(30)) * -1), ModContent.ProjectileType<Projectiles.Enemy.EnemySpellBlaze>(), Projectile.damage, 5f, Projectile.owner);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width * (Main.rand.Next(50))), Projectile.position.Y + (float)(Projectile.height * (Main.rand.Next(60))), ((Main.rand.Next(30)) * -1), ((Main.rand.Next(30)) * -1), ModContent.ProjectileType<Projectiles.Enemy.EnemySpellBlaze>(), Projectile.damage, 5f, Projectile.owner);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width * (Main.rand.Next(50))), Projectile.position.Y + (float)(Projectile.height * (Main.rand.Next(60))), ((Main.rand.Next(30)) * -1), ((Main.rand.Next(30)) * -1), ModContent.ProjectileType<Projectiles.Enemy.EnemySpellBlaze>(), Projectile.damage, 5f, Projectile.owner);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width * (Main.rand.Next(50))), Projectile.position.Y + (float)(Projectile.height * (Main.rand.Next(60))), (Main.rand.Next(30)), (Main.rand.Next(30)), ModContent.ProjectileType<Projectiles.Enemy.EnemySpellBlaze>(), Projectile.damage, 5f, Projectile.owner);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width * (Main.rand.Next(50))), Projectile.position.Y + (float)(Projectile.height * (Main.rand.Next(60))), (Main.rand.Next(30)), (Main.rand.Next(30)), ModContent.ProjectileType<Projectiles.Enemy.EnemySpellBlaze>(), Projectile.damage, 5f, Projectile.owner);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width * (Main.rand.Next(50))), Projectile.position.Y + (float)(Projectile.height * (Main.rand.Next(60))), (Main.rand.Next(30)), (Main.rand.Next(30)), ModContent.ProjectileType<Projectiles.Enemy.EnemySpellBlaze>(), Projectile.damage, 5f, Projectile.owner);

            Dust.NewDustDirect(new Vector2(Projectile.position.X - Projectile.velocity.X, Projectile.position.Y - Projectile.velocity.Y), Projectile.width, Projectile.height, 15, 0, 0, 100, default, 2f).noGravity = true;
            Dust.NewDustDirect(new Vector2(Projectile.position.X - Projectile.velocity.X, Projectile.position.Y - Projectile.velocity.Y), Projectile.width, Projectile.height, 15, 0, 0, 100, default, 1f).noGravity = true;

            Projectile.type = 15;
            Projectile.active = false;
        }
        #endregion
    }
}
