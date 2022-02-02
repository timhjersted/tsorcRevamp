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
			projectile.aiStyle = 8; //8 with 96 AI Style works; with no AIType it rained down 5 streams like a firework, good if launched above player (23 is a orange flame)
			projectile.timeLeft = 60;
			projectile.friendly = false;
			projectile.light = 0.8f;
			projectile.penetrate = 3; //was 4, was causing curse buildup way too fast
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
		}

		/*
		public override bool PreKill(int timeLeft)
		{
			Main.PlaySound(6, (int)projectile.position.X, (int)projectile.position.Y, 0, 0.04f, 0f);//grass cut / acid singe sound
			int dust = Dust.NewDust(new Vector2((float)projectile.position.X, (float)projectile.position.Y), projectile.width, projectile.height, 6, 0, 0, 50, Color.Green, 3.0f);
			Dust.NewDust(projectile.position, projectile.width, projectile.height, 71, 0.3f, 0.3f, 200, default, 1f);
			Dust.NewDust(projectile.position, projectile.height, projectile.width, 71, 0.2f, 0.2f, 200, default, 2f);
			Dust.NewDust(projectile.position, projectile.width, projectile.height, 71, 0.2f, 0.2f, 200, default, 2f);
			Main.dust[dust].noGravity = false;
			//projectile.type = 96; //killpretendtype
			return true;
		}
		*/
		public override void OnHitPlayer(Player target, int damage, bool crit)
		{
			

			if (Main.rand.Next(3) == 0) //was 12
			{
				//Vanilla Debuffs cut in half to counter expert mode doubling them
				target.AddBuff(ModContent.BuffType<Buffs.PowerfulCurseBuildup>(), 36000, false);
				//target.GetModPlayer<tsorcRevampPlayer>().PowerfulCurseLevel += 10;
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