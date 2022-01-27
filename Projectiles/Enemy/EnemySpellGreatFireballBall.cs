using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
	class EnemySpellGreatFireballBall  : ModProjectile
	{

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Enemy Spell Great Fireball Ball");

		}
		public override void SetDefaults()
		{
			projectile.hostile = true;
			projectile.height = 16;
			projectile.width = 16;
			projectile.light = 0.8f;
			projectile.magic = true;
			projectile.penetrate = 1;
			projectile.tileCollide = true;
		}

		public override void AI()
		{
			if (projectile.soundDelay == 0 && Math.Abs(projectile.velocity.X) + Math.Abs(projectile.velocity.Y) > 2f)
			{
				projectile.soundDelay = 10;
				Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 9);
			}
			int thisDust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 15, 0, 0, 100, default, 2f);
			Main.dust[thisDust].noGravity = true;

			projectile.rotation += 0.25f;
		}

        public override bool PreKill(int timeLeft)
        {
			Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 10);
			if (projectile.owner == Main.myPlayer)
			{
				Projectile.NewProjectile(projectile.position.X, projectile.position.Y, 0, 0, ModContent.ProjectileType<EnemySpellGreatFireball>(), projectile.damage, 6f, projectile.owner);
			}

			for (int i = 0; i < 5; i++)
			{
				Vector2 vel = Main.rand.NextVector2Circular(12, 12);
				int thisDust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 15, vel.X, vel.Y, 100, default, 2f);
				Main.dust[thisDust].noGravity = true;
			}
			return true;
		}
	}
}