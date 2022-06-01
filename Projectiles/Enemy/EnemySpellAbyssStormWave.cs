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
			Projectile.aiStyle = 1;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.height = 16;
			Projectile.light = 1;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = 8;
			Projectile.scale = 1.3f;
			Projectile.tileCollide = true;
			Projectile.width = 16;
			Projectile.timeLeft = 600;
		}

		public override void AI()
		{
			Dust dust = Dust.NewDustPerfect(Projectile.Center, 15, Alpha: 80, Scale: 2f);
			dust.noGravity = false;
			dust.velocity /= 3;
		}

        

        public override void Kill(int timeLeft)
		{
			Terraria.Audio.SoundEngine.PlaySound(2, (int)Projectile.position.X, (int)Projectile.position.Y, 10);
			Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center.X , Projectile.Center.Y, 0, 0, ModContent.ProjectileType<EnemySpellAbyssStorm>(), Projectile.damage, 8f, Projectile.owner);
			Dust.NewDust(Projectile.Center, Projectile.width, Projectile.height, 15, 0, 0, 100, default, 2f);
			Dust.NewDust(Projectile.Center, Projectile.width, Projectile.height, 15, 0, 0, 100, default, 2f);
		}
	}
}