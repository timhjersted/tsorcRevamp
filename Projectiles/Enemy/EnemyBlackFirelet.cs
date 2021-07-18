using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
	class EnemyBlackFirelet : ModProjectile
	{

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Black Fire");

		}
		public override void SetDefaults()
		{
			projectile.width = 8;
			projectile.height = 8;
			projectile.scale = 1;
			projectile.alpha = 100;
			projectile.timeLeft = 200;
			projectile.penetrate = 2;
			projectile.light = 0.8f;
			projectile.magic = true;
			projectile.tileCollide = true;
			projectile.hostile = true;
			projectile.friendly = false;
			projectile.knockBack = 4;
		}
		public override void AI()
		{
			if (projectile.type == 96 && projectile.localAI[0] == 0f)
			{
				projectile.localAI[0] = 1f;
				Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 20);
			}
			if (projectile.type == 27)
			{
				int num40 = Dust.NewDust(new Vector2(projectile.position.X + projectile.velocity.X, projectile.position.Y + projectile.velocity.Y), projectile.width, projectile.height, DustID.DungeonWater_Old, projectile.velocity.X, projectile.velocity.Y, 100, default(Color), 3f);
				Main.dust[num40].noGravity = true;
				if (Main.rand.Next(10) == 0)
				{
					num40 = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, DustID.DungeonWater_Old, projectile.velocity.X, projectile.velocity.Y, 100, default(Color), 1.4f);
				}
			}
			else
			{
				if (projectile.type == 95 || projectile.type == 96)
				{
					int num41 = Dust.NewDust(new Vector2(projectile.position.X + projectile.velocity.X, projectile.position.Y + projectile.velocity.Y), projectile.width, projectile.height, 75, projectile.velocity.X, projectile.velocity.Y, 100, default(Color), 3f * projectile.scale);
					Main.dust[num41].noGravity = true;
				}
				else
				{
					for (int num42 = 0; num42 < 2; num42++)
					{
						int num43 = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 54, projectile.velocity.X * 0.2f, projectile.velocity.Y * 0.2f, 100, default(Color), 2f);
						Main.dust[num43].noGravity = true;
						Dust expr_225D_cp_0 = Main.dust[num43];
						expr_225D_cp_0.velocity.X = expr_225D_cp_0.velocity.X * 0.3f;
						Dust expr_227B_cp_0 = Main.dust[num43];
						expr_227B_cp_0.velocity.Y = expr_227B_cp_0.velocity.Y * 0.3f;
						int dust = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 58, projectile.velocity.X * 0.2f, projectile.velocity.Y * 0.2f, 100, default(Color), 2f);
						Main.dust[dust].noGravity = true;
						Dust dusty = Main.dust[dust];
						dusty.velocity.X = dusty.velocity.X * 0.3f;
						Dust dusty2 = Main.dust[dust];
						dusty2.velocity.Y = dusty2.velocity.Y * 0.3f;
					}
				}
			}
			if (projectile.type != 27 && projectile.type != 96)
			{
				projectile.ai[1] += 1f;
			}
			if (projectile.ai[1] >= 20f)
			{
				projectile.velocity.Y = projectile.velocity.Y + 0.2f;
			}
			projectile.rotation += 0.3f * (float)projectile.direction;
			if (projectile.velocity.Y > 16f)
			{
				projectile.velocity.Y = 16f;
				return;
			}

			Rectangle projrec = new Rectangle((int)projectile.position.X + (int)projectile.velocity.X, (int)projectile.position.Y + (int)projectile.velocity.Y, projectile.width, projectile.height);
			Rectangle prec = new Rectangle((int)Main.player[Main.myPlayer].position.X, (int)Main.player[Main.myPlayer].position.Y, (int)Main.player[Main.myPlayer].width, (int)Main.player[Main.myPlayer].height);

			if (projrec.Intersects(prec)) //&& Main.rand.Next(2) == 0
			{
				Main.player[Main.myPlayer].AddBuff(ModContent.BuffType<Buffs.DarkInferno>(), 240, false);
			}




		}
		public bool tileCollide(Vector2 CollideVel)
		{
			Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 10);
			projectile.ai[0] += 1f;
			if (projectile.ai[0] >= 3f)
			{
				projectile.position += projectile.velocity;
				projectile.Kill();
			}
			else
			{
				if (projectile.type == ModContent.ProjectileType<BlackFirelet>() && projectile.velocity.Y > 4f)
				{
					if (projectile.velocity.Y != CollideVel.Y)
					{
						projectile.velocity.Y = -CollideVel.Y * 0.8f;
					}
				}
				else
				{
					if (projectile.velocity.Y != CollideVel.Y)
					{
						projectile.velocity.Y = -CollideVel.Y;
					}
				}
				if (projectile.velocity.X != CollideVel.X)
				{
					projectile.velocity.X = -CollideVel.X;
				}
			}
			return false;
		}
	}
}