using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
	class EnemyCursedBreath : ModProjectile
	{
		public override void SetDefaults()
		{
			projectile.width = 16;
			projectile.height = 16;
			projectile.alpha = 255;
			projectile.aiStyle = 8;
			projectile.timeLeft = 50;
			projectile.friendly = false;
			projectile.light = 0.8f;
			projectile.penetrate = 2; //was 4, was causing curse buildup way too fast
			projectile.tileCollide = true;
			aiType = 96;
			projectile.magic = true;
			projectile.hostile = true;
			projectile.ignoreWater = true;
		}
		public override void AI()
		{
			projectile.rotation += 3f;

			int dust = Dust.NewDust(new Vector2((float)projectile.position.X, (float)projectile.position.Y), projectile.width, projectile.height, 75, 0, 0, 50, Color.Chartreuse, 3.0f);
			Main.dust[dust].noGravity = true;

			//if (projectile.velocity.X <= 4 && projectile.velocity.Y <= 4 && projectile.velocity.X >= -4 && projectile.velocity.Y >= -4)
			//{
			//float accel = 2f+(Main.rand.Next(10,30)*0.001f);
			//projectile.velocity.X *= accel;
			//projectile.velocity.Y *= accel;
			//}
		}
		public override void OnHitPlayer(Player target, int damage, bool crit)
		{
			

			if (Main.rand.Next(12) == 0)
			{
				//Vanilla Debuffs cut in half to counter expert mode doubling them
				target.AddBuff(ModContent.BuffType<Buffs.PowerfulCurseBuildup>(), 36000, false);
				
			}

			if (Main.rand.Next(6) == 0)
			{
				target.AddBuff(39, 150, false); //cursed flames
				target.AddBuff(30, 1800, false); //bleeding
				target.AddBuff(33, 1800, false); //weak
			}
		}
	}
}