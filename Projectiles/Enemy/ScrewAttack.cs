using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
	class ScrewAttack : ModProjectile
	{
		public override void SetStaticDefaults()
		{
			Main.projFrames[projectile.type] = 4;
		}
		public override void SetDefaults()
		{
			projectile.friendly = false;
			projectile.hostile = true;
			projectile.tileCollide = false;
			projectile.height = 34;
			projectile.width = 34;
			projectile.timeLeft = 1500;
			projectile.scale = 2f;
			projectile.light = 1;
		}
		public override void AI()
		{
			projectile.rotation += 0.5f;

			if (Main.player[(int)projectile.ai[0]].position.X < projectile.position.X)
			{
				if (projectile.velocity.X > -10) projectile.velocity.X -= 0.1f;
			}

			if (Main.player[(int)projectile.ai[0]].position.X > projectile.position.X)
			{
				if (projectile.velocity.X < 10) projectile.velocity.X += 0.1f;
			}

			if (Main.player[(int)projectile.ai[0]].position.Y < projectile.position.Y)
			{
				if (projectile.velocity.Y > -10) projectile.velocity.Y -= 0.1f;
			}

			if (Main.player[(int)projectile.ai[0]].position.Y > projectile.position.Y)
			{
				if (projectile.velocity.Y < 10) projectile.velocity.Y += 0.1f;
			}

			if (Main.rand.Next(4) == 0)
			{
				int dust = Dust.NewDust(new Vector2((float)projectile.position.X, (float)projectile.position.Y), projectile.width, projectile.height, 5, 0, 0, 50, Color.White, 1.0f);
				Main.dust[dust].noGravity = false;
			}
			Lighting.AddLight(projectile.position, 0.7f, 0.2f, 0.2f);

			projectile.frameCounter++;
			if (projectile.frameCounter > 2)
			{
				projectile.frame++;
				projectile.frameCounter = 3;
			}
			if (projectile.frame >= 4)
			{
				projectile.frame = 0;
			}
		}
		public override void OnHitPlayer(Player target, int damage, bool crit)
		{
			target.AddBuff(BuffID.Battle, 600);
			target.AddBuff(BuffID.BrokenArmor, 300);
			target.AddBuff(BuffID.Poisoned, 3600);
			target.AddBuff(BuffID.Bleeding, 7200);
		}

	}
}
