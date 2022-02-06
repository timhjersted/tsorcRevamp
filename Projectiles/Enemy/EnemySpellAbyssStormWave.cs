using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
	public class EnemySpellAbyssStormWave : ModProjectile
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Dark Wave");
		}

		public override void SetDefaults()
		{
			projectile.aiStyle = 1;
			projectile.hostile = true;
			projectile.friendly = false;
			projectile.height = 16;
			projectile.light = 1;
			projectile.ranged = true;
			projectile.penetrate = 8;
			projectile.scale = 1.3f;
			projectile.tileCollide = true;
			projectile.width = 16;
			projectile.timeLeft = 600;
		}

		public override void AI()
		{
			Dust dust = Dust.NewDustPerfect(projectile.Center, 15, Alpha: 80, Scale: 2f);
			dust.noGravity = false;
			dust.velocity /= 3;
		}

        

        public override void Kill(int timeLeft)
		{
			Main.PlaySound(2, (int)projectile.position.X, (int)projectile.position.Y, 10);
			Projectile.NewProjectile(projectile.Center.X , projectile.Center.Y, 0, 0, ModContent.ProjectileType<EnemySpellAbyssStorm>(), projectile.damage, 8f, projectile.owner);
			Dust.NewDust(projectile.Center, projectile.width, projectile.height, 15, 0, 0, 100, default, 2f);
			Dust.NewDust(projectile.Center, projectile.width, projectile.height, 15, 0, 0, 100, default, 2f);
		}
	}
}