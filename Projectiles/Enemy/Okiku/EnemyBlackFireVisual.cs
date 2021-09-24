using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Okiku
{
	class EnemyBlackFireVisual : ModProjectile
	{
		public override string Texture => "tsorcRevamp/Projectiles/Enemy/EnemyBlackFire";
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Black Fire");

		}
		public override void SetDefaults()
		{
			projectile.width = 12;
			projectile.height = 12;
			projectile.scale = 1.5f;
			projectile.alpha = 50;
			projectile.aiStyle = -1;
			projectile.timeLeft = 360;
			projectile.friendly = false;
			projectile.hostile = true;
			projectile.penetrate = 1;
			projectile.light = 0.8f;
			projectile.magic = true;
			projectile.tileCollide = false;
			projectile.damage = 85;
			projectile.knockBack = 9;
		}
		public override void AI()
		{
			//projectile.AI(true);


			// Get the length of last frame's velocity
			float lastLength = (float)Math.Sqrt((projectile.velocity.X * projectile.velocity.X + projectile.velocity.Y * projectile.velocity.Y));

			// Align projectile facing with velocity normal
			projectile.rotation = (float)Math.Atan2(projectile.velocity.Y, projectile.velocity.X) - 2.355f;
			// Render fire particles [every frame]
			int particle = Dust.NewDust(projectile.position, projectile.width, projectile.height, 54, projectile.velocity.X * 0.2f, projectile.velocity.Y * 0.2f, 160, default(Color), 3f);
			Main.dust[particle].noGravity = true;
			Main.dust[particle].velocity *= 1.4f;
			int lol = Dust.NewDust(projectile.position, projectile.width, projectile.height, 58, projectile.velocity.X * 0.2f, projectile.velocity.Y * 0.2f, 160, default(Color), 3f);
			Main.dust[lol].noGravity = true;
			Main.dust[lol].velocity *= 1.4f;


			// Render smoke particles [every other frame]
			if (projectile.timeLeft % 10 == 0)
			{
				int particle2 = Dust.NewDust(projectile.position, projectile.width, projectile.height, 1, projectile.velocity.X * 0.2f, projectile.velocity.Y * 0.2f - 1f, 180, default(Color), 1f + (float)Main.rand.Next(2));
				Main.dust[particle2].noGravity = true;
				Main.dust[particle2].noLight = true;
				Main.dust[particle2].fadeIn = 3f;
			}			
		}

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
			target.AddBuff(ModContent.BuffType<Buffs.DarkInferno>(), 240, false);
		}
	}
}