using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
	class EnemyBioSpitBall : ModProjectile
	{
		public override void SetDefaults()
		{
			projectile.aiStyle = 1;
			projectile.hostile = true;
			projectile.height = 24;
			projectile.light = 1;
			projectile.magic = true;
			projectile.penetrate = 1; //was 8
			projectile.tileCollide = true;
			projectile.width = 24;
			projectile.timeLeft = 70;
		}

		public override bool PreKill(int timeLeft)
		{
			projectile.type = 96; //killpretendtype
			return true;
		}

		public override void AI()
		{
			//if (projectile.soundDelay == 0 && Math.Abs(projectile.velocity.X) + Math.Abs(projectile.velocity.Y) > 2f)
			//{
			//	projectile.soundDelay = 10;
			Main.PlaySound(2, (int)projectile.position.X, (int)projectile.position.Y, 17);
			//}

			projectile.rotation += 1f;
			if (Main.rand.Next(2) == 0)
			{
				int dust = Dust.NewDust(new Vector2((float)projectile.position.X, (float)projectile.position.Y), projectile.width, projectile.height, 6, 0, 0, 50, Color.Green, 3.0f);
				Main.dust[dust].noGravity = false;
			}
			Lighting.AddLight((int)(projectile.position.X / 16f), (int)(projectile.position.Y / 16f), 0.4f, 0.1f, 0.1f);

			if (projectile.velocity.X <= 4 && projectile.velocity.Y <= 4 && projectile.velocity.X >= -4 && projectile.velocity.Y >= -4)
			{
				float accel = 1f + (Main.rand.Next(10, 30) * 0.001f);
				projectile.velocity.X *= accel;
				projectile.velocity.Y *= accel;
			}


		}

		public override void OnHitPlayer(Player target, int damage, bool crit)
		{
			//Was 300, cut in half to counter expert mode doubling it
			target.AddBuff(30, 150, false); //bleeding
			target.AddBuff(20, 150, false); //poisoned
			target.AddBuff(ModContent.BuffType<Buffs.CurseBuildup>(), 18000, false); //-20 HP after several hits
			target.GetModPlayer<tsorcRevampPlayer>().CurseLevel += 5;

		}
	}
}

