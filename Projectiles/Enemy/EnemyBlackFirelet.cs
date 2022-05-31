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
			Projectile.width = 8;
			Projectile.height = 8;
			Projectile.scale = 1;
			Projectile.alpha = 100;
			Projectile.timeLeft = 200;
			Projectile.penetrate = 2;
			Projectile.light = 0.8f;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.tileCollide = true;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.knockBack = 4;
		}
		public override void AI()
		{
			if (Projectile.type == 96 && Projectile.localAI[0] == 0f)
			{
				Projectile.localAI[0] = 1f;
				Main.PlaySound(SoundID.Item, (int)Projectile.position.X, (int)Projectile.position.Y, 20);
			}
			if (Projectile.type == 27)
			{
				int num40 = Dust.NewDust(new Vector2(Projectile.position.X + Projectile.velocity.X, Projectile.position.Y + Projectile.velocity.Y), Projectile.width, Projectile.height, DustID.DungeonWater_Old, Projectile.velocity.X, Projectile.velocity.Y, 100, default(Color), 3f);
				Main.dust[num40].noGravity = true;
				if (Main.rand.Next(10) == 0)
				{
					num40 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.DungeonWater_Old, Projectile.velocity.X, Projectile.velocity.Y, 100, default(Color), 1.4f);
				}
			}
			else
			{
				if (Projectile.type == 95 || Projectile.type == 96)
				{
					int num41 = Dust.NewDust(new Vector2(Projectile.position.X + Projectile.velocity.X, Projectile.position.Y + Projectile.velocity.Y), Projectile.width, Projectile.height, 75, Projectile.velocity.X, Projectile.velocity.Y, 100, default(Color), 3f * Projectile.scale);
					Main.dust[num41].noGravity = true;
				}
				else
				{
					for (int num42 = 0; num42 < 2; num42++)
					{
						int num43 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 54, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100, default(Color), 2f);
						Main.dust[num43].noGravity = true;
						Dust expr_225D_cp_0 = Main.dust[num43];
						expr_225D_cp_0.velocity.X = expr_225D_cp_0.velocity.X * 0.3f;
						Dust expr_227B_cp_0 = Main.dust[num43];
						expr_227B_cp_0.velocity.Y = expr_227B_cp_0.velocity.Y * 0.3f;
						int dust = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 58, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100, default(Color), 2f);
						Main.dust[dust].noGravity = true;
						Dust dusty = Main.dust[dust];
						dusty.velocity.X = dusty.velocity.X * 0.3f;
						Dust dusty2 = Main.dust[dust];
						dusty2.velocity.Y = dusty2.velocity.Y * 0.3f;
					}
				}
			}
			if (Projectile.type != 27 && Projectile.type != 96)
			{
				Projectile.ai[1] += 1f;
			}
			if (Projectile.ai[1] >= 20f)
			{
				Projectile.velocity.Y = Projectile.velocity.Y + 0.2f;
			}
			Projectile.rotation += 0.3f * (float)Projectile.direction;
			if (Projectile.velocity.Y > 16f)
			{
				Projectile.velocity.Y = 16f;
				return;
			}
		}

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
			target.AddBuff(ModContent.BuffType<Buffs.DarkInferno>(), 240, false);
		}

        public bool tileCollide(Vector2 CollideVel)
		{
			Main.PlaySound(SoundID.Item, (int)Projectile.position.X, (int)Projectile.position.Y, 10);
			Projectile.ai[0] += 1f;
			if (Projectile.ai[0] >= 3f)
			{
				Projectile.position += Projectile.velocity;
				Projectile.Kill();
			}
			else
			{
				if (Projectile.type == ModContent.ProjectileType<BlackFirelet>() && Projectile.velocity.Y > 4f)
				{
					if (Projectile.velocity.Y != CollideVel.Y)
					{
						Projectile.velocity.Y = -CollideVel.Y * 0.8f;
					}
				}
				else
				{
					if (Projectile.velocity.Y != CollideVel.Y)
					{
						Projectile.velocity.Y = -CollideVel.Y;
					}
				}
				if (Projectile.velocity.X != CollideVel.X)
				{
					Projectile.velocity.X = -CollideVel.X;
				}
			}
			return false;
		}
	}
}